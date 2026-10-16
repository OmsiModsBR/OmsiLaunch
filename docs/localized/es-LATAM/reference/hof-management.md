# Gestión de HOF

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Traducción de la [página original en inglés](../../../reference/hof-management.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

OMSI lee los archivos HOF (`*.hof`: destinos, números de línea y listas de paradas que muestra el IBIS o el letrero de destino del vehículo) desde la carpeta de cada vehículo. La gestión de HOF copia archivos HOF en carpetas de vehículos **durante una sola sesión**: un mapa o un complemento puede distribuir los HOF que necesitan sus líneas, y un usuario puede reutilizar los HOF de un vehículo en otros, sin cambiar ningún vehículo de forma permanente. Esta página es la referencia normativa, tal como la implementan `HofPlanner` en `src/OmsiLaunch.Core/HofPlanner.cs`, el campo `HofManagement` del [LaunchSpec](launchspec.md), los flags `/hof-source`, `/hof-target`, `/hof-file` y `/hof-mode` de la [CLI](cli.md) y el bloque `hofs` de los [perfiles de sesión](session-profiles.md).

Estabilidad: `STABLE_BETA` para la planificación, las reglas de conflicto, el confinamiento y la restauración transaccional (gates offline `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Validado en runtime: una sesión de espejo con restauración exacta y recuperación tras un fallo mediante `/recover` (consulte el [estado de validación en runtime](../status/runtime-validation-status.md)).

La gestión de HOF no es lo mismo que `PlayerVehicle.Hof` / `/hof:<id>`, que selecciona el HOF del vehículo del jugador al iniciar y está `UNAVAILABLE` en este build.

<a id="model"></a>
## Modelo

Una solicitud es una lista de operaciones (`HofManagementSpec.Operations`). Cada `HofOperationSpec` tiene:

| Campo | Significado |
| --- | --- |
| `SourceKind`, `Source` | De dónde provienen los archivos HOF (tabla siguiente). |
| `TargetVehicles` | Uno o más vehículos instalados: la identidad de un archivo `.bus` u `.ovh` (`Vehicles\MAN_SD200\MAN_SD77.bus`) o una carpeta de vehículo (`Vehicles\MAN_SD200`), ambas bajo `Vehicles\`. Varias identidades de una misma carpeta direccionan esa carpeta una sola vez. |
| `Mode` | `Add` (predeterminado): copia los HOF del origen en cada carpeta de destino; los demás HOF que haya allí se conservan. `Mirror`: copia los HOF del origen y elimina durante la sesión todos los demás `.hof` de la carpeta de destino, de modo que el conjunto de HOF de la carpeta sea igual al del origen. |
| `Files` | Lista opcional de nombres de archivo que se toman del origen (no distingue mayúsculas de minúsculas). Si se omite: todos los `*.hof` que estén directamente en el origen. Un nombre de la lista que el origen no contiene: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Se resuelve a |
| --- | --- | --- |
| `Collection` | un nombre simple, por ejemplo `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: una colección que mantiene el usuario o una herramienta. |
| `Directory` | un directorio relativo a la instalación, por ejemplo `maps\RMG Leste\hof` | los `*.hof` de ese directorio: HOF distribuidos junto con el contenido de un mapa o complemento. |
| `Vehicle` | una identidad o carpeta de vehículo, como en los destinos | los `*.hof` de la carpeta de ese vehículo: reutilizar los HOF de un vehículo en otros. |
| `File` | un archivo `.hof` relativo a la instalación | ese único archivo. |

Solo se usan los archivos que están directamente en el directorio de origen (sin recursión). Nunca se siguen vínculos simbólicos ni junctions (consulte [confinamiento](#confinement)).

<a id="what-a-session-does"></a>
## Qué hace una sesión

1. **Planificación** (`PlanSessionAsync`, `/plan`). Se resuelve cada operación: se leen los orígenes, se comprueban los destinos y se detectan los conflictos. Cada archivo que va a cambiar aparece en `SessionPlan.PlannedMutations` con `SemanticKey` `hof.add` o `hof.mirror`, `RequestedValue` = la identidad del origen y `Operation` `exact-file-overlay` (escrito) o `session-deletion` (eliminado por un espejo); las rutas forman parte de `TouchedFiles`. Un problema hace que el plan no sea ejecutable, con los códigos que se indican más abajo.
2. **Inicio**. Después de recuperar un journal obsoleto, las operaciones se resuelven de nuevo a partir de los archivos reales y se entregan a la transacción de la sesión: los archivos escritos son overlays y los archivos eliminados por el espejo son eliminaciones de sesión. Un archivo de destino cuyos bytes ya son iguales a los del origen no se toca y no es una entrada de la transacción.
3. **Fin de la sesión, fallo, caída.** La transacción restaura exactamente cada carpeta de destino: los archivos que existían recuperan sus bytes, marcas de tiempo y atributos originales; los archivos que creó la sesión se eliminan; los archivos eliminados por el espejo vuelven. Tras una caída del propietario, la misma restauración se ejecuta en el siguiente inicio o mediante `/recover` (consulte [transacciones y recuperación](../concepts/transactions-and-recovery.md)).

No se escribe nada fuera de las carpetas de destino. Los archivos de origen solo se leen. Una sesión nunca modifica `.omsilaunch\hof\`.

<a id="conflict-rules"></a>
## Reglas de conflicto

Las operaciones se aplican en orden y deben coincidir entre sí:

- Dos operaciones que escriben el mismo archivo de destino con los mismos bytes se combinan; con bytes distintos: `OL_E_HOF_CONFLICT`.
- Un espejo que eliminaría un archivo que escribe una operación anterior, o una operación que escribe un archivo que elimina un espejo anterior: `OL_E_HOF_CONFLICT`.
- Un espejo cubre el conjunto de HOF de sesión de la carpeta: los archivos que existen más los archivos que escriben allí las operaciones anteriores.
- Un destino de HOF que también escribe la presentación de la sesión (splash o Internet Textures): `OL_E_HOF_CONFLICT` (no puede ocurrir con las rutas de presentación actuales, que están fuera de `Vehicles\`).
- Cuando un destino ya tiene un archivo con el mismo nombre en otra combinación de mayúsculas y minúsculas, se conserva la grafía existente.

<a id="confinement"></a>
## Confinamiento

Todo origen y todo destino debe permanecer dentro de la instalación: las rutas absolutas, los segmentos `..` y las rutas que salen de la raíz son `OL_E_HOF_PATH_ESCAPE`; también lo son los destinos (y los orígenes `Vehicle`) fuera de `Vehicles\`, los nombres de colección que no son un nombre de directorio simple y cualquier origen o carpeta de destino alcanzado a través de una junction o un vínculo simbólico. Los archivos `.hof` que son por sí mismos puntos de reanálisis se ignoran.

<a id="limits"></a>
## Límites

| Límite | Valor | Error |
| --- | --- | --- |
| Tamaño de un archivo HOF | 8 MiB | `OL_E_HOF_INVALID` |
| Archivos por operación | 256 | `OL_E_HOF_INVALID` |
| Total de bytes escritos por sesión | 256 MiB | `OL_E_HOF_INVALID` |
| Destinos por operación | al menos 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Códigos de error

| Código | Causa |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | La colección, el directorio, el vehículo o el archivo no existe, no contiene ningún `.hof` o le falta un archivo nombrado en `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Un archivo o carpeta de vehículo de destino no existe. |
| `OL_E_HOF_PATH_ESCAPE` | Una ruta sale de la instalación, un destino no está bajo `Vehicles\`, un nombre de colección no es un nombre simple, o se atraviesa una junction/symlink. |
| `OL_E_HOF_CONFLICT` | Dos operaciones no coinciden sobre un mismo archivo de destino (consulte las [reglas de conflicto](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Sin destinos, sin origen, un origen `File` que no es `.hof`, un destino que es un archivo distinto de `.bus`/`.ovh`, o se superó un [límite](#limits). |

<a id="using-it"></a>
## Uso

Línea de comandos (una operación; varias operaciones requieren `/spec` o un perfil de sesión):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (JSON de `/spec`, enums como enteros):

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

Preset de un perfil de sesión (un origen `package` es un directorio dentro del paquete del perfil):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: establezca `LaunchSpec.HofManagement` y llame a `PlanSessionAsync` / `StartSessionAsync` como de costumbre; `/list:Hofs` (`ContentQueryKind.Hofs`) enumera los archivos HOF instalados.

Relacionado: [LaunchSpec](launchspec.md), [perfiles de sesión](session-profiles.md), [CLI](cli.md), [errores](errors.md), [transacciones y recuperación](../concepts/transactions-and-recovery.md).
