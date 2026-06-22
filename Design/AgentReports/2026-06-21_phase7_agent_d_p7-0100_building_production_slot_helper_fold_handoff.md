# Phase 7 Agent D Handoff - P7-0100 BuildingProductionSlotSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0100` - `BuildingProductionSlotSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingProductionSlotSystem` was a disabled `SystemBase` wrapper manually owned by building production/spawn composition. It did not schedule ECS work and only exposed production-slot helper methods.
- New: `BuildingProductionSlotSystem` is a plain sealed helper. Existing production and spawn code still call the same helper methods, while the fake ECS lifecycle is removed.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingProductionSlotSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-21_phase7_agent_d_p7-0100_building_production_slot_helper_fold_handoff.md`

Behavior preserved:
- Production slot reservation.
- Pending-production reservation checks.
- Occupied-slot cleanup for stale entity references.
- Available production spawn-slot and local-position lookup.
- Produced-unit liveness checks through `EntityManager.Exists`, `UnitHealth`, and `Entity.Null`.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `327`
- Production `SystemBase`/legacy declarations: `194`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `320`
- Agent D rows: `58`
- DirectConvert rows: `43`
- Open rows: `172`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-production-slot-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only the fake `SystemBase` inheritance and disabled lifecycle.
- Slot checks still use `EntityManager` through the existing managed production/spawn path. Broader production and spawn ownership remains open for later split/conversion work.

Next guidance:
- Continue Agent D with the remaining low-risk direct rows (`BuildingPlacementRuntimeTickSystem`, `BuildingProductionUpdateSystem`) before broader split-before-convert systems.
