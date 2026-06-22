# Phase 7 Agent D Handoff - P7-0093 BuildingPlacementRuntimeTickSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0093` - `BuildingPlacementRuntimeTickSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingPlacementRuntimeTickSystem` was a disabled `SystemBase` wrapper manually constructed by the building gameplay composition source. Its ECS lifecycle was not used.
- New: `BuildingPlacementRuntimeTickSystem` is a plain sealed helper owned by building gameplay composition. Startup and simulation runtime tick orchestration stays in the same direct-call path without a fake ECS lifecycle.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs`
- `Assets/Tests/Editor/BuildingPlacementRuntimeTickSystemTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-21_phase7_agent_d_p7-0093_building_placement_runtime_tick_helper_fold_handoff.md`

Behavior preserved:
- Startup runtime boundary publish before and after map building/vehicle placement queues.
- Simulation ordering for map placement queues, runtime boundary publish, production, transports, resources, haulers, visuals, reservation cleanup, destroyed cleanup, road doors, marker refresh, and placement input.
- Existing cadence throttles for production, resource production, resource haulers, resource visuals, reservation cleanup, and destroyed cleanup.
- Existing profiler marker scopes and diagnostics timing handoff.
- Existing direct construction from `BuildingGameplayCompositionSourceSystem`.

Test update:
- `BuildingPlacementRuntimeTickSystemTests` now constructs the helper directly instead of using `World.CreateSystemManaged`.
- The resource visual cadence assertion now matches the runtime contract: active production transports tick every simulation call, while resource visuals remain throttled by `ResourceVisualIntervalSeconds`.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `325`
- Production `SystemBase`/legacy declarations: `192`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `318`
- Agent D rows: `56`
- DirectConvert rows: `41`
- Open rows: `170`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-d-placement-runtime-tick-helper-fold.log`: passed, marker `[BuildingPlacementRuntimeTickFocusedValidation] result=Passed tests=3`
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only the unused `SystemBase` inheritance and disabled lifecycle.
- Resource visuals remain throttled at the existing cadence; the focused test was corrected to avoid relying on accidental time elapsed during ECS world creation.

Next guidance:
- Continue Agent D with the next low-risk fold/direct row before broader split-before-convert building spawn, production, and selection owners.
