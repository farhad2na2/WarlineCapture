# Phase 7 Agent D Handoff - P7-0073 BuildingPlacementAdapterSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0073 BuildingPlacementAdapterSystem` from a disabled `SystemBase` wrapper into a plain placement adapter helper.

The type did not own ECS scheduling or an independent ECS update lifetime. It is an explicit delegate/adapter surface used by the existing building composition path for placement origin resolution, grid center origin resolution, placement validity, wall-gate alignment, and runtime spawn context bridging.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Placement adapter ownership remains with `BuildingGameplayCompositionSourceSystem` and explicit call sites in `BuildingGameplayCompositionSystem` / `BuildingRuntimeCompositionSystem`.
- ECS types remain only as explicit method/delegate inputs, not as a scheduled system lifetime.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingPlacementAdapterSystem.cs`
  - Removed disabled `SystemBase` boilerplate.
  - Kept delegates, placement origin resolution, center-screen origin, active placement validation, wall gate alignment, and placement validity behavior unchanged.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked P7-0073 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and current target.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `335`.
- Production `SystemBase` / legacy declarations: `202`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `39.7%`.
- Production non-UI rows: `328`.
- Production UI rows: `7`.
- Agent D rows: `66`.
- Open rows: `180`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `50`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `15`.
- `SplitThenConvert`: `115`.
- `UIOutOfScope`: `7`.

## Validation

- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0 Warning(s)`, `0 Error(s)`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-disposal-helper-fold-composition-smoke.log`
  - Result marker: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed after the handoff was added.

## Next Candidate Guidance

Only one open Agent D `RetireFold` row remains in the current inventory: `P7-0074 BuildingPlacementCommandCompositionSystem`. It has command context construction and should be reviewed carefully before folding.
