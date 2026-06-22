# Phase 7 Agent E Handoff - P7-0179 RuntimeCitySurfaceIntegrationSystem

Date: 2026-06-22
Owner lane: Agent E road/city/citizen
Inventory row: P7-0179

## Scope

Folded `RuntimeCitySurfaceIntegrationSystem` from a disabled `SystemBase` wrapper into a plain runtime-city surface integration helper.

## Code Changes

- Removed the unused `SystemBase` inheritance, disabled `OnCreate`, empty `OnUpdate`, `OnDestroy`, and `Unity.Entities` dependency from `Assets/Game/Scripts/Environment/RuntimeCitySurfaceIntegrationSystem.cs`.
- Kept surface configuration, clearing, building footprint center resolution, footprint reservation validation, road path surface validation, and primary surface sampling behavior unchanged.
- Updated `RuntimeCityVisualSystem.ResolveRuntimeCitySurfaceIntegrationSystem()` to instantiate the helper directly instead of resolving a disabled managed ECS system.
- Cleanup ownership remains explicit through `RuntimeCityVisualSystem.Dispose()` and `ClearSurface()`, both of which still call `RuntimeCitySurfaceIntegrationSystem.Clear()`.

## Inventory And Tracker Updates

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`.
- Updated `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`.
- Updated `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`.

Latest inventory:

- Total ECS system declarations: `241`.
- Production `SystemBase`/legacy declarations: `107`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `55.6%`.
- Production non-UI rows: `233`.
- Production UI rows: `8`.
- Open rows: `85`.

## Validation

Passed:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-surface-integration-helper-fold-city.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Markers:

- `/private/tmp/warline-phase7-agent-e-runtime-city-surface-integration-helper-fold-city.log`: `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`

## Next Candidate

Continue Agent E with `P7-0182 RuntimeCityYardWallPlanSystem`, the next lower-risk direct helper row with no managed blockers in the current inventory.
