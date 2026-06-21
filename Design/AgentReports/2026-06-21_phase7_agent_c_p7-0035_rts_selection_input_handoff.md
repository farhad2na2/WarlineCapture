# Phase 7 Agent C Handoff - P7-0035 RtsSelectionInputSystem

Branch:
`codex/phase7-agent-c-selection-commands`

Rows completed:
- `P7-0035` - `RtsSelectionInputSystem` - Retired/Folded

Scope:
- Agent C selection input state and command-intent enqueue helper.
- Inventory row: `P7-0035 RtsSelectionInputSystem`.

Changes:
- Folded `RtsSelectionInputSystem` out of disabled `SystemBase` into a plain manually constructed helper.
- Preserved drag state, active command mode state, queued move order state, pointer request buffers, command intent request buffers, and transport/scan/selection rectangle enqueue helpers.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.

Architecture notes:
- No new runtime `SystemBase`.
- No new `MonoBehaviour` update loop.
- No manager/controller/facade shell added.
- `RtsSelectionInputSystem` remains a passive helper used by selection startup, runtime input, command UI, camera input, and focused tests.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-rts-selection-input-fold.log`
  - Marker: `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-rts-selection-input-request-result.log`
  - Marker: `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-rts-selection-input-hud-controls.log`
  - Marker: `[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed after tracker and handoff updates.

Follow-up:
- Continue Agent C with the next open selection/command boundary from `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`.
