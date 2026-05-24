# WarlineCapture Handoff

Lane: Gameplay

Task: Step 12 - move active placement session command flow out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingPlacementSessionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementSessionSystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Active placement mutable state, active placement cost, and preview handoff remain owned by `BuildingPlacementLifecycleSystem`.
- Active placement begin/cancel/confirm/exit command flow and selection-preservation state now belong to `BuildingPlacementSessionSystem`.
- `BuildingPlacementSystem` no longer owns `_preserveBuildingSelectionOnNextExitBuildMode` and no longer calls lifecycle begin/confirm/cancel/notify/set-cost commands directly.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2139 lines and <= 125 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Build placement confirm still records the built-building stat, refreshes static minimap state, preserves building selection for the confirm exit path, and exits build mode.
- Cancel and exit build mode still reset placement input, cancel active preview state, hide the placement outline, and clear command mode.

Validation run:
- `git diff --check`
- Copied focused step 12 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step42-placement-session-architecture.log -testResults /private/tmp/warline-step42-placement-session-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity emitted existing unrelated obsolete API warnings from editor scene-builder scripts.
- Unity did not write `/private/tmp/warline-step42-placement-session-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2139 lines.
- The facade still assembles placement session contexts and keeps compatibility methods for external UI callers.
- Runtime/manual building and wall spawn wrappers, placement grid/input/preview/commit context wiring, UI command/query compatibility wrappers, selection/interaction wrappers, combat/resource wrappers, and test-only validation hooks still remain.

Cross-lane impacts:
- Architecture docs and tests now enforce the placement session command boundary.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 13: move runtime/manual building and wall spawn wrapper methods out of `BuildingPlacementSystem`, using the existing `BuildingRuntimeSpawnSystem` and runtime context boundary directly.
