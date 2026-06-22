# Phase 7 Agent F Handoff - P7-0256 BuildingDestroyedVisualSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0256 BuildingDestroyedVisualSystem`

## Summary

Folded `BuildingDestroyedVisualSystem` out of ECS into a plain direct-owned visual helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; destroyed-building visual work was already invoked directly through building combat/runtime contexts. `BuildingGameplayCompositionSourceSystem` now instantiates the helper directly instead of resolving it from the ECS World.

## Behavior Preservation

- Preserved destroyed-prefab instantiation and naming.
- Preserved alive visual root hiding through `BuildingVisualSystem.SetTransformVisible`.
- Preserved existing destroyed visual instance reuse.
- Preserved destroyed visual cleanup through the configured destroy delegate.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `49`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `73.2%`.
- Total ECS declarations: `183`.
- Open rows: `24`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingDestroyedVisualSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-building-destroyed-visual-helper-fold.log`
- `/private/tmp/warline-phase7-agent-f-building-destroyed-visual-helper-fold.log`: `[BuildingDestroyedVisualFocusedValidation] result=Passed tests=2`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
