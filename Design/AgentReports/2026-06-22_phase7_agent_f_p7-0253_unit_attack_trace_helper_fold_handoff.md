# Phase 7 Agent F Handoff - P7-0253 UnitAttackTraceSystem Helper Fold

Date: `2026-06-22`
Lane: `AgentF`
Slice: `P7-0253 UnitAttackTraceSystem`

## Summary

Folded `UnitAttackTraceSystem` out of ECS into a plain direct-owned `IUnitAttackTraceRenderer` helper.

The old type was a disabled `SystemBase` wrapper with empty `OnUpdate`; its real work was already driven explicitly by the gameplay runtime late-update path. `MatchBootstrapSystem` now instantiates the helper directly, initializes it with the same trace config/camera/layer inputs, and disposes it through the existing `IUnitAttackTraceRenderer` shutdown path.

## Behavior Preservation

- Preserved the existing `LateUpdate()` attack-trace drawing entry point.
- Preserved ECS reads for `UnitAttackTraceComponent`, `UnitAttack`, `EngageTarget`, `LocalTransform`, and turret reference buffers.
- Preserved `Graphics.DrawMeshInstanced` batching and material property setup.
- Preserved render-resource cleanup through `Dispose()`.
- Introduced no new manager/controller/facade and no new `MonoBehaviour` update loop.

## Inventory Accounting

After regenerating `Design/Architecture/systembase_to_isystem_inventory.md`:

- Production `SystemBase`/legacy declarations: `51`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `72.4%`.
- Total ECS declarations: `185`.
- Open rows: `26`.
- Managed presentation exceptions: `24`.

This slice reduces the `SystemBase` denominator by one and keeps the `ISystem` count flat because the folded type was a disabled helper wrapper, not a data processor converted to `ISystem`.

## Validation

Commands/logs:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitCombatFocusedEditModeTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-unit-attack-trace-helper-fold-unit-combat.log`
- `/private/tmp/warline-phase7-agent-f-unit-attack-trace-helper-fold-unit-combat.log`: `[UnitCombatFocusedEditModeValidation] result=Passed tests=1`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`

## Next Agent F Candidate

Continue with the remaining Agent F visual split/direct candidates from `Design/Architecture/systembase_to_isystem_inventory.md`, keeping Unity-object presentation in counted managed exceptions where needed.
