# Phase 7 Agent D Handoff - P7-0097 BuildingProductionCompositionSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0097` - `BuildingProductionCompositionSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingProductionCompositionSystem` was a disabled `SystemBase` wrapper manually constructed by building gameplay composition. It did not schedule ECS work and only composed `BuildingProductionContextSystem.Source`.
- New: `BuildingProductionCompositionSystem` is a plain sealed helper owned by building gameplay composition. Existing production context source wiring stays in the same direct-call path without a fake ECS lifecycle.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingProductionCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-22_phase7_agent_d_p7-0097_building_production_composition_helper_fold_handoff.md`

Behavior preserved:
- Runtime context source creation through `BuildingRuntimeCompositionSystem`.
- Runtime query context and spawn context construction.
- Production context source wiring for definitions, production, production update, transport, transport bridge, slot, runway, visual, spawn, resources, haulers, factions, grid access, runtime building lookup, marker refresh, focused-unit clearing, camera focus, and transport drop visuals.
- Begin-placement fallback when no `EntityManager` is available.
- Existing production queue callback still passes `UnityEngine.Time.time`.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `323`
- Production `SystemBase`/legacy declarations: `190`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `316`
- Agent D rows: `54`
- SplitThenConvert rows: `113`
- Open rows: `168`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionRequestValidation -logFile /private/tmp/warline-phase7-agent-d-production-composition-helper-fold-production.log`: passed, marker `[BuildingProductionRequestValidation] result=Passed tests=21`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-production-composition-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only the unused ECS inheritance and disabled lifecycle from a direct-owned production context composition helper.
- The helper still crosses managed GameObject/Camera/MaterialPropertyBlock boundaries by design; these remain outside unmanaged `ISystem`.

Next guidance:
- Continue Agent D with low-risk narrow composition/helpers before broad split-before-convert owners.
