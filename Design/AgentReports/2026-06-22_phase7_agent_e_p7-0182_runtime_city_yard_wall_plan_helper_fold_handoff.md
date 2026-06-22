# Phase 7 Agent E Handoff - P7-0182 RuntimeCityYardWallPlanSystem

Date: 2026-06-22

## Scope

Folded `P7-0182 RuntimeCityYardWallPlanSystem` from a disabled `SystemBase` wrapper into a plain runtime-city yard-wall plan helper.

## Code Changes

- Removed the unused `Unity.Entities` dependency and disabled ECS lifecycle from `Assets/Game/Scripts/Environment/RuntimeCityYardWallPlanSystem.cs`.
- Kept `RuntimeCityYardWallPlanState`, `HousePlan`, `CreateHousePlan`, and `TryFindYardRect` behavior unchanged.
- Updated `RuntimeCityCompositionSystem.ResolveRuntimeCityYardWallPlanSystem()` to directly construct the plain helper.

## Inventory

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Total ECS system declarations: `240`.
- Production `SystemBase`/legacy declarations: `106`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `55.8%`.
- Production non-UI rows: `232`.
- Production UI rows: `8`.
- Open rows: `84`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed; generated at `2026-06-22T11:33:41Z`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-yard-wall-plan-helper-fold-city.log`
  - Marker: `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with `P7-0190 CitizenPopulationDebugSystem`, the next open Agent E `DirectConvert` row in `Design/Architecture/systembase_to_isystem_inventory.md`.
