# Phase 7 Agent E Handoff - P7-0218 RoadBuildEcsBoundarySystem

Date: 2026-06-22
Lane: Agent E road/city/citizen
Inventory row: P7-0218 RoadBuildEcsBoundarySystem
Disposition: SplitThenConvert helper fold

## Scope

Folded `RoadBuildEcsBoundarySystem` from a disabled `SystemBase` wrapper into a plain road build ECS boundary helper. The slice stayed lane-scoped to road/building ECS boundary helper behavior and inventory/tracker accounting.

## Code Changes

- Removed the unused ECS `SystemBase` inheritance, disabled `OnCreate`, and empty `OnUpdate` from `Assets/Game/Scripts/Systems/RoadBuildEcsBoundarySystem.cs`.
- Preserved entity-manager resolution, blocker entity creation, building combat entity creation, runtime link attachment, player unit spawn near buildings, runtime building disposal, and road composition callers.
- Kept ECS and Unity imports required by the helper methods; this was not converted into a new updating ECS system.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 trackers with validation commands, log paths, and current inventory counts.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, 0 warnings, 0 errors.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `git diff --check`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-ecs-boundary-helper-fold-road-build-command.log`
  - Result: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result: passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS system declarations: 199.
- Production non-UI rows: 191.
- Production UI rows: 8.
- Production SystemBase/legacy declarations: 65.
- Production ISystem declarations: 134.
- Current production ISystem share: 67.3%.

## Follow-Up

Continue Agent E with the remaining split/managed-exception candidates. This helper fold reduced the production SystemBase/legacy denominator; it did not add a new `ISystem` because the retired wrapper had no independent ECS update responsibility.
