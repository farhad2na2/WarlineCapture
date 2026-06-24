# Architecture & Performance Audit — Re-Verification Report

**Project:** WarlineCapture-Clone (Unity 6, Entities 6.4 / DOTS, URP)
**Re-verification date:** 2026-06-24
**Auditor:** opencode (read-only)
**Baseline audit:** `Design/AgentReports/2026-06-23_audit-architecture-performance-implementation-plan.md`
**Scope:** `Assets/Game/Scripts` (745 .cs files, 14 asmdefs) — re-checking all P0/P1/P2 tasks from the baseline plan against current `HEAD` (`4a0478c0c Fix ECS startup ordering cycle`).

> **Purpose:** This document records, per task, whether the baseline-plan remediation has been addressed, partially addressed, regressed, or not started — with concrete file:line evidence and the delta since the last verification. It is intended to drive the next sprint: each "OPEN" item below is actionable and ordered by priority.

---

## Progress at a glance

| Bucket | Done | Partial | Not started | Regressed |
|---|---|---|---|---|
| P0 Critical | 2 (P0-2, P0-3) | 3 (P0-1, P0-4, P0-5) | 1 (P0-6, deferred) | 0 |
| P1 Major    | 4 (P1-3a/b, P1-3c, P1-7a, P1-7c) | 4 (P1-1, P1-2, P1-4b, P1-5) | 5 (P1-4a, P1-6, P1-7d, P1-8, +others) | 0 |
| P2 Minor     | 0 | 1 (P2-4) | 3 (P2-1, P2-6, +others) | 0 |

Net: **6 tasks fully resolved**, **8 partial**, **9 not started / regressed** since the baseline plan.

---

## P0 — Critical

### P0-1 — `[RequireMatchingQueriesForUpdate]` / `RequireForUpdate` guards — PARTIAL (~77%)

- **Status:** Partial — 96 of 125 ISystem files now carry a guard (was 79 last check). ~29 still unguarded.
- **Delta:** +17 files guarded since last verification.
- **Why it matters:** Without a guard, `OnUpdate` runs every frame regardless of matching entities → wasted CPU on ~29 systems. This was the highest-ROI perf fix in the baseline plan and is still the largest wasted-CPU issue remaining.
- **How to finish:**
  1. Enumerate the 29 remaining unguarded ISystem files: `grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "RequireForUpdate\|RequireMatchingQueriesForUpdate" {} || echo {}'`
  2. For each, add either `[RequireMatchingQueriesForUpdate]` on the struct (single-query systems) or `state.RequireForUpdate(_query)` in `OnCreate` (multi-query).
  3. For bootstrap/diagnostic systems that legitimately run every frame, add a `// intentionally runs every frame` comment and skip.
- **Acceptance:** `grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "RequireForUpdate\|RequireMatchingQueriesForUpdate" {} || echo {}'` returns only files with the intentional-run comment.
- **Priority:** P0 — finish next.

### P0-2 — `MissileTrailVfxView.Update` managed-dictionary foreach — DONE

- **Status:** Resolved. No `foreach.*_active` match in `Effects/MissileTrailVfxView.cs`.
- **Verification:** `grep -n "foreach.*_active" Assets/Game/Scripts/Effects/MissileTrailVfxView.cs` returns nothing.
- **No further action.**

### P0-3 — `TerrainLodHeightSwitch.Update` `Camera.allCameras` array alloc — DONE

- **Status:** Resolved. No `Camera.allCameras[^C]` match (the array-allocating form) anywhere under `Assets/Game/Scripts`.
- **Verification:** `grep -rn "Camera\.allCameras[^C]" Assets/Game/Scripts --include="*.cs"` returns nothing.
- **No further action.**

### P0-4 — System ordering attributes — PARTIAL (~36% done, 45 still unordered) — HIGHEST REMAINING RISK

- **Status:** Partial — 45 of 125 ISystem files still lack `UpdateInGroup`/`UpdateBefore`/`UpdateAfter` (was 49 last check, was 68 at baseline).
- **Delta:** -4 unordered files since last verification. Barely moved despite commit `d18a4b614 Add high-risk ECS system ordering` and `4a0478c0c Fix ECS startup ordering cycle`.
- **Why it matters — this is now the top priority:** Inter-system producer↔consumer pairs execute in undefined order, causing latent correctness bugs (movement applied before pathfinding resolves, orders consumed before requests produced, AI reading stale state). The recent `Fix ECS startup ordering cycle` commit indicates the team is already fighting ordering bugs — the root cause is the 45 unordered systems.
- **Still-unordered high-risk files (sample):**
  - `Rendering/Systems/UnitModelSpawnSystem.cs`
  - `Systems/DynamicBlockerInitSystem.cs`
  - `Systems/UnitHealthBarSystem.cs`
  - `Systems/BuildingGridCompositionSystem.cs`
  - `Systems/BuildingSpawnPrefabSystem.cs`
  - `Systems/BuildingTargetMoveOrderSystem.cs`
  - `Systems/BuildingStartupConfigProjectionSystem.cs`
  - `Systems/AttackOrderCommandSystem.cs`
  - `Systems/UnitGridMovementSystem.cs`  ← core movement, must run after pathfinding
  - `Systems/VisibleUnitSelectionCandidateSystem.cs`
  - `Systems/BuildingGameplayChildSystem.cs`
  - `Systems/PreGameEcsActivityDiagnosticsSystem.cs`
  - `Systems/AIPlanEntryStartupSystem.cs`
  - `Systems/RtsSelectionBoardTargetModeCommandSystem.cs`
  - `Systems/RtsSelectionDeselectAllCommandSystem.cs`
  - (and ~30 more)
- **How to finish:**
  1. Assign each unordered ISystem to a group: simulation systems → `[UpdateInGroup(typeof(SimulationSystemGroup))]`; presentation/visual → `PresentationSystemGroup`; bootstrap/one-shot → `InitializationSystemGroup` or a custom `MatchBootstrapSystemGroup`.
  2. Add `[UpdateAfter(typeof(ProducerSystem))]` on consumers for each producer↔consumer pair. Highest-risk pairs to order first:
     - `UnitPathfindingSystem` → `UnitGridMovementSystem` (pathfinding before movement)
     - `UnitMoveOrderRequestSystem` → `UnitMoveOrderSystem`
     - `UnitAttackOrderRequestSystem` → `AttackOrderCommandSystem`
     - `BuildingStartupConfigProjectionSystem` → `BuildingGridCompositionSystem` → `BuildingSpawnPrefabSystem` → `BuildingTargetMoveOrderSystem`
     - `RuntimeGameplayStateSystem` → AI chain head
  3. For systems with no inter-dependency, only `[UpdateInGroup]` is needed.
- **Acceptance:** `grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "UpdateInGroup\|UpdateBefore\|UpdateAfter" {} || echo {}'` returns only bootstrap files marked `// intentionally default-ordered`.
- **Priority:** P0 — do immediately. This is the single largest correctness-risk outstanding.

### P0-5 — `UiShellBoundarySystem.OnUpdate` direct EntityManager structural changes — PARTIAL (demoted to P1)

- **Status:** Partial — `GetSingletonEntity` is now guarded by `boundaryQuery.CalculateEntityCount()` (l.17). But `OnUpdate` still calls `state.EntityManager.CreateEntity(...)` + ~40 `AddComponentData`/`AddBuffer` (l.59+) when the boundary entity is missing. The system now self-disables via `state.Enabled = false` (l.14) after the first init, so the structural change happens at most once per world — acceptable in practice but still wrong in pattern.
- **Delta:** No structural change since last verification.
- **Why it matters (downgraded):** Because the system self-disables after first run, the practical risk is low. The pattern is still non-idiomatic (should be `OnCreate` + guard, or ECB-backed) and sets a bad precedent.
- **How to finish:**
  1. Move the boundary-entity creation + component init into `OnCreate` (one-shot) guarded by a static/`bool` flag, OR
  2. Use `BeginSimulationEntityCommandBufferSystem.Singleton` ECB instead of direct `EntityManager` calls, OR
  3. Leave as-is and document `// one-shot init, self-disables after first run` — acceptable if the team prefers the current pattern.
- **Priority:** P1 (demoted from P0) — low urgency given the self-disable.

### P0-6 — Namespaces per assembly — NOT STARTED (deferred per user)

- **Status:** 742 of 745 files still in the global namespace.
- **Delta:** No change. Deferred per user request for this round.
- **Why it matters (long-term):** Type collisions only prevented by file-unique naming; `internal` is effectively project-global; intellisense/Go-to-symbol noisy. Remains the largest long-term architecture-hygiene debt.
- **How to do it (when scheduled):** See baseline plan §P0-6 — set `rootNamespace` per asmdef, wrap each file in `namespace <asmdef-name> { … }`, do leaves-first (Contracts/Catalog/Components), compile after each assembly, fix collisions.
- **Priority:** Deferred — schedule a dedicated sprint.

---

## P1 — Major

### P1-1 — `[BurstCompile]` on hot ISystem structs — PARTIAL (~42%)

- **Status:** Partial — 72 of 125 ISystem files still lack `[BurstCompile]` anywhere (unchanged since last verification).
- **Delta:** 0 new Burst added since last check. No progress.
- **Why it matters:** ~72 systems run `OnUpdate` in managed mode → no Burst optimization on combat, AI, movement, transport, render-budget hot loops. Combined with P0-1 (guards now mostly in place), Bursted systems would skip + run fast — the two wins compound.
- **How to finish:**
  1. Add `[BurstCompile]` on the struct AND `OnUpdate` (both required — struct-level does NOT auto-apply to methods).
  2. If `OnUpdate` uses managed refs (string, `Debug.Log`, `EntityManager` managed APIs), either:
     - Extract the hot loop into a Burst-compiled `IJobEntity`/`IJobChunk` and keep `OnUpdate` as a thin managed orchestrator, OR
     - Remove the managed call (`Debug.Log` → `NativeQueue`/`FixedString` diagnostic buffers; `EntityManager` → ECB).
  3. For `UnitGridMovementSystem.cs:662`: struct has `[BurstCompile]` but `OnUpdate` does NOT — add `[BurstCompile]` to `OnUpdate` explicitly.
  4. Order combat/AI/movement first (highest frame cost): `UnitAttackSystem`, `UnitPathfindingSystem`, `UnitDeathSystem`, `UnitRespawnSystem`, `AITargetingSystem`, `AICombatOrderSystem`, `AIProductionSystem`, `AIBuildPlannerSystem`, `AISquadSystem`, `AIEconomySystem`, `AIFactionControlSystem`, `UnitMoveOrderSystem`, `UnitMoveOrderRequestSystem`, `UnitTargetOrderSystem`, `UnitAttackOrderRequestSystem`, all 7 `UnitTransport*System`, `CitizenMovementCommandSystem`, `UnitRenderBudgetSystem`, `UnitSelectionMarkerSystem`, `UnitHelicopterBladeSpinSystem`, `UnitFactionTintTargetBackfillSystem`, `MatchHudMinimapMarkerSystem`, `ThreatDetectionWarningSystem`.
- **Acceptance:** `grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "BurstCompile" {} || echo {}'` returns only UI Shell/Ecs managed systems (correctly un-Bursted) + commented exceptions.
- **Priority:** P1 — second-highest ROI after P0-4. Pair with P0-1 completion.

### P1-2 — Cache stable singletons in `OnCreate` — PARTIAL (regressed on GridConfig)

- **Status:** Partial / regressed.
  - Per-frame `GetSingleton<GridConfig>`: **26 sites** (was ~20 at baseline — went UP; likely new usages added without caching).
  - Per-frame `GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>`: **9 files** (unchanged).
- **Delta:** Commit `6270ac229 Reduce repeated grid singleton reads` + `0ea336f8e Cache AI diagnostics singleton queries` exist but the net site count increased — the work was offset by new uncached usages.
- **Why it matters:** Every per-frame `GetSingleton` is a query + chunk scan. `GridConfig` and the ECB singleton are stable for the entire world lifetime — caching once in `OnCreate` is strictly cheaper. 26+9 = 35 per-frame query sites is a measurable main-thread cost.
- **How to finish:**
  1. For `GetSingleton<GridConfig>` sites (in `Systems/EngageTargetSyncSystem.cs:21`, `UnitIdleWanderSystem.cs:26-27`, `InitialUnitsBlockerChurnSystem.cs:34`, `DynamicBlockerInitSystem.cs:17,41,44`, `UnitEngagedMovementSystem.cs:26-27`, `UnitGridMovementSystem.cs:706-707`, `UnitAirMovementSystem.cs:20`, `ThreatDetectionWarningSystem.cs:206`, `UnitAttackSystem.cs:107`, `DynamicOccupancyRebuildSystem.cs:315-316`, `UnitGridSnapSystem.cs:19`, `UnitDeathSystem.cs:309,317`, `UiShellEcsGateway.cs:743`, and ~7 more): cache `GridConfig` in a `private GridConfig _gridConfig;` field assigned in `OnCreate` via `SystemAPI.GetSingleton<GridConfig>()` (or via a cached `EntityQuery`).
  2. For `GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>` in 9 files (`UnitGridMovementSystem.cs:727`, `UnitHealthBarSystem.cs:20`, `UnitAttackStateEnsureSystem.cs:19`, `UnitAttackTraceStateEnsureSystem.cs:18`, `UnitRotationHoldSystem.cs:19`, `UnitEngagementSystem.cs:113`, `UnitDestroyedVisualSystem.cs:24`, `EngageTargetSyncSystem.cs:23`, `MapSurfaceDiagnosticsSystem.cs:59`): cache the `SystemHandle`/singleton ref in `OnCreate`.
  3. Sweep for any newly-added `GetSingleton<...>` in `OnUpdate` and route them through `OnCreate` caching.
- **Acceptance:** `grep -rn "GetSingleton<GridConfig>\|GetSingletonEntity<GridConfig>" Assets/Game/Scripts --include="*.cs" | grep -vi OnCreate` returns 0; `grep -rln "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts --include="*.cs"` returns 0.
- **Priority:** P1 — mechanical, immediate main-thread win.

### P1-3a — `BattleHudRuntimeFeedbackBoundary` moved out of Contracts — DONE

- **Status:** Resolved. Now at `UI/Components/BattleHudRuntimeFeedbackBoundary.cs` (in `Game.UI.Runtime`).
- **No further action.**

### P1-3b — `UiShellRuntimeGateway` static facade moved out of Contracts — DONE

- **Status:** Resolved. Now at `UI/Shell/UiShellRuntimeGateway.cs` (in `Game.UI.Runtime`).
- **No further action.**

### P1-3c — `TrySetLoadingProgress` routed through command queue — DONE

- **Status:** Resolved. `UiShellEcsGateway.cs:103-115` now appends to `DynamicBuffer<UiShellLoadingProgressRequestComponent>` instead of direct `entityManager.SetComponentData`.
- **Verification:** `sed -n '103,115p' UI/Shell/Ecs/UiShellEcsGateway.cs` shows `EnsureLoadingProgressRequestBuffer` + `requests.Add(new UiShellLoadingProgressRequestComponent{…})`.
- **No further action.**

### P1-4a — `MapSurfaceAuthoring` blob dedup — NOT STARTED

- **Status:** Not started. `Authorings/MapSurfaceAuthoring.cs:31` still calls `TryCreateRuntimeBlobAsset` per-authoring with no cache.
- **Why it matters:** Multiple authorings sharing the same `MapSurfaceDataAsset` build duplicate identical blobs — wasted bake-time memory + runtime memory.
- **How to do it:**
  1. Add a static `Dictionary<MapSurfaceDataAsset, BlobAssetReference<MapSurfaceBlob>>` cache keyed by source asset instance.
  2. In `Bake`, check cache first; if present, reuse the `BlobAssetReference` and call `AddBlobAsset(ref existingRef, out _)`.
  3. Clear the cache on domain reload via `[InitializeOnLoad]` or `OnDestroy` of a baker registry.
- **Acceptance:** With N authorings referencing the same asset, only 1 blob is built (verify by logging in `TryCreateRuntimeBlobAsset`).
- **Priority:** P1.

### P1-4b — `UnitGridAuthoring.transform.Find` at runtime — PARTIAL

- **Status:** Partial — down from 3 sites to 2 (l.1126, l.1131), and both are now fallback paths guarded by `authoring.modelRoot != null ? authoring.modelRoot : authoring.transform.Find("Model")`. The primary path uses a baked `modelRoot`/`destroyedRoot` reference; `Find` only fires if the baked ref is null.
- **Delta:** Improvement — the common path no longer allocates. Remaining `Find` calls only fire on misconfigured prefabs (null baked ref).
- **Why it matters (downgraded):** With the null-guard, runtime cost is near-zero on the happy path. The remaining `Find` is a defensive fallback. Could be made a hard error (log + skip) instead, but low priority.
- **How to finish:**
  1. Either delete the `Find` fallback and treat null `modelRoot` as a bake-time validation error (preferred — fail loud), OR
  2. Leave as-is and document `// fallback for legacy prefabs, bakes null ref into component`.
- **Priority:** P2 (downgraded from P1).

### P1-5 — Split `MatchBootstrapSystem` god-object — IMPROVED but still over target

- **Status:** Improved — file is now **1063 lines** (was 1172 last check, was ~770 at baseline).
- **Delta:** -109 lines since last check. Commit `ad61bc09f Extract match bootstrap startup projection`.
- **Why it matters:** Still 1063 lines — over the 600-line target. The god-object still owns boundary init + blob build + spawn orchestration + surface overlay + scene lifecycle. Progress is real but slow.
- **How to finish:**
  1. Continue extracting sub-responsibilities into per-feature bootstrap systems in `Composition/` (in `InitializationSystemGroup`):
     - `MatchMapSurfaceBootstrapSystem` (already partially delegated to `MapSurfaceRuntimeBootstrapSceneSystemHelper.cs`)
     - `MatchInitialSpawnBootstrapSystem`
     - `MatchSurfaceOverlayBootstrapSystem`
  2. Keep `MatchBootstrapSystem` as a thin coordinator or delete it once sub-systems are self-creating.
- **Acceptance:** No single `.cs` file in `Composition/` exceeds ~600 lines.
- **Priority:** P1 — continue the extraction.

### P1-6 — `.Run()` → `ScheduleParallel` — NOT STARTED

- **Status:** Not started. All 3 unchanged:
  - `Systems/AITargetingSystem.cs:124` — `AssignTargetsJob` `.Run(_squadQuery)` (blocker: writes to `NativeList` `_diagnosticEvents`, not `ParallelWriter`)
  - `Rendering/Systems/UnitRenderBudgetSortSystem.cs:16` — `SortDistancesJob` `.Run()`
  - `Rendering/Systems/UnitRenderBudgetBandSystem.cs:70` — `BuildBandPlanJob` `.Run()`
- **Why it matters:** These jobs are Bursted but run single-threaded on the main thread, missing parallelism on potentially large queries. `AITargetingSystem` is the highest cost (nested O(targets) scoring per squad).
- **How to finish:**
  1. `AITargetingSystem`: split `_diagnosticEvents` into a `NativeQueue.ParallelWriter` (or per-chunk `NativeList` then merge), convert to `ScheduleParallel`.
  2. `UnitRenderBudgetSortSystem`: consider a parallel sort or accept given throttling (runs every ~10 frames).
  3. `UnitRenderBudgetBandSystem`: cost is capped by constants — leave as-is or schedule if easy.
- **Acceptance:** `AITargetingSystem.AssignTargetsJob` scheduled via `ScheduleParallel`; no race conditions (run with Jobs parallel-safety check).
- **Priority:** P1 — `AITargetingSystem` first.

### P1-7a — Guard `BuildingResourceHaulerBridgeSystem` unconditional Debug.LogWarning — DONE

- **Status:** Resolved. l.308 now gated by `_invalidCapacityWarningEntities.Add(entity)` (dedup set — logs at most once per entity). l.360 was already gated by `VerboseResourceHaulerLogs`. Commit `c9216796a Throttle resource hauler capacity warnings`.
- **No further action.**

### P1-7c — `UnitAttackTraceSystem.LateUpdate` EntityManager loop — DONE (refactored away)

- **Status:** Resolved. The old `UnitAttackTraceSystem.cs` MonoBehaviour is gone. Replaced by `UnitAttackTracePresentationSystemHelper.cs` (an `IUnitAttackTraceRenderer`, not a per-frame MonoBehaviour EntityManager loop). `Shader.PropertyToID` cached as `static readonly int` (l.19-20).
- **No further action.**

### P1-7d — `BuildingBarrierSystem` managed dict foreach — REGRESSED (+1 site)

- **Status:** Regressed — now **10 `foreach` over `IReadOnlyDictionary<int, RuntimeBuildingEntity>`** (was 9 last check, was 9 at baseline). Not addressed.
- **Delta:** +1 site since last verification.
- **Why it matters:** Each `foreach` over `IReadOnlyDictionary` boxes the enumerator + `KeyValuePair` structs → GC alloc on the AI breach path (called from `AICombatOrderSystem` during base assaults). Scales with assault frequency.
- **How to do it:**
  1. Convert `RuntimeBuildings` from `IReadOnlyDictionary<int, RuntimeBuildingEntity>` to `NativeHashMap<int, RuntimeBuildingEntity>` (or a parallel `NativeList<RuntimeBuildingEntity>` + `NativeHashMap<int,int>` index).
  2. Populate the `NativeHashMap` via a bridge system from the managed collection (or directly from ECS if the source is ECS).
  3. Replace all 10 `foreach` with `for`/index-based iteration or `NativeHashMap` enumerator (no boxing).
- **Acceptance:** `grep -c "foreach.*RuntimeBuildings" Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs` returns 0; `IReadOnlyDictionary` removed from the `BuildingBarrierContext` struct.
- **Priority:** P1 — the regression makes this more urgent.

### P1-8 — `RuntimeBuildingEntityLink` per-building MonoBehaviour Update — NOT STARTED

- **Status:** Not started. `RuntimeBuildingEntityLink.cs:40` still has `private void Update()`.
- **Why it matters:** One MonoBehaviour `Update` per runtime building. N instances × per-frame ECS read → expensive at large building counts.
- **How to do it (pick by building count profile):**
  1. If buildings are rendered by Entities Graphics, remove `RuntimeBuildingEntityLink` entirely.
  2. If managed `GameObject` is required, convert to a single `BuildingTransformSyncSystem` (ISystem, Burst) iterating all buildings in one `IJobParallelForTransform`.
  3. If building counts are small (<100), add a dirty-flag early-out.
- **Acceptance:** `grep -n "void Update" Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs` returns no matches OR system confirmed removed.
- **Priority:** P1.

---

## P2 — Minor

### P2-1 — Split `CombatComponents.cs` — NOT STARTED

- **Status:** Not started. `Components/CombatComponents.cs` still 653 lines / 64 structs.
- **How:** Split into `Combat/LauncherComponents.cs`, `Combat/ProjectileComponents.cs`, `Combat/VfxRequestComponents.cs`, `Combat/ImpactComponents.cs`, `Combat/InterceptionComponents.cs`, `Combat/RespawnComponents.cs`, `Combat/ResourceHaulerComponents.cs`.
- **Priority:** P2.

### P2-4 — Cache `Shader.PropertyToID` as `static readonly int` — PARTIAL (runtime clean, Editor remains)

- **Status:** Partial — runtime call-sites now clean (`BuildingProductionTransportSystem.cs:17-18`, `UnitAttackTracePresentationSystemHelper.cs:19-20` both use `static readonly int`). Remaining sites are **Editor-only**:
  - `Editor/PortraitSpritePrefabRenderer.cs:306-307`
  - `Editor/UnitImpostorAtlasGenerator.cs:261-262`
- **Why it matters (downgraded):** Editor-only call-sites run at edit-time, not in builds — no runtime impact. Low priority.
- **Priority:** P2 (optional — Editor hygiene only).

### P2-6 — `GridAuthoring` static `RegisteredInstances` + bake-time `GetSingletonEntity` — NOT STARTED

- **Status:** Not started.
  - `Authorings/GridAuthoring.cs:10` — `private static readonly List<GridAuthoring> RegisteredInstances = new();` (static mutable, domain-reload-fragile, shared across bakers)
  - `:289,320,343,408` — `query.GetSingletonEntity()` in bake helpers (reaching into the runtime world during bake is fragile)
- **How:**
  1. Replace `RegisteredInstances` with a baker-scoped registry or `BakingSystem`-managed `NativeHashMap`.
  2. Move the `GetSingletonEntity`/`GetComponentData` lookups to be inputs passed into `Bake` via the authoring component, or defer to runtime init.
- **Priority:** P2.

### P2-2 / P2-3 / P2-5 / P2-7 — NOT RE-VERIFIED

Assumed unchanged. Verify on next pass:
- P2-2 (split large `IBufferElementData` elements)
- P2-3 (`UiActionRequestSystem.OnUpdate` direct EntityManager → ECB)
- P2-5 (cache `GetComponentInParent<Canvas>` on UI views)
- P2-7 (USS hygiene for static layout in `UiToolkitShellView`)

---

## Priority-ordered action list (what to do next)

### Sprint 1 (P0 — do immediately)

1. **P0-4 — Finish system ordering (45 unordered files).** Highest correctness risk. The recent `Fix ECS startup ordering cycle` commit proves the team is already fighting ordering bugs — address the root cause. Add `[UpdateInGroup]` + `[UpdateAfter]` for the 45 remaining systems; order the producer↔consumer pairs listed above first.
2. **P0-1 — Finish RequireForUpdate guards (~29 remaining).** Mechanical sweep. Pair with P1-1 (newly-Bursted systems should also be guarded).

### Sprint 2 (P1 — high ROI)

3. **P1-1 — Add `[BurstCompile]` to the 72 un-Bursted ISystem structs.** Combat/AI/movement first. Compounds with P0-1 (guarded + Bursted = skip when empty + fast when running).
4. **P1-2 — Cache `GridConfig` + ECB singletons in `OnCreate`.** 35 per-frame query sites → 35 cached fields. Mechanical, immediate main-thread win. Sweep for newly-added `GetSingleton` in `OnUpdate` too.
5. **P1-7d — Convert `BuildingBarrierSystem` managed dict to `NativeHashMap`.** Regressed (+1 site). 10 `foreach` boxing sites on the AI breach path.
6. **P1-5 — Continue splitting `MatchBootstrapSystem`** (1063 lines → target ≤600). Extract surface-overlay + initial-spawn sub-systems next.
7. **P1-6 — Convert `AITargetingSystem.AssignTargetsJob` `.Run()` → `ScheduleParallel`.** Split diagnostic accumulation to enable parallelism.
8. **P1-8 — Replace `RuntimeBuildingEntityLink` per-building `Update`** with batched `IJobParallelForTransform` or remove if Entities Graphics handles buildings.
9. **P1-4a — Add `MapSurfaceAuthoring` blob dedup cache.**

### Sprint 3 (P2 — hygiene)

10. **P2-1** Split `CombatComponents.cs` (653 lines).
11. **P2-6** Replace `GridAuthoring` static `RegisteredInstances` + bake-time `GetSingletonEntity`.
12. **P2-4** Sweep remaining Editor-only `Shader.PropertyToID` (optional).
13. **P2-2/P2-3/P2-5/P2-7** — re-verify and address.

### Deferred

- **P0-6 Namespaces** — 742/745 global-ns. Schedule a dedicated sprint.

---

## Verification commands (re-run after each sprint)

```bash
# P0-1: unguarded ISystem files (target: 0 or only commented-intentional)
grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "RequireForUpdate\|RequireMatchingQueriesForUpdate" {} || echo {}'

# P0-4: unordered ISystem files (target: 0 or only bootstrap with comment)
grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "UpdateInGroup\|UpdateBefore\|UpdateAfter" {} || echo {}'

# P1-1: ISystem files without BurstCompile (target: 0 or only managed UI Shell/Ecs)
grep -rl "ISystem" Assets/Game/Scripts --include="*.cs" | xargs -I{} sh -c 'grep -q "BurstCompile" {} || echo {}'

# P1-2: per-frame GridConfig singleton reads (target: 0 outside OnCreate)
grep -rn "GetSingleton<GridConfig>\|GetSingletonEntity<GridConfig>" Assets/Game/Scripts --include="*.cs" | grep -vi OnCreate

# P1-2: per-frame EndSimulation ECB singleton (target: 0)
grep -rln "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts --include="*.cs"

# P1-7d: BuildingBarrierSystem managed-dict foreach (target: 0)
grep -c "foreach.*RuntimeBuildings" Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs

# P1-8: RuntimeBuildingEntityLink Update (target: 0)
grep -n "void Update" Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs
```

---

## Appendix — Commit log since baseline (for traceability)

```
4a0478c0c Fix ECS startup ordering cycle          ← P0-4 (partial)
2c9370dc8 Close ECS architecture tracker validation
37c9ee6d4 Record building barrier data structure decision
35c0e1d5f Complete authoring baker hygiene stage  ← P1-4b (partial)
c9216796a Throttle resource hauler capacity warnings  ← P1-7a (done)
61efa2d7b Complete ECS run scheduling review       ← P1-6 (no code change observed)
ab4a46815 Complete ECS singleton cleanup stage     ← P1-2 (partial/regressed)
0ea336f8e Cache AI diagnostics singleton queries   ← P1-2 (partial)
6270ac229 Reduce repeated grid singleton reads     ← P1-2 (partial, offset by new usages)
02c6c9b97 Route UI shell loading progress through ECS requests  ← P1-3c (done)
ad61bc09f Extract match bootstrap startup projection  ← P1-5 (improved, -109 lines)
d99940ef7 Close ECS RequireForUpdate tracker stage  ← P0-1 (partial, +17)
98358fa61 Disable idle ECS startup helpers
9a63fc267 Add command queue RequireForUpdate guards  ← P0-1 (partial)
d18a4b614 Add high-risk ECS system ordering         ← P0-4 (partial, -4)
```