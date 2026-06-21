# Phase 7 Agent C Handoff - P7-0040 RtsSelectionRuntimeInputSystem

## Scope

- Lane: `AgentC` selection, commands, focus, and player intent.
- Inventory row: `P7-0040 RtsSelectionRuntimeInputSystem`.
- Result: retired/folded from disabled `SystemBase` into a plain runtime pointer/input helper.

## Changes

- Removed `[DisableAutoCreation]`, `SystemBase` inheritance, and empty ECS lifecycle methods from `RtsSelectionRuntimeInputSystem`.
- Preserved the public `Context` contract and runtime methods:
  - `ProcessQueuedMoveOrder`
  - `UpdateNormalPointerInput`
- Preserved existing pointer input behavior for:
  - move, attack, scan, and board target command modes;
  - command-mode camera panning;
  - selection rectangle updates;
  - persistent move-target double-click handling;
  - transport board passenger drag handling;
  - click diagnostics and command-mode cleanup.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated the Agent C tracker, Agent A tracker, and ECS architecture tracker with the new inventory counts and validation logs.

## Architecture Notes

- This type is not a scheduled ECS processor. It is manually constructed by `SelectionGameplayStartupSystem`.
- The helper reads managed input/time and calls managed delegates for camera, UI, and pointer/click behavior, so converting it to an unmanaged `ISystem` would violate the Phase 7 architecture contract.
- Folding it out of the ECS system denominator removes a broad disabled `SystemBase` shell while preserving behavior.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0` warnings, `0` errors.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-rts-selection-runtime-input.log`
  - Result marker: `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed.

## Follow-Up

- Continue Agent C with the next small selection/command helper.
- Leave broad systems such as `RtsSelectionCommandResultFlushSystem`, `RtsSelectionInputSystem`, and focus/selection state systems for explicit request/result boundary work rather than direct conversion.
