# Documentación de OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Traducción de la [página original en inglés](../../README.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta es la documentación normativa en inglés de OmsiLaunch `0.1.0-beta4`, la
línea base posterior al endurecimiento (hardening). OmsiLaunch proporciona inicio programable, propiedad de
la sesión y control en runtime para un OMSI 2 instalado; cada capacidad nativa
se resuelve y verifica por build (consulte [compatibilidad](reference/compatibility.md)). Cada página bajo `docs/` describe lo que hace el código
actual; cuando una página y el código no coinciden, prevalece el código y la página tiene un error.

Vocabulario de estabilidad usado en toda la documentación: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Los flags que se analizan pero no hacen nada se marcan como
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Nada se considera
validado en runtime a menos que el
[estado de validación en runtime](status/runtime-validation-status.md) lo indique.

<a id="who-reads-what"></a>
## Quién lee qué

| Público | Empiece aquí | Después |
| --- | --- | --- |
| Usuarios (CLI, accesos directos, perfiles de sesión) | [Instalación](getting-started/installation.md), [Primera sesión](getting-started/first-session.md) | [Referencia de la CLI](reference/cli.md), [Ejemplos de la CLI](reference/cli-examples.md), [Perfiles de sesión](reference/session-profiles.md), [Bandeja de Windows](reference/windows-tray.md), [Códigos de salida](reference/exit-codes.md) |
| Integradores (`OmsiLaunch.Api`, IPC local) | [Inicio rápido de la API pública](getting-started/api-quick-start.md), [Referencia de la API pública](reference/public-api.md), [Referencia de LaunchSpec](reference/launchspec.md) | [Ciclo de vida de la sesión](concepts/session-lifecycle.md), [Control en runtime](reference/runtime-control.md), [Capacidades](reference/capabilities.md), [Control local / IPC](reference/local-control.md), [Referencia de errores](reference/errors.md) |
| Mantenedores (release, validación, límites) | [Empaquetado](reference/packaging.md), [Modelo de plugin permanente](concepts/permanent-plugin.md) | [Transacciones y recuperación](concepts/transactions-and-recovery.md), [Compatibilidad](reference/compatibility.md), [Limitaciones conocidas](reference/known-limitations.md), [Estado de validación en runtime](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Navegación

| Página | Propósito |
| --- | --- |
| [Primeros pasos](getting-started/first-session.md) | Planificar, iniciar, observar y detener una sesión desde la raíz de OMSI. |
| [Instalación](getting-started/installation.md) | Requisitos previos, extracción del paquete en la raíz de OMSI, verificación con `/version`, desinstalación limpia. |
| [Inicio rápido de la API pública](getting-started/api-quick-start.md) | Un programa .NET completo que planifica, inicia, lee y detiene una sesión. |
| [Referencia de la CLI](reference/cli.md) | Cada flag, palabra de comando y ruta jerárquica de `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Ejemplos de la CLI](reference/cli-examples.md) | Líneas de comando para copiar y pegar en tareas comunes. |
| [Referencia de LaunchSpec](reference/launchspec.md) | Cada propiedad y valor de enum de `LaunchSpec`, reglas de carga de JSON para `/spec`. |
| [Gestión de HOF](reference/hof-management.md) | Copia de HOF con alcance de sesión y espejo en las carpetas de vehículos: orígenes, destinos, conflictos, restauración. |
| [Módulos de runtime](../../reference/runtime-modules.md) | Módulos que se ejecutan dentro de OMSI con una sesión: manifiesto, confianza, ciclo de vida, políticas de fallo, operaciones. |
| [Referencia de perfiles de sesión](reference/session-profiles.md) | Esquema `omsilaunch.session-profile/v1` de `profile.yaml`, claves, límites, precedencia. |
| [Referencia de la API pública](reference/public-api.md) | `IOmsiLaunch`, records y enums públicos, estabilidad por miembro. |
| [Inventario de la API pública](reference/public-api-inventory.md) | Lista generada de cada tipo y miembro público con firma y estabilidad. |
| [Control en runtime](reference/runtime-control.md) | Canal de comandos de runtime, timeouts, handles, semántica de detención. |
| [Referencia de capacidades](reference/capabilities.md) | Catálogo de capacidades y cada id de operación de runtime pública con su clasificación. |
| [Ciclo de vida de la sesión](concepts/session-lifecycle.md) | Transiciones de `SessionState`, lo que promete `StartSessionAsync`, cómo termina una sesión. |
| [Transacciones y recuperación](concepts/transactions-and-recovery.md) | Estados del journal, copias de seguridad, verificación de la restauración, eliminaciones de la sesión, recuperación tras un fallo. |
| [Modelo de plugin permanente](concepts/permanent-plugin.md) | El conjunto `plugins\OmsiLaunch.*`, integridad basada en el manifiesto, lo que una sesión nunca toca. |
| [Control local / IPC](reference/local-control.md) | Protocolo de named pipe `0.1`, endpoint por instalación, vinculación a `session_id`, modelo de confianza. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | El host de Windows (sin consola): diferencias con `OmsiLaunch.exe`, `/silent`, diálogos, códigos de salida. |
| [Bandeja de Windows](reference/windows-tray.md) | Indicador del área de notificación: ícono, menú, ventana de estado campo por campo, finalizar la sesión (End session), reinicio del Explorador de Windows. |
| [Referencia de errores](reference/errors.md) | Cada código `OL_E_*` / `OL_W_*` con su categoría y significado. |
| [Códigos de salida](reference/exit-codes.md) | Valores de `PublicExitCode` de 0 a 10 y códigos del shim de arranque (bootstrapper) de 100 a 106. |
| [Empaquetado / estructura de la instalación](reference/packaging.md) | Archivos del ZIP de la release, `release-manifest.json`, estructura de `.omsilaunch\`. |
| [Compatibilidad / builds de OMSI compatibles](reference/compatibility.md) | El único hash de `Omsi.exe` compatible, el hash de Steam LAA aceptado, requisitos de plataforma. |
| [Limitaciones conocidas](reference/known-limitations.md) | Lo que no es compatible, es parcial o es un riesgo aceptado en esta beta. |
| [Estado de validación en runtime](status/runtime-validation-status.md) | Lo que se ejecutó bajo OMSI, lo que solo se ejecutó offline y lo que todavía necesita una sesión real. |

Páginas que siguen siendo normativas para los mantenedores: el
[`README.md`](../../../README.md) del repositorio, las especificaciones bajo `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) y las páginas del proyecto bajo
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Esas páginas resumen; las
páginas anteriores son la referencia detallada. Las páginas históricas se enumeran en el
[manifiesto de la documentación](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Cómo se mantiene sincronizada esta documentación

Un gate de documentación (control de calidad de la documentación), `tests\OmsiLaunch.DocumentationTests`, se compila contra
`OmsiLaunch.Api` y `OmsiLaunch.Core` y compara las páginas anteriores con el
código que define la superficie pública:

| Gate | Qué comprueba |
| --- | --- |
| `docs.cli-flags` | Cada entrada de `CliInput.KnownFlags` aparece en la referencia de la CLI como `` `/flag` `` o `` `/flag:` ``; cada entrada de `CliInput.AcceptedNoEffectFlags` está marcada como `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` en su línea; cada palabra de comando y cada ruta de `CliInput.HierarchicalRoutes` aparece con su operación de runtime; cada valor de `PublicExitCode` tiene una fila `| n |` en la tabla de códigos de salida. |
| `docs.capabilities` | Cada id de `PublicCapabilityRegistry.All`, cada entrada de `PublicCapabilityRegistry.PublicRuntimeOperationIds` y cada nombre de `PublicCapabilityClassification` aparece en la referencia de capacidades. |
| `docs.errors` | Cada código de `PublicErrorCodes.All` aparece en la referencia de errores, y ningún literal `OL_E_*` / `OL_W_*` de `src\` o `tools\OmsiLaunch.Cli\` falta en `PublicErrorCodes`. |
| `docs.public-api` | Cada tipo exportado de `OmsiLaunch.Api`, cada valor de enum y cada miembro de `IOmsiLaunch` aparece en la referencia de la API pública, y se usan las cinco palabras de estabilidad. |
| `docs.launchspec` | Cada propiedad pública accesible desde `LaunchSpec` y cada valor de sus enums aparece en la referencia de LaunchSpec. |
| `docs.session-profiles` | Cada clave de `SessionProfileCompiler.SchemaKeys`, el identificador del esquema y el límite de `256 KiB` aparecen en la referencia de perfiles de sesión. |
| `docs.structure` | Cada página de la tabla de navegación existe. |
| `docs.links` | Cada enlace relativo en `docs\**\*.md` (excepto `docs\localized\`) y en los archivos `*.md` de la raíz se resuelve a un archivo o directorio. |
| `docs.localization` | Cada locale listado en `docs\localized\LOCALIZATION-MANIFEST.md` tiene todas las páginas del conjunto localizado; cada página conserva los encabezados, las tablas y los bloques de código de la página en inglés, cada fragmento de código en línea (flags, ids de capacidades y de operaciones, códigos de error, claves, identificadores) y cada enlace, y sus enlaces relativos se resuelven. |

El gate es una de las suites que ejecuta `tools\Invoke-OfflineValidation.ps1`
(se omite con `-SkipDocs`). Se ejecuta offline, nunca inicia OMSI y hace fallar el
build cuando un flag, una ruta, una capacidad, un código de error, un valor de enum o un tipo público
no está documentado o cuando un enlace está roto. No comprueba la prosa, por lo que una página todavía puede
estar equivocada sobre el comportamiento; en ese caso, informe un error contra esa página.

<a id="translations"></a>
## Traducciones

`docs\localized\<locale>\` contiene traducciones completas de esta documentación
`0.1.0-beta4` para `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` y `ja-JP`. El conjunto de páginas, las raíces de cada locale y las
páginas que intencionalmente no se traducen se enumeran en
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
Las traducciones conservan sin cambios cada comando, flag, identificador, código de error y ejemplo de
las páginas en inglés, y el gate `docs.localization` lo comprueba.
Las páginas en inglés siguen siendo la fuente normativa: cuando una traducción no coincide
con ellas, la página en inglés y el código son los que prevalecen, y la traducción
tiene un error.
