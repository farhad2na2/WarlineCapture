# Phase 7 Agent D Handoff - P7-0084 BuildingPlacementInteractionSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems

## Rows Completed

- `P7-0084 BuildingPlacementInteractionSystem`

## Scope

- Folded `BuildingPlacementInteractionSystem` from a disabled `SystemBase` wrapper into a plain placement interaction helper.
- Preserved selected-building state queries, placement confirm/cancel/exit routing, Soldier Base placement start, runtime entity destroyed handling, base-breach target resolution, and selected-building label/status helpers.
- Did not introduce any new `MonoBehaviour` runtime loop, manager/controller/facade, broad replacement shell, or managed ECS gameplay owner.

## Inventory Impact

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Production `SystemBase`/legacy declarations moved from `155` to `154`.
- Total inventory rows moved from `288` to `287`.
- Production non-UI rows moved from `281` to `280`.
- Agent D open inventory rows moved from `19` to `18`.
- SplitThenConvert rows moved from `78` to `77`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-phase7-agent-d-placement-interaction-helper-fold-placement-command.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-interaction-helper-fold-smoke.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-agent-d-placement-interaction-helper-fold-placement-command.log`: `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`
- `/private/tmp/warline-phase7-agent-d-placement-interaction-helper-fold-smoke.log`: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `dotnet build`: `0 Warning(s), 0 Error(s)`
- `git diff --check`: passed

## Next Step

Continue Agent D with the next low-risk split-before-convert row before broad spawn, production, combat, or runtime building owners.
