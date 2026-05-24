# WarlineCapture Handoff

Lane: Gameplay

Task: Step 16 - move building placement interaction context construction out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextSystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Selection/interaction context construction now belongs to `BuildingPlacementInteractionContextSystem`.
- `BuildingPlacementSystem` no longer directly constructs `BuildingPlacementInteractionSystem.Context`.
- Removed unused facade wrappers for configured spawnable/unit prefab lookup and the unused `DeleteButtonText` facade property.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2051 lines and <= 120 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Road, selection, menu, runtime-link, and feature startup flows still use the same `BuildingPlacementInteractionSystem` delegates through managed composition.

Validation run:
- `git diff --check`
- Copied focused step 16 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step46-interaction-context-architecture.log -testResults /private/tmp/warline-step46-interaction-context-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity did not write `/private/tmp/warline-step46-interaction-context-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2051 lines.
- It still exposes temporary interaction context source callbacks.
- Building query, combat/breach, resource, remaining context source bundles, and test-only validation hooks still remain.
- Unrelated dirty files present after this step: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` and `Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs`.

Cross-lane impacts:
- Architecture docs and tests now enforce the building placement interaction context construction boundary.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 17: extract building selection/query compatibility context construction next, starting with the remaining `CreateBuildingSelectionContext`, `CreateBuildingSelectionClickContext`, and `CreateBuildingPlacementQueryContext` facade factories.
