# Phase 7 Agent D Handoff - P7-0058 BuildingCombatSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems
Slice: P7-0058 `BuildingCombatSystem`

## Summary

Folded `BuildingCombatSystem` from a disabled `SystemBase` wrapper into a plain direct-owned building combat helper. Runtime behavior stayed unchanged: destroyed-building state mutation, destroyed cleanup-id collection, runtime combat-state resolution from `EntityManager`, blocker entity destruction, runtime building entity destruction sync, destroyed visual handoff, runtime object cleanup, marker refresh, minimap notification, and diagnostic callbacks still run through the existing building composition owner.

## Rows Completed

- `P7-0058` - `BuildingCombatSystem` - Retired/folded disabled `SystemBase` wrapper

## Responsibility Split

Old:
- `BuildingCombatSystem : SystemBase`
- Disabled itself in `OnCreate`.
- Had an empty `OnUpdate`.
- Was manually constructed by `BuildingGameplayCompositionSourceSystem`, not resolved from a Unity ECS world.
- Exposed managed helper APIs used by runtime building composition and tests.

New:
- `BuildingCombatSystem` is a plain sealed helper.
- It remains direct-owned by `BuildingGameplayCompositionSourceSystem`.
- ECS lifecycle hooks were removed.
- Existing `EntityManager` helper operations stayed in the helper because this slice only removes invalid ECS lifetime ownership; broader building combat/data conversion remains future Agent D split-before-convert work.

## Counts

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingCombatSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-building-combat-helper-fold-combat.log`: passed, marker `[BuildingCombatFocusedValidation] result=Passed tests=4`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-building-combat-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Inventory Impact

- Total ECS system declarations: `283`
- Production `SystemBase`/legacy declarations: `150`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `276`
- Production UI rows: `7`
- Agent D remaining rows: `14`
- SplitThenConvert rows: `73`
- Open rows: `128`

## Risks

- This slice does not convert building combat policy to unmanaged ECS. It removes the disabled ECS wrapper only.
- `BuildingDefinitionSystem`, `BuildingProductionSystem`, `BuildingProductionTransportBridgeSystem`, `BuildingProductionTransportSystem`, and `BuildingSelectionSystem` remain open broad Agent D rows requiring split-before-convert work.
