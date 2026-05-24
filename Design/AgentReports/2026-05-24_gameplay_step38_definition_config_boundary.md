# WarlineCapture Handoff

Lane: Gameplay

Task: Step 8 - move configured definition/unit registry ownership out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- `BuildingDefinitionSystem` now owns configured spawnable prefab list/read access and configured unit prefab list/read access.
- `BuildingPlacementSystem` no longer stores `spawnables`, `unitPrefabRegistryConfig`, `unitSpawnPrefabs`, or private configured spawnable/unit UI helper wrappers.
- Architecture guard now blocks those fields/helpers from returning to `BuildingPlacementSystem`.
- Retirement audit maximum is now 2216 lines and 128 public/internal declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Camp/building/unit UI reads still route through `BuildingUiCommandSystem`, now backed directly by `BuildingDefinitionSystem`.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step38-definition-config-architecture.log -testResults /private/tmp/warline-step38-definition-config-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 and compiled without `error CS`, `Scripts have compiler errors`, or `Compilation failed`.
- Unity did not emit `/private/tmp/warline-step38-definition-config-architecture.xml`; this is recorded as compile/static-architecture validation, not a confirmed NUnit result.
- Existing unrelated obsolete API warnings from editor scene builders remain.

Known gaps:
- `BuildingPlacementSystem` still forwards the top-level `BuildingPlacementSystemConfig` and still owns many context factory methods.
- `BuildingPlacementSystem` remains a temporary composition facade at 2216 lines / 128 public-internal declarations.

Cross-lane impacts:
- None expected for UI or art; public UI command/query behavior is unchanged.
- Validation clone was updated only for focused compile/architecture validation.

Next recommended task:
- Step 9: move runtime spawn/runtime creation/runtime ownership context factories out of `BuildingPlacementSystem` into managed composition/narrow systems so the facade stops building those contexts directly.
