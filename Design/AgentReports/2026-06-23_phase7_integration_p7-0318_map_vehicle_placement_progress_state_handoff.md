# Phase 7 Integration Handoff - P7-0318 MapVehiclePlacementSpawnSystem

Date: 2026-06-23
Lane: Integration
Row: `P7-0318` `MapVehiclePlacementSpawnSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS and moved placement progress state into ECS data.

## Summary

`MapVehiclePlacementSpawnSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. The class remains a direct helper owned by building gameplay composition.

This slice moved placement queue completion, authoring-hidden completion, next placement index, last cleared blocker cells, and random state into `MapVehiclePlacementProgressState`, an ECS singleton component. It intentionally did not add a broad replacement `ISystem`; the remaining work is to extract narrow placement scanning, instantiation, blocker, and result processors.

## Files Changed

- `Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs`
- `Assets/Game/Scripts/Systems/MapVehiclePlacementSpawnSystem.cs`
- `Assets/Tests/Editor/UnitMovementBlockerValidationTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`

## Behavior Preserved

- Building gameplay composition still invokes the direct helper.
- Managed `MapVehiclePlacementConfig` and authoring root visibility remain at the managed edge.
- Empty placement config completion still queues placement completion and hides authoring visuals.
- Placement clearance still records the last cleared blocker-cell count.
- Random state progression remains deterministic but is now persisted in ECS state.

## Inventory Impact

- Total ECS declarations: `168`.
- Production `SystemBase`/legacy declarations: `31`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `81.5%`.
- Managed exceptions: `24`.
- Open rows: `6`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMovementBlockerValidationTests.RunBatchValidation -logFile /private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log`: `[UnitMovementBlockerValidation] result=Passed`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Follow-Up

Continue the remaining `MapVehiclePlacementSpawnSystem` split work before marking the five-system target complete:

- Extract `MapVehiclePlacementProgressSystem`.
- Extract `MapVehiclePlacementInstantiateSystem`.
- Extract `MapVehiclePlacementBlockerSystem`.
- Extract `MapVehiclePlacementResultSystem`.
- Update building gameplay composition to schedule/request through the focused ECS processors.
- Retire or rename the remaining direct helper once it no longer owns runtime placement execution.
