# Phase 7 Agent E Handoff - P7-0187 Citizen Household Registration Helper Fold

Date: 2026-06-22

## Rows Completed

- `P7-0187 CitizenHouseholdRegistrationSystem`: folded from a disabled `SystemBase` wrapper into a plain citizen household registration helper.

## Files Changed

- `Assets/Game/Scripts/Systems/CitizenHouseholdRegistrationSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

## Contract Notes

- No ECS request/result contract changed.
- `CitizenHouseholdRegistrationSystem` keeps its helper API, delegate types, dwelling registration, and household assignment behavior.
- `CitizenPopulationCompositionSystem` now directly owns the plain helper instead of resolving it through `World.GetOrCreateSystemManaged`.
- The helper still uses managed citizen state and `UnityEngine.Time`; this slice only removes false ECS lifetime ownership and does not convert the household policy to unmanaged ECS data.

## Inventory Counts

- Total ECS declarations: `259`.
- Production `SystemBase`/legacy declarations: `126`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `51.4%`.
- Production non-UI rows: `252`.
- Production UI rows: `7`.
- Agent E rows: `78`.
- `SplitThenConvert` rows: `66`.
- Open rows: `104`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-household-registration-helper-fold-citizen.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
```

Results:

- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration removed `P7-0187 CitizenHouseholdRegistrationSystem` from the authoritative SystemBase inventory.
- Citizen focused validation passed: `/private/tmp/warline-phase7-agent-e-citizen-household-registration-helper-fold-citizen.log`, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Suggested Agent E Rows

- Continue with one remaining low-risk direct helper fold if available, otherwise move to the next split row from `Design/Architecture/systembase_to_isystem_inventory.md`.
- Remaining open Agent E SystemBase rows after this slice are `P7-0167`, `P7-0172`, `P7-0192`, `P7-0201`, `P7-0208`, `P7-0226`, `P7-0231`, and `P7-0235`.
