# Phase 7 Agent C Handoff - P7-0051 SelectionUiCommandUiSystemHelper

## Scope

- Lane: `AgentC` selection, commands, focus, and player intent.
- Inventory row: `P7-0051 SelectionUiCommandUiSystemHelper`.
- Result: retired/folded from disabled `SystemBase` into a plain UI command facade.

## Changes

- Removed `[DisableAutoCreation]`, `SystemBase` inheritance, and empty ECS lifecycle methods from `SelectionUiCommandUiSystemHelper`.
- Preserved the `ISelectionUiCommand` implementation and all public UI command methods:
  - selection mode enter/exit;
  - move, attack, scan, board target modes;
  - hold, stop, return to base, destroy focused unit;
  - select all, soldiers, vehicles;
  - board nearest/all;
  - focused transport/passenger disembark.
- Preserved UI click capture, gameplay-input lock checks, command intent queuing, screen-rect select-all requests, and diagnostics traces.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated the Agent C tracker, Agent A tracker, and ECS architecture tracker with the new inventory counts and validation logs.

## Architecture Notes

- This type has no independent ECS update lifetime and is constructed by runtime/composition/UI tests as a command facade.
- It remains managed because it bridges UI commands to selection command request buffers and reads the focused-unit UI read model through the default world for transport disembark commands.
- Folding it out removes a disabled `SystemBase` shell without forcing UI command facade behavior into a broad replacement `ISystem`.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0` warnings, `0` errors.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-command-selection.log`
  - Result marker: `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-command-hud-controls.log`
  - Result marker: `[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-selection-ui-command-request-result.log`
  - Result marker: `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed.

## Follow-Up

- Remaining Agent C rows are broader split-before-convert systems. Continue only with a scoped request/result boundary slice; avoid direct conversion of pointer, focus, or command-result orchestration systems.
