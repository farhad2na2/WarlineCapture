# Phase 7 Integration Handoff - P7-0315 ManagedGameplayStartupSystem

Date: 2026-06-22
Lane: Integration
Row: `P7-0315` `ManagedGameplayStartupSystem`
Disposition: `SplitThenConvert`
Result: Folded disabled `SystemBase` wrapper out of ECS into a plain direct-owned managed-startup helper.

## Summary

`ManagedGameplayStartupSystem` no longer derives from `SystemBase` and no longer declares disabled `OnCreate` / empty `OnUpdate` ECS lifecycle methods. `MatchBootstrapSystem` already owns this object directly with `new ManagedGameplayStartupSystem()`, so the slice preserves the existing managed startup composition path.

This slice intentionally did not add an `ISystem`: the class composes managed systems and Unity-object boundaries (`GameObject`, `Transform`, `Camera`, `Light`, `Volume`) and resolves the managed `DayNightSystem` boundary. There was no narrow unmanaged ECS processor to extract safely in this step.

## Files Changed

- `Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs`
- `Assets/Game/Scripts/Editor/ManagedGameplayStartupValidationRunner.cs`
- `Assets/Game/Scripts/Editor/ManagedGameplayStartupValidationRunner.cs.meta`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Behavior Preserved

- `Initialize(...)` remains the direct managed startup composition API.
- Road, building, selection, and citizen-population composition wiring remains unchanged.
- `DayNightSystem` managed boundary resolution remains unchanged.
- Game strings, faction visuals, building selection, UI query/command, and runtime update contexts remain passed through the same result contract.

## Inventory Impact

- Total ECS system declarations: `169`.
- Production `SystemBase`/legacy declarations: `32`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `81.1%`.
- Managed exceptions: `24`.
- Open rows: `7`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ManagedGameplayStartupValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-managed-gameplay-startup-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-integration-managed-gameplay-startup-helper-fold.log`: `[ManagedGameplayStartupValidation] result=Passed tests=1`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Notes

`ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation` was attempted first because it includes `ManagedGameplayStartupSystem.cs`, but it failed on pre-existing UI Toolkit architecture debt:

`Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs:1809 uses HierarchyFind: Transform existing = transform.Find(ExternalMenuBackgroundName);`

That UI Toolkit/Canvas migration area is explicitly out of scope for this Phase 7 slice, so the deterministic source-contract runner above was used as the focused validation.

## Follow-Up

Continue remaining Integration split-first rows. `P7-0003` and `P7-0019` remain held pending an explicit managed-reference boundary guardrail/model change.
