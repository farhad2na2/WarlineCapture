# WarlineCapture Handoff

Lane: Gameplay

Task: Step 11 - move runtime resource/unit prefab context wiring out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction now belongs to `BuildingRuntimeResourcePrefabContextSystem`.
- `BuildingPlacementSystem` no longer exposes direct `RuntimeResourceSystem` / `RuntimeUnitPrefabSystem` properties to managed composition.
- `BuildingPlacementSystem` no longer constructs `RuntimeUnitPrefabSystem.Context` or `BuildingSpawnPrefabSystem.Context` directly.
- Temporary facade surface is limited to `CreateRuntimeResourcePrefabContextSource()` while the facade is retired.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2148 lines and <= 126 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Citizen upkeep, citizen prefab resolution, unit prefab lookup, and spawn-prefab registry lookup still route through the same underlying runtime resource, runtime unit prefab, definition, and spawn-prefab systems.

Validation run:
- `git diff --check`
- Copied focused step 11 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step41-runtime-resource-prefab-architecture.log -testResults /private/tmp/warline-step41-runtime-resource-prefab-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity emitted existing unrelated obsolete API warnings from editor scene-builder scripts.
- Unity did not write `/private/tmp/warline-step41-runtime-resource-prefab-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2148 lines.
- It still exposes source bundles and wrappers for active placement/session flow, runtime/manual spawn, placement grid/input/preview/commit wiring, UI command/query compatibility, selection, interaction, combat, and remaining lifecycle callbacks.
- `BuildingGameplayCompositionSystem` still constructs the temporary facade.

Cross-lane impacts:
- Architecture tests and docs now enforce the new resource/prefab context boundary.
- No art, scene, or UI prefab behavior was intentionally changed.

Next recommended task:
- Step 12: move active placement lifecycle/session command wrappers out of `BuildingPlacementSystem` so active placement mutable state no longer has to be exposed through the facade.
