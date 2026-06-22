# Phase 7 Integration Handoff - P7-0325 RuntimeRootSystem

Date: 2026-06-23
Lane: Integration
Row: `P7-0325` `RuntimeRootSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS into a plain direct-owned runtime-root helper.

## Summary

`RuntimeRootSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. `MatchBootstrapSystem` now owns the helper directly instead of resolving it through `World.GetOrCreateSystemManaged`.

This slice intentionally did not add an `ISystem`: the helper creates and caches managed runtime root `GameObject` / `Transform` references and has no recurring ECS update loop to convert. No broad replacement shell was introduced.

## Files Changed

- `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
- `Assets/Game/Scripts/Systems/RuntimeRootSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- Runtime root creation remains driven by `MatchBootstrapSystem`.
- Root names and hierarchy parenting remain unchanged.
- Cached root transform references remain available through `Ensure`.
- Managed Unity object ownership remains outside unmanaged ECS systems.

## Inventory Impact

- Total ECS declarations: `165`.
- Production `SystemBase`/legacy declarations: `28`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `83.0%`.
- Managed exceptions: `24`.
- Open rows: `3`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

No dedicated focused RuntimeRoot runner exists for this narrow managed root helper, so compile and the Phase 7 architecture guard are the applicable gates.

## Follow-Up

Continue remaining Integration split row:

- `P7-0374` `VisibleUnitSelectionSystem`

Agent B rows `P7-0003` and `P7-0019` remain held pending an explicit managed-reference boundary guardrail/model change.
