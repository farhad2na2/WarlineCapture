# ECS Burst Hot-Path Refactor Roadmap

## Goal

Improve runtime performance by moving frequent gameplay work toward Burst-compatible ECS jobs, reducing main-thread array copies, and batching structural changes without breaking gameplay behavior.

Target shape:
- Pure simulation/data transforms use `[BurstCompile]`, `ISystem`, `IJobEntity`, `IJobChunk`, or Burst jobs.
- Managed code remains only at real boundaries: UI views, GameObject/prefab presentation, bootstrap composition, editor tooling, config assets, and diagnostics flushing.
- Frequent-tick systems do not allocate `ToEntityArray` or `ToComponentDataArray` snapshots every frame unless explicitly justified.
- Frequent structural changes go through `EntityCommandBuffer`.
- Refactor slices are small, validated, and performance-measured.

## Current Audit Snapshot

Audit date: 2026-06-11.

Scanned root:
- `Assets/Game/Scripts/Systems`

Observed:
- About 400 system files.
- 14 system files currently use `[BurstCompile]`.
- 55 files have non-Burst `OnUpdate`.
- Main array-copy offenders:
  - `SelectionGameplayStartupSystem`: 11 `To*Array` calls.
  - `TransportBoardingCommandSystem`: 10.
  - `UnitTransportRopeDisembarkSystem`: 8.
  - `AICombatOrderSystem`: 5.
  - `RtsSelectionFocusCommandSystem`: 5.
  - `AISquadSystem`, `AIStartupSystem`, `FocusableUnitLookupSystem`: 4 each.
  - `AITargetingSystem`, `BuildingBarrierSystem`, `UnitGridMovementSystem`, `ThreatDetectionWarningSystem`: 3 each.
- Direct structural mutation is concentrated mainly in:
  - `CitizenVisibleUnitSystem`
  - `CitizenMovementCommandSystem`
  - one-time/init systems such as `DynamicBlockerInitSystem`

Important interpretation:
- Low Burst count alone is not the bug.
- The highest-value fixes are fewer per-frame array copies, fewer sync points, Burst jobs for actual simulation loops, and clearer managed/ECS boundaries.

## Architecture Constraints

- Follow `Design/Architecture/gameplay_solid_ecs_contract.md`.
- Follow `Design/Architecture/performance_regression_contract.md`.
- Do not rewrite gameplay behavior while optimizing.
- Do not add `Object.Find*`, `GameObject.Find`, `Camera.main`, hierarchy path lookup, static mutable registries, service locators, broad manager/controller/facade shells, or ungated hot-path logs.
- Do not force UI, GameObject presentation, prefab spawning, config loading, or bootstrap composition into Burst.
- Preserve `.meta` files during any move or rename.
- Every slice must pass focused tests before continuing.

## Progress Tracker

### Phase 0: Baseline And Safety Rails

Status: [ ]

Purpose:
Establish measurable baseline and prevent accidental architecture drift before optimizing.

Implementation steps:
- [ ] Create `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md` with this tracker.
- [ ] Add a current audit snapshot section with counts for Burst systems, non-Burst `OnUpdate`, `ToEntityArray`, `ToComponentDataArray`, and direct structural changes.
- [ ] Capture baseline performance for:
  - match startup after loading gate
  - select unit then move
  - attack command and missile impact flow
  - transport board/board-all/unboard flow
  - AI steady-state with current unit/building count
- [ ] Record baseline metrics:
  - frame average, p95, p99, max after warmup
  - GC allocations after warmup
  - hot system p95/p99/max where available
  - entity counts for units, buildings, projectiles, markers, visible models
- [ ] Add an allowlist for systems that are intentionally managed.

Acceptance checks:
- [ ] Full EditMode suite passes.
- [ ] Baseline report exists under `Design/AgentReports`.
- [ ] The roadmap records baseline numbers and validation command used.

### Phase 1: Audit Guardrail Tests

Status: [ ]

Purpose:
Make future regressions visible while allowing current debt to be reduced gradually.

Implementation steps:
- [ ] Add or extend architecture tests to report new frequent-tick `ToEntityArray` / `ToComponentDataArray` calls.
- [ ] Add a test/report for direct `EntityManager` structural changes in runtime systems.
- [ ] Add a test/report for non-Burst `OnUpdate` systems.
- [ ] Add an allowlist for diagnostics flush systems, bootstrap/init systems, UI/presentation boundary systems, and GameObject/prefab bridge systems.
- [ ] Make the first version report-only where needed, then ratchet to fail on new unallowlisted debt.

Acceptance checks:
- [ ] Tests pass with current allowlist.
- [ ] Report clearly identifies newly introduced hot-path debt.
- [ ] No false positives from editor/test-only code.

### Phase 2: Low-Risk Burst Conversion Pass

Status: [ ]

Purpose:
Convert simple pure ECS systems first to establish patterns without touching complex command behavior.

Candidate systems:
- `UnitIdleWanderSystem`
- `UnitManualMoveRetrySystem`
- `EngageTargetValidateSystem`
- `BaseBreachOrderSystem`
- simple cleanup/state sync systems that do not touch managed objects

Implementation steps:
- [ ] For each candidate, confirm it does not touch `UnityEngine.Object`, GameObject APIs, strings/logging, managed collections, or ScriptableObjects in `OnUpdate`.
- [ ] Add `[BurstCompile]` to the system and update lifecycle methods where valid.
- [ ] Move loop bodies into `IJobEntity` when structural changes are not needed.
- [ ] Use `EndSimulationEntityCommandBufferSystem.Singleton` for add/remove component work.
- [ ] Keep one behavior-preserving slice per system or small related group.

Acceptance checks:
- [ ] Full EditMode suite passes after each group.
- [ ] No new managed allocations in converted systems.
- [ ] No behavior changes in movement, target validation, or order cleanup tests.

### Phase 3: Replace Hot Array Copies In Selection

Status: [ ]

Purpose:
Reduce the largest known source of main-thread query snapshots while keeping active Match HUD command behavior stable.

Target areas:
- `SelectionGameplayStartupSystem`
- `RtsSelectionFocusCommandSystem`
- `FocusableUnitLookupSystem`
- `SelectionSummaryQuerySystem`
- `SelectionUiReadModelSystem`

Implementation steps:
- [ ] Split any remaining mixed selection logic into managed shell/input bridge, ECS request writers, Burst-compatible candidate/filter/scoring jobs, and managed UI presentation/read-model apply boundary.
- [ ] Replace visible-unit and closest-unit candidate collection with Burst-compatible jobs.
- [ ] Replace selected-entity snapshots with query iteration or a reused native result buffer.
- [ ] Keep UI feedback and marker presentation outside Burst.
- [ ] Keep camera actions flowing through existing camera request boundaries.

Acceptance checks:
- [ ] Select single unit.
- [ ] Select multiple soldiers through squad tray.
- [ ] Select vehicles/helicopter/jet/transport through squad tray.
- [ ] Selection panel updates.
- [ ] Command buttons still arm/deselect correctly.
- [ ] No command button click falls through to world selection.
- [ ] Array-copy count is reduced for selection files.

### Phase 4: Replace Hot Array Copies In Transport And Boarding

Status: [ ]

Purpose:
Optimize boarding candidate search, board-all, passenger state, and rope disembark without changing UX.

Target areas:
- `TransportBoardingCommandSystem`
- `UnitTransportRopeDisembarkSystem`
- `SelectionTransportCommandRequestSystem`
- transport passenger drawer read model

Implementation steps:
- [ ] Move nearest eligible passenger search into a Burst-compatible candidate job.
- [ ] Use native result buffers for board-all candidates up to remaining transport capacity.
- [ ] Replace repeated entity/component array snapshots in rope disembark with chunk/job processing.
- [ ] Batch passenger add/remove/hidden visual ECS state through ECB.
- [ ] Keep GameObject visual hiding/restoring in the managed presentation utility boundary only.

Acceptance checks:
- [ ] Soldier-first board flow works.
- [ ] Transport-first board flow works.
- [ ] Board All fills only available seats.
- [ ] Cancel exits board mode and clears feedback actions.
- [ ] Passenger drawer shows correct count, portraits, health, names, and exit buttons.
- [ ] Unboard/exit-all behavior remains correct.
- [ ] Array-copy count is reduced for transport files.

### Phase 5: AI Targeting, Squad, And Combat Optimization

Status: [ ]

Purpose:
Reduce main-thread copies in AI steady state, especially targeting and combat orders.

Target areas:
- `AITargetingSystem`
- `AICombatOrderSystem`
- `AISquadSystem`
- `AIProductionSystem`
- `AIBuildPlannerSystem`

Implementation steps:
- [ ] Convert target scanning to chunk/job-based collection.
- [ ] Build temporary native target/squad data only once per AI update tick when cross-query matching is required.
- [ ] Reuse persistent native containers owned by the system where safe.
- [ ] Batch combat/path/engage mutations through ECB.
- [ ] Keep authored AI config and policy unchanged.
- [ ] Keep diagnostics gated and outside hot loops.

Acceptance checks:
- [ ] AI squads form.
- [ ] AI targets valid enemies/buildings.
- [ ] AI combat orders still issue.
- [ ] No AI behavior regressions in existing EditMode tests.
- [ ] AI steady-state frame/system timing improves or remains equal.

### Phase 6: Structural Change Cleanup

Status: [ ]

Purpose:
Remove direct structural changes from frequent runtime loops while leaving true startup/init code alone.

Target areas:
- `CitizenVisibleUnitSystem`
- `CitizenMovementCommandSystem`
- runtime command systems that add/remove path/order components in loops

Implementation steps:
- [ ] Classify structural changes as startup-only, command-event, or frequent-tick.
- [ ] Leave startup-only structural changes unless diagnostics show they affect runtime frame rate.
- [ ] Convert frequent-tick structural changes to ECB.
- [ ] Convert repeated command-event changes to ECB where behavior ordering remains clear.
- [ ] Add tests where ECB playback timing could change visible behavior.

Acceptance checks:
- [ ] Citizens still spawn/project correctly.
- [ ] Citizen movement commands still work.
- [ ] Unit path/manual move components are added/removed in the expected frame.
- [ ] No new sync-point regressions appear in diagnostics.

### Phase 7: Pathfinding And Occupancy Protection Pass

Status: [ ]

Purpose:
Protect already optimized pathfinding semantics while removing surrounding hot-path allocations only where safe.

Target areas:
- `UnitPathfindingSystem`
- `UnitPathfindingScheduleSystem`
- `UnitPathfindingApplySystem`
- `UnitPathLiveUnitSnapshotSystem`
- `DynamicOccupancyRebuildSystem`
- `BuildingBarrierSystem`

Implementation steps:
- [ ] Do not change pathfinding algorithm, traversal costs, request budgets, or detached-job scheduling without a dedicated performance report.
- [ ] Audit surrounding snapshots and live-unit collection for avoidable allocations.
- [ ] Keep pathfinding native snapshots system-owned and persistent where already designed that way.
- [ ] Add any new path diagnostics through ECS diagnostic buffers, disabled by default.
- [ ] Use focused pathfinding validation after every path-related slice.

Acceptance checks:
- [ ] Manual group move completes.
- [ ] Long-distance move segmentation works.
- [ ] Mixed infantry/vehicle pathing works.
- [ ] Friendly-pass gates remain pathable.
- [ ] No recurring allocations after warmup.
- [ ] Pathfinding p95/p99 does not regress.

### Phase 8: Render-Budget And Visual-State Hot Paths

Status: [ ]

Purpose:
Improve visual-state update costs without moving GameObject presentation into Burst incorrectly.

Target areas:
- `UnitRenderBudgetSystem`
- `UnitMassRenderSettingsSystem`
- `UnitModelSpawnSystem`
- `UnitSelectionMarkerSystem`
- helicopter/missile visual systems where pure ECS state can be separated from GameObject presentation

Implementation steps:
- [ ] Keep model/prefab/GameObject mutation managed.
- [ ] Move pure visibility, LOD decision, distance scoring, and state-tag calculation into Burst jobs.
- [ ] Batch ECS render-state tags through ECB.
- [ ] Avoid instantiate/destroy churn; keep pooling/retained presentation.
- [ ] Preserve current visible-character detailed-model policy and impostor thresholds.

Acceptance checks:
- [ ] Unit visual LOD behavior remains correct.
- [ ] Selection markers still display.
- [ ] Helicopter blade and missile visual behavior remain correct.
- [ ] Render-budget focused scenario does not regress p95/p99.
- [ ] No new GC during steady-state camera pan/zoom.

### Phase 9: Managed Boundary Cleanup

Status: [ ]

Purpose:
Make it obvious which systems are intentionally managed and prevent accidental performance debt from hiding there.

Implementation steps:
- [ ] Name and document managed boundary systems clearly.
- [ ] Ensure managed systems do not own gameplay policy if they are presentation/binding boundaries.
- [ ] Move data-only logic out of managed composition classes into ECS systems or stateless utilities.
- [ ] Keep config projection at startup and ECS-native data during runtime.
- [ ] Update architecture docs with the Burst/managed-boundary rule.

Acceptance checks:
- [ ] Architecture tests distinguish allowed managed boundaries from hot-path debt.
- [ ] No new broad manager/controller/facade shells are introduced.
- [ ] Runtime gameplay policy is still ECS-owned.

### Phase 10: Performance Ratchet And Completion

Status: [ ]

Purpose:
Lock in improvements and prevent backsliding.

Implementation steps:
- [ ] Re-run all baseline scenarios.
- [ ] Compare before/after metrics.
- [ ] Record the final report under `Design/AgentReports`.
- [ ] Tighten architecture tests from report-only to fail-on-new-debt where stable.
- [ ] Update this roadmap with completed counts:
  - Burst systems increased where appropriate.
  - `To*Array` hot-path count reduced.
  - direct structural changes in frequent loops reduced.
  - no new managed hot-path debt.
- [ ] Keep skipped opt-in balance probes skipped unless explicitly running balance reports.

Acceptance checks:
- [ ] Full EditMode suite passes.
- [ ] Focused PlayMode/runtime smoke passes.
- [ ] `git diff --check` passes.
- [ ] Performance report shows no regression and documents improvements.
- [ ] Roadmap has no incomplete required steps.

## Per-Slice Validation Checklist

- [ ] Inspect touched systems and contracts first.
- [ ] Confirm the system is a real hot path or dependency of a hot path.
- [ ] Make the smallest behavior-preserving change.
- [ ] Run focused tests for the touched domain.
- [ ] Run full EditMode suite in the shadow project after each phase.
- [ ] Run `git diff --check`.
- [ ] Record progress notes in this roadmap.
- [ ] Remove temporary diagnostics/logs before handoff.

## Test Scenarios

Required focused scenarios:
- [ ] Match startup to loading gate complete.
- [ ] Select unit and issue Move.
- [ ] Select missile launcher and issue Attack.
- [ ] Select transport and Board All.
- [ ] Passenger drawer unboard/exit-all.
- [ ] AI steady-state with current unit/building counts.
- [ ] Camera pan/zoom while units are visible.
- [ ] Pathfinding group move and long-distance move.
- [ ] Initial units spawn with faction base and air platforms.

Required automated validation:
- [ ] Full EditMode suite.
- [ ] Existing architecture contract tests.
- [ ] Focused pathfinding validation.
- [ ] Focused render-budget validation.
- [ ] Focused selection/command validation.
- [ ] Focused transport boarding validation.

## Definition Of Done

- [ ] No valid editor tests fail.
- [ ] No gameplay UX regression is found in the focused runtime scenarios.
- [ ] Hot-path `ToEntityArray` / `ToComponentDataArray` counts are materially reduced in selection, transport, AI, and combat systems.
- [ ] Frequent structural changes use ECB unless explicitly allowed.
- [ ] Pure ECS hot systems touched by the refactor are Burst-compatible.
- [ ] Managed systems are documented as presentation/bootstrap/config/diagnostic boundaries.
- [ ] Performance reports show equal or improved p95/p99 frame time and no recurring GC regression after warmup.
