# Phase 7 Agent D Handoff - P7-0066 BuildingGameplayDependencySystem

Date: 2026-06-22
Lane: Agent D - Building placement, spawn, production, and base systems

## Completed Row

| Row | System | Result |
| --- | --- | --- |
| `P7-0066` | `BuildingGameplayDependencySystem` | Retired/folded disabled `SystemBase` wrapper into a plain direct-owned dependency helper. |

## Scope

- Removed the disabled ECS lifecycle wrapper from `Assets/Game/Scripts/Systems/BuildingGameplayDependencySystem.cs`.
- Preserved startup/runtime dependency binding for menu UI, day/night, selection camera, building interaction, runtime blockers, runtime city, citizen population events, and faction visuals.
- Preserved build command mode routing, command clear routing, pointer/build-drawer queries, camera focus callbacks, HUD selection feedback, minimap dirty notification, boardable transport click checks, move-order-to-building requests, runtime blocker removal, configured-house lookup, and home-building-destroyed notification.
- No call-site behavior was changed; the helper remains directly owned by `BuildingGameplayCompositionSourceSystem`.
- No scene, prefab, material, or ScriptableObject assets were changed.

## Inventory Impact

- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Production `SystemBase`/legacy declarations: `157`.
- Production `ISystem` declarations: `133`.
- Inventory rows: `290 total`, `283 ProductionNonUI`, `7 ProductionUI`.
- Owner lane rows: `AgentD 21`.
- Dispositions: `Converted 126`, `DirectConvert 41`, `ManagedPresentationSystemBaseException 22`, `RetireFold 14`, `SplitThenConvert 80`, `UIOutOfScope 7`.
- `P7-0066` no longer appears as an active SystemBase inventory row.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - Result: passed, `0 Warning(s)`, `0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
  - Result: passed.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-dependency-helper-fold-runtime-building.log`
  - Marker: `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-dependency-helper-fold-smoke.log`
  - Marker: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`
  - Marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`
  - Result: passed.

Note: An earlier parallel batchmode attempt hit Unity project-lock/Bee artifact contention while another validation was compiling. The smoke and architecture validations were rerun serially and passed with the log paths above.

## Follow-Up

- Continue Agent D on the next split-before-convert row.
- The remaining Agent D open rows now include broad building combat, definition, placement command/commit/lifecycle/preview/startup, production, production transport, and selection systems. Those should be split by responsibility before any conversion rather than wrapped as broad `ISystem` replacements.
