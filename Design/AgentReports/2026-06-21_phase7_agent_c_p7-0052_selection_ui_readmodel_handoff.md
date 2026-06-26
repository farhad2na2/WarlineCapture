# Phase 7 Agent C Handoff - P7-0052 SelectionUiReadModelUiSystemHelper

Date: `2026-06-21`

Lane: `AgentC - Selection, Commands, Focus, And Player Intent`

## Summary

`SelectionUiReadModelUiSystemHelper` was retired/folded from a disabled `SystemBase` shell into a plain `ISelectionUiReadModel` adapter.

The public behavior was preserved:

- focused unit label, health, ownership, vehicle, attack, hold, stop, scan, and command reason reads;
- focused transport passenger count and passenger list reads;
- selected-unit presence reads;
- visible player unit, soldier, and vehicle screen queries;
- focused-unit status mapping.

No new ECS owner, manager, controller, facade, MonoBehaviour loop, or managed presentation exception was introduced.

## Files Changed

- `Assets/Game/Scripts/Systems/SelectionUiReadModelUiSystemHelper.cs`
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

- Total ECS system declarations: `367`
- Production `SystemBase`/legacy declarations: `234`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `360`
- Production UI rows: `7`
- Agent C rows: `18`
- SplitThenConvert rows: `126`

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionUiReadModelLookupTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-readmodel-lookup.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudSquadTraySelectionSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-readmodel-squad-tray.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-readmodel-rts-selection-input.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-readmodel-request-result.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- `dotnet build` passed with `0 Warning(s), 0 Error(s)`.
- `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-lookup.log`: `[SelectionUiReadModelLookupValidation] result=Passed tests=5`
- `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-squad-tray.log`: `[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`
- `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-rts-selection-input.log`: `[RtsSelectionInputSystemValidation] result=Passed tests=56`
- `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-request-result.log`: `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check` passed.

## Coordination Notes

- This was a fold, not an `ISystem` conversion. The adapter has no independent ECS update lifetime.
- Camera remains an explicit method argument for visible-unit screen queries; this slice did not move camera ownership into ECS.
- The UI contract remains `ISelectionUiReadModel`; UI Toolkit/Canvas implementation was not touched.
- Continue Agent C with the next selection/command boundary from `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`.
