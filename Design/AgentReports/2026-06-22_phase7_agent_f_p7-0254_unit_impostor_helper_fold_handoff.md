# Phase 7 Agent F Handoff - P7-0254 UnitImpostorRenderSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0254 UnitImpostorRenderSystem`

## Summary

Folded `UnitImpostorRenderSystem` out of ECS into a plain direct-owned `IUnitImpostorRenderer` helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; its rendering work was already driven explicitly by the gameplay runtime late-update path. `MatchBootstrapSystem` now instantiates the helper directly, initializes it with the same camera/layer/unit-prefab-registry/metadata inputs, and disposes it through the existing `IUnitImpostorRenderer` shutdown path.

## Behavior Preservation

- Preserved the existing `LateUpdate()` impostor rendering entry point.
- Preserved registry prefab lookup and impostor atlas/material setup.
- Preserved ECS query-driven culled-unit and source-key fallback candidate selection.
- Preserved `Graphics.RenderMeshInstanced` batching and render parameter setup.
- Preserved query and runtime material/mesh cleanup through `Dispose()`.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `50`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `72.8%`.
- Total ECS declarations: `184`.
- Open rows: `25`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitRenderBudgetSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-unit-impostor-helper-fold-render-budget.log`
- `/private/tmp/warline-phase7-agent-f-unit-impostor-helper-fold-render-budget.log`: `[UnitRenderBudgetFocusedValidation] result=Passed tests=31`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
