# Phase 7 Agent F Handoff - P7-0270 RoadBuildVisualContextSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0270 RoadBuildVisualContextSystem`

## Summary

Folded `RoadBuildVisualContextSystem` out of ECS into a plain direct-owned helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; road build composition already used it only for context construction and prefab lookup. `RoadBuildCompositionSourceSystem` now instantiates the helper directly instead of resolving it from the ECS World.

## Behavior Preservation

- Preserved road prefab resolution through `RoadVisualVariantSystem`.
- Preserved road chunk visual context construction.
- Preserved road preview context construction.
- Preserved special-road visual context construction.
- Preserved road visual resolution context call sites.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `44`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `75.3%`.
- Total ECS declarations: `178`.
- Open rows: `19`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-road-build-visual-context-helper-fold-road-build-command.log`
- `/private/tmp/warline-phase7-agent-f-road-build-visual-context-helper-fold-road-build-command.log`: `[RoadBuildCommandRequestValidation] result=Passed tests=7`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
