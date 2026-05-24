# WarlineCapture Handoff

Lane: Gameplay

Task: Step 14 - move placement grid/input/preview/commit context wiring out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Placement lifecycle, input, validation, and commit context construction now belongs to `BuildingPlacementContextSystem`.
- `BuildingPlacementSystem` no longer directly constructs `BuildingPlacementLifecycleSystem` cancel/begin/confirm contexts, `BuildingPlacementInputSystem.ActivePlacementPointerContext`, `BuildingPlacementValidationSystem.WallValidationContext`, or `BuildingPlacementCommitSystem` request/context objects.
- Placement algorithms remain in their existing narrow systems: grid, input, preview, validation, lifecycle, session, and commit.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2071 lines and <= 125 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Active placement pointer handling, wall validation, placement preview, and placement commit still use the same underlying systems and delegates.

Validation run:
- `git diff --check`
- Copied focused step 14 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- First Unity run found delegate type mismatches in `BuildingPlacementContextSystem`; fixed by matching existing validation and commit delegate types exactly.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step44-placement-context-architecture-rerun.log -testResults /private/tmp/warline-step44-placement-context-architecture-rerun.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode rerun exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity emitted existing unrelated obsolete API warnings from editor scene-builder scripts.
- Unity did not write `/private/tmp/warline-step44-placement-context-architecture-rerun.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2071 lines.
- It still exposes temporary compatibility methods and a placement context source bundle.
- UI command/query compatibility wrappers, selection/interaction wrappers, combat/resource wrappers, remaining context source bundles, and test-only validation hooks still remain.
- Unrelated dirty file present before and after this step: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`.

Cross-lane impacts:
- Architecture docs and tests now enforce the placement context construction boundary.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 15: move UI command/query compatibility wrappers out of `BuildingPlacementSystem`, likely through a narrow UI building facade/adapter boundary consumed by `BuildingGameplayCompositionSystem`.
