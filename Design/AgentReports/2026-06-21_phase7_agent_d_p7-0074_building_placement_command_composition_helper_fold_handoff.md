# Phase 7 Agent D Handoff - P7-0074 BuildingPlacementCommandCompositionSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0074 BuildingPlacementCommandCompositionSystem` from a disabled `SystemBase` wrapper into a plain placement command-context helper.

The type did not own ECS scheduling or an independent ECS update lifetime. It is manually owned by `BuildingGameplayCompositionSourceSystem` and builds explicit `BuildingPlacementCommandSystem.Context` / `BuildingPlacementContextSystem.Source` values from existing placement, visual, runtime, resource, minimap, and selection dependencies.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Command-context construction remains owned by the existing building gameplay composition path.
- ECS-specific work stays in the existing command, placement, runtime, and validation systems; this helper only wires delegates and context values.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingPlacementCommandCompositionSystem.cs`
  - Removed disabled `SystemBase` inheritance and empty lifecycle methods.
  - Removed the stale `Unity.Entities` using.
  - Kept command context construction, placement visual delegate wiring, build purchase callbacks, minimap refresh, build command mode callbacks, and selection clearing behavior unchanged.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked P7-0074 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and validation status.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `334`.
- Production `SystemBase` / legacy declarations: `201`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `39.8%`.
- Production non-UI rows: `327`.
- Production UI rows: `7`.
- Agent D rows: `65`.
- Open rows: `179`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `50`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `14`.
- `SplitThenConvert`: `115`.
- `UIOutOfScope`: `7`.

## Validation

- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0 Warning(s)`, `0 Error(s)`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-command-composition-fold-smoke.log`
  - Result marker: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed after the handoff was added.

## Next Candidate Guidance

The current inventory no longer has obvious open Agent D `RetireFold` rows. Continue Agent D by selecting a narrow `DirectConvert` or a carefully split building row. Avoid broad spawn/production owners until their pure ECS request/data responsibilities are isolated.
