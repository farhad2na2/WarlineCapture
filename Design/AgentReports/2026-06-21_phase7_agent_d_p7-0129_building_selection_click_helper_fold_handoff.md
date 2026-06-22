# Phase 7 Agent D Handoff - P7-0129 BuildingSelectionClickSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0129 BuildingSelectionClickSystem` from a disabled `SystemBase` wrapper into a plain selection-click helper.

The type did not own ECS scheduling or an independent ECS update lifetime. It is manually owned by the building composition/startup result path and exposes explicit delegate/context APIs for building selection click handling.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Building selection-click behavior remains delegate-driven through explicit context values.
- No runtime GameObject or Unity-object presentation ownership was added.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs`
  - Removed disabled `SystemBase` inheritance and empty lifecycle methods.
  - Removed the stale `Unity.Entities` using.
  - Kept pending path-job gating, grid lookup, screen-to-cell lookup, and cell-selection delegate behavior unchanged.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked P7-0129 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and validation status.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `332`.
- Production `SystemBase` / legacy declarations: `199`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `40.1%`.
- Production non-UI rows: `325`.
- Production UI rows: `7`.
- Agent D rows: `63`.
- Open rows: `177`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `48`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `14`.
- `SplitThenConvert`: `115`.
- `UIOutOfScope`: `7`.

## Validation

- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0 Warning(s)`, `0 Error(s)`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-selection-click-helper-fold-smoke.log`
  - Result marker: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed after the handoff was added.

## Next Candidate Guidance

Continue Agent D with another narrow row only. `BuildingPlacementInvalidCellSystem` looks like the next small candidate, but it owns cached prefix state and uses ECS buffers, so review ownership before folding or converting.
