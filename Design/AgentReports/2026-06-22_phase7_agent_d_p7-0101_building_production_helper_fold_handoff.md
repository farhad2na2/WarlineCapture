# Phase 7 Agent D Handoff - P7-0101 BuildingProductionSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems
Slice: P7-0101 `BuildingProductionSystem`

## Summary

Folded `BuildingProductionSystem` from a disabled `SystemBase` wrapper into a plain direct-owned building production helper. Runtime behavior stayed unchanged: pending-production pooling, production queueing, production slot reservation, transport setting resolution, production source-key matching, progress calculation, produced-unit pruning, and transport launch timing still run through the existing building composition owner.

## Rows Completed

- `P7-0101` - `BuildingProductionSystem` - Retired/folded disabled `SystemBase` wrapper

## Responsibility Split

Old:
- `BuildingProductionSystem : SystemBase`
- Disabled itself in `OnCreate`.
- Had an empty `OnUpdate`.
- Was manually constructed by `BuildingGameplayCompositionSourceSystem`, not resolved from a Unity ECS world.
- Exposed managed production helper APIs used by runtime building composition, production request boundary, UI query, build drawer, tests, and PlayMode validation.

New:
- `BuildingProductionSystem` is a plain sealed helper.
- It remains direct-owned by `BuildingGameplayCompositionSourceSystem`.
- ECS lifecycle hooks were removed.
- Existing production helper behavior stayed in the helper because this slice only removes invalid ECS lifetime ownership; future Agent D work can split true production request/state processors into narrow `ISystem` jobs.

## Counts

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionRequestValidation -logFile /private/tmp/warline-phase7-agent-d-building-production-helper-fold-production-request.log`: passed, marker `[BuildingProductionRequestValidation] result=Passed tests=21`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionMetadataValidation -logFile /private/tmp/warline-phase7-agent-d-building-production-helper-fold-production-metadata.log`: passed, marker `[BuildingProductionMetadataValidation] result=Passed tests=3`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-building-production-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

## Inventory Impact

- Total ECS system declarations: `281`
- Production `SystemBase`/legacy declarations: `148`
- Production `ISystem` declarations: `133`
- Current production `ISystem` share: `47.3%`
- Production non-UI rows: `274`
- Production UI rows: `7`
- Agent D remaining rows: `12`
- SplitThenConvert rows: `71`
- Open rows: `126`

## Why ISystem Did Not Increase

This slice removed a disabled ECS wrapper around a plain helper. It is a retire/fold slice, not an `ISystem` conversion. The production `SystemBase` denominator decreased, while the production `ISystem` count stayed at `133`.

## Risks

- This slice does not convert production policy to unmanaged ECS. It removes the disabled ECS wrapper only.
- Remaining production transport rows still require split-before-convert work before any true `ISystem` conversion can be done safely.
