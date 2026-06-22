# Phase 7 Agent E Handoff - P7-0192 Citizen ECS Projection Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0192 CitizenPopulationEcsProjectionSystem`: folded from a disabled `SystemBase` wrapper into a plain citizen ECS projection helper.

## Files Changed

- `Assets/Game/Scripts/Systems/CitizenPopulationEcsProjectionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No ECS request/result contract changed.
- `CitizenPopulationEcsProjectionSystem` keeps the existing API for entity-manager resolution, citizen/household entity creation, summary publication, entity counts, grid-config reads, and cleanup.
- Existing direct construction call sites are preserved.
- This slice does not move the managed citizen population store into unmanaged ECS data; it only removes false `SystemBase` lifetime ownership from the projection helper.

## Inventory Counts

- Total ECS declarations: `258`.
- Production `SystemBase`/legacy declarations: `125`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `51.6%`.
- Production non-UI rows: `251`.
- Production UI rows: `7`.
- Agent E rows: `77`.
- `SplitThenConvert` rows: `65`.
- Open rows: `103`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-ecs-projection-helper-fold-visible.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0192 CitizenPopulationEcsProjectionSystem` from the authoritative SystemBase inventory.
- Citizen visible-unit focused validation passed: `/private/tmp/warline-phase7-agent-e-citizen-ecs-projection-helper-fold-visible.log`, marker `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Suggested Agent E Rows

- Remaining open Agent E SystemBase rows after this slice are `P7-0167`, `P7-0172`, `P7-0201`, `P7-0208`, `P7-0226`, `P7-0231`, and `P7-0235`.
- The remaining rows are broader split candidates. Continue with one lane-scoped split and avoid mixing citizen, road, and city validation batches.
