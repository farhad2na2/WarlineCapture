# Phase 7 Agent D Handoff - P7-0108 BuildingRuntimeBoundaryPublishSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0108 BuildingRuntimeBoundaryPublishSystem` from a disabled `SystemBase` wrapper into a plain runtime boundary publish helper.

The type did not own an ECS update lifetime. Its behavior is manually invoked through `Update(Context)`, where the existing runtime tick path supplies entity-manager access, query access, runtime building data, and the boundary system dependencies. This slice removed only the disabled ECS wrapper.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Runtime boundary publish ownership remains with the existing building runtime tick composition path.
- ECS access remains explicit through `TryGetEntityManager`, `EnsureEntityQueries`, and the supplied boundary query callback.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs`
  - Removed disabled `SystemBase` boilerplate.
  - Kept `TryGetEntityManagerDelegate`, `Context`, `Update`, entity-query acquisition, boundary update dispatch, runtime building dictionary usage, and frame/time arguments unchanged.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked the P7-0108 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and current target.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `336`.
- Production `SystemBase` / legacy declarations: `203`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `39.6%`.
- Production non-UI rows: `329`.
- Production UI rows: `7`.
- Agent D rows: `67`.
- Open rows: `181`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `50`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `16`.
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

Only two open Agent D `RetireFold` rows remain in the current inventory: `P7-0073 BuildingPlacementAdapterSystem` and `P7-0074 BuildingPlacementCommandCompositionSystem`. Both are adapter/command surfaces and should be reviewed carefully before folding because they sit closer to placement command behavior.
