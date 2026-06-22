# Phase 7 Integration Handoff - P7-0323 ResourceHaulerSystem

Date: 2026-06-23
Lane: Integration
Row: `P7-0323` `ResourceHaulerSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS into a plain direct-owned resource-hauler helper.

## Summary

`ResourceHaulerSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. The class is used as a direct helper for resource-hauler order, phase, timer, cargo, load, unload, and capacity logic.

This slice intentionally did not add an `ISystem`: the current file has no recurring ECS update loop to convert and the direct helper API is still called by building/resource composition. No broad replacement shell was introduced.

## Files Changed

- `Assets/Game/Scripts/Systems/ResourceHaulerSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- Resource-hauler order creation and phase transitions remain unchanged.
- Timed action start/wait/ready semantics remain unchanged.
- Oil/fuel source and destination classification remains unchanged.
- Load, unload, cargo reset, revert, and receiving-capacity behavior remain unchanged.

## Inventory Impact

- Total ECS declarations: `166`.
- Production `SystemBase`/legacy declarations: `29`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `82.5%`.
- Managed exceptions: `24`.
- Open rows: `4`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ResourceHaulerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-resource-hauler-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-integration-resource-hauler-helper-fold.log`: `[ResourceHaulerFocusedValidation] result=Passed tests=9`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Follow-Up

Continue remaining Integration split rows:

- `P7-0325` `RuntimeRootSystem`
- `P7-0374` `VisibleUnitSelectionSystem`

Agent B rows `P7-0003` and `P7-0019` remain held pending an explicit managed-reference boundary guardrail/model change.
