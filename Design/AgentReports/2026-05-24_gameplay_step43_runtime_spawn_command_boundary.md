# WarlineCapture Handoff

Lane: Gameplay

Task: Step 13 - move runtime/manual building and wall spawn command translation out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCommandSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCommandSystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Runtime/manual spawn orchestration remains owned by `BuildingRuntimeSpawnSystem`.
- Runtime/manual spawn command translation now belongs to `BuildingRuntimeSpawnCommandSystem`.
- `BuildingPlacementSystem` no longer calls the runtime/manual spawn orchestration methods directly; it only delegates through `BuildingRuntimeSpawnCommandSystem` while legacy callers still use the facade.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2103 lines and <= 125 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Legacy runtime/manual spawn calls still support initial roster spawn, runtime building spawn, wall-run spawn, wall-segment spawn, and runtime placement footprint queries.

Validation run:
- `git diff --check`
- Copied focused step 13 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step43-runtime-spawn-command-architecture.log -testResults /private/tmp/warline-step43-runtime-spawn-command-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity emitted existing unrelated obsolete API warnings from editor scene-builder scripts.
- Unity did not write `/private/tmp/warline-step43-runtime-spawn-command-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2103 lines.
- Runtime/manual spawn compatibility methods remain on the facade for legacy editor tests and callers.
- Placement grid/input/preview/commit context wiring, UI command/query compatibility wrappers, selection/interaction wrappers, combat/resource wrappers, and test-only validation hooks still remain.

Cross-lane impacts:
- Architecture docs and tests now enforce the runtime/manual spawn command boundary.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 14: move placement grid/input/preview/commit context wiring out of `BuildingPlacementSystem` so the facade no longer assembles placement interaction contexts directly.
