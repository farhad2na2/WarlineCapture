# Phase 7 Agent C Handoff - P7-0048 SelectionRuntimeConfigSystem

Date: `2026-06-21`

Lane: `AgentC - Selection, Commands, Focus, And Player Intent`

## Summary

`SelectionRuntimeConfigSystem` was retired/folded from a disabled `SystemBase` shell into a plain startup config-state factory.

The fold removed the `DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SelectionRuntimeConfigSystem>()` dependency from `SelectionGameplayStartupSystem` and replaced it with the existing static `SelectionRuntimeConfigSystem.CreateStateFromConfig(...)` path.

The public behavior was preserved:

- camera fallback resolution;
- move, attack, and target marker prefab references;
- order marker duration;
- drag threshold;
- selection-mode hold duration;
- pan and zoom settings;
- normal/build/fullscreen camera mode values;
- config normalization.

No new ECS owner, manager, controller, facade, MonoBehaviour loop, or managed presentation exception was introduced.

## Files Changed

- `Assets/Game/Scripts/Systems/SelectionRuntimeConfigSystem.cs`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs`
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

- Total ECS system declarations: `368`
- Production `SystemBase`/legacy declarations: `235`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `361`
- Production UI rows: `7`
- Agent C rows: `19`
- SplitThenConvert rows: `127`

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-runtime-config-rts-selection-input.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-selection-runtime-config-request-result.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- `dotnet build` passed with `0 Warning(s), 0 Error(s)`.
- `/private/tmp/warline-phase7-agent-c-selection-runtime-config-rts-selection-input.log`: `[RtsSelectionInputSystemValidation] result=Passed tests=56`
- `/private/tmp/warline-phase7-agent-c-selection-runtime-config-request-result.log`: `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check` passed.

## Coordination Notes

- This was a fold, not an `ISystem` conversion. The helper has no independent ECS update lifetime.
- Camera and GameObject references remain serialized config data inside the startup-created config state; they were not moved into unmanaged ECS gameplay.
- No Agent D building, Agent E road/city/citizen, or Agent F visual implementation was changed.
- Continue Agent C with the next selection/command boundary from `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`.
