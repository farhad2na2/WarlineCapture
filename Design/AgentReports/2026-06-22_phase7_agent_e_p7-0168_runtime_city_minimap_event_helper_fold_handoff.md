# Phase 7 Agent E Handoff - P7-0168 Runtime City Minimap Event Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0168 RuntimeCityMinimapEventSystem`: folded from a disabled `SystemBase` wrapper into a plain runtime-city minimap event helper.

## Files Changed

- `Assets/Game/Scripts/Environment/RuntimeCityMinimapEventSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No runtime-city generation or minimap invalidation contract changed.
- Static minimap change publication, queued event state, UI-facing `Flush`, and `Clear` behavior stayed unchanged.
- `RuntimeCityCompositionSystem` now directly owns the plain minimap event helper instead of resolving it through `World.GetOrCreateSystemManaged`.
- This slice removes false disabled-`SystemBase` lifetime ownership only.

## Inventory Counts

- Total ECS declarations: `249`.
- Production `SystemBase`/legacy declarations: `115`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `53.8%`.
- Production non-UI rows: `241`.
- Production UI rows: `8`.
- Agent E rows: `67`.
- `DirectConvert` rows: `32`.
- Open rows: `93`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-minimap-event-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0168 RuntimeCityMinimapEventSystem` from the authoritative SystemBase inventory.
- Runtime-city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-minimap-event-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Note

- Production UI rows remain `8` because the existing `UiBuildDrawerReadModelSystem` UI `ISystem` row was already present in the dirty worktree and included by the latest inventory regeneration.

## Next Suggested Agent E Rows

- Continue with remaining low-risk direct-owned runtime-city helpers before larger managed GameObject split rows.
- Candidate rows include `P7-0173 RuntimeCityRoadCommitSystem`, `P7-0174 RuntimeCityRoadLayoutSystem`, and other no-Unity-object Agent E direct rows, subject to focused call-site inspection.
