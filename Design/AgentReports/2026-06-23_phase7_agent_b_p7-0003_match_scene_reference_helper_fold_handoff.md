# Phase 7 Agent B Handoff - P7-0003 Match Scene Reference Helper Fold

Date: `2026-06-23`
Lane: `AgentB`
Inventory row: `P7-0003 MatchSceneReferenceBoundarySystem`

## Summary

Retired `MatchSceneReferenceBoundarySystem` from the ECS inventory. The disabled `SystemBase` only stored a managed `MatchSceneView` reference; it is now replaced by direct loaded-scene root resolution in `MatchSceneReferenceSystem`.

## Changed Files

- `Assets/Game/Scripts/Composition/MatchSceneReferenceSystem.cs`
  - Removed `MatchSceneReferenceBoundarySystem : SystemBase`.
  - Removed world-scoped `Register`/`Clear` storage.
  - Added direct loaded-scene root resolution for `MatchSceneView`.
- `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
  - Removed match scene reference registration/clear calls.
- `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`
  - Reads the direct match scene resolver.
- `Assets/Game/Scripts/Composition/MatchStartSystem.cs`
  - Reads the direct match scene resolver.
- `Assets/Game/Scripts/Composition/MatchSceneView.cs`
  - Removed the now-empty `Start` forwarding method.
- `Assets/Game/Scripts/Composition/MatchSceneReferenceComponent.cs`
  - Updated the ownership note.
- `Assets/Tests/Editor/MatchSceneReferenceSystemTests.cs`
  - Added focused validation for scene-root `MatchSceneView` resolution and unloaded-scene negative behavior.
- `Design/Architecture/systembase_to_isystem_inventory.md`
  - Regenerated after the helper fold.
- `Design/Architecture/phase7_agent_b_direct_startup_tracker.md`
  - Recorded `P7-0003` as folded and left `P7-0019` as the remaining held Agent B RetireFold row.
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
  - Updated inventory counts, percentages, validation logs, and next action.

## Inventory Counts

- Total ECS system declarations: `164`
- Production `SystemBase`/legacy declarations: `26`
- Production `ISystem` declarations: `138`
- Current production `ISystem` share: `84.1%`
- Production non-UI rows: `156`
- Production UI rows: `8`
- Statuses: `Converted 131`, `Deferred 8`, `ManagedException 24`, `Open 1`

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
git diff --check
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchSceneReferenceSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-b-match-scene-reference.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor compile passed with `0 Warning(s), 0 Error(s)`.
- Inventory regeneration passed.
- `git diff --check` passed.
- Match scene reference focused validation passed: `/private/tmp/warline-phase7-agent-b-match-scene-reference.log`, marker `[MatchSceneReferenceFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Residual Risk

- `P7-0019 PerformanceDiagnosticsReferenceBoundarySystem` remains the only open Agent B RetireFold row.
- `MatchSceneReferenceSystem.TryGetLoadedMatchSceneView` assumes the active Match scene keeps `MatchSceneView` on a loaded scene root. This matches the current `Assets/Game/Scenes/Match.unity` root `Bootstrap` object.
