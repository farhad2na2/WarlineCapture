# Game Scripts Namespace Migration Tracker

## Goal

Add explicit C# namespaces to every script under `Assets/Game/Scripts` according to the owning Unity assembly definition.

This is a mechanical architecture cleanup. The migration should be fast, low-risk, and compile-gated by assembly batches. Do not combine it with gameplay fixes, UI changes, file renames, or folder reshuffles.

## Current Baseline

Measured on 2026-07-01.

| Metric | Count |
| --- | ---: |
| C# files under `Assets/Game/Scripts` | 797 |
| Files currently declaring a namespace | 4 |
| Files currently declaring `Game.Scripts.UI` | 4 |
| Assembly definitions under `Assets/Game/Scripts` | 14 |
| `rootNamespace` values currently set in game asmdefs after drift fix | 14 |

Existing namespace drift:

- `Assets/Game/Scripts/UI/CampListItemViewReferences.cs`
- `Assets/Game/Scripts/UI/MenuDiagnosticsView.cs`
- `Assets/Game/Scripts/UI/RuntimeLogBuffer.cs`
- `Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs`

Those four files used `Game.Scripts.UI`, which did not match the assembly naming convention. This drift was fixed on 2026-07-01 by moving them to `Game.UI.Runtime`.

## Target Namespace Rule

Use the asmdef name as the base namespace for all files owned by that assembly.

Examples:

- `Game.Runtime` assembly -> `namespace Game.Runtime { ... }`
- `Game.Components` assembly -> `namespace Game.Components { ... }`
- `Game.UI.Runtime` assembly -> `namespace Game.UI.Runtime { ... }`
- `Game.UI.Shell.Ecs` assembly -> `namespace Game.UI.Shell.Ecs { ... }`
- `Game.Editor` assembly -> `namespace Game.Editor { ... }`

Use block-scoped namespaces by default because the Unity editor currently compiles this project with C# 9. File-scoped namespaces require C# 10 and caused Unity compiler errors during validation.

```csharp
namespace Game.Runtime
{
}
```

Do not introduce domain subnamespaces during the first migration. Keep it assembly-level only so the change can be automated quickly and validated cheaply. Subnamespaces can be introduced later only if there is a concrete ownership reason.

## Assembly Namespace Map

| Assembly | Path root | Target namespace | Priority |
| --- | --- | --- | --- |
| `Game.Catalog.Contracts` | `Assets/Game/Scripts/Catalog/Contracts` | `Game.Catalog.Contracts` | 1 |
| `Game.Tactical.Contracts` | `Assets/Game/Scripts/Contracts` | `Game.Tactical.Contracts` | 1 |
| `Game.Rendering.Contracts` | `Assets/Game/Scripts/Rendering/Contracts` | `Game.Rendering.Contracts` | 1 |
| `Game.UI.Contracts` | `Assets/Game/Scripts/UI/Contracts` | `Game.UI.Contracts` | 1 |
| `Game.UI.Shell.Contracts.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Contracts` | `Game.UI.Shell.Contracts.Ecs` | 1 |
| `Game.Components` | `Assets/Game/Scripts/Components` | `Game.Components` | 2 |
| `Game.Configs` | `Assets/Game/Scripts/Configs` | `Game.Configs` | 2 |
| `Game.Authoring` | `Assets/Game/Scripts/Authorings` | `Game.Authoring` | 3 |
| `Game.Rendering` | `Assets/Game/Scripts/Rendering` | `Game.Rendering` | 3 |
| `Game.UI.Runtime` | `Assets/Game/Scripts/UI` | `Game.UI.Runtime` | 3 |
| `Game.UI.Shell.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs` | `Game.UI.Shell.Ecs` | 3 |
| `Game.Runtime` | `Assets/Game/Scripts` except nested asmdef folders and `Editor` | `Game.Runtime` | 4 |
| `Game.Composition` | `Assets/Game/Scripts/Composition` | `Game.Composition` | 5 |
| `Game.Editor` | `Assets/Game/Scripts/Editor` | `Game.Editor` | 6 |

Notes:

- `Game.Runtime` owns root-level and non-nested-asmdef folders such as `Balance`, `Effects`, `Environment`, `Persistence`, `RuntimeState`, `ScenarioLab`, `Systems`, `TacticalMaps`, `Tools`, and `Utilities`.
- Nested asmdef folders must be excluded from parent assembly batches.
- Editor scripts stay in `Game.Editor`; do not move editor tools into runtime namespaces.

## Fast Execution Strategy

Use medium reasoning for the initial map/guardrail pass, then low reasoning for mechanical batches. The work should be run as a script-driven migration, not by hand-editing 797 files.

1. Build a deterministic namespace inventory tool.
   - Input: `Assets/Game/Scripts/**/*.cs` plus asmdef folder ownership.
   - Output: file path, current namespace, target namespace, owning asmdef, and whether the file is generated or excluded.
   - Exclude generated package/vendor code only if it is outside `Assets/Game/Scripts`; current scope is all first-party game scripts.

2. Add a dry-run namespace wrapper tool.
   - Prefer Roslyn or a simple syntax-safe C# rewriter.
   - Preserve file header comments, `#if`, `#nullable`, `using` directives, attributes, and `.meta` files.
   - Convert existing block/file-scoped namespaces to the target block-scoped namespace.
   - Do not change type names, filenames, assembly references, serialized fields, GUIDs, or folder structure.

3. Set asmdef `rootNamespace` values.
   - Set each asmdef `rootNamespace` to the target namespace.
   - This helps Unity-created scripts default to the right namespace after migration.
   - Preserve existing asmdef GUID/meta files.

4. Migrate leaf contracts first.
   - Contracts compile fastest and expose dependency issues early.
   - Validate with `dotnet build` after the batch.

5. Migrate data/config assemblies.
   - `Game.Components`, then `Game.Configs`.
   - Validate before touching runtime systems.

6. Migrate edge runtime assemblies.
   - `Game.Authoring`, `Game.Rendering`, `Game.UI.Runtime`, `Game.UI.Shell.Ecs`.
   - Fix missing `using` directives mechanically from compiler errors only.

7. Migrate `Game.Runtime`.
   - This is the largest and highest-risk batch.
   - Prefer sub-batches by folder: `Systems`, `Environment`, `ScenarioLab`, remaining root folders.
   - Compile after each sub-batch if error count is high.

8. Migrate `Game.Composition`.
   - Composition depends on most other assemblies, so keep it near the end.

9. Migrate `Game.Editor`.
   - Editor references everything and catches missed public API namespace updates.

10. Add guardrails.
    - Add or update an editor architecture validation that fails when a first-party file under `Assets/Game/Scripts` has no namespace or has a namespace not matching its owning asmdef.
    - Add a check that game asmdefs have non-empty `rootNamespace`.

## Risk Controls

- One assembly batch at a time.
- No behavior changes.
- No public API renames.
- No file moves.
- No gameplay/Canvas/prefab edits.
- No namespace subfolders in the first pass.
- Fix compiler errors by adding/changing `using` directives, not by broad refactors.
- Run `git diff --check` after every batch.
- Run Unity compile after the final batch, and earlier if `dotnet build` disagrees with Unity assembly compilation.

## Validation Gates

Minimum validation per batch:

- `dotnet build Assembly-CSharp.csproj --no-restore`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`
- `git diff --check`

Final validation:

- Unity batchmode compile in `/Users/farhad/Projects/WarlineCapture`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`
- New namespace/rootNamespace guardrail validation.
- One focused gameplay smoke validation if compiler errors touched runtime using directives in ECS systems.

## Progress Snapshot

| Item | Status |
| --- | --- |
| Checklist progress | 26 / 26 complete |
| Checklist percent complete | 100.0% |
| Source file namespace progress | 797 / 797 files match target namespace |
| Source file percent complete | 100.0% |
| Current assembly batch | `Game.Editor` complete |
| Validation status | Inventory regenerated; Unity C# 9 file-scoped namespace compiler errors fixed by converting all migrated scripts to block-scoped namespaces; namespace tool now emits block-scoped namespaces and treats file-scoped namespace declarations as drift; removed stale cross-assembly `using` directives reported by Unity; fixed Unity-only alias fallout by moving unqualified project-type `using` aliases inside namespace blocks; repaired Unity GUI-only editor/test namespace fallout after opening the Editor directly; `git diff --check` passed; runtime/editor dotnet builds passed with 0 warnings and 0 errors; namespace/rootNamespace guardrail added and compile-gated; Unity GUI compile has 0 asset C# errors; focused Unity batchmode architecture validation passed with `[ScriptArchitectureBoundaryValidation] result=Passed tests=30` |
| Compiler error status | Current Unity GUI check 2026-07-02: fresh Unity Editor run `/private/tmp/warline-unity-gui-namespace-heartbeat.log` has 0 asset C# compiler errors; `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` build with 0 warnings and 0 errors; `git diff --check` passes |
| Baseline measured | Complete |
| Target namespace map approved | Complete |
| Inventory tool implemented | Complete |
| Dry-run wrapper tool implemented | Complete |
| asmdef `rootNamespace` values set | Complete |
| Existing `Game.Scripts.UI` namespace drift fixed | Complete |
| Contract assemblies migrated | Complete: all contract assemblies migrated |
| Components/config assemblies migrated | Complete: `Game.Components` and `Game.Configs` migrated |
| Authoring/rendering/UI edge assemblies migrated | Complete: `Game.Authoring`, `Game.Rendering`, `Game.UI.Runtime`, and `Game.UI.Shell.Ecs` migrated |
| `Game.Runtime` migrated | Complete |
| `Game.Composition` migrated | Complete |
| `Game.Editor` migrated | Complete |
| Guardrail validation added | Complete |
| Final Unity compile passed | Complete: direct Unity Editor GUI compile pass has 0 asset C# compiler errors |
| Final architecture validation passed | Complete: `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed |

## Checklist

- [x] Count `Assets/Game/Scripts` C# files.
- [x] Identify all game asmdefs.
- [x] Identify existing namespace drift.
- [x] Define assembly-name namespace policy.
- [x] Fix existing `Game.Scripts.UI` namespace drift to `Game.UI.Runtime`.
- [x] Set `rootNamespace` in all game asmdefs.
- [x] Confirm this tracker is approved as the namespace migration source of truth.
- [x] Generate namespace inventory.
- [x] Add deterministic dry-run namespace migration tool.
- [x] Migrate `Game.Catalog.Contracts`.
- [x] Migrate `Game.Tactical.Contracts`.
- [x] Migrate `Game.Rendering.Contracts`.
- [x] Migrate `Game.UI.Contracts`.
- [x] Migrate `Game.UI.Shell.Contracts.Ecs`.
- [x] Migrate `Game.Components`.
- [x] Migrate `Game.Configs`.
- [x] Migrate `Game.Authoring`.
- [x] Migrate `Game.Rendering`.
- [x] Migrate `Game.UI.Runtime`.
- [x] Migrate `Game.UI.Shell.Ecs`.
- [x] Migrate `Game.Runtime` root and root-owned folders.
- [x] Migrate `Game.Composition`.
- [x] Migrate `Game.Editor`.
- [x] Add namespace/rootNamespace architecture guardrail.
- [x] Run final compile and focused architecture validation.
- [x] Update this tracker with final results and any known residual issues.

## Drift Fix Validation Log

2026-07-01:

- `Assets/Game/Scripts/UI/CampListItemViewReferences.cs`, `MenuDiagnosticsView.cs`, `RuntimeLogBuffer.cs`, and `MenuDiagnosticsUiSystemHelper.cs` moved from `Game.Scripts.UI` to `Game.UI.Runtime`.
- All game asmdefs now set `rootNamespace` to their assembly namespace.
- `rg -n 'Game\.Scripts\.UI|namespace\s+Game\.Scripts\.UI|using\s+Game\.Scripts\.UI' Assets/Game/Scripts Assets/Tests -g '*.cs'`: only the intentional negative legacy scene assertion remains in `ThreatWarningValidationTests`.
- `rg -n '"rootNamespace"\s*:\s*""' Assets/Game/Scripts -g '*.asmdef'`: no empty game asmdef root namespaces remain.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Unity editor then reported two remaining cross-assembly import errors:
  - `Assets/Game/Scripts/Components/RuntimeCameraReferenceComponent.cs` imported `Game.Rendering` from `Game.Components`.
  - `Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs` imported `Game.UI.Shell.Ecs` from `Game.UI.Runtime`.
- Removed both unused imports instead of adding assembly references, preserving the intended assembly boundaries.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Unity editor then reported 128 namespace fallout errors. The root cause was unqualified project-type `using` aliases left before namespace declarations, for example `using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;`. Once the files were wrapped in `namespace Game.Runtime { ... }`, those aliases had to live inside the namespace block to resolve sibling types.
- Moved 151 unqualified project-type aliases inside their namespace blocks across 50 files.
- Removed the unused `using Game.UI.Runtime;` from `ManagedGameplayStartupSystemHelper` instead of adding a `Game.Runtime` -> `Game.UI.Runtime` assembly reference.
- Updated `Tools/Architecture/apply_game_scripts_namespace.py` so future namespace insertion keeps only namespace-qualified aliases outside and leaves unqualified project-type aliases in the namespaced body.
- Alias guard check: `unqualified_aliases_before_namespace=0`.
- `python3 Tools/Architecture/apply_game_scripts_namespace.py --report`: `files=797 changed=0`.
- `git diff --check`: passed.
- Unity editor then reported one remaining stale assembly import: `SelectionGameplayStartupSystemHelper` imported `Game.UI.Runtime` from `Game.Runtime`.
- Removed the unused import, preserving the runtime -> UI contracts dependency boundary.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Unity editor then reported five explicit `global::` references to now-namespaced first-party runtime helpers.
- Removed the stale `global::` qualifiers in `RuntimeCityCompositionSystemHelper` and `RoadBuildCompositionSourceCompositionSystemHelper`.
- First-party `global::` scan for runtime/helper type prefixes: no hits remain.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Unity editor then reported a 147-error composition assembly burst where `Game.Composition` could not resolve first-party runtime types after namespace migration.
- Added `using Game.Runtime;` to all `Game.Composition` scripts. `Game.Composition.asmdef` already references `Game.Runtime`; this fixes the source namespace import without changing assembly dependencies.
- Static validation of the Unity error set found 38 unique missing types, all declared under `Game.Runtime`, and all affected `Game.Composition` source files now import `Game.Runtime`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Heartbeat automation `game-scripts-namespace-migration` updated and reactivated to explicitly keep reading Unity compiler errors, grouping the latest compiler burst, compiling, and fixing until no compiler errors remain or a real blocker is documented.
- Unity editor then reported a 51-error burst in editor tooling and play-mode tests. The missing types were all now-namespaced `Game.Runtime` or `Game.Composition` types.
- Added `using Game.Runtime;` and `using Game.Composition;` to affected editor scripts, and `using Game.Runtime;` to affected play-mode tests.
- Static validation of the 51-error set checked 26 unique file/type pairs and found `unresolved_after_import_patch=0`.
- Latest Unity editor compiler stderr block after the fix reported 0 compiler errors.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Namespace Inventory Log

2026-07-01:

- Added `Tools/Architecture/generate_game_scripts_namespace_inventory.py`.
- Generated `Design/Architecture/game_scripts_namespace_inventory.md`.

2026-07-02:

- Unity editor log reported C# 9 compiler errors for migrated file-scoped namespaces (`CS8773: Feature 'file-scoped namespace' is not available in C# 9.0`).
- Converted all migrated `Assets/Game/Scripts` namespaces to block-scoped declarations.
- Updated `Tools/Architecture/apply_game_scripts_namespace.py` so future migration runs emit block-scoped namespaces and convert file-scoped declarations back to block-scoped form.
- `python3 Tools/Architecture/apply_game_scripts_namespace.py --report`: `files=797 changed=0`.
- `rg -n '^namespace [A-Za-z_][A-Za-z0-9_.]*;' Assets/Game/Scripts`: no remaining file-scoped namespaces.
- `git diff --check`: passed after whitespace cleanup.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Current generated source namespace progress is 4 / 797 files, 0.5%.
- Current generated asmdef root namespace mismatch count is 0.
- Current completed assembly count is 0 / 14; `Game.UI.Runtime` is partially complete at 4 / 101 files.
- `python3 Tools/Architecture/generate_game_scripts_namespace_inventory.py`: passed.
- `PYTHONPYCACHEPREFIX=/private/tmp/warline-pycache python3 -m py_compile Tools/Architecture/generate_game_scripts_namespace_inventory.py`: passed.
- `git diff --check`: passed.
- Plain `python3 -m py_compile ...` attempted to write under `/Users/farhad/Library/Caches/com.apple.python/...` and was blocked by sandbox permissions; redirected pycache validation is the accepted result for this slice.

## Namespace Wrapper Tooling Log

2026-07-01:

- Added `Tools/Architecture/apply_game_scripts_namespace.py`.
- Tool supports `--assembly`, dry-run by default, `--report`, `--diff`, and explicit `--apply`.
- The tool calculates assembly ownership from longest matching asmdef folder, preserving nested assemblies such as UI contracts and UI shell ECS contracts.
- The tool inserts file-scoped namespaces after top comments/usings/assembly attributes and only rewrites existing namespace names when a namespace already exists.
- `PYTHONPYCACHEPREFIX=/private/tmp/warline-pycache python3 -m py_compile Tools/Architecture/apply_game_scripts_namespace.py Tools/Architecture/generate_game_scripts_namespace_inventory.py`: passed.
- `python3 Tools/Architecture/apply_game_scripts_namespace.py --assembly Game.Catalog.Contracts --report --diff`: dry-run passed; proposed 1 file change.
- `python3 Tools/Architecture/apply_game_scripts_namespace.py --assembly Game.Tactical.Contracts --report --diff`: dry-run passed; proposed 1 file change.
- `python3 Tools/Architecture/apply_game_scripts_namespace.py --assembly Game.Rendering.Contracts --report --diff`: dry-run passed; proposed 2 file changes.
- Reference inspection showed `Game.Catalog.Contracts` has consumers in config, UI shell ECS, and UI screens, so the first apply batch should include obvious `using Game.Catalog.Contracts;` fallout fixes before compile validation.

## Batch 1 - `Game.Catalog.Contracts`

2026-07-01:

- Applied `namespace Game.Catalog.Contracts;` to `Assets/Game/Scripts/Catalog/Contracts/CatalogPrefabSource.cs`.
- Added `using Game.Catalog.Contracts;` to direct `ICatalogPrefabSource` consumers in configs, UI screens, and UI shell ECS read-model code.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 5 / 797 files, 0.6%.
- `Game.Catalog.Contracts` is 1 / 1 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 2 - `Game.Tactical.Contracts`

2026-07-02:

- Applied `namespace Game.Tactical.Contracts;` to `Assets/Game/Scripts/Contracts/TacticalCommandContracts.cs`.
- Added `using Game.Tactical.Contracts;` to 69 direct consumers across runtime command systems, UI contracts/views/helpers, editor validations, and tests.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 6 / 797 files, 0.8%.
- `Game.Tactical.Contracts` is 1 / 1 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 3 - `Game.Rendering.Contracts`

2026-07-02:

- Applied `namespace Game.Rendering.Contracts;` to `Assets/Game/Scripts/Rendering/Contracts/GameplayRendererContracts.cs` and `Assets/Game/Scripts/Rendering/Contracts/UnitImpostorVisualUtility.cs`.
- Added `using Game.Rendering.Contracts;` to the direct rendering contract consumers in rendering systems/helpers, match bootstrap/runtime composition helpers, and `UnitRenderBudgetSystemTests`.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 8 / 797 files, 1.0%.
- `Game.Rendering.Contracts` is 2 / 2 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 4 - `Game.UI.Contracts`

2026-07-02:

- Applied `namespace Game.UI.Contracts;` to all 18 files under `Assets/Game/Scripts/UI/Contracts`.
- Added `using Game.UI.Contracts;` to 100 direct consumers across Canvas UI views/helpers, shell ECS adapters, runtime composition helpers, editor validations, and tests.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 26 / 797 files, 3.3%.
- `Game.UI.Contracts` is 18 / 18 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 5 - `Game.UI.Shell.Contracts.Ecs`

2026-07-02:

- Applied `namespace Game.UI.Shell.Contracts.Ecs;` to `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`.
- Added `using Game.UI.Shell.Contracts.Ecs;` to 17 direct consumers across UI shell ECS systems, composition helpers, editor validations, and tests.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 27 / 797 files, 3.4%.
- `Game.UI.Shell.Contracts.Ecs` is 1 / 1 complete, 100.0%.
- Contract assembly migration phase is complete.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 6 - `Game.Components`

2026-07-02:

- Applied `namespace Game.Components;` to all 47 files under `Assets/Game/Scripts/Components`.
- Added `using Game.Components;` to 462 direct consumers across runtime systems, authoring, configs, rendering, UI, composition helpers, scenario lab runners, editor validations, and tests.
- Trimmed two wrapper-introduced blank EOF whitespace issues in `AssemblyInfo.cs` and `CombatComponents.cs`.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 74 / 797 files, 9.3%.
- `Game.Components` is 47 / 47 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 7 - `Game.Configs`

2026-07-02:

- Applied `namespace Game.Configs;` to all 37 files under `Assets/Game/Scripts/Configs`.
- Added `using Game.Configs;` to 120 direct consumers across authoring, composition helpers, runtime systems, UI/runtime state, rendering, editor validations, balance probes, and tests.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 111 / 797 files, 13.9%.
- `Game.Configs` is 37 / 37 complete, 100.0%.
- Components/config assembly migration phase is complete.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 8 - `Game.Authoring`

2026-07-02:

- Applied `namespace Game.Authoring;` to all 15 files under `Assets/Game/Scripts/Authorings`.
- Added `using Game.Authoring;` to 46 direct consumers across composition helpers, editor tooling/validation, and tests that consume authoring MonoBehaviours and authoring enums.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 126 / 797 files, 15.8%.
- `Game.Authoring` is 15 / 15 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 9 - `Game.Rendering`

2026-07-02:

- Applied `namespace Game.Rendering;` to all 41 non-contract files under `Assets/Game/Scripts/Rendering`.
- Added `using Game.Rendering;` to 15 direct consumers across composition helpers, runtime systems, editor validation, and tests that consume rendering helpers/systems.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 167 / 797 files, 21.0%.
- `Game.Rendering` is 41 / 41 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 10 - `Game.UI.Runtime`

2026-07-02:

- Applied `namespace Game.UI.Runtime;` to the remaining 97 global-namespace files in the 101-file `Game.UI.Runtime` assembly; 4 files were already corrected during the earlier UI namespace drift fix.
- Added `using Game.UI.Runtime;` to 36 direct consumers across UI shell ECS, composition helpers, editor validation/setup code, and tests.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 264 / 797 files, 33.1%.
- `Game.UI.Runtime` is 101 / 101 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 11 - `Game.UI.Shell.Ecs`

2026-07-02:

- Applied `namespace Game.UI.Shell.Ecs;` to all 8 non-contract files under `Assets/Game/Scripts/UI/Shell/Ecs`.
- Added `using Game.UI.Shell.Ecs;` to 5 direct consumers across UI runtime diagnostics and editor/tests.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 272 / 797 files, 34.1%.
- `Game.UI.Shell.Ecs` is 8 / 8 complete, 100.0%.
- Authoring/rendering/UI edge assembly migration phase is complete.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 12 - `Game.Runtime`

2026-07-02:

- Applied `namespace Game.Runtime;` to all 440 root/runtime-owned files under `Assets/Game/Scripts`, excluding nested asmdef folders and `Editor`.
- No external `using Game.Runtime;` fallout fixes were required by the runtime/editor dotnet builds.
- Trimmed two wrapper-introduced blank EOF whitespace issues in `Assets/Game/Scripts/AssemblyInfo.cs` and `Assets/Game/Scripts/RuntimeState/PerformanceDiagnosticsReferenceComponent.cs`.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 712 / 797 files, 89.3%.
- `Game.Runtime` is 440 / 440 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 13 - `Game.Composition`

2026-07-02:

- Applied `namespace Game.Composition;` to all 23 files under `Assets/Game/Scripts/Composition`.
- No external `using Game.Composition;` fallout fixes were required by the runtime/editor dotnet builds.
- Trimmed two wrapper-introduced blank EOF whitespace issues in `Assets/Game/Scripts/Composition/AssemblyInfo.cs` and `Assets/Game/Scripts/Composition/MatchSceneReferenceComponent.cs`.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 735 / 797 files, 92.2%.
- `Game.Composition` is 23 / 23 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Batch 14 - `Game.Editor`

2026-07-02:

- Applied `namespace Game.Editor;` to all 62 files under `Assets/Game/Scripts/Editor`.
- No editor-local missing using fallout fixes were required by the runtime/editor dotnet builds.
- Regenerated `Design/Architecture/game_scripts_namespace_inventory.md`.
- Source namespace progress is now 797 / 797 files, 100.0%.
- `Game.Editor` is 62 / 62 complete, 100.0%.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Guardrail Log

2026-07-02:

- Added `GameScriptAsmdefsMustDeclareMatchingRootNamespace` to `Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs`.
- Added `GameScriptsMustDeclareOwningAssemblyNamespace` to calculate longest-matching asmdef ownership and require each `Assets/Game/Scripts` C# file to declare the owning asmdef `rootNamespace`.
- Updated `RunAssemblyBoundaryValidation` to execute both namespace guardrails.
- `Design/Architecture/game_scripts_namespace_inventory.md` reports 797 / 797 files matching target namespace and 0 asmdef root namespace mismatches.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Unity MCP validation attempt failed before execution because the MCP named pipe was stale: `/tmp/unity-mcp-cf777efb-48936`.
- Unity batchmode validation with `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` was attempted outside the sandbox; it reached assembly reload but remained stuck in repeated licensing client handshake failures (`Unsupported protocol version '1.18.1'`, missing `com.unity.editor.headless`) before the validation method ran, so the process was stopped after several minutes.
- Final Unity batchmode validation passed in `/private/tmp/warline-namespace-final-architecture-validation.log`: `[ScriptArchitectureBoundaryValidation] result=Passed tests=30`; parsed asset C# compiler errors: 0.

## Compiler Error Follow-Up Log

2026-07-02:

- Re-read the latest Unity `Editor.log` compiler stderr block after the user-reported 51-error burst: current parsed compiler stderr errors are 0.
- Re-ran `python3 Tools/Architecture/apply_game_scripts_namespace.py --report`: `assembly=(all) files=797 changed=0 mode=dry-run`.
- Re-ran file-scoped namespace guard: no `namespace ...;` declarations remain under `Assets/Game/Scripts`.
- Re-ran `git diff --check`: passed.
- Re-ran `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Re-ran `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

2026-07-02 heartbeat:

- Re-read the latest Unity `Editor.log` compiler stderr block: current parsed compiler stderr errors remain 0.
- Re-ran `python3 Tools/Architecture/apply_game_scripts_namespace.py --report`: `assembly=(all) files=797 changed=0 mode=dry-run`.
- Re-ran `git diff --check`: passed.

2026-07-02 heartbeat compiler sweep:

- Re-read the latest Unity `Editor.log` compiler stderr block: current parsed compiler stderr errors remain 0.
- Re-ran `git diff --check`: passed.
- Re-ran `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Re-ran `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

2026-07-02 direct Unity GUI compile follow-up:

- Opened the Unity Editor GUI directly through `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -projectPath /Users/farhad/Projects/WarlineCapture`.
- Confirmed the earlier log-only parser was insufficient: the first direct GUI run exposed editor/test asmdef namespace fallout.
- Fixed the misplaced `using Game.Runtime;` inside `Assets/Tests/PlayMode/BuildingPlacementProductionPlayModeTests.cs`.
- Added missing migrated namespace imports across affected editor validation scripts and test assemblies (`Game.Runtime`, `Game.Composition`, `Game.Editor`, and the `SettingsService = Game.UI.Runtime.SettingsService` alias where needed).
- Re-ran direct Unity GUI compile with a fresh log: `/private/tmp/warline-unity-gui-namespace-check-3.log` reports 0 asset C# compiler errors.
- Re-ran `git diff --check`: passed.
- Re-ran `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Re-ran `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

2026-07-02 heartbeat compiler sweep:

- Re-read the latest Unity `Editor.log` compiler stderr block: current parsed compiler stderr errors remain 0.
- Re-ran `git diff --check`: passed.
- Re-ran `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- Re-ran `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Still Wrong / Next Iteration

Known wrong:

- No known namespace migration defects remain.

Next iteration:

- Namespace migration is complete; close the namespace migration automation.
