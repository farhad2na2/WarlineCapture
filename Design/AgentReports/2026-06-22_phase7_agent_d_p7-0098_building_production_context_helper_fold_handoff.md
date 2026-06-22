# Phase 7 Agent D Handoff - P7-0098 BuildingProductionContextSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0098` - `BuildingProductionContextSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingProductionContextSystem` was a disabled `SystemBase` wrapper manually constructed by building gameplay composition. It did not schedule ECS work and only composed production, queue, transport, transport-bridge, update, request, and resource-hauler contexts.
- New: `BuildingProductionContextSystem` is a plain sealed context factory owned by building gameplay composition. Existing production context construction remains in the same direct-call path without a fake ECS lifecycle.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-22_phase7_agent_d_p7-0098_building_production_context_helper_fold_handoff.md`

Behavior preserved:
- `BuildingProductionContextSystem.Source` data shape.
- Production update context creation and transport pool prewarming.
- Production transport and transport-bridge context construction.
- Production request context construction and production settings prewarming.
- Production queue context construction and runtime boundary entity delegate wiring.
- Player unit production queue handoff through `TryQueuePlayerUnitProduction`.
- Resource hauler bridge context construction.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `322`
- Production `SystemBase`/legacy declarations: `189`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `315`
- Agent D rows: `53`
- SplitThenConvert rows: `112`
- Open rows: `167`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionRequestValidation -logFile /private/tmp/warline-phase7-agent-d-production-context-helper-fold-production.log`: passed, marker `[BuildingProductionRequestValidation] result=Passed tests=21`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-production-context-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only the unused ECS inheritance and disabled lifecycle from a direct-owned production context factory.
- The helper still carries managed `GameObject` and `Camera` references by design because it is a managed composition helper, not a hot ECS processor.

Next guidance:
- Continue Agent D with low-risk narrow context/composition helpers before broad split-before-convert owners.
