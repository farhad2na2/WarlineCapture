# Phase 7 Agent D Handoff - P7-0087 BuildingPlacementPreviewSystem

Date: 2026-06-22
Lane: Agent D - Building Placement, Spawn, Production, And Base Systems

## Rows Completed

- `P7-0087 BuildingPlacementPreviewSystem`

## Scope

- Folded `BuildingPlacementPreviewSystem` from a disabled `SystemBase` wrapper into a plain placement preview helper.
- Preserved placement outline creation, preview material setup, valid/invalid tinting, wall preview rebuilding, wall segment validity tinting, disposal, and runtime object destruction policy.
- Did not introduce any new `MonoBehaviour` runtime loop, manager/controller/facade, broad replacement shell, or ECS gameplay owner.
- Kept Unity-object presentation behavior in the existing direct-owned helper path; no recurring ECS system lifetime remains for this preview presentation helper.

## Inventory Impact

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Production `SystemBase`/legacy declarations moved from `153` to `152`.
- Total inventory rows moved from `286` to `285`.
- Production non-UI rows moved from `279` to `278`.
- Agent D open inventory rows moved from `17` to `16`.
- SplitThenConvert rows moved from `76` to `75`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-phase7-agent-d-placement-preview-helper-fold-placement-command.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-placement-preview-helper-fold-smoke.log
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Markers:

- `/private/tmp/warline-phase7-agent-d-placement-preview-helper-fold-placement-command.log`: `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`
- `/private/tmp/warline-phase7-agent-d-placement-preview-helper-fold-smoke.log`: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `dotnet build`: `0 Warning(s), 0 Error(s)`
- `git diff --check`: passed

## Next Step

Continue Agent D with the next split-before-convert row before broad spawn, production, combat, or runtime building owners.
