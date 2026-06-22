# Phase 7 Agent F Handoff - P7-0261 BuildingPlacementVisualSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0261 BuildingPlacementVisualSystem`

## Summary

Folded `BuildingPlacementVisualSystem` out of ECS into a plain direct-owned helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; building placement/runtime composition already invoked it directly through visual creation and positioning delegates. `BuildingGameplayCompositionSourceSystem` now instantiates the helper directly instead of resolving it from the ECS World.

## Behavior Preservation

- Preserved visual wrapper creation and prefab instantiation.
- Preserved `CombinedMesh` renderer filtering behavior.
- Preserved placement world positioning, rotation, local bounds offset, and scale reset.
- Preserved gate-near-wall alignment override behavior.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `47`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `74.0%`.
- Total ECS declarations: `181`.
- Open rows: `22`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-building-placement-visual-helper-fold-placement-runtime.log`
- `/private/tmp/warline-phase7-agent-f-building-placement-visual-helper-fold-placement-runtime.log`: `[BuildingPlacementRuntimeTickFocusedValidation] result=Passed tests=3`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
