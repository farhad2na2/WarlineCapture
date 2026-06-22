# Phase 7 Agent D Handoff - P7-0119 BuildingRuntimeQuerySystem Helper Fold

## Scope

- Inventory row: `P7-0119 BuildingRuntimeQuerySystem`
- File: `Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs`
- Lane: Agent D building/production

## Change

- Folded `BuildingRuntimeQuerySystem` from a disabled `SystemBase` wrapper into a plain direct-owned runtime building query helper.
- Removed only the unused ECS lifecycle surface: `: SystemBase`, `partial`, `OnCreate`, and `OnUpdate`.
- Preserved the existing helper API and behavior for faction counts, produced-unit counts, pending-production counts, runtime building role/id lists, focus-position reads, destroyed/owner/refugee state reads, combat info reads, approach-cell queries, and base-breach target routing.

## Architecture Notes

- No manager/controller/facade was introduced.
- No replacement broad `ISystem` shell was introduced.
- The helper remains direct-owned by `BuildingGameplayCompositionSourceSystem` and consumed through existing building composition contexts.
- `Unity.Entities` remains required because the helper API and query logic still use `Entity`, `EntityManager`, `DynamicBuffer`, and ECS components.

## Inventory Impact

- Total ECS system declarations: `304`
- Production `SystemBase`/legacy declarations: `171`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `297`
- Agent D rows: `35`
- `SplitThenConvert` rows: `94`

## Validation

- Compile:
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- Building composition smoke:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-runtime-query-helper-fold-smoke.log`
  - Marker: `/private/tmp/warline-phase7-agent-d-runtime-query-helper-fold-smoke.log:699:[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- Runtime building focused validation:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-runtime-query-helper-fold-runtime-building.log`
  - Marker: `/private/tmp/warline-phase7-agent-d-runtime-query-helper-fold-runtime-building.log:580:[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- Phase 7 architecture guard:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `/private/tmp/warline-phase7-agent-a-architecture.log:582:[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- Inventory regeneration:
  - `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- Final hygiene:
  - `git diff --check`

## Coordination

- No shared contract changes were required.
- No Agent C/E/F files were edited for this slice.
