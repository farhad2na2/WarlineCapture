# Phase 7 Agent D Handoff - P7-0111 BuildingRuntimeCompositionQuerySystem

Date: 2026-06-22

## Scope

- Row: `P7-0111 BuildingRuntimeCompositionQuerySystem`
- Lane: Agent D building/production
- File changed: `Assets/Game/Scripts/Systems/BuildingRuntimeCompositionQuerySystem.cs`

## Change

- Folded `BuildingRuntimeCompositionQuerySystem` from a disabled `SystemBase` wrapper into a plain direct-owned helper.
- Removed the empty `OnCreate`/`OnUpdate` lifecycle while retaining `Unity.Entities` for delegate types that expose `EntityManager`, `Entity`, and `DynamicBuffer`.
- Preserved house detection, runtime building lookup, focus-world-position resolution, effective placement rectangle calculation, and runtime-building overlap checks.

## Architecture Notes

- No new manager/controller/facade was introduced.
- No broad replacement `ISystem` shell was introduced for helper-only behavior.
- The helper remains owned by `BuildingGameplayCompositionSourceSystem` through direct construction.
- Unity-object/runtime-building read logic remains a managed query helper boundary instead of being misclassified as a hot `ISystem`.

## Inventory Impact

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- New inventory counts:
  - Total ECS declarations: `305`
  - Production `SystemBase`/legacy declarations: `172`
  - Production `ISystem` declarations: `133`
  - Production non-UI rows: `298`
  - Agent D rows: `36`
  - `SplitThenConvert` rows: `95`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Passed with `0 Warning(s), 0 Error(s)`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-runtime-composition-query-helper-fold-smoke.log`
  - Passed: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-runtime-composition-query-helper-fold-runtime-building.log`
  - Passed: `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Passed: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Coordination

- Agent C/E/F coordination was not required for this helper fold.
- Visual behavior was not changed.
- No rows were returned to Agent A for reclassification.
