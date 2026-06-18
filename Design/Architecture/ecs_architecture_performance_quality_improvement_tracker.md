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

- Checklist progress: `92 / 100 complete (92.0%)`.
- In progress: `0`.
- Remaining open: `8`.
- Current target: `Medium-term scope complete; Phase 7 long-term ECS/Burst migration remains deferred`.
- Quick-win estimate: `3-5 working days`.
- Medium-term audit estimate: `5-8 working weeks`.
- Long-term architecture vision estimate: `2-4 months`.
- Validation status: `git diff --check` passed; Unity batchmode compile passed after quick-win settings cleanup; main project validation was available, so shadow-project fallback was not needed; baseline render-budget, production, movement, transport, and armory/UI validations passed; Phase 1 camera, building visual, marker shadow/rendering, and quality-setting smoke validations passed; building placement command validation passed after wall-preview scratch-list cleanup and focused scratch-list coverage; vehicle visual adornments validation passed after health-bar expiry scheduling; transport validation passed after deploy-order and airdrop lookup array-snapshot cleanup; selection input validation passed after attack-target, board-target, deselect-all, move-target, and immediate-selected array-snapshot cleanup; building UI query validation passed after boundary lookup cleanup; scan intel focused validation passed after selected-source lookup cleanup; Phase 2 focused rendering budget, health, animation, minimap, production, and movement validations passed; render-budget `.Run()` helpers were evaluated, left synchronous pending an output-container redesign, and revalidated; minimap marker filtering was evaluated and left unfiltered to preserve per-frame HUD rebuild behavior; UnitAnimationIndexSystem validation passed after removing disabled freeze-diagnostic counter allocation; health/animation scheduling and change-filter candidates were evaluated and left synchronous/unfiltered where immediate refresh is required; UnitMoveTargetDiagnosticSystem scratch cleanup passed diagnostics focused validation; BuildingCombatSystem cleanup scratch passed focused validation; runtime Systems `ToEntityArray`/`ToComponentDataArray` grep is clean; Phase 3 managed component inventory classified all 10 managed component classes; Phase 3 scene/camera reference boundary slices passed Unity compile/focused validation (`/private/tmp/warline-ecs-audit-scene-reference-boundary-compile.log`, `/private/tmp/warline-ecs-audit-runtime-camera-boundary-validation.log`); Phase 3 diagnostics reference boundary slice passed Unity compile (`/private/tmp/warline-ecs-audit-diagnostics-boundary-compile.log`); Phase 3 VFX prefab reference conversion passed Unity compile and focused missile validations (`/private/tmp/warline-ecs-audit-attack-vfx-unityobjectref-compile.log`, `/private/tmp/warline-ecs-audit-missile-vfx-unityobjectref-compile.log`, `/private/tmp/warline-ecs-audit-air-missile-vfx-unityobjectref-validation.log`, `/private/tmp/warline-ecs-audit-ground-missile-vfx-unityobjectref-validation.log`); Phase 3 attached-light setup buffer slice passed Unity compile (`/private/tmp/warline-ecs-audit-attached-light-setup-buffer-compile-rerun.log`); Phase 3 pose mesh setup `UnityObjectRef` slice passed Unity compile (`/private/tmp/warline-ecs-audit-pose-mesh-unityobjectref-compile.log`); Phase 3 attached-light runtime boundary passed Unity compile and combat/death validation (`/private/tmp/warline-ecs-audit-attached-light-runtime-boundary-compile.log`, `/private/tmp/warline-ecs-audit-attached-light-runtime-combat-death-validation.log`) and managed component scan now reports 0 classes; Phase 3 static mutable gameplay registry guard passed bootstrap architecture validation (`/private/tmp/warline-ecs-audit-static-registry-guard-validation.log`); Phase 3 VFX playback boundary split passed Unity compile plus air and ground missile focused validations (`/private/tmp/warline-ecs-audit-vfx-presentation-boundary-compile.log`, `/private/tmp/warline-ecs-audit-vfx-presentation-air-missile-validation.log`, `/private/tmp/warline-ecs-audit-vfx-presentation-ground-missile-validation.log`); Phase 4 instantiation categorization passed `git diff --check`; Phase 4 order-marker preview pooling passed focused validation (`/private/tmp/warline-ecs-audit-selection-order-marker-pool-validation.log`); Phase 4 health-bar and vehicle destroyed visual entity-prefab ECB slice passed focused vehicle visual validation (`/private/tmp/warline-ecs-audit-entity-prefab-ecb-vehicle-visual-validation.log`); Phase 4 building destroyed visual wrapper-aware contract validation passed after updating the stale assertion (`/private/tmp/warline-ecs-audit-phase4-building-destroyed-visual-validation.log`); Phase 4 validation sweep passed selection/order marker, building selection marker, building faction visual, production, and nearest road-build focused validations (`/private/tmp/warline-ecs-audit-phase4-selection-order-marker-validation.log`, `/private/tmp/warline-ecs-audit-phase4-building-selection-marker-validation.log`, `/private/tmp/warline-ecs-audit-phase4-building-faction-visual-validation.log`, `/private/tmp/warline-ecs-audit-phase4-production-validation.log`, `/private/tmp/warline-ecs-audit-phase4-road-build-validation.log`); Phase 5 transport constants-only slice passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-transport-constants-validation.log`); Phase 5 shared boarding transport validation helper passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-boarding-validation-helper-validation.log`); Phase 5 boarding goal-order helper passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-boarding-goal-order-helper-validation.log`, test result passed before Unity batchmode teardown was manually terminated); Phase 5 plane-ramp calculation helpers passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-plane-ramp-calculation-helper-validation.log`); Phase 5 airdrop drop-execution helper passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-airdrop-drop-execution-helper-validation.log`); Phase 5 passenger capacity state helper passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-passenger-capacity-helper-validation.log`); Phase 5 disembark planning helper passed full transport validation (`/private/tmp/warline-ecs-audit-phase5-disembark-planning-helper-validation.log`) and movement validation (`/private/tmp/warline-ecs-audit-phase5-movement-validation.log`); Phase 6 validation-exit helper passed batchmode smoke validation through movement runner (`/private/tmp/warline-ecs-audit-phase6-validation-exit-movement.log`); Phase 6 required validation-set runner exit conversions passed production, armory, build drawer, and render-budget validations (`/private/tmp/warline-ecs-audit-phase6-production-validation-exit.log`, `/private/tmp/warline-ecs-audit-phase6-armory-validation-exit.log`, `/private/tmp/warline-ecs-audit-phase6-builddrawer-validation-exit.log`, `/private/tmp/warline-ecs-audit-phase6-renderbudget-validation-exit.log`); Phase 6 match-start initial-spawn PlayMode smoke passed (`/private/tmp/warline-ecs-audit-phase6-match-start-initial-spawn-playmode.log`, `/private/tmp/warline-ecs-audit-phase6-match-start-initial-spawn-playmode-results.xml`, `1/1` passed); Phase 6 focused EditMode coverage passed for city generation, unit combat, road build, and building placement (`/private/tmp/warline-ecs-audit-phase6-runtime-city-generation-editmode-results.xml`, `/private/tmp/warline-ecs-audit-phase6-unit-combat-editmode-results.xml`, `/private/tmp/warline-ecs-audit-phase6-road-build-editmode-results.xml`, `/private/tmp/warline-ecs-audit-phase6-building-placement-editmode-results.xml`); final medium-term validation sweep passed `git diff --check`, Unity compile, render-budget, movement, transport, production/build drawer, armory/UI, and combat-death PlayMode smoke; architecture runner still fails on separate existing non-Burst `OnUpdate` debt (`Current=256`, `ceiling=23`) deferred to Phase 7.
- Latest medium-term validation: final PlayMode smoke passed with `/private/tmp/warline-ecs-audit-medium-final-combat-death-playmode.log` and result XML `/private/tmp/warline-ecs-audit-medium-final-combat-death-playmode-results.xml` (`1/1` passed).
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

- [x] Classify each managed component as scene reference, camera reference, VFX/light reference, pose mesh setup, or diagnostics.
- [x] Move scene and camera references to explicit managed bootstrap or presentation boundaries.
- [x] Move match scene reference storage from managed `IComponentData` to `MatchSceneReferenceBoundarySystem`.
- [x] Move runtime camera reference storage from managed `IComponentData` to `RuntimeCameraReferenceSystem` managed state.
- [x] Move performance diagnostics references to managed diagnostic ownership outside ECS component data.
- [x] Replace VFX prefab managed components with baked entity prefab refs or unmanaged Unity object refs where runtime playback remains GameObject-owned.
- [x] Convert normal unit attack impact and muzzle-flash VFX references from managed component classes to `UnityObjectRef<GameObject>` struct components.
- [x] Convert ground and air missile VFX references from managed component classes to `UnityObjectRef<GameObject>` struct components.
- [x] Keep VFX GameObject playback in passive managed presentation systems when Unity object access is required.
- [x] Replace light prefab/runtime managed components with baked refs, entity refs, or pooled managed presentation state.
- [x] Convert attached-light setup data from managed component class to baked `UnitAttachedLightSetupElement` buffer.
- [x] Move attached-light runtime instances from managed component data into `UnitAttachedLightSystem` managed presentation state.
- [x] Replace pose mesh setup managed component with baked mesh/entity data or a passive setup boundary.
- [x] Add architecture tests that block static mutable registries for gameplay behavior.
- [x] Run combat VFX, missile, attached light, camera, diagnostics, and compile validations.

Phase 3 notes:

Managed component inventory:

| Component | Category | Current owner/use | Boundary decision |
| --- | --- | --- | --- |
| `MatchSceneReferenceComponent` (retired) | Scene reference | Replaced by `MatchSceneReferenceBoundarySystem`, a disabled managed system that owns `MatchSceneView` state. | Completed scene-reference boundary slice; no managed `IComponentData` remains for match scene view state. |
| `RuntimeCameraReferenceComponent` (retired) | Camera reference | Replaced by `RuntimeCameraReferenceSystem` managed state; render-budget and model-spawn systems resolve the camera from their owning `World`. | Completed camera-reference boundary slice; no managed `IComponentData` remains for runtime camera state. |
| `PerformanceDiagnosticsReferenceComponent` (retired) | Diagnostics | Replaced by `PerformanceDiagnosticsReferenceBoundarySystem`; menu/match bootstraps still use `PerformanceDiagnosticsReferenceSystem` as a plain helper. | Completed diagnostics-reference boundary slice; no managed `IComponentData` remains for diagnostics ownership. |
| `UnitAttackImpactVfxReference` | VFX prefab reference | Normal unit attack impact VFX reads a `UnityObjectRef<GameObject>` prefab from source entities. | Converted from managed component class to unmanaged struct component; playback remains pooled GameObject VFX. |
| `UnitMuzzleFlashVfxReference` | VFX prefab reference | Normal unit attack muzzle flash reads a `UnityObjectRef<GameObject>` prefab plus offsets from source entities. | Converted from managed component class to unmanaged struct component; playback remains pooled GameObject VFX. |
| `GroundMissileLauncherVfxReferenceComponent` | VFX prefab reference | Ground missile launcher/fire/impact systems read launcher, trail, explosion, and smoke prefabs through `UnityObjectRef<GameObject>`. | Converted from managed component class to unmanaged struct component; playback remains pooled GameObject VFX. |
| `AirMissileLauncherVfxReferenceComponent` | VFX prefab reference | Air missile launcher systems read missile, launch, trail, airburst, air-target, and intercept prefabs through `UnityObjectRef<GameObject>`. | Converted from managed component class to unmanaged struct component; playback remains pooled GameObject VFX. |
| `UnitAttachedLightSet` (retired) | Light setup/config | Replaced by baked `UnitAttachedLightSetupElement` buffer entries. | Setup data converted from managed component class to unmanaged buffer; runtime light instances remain managed presentation state. |
| `UnitAttachedLightRuntime` (retired) | Light runtime instances | Replaced by `UnitAttachedLightSystem` managed presentation state keyed by entity; `UnitDeathSystem` emits `UnitAttachedLightCleanupRequest` data. | Runtime instances are no longer ECS component objects; GameObject ownership stays in the managed presentation boundary. |
| `UnitPoseMeshesSetup` | Pose mesh setup | Carries optional `UnityObjectRef<Mesh>` and `UnityObjectRef<Material>` setup refs; no active runtime readers were found. | Converted from managed component class to unmanaged struct component; behavior unchanged because no runtime call sites existed. |

Scene and camera reference boundary slices:

- `MatchSceneReferenceSystem` now resolves a disabled managed `MatchSceneReferenceBoundarySystem` instead of creating an entity with a managed component object.
- `MatchSceneReferenceComponent` was retired as managed `IComponentData`; the source file is preserved for Unity `.meta` stability.
- Unity compile passed with `/private/tmp/warline-ecs-audit-scene-reference-boundary-compile.log`.
- `RuntimeCameraReferenceSystem` now owns the current `Camera` as managed system state instead of writing a component object entity.
- `UnitRenderBudgetSystem` and `UnitModelSpawnSystem` now resolve the camera through `SystemState.World`, removing their camera component queries.
- `RuntimeCameraReferenceComponent` was retired as managed `IComponentData`; the source file is preserved for Unity `.meta` stability.
- Runtime camera focused validation passed with `/private/tmp/warline-ecs-audit-runtime-camera-boundary-validation.log` (`RuntimeCameraReferenceFocusedValidation`, `tests=3`).
- `PerformanceDiagnosticsReferenceSystem` is now a plain helper over `PerformanceDiagnosticsReferenceBoundarySystem`, preserving the menu-to-match diagnostics handoff without component-object storage.
- `PerformanceDiagnosticsReferenceComponent` was retired as managed `IComponentData`; the source file is preserved for Unity `.meta` stability.
- Diagnostics boundary compile passed with `/private/tmp/warline-ecs-audit-diagnostics-boundary-compile.log`.
- Normal unit attack impact and muzzle-flash VFX references now bake as struct `IComponentData` with `UnityObjectRef<GameObject>` fields instead of class managed components.
- `UnitAttackVfxRequestSystem` now reads normal component data and unwraps the `GameObject` only at the pooled VFX playback boundary.
- Attack VFX `UnityObjectRef<GameObject>` compile passed with `/private/tmp/warline-ecs-audit-attack-vfx-unityobjectref-compile.log`.
- Ground and air missile VFX references now bake as struct `IComponentData` with `UnityObjectRef<GameObject>` fields instead of class managed components.
- Ground and air missile systems now read normal component data and unwrap `GameObject` prefabs only at the pooled VFX playback boundary.
- Missile VFX `UnityObjectRef<GameObject>` compile passed with `/private/tmp/warline-ecs-audit-missile-vfx-unityobjectref-compile.log`.
- Focused missile validations passed: `/private/tmp/warline-ecs-audit-air-missile-vfx-unityobjectref-validation.log` (`AirMissileLauncherValidation`, `PASS`) and `/private/tmp/warline-ecs-audit-ground-missile-vfx-unityobjectref-validation.log` (`GroundMissileAttackFocusedValidation`, `tests=5`).
- Attached-light setup now bakes into `UnitAttachedLightSetupElement` buffer entries; `UnitAttachedLightSystem` consumes the buffer and owns spawned light `GameObject` instances in managed presentation state outside ECS component data.
- Attached-light setup buffer compile passed with `/private/tmp/warline-ecs-audit-attached-light-setup-buffer-compile-rerun.log`.
- Attached-light runtime cleanup now uses `UnitAttachedLightCleanupRequest` data from death flow; Unity compile passed with `/private/tmp/warline-ecs-audit-attached-light-runtime-boundary-compile.log` and combat/death validation passed with `/private/tmp/warline-ecs-audit-attached-light-runtime-combat-death-validation.log` (`CombatDeathFocusedValidation`, `tests=2`).
- `UnitPoseMeshesSetup` now uses `UnityObjectRef<Mesh>` and `UnityObjectRef<Material>` fields in a struct component; no runtime or baker call sites referenced this component beyond its definition.
- Pose mesh setup compile passed with `/private/tmp/warline-ecs-audit-pose-mesh-unityobjectref-compile.log`.
- Managed component scan now reports `0` direct `class ... : IComponentData` classes under `Assets/Game/Scripts`.
- `ScriptArchitectureAlignmentContractTests` now blocks new static mutable collection fields in runtime gameplay logic with an exact allowlist for existing utility/preview debts; bootstrap architecture validation passed with `/private/tmp/warline-ecs-audit-static-registry-guard-validation.log` (`BootstrapCompositionGuardrailValidation`, `tests=5`).
- `UnitAttackVfxRequestSystem`, `GroundMissileRocketTrailSystem`, and `AirMissileProjectileTrailSystem` are now managed presentation `SystemBase` boundaries; missile launch/impact systems enqueue `CombatGameObjectVfxRequest` data and `CombatGameObjectVfxPlaybackSystem` performs pooled GameObject playback.
- VFX playback boundary compile passed with `/private/tmp/warline-ecs-audit-vfx-presentation-boundary-compile.log`; focused air and ground missile validations passed with `/private/tmp/warline-ecs-audit-vfx-presentation-air-missile-validation.log` (`AirMissileLauncherValidation`, `PASS`) and `/private/tmp/warline-ecs-audit-vfx-presentation-ground-missile-validation.log` (`GroundMissileAttackFocusedValidation`, `tests=5`).

## Phase 4: Instantiation And Pooling

Purpose:
Remove hot runtime `Object.Instantiate` calls while preserving UI, editor, and one-time setup boundaries.

- [x] Categorize each `Object.Instantiate` call as gameplay entity spawn, visual GameObject, UI, editor-only, preview/cache, or one-time setup.
- [x] Leave editor-only migration/generation tools out of runtime performance scope.
- [x] Leave UI instantiation as managed UI work unless it becomes a measured UI performance issue.
- [x] Convert gameplay entity spawn paths to ECB/entity prefab instantiation where data is already baked.
- [x] Pool selection/order/building marker GameObjects instead of instantiating on demand.
- [x] Convert destroyed visual and wreck visuals only after prefab/entity visual data is explicit.
- [x] Keep road/building/decor visual instantiation managed until their visual data model is split from gameplay.
- [x] Add or update tests for marker reuse and visual lifecycle cleanup.
- [x] Run selection marker, order marker, destroyed visual, road visual, building visual, and production validations.
- [x] Document any intentionally retained managed instantiation boundaries.

Phase 4 notes:

Instantiation categorization:

- Editor-only migration/generation/validation calls are out of runtime scope: `BuildingDestroyedVisualPrefabMigration`, `RuntimeCitySpawnerStep13Validation`, `VehicleVisualAdornmentsMigration`, `UnitMidLodGenerator`, editor prefab hierarchy tools, and generated VFX asset tools.
- UI calls remain managed UI work: transport passenger drawer rows, armory list rows, build drawer rows/queue rows, placement confirmation buttons, app canvas bootstrap, shell content, and screen route flow.
- ECS entity prefab instantiation is already data-based in several paths: unit model spawn, selection marker entity spawn, initial unit/blocker spawn, citizen visible unit spawn, building spawn, transport airdrop visuals, health bars, vehicle destroyed visuals, unit respawn, and road-build ECS boundary spawn.
- Managed visual GameObject instantiation remains in presentation/config boundaries: pooled combat VFX, building/selection/order markers, destroyed visual GameObjects, bounds/definition sampling, runtime city/grid/decor visuals, day/night skybox material, road/building placement visuals, road special visuals, runtime transport visuals, and preview/cache rendering.
- Runtime root and setup objects remain one-time managed setup boundaries: runtime building/root objects, road runtime roots, placement outlines, preview root/camera, and city visual roots.
- Selection/order/building marker pooling slice: building selection already reuses one marker, move/attack command markers already cache one marker each, attack target selection/ring markers remain cached, and attack/board preview markers now prewarm the full `MaxAttackTargetPreviewMarkers` pool during `SelectionOrderMarkerSystem.Initialize` instead of instantiating during hover preview updates.
- Marker reuse coverage was added to `SelectionOrderMarkerSystemTests.Initialize_PrewarmsAttackTargetPreviewMarkerPool`; focused order-marker validation passed with `/private/tmp/warline-ecs-audit-selection-order-marker-pool-validation.log` (`SelectionOrderMarkerFocusedValidation`, `tests=15`).
- `UnitRuntimeHealthBarSystem` and `VehicleDestroyedVisualSystem` now instantiate their baked ECS prefab entities through a short-lived ECB while preserving same-update playback and existing instance-reference behavior.
- `UnitSelectionMarkerSystem`, `BuildingSpawnSystem`, `CitizenVisibleUnitSystem`, and `RoadBuildEcsBoundarySystem` remain direct `EntityManager.Instantiate` boundaries for now because they immediately inspect linked children, publish managed read models, or bridge command results in the same call. Those need separate design slices before delayed ECB playback is safe.
- Vehicle destroyed visual data is explicit (`VehicleDestroyedVisualPrefabReference` entity prefab plus `VehicleDestroyedVisualSpawnRequest`), so the vehicle wreck visual path was included in the ECB slice. Building destroyed visuals remain managed because `BuildingDefinition.DestroyedVisualPrefab` is still a `GameObject` prefab and the runtime building object owns wrapper transforms.
- Road, building placement, runtime city/grid/decor, bounds-sampling, and building destroyed visual instantiation are intentionally retained managed boundaries until their visual data models are split from gameplay/runtime GameObject state.
- `BuildingDestroyedVisualSystemTests` now asserts the wrapper-aware destroyed visual contract introduced by the runtime system: destroyed visuals with an alive visual wrapper are parented under the runtime building, keep local wrapper scale, and preserve world scale through the parent.
- Phase 4 focused validation passed for the touched entity-prefab ECB path with `/private/tmp/warline-ecs-audit-entity-prefab-ecb-vehicle-visual-validation.log` (`VehicleVisualAdornmentsFocusedValidation`, `tests=19`) and for the retained building destroyed visual boundary with `/private/tmp/warline-ecs-audit-phase4-building-destroyed-visual-validation.log` (`BuildingDestroyedVisualFocusedValidation`, `tests=2`).
- Phase 4 validation sweep passed: selection/order markers `/private/tmp/warline-ecs-audit-phase4-selection-order-marker-validation.log` (`SelectionOrderMarkerFocusedValidation`, `tests=15`), building selection marker `/private/tmp/warline-ecs-audit-phase4-building-selection-marker-validation.log` (`BuildingSelectionMarkerFocusedValidation`, `tests=6`), building faction visual `/private/tmp/warline-ecs-audit-phase4-building-faction-visual-validation.log` (`BuildingFactionVisualFocusedValidation`, `tests=4`), production `/private/tmp/warline-ecs-audit-phase4-production-validation.log` (`BuildingProductionRequestValidation`, `tests=21`), and nearest available road validation `/private/tmp/warline-ecs-audit-phase4-road-build-validation.log` (`RoadBuildCommandRequestValidation`, `tests=6`).

## Phase 5: Transport Boarding Split

Purpose:
Split `TransportBoardingCommandSystem` without changing transport behavior.

- [x] Add named constants for distance penalties and drop intervals before moving behavior.
- [x] Identify current public/tested transport entry points and preserve their behavior.
- [x] Extract boarding request validation and goal assignment into a focused owner.
- [x] Extract plane ramp approach calculations into a focused owner.
- [x] Extract airdrop timing and drop execution into a focused owner.
- [x] Extract passenger capacity checks into a focused owner.
- [x] Extract disembark/landing logic into a focused owner.
- [x] Keep existing tests passing after each extraction.
- [x] Run transport boarding, airdrop, capacity, disembark, movement, and compile validations.

Phase 5 notes:

- Introduced named constants in `TransportBoardingCommandSystem` for boarding order capacity, transport click padding, plane-ramp search radii, plane-ramp rollout distance/radius padding, transport ring search radius, rope-disembark takeoff height, rope-disembark drop interval, and plane-door open duration. No control flow or ownership moved in this slice.
- Full transport validation passed with `/private/tmp/warline-ecs-audit-phase5-transport-constants-validation.log` (`UnitTransportValidation`, `tests=73`).
- Public command entry points to preserve during extraction: `ProcessCommandIntentRequests`, `TryRequestBoardTransportOrderToClickedUnit`, `TryIssueBoardTransportOrderToTransport`, `TryIssueBoardSelectedTransportOrderToClickedPassenger`, `TryIssueBoardSelectedTransportOrderToPassenger`, `IsBoardablePlayerTransportClick`, and `TryResolveBoardablePlayerTransportClick`.
- Public/static helper entry points used by preview, deploy, air-pickup, and tests: `IsWithinTransportBoardingCommandRange`, `IsTransportLandedForBoarding`, `GetTransportBoardingDirectCells`, `IsBoardablePlayerTransport`, `IsBoardingCandidateForTransport`, `TryResolveBoardingPassengerKind`, `HasAvailableTransportBoardingSlot`, `HasAnyAvailableTransportBoardingSlot`, `IsPotentialVehicleCargoPassenger`, `IsVehicleBoardingCandidateForTransport`, `IsCargoPlaneTransport`, `IsSoldierBoardingCandidate`, `TryFindAirTransportPickupCellNearPassenger`, `ResolvePlaneRampApproachCell`, `TryResolvePlaneRampApproachCell`, `TryFindTransportApproachCell`, `ReserveFootprintCells`, `TryFindTransportDisembarkCell`, and `TryIssueDeployDisembark`.
- Runtime callers to keep behavior-compatible: `SelectionGameplayStartupSystem` owns the command-system instance, `RtsSelectionCommandResultFlushSystem` drains command buffers through `ProcessCommandIntentRequests`, `RtsSelectionPointerTargetCommandSystem` uses click/preview/capacity helpers, `RtsSelectionBoardTargetModeCommandSystem` uses target classification helpers, `UnitTransportDeployOrderSystem` uses deploy-disembark and direct-cell helpers, and `UnitTransportAirPickupSystem` uses soldier and air-pickup helpers.
- Validation coverage preserving these entry points: `UnitTransportValidationTests.RunBatchValidation` covers boarding, selected-transport passenger boarding, board-all requests, disembark, airdrop, rope disembark, deploy disembark, and click/exit-button flows; `UnitTransportBoardingSystemExtractionTests` covers static helper compatibility; `SelectionCommandRequestResultContractTests`, `RtsSelectionInputSystemTests`, and `TransportBoardingPerformanceValidation` cover command flush, architecture contracts, and selection-performance behavior.
- Started the boarding validation extraction by consolidating the duplicated transport validity, landed-state, and rope-busy checks for both selected-passenger and selected-transport board commands into `TryValidateBoardingTransport`. The helper preserves the existing invalid-transport diagnostic result names for generic transport boarding and selected-transport boarding. Goal assignment remains in `TransportBoardingCommandSystem` and is the next part of this in-progress item.
- Full transport validation passed after the helper extraction with `/private/tmp/warline-ecs-audit-phase5-boarding-validation-helper-validation.log` (`UnitTransportValidation`, `tests=73`).
- Completed the boarding request validation and goal-assignment extraction by moving passenger cell/faction/footprint reads, boarding-goal search, pending-order construction, and reservation into `TryCreateTransportBoardingGoalOrder`. The three boarding paths still own command dispatch and diagnostics, but no longer duplicate goal-order construction. Full transport validation passed with `/private/tmp/warline-ecs-audit-phase5-boarding-goal-order-helper-validation.log` (`UnitTransportValidation`, `tests=73`); Unity batchmode hung after writing shutdown logs and was manually terminated after the pass result.
- Extracted shared plane-ramp search calculations into focused helpers for ramp search radius, ring-candidate filtering, approach scoring, and distance scoring. The pathing/passability checks remain in the existing boarding/disembark flows. Full transport validation passed with `/private/tmp/warline-ecs-audit-phase5-plane-ramp-calculation-helper-validation.log` (`UnitTransportValidation`, `tests=73`).
- Extracted airdrop passenger drop execution and timing into `StartPassengerDrop`, with drop duration and next-drop interval decisions isolated in `ResolveDropDurationSeconds` and `ResolveDropIntervalSeconds`. Passenger selection, landing-cell validation, and request completion remain in `ProcessAirdropRequest`. Full transport validation passed with `/private/tmp/warline-ecs-audit-phase5-airdrop-drop-execution-helper-validation.log` (`UnitTransportValidation`, `tests=73`).
- Extracted passenger capacity counts into `TransportSlotAvailability`, centralizing soldier/vehicle occupied counts, capacities, available slots, and per-passenger-kind count resolution. Boarding command paths still own diagnostics and passenger planning. Full transport validation passed with `/private/tmp/warline-ecs-audit-phase5-passenger-capacity-helper-validation.log` (`UnitTransportValidation`, `tests=73`).
- Extracted shared disembark/landing planning into `TryPlanPassengerDisembarkCells`, covering transport-ring exits, plane-ramp exits, footprint reservation, and optional plane-ramp rollout planning for bulk and single-passenger disembark. Passenger removal, visibility, and move-order issuing remain in the callers. Full transport validation passed with `/private/tmp/warline-ecs-audit-phase5-disembark-planning-helper-validation.log` (`UnitTransportValidation`, `tests=73`) and movement validation passed with `/private/tmp/warline-ecs-audit-phase5-movement-validation.log` (`UnitMovementBlockerValidation`).

## Phase 6: Test Infrastructure And Coverage

Purpose:
Make validation runners safe in the Editor and add integration coverage where the audit found gaps.

- [x] Create a shared validation-exit helper that exits only during batchmode validation.
- [x] Replace raw `EditorApplication.Exit` calls in the highest-used validation files first.
- [x] Preserve static validation methods used by automation.
- [x] Verify affected tests still run from Unity Test Runner without quitting the editor.
- [x] Add PlayMode smoke for match start to initial unit spawn.
- [x] Add PlayMode smoke for building placement to production.
- [x] Add PlayMode smoke for transport boarding to disembark.
- [x] Add PlayMode smoke for basic combat and death flow.
- [x] Add focused EditMode tests for city generation, unit combat, road build, and building placement gaps.
- [x] Run full focused validation set and at least one PlayMode smoke group.

Phase 6 notes:

- Added `ValidationExit`, a shared editor-test helper that calls `EditorApplication.Exit(code)` only when `Application.isBatchMode` is true.
- Converted the first high-use validation runners to the helper without changing static entry point names: `UnitTransportValidationTests.RunBatchValidation`, `UnitMovementBlockerValidationTests.RunBatchValidation`, `UnitMovementBlockerValidationTests.RunHoldCommandFocusedValidation`, `TransportBoardingPerformanceValidation.RunBatchValidation`, and `SelectionCommandRequestResultContractTests.RunBatchValidation`.
- `ValidationExit` batchmode behavior was smoke-validated through `/private/tmp/warline-ecs-audit-phase6-validation-exit-movement.log` (`UnitMovementBlockerValidation`, passed).
- Converted required validation-set runner exits to `ValidationExit` without changing static entry point names: `UnitRenderBudgetPerformanceValidation.RunBatchValidation`, all `BuildingProductionSystemTests` static production runners, `BuildDrawerCatalogQuerySystemTests.RunFocusedValidation`, and `ArmoryCurrentContentPrefabTests.RunFocusedValidation`.
- Batchmode validation confirmed the converted runners still exit correctly for automation while `ValidationExit` no-ops outside batchmode for Editor Test Runner safety: production `/private/tmp/warline-ecs-audit-phase6-production-validation-exit.log`, armory `/private/tmp/warline-ecs-audit-phase6-armory-validation-exit.log`, build drawer `/private/tmp/warline-ecs-audit-phase6-builddrawer-validation-exit.log`, and render budget `/private/tmp/warline-ecs-audit-phase6-renderbudget-validation-exit.log`.
- Added `InitialUnitsMatchStartPlayModeTests.MatchStartPlayRequested_SpawnsConfiguredInitialUnit`, a deterministic PlayMode smoke that verifies initial unit spawning is gated before match-start `PlayRequested` and then spawns the configured ECS unit once gameplay is marked active.
- Focused PlayMode validation passed with `/private/tmp/warline-ecs-audit-phase6-match-start-initial-spawn-playmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-match-start-initial-spawn-playmode-results.xml` (`1/1` passed).
- Added `BuildingPlacementProductionPlayModeTests.BuildDrawerPlacementThenProduction_UsesRuntimeBoundaryData`, a deterministic PlayMode smoke covering configured building camp item placement followed by producer-backed unit production queueing from runtime boundary data.
- Focused PlayMode validation passed with `/private/tmp/warline-ecs-audit-phase6-building-placement-production-playmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-building-placement-production-playmode-results.xml` (`1/1` passed).
- Added `GameSceneTransportBoardingPlayModeTests.DeterministicHelicopterBoardThenExitCommand_BoardsAndDisembarksSameSoldier`, a deterministic PlayMode smoke that boards a selected soldier into a landed transport helicopter through the command path, then requests transport exit and verifies the same passenger completes rope drop/disperse and leaves the passenger buffer.
- Focused PlayMode validation passed with `/private/tmp/warline-ecs-audit-phase6-transport-board-disembark-playmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-transport-board-disembark-playmode-results.xml` (`1/1` passed).
- Added `CombatDeathPlayModeTests.SoldierAttackDamageThenDeath_DestroysTargetWithoutRespawn`, a deterministic PlayMode smoke covering lethal standard unit attack, death cleanup, empty respawn queue, and no delayed respawn.
- Focused PlayMode validation passed with `/private/tmp/warline-ecs-audit-phase6-combat-death-playmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-combat-death-playmode-results.xml` (`1/1` passed).
- Added `RuntimeCityGenerationFocusedTests`, covering deterministic city-center/base-exclusion planning and connected town-road/autobahn layout.
- Focused EditMode validation passed with `/private/tmp/warline-ecs-audit-phase6-runtime-city-generation-editmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-runtime-city-generation-editmode-results.xml` (`2/2` passed).
- Added `UnitCombatFocusedEditModeTests.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState`, covering non-lethal standard attack damage, recent attacker state, health-bar visibility, attack cooldown, and trace state without death cleanup.
- Focused EditMode validation passed with `/private/tmp/warline-ecs-audit-phase6-unit-combat-editmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-unit-combat-editmode-results.xml` (`1/1` passed).
- Added road-build command failure-path coverage for missing runtime state and converted `RoadBuildCommandSystemTests.RunFocusedValidation` to `ValidationExit`.
- Focused EditMode validation passed with `/private/tmp/warline-ecs-audit-phase6-road-build-editmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-road-build-editmode-results.xml` (`7/7` passed).
- Added building-placement configured-placement rejection coverage for missing config and converted `BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation` to `ValidationExit`.
- Focused EditMode validation passed with `/private/tmp/warline-ecs-audit-phase6-building-placement-editmode.log` and result XML `/private/tmp/warline-ecs-audit-phase6-building-placement-editmode-results.xml` (`17/17` passed).
- Final medium-term validation sweep passed: `git diff --check`; Unity compile `/private/tmp/warline-ecs-audit-medium-final-compile.log`; render budget `/private/tmp/warline-ecs-audit-medium-final-render-budget.log`; movement `/private/tmp/warline-ecs-audit-medium-final-movement.log`; transport `/private/tmp/warline-ecs-audit-medium-final-transport.log` (`73` tests); production `/private/tmp/warline-ecs-audit-medium-final-production.log` (`21` tests); build drawer `/private/tmp/warline-ecs-audit-medium-final-builddrawer.log` (`22` tests); armory/UI `/private/tmp/warline-ecs-audit-medium-final-armory.log` (`3` tests); combat-death PlayMode smoke `/private/tmp/warline-ecs-audit-medium-final-combat-death-playmode.log` and `/private/tmp/warline-ecs-audit-medium-final-combat-death-playmode-results.xml` (`1/1` passed).

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

- [x] `git diff --check`.
- [x] Unity batchmode compile.
- [x] Rendering budget focused validation.
- [x] Unit movement focused validation.
- [x] Transport boarding focused validation.
- [x] Production/build drawer focused validation.
- [x] Armory/UI focused validation.
- [x] Relevant PlayMode smoke after test infrastructure work.

## Completion Criteria

- Quick-win phases complete and validated.
- No accidental changes to paused five-SystemBase split tracker scope.
- Hot per-frame allocation sites from the audit are either fixed or documented as false positives.
- Safe `.Run()` conversions are scheduled or documented as managed-boundary exceptions.
- Managed components are reduced only through architecture-approved boundaries.
- Runtime `Object.Instantiate` hot paths are converted, pooled, or documented as acceptable managed boundaries.
- Test validation runners no longer kill the Editor Test Runner.
- Final validation set passes.
