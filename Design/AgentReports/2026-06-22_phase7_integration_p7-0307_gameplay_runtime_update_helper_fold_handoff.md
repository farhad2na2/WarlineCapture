# Phase 7 Integration Handoff - P7-0307 GameplayRuntimeUpdateSystem

Date: 2026-06-22
Lane: Integration
Row: `P7-0307` `GameplayRuntimeUpdateSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS into a plain direct-owned runtime-update helper.

## Summary

`GameplayRuntimeUpdateSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. `MatchBootstrapSystem` already owned this object directly with `new GameplayRuntimeUpdateSystem()`, so the slice preserves the existing managed runtime orchestration path.

This slice intentionally did not add an `ISystem`: the class coordinates managed runtime helpers, UI hooks, render late-update hooks, and a loading-gate EntityQuery read. There was no ECS update loop to move into a narrow unmanaged processor in this safe slice.

## Files Changed

- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs`
- `Assets/Game/Scripts/Editor/GameplayRuntimeUpdateValidationRunner.cs`
- `Assets/Game/Scripts/Editor/GameplayRuntimeUpdateValidationRunner.cs.meta`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- Runtime update ordering remains owned by `MatchBootstrapSystem`.
- Road/building/selection/day-night/citizen/main-menu managed update calls remain unchanged.
- Runtime city, grid blocker, and decoration update gates remain unchanged.
- Loading-gate readiness, fail-open, and diagnostics behavior remains unchanged.
- LateUpdate and OnGUI direct helper paths remain available for attack traces, impostors, road GUI, and selection rectangle drawing.

## Inventory Impact

- Total ECS system declarations: `170`.
- Production `SystemBase`/legacy declarations: `33`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `80.6%`.
- Managed exceptions: `24`.
- Open rows: `8`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod GameplayRuntimeUpdateValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-gameplay-runtime-update-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-integration-gameplay-runtime-update-helper-fold.log`: `[GameplayRuntimeUpdateValidation] result=Passed tests=1`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Notes

An existing `MatchRuntimeShellSmokeValidation.RunInitialBuildingSmoke` batchmode attempt exited successfully but did not emit a pass/fail marker under `-quit`, so it was not counted as the focused validation for this slice. The committed focused runner validates the direct-owned helper contract and inactive update paths deterministically.

## Follow-Up

Continue remaining Integration split-first rows. `P7-0003` and `P7-0019` remain held pending an explicit managed-reference boundary guardrail/model change.
