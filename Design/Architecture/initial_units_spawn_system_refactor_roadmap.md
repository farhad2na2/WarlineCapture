# InitialUnitsSpawnSystem Refactor Roadmap

This document owns the `InitialUnitsSpawnSystem` refactor plan. The system is an ECS `ISystem`, but it currently mixes startup gating, progress state, resource projection, respawn queue setup, initial building/base request generation, unit and blocker spawning, air-platform placement, mission-specific roster overrides, completion gates, and diagnostics. The goal is behavior-preserving decomposition into narrow ECS/gameplay `*System` boundaries without changing startup results or loading-gate timing.

## Fixed Step Count

This roadmap has 36 steps. Do not append surprise steps after step 36. If new work is discovered, update the relevant existing step and keep the final validation gate as the last step.

## Target

Target file: `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs`

Current size at roadmap creation: 1236 lines. This is an observation, not a hard acceptance limit. The acceptance target is single responsibility and stable startup performance.

Final target: `InitialUnitsSpawnSystem` may remain only as the ECS initial-spawn tick that sequences startup gating, progress, building requests, unit/blocker batch spawning, completion, and diagnostics through narrow `*System` owners. It must not own resource projection, respawn queue projection, faction-base layout/request algorithms, configured building request policy, spawnable read-model lookup, air-platform slot policy, spawn-cell reservation/search policy, unit component setup, blocker spawning, M01 roster policy, completion fail-open policy, or diagnostic formatting. If the remaining type becomes pure pass-through after these steps, step 35 decides whether it should stay as the named ECS tick or be retired. No broad replacement shell may be introduced.

## Current Responsibility Inventory

- ECS startup gate: requires runtime gameplay state, grid config, dynamic occupancy, initial-spawn config, and building runtime boundary readiness before spawning.
- Progress lifecycle: creates `InitialUnitsSpawnProgress`, creates per-entry `InitialUnitsFactionUnitSpawnProgress`, tracks random state, resources applied, blocker progress, building request issuance, building completion wait frames, and initialization cleanup.
- Respawn queue projection: creates/configures respawn queue state and faction spawn-point buffers from initial faction spawn entries.
- Initial resources: projects initial dollars to `FactionEconomy` and creates default player economy policy if missing.
- Building/base requests: resolves configured/fallback spawnable ids, creates faction-base wall/gate/core placement requests, configured building requests, request ids, wall-run segments, overlap flags, and base-core camera focus.
- Completion gating: waits for initial buildings, supports M01 compact runtime skip, increments fail-open wait frames, and removes progress only when units/buildings/blockers are complete.
- Grid reservation and spawn search: snapshots walkable/dynamic blocked/occupied buffers, reserves static blockers, reserves existing unit footprints, finds unit/blocker cells, and handles air-unit platform preference.
- Unit spawn application: instantiates prefab units, applies grid/transform/faction/respawn/attack/move state, clears path/combat/wander components, advances entry progress, and preserves batch size.
- Custom Game source-key handling: matches source-key spawn metadata and skips unresolved source-key units without creating fallback impostor stand-ins.
- M01 compact policy: collapses the initial roster to one player and one enemy unit for the compact mission runtime.
- Blocker spawning: spawns configured blockers around the map center with existing spawn-cell utility and tracks blocker progress.
- Diagnostics: owns disabled-by-default spawn-state logs, duplicate-cell diagnostics, initial-spawn warnings, and freeze logs.

## Public/Internal Surface Hard Rule

`InitialUnitsSpawnSystem` must expose no public/internal helper API. It may expose only the ECS lifecycle methods required by `ISystem`:

- `public void OnCreate(ref SystemState state)`
- `public void OnUpdate(ref SystemState state)`

Retired private helper responsibilities must move to the target owners listed in the steps below. New public/static helper surface must not be added to `InitialUnitsSpawnSystem`. Static helpers are allowed only in extracted systems when they are pure deterministic math/data helpers with no runtime dependencies.

## Architecture Rules

- Do not replace `InitialUnitsSpawnSystem` with `InitialUnitsSpawnManager`, `InitialUnitsSpawnController`, `InitialUnitsSpawnFacade`, `InitialUnitsSpawnOrchestrator`, or another broad shell.
- New gameplay runtime types must be named `*System`, except existing `Config` assets, `Component`/`Entity` data, and Unity edge types.
- No singleton/static runtime access. Do not add direct mutable state on `InitialUnitsRuntimeState`; if compatibility remains temporarily required, isolate it to a narrow owner.
- Do not use reflection.
- Do not move initial-spawn behavior into UI, bootstrap, editor tooling, scene views, or config assets.
- Initial building/base spawning must keep using ECS building runtime boundary buffers and must not reintroduce `BuildingPlacementSystem`, `BuildingPlacementRuntimeComponent`, or managed placement facades.

## Performance And Behavior Rules

- Preserve `InitialSpawnBatchSize = 24`, `InitialBlockerBatchSize = 24`, `DiagnosticIntervalFrames = 120`, `InitialBaseCoreRequestEntryIndex = -100`, `MaxInitialBuildingCompletionWaitFrames = 300`, and `FreezeLogThresholdSeconds = 0.05`.
- Preserve the current play-request gate, required queries, update order, per-config processing order, `EntityCommandBuffer` playback timing, and cleanup timing for `InitialUnitsSpawnInitialized`, `InitialUnitsSpawnProgress`, and `InitialUnitsFactionUnitSpawnProgress`.
- Preserve random-state semantics: initialize from `InitialUnitsSpawnConfig.RandomSeed`, never allow zero state, update `RespawnQueueState.RandomState`, and keep current per-entry/per-frame spawn order.
- Preserve initial resource behavior: player faction `0` receives `InitialDollars`, missing player economy is created with disabled policy, and non-player economies are untouched.
- Preserve initial building/base behavior: faction-base request order, wall/gate/core fallback ids, configured building request semantics, request ids, `PlanEntity`, `EntryIndex`, wall overlap flags, and fail-open behavior must not change.
- Preserve Custom Game behavior: unresolved source-key units must be marked spawned and skipped; they must not create source-key fallback visual entities or 2D impostor stand-ins.
- Preserve M01 compact behavior: compact runtime skips initial building requests and keeps only one player unit plus one enemy unit.
- Preserve spawn-cell behavior: static blocker reservation, existing unit reservation, dynamic blocked/occupied checks, footprint handling, air platform preference, helipad/airport slot mapping, and blocker spawn center/radius policy must not change.
- Preserve diagnostic content and disabled-by-default gates. Hot startup paths must not construct diagnostic strings unless diagnostics or warning paths are enabled.
- Avoid new per-frame managed allocations in the active initial-spawn tick. Extraction must not add LINQ, reflection, scene searches, runtime asset loading, or additional persistent mutable static state.

## Required Validation Gates

Every implementation step must run:

- `git diff --check` scoped to touched files.
- Focused architecture validation once this roadmap's tests exist.

Every phase boundary must also run when feasible:

- `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation`.
- EditMode `CustomGameStartupSystemTests.RunBatchValidation` or the focused Custom Game startup tests that cover initial-spawn source-key and converted-prefab behavior.
- Focused initial-spawn tests added by this roadmap for resources, progress, building requests, air platform spawn, spawn-cell reservation, and completion gating.
- Runtime play-button/loading-gate smoke with `RuntimeFpsPlayButtonProbe.Run` when unit/blocker/building request order, startup gating, or completion timing changes.

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for Unity validation.

## Phase 1: Baseline, Contract, And Surface Freeze

1. Complete: Add architecture guard and batch validation
   - Add architecture contract wording that `InitialUnitsSpawnSystem` is a temporary mixed-responsibility ECS startup system that must shrink to initial-spawn tick ownership.
   - Add focused architecture validation entry point for this roadmap.
   - Guard the 36-step roadmap, target file, current responsibility inventory, forbidden broad replacement names, building runtime boundary requirement, and bounded public/internal surface.
   - Expected output: future changes cannot normalize or grow the mixed-responsibility system.
   - Added contract wording in `Design/Architecture/gameplay_solid_ecs_contract.md` and performance guardrails in `Design/Architecture/performance_regression_contract.md`.
   - Added `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation`.
   - Added guards for the fixed 36-step roadmap, target file, current size, final tick target, broad replacement shell ban, building runtime boundary requirement, constants/behavior anchors, and no public/internal helper surface beyond ECS lifecycle methods.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/performance_regression_contract.md` passed.
     - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation -logFile /private/tmp/warline-initial-units-spawn-step1-architecture.log` passed with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

2. Complete: Freeze public/internal helper surface
   - Inventory every public/internal member on `InitialUnitsSpawnSystem`.
   - Add or tighten a guard preventing new helper surface from being added to the system.
   - Expected output: `InitialUnitsSpawnSystem` exposes only `OnCreate` and `OnUpdate`.
   - Public/internal surface inventory:
     - `public void OnCreate(ref SystemState state)` remains the ECS lifecycle query/setup entrypoint.
     - `public void OnUpdate(ref SystemState state)` remains the ECS lifecycle tick entrypoint.
   - No other public/internal helper methods are allowed on `InitialUnitsSpawnSystem`; private helper responsibilities are assigned to later roadmap owner systems.
   - `GameplayArchitectureContractTests.InitialUnitsSpawnSystemBaselineMustStayExplicitUntilExtracted` enforces the two-member public/internal surface.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/performance_regression_contract.md` passed.
     - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation -logFile /private/tmp/warline-initial-units-spawn-step2-architecture.log` passed with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

3. Complete: Add deterministic behavior baseline
   - Document focused EditMode commands and expected outputs for Custom Game source-key skips, converted prefab spawning, initial resources, initial building requests, air-platform spawn, and completion gating.
   - Document runtime play-button/loading-gate smoke expectations.
   - Do not change runtime behavior in this step.
   - Expected output: later extraction steps have a behavior/performance comparison point.
   - Baseline Custom Game startup command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod CustomGameStartupSystemTests.RunBatchValidation -logFile /private/tmp/warline-initial-units-spawn-step3-custom-game.log`
   - Baseline initial faction base command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod InitialFactionBaseValidationTests.RunBatchValidation -logFile /private/tmp/warline-initial-units-spawn-step3-faction-base.log`
   - Baseline architecture command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation -logFile /private/tmp/warline-initial-units-spawn-step3-architecture.log`
   - Runtime smoke command for later behavior-changing steps:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeFpsPlayButtonProbe.Run -logFile /private/tmp/warline-initial-units-spawn-runtime-fps.log`
   - Expected Custom Game outputs:
     - `CustomGameStartupSystem` creates runtime singleton/config buffers in an empty world without starting mission tutorial runtime.
     - Source-key unit entries without converted ECS prefab entities are skipped and do not create fallback impostor stand-ins.
     - Converted prefab-backed unit entries spawn real prefab-backed units and do not route through source-key fallback visuals.
     - Runtime grid bootstrap creates buffers without invalidating handles.
   - Expected initial faction base outputs:
     - `InitialFactionBaseLayoutPlanner` builds the exact base recipe.
     - Scene initial-units config enables faction bases with real prefabs and minimum unit counts.
     - Building config resolves every initial base prefab.
     - Airport/helipad platform prefabs expose production spawn points.
     - Runtime base placement spawns required base buildings and helipad spawn resolver skips an occupied pad for the initial transport helicopter.
   - Expected runtime smoke outputs:
     - The Game scene play-button flow clears the loading gate after initial units/buildings/blockers complete or the existing fail-open policy triggers.
     - Initial units, faction bases, configured buildings, and blockers spawn without reintroducing source-key fallback visual entities for unresolved Custom Game units.
     - Steady-state FPS after warmup should remain comparable to the current runtime probe baseline; scene-load/import hitches are excluded.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/performance_regression_contract.md` passed.
     - Fixed `Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` so faction `1` has the same defended-base unit variety as faction `0`, preserving local base offsets.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `CustomGameStartupSystemTests.RunBatchValidation` exited `0` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; no explicit pass marker is emitted by that method, and the log contained only an expected `ThrowsConstraint` stack frame from the invalid-handle regression assertion.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.

## Phase 2: Query, Startup Gate, Progress, And Respawn Queue

4. Complete: Extract ECS query ownership
   - Create `InitialUnitsSpawnQuerySystem`.
   - Move building runtime boundary query creation and initial-spawn progress/init query construction out of the tick.
   - Preserve `RequireForUpdate` requirements.
   - Expected output: initial-spawn query shape has one owner.
   - Added `Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs`.
   - Moved building runtime boundary, pending-init, and progress query construction into `InitialUnitsSpawnQuerySystem.Context`.
   - `InitialUnitsSpawnSystem.OnCreate` now stores the query context and preserves the same `RequireForUpdate` requirements.
   - `InitialUnitsSpawnSystem.OnUpdate` now consumes `_queryContext.PendingInitQuery` and `_queryContext.ProgressQuery` instead of building those queries inside the tick.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

5. Complete: Extract startup and play-request gating
   - Create `InitialUnitsSpawnStartupGateSystem`.
   - Move play-request gate, M01 compact-runtime detection handoff, and boundary-entity availability shaping.
   - Preserve early-return order.
   - Expected output: the tick starts only when the gate result says initial spawning is actionable.
   - Added `Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs`.
   - Added `RuntimeGameplayStateQuery` to `InitialUnitsSpawnQuerySystem.Context` so startup play-request gating uses the query owner rather than direct singleton reach-through in the tick.
   - `InitialUnitsSpawnSystem.OnUpdate` now evaluates `_startupGateSystem.Evaluate(state.EntityManager, _queryContext)`, returns only when the gate result is not actionable, and reads M01 compact runtime and building boundary entity from the gate result.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

6. Complete: Extract progress initialization
   - Create `InitialUnitsSpawnProgressSystem`.
   - Move adding `InitialUnitsSpawnProgress` and `InitialUnitsFactionUnitSpawnProgress`.
   - Preserve random seed clamping and per-entry progress sizing.
   - Expected output: progress lifecycle is separate from spawn work.
   - Added `Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs`.
   - Moved pending init entity iteration, random seed clamping, `InitialUnitsSpawnProgress` creation, and per-entry `InitialUnitsFactionUnitSpawnProgress` buffer initialization into `InitialUnitsSpawnProgressSystem.InitializePending`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates pending progress initialization through `_progressSystem.InitializePending(em, _queryContext)` before reading active progress entities.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

7. Complete: Extract faction spawn snapshot
   - Create `InitialFactionSpawnSnapshotSystem`.
   - Move copying `InitialUnitsFactionSpawnEntry` buffers into temporary native snapshots.
   - Preserve allocation lifetime and faction order.
   - Expected output: downstream systems consume an explicit faction-spawn snapshot.
   - Added `Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs`.
   - Moved `InitialUnitsFactionSpawnEntry` buffer copying into `InitialFactionSpawnSnapshotSystem.Create`, preserving caller-provided allocator and original buffer order.
   - `InitialUnitsSpawnSystem.OnUpdate` now creates the faction snapshot through `_factionSpawnSnapshotSystem.Create(em, entity, Allocator.Temp)` and retains the same disposal point after batch processing.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs.meta Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

8. Complete: Extract respawn queue projection
   - Create `InitialRespawnQueueProjectionSystem`.
   - Move respawn queue creation/configuration and faction spawn-point buffer projection.
   - Preserve spawn radius, respawn delay, spawn-point order, and random-state writeback.
   - Expected output: respawn queue setup is not mixed with unit spawning.
   - Added `Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs`.
   - Moved respawn queue creation, spawn radius/delay projection, `RespawnFactionSpawnPoint` buffer clearing/rebuild, and queue random-state writeback into `InitialRespawnQueueProjectionSystem`.
   - `InitialUnitsSpawnSystem.OnUpdate` now uses `_respawnQueueProjectionSystem.GetOrCreateQueue`, `ProjectInitialConfig`, and `WriteRandomState` while preserving the original queue creation point and faction spawn order.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs.meta Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs.meta Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

## Phase 3: Resources, Building Requests, And Completion

9. Complete: Extract initial resource projection
   - Create `InitialSpawnResourceSystem`.
   - Move `FactionEconomy` lookup/create and initial-dollar projection.
   - Preserve player faction `0` behavior and disabled default policy.
   - Expected output: resource startup ownership is isolated.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs`.
   - Moved `FactionEconomy` lookup/update, default player economy creation, and disabled `FactionEconomyPolicy` creation into `InitialSpawnResourceSystem.ApplyInitialTotals`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates initial-dollar projection through `_resourceSystem.ApplyInitialTotals(em, config)` when `InitialResourcesApplied` is still unset.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs.meta Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs.meta Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs.meta Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

10. Complete: Extract building runtime boundary access
   - Create `InitialBuildingBoundarySystem`.
   - Move boundary entity lookup and read/write buffer precondition checks.
   - Preserve ECS building runtime boundary buffer usage.
   - Expected output: no building request code reaches through the main tick.
   - Added `Assets/Game/Scripts/Systems/InitialBuildingBoundarySystem.cs`.
   - Moved runtime spawn request buffer, configured spawnable read-model buffer, and faction production spawn-point read-model buffer precondition/access helpers into `InitialBuildingBoundarySystem`.
   - Initial building completion, request enqueueing, spawnable read-model resolution, and air-platform spawn lookup now access building runtime boundary buffers through `InitialBuildingBoundarySystem`.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs.meta Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs.meta Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs.meta Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs.meta Assets/Game/Scripts/Systems/InitialBuildingBoundarySystem.cs Assets/Game/Scripts/Systems/InitialBuildingBoundarySystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

11. Complete: Extract spawnable id/read-model resolution
   - Create `InitialBuildingSpawnableSystem`.
   - Move configured/fallback key normalization and `BuildingConfiguredSpawnableReadModel` lookup.
   - Preserve current fallback ids and ordinal string behavior.
   - Expected output: spawnable lookup has one owner.
   - Added `Assets/Game/Scripts/Systems/InitialBuildingSpawnableSystem.cs`.
   - Moved configured/fallback spawnable id resolution, building id normalization, and configured spawnable read-model scanning into `InitialBuildingSpawnableSystem`.
   - Faction base request planning and configured initial building request planning now use `InitialBuildingSpawnableSystem` instead of local spawnable helper methods.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnStartupGateSystem.cs.meta Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnProgressSystem.cs.meta Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs Assets/Game/Scripts/Systems/InitialFactionSpawnSnapshotSystem.cs.meta Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs Assets/Game/Scripts/Systems/InitialRespawnQueueProjectionSystem.cs.meta Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs Assets/Game/Scripts/Systems/InitialSpawnResourceSystem.cs.meta Assets/Game/Scripts/Systems/InitialBuildingBoundarySystem.cs Assets/Game/Scripts/Systems/InitialBuildingBoundarySystem.cs.meta Assets/Game/Scripts/Systems/InitialBuildingSpawnableSystem.cs Assets/Game/Scripts/Systems/InitialBuildingSpawnableSystem.cs.meta Design/Architecture/initial_units_spawn_system_refactor_roadmap.md Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.
     - `CustomGameStartupSystemTests.RunBatchValidation` exited `0` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing stale validation-worktree copies of `CustomGameStartupSystemTests.cs`, `CustomGameStartupSystem.cs`, `Game.unity`, and `BuildScript.cs` to match the current source workspace.

12. Complete: Extract faction base request planning
   - Create `InitialFactionBaseRequestSystem`.
   - Move base wall/gate/core preconditions, placement-id/model collection, footprint resolution, and base request sequencing.
   - Preserve `CreateFactionBases`, fallback ids, gate gap, placement order, and core request entry index.
   - Expected output: base creation is a narrow building-request owner.
   - Added `Assets/Game/Scripts/Systems/InitialFactionBaseRequestSystem.cs`.
   - Moved faction-base wall/gate/core spawnable preconditions, placement id/model collection, footprint resolution, gate gap calculation, flank/wall/core request sequencing, and core request entry-index routing into `InitialFactionBaseRequestSystem.Enqueue`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates faction-base request creation through `_factionBaseRequestSystem.Enqueue(state.EntityManager, boundaryEntity, entity, config, baseGrid, factionSpawns, InitialBaseCoreRequestEntryIndex, out baseRequestCount)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialFactionBaseRequestSystem.cs Assets/Game/Scripts/Systems/InitialFactionBaseRequestSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.

13. Complete: Extract wall-run segment request creation
   - Create `InitialBaseWallRunRequestSystem`.
   - Move wall-run origin generation and segment request enqueueing.
   - Preserve `BuildingPlacementCommitCompositionSystemHelper.BuildWallRunOrigins` behavior and wall orientation decisions.
   - Expected output: wall-run request logic no longer lives in the tick.
   - Added `Assets/Game/Scripts/Systems/InitialBaseWallRunRequestSystem.cs`.
   - Moved wall-run orientation normalization, footprint selection, `BuildingPlacementCommitCompositionSystemHelper.BuildWallRunOrigins` expansion, and wall-segment request enqueueing into `InitialBaseWallRunRequestSystem.Enqueue`.
   - `InitialFactionBaseRequestSystem.Enqueue` now delegates wall-run segment request creation through `new InitialBaseWallRunRequestSystem().Enqueue(...)` while retaining faction-base layout and flank/core planning ownership.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialFactionBaseRequestSystem.cs Assets/Game/Scripts/Systems/InitialBaseWallRunRequestSystem.cs Assets/Game/Scripts/Systems/InitialBaseWallRunRequestSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.

14. Complete: Extract configured initial building requests
   - Create `InitialConfiguredBuildingRequestSystem`.
   - Move `InitialUnitsFactionBuildingSpawnEntry` request creation and unresolved-entry diagnostics.
   - Preserve no-faction-spawn and unresolved-spawnable skip behavior.
   - Expected output: authored initial building requests have one owner.
   - Added `Assets/Game/Scripts/Systems/InitialConfiguredBuildingRequestSystem.cs`.
   - Moved authored `InitialUnitsFactionBuildingSpawnEntry` scanning, faction-spawn lookup, configured spawnable read-model validation, warning diagnostics, and building request enqueueing into `InitialConfiguredBuildingRequestSystem.Enqueue`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates authored initial building requests through `_configuredBuildingRequestSystem.Enqueue(...)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialConfiguredBuildingRequestSystem.cs Assets/Game/Scripts/Systems/InitialConfiguredBuildingRequestSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.

15. Complete: Extract completed building request processing
   - Create `InitialBuildingCompletionSystem`.
   - Move request status scanning, pending/succeeded handling, request removal, and base-core camera focus.
   - Preserve `PlanEntity`, `EntryIndex`, camera focus world calculation, and M01 exclusion.
   - Expected output: building request completion is isolated.
   - Added `Assets/Game/Scripts/Systems/InitialBuildingCompletionSystem.cs`.
   - Moved runtime spawn request status scanning, pending/succeeded handling, request removal, and base-core camera focus calculation into `InitialBuildingCompletionSystem.Process`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates completed initial-building request processing through `_buildingCompletionSystem.Process(...)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialBuildingCompletionSystem.cs Assets/Game/Scripts/Systems/InitialBuildingCompletionSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.

16. Complete: Extract initial-spawn completion gate
   - Create `InitialSpawnCompletionSystem`.
   - Move `CanCompleteInitialSpawn`, `RequiresInitialBuildingCompletion`, fail-open wait-frame policy, and final initialized/progress cleanup request shaping.
   - Preserve wait-frame limit and warning content.
   - Expected output: completion policy no longer mixes with spawn loops.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnCompletionSystem.cs`.
   - Moved initial building completion requirement checks, fail-open wait-frame handling, fail-open warning, and final initialized/progress cleanup ECB request shaping into `InitialSpawnCompletionSystem.Update`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates completion gating through `_completionSystem.Update(...)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialSpawnCompletionSystem.cs Assets/Game/Scripts/Systems/InitialSpawnCompletionSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.
     - `CustomGameStartupSystemTests.RunBatchValidation` exited `0` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; the log still includes expected warning/error stack lines from existing Custom Game validation scenarios.
     - `RuntimeFpsPlayButtonProbe.Run` exited `0` and wrote `result=completed`, `clickedGameButton=true`, `avgFps=161.5` to `/private/tmp/warlinecapture-runtime-fps-probe.json`; the smoke log also emitted repeated duplicate `GridConfig` singleton exceptions from the wider runtime scene, so this smoke is recorded as completed-with-runtime-errors rather than a clean pass.

## Phase 4: Grid Reservation, Spawn Search, And Mission Overrides

17. Complete: Extract grid spawn context
   - Create `InitialSpawnGridContextSystem`.
   - Move `GridConfig`, walkable, dynamic-blocked, occupied, and reserved-bit-array setup.
   - Preserve allocator lifetime and grid buffer access order.
   - Expected output: unit and blocker systems receive explicit grid context.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnGridContextSystem.cs`.
   - Added `InitialUnitsSpawnQuerySystem.Context.GridContextQuery` for `GridConfig`, `GridWalkable`, `DynamicBlockerData`, and `DynamicOccupancyData`.
   - Moved grid config lookup, grid walkable buffer access, dynamic blocked/occupied bit-array access, reserved bit-array allocation, and reserved bit-array disposal into `InitialSpawnGridContextSystem.Context`.
   - `InitialUnitsSpawnSystem.OnUpdate` now uses `_gridContextSystem.TryGetGridConfig(...)` for initial building requests/completion and `_gridContextSystem.TryCreate(...)` for unit/blocker spawn context.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs Assets/Game/Scripts/Systems/InitialSpawnGridContextSystem.cs Assets/Game/Scripts/Systems/InitialSpawnGridContextSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.
     - `RuntimeFpsPlayButtonProbe.Run` exited `0` and wrote `result=completed`, `clickedGameButton=true`, `avgFps=64.0` to `/private/tmp/warlinecapture-runtime-fps-probe.json`; initial-spawn grid access no longer reports the duplicate `GridConfig` exception, but the smoke log still reports duplicate-grid singleton exceptions in other systems such as `InitialUnitsBlockerChurnSystem`, `UnitEngagementSystem`, and `UnitAirMovementSystem`.

18. Complete: Extract reserved-footprint projection
   - Create `InitialSpawnReservationSystem`.
   - Move static blocker and existing unit footprint reservation.
   - Preserve in-bounds checks, center-vs-origin footprint semantics, and `StaticGridBlocker` exclusions.
   - Expected output: reservation policy has one owner.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnReservationSystem.cs`.
   - Moved static blocker origin/size reservation and existing non-static unit center-footprint reservation into `InitialSpawnReservationSystem`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates reserved-cell projection through `_reservationSystem.ReserveStaticBlockerFootprints(...)` and `_reservationSystem.ReserveExistingUnitFootprints(...)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialSpawnReservationSystem.cs Assets/Game/Scripts/Systems/InitialSpawnReservationSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

19. Complete: Extract unit spawn-cell search
   - Create `InitialUnitSpawnCellSystem`.
   - Move ground and air-unit spawn-cell search, including direct air-cell reservation.
   - Preserve walkable/occupied/reserved/blocked rules and fallback to `SpawnCellUtility.TryFindSpawnCellNear`.
   - Expected output: unit spawn placement can be tested without the full tick.
   - Added `Assets/Game/Scripts/Systems/InitialUnitSpawnCellSystem.cs`.
   - Moved direct air-cell reservation and ground fallback spawn-cell search into `InitialUnitSpawnCellSystem.TryFindInitialUnitSpawnCell`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates unit spawn-cell search through `_unitSpawnCellSystem.TryFindInitialUnitSpawnCell(...)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnCellSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnCellSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

20. Complete: Extract air platform spawn policy
   - Create `InitialAirPlatformSpawnSystem`.
   - Move helipad/airport selection, slot-index mapping, spawn-point buffer scan, and platform world-position output.
   - Preserve configured offset thresholds and faction/building-id filtering.
   - Expected output: initial air-unit placement reads the building production spawn-point read model through one owner.
   - Added `Assets/Game/Scripts/Systems/InitialAirPlatformSpawnSystem.cs`.
   - Moved helipad/airport selection, slot-index mapping, faction/building-id filtering, spawn-point buffer scan, in-bounds checks, and platform world-position output into `InitialAirPlatformSpawnSystem`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates air-unit platform placement through `_airPlatformSpawnSystem.TryGetInitialAirPlatformSpawn(...)`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialAirPlatformSpawnSystem.cs Assets/Game/Scripts/Systems/InitialAirPlatformSpawnSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.

21. Complete: Extract M01 compact roster policy
   - Create `InitialMissionRosterSystem`.
   - Move compact runtime building skip and unit roster reduction.
   - Preserve one-player/one-enemy behavior and zeroing semantics.
   - Expected output: mission-specific policy no longer lives in the generic tick.
   - Added `Assets/Game/Scripts/Systems/InitialMissionRosterSystem.cs`.
   - Moved M01 compact initial-building skip policy into `InitialMissionRosterSystem.ShouldSkipInitialBuildingRequests`.
   - Moved compact runtime unit roster reduction into `InitialMissionRosterSystem.ApplyM01CompactUnitRoster`, preserving first player/first enemy selection, count zeroing, spawn-offset zeroing, and spawned-progress zeroing for removed entries.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates M01 compact building skip and roster trimming through `_missionRosterSystem`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialMissionRosterSystem.cs Assets/Game/Scripts/Systems/InitialMissionRosterSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

## Phase 5: Unit And Blocker Spawn Application

22. Complete: Extract Custom Game source-key policy
   - Create `InitialUnitSourceKeySystem`.
   - Move source-key metadata match and unresolved-source-key skip handling.
   - Preserve no-fallback-impostor behavior and warning content.
   - Expected output: source-key compatibility behavior is explicit and testable.
   - Added `Assets/Game/Scripts/Systems/InitialUnitSourceKeySystem.cs`.
   - Moved Custom Game source-key metadata matching into `InitialUnitSourceKeySystem.TryGetCustomGameUnitSourceKey`.
   - Moved missing-prefab skip behavior and the unresolved source-key warning into `InitialUnitSourceKeySystem.TrySkipMissingPrefabUnit`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates source-key matching and unresolved-prefab skip handling through `_sourceKeySystem`, preserving the no-fallback behavior.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitSourceKeySystem.cs Assets/Game/Scripts/Systems/InitialUnitSourceKeySystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

23. Complete: Extract unit spawn batch planning
   - Create `InitialUnitSpawnBatchSystem`.
   - Move per-entry remaining count, batch cap, faction spawn-cell lookup, footprint/air checks, and progress advancement request shaping.
   - Preserve unit entry order and batch size.
   - Expected output: the tick no longer owns the unit spawn loop.
   - Added `Assets/Game/Scripts/Systems/InitialUnitSpawnBatchSystem.cs`.
   - Moved per-entry remaining/count batch planning into `InitialUnitSpawnBatchSystem.TryCreateEntryBatch`.
   - Moved faction spawn-cell lookup, unit spawn center calculation, prefab footprint lookup, and air-unit detection into `InitialUnitSpawnBatchSystem.TryCreateSpawnPlan`.
   - Moved spawned-progress writeback and remaining-batch decrement into `InitialUnitSpawnBatchSystem.ApplySpawnedCount`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates unit batch planning, spawn-plan creation, and progress advancement through `_unitSpawnBatchSystem`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnBatchSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnBatchSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

24. Complete: Extract spawned-unit component setup
   - Create `InitialUnitSpawnApplySystem`.
   - Move instantiate plus grid/transform/prev-position/move-state/faction/respawn/attack setup.
   - Preserve set-vs-add behavior based on prefab components.
   - Expected output: spawned unit initialization is owned by one system.
   - Added `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs`.
   - Moved unit instantiation and grid/transform/previous-position/move-state/faction/respawn/attack component initialization into `InitialUnitSpawnApplySystem.InstantiateAndConfigureSpawnedUnit`.
   - Moved set-vs-add component behavior into `InitialUnitSpawnApplySystem.SetOrAddComponent`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates spawned-unit instantiation and base component setup through `_unitSpawnApplySystem`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

25. Complete: Extract spawned-unit runtime reset
   - Create `InitialUnitSpawnResetSystem`.
   - Move idle-wander initialization and removal of path/combat/wander request components.
   - Preserve reset component list and random-state usage.
   - Expected output: post-spawn runtime cleanup is separate from placement search.
   - Added `Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs`.
   - Moved idle-wander initialization and random-state advancement into `InitialUnitSpawnResetSystem.ResetSpawnedUnitRuntimeState`.
   - Moved the spawned-unit runtime reset list (`UnitPathFollow`, `UnitPathRange`, `EngageTarget`, `UnitPathRequest`, `UnitTarget`, `AutoWanderMoveTag`) into `InitialUnitSpawnResetSystem`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates spawned-unit runtime reset side effects through `_unitSpawnResetSystem`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

26. Complete: Extract blocker spawn batch
   - Create `InitialBlockerSpawnSystem`.
   - Move blocker target count, blocker batch cap, center/radius search, instantiate, grid, and transform setup.
   - Preserve M01 zero-blocker behavior and blocker progress updates.
   - Expected output: blocker spawning is independent of unit spawning.
   - Added `Assets/Game/Scripts/Systems/InitialBlockerSpawnSystem.cs`.
   - Moved blocker target-count calculation, batch cap, spawn-cell search, diagnostics, instantiation, grid component setup, and transform setup into `InitialBlockerSpawnSystem.SpawnBatch`.
   - Preserved M01 zero-blocker behavior and the existing planned-progress increment semantics through `InitialBlockerSpawnSystem.Result`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates blocker spawning through `_blockerSpawnSystem` and uses the returned target/progress values for completion.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialBlockerSpawnSystem.cs Assets/Game/Scripts/Systems/InitialBlockerSpawnSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

27. Complete: Extract structural playback boundary
   - Create `InitialSpawnStructuralApplySystem`.
   - Move `EntityCommandBuffer` lifetime/playback/dispose and final cleanup component requests.
   - Preserve playback order after unit/blocker spawn and before diagnostics.
   - Expected output: structural mutation order has one owner.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnStructuralApplySystem.cs`.
   - Moved initial-spawn command-buffer creation into `InitialSpawnStructuralApplySystem.Create`.
   - Moved command-buffer playback and disposal into `InitialSpawnStructuralApplySystem.PlaybackAndDispose`.
   - `InitialUnitsSpawnSystem.OnUpdate` now uses `InitialSpawnStructuralApplySystem.Context` while preserving playback before diagnostics and after unit/blocker/completion structural requests.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialSpawnStructuralApplySystem.cs Assets/Game/Scripts/Systems/InitialSpawnStructuralApplySystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

## Phase 6: Diagnostics, Tests, Performance, And Ownership Decision

28. Complete: Extract spawn-state diagnostics
   - Create `InitialSpawnDiagnosticStateSystem`.
   - Move diagnostic cadence and spawn-state query count collection.
   - Preserve disabled-by-default behavior and diagnostic interval.
   - Expected output: diagnostic cadence state is not stored on the tick.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnDiagnosticStateSystem.cs`.
   - Moved spawn-state diagnostic cadence storage and query-count collection into `InitialSpawnDiagnosticStateSystem.LogSpawnState`.
   - Preserved the `DiagnosticIntervalFrames = 120` call path and disabled-by-default execution gate in `InitialUnitsSpawnSystem`.
   - `InitialUnitsSpawnSystem.OnUpdate` now delegates spawn-state diagnostics through `_diagnosticStateSystem`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialSpawnDiagnosticStateSystem.cs Assets/Game/Scripts/Systems/InitialSpawnDiagnosticStateSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

29. Complete: Migrate initial-spawn diagnostics to ECS logging boundary
   - Create `InitialSpawnDiagnosticLogComponents`, `InitialSpawnDiagnosticLogSystem`, and `InitialSpawnDiagnosticLogFlushSystem`.
   - Route warnings/freeze/spawn-state/duplicate diagnostics through the ECS diagnostic log boundary where feasible.
   - Preserve warning content and gated string construction.
   - Expected output: direct Unity logging is isolated to a shell-edge flush system or documented unavoidable warning path.
   - Added `Assets/Game/Scripts/Components/InitialSpawnDiagnosticLogComponents.cs`.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogSystem.cs`.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogFlushSystem.cs`.
   - Routed initial-spawn no-free-cell, blocker, source-key, configured-building, fail-open completion, spawn-state, duplicate-cell, and freeze diagnostics through `InitialSpawnDiagnosticLogSystem`.
   - Kept Unity `Debug.Log*` calls isolated to `InitialSpawnDiagnosticLogFlushSystem`, which flushes the ECS diagnostic log buffer after `InitialUnitsSpawnSystem`.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialConfiguredBuildingRequestSystem.cs Assets/Game/Scripts/Systems/InitialSpawnCompletionSystem.cs Assets/Game/Scripts/Systems/InitialBlockerSpawnSystem.cs Assets/Game/Scripts/Systems/InitialUnitSourceKeySystem.cs Assets/Game/Scripts/Systems/InitialSpawnDiagnosticStateSystem.cs Assets/Game/Scripts/Components/InitialSpawnDiagnosticLogComponents.cs Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogSystem.cs Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogFlushSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

30. Complete: Extract duplicate-cell diagnostics
   - Create `InitialSpawnDuplicateCellDiagnosticSystem`.
   - Move duplicate center/footprint scan and sample formatting.
   - Preserve diagnostics-only execution and sample-length cap behavior.
   - Expected output: duplicate diagnostics cannot expand the spawn tick.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnDuplicateCellDiagnosticSystem.cs`.
   - Moved duplicate center/footprint scanning, occupied-cell counting, and sample formatting into `InitialSpawnDuplicateCellDiagnosticSystem.LogInitialSpawnCellDuplicates`.
   - Preserved diagnostics-only invocation from `InitialUnitsSpawnSystem` and the existing `samples.Length < 430` cap.
   - Duplicate-cell diagnostic output now routes through the step-29 initial-spawn ECS diagnostic log boundary.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialSpawnDuplicateCellDiagnosticSystem.cs Assets/Game/Scripts/Systems/InitialSpawnDuplicateCellDiagnosticSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

31. Complete: Extract freeze/performance diagnostics
   - Create `InitialSpawnFreezeDiagnosticSystem`.
   - Move freeze threshold, elapsed timing, and freeze log message shaping.
   - Preserve disabled-by-default behavior and threshold value.
   - Expected output: startup hitch logging is isolated.
   - Added `Assets/Game/Scripts/Systems/InitialSpawnFreezeDiagnosticSystem.cs`.
   - Moved freeze diagnostics start-time capture into `InitialSpawnFreezeDiagnosticSystem.BeginFrame`.
   - Moved elapsed-time calculation, disabled-by-default gate, `FreezeLogThresholdSeconds = 0.05d`, and freeze log message shaping into `InitialSpawnFreezeDiagnosticSystem.LogIfExceeded`.
   - Freeze diagnostics now route through the step-29 initial-spawn ECS diagnostic log boundary.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs Assets/Game/Scripts/Systems/InitialSpawnFreezeDiagnosticSystem.cs Assets/Game/Scripts/Systems/InitialSpawnFreezeDiagnosticSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

32. Complete: Add focused resource/building/source-key tests
   - Add or update tests for initial dollars, missing economy creation, base/building request buffers, Custom Game unresolved source-key skip, and converted prefab spawning.
   - Expected output: resource and building request extractions have deterministic coverage.
   - Added `Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs`.
   - Added `InitialSpawnResourceSystem_CreatesPlayerEconomyWithConfiguredDollars` for initial dollars, missing economy creation, and disabled economy policy.
   - Added `InitialConfiguredBuildingRequestSystem_QueuesRuntimeSpawnRequestFromReadModel` for configured building request buffer output.
   - Added `InitialUnitSourceKeySystem_SkipsUnresolvedSourceKeyWithoutFallback` for Custom Game unresolved source-key skip and warning enqueue behavior.
   - Added `InitialUnitSpawnApplySystem_InstantiatesConvertedPrefabBackedUnit` for converted prefab-backed unit instantiation and core component setup.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialUnitsSpawnFocusedTests.RunResourceBuildingSourceKeyBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnFocusedValidation] result=Passed group=ResourceBuildingSourceKey methods=4`.

33. Complete: Add focused spawn-cell/progress/completion tests
   - Add tests for progress initialization, spawn batch size, reserved footprint rejection, air-platform slot mapping, blocker spawn progress, completion wait, and fail-open.
   - Expected output: spawn placement and loading-gate behavior can be refactored safely.
   - Added `InitialUnitsSpawnProgressSystem_InitializesProgressAndUnitEntries` for random-state clamping and per-entry progress buffer initialization.
   - Added `InitialUnitSpawnBatchSystem_ClampsEntryToInitialSpawnBatchSize` for preserving the 24-unit batch cap.
   - Added `InitialUnitSpawnCellSystem_RejectsReservedFootprint` for reserved-footprint rejection.
   - Added `InitialAirPlatformSpawnSystem_ResolvesConfiguredHelipadSlot` for configured helipad slot mapping through the building runtime boundary read-model.
   - Added `InitialBlockerSpawnSystem_PreservesPlannedProgressIncrement` for blocker target/progress accounting when the configured prefab is unavailable.
   - Added `InitialSpawnCompletionSystem_WaitsThenFailOpens` for loading-gate wait and fail-open completion behavior.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialUnitsSpawnFocusedTests.RunSpawnProgressCompletionBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnFocusedValidation] result=Passed group=SpawnProgressCompletion methods=6`.

34. Complete: Performance and allocation audit
   - Audit hot startup allocations, Native container lifetimes, structural-change counts, diagnostic string construction, and managed collection use in extracted initial-spawn systems.
   - Record runtime FPS/loading-gate expectations in this roadmap.
   - Expected output: refactor proves it did not trade architecture for startup hitches.
   - Audit result:
     - `InitialUnitsSpawnSystem.cs` is now 314 lines and remains the only initial-spawn ECS tick owner.
     - Preserved startup constants: `InitialSpawnBatchSize = 24`, `InitialBlockerBatchSize = 24`, `DiagnosticIntervalFrames = 120`, `InitialBaseCoreRequestEntryIndex = -100`, `MaxInitialBuildingCompletionWaitFrames = 300`, and freeze threshold `0.05d`.
     - No `Allocator.Persistent`, LINQ, reflection, scene searches, or runtime asset loading were introduced in the initial-spawn extracted systems.
     - Hot Native containers remain `Allocator.Temp` scoped and disposed: progress/config entity arrays, grid reserved bitsets, faction spawn snapshots, duplicate-cell diagnostic hash sets, and diagnostic flush entity arrays.
     - Structural changes remain centralized through `InitialSpawnStructuralApplySystem` with one `EntityCommandBuffer` per active config processing pass, preserving playback timing.
     - Managed `List`/`Dictionary` allocations remain only in one-time faction-base request planning before `InitialBuildingRequestsIssued` is set; they are not steady-state per-frame allocations after base requests are issued.
     - Diagnostic string construction remains gated by disabled-by-default diagnostics or warning/fail-open paths; ECS diagnostic log flushing is isolated in `InitialSpawnDiagnosticLogFlushSystem`.
   - Runtime FPS/loading-gate expectation:
     - The Game scene play-button loading gate should still clear when all units, blockers, and initial buildings complete, or when the existing building completion fail-open reaches `MaxInitialBuildingCompletionWaitFrames`.
     - Steady-state FPS after initial spawn warmup should remain comparable to the current baseline; startup/import hitches and editor compilation are excluded from the comparison.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

35. Complete: Final tick ownership decision
   - Decide whether `InitialUnitsSpawnSystem` remains as the named ECS initial-spawn tick or is retired after all responsibilities move.
   - If it remains, enforce that it exposes only lifecycle methods and delegates to narrow systems.
   - If it is retired, delete the file and update all references.
   - Expected output: no broad facade remains.
   - Decision: keep `InitialUnitsSpawnSystem` as the named ECS initial-spawn tick.
   - Rationale:
     - The remaining type owns startup update order, required ECS queries, constants, and lifecycle sequencing across the extracted narrow systems.
     - Retiring it now would require another tick owner for the same update order; that would either be a broad replacement shell, which the roadmap forbids, or unnecessary churn before feature work.
     - The system no longer owns resource projection, building/base request algorithms, spawn-cell algorithms, unit component setup, blocker spawning, completion fail-open policy, or diagnostic formatting.
   - Enforcement:
     - `InitialUnitsSpawnSystem` remains bounded to `public void OnCreate(ref SystemState state)` and `public void OnUpdate(ref SystemState state)`.
     - New helper surface must stay in narrow `*System` owners, not return to the tick.
     - The tick may keep constants that define startup cadence/order because they are part of the ECS initial-spawn scheduling contract.
   - Validation:
     - `git diff --check -- Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.

36. Complete: Validation gate
   - Run architecture validation, focused initial-spawn tests, Custom Game startup tests, runtime play-button/loading-gate smoke, and FPS probe when feasible.
   - Update this roadmap with exact validation commands and results.
   - Expected output: compile-clean, behavior-preserving, no loading-gate regression, and no architecture allowlist debt remains.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/Initial*.cs Assets/Game/Scripts/Components/Initial*.cs Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/initial_units_spawn_system_refactor_roadmap.md` passed.
     - `GameplayArchitectureContractTests.RunInitialUnitsSpawnArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnArchitectureValidation] result=Passed methods=4`.
     - `InitialUnitsSpawnFocusedTests.RunResourceBuildingSourceKeyBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnFocusedValidation] result=Passed group=ResourceBuildingSourceKey methods=4`.
     - `InitialUnitsSpawnFocusedTests.RunSpawnProgressCompletionBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialUnitsSpawnFocusedValidation] result=Passed group=SpawnProgressCompletion methods=6`.
     - `CustomGameStartupSystemTests.RunBatchValidation` exited `0` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; the log contained the existing no-converted-prefab Custom Game warning and expected `ThrowsConstraint` stack frame from the invalid-handle regression assertion.
     - `InitialFactionBaseValidationTests.RunBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[InitialFactionBaseValidation] result=Passed`.
     - `RuntimeFpsPlayButtonProbe.Run` completed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `[RuntimeFpsPlayButtonProbe] result=completed avgFps=62.8 minFps=0.1 maxFps=133.4 logs=2999 output=/private/tmp/warlinecapture-runtime-fps-probe.json`; the probe clicked the Game button and did not time out on the loading gate.
   - Known validation gaps:
     - Runtime smoke logged repeated duplicate-`GridConfig` singleton exceptions from existing non-initial-spawn systems such as `InitialUnitsBlockerChurnSystem`, `UnitEngagementSystem`, and `UnitAirMovementSystem` when the scene had two `GridConfig` entities.
     - Runtime smoke logged a Unity QuickSearch `ArgumentOutOfRangeException` during editor indexing.
     - These runtime smoke gaps are recorded for a follow-up grid-query cleanup task; they did not prevent the play-button probe from completing and are outside the initial-spawn decomposition surface.
