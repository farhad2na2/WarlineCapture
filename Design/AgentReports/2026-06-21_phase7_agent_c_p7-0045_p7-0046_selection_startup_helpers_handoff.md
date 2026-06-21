# Phase 7 Agent C Handoff - Selection Building And Startup Helper Folds

Branch:
`codex/phase7-agent-c-selection-commands`

Rows completed:

- `P7-0045` - `SelectionBuildingInteractionSystem` - Retired/folded from disabled `SystemBase` into a plain helper.
- `P7-0046` - `SelectionGameplayStartupSystem` - Retired/folded from disabled `SystemBase` into a plain startup composition boundary.

Contracts changed:

- No ECS request/result component schema changed.
- No command behavior changed.
- Existing direct construction by `ManagedGameplayStartupSystem` is preserved.

Counts:

- Converted to ISystem: `0`
- Split passive/managed boundaries: `2`
- Managed SystemBase exceptions: `0`
- Retired/folded: `2`

Implementation notes:

- `SelectionBuildingInteractionSystem` now remains a plain building selection/move helper. It still owns match HUD selection panel binding, building selection HUD feedback, focused-unit clearing, boardable transport click tests, and move-order-to-building routing.
- `SelectionGameplayStartupSystem` now remains a plain startup composition boundary. It still owns selection runtime update orchestration, context construction, UI binding callbacks, command result draining, pointer/input/camera wiring, HUD read-model refresh, and direct construction by `ManagedGameplayStartupSystem`.
- The actual managed camera/runtime presentation systems resolved by startup remain counted managed boundaries where appropriate; this slice did not replace them or introduce a new runtime loop.

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` - passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` - passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-startup-rts-selection-input.log` - passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-c-selection-startup-request-result.log` - passed with `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudSquadTraySelectionSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-startup-squad-tray.log` - passed with `[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` - passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` - passed.

Risks:

- `SelectionGameplayStartupSystem` is still a large managed composition boundary. This slice removes it from ECS scheduling debt but does not split its responsibilities further.
- Some camera and UI references remain as constructor/context arguments. That is expected for startup composition and does not add a new updating MonoBehaviour or broad replacement shell.
- Agent C has no remaining open SystemBase rows in the regenerated inventory; next Phase 7 work should continue with the Agent F request-contract slice.
