# ECS Architecture Priority Implementation Progress Tracker

**Project:** WarlineCapture
**Created:** 2026-06-24
**Scope:** `Assets/Game/Scripts/Systems`, `Assets/Game/Scripts/Rendering/Systems`, `Assets/Game/Scripts/UI/Shell/Ecs`, `Assets/Game/Scripts/Composition`, authoring/baker cleanup directly called out by the current audit.
**Purpose:** Turn the current P0/P1 architecture-performance audit into a staged implementation tracker with enough detail that future passes can continue without reinterpreting stale claims.

## Status Legend

| Status | Meaning |
|---|---|
| `Pending` | Not started. |
| `In Progress` | Being implemented or validated. |
| `Blocked` | Cannot continue without a named blocker being resolved. |
| `Complete` | Implementation and required validation are done. |
| `Skipped` | Intentionally not implemented; reason documented. |

## Current Audit Snapshot

Static source scan from 2026-06-24:

| Area | Current finding | Decision |
|---|---:|---|
| Unordered `ISystem` structs | 50 | P0 correctness risk; handle first. |
| Unguarded `ISystem` structs | 45 | P0/P1 runtime hygiene; handle with ordering sweep. |
| `MatchBootstrapSystem.cs` size | 1172 lines | P1 maintainability risk; split after P0 ECS safety. |
| `.Run()` sites in runtime/render ECS systems | 9 direct matches | Review after guards/order are stable; do not blindly parallelize. |
| `RuntimeBuildingEntityLink.Update` | About 0.163ms/frame in focused capture | No code change unless a future profile makes it hot. |
| `UiShellBoundarySystem` structural creation in `OnUpdate` | Present | Lower-risk but should be cleaned with UI shell ECS writes. |
| `UnitGridAuthoring.transform.Find` | Present in baker/authoring path | Authoring robustness issue, not per-frame runtime issue. |
| `MapSurfaceAuthoring` blob dedup | Missing | Bake/runtime memory hygiene; lower priority than ECS ordering. |

## Progress Rules

- Stage progress is tracked as a percentage from `0%` to `100%`.
- `0%` means no implementation or validation work has started.
- `25%` means source audit and exact target list are complete.
- `50%` means the first implementation batch is complete.
- `75%` means implementation is complete and focused validation is running or partially complete.
- `100%` means implementation is complete, validation result is recorded, or the stage is intentionally skipped with a decision record.
- For `Blocked` stages, keep the current percentage and document the blocker, owner, and whether another stage can continue.
- Update both the Stage Overview row and the stage detail table after every batch.

## Progress Rollup

| Metric | Current value |
|---|---:|
| Total stages | 12 |
| Complete/skipped/decision-record stages | 3 |
| In-progress stages | 1 |
| Pending stages | 8 |
| Overall stage-count progress | 29% |
| Active implementation progress excluding Stage 10 decision record | 23% |

## Non-Negotiable Guardrails

- [ ] Do not trigger Android builds. Android validation remains user-triggered.
- [ ] Do not commit or revert unrelated in-flight files from other lanes.
- [ ] Re-check source before each implementation step; do not apply stale audit instructions literally.
- [ ] Add ordering attributes only after identifying the producer/consumer relationship in source.
- [ ] Do not add `RequireForUpdate` to a system whose first responsibility is to create the entity or singleton it would require.
- [ ] Do not cache singleton component data in `OnCreate` unless it is immutable and guaranteed to exist before system creation.
- [ ] Do not cache an `EntityCommandBuffer` across frames. Cache queries/handles, not frame-scoped command buffers.
- [ ] Keep implementation batches small enough that compile or test failures identify one area.
- [ ] Update this tracker after every completed batch.

## Stage Overview

| Stage | Priority | Status | Progress | Owner | Depends on | Validation |
|---|---|---|---:|---|---|---|
| 0 - Baseline Recheck | P0 | Complete | 100% | Support | None | Static scan refreshed; Unity batch compile passed |
| 1 - ECS Ordering Plan + High-Risk Ordering Batch | P0 | Complete | 100% | Support | Stage 0 | Compile, movement/blocker validation, and attack validation passed |
| 2 - `RequireForUpdate` Sweep | P0 | In Progress | 50% | Support | Stage 0 | Command guard batches compiled and focused tests passed; 30 unguarded remain |
| 3 - Bootstrap Split Plan + First Extraction | P1 | Pending | 0% | Support | Stages 1-2 preferred | Compile + menu-to-match smoke |
| 4 - UI Shell ECS Write Safety | P1 | Pending | 0% | Support | Stages 1-2 preferred | UI shell focused tests + compile |
| 5 - Singleton/ECB Access Cleanup | P1 | Pending | 0% | Support | Stage 2 preferred | Compile + focused ECS tests |
| 6 - `.Run()` Scheduling Review | P1 | Pending | 0% | Support | Stages 1-2, profiler context | Compile + targeted perf smoke |
| 7 - ResourceHauler Diagnostics Cleanup | P1 | Pending | 0% | Support | Stage 0 | Compile + resource hauler smoke if available |
| 8 - Authoring/Baker Hygiene | P1/P2 | Pending | 0% | Support | P0 stages complete | Compile + bake/reimport validation |
| 9 - BuildingBarrier Data-Structure Review | P2 | Pending | 0% | Support | Profiler proof preferred | Profile-guided only |
| 10 - RuntimeBuildingEntityLink Decision Record | P2 | Complete | 100% | Support | Focused profiler capture | No code change recommended |
| 11 - Final Validation + Tracker Closure | P0 | Pending | 0% | Support | Stages 1-9 complete/skipped | Compile + focused tests + final static scan |

---

## Stage 0 - Baseline Recheck

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P0 |
| Owner | Support |
| Dependencies | None |
| Blocks | Every implementation stage |

**Goal:** Confirm current counts and dirty state before changing code.

**Implementation Steps**

1. Run `git status --short --untracked-files=no`.
2. Record unrelated dirty files in this tracker before editing.
3. Re-run static scans:
   - unordered `ISystem` structs,
   - unguarded `ISystem` structs,
   - `.Run()` sites,
   - direct UI shell `EntityManager.SetComponentData` writes from gateway/view-facing code.
4. If Unity Editor is closed and licensing is available, run a batch compile before edits.
5. Do not proceed if compile is already broken unless the broken area is the target.

**Acceptance Criteria**

- [x] Dirty state documented.
- [x] Static counts refreshed.
- [x] Baseline compile result recorded or blocker documented.

**Validation Commands**

- Static only:
  - `rg -n "partial struct .*ISystem|: ISystem" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs`
  - `rg -n "\.Run\(" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs`
- Unity compile command should use the project-standard batchmode workflow already used in this repo.

**Notes**

- Initial 2026-06-24 audit found about 50 unordered and 45 unguarded `ISystem` structs.
- 2026-06-24 heartbeat baseline recheck: `git status --short --untracked-files=no` returned no tracked dirty files.
- 2026-06-24 heartbeat baseline recheck: Unity Hub and Unity Licensing Client were running, but no Unity Editor process was open.
- 2026-06-24 heartbeat baseline static scan:
  - `ISystem` structs scanned: `138`.
  - unordered `ISystem` structs: `50`.
  - unguarded `ISystem` structs: `45`.
  - direct `.Run()` sites in runtime/render/UI shell ECS roots: `9`.
  - direct UI shell gateway loading-progress write confirmed at `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs:103`.
  - `UiShellBoundarySystem` structural creation in `OnUpdate` confirmed at `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs:51`.
- 2026-06-24 heartbeat baseline compile: Unity `6000.4.0f1` batch compile passed with `Exiting batchmode successfully now!`; log: `/private/tmp/warline-ecs-stage0-compile.log`.

---

## Stage 1 - ECS Ordering Plan + High-Risk Ordering Batch

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 0 |
| Blocks | Reliable gameplay startup, movement, spawn, blocker, and UI presentation ordering |

**Goal:** Add explicit `[UpdateInGroup]`, `[UpdateAfter]`, and `[UpdateBefore]` attributes for systems where source shows a producer/consumer relationship.

**Why this is P0**

Unordered systems can appear correct in one Unity/editor run and change behavior after domain reloads, asmdef changes, package updates, or platform-specific player builds. This is a latent correctness risk, not only a performance issue.

**High-Risk Files To Audit First**

| File | Current issue | What to verify before editing |
|---|---|---|
| `Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs` | No explicit ordering attribute on system struct. | Which systems write `UnitPathFollow`, `UnitMoveOrder`, path buffers, occupancy data, and which systems consume movement results. |
| `Assets/Game/Scripts/Systems/AttackOrderCommandSystem.cs` | Unordered and currently unguarded. | Which UI/AI systems enqueue attack commands and which request/execution systems consume them. |
| `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs` | Unordered and currently unguarded. | Whether it is composition-only helper or live ECS update; avoid fake ordering if it should not run every frame. |
| `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs` | Unordered and currently unguarded. | Which systems require prefab registry availability before spawning. |
| `Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs` | Guarded but unordered. | Whether blocker init must run after grid creation and before occupancy/path/spawn systems. |
| `Assets/Game/Scripts/Systems/UnitHealthBarSystem.cs` | Guarded but unordered. | Whether health bar presentation should run after combat/death and before render presentation cleanup. |

**Implementation Steps**

1. Create a temporary source audit table for the high-risk files:
   - components/buffers read,
   - components/buffers written,
   - singleton dependencies,
   - command buffer dependencies,
   - obvious producer system,
   - obvious consumer system.
2. Add `[UpdateInGroup(typeof(SimulationSystemGroup))]` or a narrower existing group only when the current system is truly a simulation system.
3. Add `[UpdateAfter]` for direct data producers only.
4. Add `[UpdateBefore]` for direct data consumers only.
5. Avoid ordering against broad unrelated systems just to silence the count.
6. Compile after the first high-risk batch before touching the remaining unordered systems.
7. Continue remaining unordered systems in small groups:
   - movement/order systems,
   - spawn/prefab systems,
   - blocker/grid systems,
   - selection command systems,
   - transport systems,
   - diagnostics/read-model/presentation systems.

**Acceptance Criteria**

- [x] Every high-risk file above has an explicit ordering decision.
- [x] If a high-risk file remains unordered, this tracker documents why.
- [x] No new circular or contradictory update ordering errors.
- [x] Static unordered count is lower and remaining items are either documented or moved to a later batch.

**Focused Validation**

- [x] Unity compile.
- [ ] Match start smoke in Editor if available.
- [x] Move order smoke: select unit, issue move, confirm movement starts.
- [x] Attack order smoke: select combat unit, issue attack, confirm command is consumed.
- [ ] Spawn smoke: initial units and produced units still resolve prefabs.
- [x] Blocker/path smoke: units do not move through blocked building cells.

**Known Risks**

- Adding order attributes without source proof can hide a deeper dependency issue.
- Startup systems that create singleton/boundary entities may require initialization-group ordering instead of simulation ordering.

**Notes**

- 2026-06-24 heartbeat: high-risk source audit completed.
- Added explicit ordering to:
  - `Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs`: `SimulationSystemGroup`, after `RuntimeGridDeduplicationSystem`, before `StaticGridBlockerUpdateSystem` and `DynamicOccupancyRebuildSystem`.
  - `Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs`: `SimulationSystemGroup`, after `UnitPathfindingSystem`, `DynamicOccupancyRebuildSystem`, and `UnitEngagedMovementSystem`.
  - `Assets/Game/Scripts/Systems/AttackOrderCommandSystem.cs`: `SimulationSystemGroup`, before `UnitAttackOrderRequestSystem`.
  - `Assets/Game/Scripts/Systems/UnitHealthBarSystem.cs`: `SimulationSystemGroup`, after `UnitRuntimeHealthBarSystem`, before `EngageTargetValidateSystem`.
- `BuildingGridCompositionSystem` and `BuildingSpawnPrefabSystem` remained unordered by decision: both are disabled helper/composition `ISystem` structs with `state.Enabled = false` and empty `OnUpdate`; forcing runtime ordering would be fake signal. They should be handled later by converting to non-system helpers or documenting/classifying disabled helper systems.
- Attempted but rejected ordering edge: `AttackOrderCommandSystem` after `UiActionRequestSystem`. Unity compile failed because this would make `Game.Runtime` reference UI shell ECS implementation directly. The edge was removed to preserve assembly boundaries.
- Static unordered count after the safe high-risk batch: `46`, down from `50`.
- Validation:
  - Unity compile passed: `/private/tmp/warline-ecs-stage1-ordering-compile-2.log`.
  - `UnitMovementBlockerValidationTests.RunBatchValidation` passed: `/private/tmp/warline-ecs-stage1-unit-movement-blocker.log`.
  - `GroundMissileLauncherRuntimeTests.RunAttackFocusedValidation` passed: `/private/tmp/warline-ecs-stage1-attack-focused.log`.

---

## Stage 2 - `RequireForUpdate` Sweep

| Field | Value |
|---|---|
| Status | In Progress |
| Progress | 50% |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 0 |
| Blocks | Reduced idle frame work and safer singleton access |

**Goal:** Ensure `ISystem.OnUpdate` does not run every frame unless the system has a real reason to run without matching data.

**Current Priority Files**

Initial 2026-06-24 scan found about 45 unguarded systems, including command systems, startup systems, composition systems, diagnostics, and UI shell ECS boundary.

**Implementation Rules**

1. If the system reads a singleton, add `state.RequireForUpdate<ThatSingleton>()` or require its cached query.
2. If the system consumes command buffers, require the command boundary/singleton that owns those buffers.
3. If the system processes units/buildings/factions, require the component type that defines the minimum useful workload.
4. If the system creates the singleton it needs, do not require that singleton. Use one of:
   - create in `OnCreate`,
   - run once and `state.Enabled = false`,
   - require a separate bootstrap/lifecycle marker.
5. If a system is a pure helper accidentally implemented as `ISystem`, consider whether it should become a normal helper class instead of adding a meaningless guard.

**Implementation Steps**

1. Split unguarded list into categories:
   - singleton creator/bootstrap,
   - command queue consumer,
   - runtime simulation,
   - read-model/presentation,
   - diagnostics,
   - composition/helper.
2. Handle runtime simulation and command queue consumers first.
3. Handle read-model/presentation second.
4. Leave singleton creators for Stage 4 or dedicated bootstrap cleanup.
5. Re-run static scan after each batch.

**Acceptance Criteria**

- [ ] Every unguarded system has either a `RequireForUpdate` or a documented reason for no guard.
- [ ] No system is guarded by the entity/component it is responsible for creating.
- [ ] No new startup deadlocks where a required singleton is never created.

**Focused Validation**

- [x] Unity compile.
- [ ] Match start smoke.
- [ ] UI shell opens main menu and transitions to match.
- [ ] Initial spawn still completes.

**Notes**

- 2026-06-24 heartbeat: command queue guard batch completed.
- Added `RequireForUpdate` guards to command queue consumers:
  - `AttackOrderCommandSystem`
  - `UnitAttackOrderRequestSystem`
  - `UnitMoveOrderRequestSystem`
  - `TransportBoardingCommandSystem`
  - `ScanIntelCommandSystem`
- Added `RequireForUpdate` guards to disabled-auto-creation RTS selection command systems that still have live `OnUpdate` command processing when composed:
  - `RtsSelectionAttackTargetModeCommandSystem`
  - `RtsSelectionBoardTargetModeCommandSystem`
  - `RtsSelectionCancelActiveCommandModeSystem`
  - `RtsSelectionDeselectAllCommandSystem`
  - `RtsSelectionImmediateSelectedUnitCommandSystem`
  - `RtsSelectionMissileLauncherRadarAttackCommandSystem`
  - `RtsSelectionModeCommandSystem`
  - `RtsSelectionMoveTargetModeCommandSystem`
  - `RtsSelectionScanTargetModeCommandSystem`
  - `RtsSelectionSelectAllCommandSystem`
- Static unguarded count moved from `45` to `40` after the first command batch, then to `30` after the RTS selection command batch.
- Validation:
  - Unity compile passed: `/private/tmp/warline-ecs-stage2-guards-compile.log`.
  - Unity compile passed after RTS selection command batch: `/private/tmp/warline-ecs-stage2-guards-compile-2.log`.
  - `ScanIntelCommandSystemTests.RunFocusedValidation` logged `result=Passed tests=2`: `/private/tmp/warline-ecs-stage2-scan-focused.log`. The Unity batch process did not exit after the pass marker and was stopped manually after success.
  - `SelectionCommandRequestResultContractTests.RunBatchValidation` passed with `tests=48`: `/private/tmp/warline-ecs-stage2-selection-command.log`.
  - `SelectionCommandRequestResultContractTests.RunBatchValidation` passed again after the RTS selection command batch with `tests=48`: `/private/tmp/warline-ecs-stage2-selection-command-2.log`.
  - `UnitTransportValidationTests.RunBatchValidation` passed with `tests=73`: `/private/tmp/warline-ecs-stage2-transport.log`.
- Remaining unguarded systems are now mostly startup/bootstrap creators, disabled composition/helper systems, runtime state/read-model systems, and presentation/diagnostics systems. They need classification before adding guards so singleton creators are not deadlocked by requiring the entities they create.

---

## Stage 3 - Bootstrap Split Plan + First Extraction

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stages 1-2 preferred |
| Blocks | Long-term composition maintainability |

**Goal:** Reduce `MatchBootstrapSystem.cs` from a 1172-line god object into smaller composition helpers without changing scene references, MonoBehaviour identity, or gameplay behavior.

**Current Finding**

`Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs` is 1172 lines. This is now the largest maintainability risk in the audited list.

**Refactor Strategy**

Do not rewrite the bootstrap. Extract one cohesive responsibility at a time. Prefer static/internal helper classes or small composition services with no scene serialization changes.

**Candidate Extraction Order**

1. **Scene reference discovery and validation**
   - Move pure lookup/validation helpers first.
   - No behavior change; easiest compile-only validation.
2. **UI binding/composition**
   - Move code that wires concrete UI views to contracts.
   - Preserve composition assembly ownership.
3. **ECS query/entity boundary setup**
   - Move repeated query creation and boundary resolution.
   - Keep query lifetime obvious; do not hide disposable state.
4. **Match start/lifecycle transitions**
   - Move request/start/progress wiring after UI and ECS boundaries are clear.
5. **Diagnostics/profiler marker setup**
   - Move last unless needed by earlier extractions.

**Implementation Steps**

1. Read the file and create a method inventory table:
   - method name,
   - responsibility,
   - fields touched,
   - safe extraction target.
2. Select one extraction with the fewest field dependencies.
3. Create a helper under `Assets/Game/Scripts/Composition`.
4. Keep method signatures explicit; do not introduce service locator/global state.
5. Compile after each extraction.
6. Update this tracker with remaining line count.

**Acceptance Criteria**

- [ ] First extraction compiles.
- [ ] No scene/prefab serialized reference changes.
- [ ] `MatchBootstrapSystem.cs` line count decreases.
- [ ] Extracted helper has one responsibility and no hidden global state.

**Focused Validation**

- [ ] Unity compile.
- [ ] Menu opens.
- [ ] Deploy button enters match.
- [ ] Existing architecture tests still pass if composition boundaries changed.

---

## Stage 4 - UI Shell ECS Write Safety

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stages 1-2 preferred |
| Blocks | Consistent UI-to-ECS architecture |

**Goal:** Remove direct UI-facing ECS writes where they violate the established command-buffer/boundary pattern.

**Confirmed Sites**

| File | Site | Current issue |
|---|---|---|
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs` | `TrySetLoadingProgress` | Direct `EntityManager.SetComponentData` from gateway-facing API. |
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs` | `OnUpdate` | Creates entity and adds many components/buffers inside `OnUpdate`. |

**Implementation Steps**

1. Add a UI shell loading-progress command buffer component if one does not already exist.
2. Change `UiShellRuntimeGateway.TrySetLoadingProgress` / `UiShellEcsGateway.TrySetLoadingProgress` to enqueue a request instead of direct `SetComponentData`.
3. Add or reuse a UI shell ECS system that consumes the request and writes `UiShellLoadingProgressComponent`.
4. Move `UiShellBoundarySystem` singleton creation out of normal `OnUpdate` if safe:
   - preferred: create/seed boundary in `OnCreate` if no dependency prevents it,
   - acceptable: use an initialization ECB and disable after playback,
   - fallback: keep current self-disable behavior but document why.
5. Keep duplicate-boundary cleanup behavior if it is still required.

**Acceptance Criteria**

- [ ] Gateway-facing loading progress writes use command/request flow.
- [ ] `UiShellBoundarySystem.OnUpdate` no longer performs large structural creation, or the exception is documented with reason.
- [ ] Main menu loading progress still updates.
- [ ] Match intro/loading UI still updates.

**Focused Validation**

- [ ] Unity compile.
- [ ] `UIShellCurrentContentLoadTests` or equivalent focused UI shell validation.
- [ ] Menu-to-match smoke.

**Risks**

- Moving boundary creation too early can break tests or worlds that expect to seed a boundary manually.
- Command queues must be available before views call the runtime gateway.

---

## Stage 5 - Singleton/ECB Access Cleanup

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 2 preferred |
| Blocks | Main-thread cleanup and cleaner ECS patterns |

**Goal:** Reduce repeated per-frame singleton query overhead without introducing stale data or invalid command buffer lifetime.

**Important Rule**

Do not cache singleton component values in `OnCreate` unless immutable. Most cleanup should cache `EntityQuery`, `ComponentLookup`, `BufferLookup`, or type handles, then call `.Update(ref state)` in `OnUpdate`.

**Targets**

1. Repeated `GridConfig` reads:
   - use cached grid query/entity where the grid is stable,
   - still read current component data in `OnUpdate`.
2. Repeated `EndSimulationEntityCommandBufferSystem.Singleton` reads:
   - keep ECB creation frame-local,
   - cache the query only if useful.
3. Repeated diagnostics/runtime-state singleton reads:
   - batch once per system update and pass as job data or local booleans.

**Implementation Steps**

1. Inventory hot systems with repeated singleton reads.
2. Start with systems already high in profiler or called during match startup.
3. Replace repeated `SystemAPI.GetSingleton<T>()` calls inside one `OnUpdate` with a single local read.
4. For systems creating queries manually every update, move query creation to `OnCreate`.
5. Compile after each small batch.

**Acceptance Criteria**

- [ ] No cached command buffer is reused across frames.
- [ ] No mutable singleton data is cached across frames.
- [ ] Repeated same-frame singleton reads are reduced in touched files.
- [ ] Unity compile passes.

---

## Stage 6 - `.Run()` Scheduling Review

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stages 1-2; profiler context preferred |
| Blocks | Parallelism improvements |

**Goal:** Convert safe `.Run()` jobs to scheduled jobs, but only where dependencies, native containers, and managed side effects allow it.

**Current Direct `.Run()` Matches**

| File | Site count | First decision |
|---|---:|---|
| `Assets/Game/Scripts/Systems/AITargetingSystem.cs` | 1 | Candidate for scheduling review. |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSortSystem.cs` | 1 | Candidate, but sort dependencies must be checked. |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetBandSystem.cs` | 1 | Candidate, but budget data dependencies must be checked. |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSnapshotSystem.cs` | 1 | Candidate, but snapshot publication ordering must be checked. |
| `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs` | 2 | Review after recent ECB cleanup; may have snapshot ordering constraints. |
| `Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs` | 1 | Review for UI/diagnostic side effects. |
| `Assets/Game/Scripts/Systems/FocusableUnitLookupSystem.cs` | 1 | Review for lookup/snapshot publication constraints. |
| `Assets/Game/Scripts/Systems/UnitDestroyedVisualSystem.cs` | 1 | Presentation/ECB cleanup; may remain `.Run()` if tiny. |

**Implementation Steps**

1. For each `.Run()` site, document:
   - job type,
   - data read/write,
   - native containers used,
   - whether it writes ECB,
   - whether later same-frame managed code depends on completion.
2. Convert only one file at a time.
3. Prefer `Schedule` before `ScheduleParallel` if write conflicts or ordering are unclear.
4. If same-frame output is required, scheduling plus immediate `Complete()` is usually not an improvement; document and skip.
5. Re-run focused compile/validation after each conversion.

**Acceptance Criteria**

- [ ] Each `.Run()` site is either converted or has a documented reason to remain.
- [ ] No safety exceptions from unresolved job dependencies.
- [ ] No behavior change in selection, targeting, render budget, or destroyed visuals.

---

## Stage 7 - ResourceHauler Diagnostics Cleanup

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 0 |
| Blocks | Cleaner logs and less avoidable warning spam |

**Goal:** Keep important ResourceHauler warnings actionable without allowing repeated warning spam in normal gameplay.

**Confirmed Finding**

`Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs:308` logs `invalid-capacity` unconditionally.

`Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs:360` is already guarded by `VerboseResourceHaulerLogs`; the earlier audit claim that both were unguarded is stale.

**Implementation Options**

1. If `invalid-capacity` is a configuration error:
   - keep warning,
   - throttle it per prefab/unit type or entity,
   - include enough context to fix config.
2. If it can occur normally:
   - guard behind `VerboseResourceHaulerLogs`,
   - optionally emit one aggregate diagnostic counter.

**Implementation Steps**

1. Inspect how `BarrelCapacity` is authored and whether zero capacity is valid.
2. Decide one-time warning vs verbose-only guard.
3. Implement minimal change.
4. Compile.

**Acceptance Criteria**

- [ ] Warning behavior is intentional and documented.
- [ ] No repeated warning spam from the same invalid hauler.
- [ ] Resource hauler behavior is unchanged except logging.

---

## Stage 8 - Authoring/Baker Hygiene

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1/P2 |
| Owner | Support |
| Dependencies | P0 stages complete |
| Blocks | Bake robustness and asset consistency |

**Goal:** Remove fragile authoring-time string lookup and duplicate blob creation without breaking existing prefabs/scenes.

### 8A - `UnitGridAuthoring.transform.Find`

**Current Sites**

- `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs:471`
- `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs:472`
- `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs:1114`

**Important Classification**

These calls are in the authoring/baker path, not normal per-frame match runtime. This should not be sold as an FPS fix.

**Implementation Steps**

1. Add serialized optional fields:
   - `Transform modelRoot`,
   - `Transform destroyedRoot`.
2. In the baker, prefer serialized references.
3. Temporarily keep `transform.Find("Model")` / `transform.Find("Destroyed")` fallback for existing prefabs.
4. Add an editor validation helper/report to find prefabs where the serialized fields are missing.
5. Populate references through an editor migration if needed.
6. After migration, decide whether to keep fallback for compatibility or remove it.

**Acceptance Criteria**

- [ ] Existing prefabs still bake before migration.
- [ ] New prefabs can use explicit references.
- [ ] Missing references are detectable.

### 8B - `MapSurfaceAuthoring` blob dedup

**Current Site**

- `Assets/Game/Scripts/Authorings/MapSurfaceAuthoring.cs:29`

**Implementation Steps**

1. Confirm whether multiple `MapSurfaceAuthoring` components can share the same `MapSurfaceDataAsset`.
2. Add a baker-local or bake-session cache keyed by `MapSurfaceDataAsset` if Unity baking lifecycle supports it safely.
3. Ensure `AddBlobAsset` is still used correctly so Unity owns the blob lifetime.
4. Validate with scene bake/reimport.

**Acceptance Criteria**

- [ ] Shared surface assets do not create duplicate equivalent blobs.
- [ ] Blob lifetime remains managed by Unity baking.
- [ ] Map surface systems still receive valid `MapSurfaceComponent`.

---

## Stage 9 - BuildingBarrier Data-Structure Review

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P2 |
| Owner | Support |
| Dependencies | Profiler proof preferred |
| Blocks | Optional AI breach-path optimization |

**Goal:** Decide whether `BuildingBarrierSystem` dictionary iteration is worth changing.

**Current Finding**

`Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs` enumerates `RuntimeBuildings` in several paths. Some paths already special-case concrete `Dictionary<int, RuntimeBuildingEntity>` before falling back to `IReadOnlyDictionary` enumeration.

**Decision**

Do not convert to `NativeHashMap` blindly. This system works with managed `RuntimeBuildingEntity` objects and `RectInt`/building definitions. A Native container migration may force a larger data-model refactor.

**Implementation Steps**

1. Profile the AI breach / road barrier path with realistic building counts.
2. If hot, first consider a managed snapshot list or cached array of active barrier buildings.
3. Use `NativeHashMap` only if the data can be represented as blittable IDs/rects/faction fields without managed object access.
4. Keep behavior identical for gate opening/closing and perimeter detection.

**Acceptance Criteria**

- [ ] No change unless profiler proves this path is hot or allocation-heavy.
- [ ] If changed, barrier/gate behavior is validated with enemy wall/gate scenarios.

---

## Stage 10 - RuntimeBuildingEntityLink Decision Record

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P2 |
| Owner | Support |
| Dependencies | Focused profiler capture |
| Decision | No code change now |

**Finding**

`Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs` has one `MonoBehaviour.Update` per linked building.

**Profiler Evidence**

Focused capture documented in `Design/AgentReports/2026-06-23_audit-architecture-performance-implementation-plan.md`:

- `RuntimeBuildingEntityLink.Update`: about `326.34ms` total over `2000` frames.
- About `0.163ms/frame` for roughly `163` links.
- Max sample about `0.014ms`.
- `0` GC bytes.

**Decision**

Do not convert to jobs or remove it in this pass. It is not currently an actionable top bottleneck compared with building placement/input/pathfinding markers.

**Reopen Criteria**

- New capture shows `RuntimeBuildingEntityLink.Update` materially above `1.0ms/frame`.
- Building counts increase enough that the per-building update becomes visible in top frame markers.
- Entities Graphics fully replaces the GameObject building presentation path.

---

## Stage 11 - Final Validation + Tracker Closure

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stages 1-9 complete/skipped |
| Blocks | Closing this remediation tracker |

**Goal:** Verify the codebase is safer after the staged cleanup and update the decision record.

**Final Checklist**

- [ ] Static unordered `ISystem` count refreshed.
- [ ] Static unguarded `ISystem` count refreshed.
- [ ] `.Run()` sites refreshed and documented.
- [ ] `MatchBootstrapSystem.cs` line count refreshed.
- [ ] Unity compile passes.
- [ ] Architecture tests pass if boundaries changed.
- [ ] Focused tests/smokes for touched systems pass.
- [ ] This tracker marks every stage `Complete`, `Skipped`, or `Blocked` with blocker and owner.

**Final Report Should Include**

- Files changed.
- Contracts touched.
- User-visible behavior.
- Validation run.
- Validation result.
- Known gaps.
- Next recommended task.
