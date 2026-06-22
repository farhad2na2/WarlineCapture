# Phase 7 Agent E Handoff - P7-0164 Runtime City Landmark Offset Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0164 RuntimeCityLandmarkOffsetSystem`: folded from a disabled `SystemBase` wrapper into a plain runtime-city landmark-offset helper.

## Files Changed

- `Assets/Game/Scripts/Environment/RuntimeCityLandmarkOffsetSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No runtime-city generation, placement, or spawn contract changed.
- Landmark offset arrays, offset ordering, hall-distance filtering, and `RuntimeCityLandmarkOffsetState` access stayed unchanged.
- `RuntimeCityCompositionSystem` now directly owns the plain landmark-offset helper instead of resolving it through `World.GetOrCreateSystemManaged`.
- This slice removes false disabled-`SystemBase` lifetime ownership only.

## Inventory Counts

- Total ECS declarations: `250`.
- Production `SystemBase`/legacy declarations: `117`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `53.2%`.
- Production non-UI rows: `243`.
- Production UI rows: `7`.
- Agent E rows: `69`.
- `DirectConvert` rows: `34`.
- Open rows: `95`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-landmark-offset-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0164 RuntimeCityLandmarkOffsetSystem` from the authoritative SystemBase inventory.
- Runtime-city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-landmark-offset-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Suggested Agent E Rows

- Continue with remaining low-risk direct-owned runtime-city helpers before larger managed GameObject split rows.
- Candidate rows include `P7-0163 RuntimeCityIngressSystem`, `P7-0168 RuntimeCityMinimapEventSystem`, and other no-Unity-object Agent E direct rows, subject to focused call-site inspection.
