# Phase 7 Agent F Handoff - P7-0264 BuildingSelectionMarkerSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0264 BuildingSelectionMarkerSystem`

## Summary

Folded `BuildingSelectionMarkerSystem` out of ECS into a plain direct-owned helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; runtime building composition already invoked it directly through selection marker refresh, hide, and dispose calls. `BuildingGameplayCompositionSourceSystem` now instantiates the helper directly instead of resolving it from the ECS World.

## Behavior Preservation

- Preserved marker prefab instantiation, parent assignment, and runtime marker naming.
- Preserved active-building selection resolution and destroyed-building hiding.
- Preserved marker position, rotation, footprint scaling, surface lift, and renderer-bounds filtering.
- Preserved premium boundary view setup.
- Preserved premium object outline setup.
- Preserved hide and dispose behavior.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `45`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `74.9%`.
- Total ECS declarations: `179`.
- Open rows: `20`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingSelectionMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-building-selection-marker-helper-fold.log`
- `/private/tmp/warline-phase7-agent-f-building-selection-marker-helper-fold.log`: `[BuildingSelectionMarkerFocusedValidation] result=Passed tests=6`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
