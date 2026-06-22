# Phase 7 Agent E Handoff - P7-0201 Citizen Refugee Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0201 CitizenRefugeeSystem`: folded from a disabled `SystemBase` wrapper into a plain citizen refugee helper.

## Files Changed

- `Assets/Game/Scripts/Systems/CitizenRefugeeSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No ECS request/result contract changed.
- `CitizenRefugeeSystem` keeps the existing refugee API, delegate types, explicit `State` fallback path, displacement handling, tent assignment, refugee upkeep, and citizen death callbacks.
- `CitizenPopulationCompositionSystem` now directly owns the plain helper instead of resolving it through `World.GetOrCreateSystemManaged`.
- This slice does not convert refugee policy to unmanaged ECS processors; it removes false disabled-`SystemBase` lifetime ownership while preserving behavior.

## Inventory Counts

- Total ECS declarations: `257`.
- Production `SystemBase`/legacy declarations: `124`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `51.8%`.
- Production non-UI rows: `250`.
- Production UI rows: `7`.
- Agent E rows: `76`.
- `SplitThenConvert` rows: `64`.
- Open rows: `102`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-refugee-helper-fold-visible.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0201 CitizenRefugeeSystem` from the authoritative SystemBase inventory.
- Citizen visible-unit focused validation passed: `/private/tmp/warline-phase7-agent-e-citizen-refugee-helper-fold-visible.log`, marker `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Suggested Agent E Rows

- Remaining open Agent E SystemBase rows after this slice are `P7-0167`, `P7-0172`, `P7-0208`, `P7-0226`, `P7-0231`, and `P7-0235`.
- The remaining rows are road/runtime-city split candidates. Continue with one lane-scoped split and avoid mixing road and city validation batches.
