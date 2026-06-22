# Phase 7 Agent E Handoff - P7-0194 CitizenPopulationLifecycleSystem

Date: 2026-06-22

## Scope

Folded `P7-0194 CitizenPopulationLifecycleSystem` from a disabled `SystemBase` wrapper into a plain citizen population lifecycle helper.

## Code Changes

- Removed the unused `Unity.Entities` dependency and disabled ECS lifecycle from `Assets/Game/Scripts/Systems/CitizenPopulationLifecycleSystem.cs`.
- Kept lifecycle `State`, reset behavior, update interval scheduling, path-job skip handling, visible sync, logical citizen update, and totals refresh behavior unchanged.
- Updated `CitizenPopulationCompositionSystem.ResolveCitizenPopulationLifecycleSystem()` to directly construct the plain helper.

## Inventory

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Total ECS system declarations: `237`.
- Production `SystemBase`/legacy declarations: `103`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `56.5%`.
- Production non-UI rows: `229`.
- Production UI rows: `8`.
- Open rows: `81`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-lifecycle-helper-fold-visible-unit.log`
  - Marker: `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-lifecycle-helper-fold-movement.log`
  - Marker: `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with `P7-0196 CitizenPopulationRuntimeUpdateSystem`, the next open Agent E `DirectConvert` row in `Design/Architecture/systembase_to_isystem_inventory.md`.
