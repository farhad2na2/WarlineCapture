# Phase 7 Agent E Handoff - P7-0231 Road Network Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0231 RoadNetworkSystem`: folded from a disabled `SystemBase` wrapper into a plain road network graph helper.

## Files Changed

- `Assets/Game/Scripts/Systems/RoadNetworkSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No ECS request/result contract changed.
- `RoadNetworkSystem` keeps the existing road visual types, tile connection masks, edge keys, stroke data, tile data, graph dictionaries, special road metadata, snapshot/restore behavior, stroke create/delete APIs, and adjacency/mask helpers.
- Existing road mutation, visual, preview, path-planning, grid projection, input, session, and composition callers still use the same direct helper API.
- This slice removes false disabled-`SystemBase` lifetime ownership only; road command queue processing and runtime generation request splitting remain assigned to later Agent E rows.

## Inventory Counts

- Total ECS declarations: `253`.
- Production `SystemBase`/legacy declarations: `120`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `52.6%`.
- Production non-UI rows: `246`.
- Production UI rows: `7`.
- Agent E rows: `72`.
- `SplitThenConvert` rows: `60`.
- Open rows: `98`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-network-helper-fold-roadbuild.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0231 RoadNetworkSystem` from the authoritative SystemBase inventory.
- Road-build command validation passed: `/private/tmp/warline-phase7-agent-e-road-network-helper-fold-roadbuild.log`, marker `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Suggested Agent E Rows

- Remaining open Agent E SystemBase rows after this slice are `P7-0208` and `P7-0235`.
- `P7-0208 RoadBuildCommandSystem` has real `EntityManager` and queue-buffer processing, so it should be handled as a request processor conversion/split rather than a simple helper fold.
