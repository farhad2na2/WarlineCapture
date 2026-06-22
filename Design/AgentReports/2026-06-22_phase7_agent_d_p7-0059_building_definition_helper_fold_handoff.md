# Phase 7 Agent D Handoff - P7-0059 BuildingDefinitionSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems
Slice: P7-0059 `BuildingDefinitionSystem`

## Summary

Folded `BuildingDefinitionSystem` from a disabled `SystemBase` wrapper into a plain direct-owned building definition helper. Runtime behavior stayed unchanged: configured spawnable lookup, configured unit lookup, building definition metadata resolution, unit definition metadata resolution, production source-key resolution, prefab local-bounds caching, combined visual template creation/cleanup, and runway metadata extraction still run through the existing building composition owner.

## Rows Completed

- `P7-0059` - `BuildingDefinitionSystem` - Retired/folded disabled `SystemBase` wrapper

## Responsibility Split

Old:
- `BuildingDefinitionSystem : SystemBase`
- Disabled itself in `OnCreate`.
- Had an empty `OnUpdate`.
- Was manually constructed by `BuildingGameplayCompositionSourceSystem`, not resolved from a Unity ECS world.
- Exposed managed definition/config lookup APIs used by placement, production, runtime boundary, UI query, initial spawn, and tests.

New:
- `BuildingDefinitionSystem` is a plain sealed helper.
- It remains direct-owned by `BuildingGameplayCompositionSourceSystem`.
- ECS lifecycle hooks were removed.
- Existing managed prefab/config lookup behavior stayed in the helper because this slice only removes invalid ECS lifetime ownership; future Agent D work can split pure request/read-model publication from Unity-object prefab/config boundaries.

## Counts

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionMetadataValidation -logFile /private/tmp/warline-phase7-agent-d-building-definition-helper-fold-production-metadata.log`: passed, marker `[BuildingProductionMetadataValidation] result=Passed tests=3`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-d-building-definition-helper-fold-runtime-boundary.log`: passed, marker `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-building-definition-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Inventory Impact

- Total ECS system declarations: `282`
- Production `SystemBase`/legacy declarations: `149`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `275`
- Production UI rows: `7`
- Agent D remaining rows: `13`
- SplitThenConvert rows: `72`
- Open rows: `127`

## Risks

- This slice does not convert definition/config lookup into unmanaged ECS. It removes the disabled ECS wrapper only.
- Remaining broad Agent D rows still require split-before-convert work, especially production and transport production systems.
