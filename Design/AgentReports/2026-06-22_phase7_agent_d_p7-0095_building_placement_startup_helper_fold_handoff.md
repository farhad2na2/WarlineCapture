# Phase 7 Agent D Handoff - P7-0095 BuildingPlacementStartupSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems
Slice: P7-0095 `BuildingPlacementStartupSystem`

## Summary

Folded `BuildingPlacementStartupSystem` from a disabled `SystemBase` wrapper into a plain direct-owned placement startup helper. The runtime behavior stayed unchanged: building placement config application, road-footprint state binding, configured spawnable definition rebuild, runtime building root creation, preview initialization, road-footprint queries, and disposal still run through the existing building composition owner.

## Rows Completed

- `P7-0095` - `BuildingPlacementStartupSystem` - Retired/folded disabled `SystemBase` wrapper

## Responsibility Split

Old:
- `BuildingPlacementStartupSystem : SystemBase`
- Disabled itself in `OnCreate`.
- Had an empty `OnUpdate`.
- Was manually constructed by `BuildingGameplayCompositionSourceSystem`, not resolved from a Unity ECS world.
- Owned managed startup/config helper methods used by building composition.

New:
- `BuildingPlacementStartupSystem` is a plain sealed helper.
- It remains direct-owned by `BuildingGameplayCompositionSourceSystem`.
- ECS lifecycle hooks were removed.
- Managed startup/config helper behavior stayed in the same type because it is direct composition support, not an ECS runtime system.

## Counts

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-phase7-agent-d-placement-startup-helper-fold-placement-command.log`: passed, marker `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-startup-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Inventory Impact

- Total ECS system declarations: `284`
- Production `SystemBase`/legacy declarations: `151`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `277`
- Production UI rows: `7`
- Agent D remaining rows: `15`
- SplitThenConvert rows: `74`
- Open rows: `129`

## Risks

- This slice does not convert building startup/config behavior to unmanaged ECS. It only removes an invalid ECS lifecycle wrapper around direct-owned managed composition helper behavior.
- Broad Agent D owners such as `BuildingDefinitionSystem`, `BuildingCombatSystem`, `BuildingProductionSystem`, and transport production bridges remain open and need split-before-convert work.
