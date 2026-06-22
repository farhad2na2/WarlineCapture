# Phase 7 Agent B Handoff - P7-0019 Performance Diagnostics Reference Helper Fold

Date: `2026-06-23`
Lane: `AgentB`
Inventory row: `P7-0019 PerformanceDiagnosticsReferenceBoundarySystem`

## Summary

Retired `PerformanceDiagnosticsReferenceBoundarySystem` from the ECS inventory. The disabled `SystemBase` only stored a managed `PerformanceDiagnosticsSystem` reference; it is now replaced by direct loaded-Menu-scene root resolution through `PerformanceDiagnosticsReferenceSystem`.

## Changed Files

- `Assets/Game/Scripts/Composition/PerformanceDiagnosticsReferenceSystem.cs`
  - Moved from `Systems` into the composition assembly.
  - Removed `PerformanceDiagnosticsReferenceBoundarySystem : SystemBase`.
  - Removed world-scoped `Register`/`Clear` storage.
  - Added direct loaded Menu scene root resolution for initialized `MenuBootstrapView.PerformanceDiagnostics`.
- `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`
  - Removed diagnostics reference registration/clear calls.
  - Exposes whether menu diagnostics has been initialized.
- `Assets/Game/Scripts/Composition/MenuBootstrapView.cs`
  - Exposes initialized diagnostics status to the resolver.
- `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
  - Reads the direct diagnostics resolver and keeps its existing fallback when menu diagnostics is unavailable.
- `Assets/Game/Scripts/RuntimeState/PerformanceDiagnosticsReferenceComponent.cs`
  - Updated the ownership note.
- `Assets/Tests/Editor/PerformanceDiagnosticsSystemAllocationTests.cs`
  - Added focused resolver validation for uninitialized and initialized menu diagnostics.
- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated after the helper fold.
- `Design/Architecture/phase7_agent_b_direct_startup_tracker.md`
  - Recorded `P7-0019` as folded and noted no remaining open Agent B inventory rows.
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated final inventory counts, percentages, validation logs, and next action.

## Inventory Counts

- Total ECS system declarations: `163`
- Production `SystemBase`/legacy declarations: `25`
- Production `ISystem` declarations: `138`
- Current production `ISystem` share: `84.7%`
- Production non-UI rows: `155`
- Production UI rows: `8`
- Statuses: `Converted 131`, `Deferred 8`, `ManagedException 24`
- Open rows: `0`

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-b-performance-diagnostics-reference.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
git diff --check
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor compile passed with `0 Warning(s), 0 Error(s)`.
- Performance diagnostics focused validation passed: `/private/tmp/warline-phase7-agent-b-performance-diagnostics-reference.log`, marker `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`.
- Inventory regeneration passed.
- `git diff --check` passed.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Residual Risk

- The resolver intentionally returns menu diagnostics only after `MenuBootstrapSystem` has initialized diagnostics. If the Menu scene is unavailable or diagnostics is not initialized, `MatchBootstrapSystem` keeps the existing local fallback diagnostics path.
- The current authoritative inventory has `0` open Phase 7 rows; remaining production `SystemBase` declarations are counted managed exceptions or UI out-of-scope rows.
