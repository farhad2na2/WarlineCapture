# Phase 7 Agent F Handoff - P7-0263 BuildingRuntimeVisualSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0263 BuildingRuntimeVisualSystem`

## Summary

Folded `BuildingRuntimeVisualSystem` out of ECS into a plain direct-owned helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; runtime building composition already invoked it directly through initialization, resource visual, and marker-refresh helper calls. `BuildingGameplayCompositionSourceSystem` now instantiates the helper directly instead of resolving it from the ECS World.

## Behavior Preservation

- Preserved runtime building visual initialization and alive-root setup.
- Preserved `Door_Z` and barrier setup.
- Preserved animated part collection and faction renderer cache/application.
- Preserved resource visual animation updates.
- Preserved destroyed-building marker visibility refresh and faction clear behavior.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `46`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `74.4%`.
- Total ECS declarations: `180`.
- Open rows: `21`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingSelectionMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-building-runtime-visual-helper-fold-selection-marker.log`
- `/private/tmp/warline-phase7-agent-f-building-runtime-visual-helper-fold-selection-marker.log`: `[BuildingSelectionMarkerFocusedValidation] result=Passed tests=6`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
