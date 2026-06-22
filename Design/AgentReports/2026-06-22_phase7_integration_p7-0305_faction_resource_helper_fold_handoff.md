# Phase 7 Integration Handoff - P7-0305 FactionResourceSystem

Date: 2026-06-22
Lane: Integration
Row: `P7-0305` `FactionResourceSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS into a plain direct-owned resource helper.

## Summary

`FactionResourceSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. The existing managed helper API remains intact for building production, runtime boundary, resource hauler, and focused test call sites that already construct `new FactionResourceSystem()`.

This slice intentionally did not add an `ISystem`: the row contained managed runtime-building dictionary/resource math and public read-model helpers, not ECS update work. Adding a broad shell would inflate `ISystem` count without moving hot gameplay behavior into jobs.

## Files Changed

- `Assets/Game/Scripts/Systems/FactionResourceSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- Resource totals still count storage buildings only.
- Faction economy snapshots still sum storage and production rates by faction.
- Faction resource sale/drain logic still drains across eligible owned buildings.
- Primary/fuel capacity helpers still derive UI/read-model capacity values.
- Resource production still extracts oil up to capacity and converts oil into fuel.

## Inventory Impact

- Total ECS system declarations: `171`.
- Production `SystemBase`/legacy declarations: `34`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `80.1%`.
- Managed exceptions: `24`.
- Open rows: `9`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FactionResourceSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-faction-resource-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-integration-faction-resource-helper-fold.log`: `[FactionResourceFocusedValidation] result=Passed tests=6`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Follow-Up

Continue remaining Integration split-first rows. `P7-0003` and `P7-0019` remain held pending an explicit managed-reference boundary guardrail/model change.
