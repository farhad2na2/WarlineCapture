# WarlineCapture Handoff

Lane: Gameplay

Task: Step 18 - extract remaining runtime context factories from `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Building spawn, runtime entity, runtime visual, redirect, combat, runtime query, and barrier context construction now belongs to `BuildingRuntimeContextSystem`.
- `BuildingPlacementSystem` no longer directly constructs:
  - `BuildingSpawnSystem.Context`
  - `BuildingRuntimeEntitySystem.Context`
  - `BuildingRuntimeVisualSystem.Context`
  - `BuildingPlacementRedirectSystem.Context`
  - `BuildingCombatSystem.Context<RuntimeBuildingData>`
  - `BuildingRuntimeQuerySystem.Context`
  - `BuildingBarrierSystem.Context`
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 2038 lines and <= 120 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Runtime building spawn, visual updates, combat cleanup, road-barrier updates, redirect scans, and runtime query paths still use the same systems and delegates.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Copied focused step 18 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- First Unity run failed with a C# explicit/implicit lambda mismatch in `BuildingRuntimeContextSystem`; fixed by making the barrier approach-cell delegate parameters explicit.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step48-runtime-context-architecture-rerun.log -testResults /private/tmp/warline-step48-runtime-context-architecture-rerun.xml`

Validation result:
- Focused `git diff --check` passed.
- Full `git diff --check` is blocked by unrelated existing whitespace in `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`; this step did not touch that file.
- Unity batchmode rerun exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity did not write `/private/tmp/warline-step48-runtime-context-architecture-rerun.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 2038 lines.
- Runtime context source assembly still lives in the facade until Step 21 moves production construction out of `BuildingGameplayCompositionSystem`.
- Selection/query facade wrappers, config/init ownership, production composition construction, and editor test harnesses still block facade deletion.
- Unrelated dirty files remain: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` and `Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs`.

Cross-lane impacts:
- Architecture docs and tests now enforce the expanded `BuildingRuntimeContextSystem` ownership boundary.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 19: extract selection and query facade wrappers, starting with `CreateBuildingSelectionContext`, `CreateBuildingSelectionClickContext`, and `CreateBuildingPlacementQueryContext`.
