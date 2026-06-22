# Phase 7 Agent E Handoff - P7-0196 CitizenPopulationRuntimeUpdateSystem

Date: 2026-06-22

## Scope

Folded `P7-0196 CitizenPopulationRuntimeUpdateSystem` from a disabled `SystemBase` wrapper into a plain citizen population runtime update helper.

## Code Changes

- Removed unused ECS/Burst-related usings and the disabled ECS lifecycle from `Assets/Game/Scripts/Systems/CitizenPopulationRuntimeUpdateSystem.cs`.
- Kept runtime bind/reset, citizen lifecycle update orchestration, logical citizen update, visible citizen sync, household/citizen storage, death handling, totals refresh, and movement request processing behavior unchanged.
- No new manager/controller/facade or updating MonoBehaviour loop was introduced.

## Inventory

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Total ECS system declarations: `236`.
- Production `SystemBase`/legacy declarations: `102`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `56.8%`.
- Production non-UI rows: `228`.
- Production UI rows: `8`.
- Open rows: `80`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-runtime-update-helper-fold-visible-unit.log`
  - Marker: `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-runtime-update-helper-fold-movement.log`
  - Marker: `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with `P7-0198 CitizenPopulationTotalsSystem`, the next open Agent E `DirectConvert` row in `Design/Architecture/systembase_to_isystem_inventory.md`.
