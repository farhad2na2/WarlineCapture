# Phase 7 Agent C Handoff - P7-0049 SelectionRuntimeDiagnosticsSystem

## Scope

- Lane: `AgentC` selection, commands, focus, and player intent.
- Inventory row: `P7-0049 SelectionRuntimeDiagnosticsSystem`.
- Result: retired/folded from disabled `SystemBase` into a plain diagnostics helper.

## Changes

- Removed the disabled `SystemBase` inheritance and empty ECS lifecycle from `SelectionRuntimeDiagnosticsSystem`.
- Preserved the existing static and instance diagnostics API:
  - `EnqueueSelectionDiagnostic`
  - `EnqueueSelectionDiagnosticMessage`
  - `LogSelectionClickDiagnostic`
  - `LogSelectionClickDiagnosticMessage`
  - `LogSelectionClickDebug`
  - `LogMoveCommandTrace`
  - `LogScanCommandTrace`
- Updated `SelectionGameplayStartupSystem` to manually construct the diagnostics helper instead of asking the ECS world for a managed system.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated the Agent C tracker, Agent A tracker, and ECS architecture tracker with the new inventory counts and validation logs.

## Architecture Notes

- This type is not a hot gameplay system and has no independent ECS update lifetime.
- It intentionally remains managed because it owns Unity `Debug` logging and `Application.isBatchMode` diagnostics behavior.
- Folding it into a plain helper avoids creating a broad replacement `ISystem` shell and keeps diagnostics outside the Phase 7 production ECS denominator.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0` warnings, `0` errors.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json`
  - Result: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-selection-runtime-diagnostics.log`
  - Result marker: `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed.

## Follow-Up

- Continue Agent C with the next low-risk selection/command helper.
- Do not convert diagnostics helpers into updating systems unless they gain actual ECS-owned recurring behavior.
