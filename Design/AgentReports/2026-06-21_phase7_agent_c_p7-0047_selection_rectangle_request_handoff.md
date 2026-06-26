# Phase 7 Agent C Handoff - P7-0047 SelectionRectangleRequestCompositionSystemHelper

Date: `2026-06-21`

Lane: `AgentC - Selection, Commands, Focus, And Player Intent`

## Summary

`SelectionRectangleRequestCompositionSystemHelper` was retired/folded from a disabled `SystemBase` shell into a plain manually constructed selection-rectangle request helper.

The public behavior was preserved:

- extraction and consumption of pending rectangle pointer requests;
- visible player unit collection through the existing `VisibleUnitSelectionCameraSystemHelper` helper;
- fallback building selection when no units are in the rectangle;
- selected unit tag application;
- selected move cache updates;
- HUD selection and squad callbacks;
- focused-unit assignment through `FocusedUnitLifecycleSystem`.

No new ECS owner, manager, controller, facade, MonoBehaviour loop, or managed presentation exception was introduced.

## Files Changed

- `Assets/Game/Scripts/Systems/SelectionRectangleRequestCompositionSystemHelper.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Inventory Impact

Regenerated command:

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
```

Updated inventory:

- Total ECS system declarations: `369`
- Production `SystemBase`/legacy declarations: `236`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `362`
- Production UI rows: `7`
- Agent C rows: `20`
- SplitThenConvert rows: `128`

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-rectangle-rts-selection-input.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-selection-rectangle-request-result.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- `dotnet build` passed with `0 Warning(s), 0 Error(s)`.
- `/private/tmp/warline-phase7-agent-c-selection-rectangle-rts-selection-input.log`: `[RtsSelectionInputSystemValidation] result=Passed tests=56`
- `/private/tmp/warline-phase7-agent-c-selection-rectangle-request-result.log`: `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check` passed.

## Coordination Notes

- This was a fold, not an `ISystem` conversion. The helper has no independent ECS update lifetime and is still manually owned by the selection startup/runtime composition path.
- Camera remains an explicit method argument supplied by the existing managed input/composition boundary; this slice did not move camera ownership into ECS.
- Building fallback selection remains delegated through `TrySelectBuildingInRectAction`; no Agent D building implementation was changed.
- Continue Agent C with the next selection/command boundary from `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`.
