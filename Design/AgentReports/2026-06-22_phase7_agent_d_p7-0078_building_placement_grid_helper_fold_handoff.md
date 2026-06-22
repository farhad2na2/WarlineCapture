# Phase 7 Agent D Handoff - P7-0078 BuildingPlacementGridSystem

Date: 2026-06-22

## Scope

- Row: `P7-0078 BuildingPlacementGridSystem`
- Lane: Agent D building/production
- File changed: `Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs`

## Change

- Folded `BuildingPlacementGridSystem` from a disabled `SystemBase` wrapper into a plain direct-owned helper.
- Removed the empty `OnCreate`/`OnUpdate` lifecycle and `Unity.Entities` dependency.
- Preserved footprint center math, center-screen origin resolution, screen-to-grid camera projection, rotated footprint resolution, and wall-run focus-position behavior.

## Architecture Notes

- No new manager/controller/facade was introduced.
- No broad replacement `ISystem` shell was introduced for helper-only behavior.
- The helper remains owned by `BuildingGameplayCompositionSourceSystem` through direct construction.
- The camera-based screen projection remains managed helper code outside ECS rather than being misclassified as a hot `ISystem`.

## Inventory Impact

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- New inventory counts:
  - Total ECS declarations: `308`
  - Production `SystemBase`/legacy declarations: `175`
  - Production `ISystem` declarations: `133`
  - Production non-UI rows: `301`
  - Agent D rows: `39`
  - `SplitThenConvert` rows: `98`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Passed with `0 Warning(s), 0 Error(s)`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-grid-helper-fold-smoke.log`
  - Passed: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-placement-grid-helper-fold-runtime-building.log`
  - Passed: `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Passed: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Coordination

- Agent C/E/F coordination was not required for this helper fold.
- Visual behavior was not changed.
- No rows were returned to Agent A for reclassification.
