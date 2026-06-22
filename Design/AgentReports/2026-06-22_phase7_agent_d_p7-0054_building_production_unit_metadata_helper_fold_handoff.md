# Phase 7 Agent D Handoff - P7-0054 BuildingProductionUnitMetadataSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0054` - `BuildingProductionUnitMetadataSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingProductionUnitMetadataSystem` was a disabled `SystemBase` wrapper with only static metadata helpers. It did not schedule ECS work and did not need ECS lifecycle ownership.
- New: `BuildingProductionUnitMetadataSystem` is a static production metadata helper. Existing call sites in `MatchBootstrapSystem` and production tests still use the same static methods.

Files changed:
- `Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-22_phase7_agent_d_p7-0054_building_production_unit_metadata_helper_fold_handoff.md`

Behavior preserved:
- `PrepareTransportDropVisual(GameObject visual)` still disables `UnitGridAuthoring` on the drop visual when present.
- `TryGetMetadata(GameObject prefab, out BuildingProductionSystem.UnitProductionMetadata metadata)` still reads `UnitGridAuthoring` production duration, transport prefab, air-unit flag, transport timing/concurrency/runway settings, and configured footprint cells.
- Existing `MatchBootstrapSystem` metadata resolver and transport drop visual delegate wiring stayed unchanged.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `321`
- Production `SystemBase`/legacy declarations: `188`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `314`
- Agent D rows: `52`
- SplitThenConvert rows: `111`
- Open rows: `166`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionMetadataValidation -logFile /private/tmp/warline-phase7-agent-d-production-unit-metadata-helper-fold-metadata.log`: passed, marker `[BuildingProductionMetadataValidation] result=Passed tests=3`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-agent-d-production-unit-metadata-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only fake ECS inheritance from a static metadata helper.
- The helper still reads `GameObject` authoring metadata by design because this is a composition/bootstrap metadata boundary, not hot ECS gameplay.

Next guidance:
- Continue Agent D with the next low-risk narrow helper before broad split-before-convert owners.
