# Phase 7 Agent E Handoff - P7-0158 RuntimeCityEntryBuildingSpawnSystem

Date: 2026-06-22
Owner lane: Agent E road/city/citizen
Inventory row: P7-0158

## Scope

Folded `RuntimeCityEntryBuildingSpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime-city entry building spawn helper.

## Code Changes

- Removed the unused `SystemBase` inheritance, disabled `OnCreate`, empty `OnUpdate`, and `Unity.Entities` dependency from `Assets/Game/Scripts/Environment/RuntimeCityEntryBuildingSpawnSystem.cs`.
- Kept `RuntimeCityEntryBuildingSpawnState`, entry shop placement, entry house placement, plot/footprint reservations, and placement API behavior unchanged.
- Updated `RuntimeCityCompositionSystem.ResolveRuntimeCityEntryBuildingSpawnSystem()` to instantiate the helper directly instead of resolving a disabled managed ECS system.

## Inventory And Tracker Updates

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`.
- Updated `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`.
- Updated `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`.

Latest inventory:

- Total ECS system declarations: `243`.
- Production `SystemBase`/legacy declarations: `109`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `55.1%`.
- Production non-UI rows: `235`.
- Production UI rows: `8`.
- Open rows: `87`.

## Validation

Passed:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-entry-building-spawn-helper-fold-city.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-agent-e-runtime-city-entry-building-spawn-helper-fold-city.log`: `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Next Candidate

Continue Agent E with `P7-0175 RuntimeCityRoadsideBuildingSpawnSystem`, another direct-owned runtime-city building placement helper with no managed blockers in the current inventory row.
