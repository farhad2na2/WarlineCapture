# Phase 7 Agent D Handoff - P7-0126 BuildingRuntimeUpdateSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0126 BuildingRuntimeUpdateSystem` from a disabled `SystemBase` wrapper into a plain public runtime update dispatcher.

The type did not own ECS queries, scheduling, component state, or an ECS update path. It only exposed `UpdateStartup`, `Update`, and `UpdateSimulation` dispatcher methods used by the existing managed composition layer. The fold preserves the public API and keeps startup/simulation callback ordering unchanged.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Runtime ownership remains with the existing building composition path.
- Hot ECS simulation work remains outside this helper; this change only removes a disabled legacy ECS declaration.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs`
  - Removed `Unity.Entities` inheritance and disabled `SystemBase` boilerplate.
  - Kept `Context`, `UpdateStartup`, `Update`, and `UpdateSimulation` behavior intact.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked the P7-0126 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and current target.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `341`.
- Production `SystemBase` / legacy declarations: `208`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `39.0%`.
- Production non-UI rows: `334`.
- Production UI rows: `7`.
- Agent D rows: `72`.
- Open rows: `186`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `51`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `20`.
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

Continue Agent D with the next low-risk row before broad spawn/production owners. `P7-0092 BuildingPlacementRuntimeTickDiagnosticsSystem` looks like a likely direct-owned diagnostics helper, but it should be reviewed carefully because it has cooldown state and uses Unity runtime diagnostics APIs.
