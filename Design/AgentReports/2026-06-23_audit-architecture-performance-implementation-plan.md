# Architecture & Performance Audit — Actionable Implementation Plan

**Project:** WarlineCapture-Clone (Unity 6, Entities 6.4 / DOTS, URP)
**Audit date:** 2026-06-23
**Auditor:** opencode (read-only audit, no files modified)
**Scope:** `Assets/Game/Scripts` (745 .cs files, 14 asmdefs) + `Packages/com.sniveler-code.gpu-animation`
**Stack:** Unity 6, URP 17.4, Entities 6.4, Burst, Collections, UI Toolkit + UGUI hybrid, Input System 1.19, Jenkinsfile CI

> **For implementing agents:** Each task below is self-contained with: Location, Problem, Acceptance Criteria, Effort, Dependencies, and Verification. Pick up tasks in priority order. P0 first, then P1, then P2. Tasks within the same priority are parallelizable across multiple agents. Do NOT skip the Baseline step.

---

## Baseline (do first, before any remediation)

**Task B-0 — Capture profiler baseline**
- **Why:** Every perf claim below needs a before/after measurement. `ProfilerCaptures/` is currently empty.
- **Action:** In Unity Editor, open `Assets/Game/Scenes/Match.unity`, enter Play mode, record a 60s Profiler session with a full match running (units spawned, AI active, combat). Save the `.raw` capture to `ProfilerCaptures/baseline_2026-06-23.raw`. Note the Editor log path for GC allocation callstacks (existing tooling: `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`).
- **Acceptance:** `ProfilerCaptures/baseline_2026-06-23.raw` exists; main-thread ms/frame and GC alloc/frame numbers recorded in `Design/AgentReports/2026-06-23_perf_baseline_numbers.md`.
- **Effort:** S (manual, no code).

---

# Priority 0 — Critical (correctness + largest wasted CPU)

## P0-1 — Add `[RequireMatchingQueriesForUpdate]` / `RequireForUpdate` guards to all unguarded ISystem structs

- **Problem:** Zero uses of `[RequireMatchingQueriesForUpdate]` anywhere. ~90 ISystem structs run `OnUpdate` every frame regardless of matching entities. Combined with most `OnCreate` methods lacking `state.RequireForUpdate(...)`, this is the largest wasted-CPU issue in the project.
- **Already guarded (do NOT touch):** `Systems/EngageTargetSyncSystem.cs:15`, `UnitHealthBarSystem.cs:14`, `UnitGridMovementSystem.cs:681`, `UnitAttackStateEnsureSystem.cs:13`, `UnitAttackTraceStateEnsureSystem.cs:12`, `UnitRotationHoldSystem.cs:13`, `UnitEngagementSystem.cs:61`, `UnitDestroyedVisualSystem.cs:16`, `MapSurfaceDiagnosticsSystem.cs:19`, `UnitAttackVfxSystems.cs:15`.
- **Approach (pick one per system, prefer the attribute for single-query systems, `RequireForUpdate` in `OnCreate` for multi-query):
  1. `[RequireMatchingQueriesForUpdate]` on the struct when the system has exactly one primary query and should run only when it matches.
  2. In `OnCreate`: `state.RequireForUpdate<MyComponent>()` or cache a query ref `state.RequireForUpdate(_myQuery)` for multi-query systems.
  3. For systems that legitimately run every frame (bootstrap, diagnostics with a static gate), add a comment `// intentionally runs every frame` and skip.
- **Files to update (high-impact subset, do these first):**
  - `Systems/UnitPathfindingSystem.cs:7`
  - `Systems/UnitAttackSystem.cs:9`
  - `Systems/UnitDeathSystem.cs:9`
  - `Systems/UnitRespawnSystem.cs:8`
  - `Systems/AITargetingSystem.cs:8`
  - `Systems/AICombatOrderSystem.cs:9`
  - `Systems/AIProductionSystem.cs:8`
  - `Systems/AIBuildPlannerSystem.cs:8`
  - `Systems/AISquadSystem.cs:8`
  - `Systems/AIEconomySystem.cs:7`
  - `Systems/AIFactionControlSystem.cs:7`
  - `Systems/UnitMoveOrderSystem.cs:9`
  - `Systems/UnitMoveOrderRequestSystem.cs:5`
  - `Systems/UnitTargetOrderSystem.cs:7`
  - `Systems/UnitAttackOrderRequestSystem.cs:5`
  - `Systems/UnitTransportBoardingSystem.cs:10` + all 7 `UnitTransport*System.cs`
  - `Systems/UnitSurfaceTrackingSystem.cs:9`
  - `Systems/UnitAnimationIndexSystem.cs:12`
  - `Systems/CitizenMovementCommandSystem.cs:5`
  - `Systems/MatchHudMinimapMarkerSystem.cs:8`
  - `Systems/ThreatDetectionWarningSystem.cs:9`
  - `Rendering/Systems/UnitRenderBudgetSystem.cs:12`
  - `Rendering/Systems/UnitSelectionMarkerSystem.cs:13`
  - `Rendering/Systems/UnitHelicopterBladeSpinSystem.cs:12`
  - `Rendering/Systems/UnitFactionTintTargetBackfillSystem.cs:9`
- **Then sweep the remaining ~60 systems** listed in the audit's §2.4(a).
- **Acceptance criteria:**
  - `rg "RequireMatchingQueriesForUpdate|RequireForUpdate" Assets/Game/Scripts` returns ≥ 90 matches.
  - No ISystem `OnUpdate` runs unconditionally except bootstrap/diagnostic systems marked with a `// intentionally runs every frame` comment.
  - Play-mode smoke test: enter Match scene, verify units spawn, AI fights, match completes — no new exceptions.
  - Profiler: main-thread ms/frame drops on the post-fix capture vs `baseline_2026-06-23.raw`.
- **Effort:** M (mechanical, ~90 files, ~2h).
- **Dependencies:** B-0.
- **Verification:** `rg -c "RequireMatchingQueriesForUpdate|RequireForUpdate" Assets/Game/Scripts | wc -l` ≥ 90; full PlayMode test run.

## P0-2 — Fix `MissileTrailVfxView.Update` managed-dictionary foreach

- **Location:** `Assets/Game/Scripts/Effects/MissileTrailVfxView.cs:66-96` (specifically l.72)
- **Problem:** `Update()` iterates `_active` (`Dictionary<Entity, TrailInstance>`, l.27) with `foreach (KeyValuePair<Entity, TrailInstance> pair in _active)`. Per-frame enumerator/boxing alloc, scales with active missile count.
- **Fix:** Maintain a parallel `List<Entity>` key cache (or `NativeList<Entity>` if the view is burst-friendly) updated on add/remove. Iterate the list by index, look up the dict by key only when releasing. Better: replace the `Dictionary` with a `NativeHashMap<Entity, TrailInstance>` + `NativeList<Entity>` order list if the view can be made Burst-friendly; otherwise use `List<KeyValuePair<Entity,TrailInstance>>` populated from the dict on mutation and iterate by index.
- **Acceptance criteria:**
  - `rg "foreach.*_active" Assets/Game/Scripts/Effects/MissileTrailVfxView.cs` returns no matches.
  - GC alloc/frame from this system = 0 in Profiler (filter by `MissileTrailVfxView.Update`).
  - Trails still render and release correctly during a combat-heavy match.
- **Effort:** S.
- **Dependencies:** B-0.
- **Verification:** Profiler GC alloc on `MissileTrailVfxView.Update` = 0; visual smoke test with 20+ active missiles.

## P0-3 — Fix `TerrainLodHeightSwitch.Update` `Camera.allCameras` array alloc

- **Location:** `Assets/Game/Scripts/Rendering/TerrainLodHeightSwitch.cs:56-59, 91-113` (specifically l.99)
- **Problem:** `Update()` → `ResolveCamera()` calls `Camera[] cameras = Camera.allCameras;` allocating a new managed array every frame on the no-cache path.
- **Fix:** Use `Camera.allCamerasCount` + `Camera.GetCameraAt(i)` in a `for` loop (no array alloc), and cache the resolved camera in `_resolvedCamera` on first success (the cache field already exists — just stop allocating the array to probe). Even simpler: since the project avoids `Camera.main`, use a `[SerializeField] Camera` reference or resolve once on `OnEnable`/first `Update`.
- **Acceptance criteria:**
  - `rg "Camera\.allCameras[^C]" Assets/Game/Scripts` returns no matches (the `Count` form is allowed).
  - Profiler: zero array allocs from `TerrainLodHeightSwitch.Update`.
  - LOD switching still works when the camera moves between height thresholds.
- **Effort:** S.
- **Dependencies:** B-0.
- **Verification:** Profiler GC alloc on `TerrainLodHeightSwitch.Update` = 0; LOD switch test at low/high altitude.

## P0-4 — Add system ordering attributes to the 68 unordered systems

- **Problem:** 68 ISystem structs have no `[UpdateInGroup]`/`[UpdateBefore]`/`[UpdateAfter]` → run at default order. Inter-system producer↔consumer pairs execute in undefined order, causing latent bugs (e.g. movement applied before pathfinding resolves, orders consumed before requests produced).
- **Highest-risk pairs to order first (producer → consumer):**
  1. `Systems/UnitPathfindingSystem.cs:7` → `Systems/UnitGridMovementSystem.cs:662` (pathfinding must run before movement application)
  2. `Systems/UnitMoveOrderRequestSystem.cs:5` → `Systems/UnitMoveOrderSystem.cs:9`
  3. `Systems/UnitAttackOrderRequestSystem.cs:5` → `Systems/AttackOrderCommandSystem.cs:8`
  4. `Systems/UnitTargetOrderSystem.cs:7` → consumer systems
  5. `Systems/UnitTransportBoardingSystem.cs:10` ↔ `Systems/TransportBoardingCommandSystem.cs:8`
  6. `Systems/UnitTransportDeployOrderSystem.cs:9` → `Systems/UnitTransportDeployAttackSystem.cs:5`
  7. `Systems/UnitTransportAirPickupSystem.cs:8` → `Systems/UnitTransportAirdropSystem.cs:9`
  8. `Systems/RuntimeGameplayStateSystem.cs:5` → AI chain head (`AIEconomySystem.cs:7`, `AIFactionControlSystem.cs:7`)
  9. AI internal chain: `AIEconomySystem` → `AIFactionControlSystem` → `AIBuildPlannerSystem` → `AIProductionSystem` → `AISquadSystem` → `AITargetingSystem` → `AICombatOrderSystem` (some `[UpdateAfter]` exists internally — audit and complete the chain)
  10. Building systems: `BuildingStartupConfigProjectionSystem.cs:3` → `BuildingGridCompositionSystem.cs:4` → `BuildingSpawnPrefabSystem.cs:4` → `BuildingTargetMoveOrderSystem.cs:5`
- **Approach:**
  1. Assign each unordered system to a group: most simulation systems → `[UpdateInGroup(typeof(SimulationSystemGroup))]`; presentation/visual → `PresentationSystemGroup` (some already do — check `UiToolkitShellApplySystem.cs:5`); bootstrap/one-shot → `InitializationSystemGroup` or a custom `MatchBootstrapSystemGroup`.
  2. Add `[UpdateBefore(typeof(ConsumerSystem))]` on the producer or `[UpdateAfter(typeof(ProducerSystem))]` on the consumer — pick one direction consistently (prefer `[UpdateAfter]` on consumers to keep producers clean).
  3. For systems with no inter-dependency but which must be in a specific group, only `[UpdateInGroup]` is needed.
- **Full list of 68 unordered systems:** see audit §2.4(b). Sweep them all.
- **Acceptance criteria:**
  - Every ISystem has either `[UpdateInGroup(...)]` or an existing `[UpdateBefore]/[UpdateAfter]` chain that implies a group.
  - `rg -L "UpdateInGroup|UpdateBefore|UpdateAfter" Assets/Game/Scripts --type cs` (files lacking all three) returns only bootstrap systems explicitly marked `// intentionally default-ordered`.
  - Play-mode test: run a full match, verify no order-regression bugs (units follow paths, AI issues orders, transport deploys).
- **Effort:** M-L (68 files, requires understanding each system's role).
- **Dependencies:** P0-1 (so `RequireForUpdate` guards don't interact with ordering surprises).
- **Verification:** `rg -l "ISystem" Assets/Game/Scripts | xargs -I{} rg -L "UpdateInGroup|UpdateBefore|UpdateAfter" {}` returns only allowed bootstrap files; full PlayMode test.

## P0-5 — Fix `UiShellBoundarySystem.OnUpdate` direct EntityManager structural changes

- **Location:** `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs:43` (and l.19 `GetSingletonEntity` without guard)
- **Problem:** `OnUpdate` calls `state.EntityManager.CreateEntity(...)` + ~40 `AddComponentData`/`AddBuffer` calls (l.43-301) when the boundary entity is missing, plus `boundaryQuery.GetSingletonEntity()` (l.19) every frame without `RequireForUpdate` guard (throws if missing).
- **Fix:**
  1. Add `state.RequireForUpdate(boundaryQuery)` in `OnCreate` OR check `boundaryQuery.CalculateEntityCount() == 0` before `GetSingletonEntity`.
  2. Move the boundary-entity creation + component initialization into `OnCreate` (one-shot) guarded by a "already initialized" flag, OR into a separate `InitializationSystemGroup` bootstrap system.
  3. If creation must be deferred (world not ready in `OnCreate`), use an `EntityCommandBuffer` from `BeginSimulationEntityCommandBufferSystem.Singleton` instead of direct `EntityManager` calls.
- **Acceptance criteria:**
  - `rg "EntityManager\.CreateEntity|EntityManager\.AddComponent" Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs` returns no matches inside `OnUpdate`.
  - No `GetSingletonEntity` called without a prior count/`RequireForUpdate` guard.
  - UI shell boots correctly into the match scene (transition complete event fires).
- **Effort:** S-M.
- **Dependencies:** P0-1.
- **Verification:** PlayMode test entering Match scene; no exceptions in log; UI shell state transitions work.

## P0-6 — Introduce namespaces per assembly

- **Problem:** 741 of 745 `.cs` files declare NO namespace (global namespace). Every `asmdef` has `rootNamespace: ""`. Type collisions only prevented by file-unique naming; `internal` is effectively project-global; intellisense/Go-to-symbol noisy; external consumers cannot selectively `using` a feature namespace.
- **Target namespace scheme (match asmdef name):**
  | asmdef | namespace |
  |---|---|
  | Game.Runtime | `Game.Runtime` |
  | Game.Composition | `Game.Composition` |
  | Game.Components | `Game.Components` |
  | Game.Configs | `Game.Configs` |
  | Game.Authoring | `Game.Authoring` |
  | Game.Rendering | `Game.Rendering` |
  | Game.Rendering.Contracts | `Game.Rendering.Contracts` |
  | Game.UI.Runtime | `Game.UI.Runtime` |
  | Game.UI.Contracts | `Game.UI.Contracts` |
  | Game.UI.Toolkit | `Game.UI.Toolkit` |
  | Game.UI.Shell.Ecs | `Game.UI.Shell.Ecs` |
  | Game.UI.Shell.Contracts.Ecs | `Game.UI.Shell.Contracts.Ecs` |
  | Game.Catalog.Contracts | `Game.Catalog.Contracts` |
  | Game.Editor | `Game.Editor` |
- **Approach (mechanical, do per-assembly to keep PRs reviewable):**
  1. For each asmdef, set `rootNamespace` to the target namespace.
  2. Wrap every `.cs` file in that asmdef's folder in `namespace <target> { ... }` (file-scoped namespaces `namespace <target>;` are preferred for C# 10+ — verify the Unity C# version supports them; Unity 6 supports C# 9 by default, so use block-scoped `namespace X { ... }`).
  3. Update any `global::` references if they arise.
  4. Run a compile after each assembly — fix any name collisions that surface (these are the bugs the current global namespace was hiding).
- **Order (do leaves first, apex last):**
  1. `Game.Catalog.Contracts`, `Game.UI.Contracts`, `Game.Rendering.Contracts` (leaves, no internal deps)
  2. `Game.Components`, `Game.Configs`
  3. `Game.Authoring`, `Game.Rendering`
  4. `Game.UI.Runtime`, `Game.UI.Toolkit`, `Game.UI.Shell.Contracts.Ecs`, `Game.UI.Shell.Ecs`
  5. `Game.Runtime`
  6. `Game.Composition`
  7. `Game.Editor`
- **Acceptance criteria:**
  - `rg -L "^namespace " Assets/Game/Scripts --type cs` returns ≤ 4 files (AssemblyInfo.cs etc.).
  - Every `asmdef` has non-empty `rootNamespace` matching its name.
  - Project compiles clean in Unity.
- **Effort:** L (mechanical but wide; ~741 files; do per-assembly PRs).
- **Dependencies:** None (independent). Can run in parallel with P0-1..P0-5 if agents own disjoint assemblies.
- **Verification:** `rg -c "^namespace " Assets/Game/Scripts --type cs` ≈ 741; Unity compile clean; PlayMode smoke test.

---

# Priority 1 — Major (perf + architecture)

## P1-1 — Add `[BurstCompile]` to the ~50 hot-path un-Bursted ISystem structs

- **Problem:** ~112 of 139 ISystem structs have no `[BurstCompile]` on struct or `OnUpdate`. Only 27 carry it on the struct; 28 on `OnUpdate`. Hot-path offenders are the combat, AI, movement, transport, render-budget, and minimap systems.
- **Files (do in this order — combat/AI/movement first):**
  - `Systems/UnitAttackSystem.cs:9`
  - `Systems/UnitDeathSystem.cs:9`
  - `Systems/UnitRespawnSystem.cs:8`
  - `Systems/AITargetingSystem.cs:8`, `AICombatOrderSystem.cs:9`, `AIProductionSystem.cs:8`, `AIBuildPlannerSystem.cs:8`, `AISquadSystem.cs:8`, `AIEconomySystem.cs:7`, `AIFactionControlSystem.cs:7`
  - `Systems/UnitPathfindingSystem.cs:7`
  - `Systems/UnitMoveOrderSystem.cs:9`, `UnitMoveOrderRequestSystem.cs:5`, `UnitTargetOrderSystem.cs:7`, `UnitAttackOrderRequestSystem.cs:5`
  - All 7 `Systems/UnitTransport*System.cs` (`Boarding`, `DeployOrder`, `Airdrop`, `AirPickup`, `Capacity`, `PassengerState`, `DeployAttack`, `RopeDisembark`)
  - `Systems/UnitSurfaceTrackingSystem.cs:9`
  - `Systems/UnitAnimationIndexSystem.cs:12`
  - `Systems/CitizenMovementCommandSystem.cs:5`
  - `Rendering/Systems/UnitRenderBudgetSystem.cs:12`, `UnitFactionTintTargetBackfillSystem.cs:9`, `UnitSelectionMarkerSystem.cs:13`, `UnitHelicopterBladeSpinSystem.cs:12`
  - `Systems/MatchHudMinimapMarkerSystem.cs:8`
  - `Systems/ThreatDetectionWarningSystem.cs:9`
- **Approach per system:**
  1. Add `[BurstCompile]` on the struct AND on `OnUpdate` (both are required — struct-level does NOT auto-apply to methods).
  2. If `OnUpdate` uses managed refs (string, `Debug.Log`, `EntityManager` managed APIs, `ComponentLookup<T>` for managed components), either:
     - Extract the hot loop into a Burst-compiled `IJobEntity`/`IJobChunk` and keep `OnUpdate` as a thin managed orchestrator (no `[BurstCompile]` on `OnUpdate`), OR
     - Remove the managed call (replace `Debug.Log` with `NativeQueue`/`FixedString` diagnostic buffers; replace `EntityManager` with ECB).
  3. For systems that already schedule Burst jobs from `OnUpdate` (e.g. `UnitGridMovementSystem`), adding `[BurstCompile]` on `OnUpdate` lets the scheduler itself run in Burst (cheaper schedule calls).
- **Important note on `UnitGridMovementSystem.cs:662`:** the struct has `[BurstCompile]` but `OnUpdate` does NOT — struct-level Burst does NOT auto-Burst `OnUpdate`. Add `[BurstCompile]` to `OnUpdate` explicitly.
- **Acceptance criteria:**
  - `rg -l "ISystem" Assets/Game/Scripts --type cs | xargs -I{} rg -L "BurstCompile" {}` returns only UI Shell/Ecs systems (managed, correctly un-Bursted) and systems with a comment explaining why.
  - No Burst compile errors in the Unity console (Jobs > Burst > Show Inspector).
  - Profiler: main-thread ms/frame on simulation systems drops vs baseline.
- **Effort:** L (per-system work; some need job extraction; ~50 systems).
- **Dependencies:** P0-1 (RequireForUpdate guards should be in place first so newly-Bursted systems don't run needlessly).
- **Verification:** Burst AOT settings panel shows 0 errors; Profiler compare vs baseline; full match PlayMode test.

## P1-2 — Cache stable singletons in `OnCreate`

- **Problem:** ~15 systems call `SystemAPI.GetSingleton<GridConfig>()` or `SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()` per frame in `OnUpdate`. Both are stable singletons — cache them once in `OnCreate`.
- **Files:**
  - `GetSingleton<GridConfig>` per frame (6 systems):
    - `Systems/DynamicBlockerInitSystem.cs:17,41,44`
    - `Systems/UnitIdleWanderSystem.cs:26,27`
    - `Systems/InitialUnitsBlockerChurnSystem.cs:34`
    - `Systems/EngageTargetSyncSystem.cs:21`
    - `Systems/AITargetingSystem.cs:75,144,151`
    - `Rendering/Systems/UnitRenderBudgetSystem.cs:132`
  - `GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()` per frame (9 systems):
    - `Systems/UnitGridMovementSystem.cs:727`
    - `Systems/UnitHealthBarSystem.cs:20`
    - `Systems/UnitAttackStateEnsureSystem.cs:19`
    - `Systems/UnitAttackTraceStateEnsureSystem.cs:18`
    - `Systems/UnitRotationHoldSystem.cs:19`
    - `Systems/UnitEngagementSystem.cs:113`
    - `Systems/UnitDestroyedVisualSystem.cs:24`
    - `Systems/EngageTargetSyncSystem.cs:23`
    - `Systems/MapSurfaceDiagnosticsSystem.cs:59`
- **Fix pattern:**
  ```csharp
  private GridConfig _gridConfig;
  public void OnCreate(ref SystemState state) {
      _gridConfig = SystemAPI.GetSingleton<GridConfig>(); // cache once
      // OR for ECB: var ecbSys = state.WorldUnmanaged.GetExistingSystemState<EndSimulationEntityCommandBufferSystem>();
      //             _ecbSingleton = ecbSys.CreateCommandBuffer();
  }
  // In OnUpdate: use _gridConfig instead of SystemAPI.GetSingleton<GridConfig>()
  ```
  For ECB singletons, cache the `SystemHandle` or use `SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged)` — but the lookup itself is the cost; cache the singleton reference. Note: in Entities 1.0+/6.x, `SystemAPI.GetSingleton<T>()` on a singleton component is cheap-ish but still a query; caching is strictly better.
- **Acceptance criteria:**
  - `rg "GetSingleton<GridConfig>" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering` returns matches only in `OnCreate` methods (or removed entirely).
  - `rg "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts` matches only in `OnCreate`.
  - No behavior change; full match completes.
- **Effort:** S-M (mechanical, ~15 files).
- **Dependencies:** None (independent of P0/P1-1).
- **Verification:** Profiler shows fewer `GetSingleton` calls; PlayMode smoke test.

## P1-3 — Move runtime logic out of `Game.UI.Contracts`

- **Problem:** The pure Contracts assembly contains runtime logic.
- **Task P1-3a (Critical) — `BattleHudRuntimeFeedbackBoundary`:**
  - **Location:** `Assets/Game/Scripts/UI/Contracts/BattleHudRuntimeFeedbackBoundary.cs:3` (entire file, ~220 lines)
  - **Problem:** Sealed class with runtime feedback control logic (mutates views, reads `Time.unscaledTime` l.110/120, branches on result strings l.204-211) in the pure Contracts assembly.
  - **Fix:** Move the file to `Assets/Game/Scripts/UI/` (root of `Game.UI.Runtime` assembly) or a new `UI/Feedback/` subfolder. Update its namespace once P0-6 lands. Update any `using` (none needed while global namespace). Verify no Contracts-assembly code references it ( Contracts shouldn't reference Runtime — if it does, that's a separate cycle bug to fix by inverting the dependency via an interface in Contracts).
- **Task P1-3b (Major) — `UiShellRuntimeGateway` static facade:**
  - **Location:** `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs:50` (static class with `Register`/dispatch + `NullUiShellRuntimeGateway` impl l.169-299)
  - **Problem:** Static mutable service-locator with hidden dispatch in Contracts. Keep only `IUiShellRuntimeGateway` (l.3) in Contracts; move the static `UiShellRuntimeGateway` facade + `NullUiShellRuntimeGateway` to `Game.UI.Runtime`.
- **Task P1-3c (Major) — `TrySetLoadingProgress` direct EntityManager write:**
  - **Location:** `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs:74` → impl `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs:103-115`
  - **Problem:** `TrySetLoadingProgress` performs a direct `EntityManager.SetComponentData` from the UI-facing gateway, bypassing the `TryEnqueue*` `DynamicBuffer` command queue used by all other UI→ECS writes. Inconsistent; risk of per-frame UI-driven mutation.
  - **Fix:** Introduce a `UiLoadingProgressCommandElement` `IBufferElementData` on the boundary entity; have `TrySetLoadingProgress` append to that buffer (mirroring `TryEnqueueUiAction`); add a small ISystem in `Game.UI.Shell.Ecs` that consumes the buffer and writes `UiShellLoadingProgressComponent`. Remove the direct `EntityManager.SetComponentData` from `UiShellEcsGateway`.
- **Minor (optional) — relocate borderline types:**
  - `Contracts/TacticalCommandContracts.cs:178` `TacticalCommandFeedbackText` (static mapping class) → `Game.UI.Runtime`
  - `Contracts/IMatchIntroStateQuery.cs:8` `NullMatchIntroStateQuery` (impl) → `Game.UI.Runtime`
- **Acceptance criteria:**
  - `rg "class BattleHudRuntimeFeedbackBoundary|class UiShellRuntimeGateway|class TacticalCommandFeedbackText|class NullUiShellRuntimeGateway|class NullMatchIntroStateQuery" Assets/Game/Scripts/UI/Contracts` returns no matches.
  - `Game.UI.Contracts.asmdef` still has zero asmdef references (pure leaf).
  - UI shell transitions + loading progress bar still work end-to-end.
- **Effort:** M (P1-3a/b mechanical; P1-3c needs a new buffer + consumer system).
- **Dependencies:** P0-6 (namespaces) for clean relocation, but can be done before namespaces by just moving files between folders + asmdefs.
- **Verification:** `rg -l "class " Assets/Game/Scripts/UI/Contracts --type cs` returns only interfaces/structs/enums; PlayMode test of menu→match transition + loading progress.

## P1-4 — Bake-path: dedup `MapSurfaceAuthoring` blobs + cache child refs in `UnitGridAuthoring`

- **Task P1-4a (Major) — Blob dedup:**
  - **Location:** `Assets/Game/Scripts/Authorings/MapSurfaceAuthoring.cs:31`
  - **Problem:** Each `MapSurfaceAuthoring` builds a fresh blob via `TryCreateRuntimeBlobAsset`. Multiple authorings sharing the same `MapSurfaceDataAsset` produce duplicate identical blobs.
  - **Fix:** Introduce a static `Dictionary<MapSurfaceDataAsset, BlobAssetReference<MapSurfaceBlob>>` cache keyed by the source asset instance (or by a content hash if assets may be duplicated instances). In `Bake`, check the cache first; if present, reuse the `BlobAssetReference` and call `AddBlobAsset(ref existingRef, out _)`. Clear the cache on domain reload (`[InitializeOnLoad]`/`OnDestroy` of a baker registry, or use Unity's `Object`-keyed `BlobAssetRegistry` if available in Entities 6.4).
  - **Acceptance:** With N authorings referencing the same asset, only 1 blob is built (verify by logging in `TryCreateRuntimeBlobAsset`).
- **Task P1-4b (Major) — Cache child refs at bake time:**
  - **Location:** `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs:471,472,1114` (runtime `transform.Find("Model"/"Destroyed")`)
  - **Problem:** String-based `Transform.Find` at runtime (spawn/setup) — allocates and searches hierarchy.
  - **Fix:** During `Bake`, resolve `transform.Find("Model")`/`transform.Find("Destroyed")` once and store the resulting child indices or prefab references in a baked component (e.g. `UnitModelChildReference` holding the child `Entity` or index). At runtime, read the baked reference instead of calling `Transform.Find`.
  - **Acceptance:** `rg "transform\.Find" Assets/Game/Scripts/Authorings` returns no matches in runtime code paths; unit spawn still wires models correctly.
- **Effort:** M (P1-4a needs a cache + domain-reload handling; P1-4b needs baked component plumbing).
- **Dependencies:** None.
- **Verification:** Bake the MatchSubScene; verify blob count = unique asset count; spawn units and confirm models/destructed states resolve.

## P1-5 — Split `MatchBootstrapSystem` god-object

- **Location:** `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs` (51 KB, ~770+ lines)
- **Problem:** Owns boundary init, blob build, spawn orchestration, surface overlay bootstrap, and scene lifecycle in one class. Hard to maintain; any change risks regressions across all features.
- **Fix:** Split into per-feature bootstrap systems in the `Composition` folder, each in `InitializationSystemGroup` (or a custom `MatchBootstrapSystemGroup`):
  1. `MatchBoundaryBootstrapSystem` — boundary entity + initial components
  2. `MatchMapSurfaceBootstrapSystem` — blob build/runtime bootstrap (already partially delegated to `MapSurfaceRuntimeBootstrapSceneSystemHelper.cs`)
  3. `MatchInitialSpawnBootstrapSystem` — initial faction/unit/building spawn orchestration
  4. `MatchSceneLifecycleBootstrapSystem` — scene load/unload wiring (already partially in `MatchSceneReferenceSceneSystemHelper.cs` + `MatchStartSceneSystemHelper.cs`)
  5. `MatchSurfaceOverlayBootstrapSystem` — runtime surface overlay buffer build
- Keep `MatchBootstrapSystem` as a thin coordinator that creates/orders the sub-systems (or delete it entirely if the sub-systems are self-creating).
- **Acceptance criteria:**
  - No single `.cs` file in `Composition/` exceeds ~600 lines.
  - Match scene boots identically (smoke test: enter match, spawn, play, exit).
- **Effort:** M-L (refactor; requires understanding the 770 lines).
- **Dependencies:** P0-4 (ordering) so the split systems run in the right sequence.
- **Verification:** File-size check; full match lifecycle PlayMode test.

## P1-6 — Convert `.Run()` jobs to `ScheduleParallel` where possible

- **Problem:** Several Burst jobs run single-threaded via `.Run()` on potentially large queries, missing parallelism.
- **Files:**
  - `Systems/AITargetingSystem.cs:122` — `AssignTargetsJob` `.Run(_squadQuery)`. Blocker: writes to `_diagnosticEvents` (`NativeList`, not `ParallelWriter`). Fix: split diagnostic accumulation into a `NativeQueue.ParallelWriter` or a per-chunk `NativeList` then merge; convert to `ScheduleParallel`.
  - `Rendering/Systems/UnitRenderBudgetSortSystem.cs:16` — `SortDistancesJob` `.Run()` every budget frame. Consider a parallel sort or accept given throttling (every ~10 frames). Lower priority.
  - `Rendering/Systems/UnitRenderBudgetBandSystem.cs:70` — `BuildBandPlanJob` `.Run()`. Cost is capped by constants — acceptable; leave as-is or schedule if easy.
  - `Systems/AIBuildPlannerSystem.cs`, `AIEconomySystem.cs:209`, `AIProductionSystem.cs:317` — decision jobs `.Run()`. Low frequency per faction; acceptable unless faction count grows. Lower priority.
- **Acceptance criteria (for `AITargetingSystem` at least):**
  - `AssignTargetsJob` scheduled via `ScheduleParallel`; diagnostic events accumulated in parallel-safe structure.
  - No race conditions (run with Jobs > Debugger > Enable Parallel `NativeQueue` race detection if available).
  - Profiler: targeting pass main-thread time drops.
- **Effort:** M (AITargetingSystem refactor; others optional).
- **Dependencies:** P1-1 (Burst on the system).
- **Verification:** Profiler compare; PlayMode test with multiple AI factions.

## P1-7 — Fix remaining managed-loop GC sources

- **Task P1-7a (Major) — `BuildingResourceHaulerBridgeSystem.cs:308`:** unconditional interpolated-string `Debug.LogWarning` on hot path. Guard behind `VerboseResourceHaulerLogs` like the surrounding lines (l.278-367 are already gated).
- **Task P1-7b (Major) — `UnitMassRenderSettingsSystem.cs:114-131`:** managed `GetSharedComponentManaged`/`SetSharedComponentManaged` per entity in `OnUpdate` (capped 12000/frame, spike-prone on large spawns). Investigate batching or moving to a shared-component-filter approach; if managed access is unavoidable, spread across frames more aggressively during large spawns.
- **Task P1-7c (Major) — `UnitAttackTraceSystem.cs:87-143`:** `LateUpdate()` per-entity `EntityManager.GetComponentData`/`HasComponent` loop. Move to an `IJobChunk`/`IJobEntity` writing into pre-allocated `Matrix4x4[]`/`Vector4[]` arrays (the arrays already exist l.43-45); keep only the `Graphics.DrawMeshInstanced` call on main thread.
- **Task P1-7d (Major) — `BuildingBarrierSystem.cs:133+`:** `foreach` over managed `IReadOnlyDictionary` on AI breach path (called from `AICombatOrderSystem.cs:534`). Convert `RuntimeBuildings` to a `NativeHashMap<int, RuntimeBuildingEntity>` (or a parallel `NativeList` + `NativeHashMap` index) populated by a bridge system; iterate with `for`/`NativeHashMap` enumerator (no boxing).
- **Acceptance criteria:**
  - `rg "Debug\.LogWarning.*\$" Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs` returns only gated matches.
  - Profiler: no GC alloc spikes during large unit spawns or combat.
- **Effort:** M-L (P1-7c/d are real refactors).
- **Dependencies:** B-0.
- **Verification:** Profiler GC alloc during combat + large spawn = 0 from these systems.

## P1-8 — `RuntimeBuildingEntityLink` per-building MonoBehaviour Update

- **Location:** `Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs:40`
- **Problem:** One MonoBehaviour per runtime building, each `Update` reads `LocalTransform` from ECS and sets `transform` position/rotation. N instances × per-frame ECS read — expensive at large building counts.
- **Fix options (pick by building count profile):**
  1. If buildings are rendered by Entities Graphics already (check `Game.Rendering`), remove `RuntimeBuildingEntityLink` entirely and let the hybrid renderer drive transforms.
  2. If a managed `GameObject` is required per building (for colliders/physics/UI anchor), convert to a single `BuildingTransformSyncSystem` (ISystem, Burst) that iterates all buildings in one job and writes to a `TransformAccessArray` (Unity's `IJobParallelForTransform`).
  3. If building counts are small (<100), leave as-is but add an early-out when the entity transform hasn't changed (dirty flag).
- **Acceptance:** `rg "void Update" Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs` returns no matches OR the system is confirmed removed/replaced.
- **Effort:** M (option 2) / S (option 1 or 3).
- **Dependencies:** None.
- **Verification:** Profiler `Update()` cost from `RuntimeBuildingEntityLink` = 0 or negligible; buildings still position correctly.

---

# Priority 2 — Minor / hygiene

## P2-1 — Split `Components/CombatComponents.cs` by domain

- **Location:** `Assets/Game/Scripts/Components/CombatComponents.cs` (64 structs / 390 fields / 653 lines)
- **Fix:** Split into `Combat/LauncherComponents.cs`, `Combat/ProjectileComponents.cs`, `Combat/VfxRequestComponents.cs`, `Combat/ImpactComponents.cs`, `Combat/InterceptionComponents.cs`, `Combat/RespawnComponents.cs`, `Combat/ResourceHaulerComponents.cs`. Keep a `Combat/CombatComponents.cs` for shared types.
- **Acceptance:** No single `*Components.cs` file exceeds ~300 lines.
- **Effort:** S-M (move structs, update no references since global namespace — but verify after P0-6 lands).
- **Dependencies:** P0-6 (namespaces) preferred but not required.

## P2-2 — Split large `IBufferElementData` elements

- **Problem:** Several buffer elements are 108–140 bytes; `FixedString64/128Bytes` fields dominate.
- **Files:**
  - `Components/SelectionInputRequestComponents.cs:147` `RtsSelectionCommandResultElement` ≈140 B (FixedString64 + 2 Entity + many fields)
  - `Components/SelectionInputRequestComponents.cs:123` `RtsSelectionCommandIntentRequestElement` ≈120 B
  - `Components/UnitVisualComponents.cs:233` `UnitAttachedLightSetupElement` ≈108 B — FixedString64 dominates; drop after bake/spawn or split name into a side buffer indexed by `int`.
  - `Components/BuildingRuntimeEcsBoundaryComponents.cs` (12 buffers, 40–80 B each, several with `FixedString128Bytes` ×2 = 256 B strings)
- **Fix:** For each large element, audit whether the `FixedString` field is needed at runtime or only at bake/spawn. If bake-only, move it to a separate rarely-accessed buffer (or a blob) keyed by index. If runtime-needed, consider `FixedString32Bytes` if names fit.
- **Acceptance:** No `IBufferElementData` exceeds ~64 bytes (or has a documented reason).
- **Effort:** M (per-buffer analysis).
- **Dependencies:** None.

## P2-3 — `UnitActionRequestSystem.OnUpdate` direct EntityManager → ECB

- **Location:** `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs:119`
- **Problem:** `EntityManager.CreateEntity(typeof(...))` and `AddBuffer` calls (l.133-163) inside `OnUpdate`. Should be ECB-backed.
- **Fix:** Replace with `EntityCommandBuffer` from `BeginSimulationEntityCommandBufferSystem.Singleton` (or `EndSimulation...`). Create entity + add buffer via ECB, Playback at end of frame.
- **Acceptance:** `rg "EntityManager\.CreateEntity|EntityManager\.AddBuffer" Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs` returns no matches in `OnUpdate`.
- **Effort:** S.
- **Dependencies:** P0-5 (same area).
- **Verification:** PlayMode UI action request test.

## P2-4 — Cache `Shader.PropertyToID` as `static readonly int`

- **Location:** `Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs:605-606` (and sweep for other `Shader.PropertyToID` in runtime paths)
- **Fix:** Replace inline `Shader.PropertyToID("...")` calls with `static readonly int s_IdProp = Shader.PropertyToID("...");` fields.
- **Acceptance:** `rg "Shader\.PropertyToID" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering Assets/Game/Scripts/UI` returns only field initializers (no calls inside methods).
- **Effort:** S.
- **Dependencies:** None.

## P2-5 — Cache `GetComponentInParent<Canvas>` on UI views

- **Location:** `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs:191` (`ResolveEventCamera`, called from `ContainsScreenPoint` — per pointer event, no cache)
- **Fix:** Cache the `Canvas`/`Camera` reference in `Awake`/`OnEnable`; refresh only on `OnRectTransformDimensionsChange` if needed. Same pattern for `MatchHudMinimapView.cs:179`, `MatchHudRightQuickRailView.cs:105`, `MatchOverlayCommandControlsView.cs:73`, `BuildPlacementConfirmationBarView.cs:335`, `UIPlaceholderModalButtonView.cs:28`.
- **Acceptance:** No `GetComponentInParent<Canvas>` in `Update`/input handler paths.
- **Effort:** S.
- **Dependencies:** None.

## P2-6 — `GridAuthoring` static `RegisteredInstances` + runtime-world access during bake

- **Location:** `Assets/Game/Scripts/Authorings/GridAuthoring.cs:56` (static mutable `RegisteredInstances` list), `:289-294,320-324,343-344,408-409` (`GetSingletonEntity`/`GetComponentData` in bake helpers reaching into the runtime world)
- **Problem:** Static mutable list shared across bakers (order-dependent, domain-reload-fragile). Bake helpers reaching into the runtime world via `EntityManager` during bake is fragile (bake world ≠ runtime world assumptions).
- **Fix:** Replace `RegisteredInstances` with a baker-scoped registry (per-bake-session, cleared on bake complete) or a `BakingSystem`-managed `NativeHashMap`. Move the `GetSingletonEntity`/`GetComponentData` lookups to be inputs passed into `Bake` via the authoring component, or defer to runtime initialization.
- **Acceptance:** No `static` mutable collections in `GridAuthoring`; no `EntityManager.GetSingletonEntity` in bake helpers.
- **Effort:** M.
- **Dependencies:** None.

## P2-7 — USS hygiene for static layout in `UiToolkitShellView`

- **Location:** `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs:1393-1398` (container absolute-fill inline `style.*`), and the dynamic-fill inline styles at l.1515, 2107, 2257, 2291, 2372-2375, 2537-2538, 2562 (these are diff-gated, perf-OK — leave).
- **Fix:** Move the static absolute-fill container layout (l.1393-1398) into a `.uss` file (create `Assets/Game/UI/Shell/ShellLayout.uss` or similar). Keep inline `style.*` only for truly dynamic values (health/progress fills, minimap markers — already diff-gated).
- **Acceptance:** A `.uss` file exists under `Assets/Game/` for shell layout; `UiToolkitShellView.cs:1393-1398` uses class names instead of inline `style.position/left/right/top/bottom`.
- **Effort:** S.
- **Dependencies:** None.

## P2-8 — `BuildingBarrierSystem` managed dictionary → NativeHashMap (cross-ref P1-7d)

- If P1-7d is done, this is complete. Tracked separately here because the fix touches both perf and the AI breach-path correctness boundary.
- **Effort:** covered by P1-7d.

---

# Dependency graph between tasks

```
B-0 ──┬─> P0-1 ──┬─> P0-4
      │          └─> P0-5
      │          └─> P1-1
      ├─> P0-2
      ├─> P0-3
      └─> P1-7

P0-6 (independent, parallel with everything)

P1-2 (independent)
P1-3 (independent; benefits from P0-6)
P1-4 (independent)
P1-5 ── depends on P0-4 (ordering) for the split systems
P1-6 ── depends on P1-1 (Burst)
P1-8 (independent)

P2-* (all independent; P2-1 benefits from P0-6)
```

---

# Parallelization plan (for multi-agent execution)

**Wave 1 (P0, parallel):**
- Agent A: B-0 (profiler baseline) — blocks perf-verification of others but not code work
- Agent B: P0-1 (RequireForUpdate guards) — ~90 files
- Agent C: P0-2 + P0-3 (two small GC fixes)
- Agent D: P0-5 (UiShellBoundarySystem)
- Agent E: P0-6 namespaces — start with leaf assemblies (Catalog.Contracts, UI.Contracts, Rendering.Contracts, Components, Configs)

**Wave 2 (P0 cont. + P1, parallel):**
- Agent B: P0-4 (system ordering) — after P0-1
- Agent E: P0-6 namespaces — continue (Authoring, Rendering, UI.*, Runtime, Composition, Editor)
- Agent F: P1-2 (singleton caching)
- Agent G: P1-3 (UI Contracts cleanup)
- Agent H: P1-4 (bake dedup + child refs)

**Wave 3 (P1 cont., parallel):**
- Agent B: P1-1 (Burst on ~50 systems) — after P0-1
- Agent F: P1-7 (managed-loop GC) + P2-4/P2-5
- Agent G: P1-5 (split MatchBootstrapSystem) — after P0-4
- Agent H: P1-6 (Run→ScheduleParallel) — after P1-1
- Agent I: P1-8 (RuntimeBuildingEntityLink)

**Wave 4 (P2 hygiene, parallel):**
- P2-1, P2-2, P2-3, P2-6, P2-7 — distribute among free agents

---

# Verification checklist (run before merging any task)

1. **Compile:** Unity console shows 0 errors, 0 new warnings (Burst panel: 0 errors).
2. **Static checks:**
   - `rg -c "RequireMatchingQueriesForUpdate|RequireForUpdate" Assets/Game/Scripts` ≥ 90 (after P0-1)
   - `rg -L "UpdateInGroup|UpdateBefore|UpdateAfter" <unordered-system-files>` empty (after P0-4)
   - `rg -L "^namespace " Assets/Game/Scripts --type cs` ≤ 4 (after P0-6)
   - `rg "Camera\.allCameras[^C]" Assets/Game/Scripts` empty (after P0-3)
   - `rg "foreach.*_active" Assets/Game/Scripts/Effects/MissileTrailVfxView.cs` empty (after P0-2)
3. **PlayMode test:** Enter Menu → start Match → full match (spawn, AI combat, transport, building, win/lose) → exit. No exceptions in log. Compare against baseline profiler capture.
4. **Profiler compare:** Load `ProfilerCaptures/baseline_2026-06-23.raw` and the new capture; confirm main-thread ms/frame and GC alloc/frame did not increase (should decrease for perf tasks).
5. **Test suite:** Run `Game.Tests.Editor` + `Game.Tests.PlayMode` (if present); 0 new failures.

---

# Appendix — Strengths (no action needed; preserve these)

- Assembly DAG is clean; contracts-leaves enforced by asmdef refs.
- All ~40 Burst jobs correctly annotated + `[ReadOnly]` + dependency-chained.
- 0 NativeContainer leaks — exemplary `IsCreated`-guarded disposal.
- 0 per-frame `MaterialPropertyBlock` allocations — all cached with `??=`.
- 0 `Camera.main` / `FindObjectsOfType` / `InvokeRepeating` in runtime.
- GPU instancing (`RenderMeshInstanced`/`DrawMeshInstanced`) used for impostors + tracers.
- Sniveler gpu-animation tint driven from a Burst `ScheduleParallel` job — idiomatic.
- Managed SystemBase classes correctly retained only for managed-object work; ISystem↔SystemBase bridge is one-directional, no conflicting writers.
- UI→ECS queue discipline consistent (one exception flagged in P1-3c).
- All diagnostic `Debug.Log` calls are flag/throttle/freeze-gated.

---

# Appendix — Full audit findings reference

The complete read-only audit findings (with every file:line reference and severity) are in the audit conversation transcript. Key sections:
- §1 Architecture & Layering — asmdef DAG, namespace failure (741/745 global-ns), SubScene usage.
- §2 DOTS/ECS Correctness — ISystem vs SystemBase counts, Burst gaps, baking, ordering, ECB, singletons, archetypes.
- §3 Performance — GC alloc (2 Critical), Burst `.Run()` vs `ScheduleParallel`, NativeContainer leaks (0), rendering/instancing, runtime `Find`/`GetComponentIn*`, MonoBehaviour Update costs.
- §4 UI Layer — Contracts purity violations, hybrid boundary discipline, USS hygiene, asset references.

For any task above, refer back to the corresponding audit section for the full file:line list.