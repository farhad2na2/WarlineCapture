# Phase 7 Agent E Handoff - P7-0157 RuntimeCityDiagnosticSystem Helper Fold

Date: 2026-06-22

## Scope

- Lane: Agent E road/city/citizen.
- Inventory row: P7-0157 `RuntimeCityDiagnosticSystem`.
- Files changed:
  - `Assets/Game/Scripts/Environment/RuntimeCityDiagnosticSystem.cs`
  - `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
  - `Design/Architecture/systembase_to_isystem_inventory.md`
  - `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
  - `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Change Summary

- Folded `RuntimeCityDiagnosticSystem` from a disabled `SystemBase` wrapper into a plain direct-owned runtime-city diagnostic helper.
- Removed the empty ECS lifecycle from the diagnostic helper.
- Updated `RuntimeCityCompositionSystem` to instantiate the diagnostic helper directly instead of resolving it through `World.GetOrCreateSystemManaged`.
- Preserved the existing diagnostic logging methods and all existing runtime city callers.

## Inventory Delta

- Total ECS system declarations: 276.
- Production `SystemBase`/legacy declarations: 143.
- Production `ISystem` declarations: 133.
- Current production `ISystem` share: 48.2%.
- Production non-UI rows: 269.
- Production UI rows: 7.
- Agent E rows: 95.
- Dispositions: Converted 126, DirectConvert 41, ManagedPresentationSystemBaseException 22, RetireFold 12, SplitThenConvert 68, UIOutOfScope 7.
- Statuses: Converted 126, Deferred 7, ManagedException 22, Open 121.

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-diagnostic-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with 0 warnings and 0 errors.
- Runtime city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-diagnostic-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Follow-Up

- Continue Agent E with the next low-risk `RetireFold` row from `Design/Architecture/systembase_to_isystem_inventory.md`.
- Do not mix this helper fold with road/citizen conversion batches; keep the next validation batch lane-scoped.
