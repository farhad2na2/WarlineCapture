# ECS Architecture Performance Quality Improvement Tracker

## Purpose

Track implementation of the suggested improvements from:

- `Design/AgentReports/2026-06-18_audit_unity-ecs-architecture-performance-quality.md`

This tracker turns the audit into a safe implementation order. The audit is directionally useful, but some recommendations must be constrained by the existing architecture contracts:

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`

Do not merge this work into the paused five-SystemBase split tracker. This tracker is for project-wide performance, architecture, and quality improvements.

## Progress Snapshot

- Checklist progress: `40 / 94 complete (42.6%)`.
- In progress: `0`.
- Remaining open: `54`.
- Current target: `Quick-win scope complete; deeper audit phases deferred`.
- Quick-win estimate: `3-5 working days`.
- Medium-term audit estimate: `5-8 working weeks`.
- Long-term architecture vision estimate: `2-4 months`.
- Validation status: `git diff --check` passed; Unity batchmode compile passed after quick-win settings cleanup; main project validation was available, so shadow-project fallback was not needed; baseline render-budget, production, movement, transport, and armory/UI validations passed; Phase 1 camera, building visual, marker shadow/rendering, and quality-setting smoke validations passed; building placement command validation passed after wall-preview scratch-list cleanup and focused scratch-list coverage; vehicle visual adornments validation passed after health-bar expiry scheduling; transport validation passed after deploy-order and airdrop lookup array-snapshot cleanup; selection input validation passed after attack-target, board-target, deselect-all, move-target, and immediate-selected array-snapshot cleanup; building UI query validation passed after boundary lookup cleanup; scan intel focused validation passed after selected-source lookup cleanup; Phase 2 focused rendering budget, health, animation, minimap, production, and movement validations passed; render-budget `.Run()` helpers were evaluated, left synchronous pending an output-container redesign, and revalidated; minimap marker filtering was evaluated and left unfiltered to preserve per-frame HUD rebuild behavior; UnitAnimationIndexSystem validation passed after removing disabled freeze-diagnostic counter allocation; health/animation scheduling and change-filter candidates were evaluated and left synchronous/unfiltered where immediate refresh is required; UnitMoveTargetDiagnosticSystem scratch cleanup passed diagnostics focused validation; BuildingCombatSystem cleanup scratch passed focused validation; runtime Systems `ToEntityArray`/`ToComponentDataArray` grep is clean; architecture runner now fails on separate existing non-Burst `OnUpdate` debt (`Current=256`, `ceiling=23`).
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.

## Agreement Assessment

| Audit area | Decision | Reason |
| --- | --- | --- |
| P4 cached collections | Agree, do soon | Low-risk GC reduction in known hot paths. |
| P1 `.Run()` to scheduled jobs | Agree selectively | Data-parallel unmanaged jobs should schedule; managed/UI/visual paths may need to stay main-thread or be split first. |
| P7 `WithChangeFilter` | Agree selectively | Good for health/animation updates; unsafe where markers/UI need forced refreshes. |
| P9 shadows | Agree for mobile | Reduces GPU cost; PC quality should be visually checked before aggressive reduction. |
| Q5 `.DS_Store` cleanup | Agree | Repository hygiene. |
| Q2 test runner exits | Agree | Current static validation runners should not kill the Editor Test Runner. |
| P3 managed `IComponentData` | Agree with constraints | Replace with baked entity refs, blob data, or passive managed boundaries. Do not add static mutable gameplay registries. |
| P2 `Object.Instantiate` | Agree with constraints | Convert gameplay/entity spawns first; leave UI, editor-only, and one-time setup unless hot. Pool visual markers. |
| P6 Burst coverage | Agree with constraints | Add Burst only after unmanaged access is proven. |
| A1 `SystemBase` migration | Agree with constraints | Convert hot unmanaged data systems; keep UI/camera/prefab/scene boundaries managed. |
| P5 transport split | Agree | The file is too broad; split in behavior-preserving slices. |
| A5 IL2CPP | Defer | Release-build decision; requires full build validation and affects iteration time. |
| A7 run in background | Defer | Product decision; can change pause/alt-tab behavior. |
| Addressables | Defer | Larger asset-loading strategy; not an audit quick win. |

## Estimate

| Work band | Scope | Estimate |
| --- | --- | --- |
| Quick wins | Baseline, `.DS_Store`, mobile shadows, cached collections, first safe scheduling/change-filter passes. | `3-5 working days` |
| Medium-term audit | Managed component reduction, instantiation/pooling, transport split, test-runner exit cleanup, initial PlayMode coverage. | `5-8 working weeks` |
| Long-term vision | Top 50 `SystemBase` migrations, high Burst coverage, broad PlayMode/test coverage, Addressables/subscene strategy. | `2-4 months` |

Estimate assumptions:

- Unity validation remains available or shadow validation is used when the main project is locked.
- Work is done in small behavior-preserving slices with focused tests.
- UI Toolkit replacement and the paused five-SystemBase conversion remain separate unless explicitly resumed.

## Ground Rules

- Preserve gameplay behavior and visual presentation unless a phase explicitly says otherwise.
- Do not force UI, camera, prefab, serialized config, scene, or presentation ownership into unmanaged `ISystem`.
- Do not introduce static mutable registries for gameplay behavior.
- Prefer baked entity references, blob data, ECS request/result data, object pools, or passive managed presentation boundaries.
- Keep every slice compiling and validated before starting the next one.
- Preserve Unity `.meta` files.
- Run `git diff --check` after each slice.

## Phase 0: Baseline And Safety

Purpose:
Capture the current state before changing architecture or performance behavior.

- [x] Run `git diff --check`.
- [x] Run Unity compile gate in the main project.
- [x] If the main project is locked, retry once and use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for shadow validation if available.
- [x] Capture current `.Run()` and `.ScheduleParallel()` counts.
- [x] Capture current `SystemBase`, `ISystem`, and `[BurstCompile]` counts.
- [x] Capture current managed `IComponentData` and `Object.Instantiate` call sites.
- [x] Capture current `.DS_Store`, empty-folder, quality-setting, and project-setting state.
- [x] Record baseline focused validation logs for production, rendering budget, unit movement, transport, armory/UI, and architecture tests.

Baseline notes:

- Initial scheduling counts under game runtime/rendering/environment systems: `15` `.Run()` calls and `24` `.ScheduleParallel` calls.
- Current system inheritance counts under `Assets/Game/Scripts`: `248` `SystemBase` files and `112` `ISystem` files.
- Current managed component count by direct `class ... : IComponentData` scan: `10`.
- Current instantiate call count by direct runtime script scan: `59`.
- Initial `.DS_Store` files found and removed: `Assets/Game/Scripts/.DS_Store`, `Assets/Game/Scripts/Rendering/.DS_Store`.
- Empty folders verified: `Assets/Game/Scripts/Bootstrap`, `Assets/Game/Scripts/Rewards`, `Assets/Game/Scripts/Profile`.
- Main project Unity validation was available during quick-win slices, so the shadow-project fallback path was not needed.
- Baseline render-budget validation passed: `/private/tmp/warline-ecs-audit-baseline-render-budget.log` (`UnitRenderBudgetFocusedValidation`, `tests=28`).
- Baseline production validation passed: `/private/tmp/warline-ecs-audit-baseline-production.log` (`BuildingProductionRequestValidation`, `tests=21`).
- Baseline movement validation passed: `/private/tmp/warline-ecs-audit-baseline-movement.log` (`UnitMovementBlockerValidation`).
- Baseline transport validation passed: `/private/tmp/warline-ecs-audit-baseline-transport.log` (`UnitTransportValidation`, `tests=73`).
- Baseline armory/UI validation passed: `/private/tmp/warline-ecs-audit-baseline-armory-ui.log` (`ArmoryCurrentContentValidation`, `tests=3`).
- Baseline architecture validation failed: `/private/tmp/warline-ecs-audit-baseline-architecture.log` (`EcsBurstHotPathArchitectureValidation`, array snapshot debt `Current=14`, `ceiling=0`). After the deploy-order snapshot cleanup, `/private/tmp/warline-ecs-audit-baseline-architecture-after-deploy-array.log` reports `Current=13`, `ceiling=0`.

## Phase 1: Quick Cleanup And Project Settings

Purpose:
Apply low-risk hygiene and mobile GPU wins before deeper code refactors.

- [x] Remove `.DS_Store` files under `Assets/Game/Scripts`.
- [x] Add `.DS_Store` and `Assets/**/.DS_Store` to `.gitignore`.
- [x] Verify whether `Assets/Game/Scripts/Bootstrap`, `Rewards`, and `Profile` have `.meta` references before deleting or preserving them.
- [x] Preserve empty Unity folders through existing folder `.meta` files rather than adding `.gitkeep` assets under `Assets`.
- [x] Change mobile quality shadow cascades from `4` to `2`.
- [x] Change mobile shadow distance from `240` to `150`.
- [x] Reduce PC shadow distance from `240` to `180`, keeping PC cascades unless visual QA rejects it.
- [x] Run compile and visual smoke validation for match camera, units, buildings, and shadows.

Phase 1 notes:

- `ProjectSettings/QualitySettings.asset` Mobile quality now uses `shadowCascades: 2` and `shadowDistance: 150`.
- PC quality keeps `shadowCascades: 4` and now uses `shadowDistance: 180`.
- Unity batchmode compile passed with log `/private/tmp/warline-ecs-audit-quickwins-compile.log`.
- Phase 1 smoke validation passed: camera `/private/tmp/warline-ecs-audit-phase1-camera-smoke.log` (`RtsCameraFocusedValidation`, `tests=11`), building faction visuals `/private/tmp/warline-ecs-audit-phase1-building-faction-visual-smoke.log` (`BuildingFactionVisualFocusedValidation`, `tests=4`), building selection visuals `/private/tmp/warline-ecs-audit-phase1-building-selection-visual-smoke.log` (`BuildingSelectionMarkerFocusedValidation`, `tests=6`), and marker shadow/rendering settings `/private/tmp/warline-ecs-audit-phase1-marker-shadow-smoke.log` (`SelectionOrderMarkerFocusedValidation`, `tests=15`). Quality settings were rechecked in `ProjectSettings/QualitySettings.asset` with Mobile `shadowCascades: 2`, Mobile `shadowDistance: 150`, PC `shadowCascades: 4`, and PC `shadowDistance: 180`.

## Phase 2: Low-Risk GC And Job Scheduling

Purpose:
Remove obvious per-frame allocations and schedule safe data-parallel jobs without changing ownership boundaries.

- [x] Cache `BuildingPlacementInputSystem` scratch `List<Vector2Int>` and `List<WallRun>` allocations as reusable fields.
- [x] Cache `BuildingBarrierSystem` perimeter dictionary allocations as reusable fields.
- [x] Cache/evaluate `BuildingVisualSystem`, `SelectionUiReadModelLookup`, `AttackOrderCommandSystem`, and `BuildingDefinitionSystem` scratch allocations where safe.
- [x] Add focused tests or allocation checks for touched hot paths where existing coverage is weak.
- [x] Replace `UnitTransportDeployOrderSystem` deploy-entity `ToEntityArray` snapshot with chunk iteration.
- [x] Replace `RtsSelectionAttackTargetModeCommandSystem` selected-entity `ToEntityArray` snapshots with chunk iteration.
- [x] Replace `RtsSelectionBoardTargetModeCommandSystem` selected-entity and pending-boarding `To*Array` snapshots with chunk iteration.
- [x] Replace `RtsSelectionDeselectAllCommandSystem` selected-unit `ToEntityArray` snapshot with chunk iteration.
- [x] Replace `RtsSelectionMoveTargetModeCommandSystem` selected-faction `ToComponentDataArray` snapshot with chunk iteration.
- [x] Replace `RtsSelectionImmediateSelectedUnitCommandSystem` immediate selected-unit `ToEntityArray` snapshots with chunk/list iteration.
- [x] Replace `BuildingUiQuerySystem` runtime-boundary `ToEntityArray` snapshot with first-chunk entity lookup.
- [x] Replace `ScanIntelCommandSystem` selected scan-source `ToEntityArray` snapshot with chunk iteration.
- [x] Replace `UnitTransportAirdropSystem` source/registry visual-prefab `ToEntityArray` snapshots with chunk iteration.
- [x] Evaluate `UnitRenderBudgetDistanceSystem` `.Run()` scheduling and leave synchronous pending output-container redesign.
- [x] Evaluate `UnitRenderBudgetSortSystem` `.Run()` scheduling and leave synchronous pending output-container redesign.
- [x] Evaluate `UnitRenderBudgetBandSystem` `.Run()` scheduling and leave synchronous pending output-container redesign.
- [x] Evaluate and convert safe health bar and animation index `.Run()` calls only if they do not touch managed presentation data.
- [x] Schedule `UnitHealthBarSystem` recent-damage expiry job and chain it before health-bar fill updates.
- [x] Remove `UnitAnimationIndexSystem` disabled freeze-diagnostic `NativeArray` counter allocation.
- [x] Evaluate health and animation `WithChangeFilter` candidates and leave unfiltered where stale refresh can occur.
- [x] Cache `UnitMoveTargetDiagnosticSystem` missing-entity prune scratch allocation.
- [x] Cache `BuildingCombatSystem` destroyed-building cleanup ID scratch allocation.
- [x] Evaluate minimap marker filtering separately because static/forced refresh behavior may be required.
- [x] Run focused rendering budget, health, animation, minimap, production, and movement validations.

Phase 2 notes:

- `BuildingBarrierSystem` now reuses `_enemyWallPerimetersScratch`; validation passed with `/private/tmp/warline-ecs-audit-building-barrier-quickwin.log` and rerun log `/private/tmp/warline-ecs-audit-building-barrier-quickwin-rerun.log`.
- `BuildingPlacementInputSystem` now keeps reusable scratch lists for immediate wall-placement preview/focus/validation origins and final wall-run validation. The public owned-list methods remain unchanged for compatibility and commit-request ownership. Building placement command validation passed with `/private/tmp/warline-ecs-audit-building-placement-scratch.log` (`BuildingPlacementCommandRequestValidation`, `tests=11`).
- `BuildingPlacementValidationSystemTests` now includes a focused scratch-list coverage test that verifies immediate preview storage is reused while owned-list results remain independent. Validation passed with `/private/tmp/warline-ecs-audit-building-placement-scratch-test.log` (`BuildingPlacementCommandRequestValidation`, `tests=12`).
- `SelectionUiReadModelLookup.ResolveHudSelectionStatus` now reuses `_selectionStatusParts`; validation passed with `/private/tmp/warline-ecs-audit-selection-ui-lookup-quickwin.log` and rerun log `/private/tmp/warline-ecs-audit-selection-ui-lookup-quickwin-rerun.log`.
- `BuildingVisualSystem.FindAnimatedBuildingParts` returns a new array that becomes owned runtime state, so the local list is not a safe scratch-cache-only change.
- `BuildingDefinitionSystem.BuildProductionSlots` returns an owned list, so caching that list would leak mutable state across definitions.
- `AttackOrderCommandSystem` already uses caller-provided scratch in the normal runtime path; the fallback allocation is a compatibility path and should be removed only after all callers are verified.
- `BuildingCombatSystem.UpdateDestroyedBuildings` now reuses `_destroyedCleanupIdsScratch` instead of allocating a cleanup ID list during runtime ticks. The public `CollectDestroyedCleanupIds` return-list API remains unchanged for tests and external callers. Building combat validation passed with `/private/tmp/warline-ecs-audit-building-combat-cleanup-scratch.log` (`BuildingCombatFocusedValidation`, `tests=4`).
- `UnitRenderBudgetDistance`, `UnitRenderBudgetSort`, and `UnitRenderBudgetBand` fill `NativeList`/`NativeHashSet` outputs that are consumed immediately. A real scheduled chain needs an output-count/container redesign; replacing `.Run()` with `Schedule().Complete()` is not counted as a useful quick win. Focused render-budget validation passed after this review with `/private/tmp/warline-ecs-audit-render-budget-run-review.log` (`UnitRenderBudgetFocusedValidation`, `tests=28`).
- `UnitTransportDeployOrderSystem` now iterates deploy-order chunks with a cached `EntityTypeHandle` instead of allocating a deploy entity array. Transport validation passed with `/private/tmp/warline-ecs-audit-transport-deploy-no-array.log` (`UnitTransportValidation`, `tests=73`). Architecture array snapshot debt reduced from `14` to `13`, but the focused architecture guard still fails until the remaining debt is removed or an approved ceiling is documented.
- `RtsSelectionAttackTargetModeCommandSystem` now iterates selected chunks with `EntityManager.GetEntityTypeHandle()` instead of allocating selected entity arrays. Selection validation passed with `/private/tmp/warline-ecs-audit-attack-target-no-array.log` (`RtsSelectionInputSystemValidation`, `tests=56`). Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-attack-array.log` confirms Systems-only array snapshot debt is now `11`.
- `RtsSelectionBoardTargetModeCommandSystem` now iterates selected chunks and pending boarding target chunks instead of allocating entity/component arrays. Selection validation passed with `/private/tmp/warline-ecs-audit-board-target-no-array.log` (`RtsSelectionInputSystemValidation`, `tests=56`). Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-board-array.log` confirms Systems-only array snapshot debt is now `9`.
- `RtsSelectionDeselectAllCommandSystem` now iterates selected chunks and removes selected tags through an immediate ECB instead of allocating selected entity arrays.
- `RtsSelectionMoveTargetModeCommandSystem` now iterates selected-move chunks with a `Faction` component type handle instead of allocating faction component arrays.
- Selection validation passed after the deselect/move cleanup with `/private/tmp/warline-ecs-audit-deselect-move-no-array.log` (`RtsSelectionInputSystemValidation`, `tests=56`). Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-deselect-move-array.log` confirms Systems-only array snapshot debt is now `7`.
- `RtsSelectionImmediateSelectedUnitCommandSystem` now iterates selected-move chunks for hold/stop and collects selected player units through chunk iteration before return-to-base and destroy-selected mutations. Selection validation passed with `/private/tmp/warline-ecs-audit-immediate-selected-no-array.log` (`RtsSelectionInputSystemValidation`, `tests=56`). Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-immediate-array.log` confirms Systems-only array snapshot debt is now `4`.
- `BuildingUiQuerySystem` now reads the first `BuildingRuntimeBoundaryTag` entity through chunk entity access instead of allocating a boundary entity array. Building UI query validation passed with `/private/tmp/warline-ecs-audit-building-ui-query-no-array.log` (`BuildingUiQueryValidation`, `tests=5`). Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-building-ui-array.log` confirms Systems-only array snapshot debt is now `3`.
- `ScanIntelCommandSystem` now iterates selected-source chunks while preserving diagnostic selected-count and candidate-index output. Scan intel focused validation passed with `/private/tmp/warline-ecs-audit-scan-intel-no-array.log` (`ScanIntelCommandFocusedValidation`, `tests=2`). Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-scan-array.log` confirms Systems-only array snapshot debt is now `2`.
- `UnitTransportAirdropSystem` now iterates source-key prefab candidates and registry entities through chunks instead of allocating entity arrays. Transport validation passed with `/private/tmp/warline-ecs-audit-transport-airdrop-no-array.log` (`UnitTransportValidation`, `tests=73`). `rg "ToEntityArray|ToComponentDataArray" Assets/Game/Scripts/Systems -g '*.cs'` is now clean. Architecture rerun `/private/tmp/warline-ecs-audit-baseline-architecture-after-airdrop-array.log` gets past array-snapshot debt and now fails on the separate existing non-Burst `OnUpdate` guard (`Current=256`, `ceiling=23`).
- `UnitHealthBarSystem` now schedules `ExpireRecentDamageVisibilityJob` and chains the handle before the parallel health-bar fill update. Vehicle visual adornments validation passed with `/private/tmp/warline-ecs-audit-healthbar-scheduled-expiry.log` (`VehicleVisualAdornmentsFocusedValidation`, `tests=19`).
- `UnitAnimationIndexSystem.ResolveAnimationIndexJob` remains synchronous for now because it feeds same-update immediate visual-root animation index application; replacing it with `Schedule().Complete()` would not be a useful quick win. The disabled freeze-diagnostic `NativeArray<int>` counter allocation was removed; metrics now count units in the existing apply loop only when freeze logging is enabled. Animation validation passed with `/private/tmp/warline-ecs-audit-animation-index-counter-gated.log` (`UnitAnimationIndexFocusedValidation`, `tests=3`).
- Health and animation change filters were evaluated and left unfiltered: health-bar fill depends on parent-unit `UnitHealth`, `RecentDamageHealthBarVisibility`, passenger, and culling lookup state rather than changes on the health-bar child entity; animation index resolution decrements `UnitAttackAnimationComponent.TimeRemaining` every frame and must keep same-frame visual-root application.
- `UnitMoveTargetDiagnosticSystem` now reuses `_missingTargetScratch` when pruning missing diagnostic target entries instead of allocating a list on the disabled-by-default move-command trace path. Diagnostics validation passed with `/private/tmp/warline-ecs-audit-move-target-diagnostic-scratch.log` (`RuntimeDiagnosticsValidation`, `tests=4`).
- `MatchHudMinimapMarkerSystem` was evaluated for filtering and left unfiltered: it intentionally clears and rebuilds the HUD marker boundary every update from live player/enemy units plus scan intel last-seen data, so a change filter would risk stale markers and static/forced refresh gaps. Minimap marker validation passed with `/private/tmp/warline-ecs-audit-phase2-minimap-marker.log` (`MatchHudMinimapMarkerFocusedValidation`, `tests=3`).
- Phase 2 focused validation gate passed: render budget `/private/tmp/warline-ecs-audit-phase2-render-budget.log` (`UnitRenderBudgetFocusedValidation`, `tests=28`), health `/private/tmp/warline-ecs-audit-healthbar-scheduled-expiry.log` (`VehicleVisualAdornmentsFocusedValidation`, `tests=19`), animation `/private/tmp/warline-ecs-audit-phase2-animation-index.log` (`UnitAnimationIndexFocusedValidation`, `tests=3`), minimap `/private/tmp/warline-ecs-audit-phase2-minimap-marker.log` (`MatchHudMinimapMarkerFocusedValidation`, `tests=3`), production `/private/tmp/warline-ecs-audit-phase2-production.log` (`BuildingProductionRequestValidation`, `tests=21`), and movement `/private/tmp/warline-ecs-audit-phase2-movement.log` (`UnitMovementBlockerValidation`).

## Phase 3: Managed Component Boundary Reduction

Purpose:
Reduce managed `IComponentData` without hiding Unity object ownership in unmanaged code.

- [ ] Classify each managed component as scene reference, camera reference, VFX/light reference, pose mesh setup, or diagnostics.
- [ ] Move scene and camera references to explicit managed bootstrap or presentation boundaries.
- [ ] Move performance diagnostics references to managed diagnostic ownership outside ECS component data.
- [ ] Replace VFX prefab managed components with baked entity prefab refs where runtime spawning can be ECS-owned.
- [ ] Keep VFX GameObject playback in passive managed presentation systems when Unity object access is required.
- [ ] Replace light prefab/runtime managed components with baked refs, entity refs, or pooled managed presentation state.
- [ ] Replace pose mesh setup managed component with baked mesh/entity data or a passive setup boundary.
- [ ] Add architecture tests that block static mutable registries for gameplay behavior.
- [ ] Run combat VFX, missile, attached light, camera, diagnostics, and compile validations.

## Phase 4: Instantiation And Pooling

Purpose:
Remove hot runtime `Object.Instantiate` calls while preserving UI, editor, and one-time setup boundaries.

- [ ] Categorize each `Object.Instantiate` call as gameplay entity spawn, visual GameObject, UI, editor-only, preview/cache, or one-time setup.
- [ ] Leave editor-only migration/generation tools out of runtime performance scope.
- [ ] Leave UI instantiation as managed UI work unless it becomes a measured UI performance issue.
- [ ] Convert gameplay entity spawn paths to ECB/entity prefab instantiation where data is already baked.
- [ ] Pool selection/order/building marker GameObjects instead of instantiating on demand.
- [ ] Convert destroyed visual and wreck visuals only after prefab/entity visual data is explicit.
- [ ] Keep road/building/decor visual instantiation managed until their visual data model is split from gameplay.
- [ ] Add or update tests for marker reuse and visual lifecycle cleanup.
- [ ] Run selection marker, order marker, destroyed visual, road visual, building visual, and production validations.
- [ ] Document any intentionally retained managed instantiation boundaries.

## Phase 5: Transport Boarding Split

Purpose:
Split `TransportBoardingCommandSystem` without changing transport behavior.

- [ ] Add named constants for distance penalties and drop intervals before moving behavior.
- [ ] Identify current public/tested transport entry points and preserve their behavior.
- [ ] Extract boarding request validation and goal assignment into a focused owner.
- [ ] Extract plane ramp approach calculations into a focused owner.
- [ ] Extract airdrop timing and drop execution into a focused owner.
- [ ] Extract passenger capacity checks into a focused owner.
- [ ] Extract disembark/landing logic into a focused owner.
- [ ] Keep existing tests passing after each extraction.
- [ ] Run transport boarding, airdrop, capacity, disembark, movement, and compile validations.

## Phase 6: Test Infrastructure And Coverage

Purpose:
Make validation runners safe in the Editor and add integration coverage where the audit found gaps.

- [ ] Create a shared validation-exit helper that exits only during batchmode validation.
- [ ] Replace raw `EditorApplication.Exit` calls in the highest-used validation files first.
- [ ] Preserve static validation methods used by automation.
- [ ] Verify affected tests still run from Unity Test Runner without quitting the editor.
- [ ] Add PlayMode smoke for match start to initial unit spawn.
- [ ] Add PlayMode smoke for building placement to production.
- [ ] Add PlayMode smoke for transport boarding to disembark.
- [ ] Add PlayMode smoke for basic combat and death flow.
- [ ] Add focused EditMode tests for city generation, unit combat, road build, and building placement gaps.
- [ ] Run full focused validation set and at least one PlayMode smoke group.

## Phase 7: Long-Term ECS And Burst Migration

Purpose:
Convert only proven hot unmanaged work to `ISystem`/Burst while preserving managed boundaries.

- [ ] Rank remaining `SystemBase` files by update frequency, entity count, allocation risk, and managed-boundary risk.
- [ ] Pick the first 10 hot unmanaged candidates for `ISystem` conversion.
- [ ] For each candidate, split managed Unity access into a passive boundary before conversion.
- [ ] Add `[BurstCompile]` only after unmanaged access is proven by compile and tests.
- [ ] Add architecture tests for converted systems to block `GameObject`, `UnityEngine.Object`, and unmanaged-incompatible dependencies.
- [ ] Update performance benchmarks after each migration batch.
- [ ] Re-estimate the next 10 candidates after every completed batch.
- [ ] Keep Addressables, subscene streaming, and 80% coverage goals as separate long-term planning tracks.

## Required Validation Set

- [ ] `git diff --check`.
- [ ] Unity batchmode compile.
- [ ] Rendering budget focused validation.
- [ ] Unit movement focused validation.
- [ ] Transport boarding focused validation.
- [ ] Production/build drawer focused validation.
- [ ] Armory/UI focused validation.
- [ ] Relevant PlayMode smoke after test infrastructure work.

## Completion Criteria

- Quick-win phases complete and validated.
- No accidental changes to paused five-SystemBase split tracker scope.
- Hot per-frame allocation sites from the audit are either fixed or documented as false positives.
- Safe `.Run()` conversions are scheduled or documented as managed-boundary exceptions.
- Managed components are reduced only through architecture-approved boundaries.
- Runtime `Object.Instantiate` hot paths are converted, pooled, or documented as acceptable managed boundaries.
- Test validation runners no longer kill the Editor Test Runner.
- Final validation set passes.
