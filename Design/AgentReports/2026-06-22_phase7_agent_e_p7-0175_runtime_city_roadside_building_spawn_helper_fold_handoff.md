# Phase 7 Agent E Handoff - P7-0175 RuntimeCityRoadsideBuildingSpawnSystem

Date: 2026-06-22
Owner lane: Agent E road/city/citizen
Inventory row: P7-0175

## Scope

Folded `RuntimeCityRoadsideBuildingSpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime-city roadside building spawn helper.

## Code Changes

- Removed the unused `SystemBase` inheritance, disabled `OnCreate`, empty `OnUpdate`, and `Unity.Entities` dependency from `Assets/Game/Scripts/Environment/RuntimeCityRoadsideBuildingSpawnSystem.cs`.
- Kept `RuntimeCityRoadsideBuildingSpawnSystem.Plan`, `RuntimeCityRoadsideBuildingSpawnState`, roadside plan creation, central shop placement, gas station placement, outer shop placement, roadside house placement, and placement API behavior unchanged.
- Updated `RuntimeCityCompositionSystem.ResolveRuntimeCityRoadsideBuildingSpawnSystem()` to instantiate the helper directly instead of resolving a disabled managed ECS system.

## Inventory And Tracker Updates

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`.
- Updated `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`.
- Updated `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`.

Latest inventory:

- Total ECS system declarations: `242`.
- Production `SystemBase`/legacy declarations: `108`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `55.4%`.
- Production non-UI rows: `234`.
- Production UI rows: `8`.
- Open rows: `86`.

## Validation

Passed:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-roadside-building-spawn-helper-fold-city.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-agent-e-runtime-city-roadside-building-spawn-helper-fold-city.log`: `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Next Candidate

Continue Agent E with `P7-0179 RuntimeCitySurfaceIntegrationSystem`, the next lower-risk direct helper row with no managed blockers in the current inventory.
