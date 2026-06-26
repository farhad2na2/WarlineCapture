# Phase 7 Integration Handoff - P7-0319 MatchHudSquadTraySelectionSystem

Date: 2026-06-23
Lane: Integration
Row: `P7-0319` `MatchHudSquadTraySelectionSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS into a plain direct-owned squad-tray selection helper.

## Summary

`MatchHudSquadTraySelectionSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. `SelectionGameplayStartupSystem` already owns this object directly with `new MatchHudSquadTraySelectionSystem()`, so this slice preserves the existing UI-driven squad tray selection path.

This slice intentionally did not add an `ISystem`: the helper still has a managed camera/UI edge through `Camera` and `IMatchHudSquadTrayView`, and it is invoked directly from UI binding rather than an ECS update loop. No broad replacement shell was introduced.

## Files Changed

- `Assets/Game/Scripts/Systems/MatchHudSquadTraySelectionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- `SelectSlot(...)` remains the direct squad tray click-selection API.
- Camera-centered ranking, viewport prioritization, and soldier-cluster selection behavior remain unchanged.
- `SelectionStateCompositionSystemHelper` cache writes and `FocusedUnitLifecycleSystem` focus application remain unchanged.
- The HUD view still receives selected-slot, disabled-slot, and portrait state through the same interface.

## Inventory Impact

- Total ECS declarations: `167`.
- Production `SystemBase`/legacy declarations: `30`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `82.0%`.
- Managed exceptions: `24`.
- Open rows: `5`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudSquadTraySelectionSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-match-hud-squad-tray-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-integration-match-hud-squad-tray-helper-fold.log`: `[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Follow-Up

Continue remaining Integration split rows:

- `P7-0323` `ResourceHaulerSystem`
- `P7-0325` `RuntimeRootSystem`
- `P7-0374` `VisibleUnitSelectionSystem`

Agent B rows `P7-0003` and `P7-0019` remain held pending an explicit managed-reference boundary guardrail/model change.
