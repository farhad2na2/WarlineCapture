# Phase 7 Integration Handoff - P7-0374 VisibleUnitSelectionSystem

Date: 2026-06-23
Lane: Integration
Rows: `P7-0374` `VisibleUnitSelectionSystem`, generated replacement row `P7-0384` `VisibleUnitSelectionCandidateSystem`
Disposition: `SplitThenConvert`
Result: Split unmanaged visible-unit candidate collection into `VisibleUnitSelectionCandidateSystem : ISystem`; folded the old disabled `SystemBase` wrapper into a direct managed screen-filter helper.

## Summary

`VisibleUnitSelectionSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. Existing selection call sites still use the direct helper API for `Camera.WorldToScreenPoint` and screen-rectangle filtering.

The ECS-side candidate collection now lives in `VisibleUnitSelectionCandidateSystem : ISystem`. It publishes `VisibleUnitSelectionCandidateElement` snapshots containing entity, world position, and vehicle classification. The candidate collector file has no managed camera/UI blockers, so the generated inventory classifies it as `Converted`.

## Files Changed

- `Assets/Game/Scripts/Systems/VisibleUnitSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs`
- `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs.meta`
- `Assets/Tests/Editor/SelectionStateCompositionSystemHelperTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- Existing rectangle selection call sites keep the same `VisibleUnitSelectionSystem` direct helper API.
- Camera projection and screen-rectangle filtering remain managed and outside the unmanaged `ISystem`.
- Player-faction filtering, building/static-blocker exclusion, and move-unit requirements remain unchanged.
- Vehicle/soldier classification still prefers `UnitSourcePrefabKey` prefixes before movement/footprint fallback.
- Selected-unit tag application remains a direct helper call used by selection request processing.

## Inventory Impact

- Total ECS declarations: `165`.
- Production `SystemBase`/legacy declarations: `27`.
- Production `ISystem` declarations: `138`.
- Current production `ISystem` share: `83.6%`.
- Managed exceptions: `24`.
- Open rows: `2` (`P7-0003`, `P7-0019`, both held Agent B RetireFold rows).

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionStateCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-visible-unit-selection-state.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-integration-visible-unit-selection-state.log`: `[SelectionStateFocusedValidation] result=Passed tests=8`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

Attempted broader selection command validation:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-visible-unit-selection-isystem.log
```

This runner exited non-zero before reaching the visible-selection fixture on `RtsSelectionInputSystemTests.RuntimeInput_DefersUnitSelectionUntilPointerRelease`, which asserted a pre-existing diagnostic log string. The direct slice validation above passed.

## Follow-Up

Only held Agent B RetireFold rows remain open:

- `P7-0003` `MatchSceneReferenceBoundarySystem`
- `P7-0019` `PerformanceDiagnosticsReferenceBoundarySystem`

Both remain held pending an explicit managed-reference boundary guardrail/model change; direct per-instance folding would break world-scoped sharing.
