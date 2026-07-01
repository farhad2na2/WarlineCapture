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

- `Game.Runtime` assembly -> `namespace Game.Runtime;`
- `Game.Components` assembly -> `namespace Game.Components;`
- `Game.UI.Runtime` assembly -> `namespace Game.UI.Runtime;`
- `Game.UI.Shell.Ecs` assembly -> `namespace Game.UI.Shell.Ecs;`
- `Game.Editor` assembly -> `namespace Game.Editor;`

Use file-scoped namespaces by default:

```csharp
namespace Game.Runtime;
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
   - Convert existing block/file-scoped namespaces to the target file-scoped namespace.
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
| Checklist progress | 6 / 26 complete |
| Checklist percent complete | 23.1% |
| Source file namespace progress | 4 / 797 files have an explicit namespace |
| Source file percent complete | 0.5% |
| Baseline measured | Complete |
| Target namespace map approved | Pending |
| Inventory tool implemented | Pending |
| Dry-run wrapper tool implemented | Pending |
| asmdef `rootNamespace` values set | Complete |
| Existing `Game.Scripts.UI` namespace drift fixed | Complete |
| Contract assemblies migrated | Pending |
| Components/config assemblies migrated | Pending |
| Authoring/rendering/UI edge assemblies migrated | Pending |
| `Game.Runtime` migrated | Pending |
| `Game.Composition` migrated | Pending |
| `Game.Editor` migrated | Pending |
| Guardrail validation added | Pending |
| Final Unity compile passed | Pending |

## Checklist

- [x] Count `Assets/Game/Scripts` C# files.
- [x] Identify all game asmdefs.
- [x] Identify existing namespace drift.
- [x] Define assembly-name namespace policy.
- [x] Fix existing `Game.Scripts.UI` namespace drift to `Game.UI.Runtime`.
- [x] Set `rootNamespace` in all game asmdefs.
- [ ] Confirm this tracker is approved as the namespace migration source of truth.
- [ ] Generate namespace inventory.
- [ ] Add deterministic dry-run namespace migration tool.
- [ ] Migrate `Game.Catalog.Contracts`.
- [ ] Migrate `Game.Tactical.Contracts`.
- [ ] Migrate `Game.Rendering.Contracts`.
- [ ] Migrate `Game.UI.Contracts`.
- [ ] Migrate `Game.UI.Shell.Contracts.Ecs`.
- [ ] Migrate `Game.Components`.
- [ ] Migrate `Game.Configs`.
- [ ] Migrate `Game.Authoring`.
- [ ] Migrate `Game.Rendering`.
- [ ] Migrate `Game.UI.Runtime`.
- [ ] Migrate `Game.UI.Shell.Ecs`.
- [ ] Migrate `Game.Runtime` root and root-owned folders.
- [ ] Migrate `Game.Composition`.
- [ ] Migrate `Game.Editor`.
- [ ] Add namespace/rootNamespace architecture guardrail.
- [ ] Run final compile and focused architecture validation.
- [ ] Update this tracker with final results and any known residual issues.

## Drift Fix Validation Log

2026-07-01:

- `Assets/Game/Scripts/UI/CampListItemViewReferences.cs`, `MenuDiagnosticsView.cs`, `RuntimeLogBuffer.cs`, and `MenuDiagnosticsUiSystemHelper.cs` moved from `Game.Scripts.UI` to `Game.UI.Runtime`.
- All game asmdefs now set `rootNamespace` to their assembly namespace.
- `rg -n 'Game\.Scripts\.UI|namespace\s+Game\.Scripts\.UI|using\s+Game\.Scripts\.UI' Assets/Game/Scripts Assets/Tests -g '*.cs'`: only the intentional negative legacy scene assertion remains in `ThreatWarningValidationTests`.
- `rg -n '"rootNamespace"\s*:\s*""' Assets/Game/Scripts -g '*.asmdef'`: no empty game asmdef root namespaces remain.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passed, 0 warnings, 0 errors.

## Still Wrong / Next Iteration

Known wrong:

- Almost all `Assets/Game/Scripts` files are currently in the global namespace.
- Almost all scripts still need their first namespace assignment.

Next iteration:

- Implement the inventory and dry-run migration tooling.
- Review the generated file-to-namespace map before applying the first code batch.
- Start with contract assemblies because they are low dependency and fast to compile.
