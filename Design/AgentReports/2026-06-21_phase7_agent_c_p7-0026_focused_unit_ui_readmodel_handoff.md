# Phase 7 Agent C Handoff - P7-0026 FocusedUnitUiReadModelSystem

## Scope

- Lane: `AgentC` selection, commands, focus, and player intent.
- Inventory row: `P7-0026 FocusedUnitUiReadModelSystem`.
- Result: retired/folded from disabled `SystemBase` into a plain focused-unit UI read-model helper.

## Changes

- Removed `SystemBase` inheritance and empty ECS lifecycle methods from `FocusedUnitUiReadModelSystem`.
- Preserved the public read-model contract:
  - `Publish`
  - `TryRead`
- Preserved focused-unit read-model publication for:
  - focused entity;
  - ownership and vehicle flags;
  - attack/hold/stop/scan availability and disabled reason codes;
  - health text and numeric health;
  - transport capacity state;
  - passenger list buffer;
  - world and portrait pose.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated the Agent C tracker, Agent A tracker, and ECS architecture tracker with the new inventory counts and validation logs.

## Architecture Notes

- This type has no independent ECS update lifetime and is manually constructed by selection/HUD command boundaries.
- It remains a managed helper because it owns a managed scratch list and read-model publication API used by UI-facing selection code.
- Folding it out of ECS removes a disabled `SystemBase` shell without introducing a broad replacement `ISystem`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0` warnings, `0` errors.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-ui-readmodel-selection.log`
  - Result marker: `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-focused-unit-ui-readmodel-transport.log`
  - Result marker: `[UnitTransportValidation] result=Passed tests=73`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed.

## Follow-Up

- Continue Agent C with another small facade/helper if one remains, otherwise move into explicit request/result boundary work for the broader selection systems.
