# Phase 7 Agent D Handoff - P7-0085 BuildingPlacementInvalidCellSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0085 BuildingPlacementInvalidCellSystem` from a disabled `SystemBase` wrapper into a plain placement invalid-cell cache helper.

The type did not own ECS scheduling or an independent ECS update lifetime. It is manually owned by `BuildingGameplayCompositionSourceSystem` and stores cached placement-invalid prefix data used by runtime building placement and spawn validation paths.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Placement-invalid prefix state remains explicit helper-owned cache data.
- ECS buffer access remains explicit through method inputs and existing grid/query helpers.
- No runtime GameObject or Unity-object presentation ownership was added.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellSystem.cs`
  - Removed disabled `SystemBase` inheritance and empty lifecycle methods.
  - Kept cached prefix rebuild/clear, road-footprint mask use, placement validity, runtime blocker checks, and overlap validation behavior unchanged.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked P7-0085 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and validation status.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `331`.
- Production `SystemBase` / legacy declarations: `198`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `40.2%`.
- Production non-UI rows: `324`.
- Production UI rows: `7`.
- Agent D rows: `62`.
- Open rows: `176`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `47`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `14`.
- `SplitThenConvert`: `115`.
- `UIOutOfScope`: `7`.

## Validation

- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0 Warning(s)`, `0 Error(s)`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-invalid-cell-helper-fold-smoke.log`
  - Result marker: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed after the handoff was added.

## Next Candidate Guidance

Continue Agent D with another narrow row only. Re-review remaining direct candidates before touching them; some rows are stateful and may need an explicit data split before conversion rather than another helper fold.
