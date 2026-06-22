# Phase 7 Agent E Handoff - P7-0191 CitizenPopulationDiagnosticSystem Helper Fold

Date: 2026-06-22

## Scope

- Lane: Agent E road/city/citizen.
- Inventory row: P7-0191 `CitizenPopulationDiagnosticSystem`.
- Files changed:
  - `Assets/Game/Scripts/Systems/CitizenPopulationDiagnosticSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
  - `Design/Architecture/systembase_to_isystem_inventory.md`
  - `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
  - `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Change Summary

- Folded `CitizenPopulationDiagnosticSystem` from a disabled `SystemBase` wrapper into a plain citizen diagnostics helper.
- Preserved the `FrameTimings` struct, static timing helpers, instance timing helpers, and diagnostic log behavior.
- Updated `CitizenPopulationCompositionSystem.Result` to instantiate the diagnostics helper directly instead of resolving it through `World.GetOrCreateSystemManaged`.
- Kept citizen lifecycle callers unchanged.

## Inventory Delta

- Total ECS system declarations: 273.
- Production `SystemBase`/legacy declarations: 140.
- Production `ISystem` declarations: 133.
- Current production `ISystem` share: 48.7%.
- Production non-UI rows: 266.
- Production UI rows: 7.
- Agent E rows: 92.
- Dispositions: Converted 126, DirectConvert 41, ManagedPresentationSystemBaseException 22, RetireFold 9, SplitThenConvert 68, UIOutOfScope 7.
- Statuses: Converted 126, Deferred 7, ManagedException 22, Open 118.

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-diagnostic-helper-fold-citizen.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with 0 warnings and 0 errors.
- Citizen focused validation passed: `/private/tmp/warline-phase7-agent-e-citizen-diagnostic-helper-fold-citizen.log`, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Follow-Up

- Continue Agent E with the next low-risk row from `Design/Architecture/systembase_to_isystem_inventory.md`.
- Keep future citizen slices separate from road/runtime-city slices unless a shared request/result contract requires explicit coordination.
