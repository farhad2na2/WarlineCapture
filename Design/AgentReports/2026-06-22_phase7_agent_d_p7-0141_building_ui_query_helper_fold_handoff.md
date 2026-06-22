# Phase 7 Agent D Handoff - P7-0141 BuildingUiQuerySystem

Date: 2026-06-22
Lane: Agent D - building/production

## Completed Slice

| Inventory id | Type | Result |
| --- | --- | --- |
| `P7-0141` | `BuildingUiQuerySystem` | Retired/folded disabled `SystemBase` wrapper into a plain direct-owned building UI query helper. |

## Scope

- Removed the disabled ECS lifecycle wrapper from `Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs`.
- Preserved selected-building labels, descriptions, health, preview prefab lookup, produced-unit lists, pending-production UI entries, owner-faction filtering, and visible-selectable checks.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent A, Agent D, and project Phase 7 progress trackers.

## Architecture Notes

- No UI Toolkit or Canvas migration work was touched.
- No domain ownership moved to a new managed runtime `SystemBase`.
- No new manager/controller/facade, broad replacement shell, or updating MonoBehaviour loop was introduced.
- This was a disabled wrapper fold only; the existing direct-owned query helper and adapter contract remain unchanged.

## Inventory Impact

- Total ECS system declarations: `293`.
- Production `SystemBase`/legacy declarations: `160`.
- Production `ISystem` declarations: `133`.
- Production non-UI rows: `286`.
- Agent D rows: `24`.
- `SplitThenConvert` rows: `83`.
- Open rows: `138`.

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingUiQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-building-ui-query-helper-fold-query.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-building-ui-query-helper-fold-runtime-building.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-building-ui-query-helper-fold-smoke.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-agent-d-building-ui-query-helper-fold-query.log`: `[BuildingUiQueryValidation] result=Passed tests=5`
- `/private/tmp/warline-phase7-agent-d-building-ui-query-helper-fold-runtime-building.log`: `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- `/private/tmp/warline-phase7-agent-d-building-ui-query-helper-fold-smoke.log`: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: `0 Warning(s), 0 Error(s)`
- `git diff --check`: passed

## Follow-Up

- Continue Agent D with the next low-risk row before broad production, placement, definition, combat, or selection owners.
