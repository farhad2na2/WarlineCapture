# Phase 7 Agent C Handoff - Command Flush And Pointer Target Helper Folds

Branch:
`codex/phase7-agent-c-selection-commands`

Rows completed:

- `P7-0030` - `RtsSelectionCommandResultFlushSystem` - Retired/folded from disabled `SystemBase` into a plain helper.
- `P7-0039` - `RtsSelectionPointerTargetCommandSystem` - Retired/folded from disabled `SystemBase` into a plain helper.

Contracts changed:

- No ECS request/result component schema changed.
- No command behavior changed.
- Existing manually constructed selection startup ownership is preserved.

Counts:

- Converted to ISystem: `0`
- Split passive/managed boundaries: `2`
- Managed SystemBase exceptions: `0`
- Retired/folded: `2`

Implementation notes:

- `RtsSelectionCommandResultFlushSystem` now remains a plain command-result helper. It still owns command-result flushing, HUD callbacks, command-mode transitions, order-marker updates, selected-building destroy fallback, and command-family request/result processing.
- `RtsSelectionPointerTargetCommandSystem` now remains a plain pointer target helper. It still owns pointer-to-unit, pointer-to-cell, move/attack/scan/board target request routing, map-surface target resolution, selected footprint target search, and click diagnostics.
- Both helpers are still created explicitly by `SelectionGameplayStartupSystem`; neither is scheduled as an ECS world system.

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` - passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` - passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-command-result-flush-request-result.log` - passed with `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-pointer-target-rts-selection-input.log` - passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MapSurfaceLayeredGridFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-pointer-target-map-surface.log` - passed with `[MapSurfaceLayeredGridFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-pointer-target-transport.log` - passed with `[UnitTransportValidation] result=Passed tests=73`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` - passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` - passed.

Risks:

- `RtsSelectionPointerTargetCommandSystem` still uses `Camera` through explicit context arguments. That remains acceptable for this slice because it is no longer an ECS system and no managed ticking owner was introduced.
- Remaining Agent C rows are `SelectionBuildingInteractionSystem` and `SelectionGameplayStartupSystem`; both are mixed startup/building/camera boundaries and should be split or coordinated with Agent D rather than folded blindly.
