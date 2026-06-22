# Phase 7 Agent D Handoff - P7-0103 BuildingProductionTransportBridgeSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems
Slice: P7-0103 `BuildingProductionTransportBridgeSystem`

## Summary

Folded `BuildingProductionTransportBridgeSystem` from a disabled `SystemBase` wrapper into a plain direct-owned production transport bridge helper. Runtime behavior stayed unchanged: ground goal-cell resolution, faction helipad spawn resolution, unit-footprint lookup, produced-unit movement, produced-unit rotation alignment, spawn-near-building routing, newest produced-unit lookup, read-model fallback lookup, and camera focus callback policy still run through the existing building composition owner.

## Rows Completed

- `P7-0103` - `BuildingProductionTransportBridgeSystem` - Retired/folded disabled `SystemBase` wrapper

## Responsibility Split

Old:
- `BuildingProductionTransportBridgeSystem : SystemBase`
- Disabled itself in `OnCreate`.
- Had an empty `OnUpdate`.
- Was manually constructed by `BuildingGameplayCompositionSourceSystem`, not resolved from a Unity ECS world.
- Exposed managed bridge helper APIs used by production transport, production request, spawn, camera focus, and tests.

New:
- `BuildingProductionTransportBridgeSystem` is a plain sealed helper.
- It remains direct-owned by `BuildingGameplayCompositionSourceSystem`.
- ECS lifecycle hooks were removed.
- Existing bridge helper behavior stayed in the helper because this slice only removes invalid ECS lifetime ownership; future Agent D work can split true transport request/state processors into narrow ECS systems.

## Counts

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionCameraFocusValidation -logFile /private/tmp/warline-phase7-agent-d-production-transport-bridge-helper-fold-camera-focus.log`: passed, marker `[BuildingProductionCameraFocusValidation] result=Passed tests=10`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionRequestValidation -logFile /private/tmp/warline-phase7-agent-d-production-transport-bridge-helper-fold-production-request.log`: passed, marker `[BuildingProductionRequestValidation] result=Passed tests=21`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-production-transport-bridge-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

## Inventory Impact

- Total ECS system declarations: `280`
- Production `SystemBase`/legacy declarations: `147`
- Production `ISystem` declarations: `133`
- Current production `ISystem` share: `47.5%`
- Production non-UI rows: `273`
- Production UI rows: `7`
- Agent D remaining rows: `11`
- SplitThenConvert rows: `70`
- Open rows: `125`

## Why ISystem Did Not Increase

This slice removed a disabled ECS wrapper around a plain helper. It is a retire/fold slice, not an `ISystem` conversion. The production `SystemBase` denominator decreased, while the production `ISystem` count stayed at `133`.

## Risks

- This slice does not convert production transport policy to unmanaged ECS. It removes the disabled ECS wrapper only.
- `BuildingProductionTransportSystem` remains the broad production transport owner and still needs split-before-convert work.
