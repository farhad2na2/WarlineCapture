# Phase 7 Agent C Handoff - P7-0025 FocusedUnitLifecycleSystem

Date: `2026-06-21`

Lane: `AgentC - Selection, Commands, Focus, And Player Intent`

## Summary

`FocusedUnitLifecycleSystem` was retired/folded from a disabled `SystemBase` shell into a plain manually constructed selected/focused lifecycle helper.

The public API and call-site contract were preserved:

- selected-tag clearing and selected-entity collection;
- focused-unit get/set/clear through `SelectionStateCompositionSystemHelper`;
- clicked-unit focus through injected delegates;
- HUD selection and squad callbacks;
- air-selection cleanup hook;
- lifecycle diagnostics.

No new ECS owner, manager, controller, facade, MonoBehaviour loop, or managed presentation exception was introduced.

## Files Changed

- `Assets/Game/Scripts/Systems/FocusedUnitLifecycleSystem.cs`
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

- Total ECS system declarations: `370`
- Production `SystemBase`/legacy declarations: `237`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `363`
- Production UI rows: `7`
- Agent C rows: `21`
- SplitThenConvert rows: `129`

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionStateCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-selection-state.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-rts-selection-input.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-request-result.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudSquadTraySelectionSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-squad-tray.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionSummaryQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-summary.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- `dotnet build` passed with `0 Warning(s), 0 Error(s)`.
- `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-selection-state.log`: `[SelectionStateFocusedValidation] result=Passed tests=7`
- `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-rts-selection-input.log`: `[RtsSelectionInputSystemValidation] result=Passed tests=56`
- `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-request-result.log`: `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`
- `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-squad-tray.log`: `[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`
- `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-summary.log`: `[SelectionSummaryFocusedValidation] result=Passed tests=11`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check` passed.

## Coordination Notes

- This was a fold, not an `ISystem` conversion. The helper has no independent ECS update lifetime and is still manually owned by the selection startup/runtime composition path.
- No Agent D building, Agent E road/city/citizen, or Agent F visual/camera implementation was changed.
- Continue Agent C with the next selection/command boundary from `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`.
