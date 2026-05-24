# WarlineCapture Handoff

Lane: Gameplay

Task: Step 20 - extract config/init ownership into a narrow startup/config system.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Building placement config application, runtime camera/build-plane/preview config state, runtime building root creation, configured definition startup selection, and placement preview initialization now belong to `BuildingPlacementStartupSystem`.
- `BuildingPlacementSystem` no longer owns serialized config/cache fields, direct `RuntimeBuildings` root creation, or direct configured spawnable lookup rebuild.
- Retirement drift guard updated to `BuildingPlacementSystem.cs` <= 1982 lines and <= 120 public/internal facade declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Building placement config, preview colors/height, build plane, world camera override, and configured Soldier Base/Tent/Factory placement definitions should resolve as before.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Copied focused Step 20 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step50-startup-config-architecture.log -testResults /private/tmp/warline-step50-startup-config-architecture.xml`

Validation result:
- Focused `git diff --check` passed.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity did not write `/private/tmp/warline-step50-startup-config-architecture.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.
- Full `git diff --check` remains blocked by unrelated existing whitespace in `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`; this step did not touch that file.

Known gaps:
- `BuildingPlacementSystem` still exists as a temporary composition facade at 1982 lines.
- The facade still exposes `Init(...)`, `BindDependencies(...)`, and `Dispose()` compatibility methods because Step 21 has not yet replaced `BuildingGameplayCompositionSystem` facade construction.
- Runtime dependency binding and runtime building instance/combat/blocker cleanup still run through the facade until production construction moves to a narrow building gameplay harness.
- Unrelated dirty files remain: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`, `Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs`, and prior Step 19 files/reports not committed.

Cross-lane impacts:
- Architecture docs and tests now enforce startup/config ownership outside the facade.
- No art, scene, UI prefab, balance, or ECS gameplay data was intentionally changed.

Next recommended task:
- Step 21: replace `BuildingGameplayCompositionSystem` construction so it no longer calls `new BuildingPlacementSystem()`.
