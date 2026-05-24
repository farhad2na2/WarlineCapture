# WarlineCapture Handoff

Lane: Gameplay

Task: Step 15 - move building UI command/query context construction out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- UI command/query context construction now belongs to `BuildingUiContextSystem`.
- `BuildingPlacementSystem` no longer directly constructs `BuildingUiCommandSystem.Context` or `BuildingUiQuerySystem.Context`.
- `BuildingUiCommandSystem` remains command-only and must not own UI query delegates or query context construction.
- Removed the unused `BuildingPlacementSystem.GetResourceTotals` resource facade wrapper.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2067 lines and <= 124 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Menu/building UI still receives the same command and query contexts through managed composition.

Validation run:
- `git diff --check`
- Copied focused step 15 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step45-ui-context-architecture.log -testResults /private/tmp/warline-step45-ui-context-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity did not write `/private/tmp/warline-step45-ui-context-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2067 lines.
- It still exposes temporary UI compatibility methods and UI context source callbacks.
- Selection/interaction wrappers, combat/resource wrappers, remaining context source bundles, and test-only validation hooks still remain.
- Unrelated dirty file present before and after this step: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`.

Cross-lane impacts:
- Architecture docs and tests now enforce the building UI context construction boundary.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 16: move selection/interaction compatibility wrappers out of `BuildingPlacementSystem`, starting with a narrow interaction context boundary so `BuildingPlacementSystem` stops constructing `BuildingPlacementInteractionSystem.Context` directly.
