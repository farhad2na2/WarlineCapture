# Phase 7 Agent B Handoff - P7-0020 PerformanceDiagnosticsSystem

Date: `2026-06-21`
Lane: `AgentB`
Inventory row: `P7-0020`
System: `PerformanceDiagnosticsSystem`

## Result

- Retired/folded `PerformanceDiagnosticsSystem` out of ECS inheritance.
- Removed disabled `SystemBase` lifecycle methods from a diagnostics object that was already manually owned by menu/bootstrap composition.
- Preserved all public diagnostics APIs, profiler recorder ownership, freeze/FPS logging, and allocation behavior.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`; the type no longer appears in the ECS system denominator.

## Changed Files

- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- Phase 7 tracker documents

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
```

Result: passed with `0 Warning(s), 0 Error(s)`.

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-b-performance-diagnostics.log
```

Result: passed, marker `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=1`.

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
```

Result: passed.

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Result: passed.

```bash
git diff --check
```

Result: passed.

## Residual Risk

- This remains a managed diagnostics helper because it owns `ProfilerRecorder`, string builders, and Unity diagnostics APIs. That ownership is now explicit composition-owned code rather than an inert ECS system.

## Next Target

- Continue Agent B with `P7-0002 MapSurfaceRuntimeBootstrapSystem` split safety review.
