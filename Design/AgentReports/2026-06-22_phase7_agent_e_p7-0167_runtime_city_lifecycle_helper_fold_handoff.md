# Phase 7 Agent E Handoff - P7-0167 Runtime City Lifecycle Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0167 RuntimeCityLifecycleSystem`: folded from a disabled `SystemBase` wrapper into a plain runtime-city lifecycle helper.

## Files Changed

- `Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No ECS request/result contract changed.
- `RuntimeCityLifecycleSystem` keeps the existing lifecycle API, `Context`, generation routine ownership, spawned/generating flags, yield cadence, and diagnostic callbacks.
- `RuntimeCityCompositionSystem` now directly owns the plain lifecycle helper instead of resolving it through `World.GetOrCreateSystemManaged`.
- This slice does not convert runtime-city coroutine lifecycle ownership to unmanaged ECS data; it removes false disabled-`SystemBase` lifetime ownership while preserving behavior.

## Inventory Counts

- Total ECS declarations: `256`.
- Production `SystemBase`/legacy declarations: `123`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `52.0%`.
- Production non-UI rows: `249`.
- Production UI rows: `7`.
- Agent E rows: `75`.
- `SplitThenConvert` rows: `63`.
- Open rows: `101`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-lifecycle-helper-fold-generation.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0167 RuntimeCityLifecycleSystem` from the authoritative SystemBase inventory.
- Runtime-city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-lifecycle-helper-fold-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Suggested Agent E Rows

- Remaining open Agent E SystemBase rows after this slice are `P7-0172`, `P7-0208`, `P7-0226`, `P7-0231`, and `P7-0235`.
- The next narrow fold candidate is likely `P7-0172 RuntimeCityRoadBuildBridgeSystem`; validate with runtime-city generation plus road-build focused coverage if touched.
