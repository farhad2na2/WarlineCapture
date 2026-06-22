# Phase 7 Agent E Handoff - P7-0149 RuntimeCityBulkPlotPlanSystem Helper Fold

Date: 2026-06-22

## Scope

Folded `P7-0149 RuntimeCityBulkPlotPlanSystem` from a disabled `SystemBase` wrapper into a plain Agent E runtime-city bulk plot plan helper.

## Code Changes

- `Assets/Game/Scripts/Environment/RuntimeCityBulkPlotPlanSystem.cs`
  - Removed `SystemBase` inheritance and empty disabled ECS lifecycle methods.
  - Kept `RuntimeCityBulkPlotPlanState`, `Plan`, central/outer/entry plot collection, and prefab-selection shuffle behavior unchanged.
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
  - Replaced `GetOrCreateSystemManaged<RuntimeCityBulkPlotPlanSystem>()` with direct helper construction.

## Architecture Notes

- This row was inventoried as `DirectConvert`, but the implementation had no active ECS update responsibility. It was a disabled wrapper around a plain state object, so folding it out of ECS reduces the denominator without creating a broad replacement `ISystem`.
- No Unity object presentation, UI, prefab, camera, scene, or visual ownership was introduced.
- Bulk building spawn routines still consume the same plot plan API through runtime-city composition state.

## Inventory After Regeneration

- Total ECS system declarations: `246`
- Production `SystemBase`/legacy declarations: `112`
- Production `ISystem` declarations: `134`
- Current production `ISystem` share: `54.5%`
- Production non-UI rows: `238`
- Production UI rows: `8`
- Open rows: `90`

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-bulk-plot-plan-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
git diff --check
```

Results:

- Editor compile passed with `0 Warning(s), 0 Error(s)`.
- Runtime city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-bulk-plot-plan-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with `P7-0150 RuntimeCityChainSystem` or inspect the remaining open runtime-city direct-convert rows before choosing the next narrow slice.
