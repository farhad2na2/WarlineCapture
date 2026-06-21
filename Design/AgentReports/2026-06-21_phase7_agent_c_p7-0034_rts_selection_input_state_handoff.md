# Phase 7 Agent C Handoff - P7-0034 RtsSelectionInputStateSystem

Date: `2026-06-21`
Lane: `AgentC`
Inventory row: `P7-0034`
System: `RtsSelectionInputStateSystem`

## Result

- Retired/folded `RtsSelectionInputStateSystem` out of ECS inheritance.
- Preserved its manually constructed request-buffer helper API used by `RtsSelectionInputSystem`, `UiRuntimeBoundaryAdapters`, `RtsSelectionCommandResultFlushSystem`, `RtsSelectionFocusCommandSystem`, and tests.
- Removed disabled `SystemBase` lifecycle methods that never owned scheduled ECS work.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`; the type no longer appears in the ECS system denominator.

## Changed Files

- `Assets/Game/Scripts/Systems/RtsSelectionInputStateSystem.cs`
- `Assets/Tests/Editor/RtsSelectionInputSystemTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- Agent C and Phase 7 tracker documents

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
```

Result: passed with `0` warnings and `0` errors.

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
```

Result: passed; production `SystemBase`/legacy declarations decreased to `245`.

```bash
git diff --check
```

Result: passed.

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-c-rts-selection-input.log
```

Result: passed with marker `[RtsSelectionInputSystemValidation] result=Passed tests=56`.

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Result: passed with marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Residual Risk

- Low. This was a lifecycle inheritance cleanup for a helper that callers already instantiate directly with `new`.
- The affected validation suite had two stale source-string assertions for transport boarding planning/capacity names; those assertions were updated to preserve the behavior contract without locking the implementation to old local variable names.

## Next Target

- Continue Agent C with the next low-risk selection/command row after focused validation passes.
