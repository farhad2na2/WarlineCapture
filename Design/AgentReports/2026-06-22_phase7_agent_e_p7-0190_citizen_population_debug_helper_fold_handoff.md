# Phase 7 Agent E Handoff - P7-0190 CitizenPopulationDebugSystem

Date: 2026-06-22

## Scope

Folded `P7-0190 CitizenPopulationDebugSystem` from a disabled `SystemBase` wrapper into a plain citizen debug helper.

## Code Changes

- Removed the disabled ECS lifecycle shell from `Assets/Game/Scripts/Systems/CitizenPopulationDebugSystem.cs`.
- Kept `KillCitizenAction`, `TryGetCitizenDebugSnapshot`, `TrySetCitizenStatusForDebug`, and `TryKillCitizenForDebug` behavior unchanged.
- Preserved ECS projection reads through `CitizenPopulationEcsProjectionSystem`; no new managed runtime update path was introduced.
- Updated `CitizenPopulationCompositionSystem.ResolveCitizenPopulationDebugSystem()` to directly construct the plain helper.

## Inventory

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Total ECS system declarations: `239`.
- Production `SystemBase`/legacy declarations: `105`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `56.1%`.
- Production non-UI rows: `231`.
- Production UI rows: `8`.
- Open rows: `83`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-debug-helper-fold-visible-unit.log`
  - Marker: `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-debug-helper-fold-movement.log`
  - Marker: `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with `P7-0193 CitizenPopulationEventSystem`, the next open Agent E `DirectConvert` row in `Design/Architecture/systembase_to_isystem_inventory.md`.
