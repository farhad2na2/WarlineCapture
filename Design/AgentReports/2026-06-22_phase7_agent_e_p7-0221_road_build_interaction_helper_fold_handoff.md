# Phase 7 Agent E Handoff - P7-0221 RoadBuildInteractionSystem

Date: 2026-06-22
Lane: Agent E road/city/citizen
Inventory row: P7-0221 RoadBuildInteractionSystem
Disposition: SplitThenConvert helper fold

## Scope

Folded `RoadBuildInteractionSystem` from a disabled `SystemBase` wrapper into a plain road build interaction helper. The slice stayed lane-scoped to road building interaction behavior and inventory/tracker accounting.

## Code Changes

- Removed the unused ECS `SystemBase` inheritance, disabled `OnCreate`, and empty `OnUpdate` from `Assets/Game/Scripts/Systems/RoadBuildInteractionSystem.cs`.
- Kept `Unity.Entities` because the helper still uses `Entity` and `EntityManager` for building blocker/combat entity cleanup.
- Preserved building placement commit, selection hit tests, building selection, deletion, ECS entity cleanup, runtime-grid blocker cleanup, and road composition callers.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 trackers with validation commands, log paths, and current inventory counts.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, 0 warnings, 0 errors.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `git diff --check`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-interaction-helper-fold-road-build-command.log`
  - Result: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result: passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS system declarations: 200.
- Production non-UI rows: 192.
- Production UI rows: 8.
- Production SystemBase/legacy declarations: 66.
- Production ISystem declarations: 134.
- Current production ISystem share: 67.0%.

## Follow-Up

Continue Agent E with the remaining split/managed-exception candidates. This helper fold reduced the production SystemBase/legacy denominator; it did not add a new `ISystem` because the retired wrapper had no independent ECS update responsibility.
