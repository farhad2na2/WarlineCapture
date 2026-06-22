# Phase 7 Agent D Handoff - P7-0080 BuildingPlacementInputSystem

Date: 2026-06-22

## Scope

- Row: `P7-0080 BuildingPlacementInputSystem`
- Lane: Agent D building/production
- File changed: `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs`

## Change

- Folded `BuildingPlacementInputSystem` from a disabled `SystemBase` wrapper into a plain direct-owned helper.
- Removed the empty `OnCreate`/`OnUpdate` lifecycle and `Unity.Entities` dependency.
- Preserved pointer drag state, placement hover updates, wall placement origin scratch buffers, final wall-run scratch buffers, wall-run commit behavior, and placement UI pointer suppression behavior.

## Architecture Notes

- No new manager/controller/facade was introduced.
- No broad replacement `ISystem` shell was introduced for helper-only behavior.
- The helper remains owned by `BuildingGameplayCompositionSourceSystem` through direct construction.
- This slice did not move gameplay policy into a managed presentation/config exception.

## Inventory Impact

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- New inventory counts:
  - Total ECS declarations: `307`
  - Production `SystemBase`/legacy declarations: `174`
  - Production `ISystem` declarations: `133`
  - Production non-UI rows: `300`
  - Agent D rows: `38`
  - `SplitThenConvert` rows: `97`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Passed with `0 Warning(s), 0 Error(s)`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-input-helper-fold-smoke.log`
  - Passed: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-placement-input-helper-fold-runtime-building.log`
  - Passed: `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Passed: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Coordination

- Agent C/E/F coordination was not required for this helper fold.
- Visual behavior was not changed.
- No rows were returned to Agent A for reclassification.
