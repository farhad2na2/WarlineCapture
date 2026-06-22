# Phase 7 Agent D Handoff - P7-0142 MapBuildingPlacementSpawnSystem

Date: 2026-06-22
Lane: Agent D - building/production

## Completed Slice

| Inventory id | Type | Result |
| --- | --- | --- |
| `P7-0142` | `MapBuildingPlacementSpawnSystem` | Retired/folded disabled `SystemBase` wrapper into a plain direct-owned map placement spawn helper. |

## Scope

- Removed the disabled ECS lifecycle wrapper from `Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnSystem.cs`.
- Preserved authored map placement queueing, hidden-authoring completion, runtime spawn routing, owner faction assignment, and authored visual wrapping.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent A, Agent D, and project Phase 7 progress trackers.

## Architecture Notes

- No domain ownership moved to UI, GameObject presentation, camera, or a managed runtime `SystemBase`.
- No new manager/controller/facade, broad replacement shell, or updating MonoBehaviour loop was introduced.
- This was a disabled wrapper fold only; the existing direct-owned helper flow remains the runtime owner for authored map placement startup work.

## Inventory Impact

- Total ECS system declarations: `294`.
- Production `SystemBase`/legacy declarations: `161`.
- Production `ISystem` declarations: `133`.
- Production non-UI rows: `287`.
- Agent D rows: `25`.
- `SplitThenConvert` rows: `84`.
- Open rows: `139`.

## Validation

Commands:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-phase7-agent-d-map-building-placement-spawn-helper-fold-boundary.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-map-building-placement-spawn-helper-fold-smoke.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-agent-d-map-building-placement-spawn-helper-fold-boundary.log`: `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`
- `/private/tmp/warline-phase7-agent-d-map-building-placement-spawn-helper-fold-smoke.log`: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: `0 Warning(s), 0 Error(s)`
- `git diff --check`: passed

## Follow-Up

- Continue Agent D with the next low-risk row before broad production, placement, or definition owners.
