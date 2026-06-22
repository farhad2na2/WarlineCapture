# Phase 7 Agent E Handoff - P7-0193 CitizenPopulationEventSystem

Date: 2026-06-22

## Scope

Folded `P7-0193 CitizenPopulationEventSystem` from a disabled `SystemBase` wrapper into a plain citizen population event helper.

## Code Changes

- Removed the unused `Unity.Entities` dependency and disabled ECS lifecycle from `Assets/Game/Scripts/Systems/CitizenPopulationEventSystem.cs`.
- Kept `Init`, `Reset`, `NotifyVisibleCitizenDestroyed`, and `NotifyHomeBuildingDestroyed` behavior unchanged.
- Preserved refugee handoff delegates and travel-estimation callbacks through the existing citizen helper graph.
- Updated `CitizenPopulationCompositionSystem.ResolveCitizenPopulationEventSystem()` to directly construct the plain helper.

## Inventory

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Total ECS system declarations: `238`.
- Production `SystemBase`/legacy declarations: `104`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `56.3%`.
- Production non-UI rows: `230`.
- Production UI rows: `8`.
- Open rows: `82`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-event-helper-fold-visible-unit.log`
  - Marker: `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingCombatSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-event-helper-fold-building-combat.log`
  - Marker: `[BuildingCombatFocusedValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with `P7-0194 CitizenPopulationLifecycleSystem`, the next open Agent E `DirectConvert` row in `Design/Architecture/systembase_to_isystem_inventory.md`.
