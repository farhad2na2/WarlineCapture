# Phase 7 Agent F Handoff - P7-0252 Unit Selection Marker Outline Split

Date: 2026-06-22

Lane: Agent F rendering / marker presentation

Rows:

- `P7-0252` `UnitSelectionMarkerSystem`
- `P7-0383` `UnitSelectionObjectOutlinePresentationSystem`

## Summary

`UnitSelectionMarkerSystem` is now a blocker-free converted `ISystem` for selected-unit marker instance ownership, health/passenger eligibility, marker scale, and variant visibility.

Selection object-outline `Material`, generated safe-volume `Mesh`, and `RenderMeshArray` presentation setup moved into `UnitSelectionObjectOutlinePresentationSystem`, a counted managed `SystemBase` presentation exception. This preserves the existing ECS outline entities and authored marker visuals without keeping `Material` ownership in the converted `ISystem`.

## Inventory Accounting

- Production `SystemBase`/legacy declarations: `52`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `72.0%`.
- Total ECS declarations: `186`.
- Open rows: `27`.
- Managed presentation exceptions: `24`.

This slice intentionally adds one counted managed presentation `SystemBase` boundary while converting one already-`ISystem` open split row. The `ISystem` count therefore stays flat, the managed-exception count increases by one, and open split debt decreases by one.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod VehicleVisualAdornmentsSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-unit-selection-marker-outline-split-vehicle-visual.log`: passed, marker `[VehicleVisualAdornmentsFocusedValidation] result=Passed tests=20`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

## Notes

- No `MonoBehaviour` update/coroutine loop was introduced.
- No UI Toolkit or Canvas migration files were touched.
- The managed exception is scoped to selection object-outline presentation and is covered by the reviewed architecture policy-mix allowlist because its type names necessarily contain selection vocabulary.
