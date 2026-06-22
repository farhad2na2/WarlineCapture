# Phase 7 Agent E Handoff - P7-0171 RuntimeCityReadinessQuerySystem Helper Fold

Date: 2026-06-22

## Scope

- Lane: Agent E road/city/citizen.
- Inventory row: P7-0171 `RuntimeCityReadinessQuerySystem`.
- Files changed:
  - `Assets/Game/Scripts/Environment/RuntimeCityReadinessQuerySystem.cs`
  - `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
  - `Design/Architecture/systembase_to_isystem_inventory.md`
  - `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
  - `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Change Summary

- Folded `RuntimeCityReadinessQuerySystem` from a disabled `SystemBase` wrapper into a plain runtime-city readiness query helper.
- Preserved `TryGetGridConfig`, `TryGetGridData`, `HasPendingInitialUnitsSpawn`, `CollectInitialBaseExclusionRoadRects`, and `Clear`.
- Updated `RuntimeCityCompositionSystem` to instantiate the helper directly instead of resolving it through `World.GetOrCreateSystemManaged`.
- Moved live `EntityManager` access to `World.DefaultGameObjectInjectionWorld` inside the helper so the cached `EntityQuery` behavior remains explicit and disposable through `Clear()`.

## Inventory Delta

- Total ECS system declarations: 274.
- Production `SystemBase`/legacy declarations: 141.
- Production `ISystem` declarations: 133.
- Current production `ISystem` share: 48.5%.
- Production non-UI rows: 267.
- Production UI rows: 7.
- Agent E rows: 93.
- Dispositions: Converted 126, DirectConvert 41, ManagedPresentationSystemBaseException 22, RetireFold 10, SplitThenConvert 68, UIOutOfScope 7.
- Statuses: Converted 126, Deferred 7, ManagedException 22, Open 119.

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-readiness-query-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with 0 warnings and 0 errors.
- Runtime city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-readiness-query-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Follow-Up

- Continue Agent E with the next low-risk row from `Design/Architecture/systembase_to_isystem_inventory.md`.
- Keep the next validation batch lane-scoped; do not mix runtime city helper folds with road/citizen conversion batches.
