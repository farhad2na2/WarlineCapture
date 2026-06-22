# Phase 7 Agent D Handoff - P7-0105 BuildingProductionUpdateSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0105` - `BuildingProductionUpdateSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingProductionUpdateSystem` was a disabled `SystemBase` wrapper manually owned by building production composition/runtime tick. It did not schedule ECS work and only exposed production update helper methods.
- New: `BuildingProductionUpdateSystem` is a plain sealed helper. Existing production runtime tick wiring still calls the same helper methods, while the fake ECS lifecycle is removed.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingProductionUpdateSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-21_phase7_agent_d_p7-0105_building_production_update_helper_fold_handoff.md`

Behavior preserved:
- Pending-production iteration over runtime building dictionaries.
- Active transport update iteration.
- Null pending cleanup and production timeline rebuild.
- Transport launch window checks, delayed production, and transport spawn handoff.
- Direct unit spawn completion and pending production removal.
- Random-state mutation through the existing ref parameter.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `326`
- Production `SystemBase`/legacy declarations: `193`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `319`
- Agent D rows: `57`
- DirectConvert rows: `42`
- Open rows: `171`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-production-update-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only the fake `SystemBase` inheritance and disabled lifecycle.
- Production update behavior still uses managed runtime-building collections and existing transport/spawn helpers. Broader production/spawn conversion remains open.

Next guidance:
- Continue Agent D with the remaining low-risk direct row `BuildingPlacementRuntimeTickSystem` before broader split-before-convert systems.
