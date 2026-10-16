# Compatibilité

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> Traduction de la [page originale en anglais](../../../reference/compatibility.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

OmsiLaunch fonctionne avec un **OMSI 2** installé. Il n'exige pas un fichier `Omsi.exe` particulier : chaque hypothèse native qu'il formule à l'intérieur d'OMSI (une fonction, un site de patch, une classe, un slot de VMT, une globale, un décalage de champ) est résolue et vérifiée individuellement, dans le processus, avant d'être utilisée, et une capacité n'est disponible que lorsque toutes ses hypothèses sont vérifiées. Cette page indique ce que signifie « OMSI 2 installé », comment les builds sont identifiés, comment les capacités sont résolues, ainsi que les exigences du système d'exploitation et du runtime. Sources : `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Exigence : OMSI 2 installé

Un plan n'est refusé avec `OL_E_UNSUPPORTED_BUILD` que lorsque l'installation n'est pas une installation d'OMSI 2 :

| Vérification | Refusé lorsque |
| --- | --- |
| `Omsi.exe` | absent de la racine de l'installation |
| Image | n'est pas une image PE x86 32 bits |
| Version | sa ressource de version ne porte pas de version d'OMSI 2 (2.x) |
| Installation | la racine n'a pas de répertoire `maps\` ou `Vehicles\` |

Le SHA-256, la taille et la version exacte de l'exécutable ne sont **pas** des critères de compatibilité. Ils sont enregistrés comme diagnostics (`omsi.build.detected`, `OmsiLaunch.exe profiles`) et servent de preuves.

<a id="build-identity-and-capability-resolution"></a>
## Identité du build et résolution des capacités

| Concept | Signification |
| --- | --- |
| Identité de code | SHA-256 de la section de code principale de l'exécutable. Une identité connue sélectionne une disposition connue comme chemin rapide. Les variantes qui ne diffèrent que par leurs en-têtes (par exemple une copie Large Address Aware) partagent l'identité de code. |
| Disposition connue | `Omsi23004` : OMSI 2.3.004 (version de fichier 2.2.032), identité de code `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Toutes les validations à l'exécution de la [page d'état](../status/runtime-validation-status.md) ont été effectuées sur cette disposition. |
| Cible native | Une hypothèse de la frontière native, déclarée dans le catalogue du build avec sa garde. |
| Capacité | Un groupe de cibles, par exemple `world.new-map` ou `time.read`. Disponible uniquement lorsque chaque cible est résolue **et** vérifiée. |

Comment une cible est résolue, dans le plugin, avant qu'OmsiLaunch ne patche quoi que ce soit :

1. **Identité connue :** l'adresse de référence de la disposition connue est essayée en premier, et elle doit encore passer la garde de la cible.
2. **Sinon, ou lorsque la garde échoue**, des stratégies indépendantes du build sont essayées :
   - nom de classe RTTI Delphi (`vmtSelfPtr` vérifié) ;
   - méthodes, champs et propriétés publiés d'une classe (tables RTTI) ;
   - slot de VMT contenant une méthode résolue ;
   - une signature de code masquée qui doit correspondre exactement une fois (les opérandes qui dépendent de la disposition sont des jokers), capturant une adresse, une globale (directement ou via sa cellule de pointeur) ou un déplacement de champ à l'intérieur d'une méthode de la classe propre au champ.
3. **Rien ne correspond de façon unique et ne passe la garde :** la cible reste non résolue. Il n'y a **aucun repli sur une adresse fixe**.

Gardes : octets d'entrée masqués pour les fonctions et les sites de patch, destination de l'appel pour un appel patché, `vmtSelfPtr` et nom de classe RTTI pour les classes, méthode contenue pour les slots de VMT, et section de données accessible en écriture pour les globales. Les dispositions d'objets lues par les lecteurs runtime (véhicule, humain, météo, enregistrements d'horaires, etc.) n'ont pas encore de validation structurelle indépendante du build. Elles ne sont vérifiées que pour une identité de code connue, de sorte que les capacités qui les lisent sont indisponibles sur un build d'identité inconnue.

Sur la disposition connue, chaque capacité est résolue. Sur un build d'identité inconnue, les capacités dont les cibles se résolvent par les stratégies ci-dessus (le démarrage headless, NEW_MAP, les situations enregistrées, la suppression des textures Internet, la lecture et l'écriture de l'heure, D3D) peuvent être disponibles, et chacune est vérifiée individuellement ; les lectures dépendantes de la disposition sont indisponibles.

<a id="where-compatibility-is-decided"></a>
## Où la compatibilité est décidée

| Étape | Ce qui se passe |
| --- | --- |
| Planification (`PlanSessionAsync`, `/plan`) | Vérification d'OMSI 2 (`omsi.omsi2`, sinon `OL_E_UNSUPPORTED_BUILD`). L'image du fichier est résolue à titre de **prédiction** : les capacités natives requises portent `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION` avec l'avertissement `OL_W_CAPABILITY_PENDING`. Une identité de code inconnue ajoute l'avertissement `OL_W_BUILD_UNRECOGNIZED`. Les avertissements ne rendent pas un plan non exécutable. |
| Démarrage (`StartSessionAsync`) | La spec est replanifiée ; un plan qui n'est plus exécutable est rejeté avec `OL_E_PLAN_NOT_RUNNABLE`. |
| Dans le processus (démarrage du plugin) | L'image chargée est résolue (fait foi ; un exécutable protégé par DRM peut différer de son fichier). Le rapport est envoyé en télémétrie `native.capabilities`. Si `boot.headless-start` et la capacité de mode monde (`world.new-map` ou `world.saved-situation`) ne sont pas disponibles, la session échoue avec `OL_E_CAPABILITY_UNRESOLVED` avant tout hook, et les fichiers sont restaurés. Si elles sont disponibles mais que le formulaire Start actif n'est pas dans l'état attendu (par exemple parce qu'un autre plugin l'a patché), la session échoue avec `OL_E_BUILD_VALIDATION_FAILED`. |
| Opérations runtime | Une opération dont la capacité est indisponible est rejetée avec `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) sans toucher à la mémoire d'OMSI. |

`OmsiLaunch.exe profiles --json` rapporte l'installation (`is_omsi2`, `build_id`, `identity`, hashes du fichier et du code, `file_variant`), les dispositions connues et les capacités prédites à partir du fichier.

<a id="operating-system-and-architecture"></a>
## Système d'exploitation et architecture

`CurrentWindowsX64Platform.Detect` calcule `RuntimePlatformInfo`. La plateforme courante n'est prise en charge que si toutes les conditions suivantes sont remplies :

| Exigence | Vérification | Erreur en cas de non-respect |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 ou ultérieur | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows 64 bits et processus hôte 64 bits | `OSArchitecture == X64` et `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Installation accessible en écriture | Le répertoire racine existe, n'est pas en lecture seule et contient `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` indique également `OmsiArchitecture` et `PluginArchitecture` comme `X86` (OMSI est un processus 32 bits ; la closure du plugin est x86 et s'exécute sous WOW64), `LegacyPlatform=false`, et `Wow64Available`. Windows ARM64 n'est pas pris en charge, même là où une émulation x64 existe, car le processus hôte doit lui-même être x64.

<a id="net-requirements"></a>
## Exigences .NET

| Composant | Runtime | Remarques |
| --- | --- | --- |
| Contrôleur (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Le bootstrapper natif localise le runtime par `hostfxr` via le `nethost.dll` fourni dans le paquet. Un runtime absent est signalé par le shim (codes de sortie 100-106 ; voir [CLI](cli.md) et [codes de sortie](exit-codes.md)). |
| Closure du plugin (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hébergé par DNNE 2.0.6 dans `Omsi.exe` | Nécessite que le runtime .NET 6 Desktop/Core x86 soit installé sur la machine ; le runtime 64 bits seul ne suffit pas pour le plugin. |
| Pont natif (`plugins\OmsiLaunch.Native.x86.dll`) | x86 natif | Chargé uniquement depuis `plugins\` (voir [plugin permanent](../concepts/permanent-plugin.md)). Il ne contient aucune adresse OMSI : il reçoit du plugin la table des cibles vérifiées. |

<a id="legacy-platforms"></a>
## Plateformes historiques

Windows 7, Windows 8.x, Windows XP et les autres systèmes NT 6 et antérieurs sont en dehors du périmètre de prise en charge actuel. `RuntimePlatformInfo.LegacyPlatform` vaut toujours `false` et il n'existe aucun adaptateur historique ; le champ et l'interface de séparation `IPluginNativeServices` n'existent que pour qu'un futur adaptateur historique puisse être ajouté sans modifier l'API publique (voir `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Rien dans cette version ne s'exécute sur ces systèmes.

<a id="steam-and-large-address-aware-notes"></a>
## Remarques sur Steam et Large Address Aware

- Une copie Large Address Aware qui ne diffère que par ses en-têtes a la même identité de code que son original et utilise la même disposition connue.
- L'exécutable Steam est protégé par DRM sur le disque, de sorte que son fichier peut différer de son image chargée : le plan peut signaler `OL_W_CAPABILITY_PENDING`, et la résolution dans le processus tranche. Une session avec l'exécutable Steam authentique n'a pas été validée à l'exécution (voir l'[état de la validation à l'exécution](../status/runtime-validation-status.md)).
- Steam lance OMSI lui-même ; une session doit être démarrée via `OmsiLaunch.exe` pour que le handoff existe. Démarré depuis Steam, le plugin permanent reste inerte (pas de handoff, pas de hooks).

<a id="related-pages"></a>
## Pages connexes

- [Limitations connues](known-limitations.md)
- [Statut de validation à l'exécution](../status/runtime-validation-status.md)
- [Installation](../getting-started/installation.md)
