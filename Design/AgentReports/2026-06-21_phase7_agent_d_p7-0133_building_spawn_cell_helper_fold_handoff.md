# Phase 7 Agent D Handoff - P7-0133 BuildingSpawnCellSystem

Date: 2026-06-21
Lane: Agent D - Building / Production
Tracker: `Design/Architecture/phase7_agent_d_building_production_tracker.md`
Inventory: `Design/Architecture/systembase_to_isystem_inventory.md`

## Scope

Folded `P7-0133 BuildingSpawnCellSystem` from a disabled `SystemBase` wrapper into a plain spawn-cell helper.

The type did not own ECS scheduling or an independent ECS update lifetime. It contains pure spawn-cell utility logic over `GridConfig`, `NativeArray<GridWalkable>`, `NativeBitArray` blocker/occupied/reserved sets, and `Unity.Mathematics.Random`.

## Architecture Contract

- No new manager/controller/facade was introduced.
- No new updating `MonoBehaviour` loop was introduced.
- No broad replacement `ISystem` shell was introduced.
- No UI Toolkit or Canvas migration was touched.
- Spawn-cell calculation remains a direct helper API with explicit Native container inputs.
- No runtime GameObject or Unity-object presentation ownership was added.

## Files Changed

- `Assets/Game/Scripts/Systems/BuildingSpawnCellSystem.cs`
  - Removed disabled `SystemBase` inheritance and empty lifecycle methods.
  - Removed the stale `Unity.Entities` using.
  - Kept perimeter candidate filtering, reservation, random candidate selection, fallback spawn-cell search, and NativeArray/NativeBitArray behavior unchanged.

- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated authoritative Phase 7 inventory after the fold.

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
  - Marked P7-0133 helper fold complete.

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated main Phase 7 accounting and validation status.

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
  - Updated project-wide Phase 7 snapshot and current target.

## Inventory Impact

- Total ECS system declarations: `333`.
- Production `SystemBase` / legacy declarations: `200`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `39.9%`.
- Production non-UI rows: `326`.
- Production UI rows: `7`.
- Agent D rows: `64`.
- Open rows: `178`.

Disposition snapshot:

- `Converted`: `126`.
- `DirectConvert`: `49`.
- `ManagedPresentationSystemBaseException`: `22`.
- `RetireFold`: `14`.
- `SplitThenConvert`: `115`.
- `UIOutOfScope`: `7`.

## Validation

- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0 Warning(s)`, `0 Error(s)`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-spawn-cell-helper-fold-smoke.log`
  - Result marker: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed after the handoff was added.

## Next Candidate Guidance

Continue Agent D with another narrow building row only. The next safest candidates appear to be disabled helper-style direct candidates such as placement invalid-cell or selection-click logic, but each should be reviewed for state ownership before folding or conversion.
