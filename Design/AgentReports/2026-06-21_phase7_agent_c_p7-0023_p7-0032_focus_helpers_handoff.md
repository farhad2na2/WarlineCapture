# Phase 7 Agent C Handoff - Focus Helper Folds

Branch:
`codex/phase7-agent-c-selection-commands`

Rows completed:

- `P7-0023` - `FocusableUnitLookupSystem` - Retired/folded from `SystemBase` into a plain helper.
- `P7-0032` - `RtsSelectionFocusCommandSystem` - Retired/folded from disabled `SystemBase` into a plain helper.

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

- `FocusableUnitLookupSystem` now remains a plain query/camera hit helper. It still owns EntityQuery cache setup, grid-cell focusable lookup, screen-distance lookup, selection-hitbox screen bounds, transit-state filtering, and the Burst chunk collector.
- `RtsSelectionFocusCommandSystem` now remains a plain focus command helper. It still owns external focus/select-all/deselect/selection-mode request consumption, HUD command result/mode callbacks, focus validation, and input guard cleanup.
- Both helpers are still created explicitly by `SelectionGameplayStartupSystem`; neither is scheduled as an ECS world system.

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` - passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` - passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FocusableUnitLookupSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focusable-unit-lookup.log` - passed with `[FocusableUnitLookupFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-focus-command-rts-selection-input.log` - passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-focus-command-request-result.log` - passed with `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` - passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` - passed.

Risks:

- These rows still use `Camera` through explicit method/context arguments. That is acceptable for this slice because they are no longer ECS systems and no managed ticking owner was introduced.
- Remaining Agent C work is broader: `RtsSelectionCommandResultFlushSystem`, `RtsSelectionPointerTargetCommandSystem`, `SelectionBuildingInteractionSystem`, and `SelectionGameplayStartupSystem` need split planning rather than a blind inheritance change.
