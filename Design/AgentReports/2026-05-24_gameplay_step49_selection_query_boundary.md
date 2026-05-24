# WarlineCapture Handoff

Lane: Gameplay

Task: Step 19 - extract selection and query facade wrappers.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Selection-click context construction now belongs to `BuildingSelectionClickSystem`.
- Selection context construction now belongs to `BuildingSelectionSystem`.
- Selected-building query context construction now belongs to `BuildingPlacementQuerySystem`.
- `BuildingPlacementSystem` no longer directly constructs `BuildingSelectionSystem.Context`, `BuildingSelectionClickSystem.Context`, or `BuildingPlacementQuerySystem.Context`.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2036 lines and <= 120 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Building selection, selected-building focus, placement status text, and selected-building production/health/preview reads still use the same delegates and systems.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Copied focused Step 19 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step49-selection-query-architecture.log -testResults /private/tmp/warline-step49-selection-query-architecture.xml`

Validation result:
- Focused `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity did not write `/private/tmp/warline-step49-selection-query-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.
- Full `git diff --check` remains blocked by unrelated existing whitespace in `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`; this step did not touch that file.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2036 lines.
- The facade still exposes temporary wrapper methods for selection/query callers until production construction and test harnesses migrate.
- Config/init ownership, production composition construction, editor validation harness migration, facade deletion, and allowlist removal remain open.
- Unrelated dirty files remain: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` and `Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs`.

Cross-lane impacts:
- Architecture docs and tests now enforce selection/query context ownership outside the facade.
- No art, scene, UI prefab, balance, or ECS gameplay data was intentionally changed.

Next recommended task:
- Step 20: extract config/init ownership into managed composition or narrow startup/config systems.
