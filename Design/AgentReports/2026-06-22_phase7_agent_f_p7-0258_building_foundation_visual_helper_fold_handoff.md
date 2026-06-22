# Phase 7 Agent F Handoff - P7-0258 BuildingFoundationVisualSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0258 BuildingFoundationVisualSystem`

## Summary

Folded `BuildingFoundationVisualSystem` out of ECS into a plain direct-owned helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; runtime building creation already invoked it directly when applying surface foundation data. `BuildingRuntimeCreationSystem` now instantiates the helper directly instead of resolving it from the ECS World.

## Behavior Preservation

- Preserved runtime building GameObject foundation-height adjustment.
- Preserved combat-entity `LocalTransform` foundation-height update.
- Preserved `BuildingSurfaceComponent` add/update behavior from `BuildingSurfacePlacementSystem.Result`.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `48`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `73.6%`.
- Total ECS declarations: `182`.
- Open rows: `23`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-building-foundation-visual-helper-fold-runtime-building.log`
- `/private/tmp/warline-phase7-agent-f-building-foundation-visual-helper-fold-runtime-building.log`: `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
