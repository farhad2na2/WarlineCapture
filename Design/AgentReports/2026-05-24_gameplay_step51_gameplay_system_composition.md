# WarlineCapture Handoff

Lane: Gameplay

Task: Step 21 - replace `BuildingGameplayCompositionSystem` construction so it no longer calls `new BuildingPlacementSystem()`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Production composition now constructs `BuildingGameplaySystem`, not `BuildingPlacementSystem`.
- `BuildingPlacementSystem.cs` is reduced to a one-line legacy wrapper for existing editor validation harnesses.
- `BuildingGameplaySystem.cs` owns the former implementation at 1981 lines, with 120 public/internal implementation declarations.
- Production `BuildingPlacementSystem` construction is now forbidden; production facade references are limited to the one-line wrapper file only.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Runtime building placement, UI command/query wiring, city spawn wiring, citizen population wiring, and building runtime ticking still use the same implementation and contexts through `BuildingGameplaySystem`.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs.meta Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Production facade reference scan: `rg --pcre2 -n "\bBuildingPlacementSystem\b(?!Config)|new\s+BuildingPlacementSystem\s*\(" Assets/Game/Scripts -g '*.cs'`
- Copied focused Step 21 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step51-gameplay-system-composition.log -testResults /private/tmp/warline-step51-gameplay-system-composition.xml`

Validation result:
- Focused `git diff --check` passed.
- Production facade reference scan found only `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`.
- Unity batchmode exited 0 with no `error CS`, no `Scripts have compiler errors`, and no `Compilation failed` entries.
- Unity did not write `/private/tmp/warline-step51-gameplay-system-composition.xml`; result is compile-clean batchmode validation rather than a confirmed XML test result.
- Full `git diff --check` remains blocked by unrelated existing whitespace in `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`; this step did not touch that file.

Known gaps:
- Existing editor validation harnesses still construct or type against `BuildingPlacementSystem`; Step 22 must migrate those to a narrow building gameplay harness.
- `BuildingGameplaySystem` still exposes former facade compatibility methods and source/context wrappers until the remaining migration steps remove them.
- `BuildingPlacementSystem.cs` and `.meta` still exist as a one-line legacy wrapper until Step 23.
- Unrelated dirty files remain: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`, `Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs`, and prior Step 19/20 files/reports not committed.

Cross-lane impacts:
- Architecture docs and tests now treat `BuildingPlacementSystem` as legacy editor-harness wrapper debt only.
- No art, scene, UI prefab, balance, or ECS gameplay data was intentionally changed.

Next recommended task:
- Step 22: migrate remaining editor validation tests to a narrow building gameplay harness.
