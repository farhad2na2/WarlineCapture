# Phase 7 Agent D Handoff - P7-0113 BuildingRuntimeContextSystem Helper Fold

## Scope

- Inventory row: `P7-0113 BuildingRuntimeContextSystem`
- File: `Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs`
- Lane: Agent D building/production

## Change

- Folded `BuildingRuntimeContextSystem` from a disabled `SystemBase` wrapper into a plain direct-owned runtime context factory helper.
- Removed only the unused ECS lifecycle surface: `: SystemBase`, `partial`, `OnCreate`, and `OnUpdate`.
- Preserved the existing helper API and behavior for runtime source packaging and spawn, spawn-command, creation, ownership, city-spawn, building-spawn, runtime-entity, runtime-visual, selection-marker, redirect, combat, runtime-query, barrier, and resource-hauler context construction.

## Architecture Notes

- No manager/controller/facade was introduced.
- No broad replacement `ISystem` shell was introduced.
- The helper remains direct-owned by `BuildingGameplayCompositionSourceSystem` and consumed through existing building runtime composition flows.
- `Unity.Entities` remains required because the context factory API carries `Entity`, `EntityManager`, `EntityQuery`, `DynamicBuffer`, and ECS grid data.
- Unity-object references remain data passed through existing managed composition boundaries; this slice removed only the unnecessary ECS system lifetime.

## Inventory Impact

- Total ECS system declarations: `303`
- Production `SystemBase`/legacy declarations: `170`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `296`
- Agent D rows: `34`
- `SplitThenConvert` rows: `93`

## Validation

- Compile:
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- Building composition smoke:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-runtime-context-helper-fold-smoke.log`
  - Marker: `/private/tmp/warline-phase7-agent-d-runtime-context-helper-fold-smoke.log:768:[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- Runtime building focused validation:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-runtime-context-helper-fold-runtime-building.log`
  - Marker: `/private/tmp/warline-phase7-agent-d-runtime-context-helper-fold-runtime-building.log:582:[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- Phase 7 architecture guard:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `/private/tmp/warline-phase7-agent-a-architecture.log:581:[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- Inventory regeneration:
  - `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- Final hygiene:
  - `git diff --check`

## Coordination

- No shared contract changes were required.
- No Agent C/E/F files were edited for this slice.
