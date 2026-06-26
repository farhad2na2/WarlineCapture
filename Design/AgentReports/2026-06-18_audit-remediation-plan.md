# Unity ECS Audit — Remediation Plan

**Date:** 2026-06-18
**Source audit:** `2026-06-18_audit_unity-ecs-architecture-performance-quality.md`
**Project:** `/Users/farhad/Projects/WarlineCapture-Clone`
**Unity:** 6000.4.0f1 · URP 17.4.0 · Entities 6.4.0

---

## How to Use This Document

Each work item is a self-contained task an agent can pick up. Items are grouped by phase and ordered by priority inside each phase. Every item includes:

- **Finding ID** — maps back to the audit finding (P1, P2, …, A1, …, Q1, …)
- **Severity** — Critical / Major / Minor
- **Effort** — Low (<1h) / Medium (1-4h) / High (>4h)
- **Current state** — verified metric as of 2026-06-18 (after the first fix pass)
- **Goal** — target metric/state
- **Files** — exact paths and line numbers to touch
- **Step-by-step** — numbered instructions an agent can follow verbatim
- **Verification** — how to confirm the fix worked (grep counts + Unity batch mode)
- **Safety notes** — gotchas discovered during the first fix pass that the original audit got wrong

**Before starting any item:** read the corresponding finding in the source audit file for full context.

**After finishing any item:** run the verification block at the bottom of this document before moving on.

---

## Already Fixed (2026-06-18)

### First pass
| Finding | What was done |
|---|---|
| P9 | Shadow cascades 4→2 (Mobile), distance 240→150 (Mobile) / 180 (PC) in `ProjectSettings/QualitySettings.asset` |
| Q5 | Deleted 6 `.DS_Store` files; added `Assets/**/.DS_Store` to `.gitignore` |
| A4 | Removed empty folders `Bootstrap/`, `Rewards/`, `Profile/` + their `.meta` |
| A5 | Set `scriptingBackend: iPhone: 1` and `Standalone: 1` (Android was already 1) |
| A7 | Set `runInBackground: 1` |
| P1 (partial) | Converted 2 race-free `.Run()` → `.ScheduleParallel()`: `UnitHealthBarSystem:26`, `UnitAnimationIndexSystem:42` |
| P7 (partial) | Added `[WithChangeFilter(typeof(UnitHealth))]` to `UnitDeathSystem.CollectDeathBeginCandidatesJob` |

### Phase 1 complete (verified with Unity batch mode exit 0)
| Finding | What was done |
|---|---|
| P7 (complete) | Added `[WithChangeFilter]` to 4 more systems: `UnitHealthBarSystem.UpdateJob` (UnitHealth), `UnitRuntimeHealthBarSystem.CollectHealthBarChangesJob` (UnitHealth), `UnitAnimationIndexSystem.ResolveAnimationIndexJob` (UnitAttackAnimationComponent), `MatchHudMinimapMarkerSystem.CollectMarkersJob` (LocalTransform). Total now 6. |
| Q4 | Replaced deprecated `Graphics.DrawMeshInstanced` (camera-param overload) in `UnitImpostorRenderSystem.cs:428` with `Graphics.RenderMeshInstanced` + `RenderParams`. Removed `#pragma warning disable CS0618`. |
| A6 | Added Low and Ultra quality tiers to `ProjectSettings/QualitySettings.asset`. Now 4 tiers (Low=0, Mobile=1, PC=2, Ultra=3). Updated `m_PerPlatformDefaultQuality` indices. Note: Low tier reuses Mobile URP asset GUID — a dedicated Low URP asset should be authored in Unity Editor later. |
| Q2 | Routed all `EditorApplication.Exit(N)` calls in test files through the existing `Assets/Tests/Editor/ValidationExit.cs` helper (46 files converted). Helper now guards on `Application.isBatchMode \|\| -runTests`. Fixed 5 files that had fully-qualified `UnityEditor.EditorApplication.Exit` form. |

### Phase 2 partial (verified with Unity batch mode exit 0)
| Finding | What was done | Status |
|---|---|---|
| P1 (complete) | Parallelized 9 more `.Run()` jobs via `NativeList<T>.AsParallelWriter()` + `ScheduleParallel()`: `UnitRuntimeHealthBarSystem`, `UnitDeathSystem` (×2), `MatchHudMinimapMarkerSystem` (×3 merged), `UnitRenderBudgetDistanceSystem` (IJob→IJobFor), `VehicleWreckCleanupSystem`, `UnitSelectionMarkerSystem`. `.Run()` count: 13 → 4. Remaining 4 are genuinely single-threaded (sort, sequential bucketing, ComponentLookup writes, IJob scan). | ✅ Done |
| P3 | Discovered all 10 managed `class IComponentData` were already converted to `struct` in a prior session (audit was stale). Current count: 0. | ✅ Already fixed |
| P2 | 21 `Object.Instantiate` calls in systems — deferred (multi-day refactor: bake to entity prefabs + ECB/pooling) | ⏳ Deferred |
| P5 | `TransportBoardingCommandSystem.cs` now 4,013 lines (grew since audit) — deferred (1-week split into 5 systems) | ⏳ Deferred |
| P8 | 77 `foreach` over `Dictionary<int, RuntimeBuildingEntity>` in 15 systems — deferred (requires `RuntimeBuildingEntity` managed→struct conversion first, multi-week) | ⏳ Deferred |

---

## PHASE 1 — Quick Wins (Low effort, do first)

These can each be completed in under a day with no architectural risk.

---

### Task 1.1 — Add `WithChangeFilter` to 4 more polling systems

**Finding:** P7
**Severity:** Minor
**Effort:** Low (4h total)
**Current state:** 2 `WithChangeFilter` usages across the codebase
**Goal:** 6 usages (skip the work that would race — see Safety notes)

**Files and changes:**

| File | Job/Query | Component to filter on | Why |
|---|---|---|---|
| `Assets/Game/Scripts/Systems/UnitHealthBarSystem.cs` | `UpdateJob` (IJobEntity, ~line 61) | `UnitHealth` | Health bar fill only changes when health changes |
| `Assets/Game/Scripts/Systems/UnitRuntimeHealthBarSystem.cs` | `CollectHealthBarChangesJob` (IJobEntity, ~line 52) | `UnitHealth` | Health bar create/destroy only triggers on health change |
| `Assets/Game/Scripts/Systems/UnitAnimationIndexSystem.cs` | `ResolveAnimationIndexJob` (IJobEntity, ~line 98) | `UnitAttackAnimationComponent` | Animation index only re-resolves when attack state changes |
| `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs` | `CollectMarkersJob` (IJobEntity, ~line 67) | `LocalTransform` | Minimap markers only move when position changes |

**Step-by-step (per file):**

1. Open the file at the line listed above
2. Find the `IJobEntity` struct (for jobs) or the `SystemAPI.Query` chain (for main-thread foreach)
3. **For IJobEntity:** add `[WithChangeFilter(typeof(YourComponent))]` attribute on the struct, directly above `[BurstCompile]` (or beside the existing `[WithNone(...)]` / `[WithAll(...)]` attributes)
4. **For SystemAPI.Query foreach:** add `.WithChangeFilter<YourComponent>()` to the chain, e.g.:
   ```csharp
   foreach (var (_, _, entity) in SystemAPI
        .Query<RefRO<UnitGrid>, RefRO<UnitFootprint>>()
        .WithNone<StaticGridBlocker, RuntimeBuildingCombatTag>()
        .WithChangeFilter<UnitGrid>()   // <-- add this
        .WithEntityAccess())
   ```
5. Reference example: `Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs:357` (`.WithChangeFilter<UnitGrid>()`)
6. Reference example: `Assets/Game/Scripts/Systems/UnitDeathSystem.cs` `CollectDeathBeginCandidatesJob` (already has `[WithChangeFilter(typeof(UnitHealth))]`)

**Safety notes:**
- Do NOT add `WithChangeFilter` to jobs that also read components they don't filter on as their primary trigger. The filter must match the component whose change is the actual trigger for the work.
- For `MatchHudMinimapMarkerSystem`, filtering on `LocalTransform` will skip chunks where no unit moved. This is correct for minimap updates. Verify in Play mode that markers still update when units spawn/despawn (structural changes always run; change filter only skips per-chunk component reads).

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -rh "WithChangeFilter" --include="*.cs" Assets/Game/Scripts/ | wc -l
# Expect: 6
```

---

### Task 1.2 — Replace deprecated API in `UnitImpostorRenderSystem.cs`

**Finding:** Q4
**Severity:** Minor
**Effort:** Low (30 min)
**Current state:** 1 `#pragma warning disable CS0618` at `Assets/Game/Scripts/Rendering/UnitImpostorRenderSystem.cs:428`
**Goal:** 0 deprecated-API suppressions

**Step-by-step:**

1. Open `Assets/Game/Scripts/Rendering/UnitImpostorRenderSystem.cs` at line 428
2. Find the `#pragma warning disable CS0618` block
3. Identify the deprecated call inside the block (read ~20 lines around it)
4. Look up the Unity 6 (6000.4) replacement in the Unity docs:
   - Open https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ (search the method name)
   - The deprecation message in the console/editor will name the recommended replacement
5. Replace the deprecated call with the current API
6. Remove the `#pragma warning disable CS0618` and the matching `#pragma warning restore CS0618` lines
7. If you cannot determine the replacement, document the specific deprecated call in the verification block below and leave the file unchanged — do NOT guess

**Safety notes:**
- Common Unity 6 deprecations: `Graphics.DrawMeshInstanced` → `Graphics.RenderMeshInstanced`, `MaterialPropertyBlock` setters with `float[]` → overloaded `float` versions, old `Camera` projection APIs → `Camera.projectionMatrix` helpers.
- Verify the replacement compiles before moving on.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -rln "CS0618" --include="*.cs" Assets/Game/Scripts/
# Expect: no output (0 files)
```

---

### Task 1.3 — Add Low and Ultra quality tiers

**Finding:** A6
**Severity:** Minor
**Effort:** Low (1h)
**Current state:** 2 quality tiers (Mobile tier 0, PC tier 1) in `ProjectSettings/QualitySettings.asset`
**Goal:** 4 quality tiers (Low, Mobile, PC, Ultra) for device scaling

**Step-by-step (direct file edit — do NOT use Unity Editor for reproducibility):**

1. Open `ProjectSettings/QualitySettings.asset` in a text editor
2. Find the existing `qualitySettings:` block (it contains two `  - name:` entries)
3. Add a new tier BEFORE the existing Mobile tier:
   ```yaml
     - name: Low
       pixelLightCount: 0
       shadows: 0
       shadowResolution: 0
       shadowProjection: 1
       shadowCascades: 1
       shadowDistance: 80
       shadowNearPlaneOffset: 3
       shadowCascade2Split: 0.33333334
       shadowCascade4Split: {x: 0.067, y: 0.2, z: 0.467}
       shadowBias: 1
       shadowNormalBias: 1
       shadowSoftness: 0
       shadowSoftnessFade: 1
       shadowCascadeBlend: 0.6
       skinWeights: 1
       globalTextureMipmapLimit: 1
       textureMipmapLimitSettings: []
       anisotropicTextures: 0
       antiAliasing: 0
       softParticles: 0
       softVegetation: 0
       realtimeReflectionProbes: 0
       billboardsFaceCameraPosition: 0
       useHDR: 0
       detailObjDensity: 0.5
       density: 0.5
       lodBias: 0.7
       maximumLODLevel: 0
       enableLODCrossFade: 1
       particleRaycastBudget: 16
       asyncUploadTimeSlice: 2
       asyncUploadBufferSize: 4
       asyncUploadPersistentBuffer: 1
       resolutionScalingFixedDPIFactor: 1
       customRenderPipeline: {fileID: 0}
       excludedTargetPlatforms: []
       default: 0
   ```
4. Add a new tier AFTER the existing PC tier:
   ```yaml
     - name: Ultra
       pixelLightCount: 8
       shadows: 2
       shadowResolution: 3
       shadowProjection: 1
       shadowCascades: 4
       shadowDistance: 300
       shadowNearPlaneOffset: 3
       shadowCascade2Split: 0.5
       shadowCascade4Split: {x: 0.067, y: 0.2, z: 0.467}
       shadowBias: 1
       shadowNormalBias: 1
       shadowSoftness: 1
       shadowSoftnessFade: 1
       shadowCascadeBlend: 0.6
       skinWeights: 4
       globalTextureMipmapLimit: 0
       textureMipmapLimitSettings: []
       anisotropicTextures: 2
       antiAliasing: 8
       softParticles: 1
       softVegetation: 1
       realtimeReflectionProbes: 1
       billboardsFaceCameraPosition: 1
       useHDR: 1
       detailObjDensity: 1
       density: 1
       lodBias: 3
       maximumLODLevel: 0
       enableLODCrossFade: 1
       particleRaycastBudget: 4096
       asyncUploadTimeSlice: 2
       asyncUploadBufferSize: 16
       asyncUploadPersistentBuffer: 1
       resolutionScalingFixedDPIFactor: 1
       customRenderPipeline: {fileID: 0}
       excludedTargetPlatforms: []
       default: 0
   ```
5. Update the `m_PerPlatformDefaultQuality:` block (if present) to map platforms to tiers. Use the GUIDs from the new tier blocks once Unity regenerates them — or leave defaults and configure per-platform in Unity Editor afterward.
6. Save the file
7. Open Unity to verify the tiers appear in Project Settings → Quality

**Safety notes:**
- Editing `QualitySettings.asset` directly is supported but Unity must reimport the file. The 4-tier structure must match Unity's expected schema exactly or the file will be rejected on next editor open.
- If unsure about exact field values, use Unity Editor (Edit → Project Settings → Quality → +) to create the tiers, then commit the resulting `QualitySettings.asset` — this guarantees schema correctness.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -c "  - name:" ProjectSettings/QualitySettings.asset
# Expect: 4
```

---

### Task 1.4 — Guard `EditorApplication.Exit` in test files

**Finding:** Q2
**Severity:** Major
**Effort:** Medium (1-2 days, mechanical)
**Current state:** 56 test files contain `EditorApplication.Exit(0)` or `EditorApplication.Exit(1)`
**Goal:** 0 unguarded `EditorApplication.Exit` calls in `[Test]` classes

**Step-by-step (per file):**

1. List affected files:
   ```bash
   cd /Users/farhad/Projects/WarlineCapture-Clone
   grep -rl 'EditorApplication\.Exit' Assets/Tests/ --include="*.cs"
   ```
2. **A shared helper already exists** at `Assets/Tests/Editor/ValidationExit.cs` with `Exit(int)`, `Passed()`, `Failed()` methods that guard on `Application.isBatchMode` + `-runTests`. Use it — do NOT create a new helper.
3. For each file, replace `EditorApplication.Exit(N)` with `ValidationExit.Exit(N)`. Also handle the fully-qualified form `UnityEditor.EditorApplication.Exit(N)` → `ValidationExit.Exit(N)` (NOT `UnityEditor.ValidationExit.Exit(N)` — `ValidationExit` is in the global namespace).
4. Bulk replacement command (handles both forms):
   ```bash
   find Assets/Tests -name "*.cs" -print0 | xargs -0 grep -l 'EditorApplication\.Exit' | \
     while IFS= read -r f; do
       perl -pi -e 's/UnityEditor\.EditorApplication\.Exit\(/ValidationExit.Exit(/g; s/EditorApplication\.Exit\(/ValidationExit.Exit(/g' "$f"
     done
   ```
5. Do NOT touch `ValidationExit.cs` itself (the helper keeps `EditorApplication.Exit` inside its guarded body).
6. Do NOT touch the `[Test]` methods themselves — only the `MenuItem`/`RunFocusedValidation` methods.
7. Compile-check after the bulk replacement.

**Safety notes:**
- The codebase has TWO existing patterns for the validation runner: older files use `ValidationExit.Passed()` / `ValidationExit.Failed()`, newer files use `EditorApplication.Exit(0/1)`. Both go through the same `ValidationExit` helper after this task. Do not convert `.Passed()`/`.Failed()` calls — they already route through the guard.
- Some files use the fully-qualified `UnityEditor.EditorApplication.Exit(...)` form — make sure the replacement strips the `UnityEditor.` prefix (see step 3).
- If a file's `EditorApplication.Exit` call is inside a `try/finally`, keep the guard inside the `finally` block.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
# Count files where EditorApplication.Exit is NOT inside a -runTests/-batchmode guard
grep -rl 'EditorApplication\.Exit' Assets/Tests/ --include="*.cs" | \
  while read f; do
    if ! grep -q '\-runTests\|\-batchmode' "$f"; then
      echo "$f"
    fi
  done | wc -l
# Expect: 0
```

---

## PHASE 2 — Performance Refactors (Medium effort, high impact)

These require careful refactoring and Burst-safety analysis. Do them after Phase 1.

---

### Task 2.1 — Parallelize remaining `.Run()` jobs (refactor required)

**Finding:** P1
**Severity:** Major
**Effort:** High (1 day per site, 11 sites remaining)
**Current state:** 13 `.Run()` calls in `Assets/Game/Scripts/` (11 of which write shared `NativeList`/`NativeHashSet` from `IJob`/`IJobEntity`)
**Goal:** `.Run()` only on jobs that genuinely require main-thread execution (UI/camera/input/managed access)

**⚠️ Critical safety notes (the original audit got this wrong):**

The original audit recommended blindly replacing `.Run()` with `.ScheduleParallel()`. This is **unsafe** for most of the remaining sites because they write to a single shared `NativeList`/`NativeHashSet` from multiple parallel threads. `NativeList<T>` and `NativeHashSet<T>` are NOT thread-safe for parallel writes — you must use the parallel-writer variants or restructure the job.

**Do NOT do a one-line `.Run()` → `.ScheduleParallel()` swap on any of these files. Each one needs a structural refactor.**

**Per-file plan:**

| File | Job | Why it's `.Run()` now | Required refactor |
|---|---|---|---|
| `Rendering/Systems/UnitRenderBudgetDistanceSystem.cs:52` | `CollectDistanceJob : IJob` | Writes one `NativeList<UnitDistance>` | Convert to `IJobEntity` writing per-entity output into a `NativeQueue<UnitDistance>` parallel writer, then drain the queue into a `NativeList` on main thread after `Complete()` |
| `Rendering/Systems/UnitRenderBudgetSortSystem.cs:16` | `SortDistancesJob : IJob` | In-place sort | **Keep `.Run()`** — sorting is inherently single-threaded. This is correct as-is. |
| `Rendering/Systems/UnitRenderBudgetBandSystem.cs:70` | `BuildBandPlanJob : IJob` | Writes shared `NativeHashSet`s with sequential dependency | **Keep `.Run()`** — the algorithm has data-dependent early-exit loops that can't be parallelized without rewriting the bucketing logic. |
| `Systems/UnitRuntimeHealthBarSystem.cs:35` | `CollectHealthBarChangesJob : IJobEntity` | Writes 3 shared `NativeList<Entity>` (Create/Remove/Destroy) | Use `NativeList<T>.AsParallelWriter()` and pass the parallel writers into the job, then `.ScheduleParallel()`. After `Complete()`, the lists are safe to read on main thread. |
| `Systems/UnitDeathSystem.cs:56` | `CollectDeathBeginCandidatesJob : IJobEntity` | Writes shared `NativeList<DeathBeginCandidate>` | Same pattern: `NativeList<DeathBeginCandidate>.AsParallelWriter()` + `.ScheduleParallel()`. Note: this job already has `[WithChangeFilter(typeof(UnitHealth))]` from the first pass. |
| `Systems/UnitDeathSystem.cs:87` | `CollectDeathAnimationFinalizeJob : IJobEntity` | Writes shared `NativeList<Entity>` | Same parallel-writer pattern. |
| `Systems/MatchHudMinimapMarkerSystem.cs:31,37,42` | 3× `CollectMarkersJob`/`CollectScanIntelMarkersJob : IJobEntity` | All write the same shared `NativeList<MatchHudMinimapMarkerElement>` | Refactor to a single `IJobEntity` with faction filter inside `Execute`, using `NativeList<MatchHudMinimapMarkerElement>.AsParallelWriter()`. Merge the 3 sequential `.Run()` calls into 1 `.ScheduleParallel()`. |

**Step-by-step (template for the parallel-writer refactor):**

1. Open the target file
2. Find the job struct and its `Execute` method
3. Change the shared `NativeList<T>` field to a `NativeList<T>.ParallelWriter` field:
   ```csharp
   // BEFORE:
   public NativeList<Entity> Create;
   // AFTER:
   public NativeList<Entity>.ParallelWriter Create;
   ```
4. In the scheduling site, change:
   ```csharp
   // BEFORE:
   Create = create,
   // AFTER:
   Create = create.AsParallelWriter(),
   ```
5. Inside `Execute`, replace `.Add(...)` calls — they work the same on `ParallelWriter`
6. Replace `.Run()` with `.ScheduleParallel(state.Dependency)` and assign `state.Dependency = handle;`
7. After the job, call `state.Dependency.Complete();` before reading the `NativeList` on the main thread
8. Compile-check
9. Run the matching test file (if any) to verify behavior

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -rh "\.Run()" --include="*.cs" Assets/Game/Scripts/ | wc -l
# Goal: <= 3 (only the genuinely-single-threaded jobs: UnitRenderBudgetSortSystem, UnitRenderBudgetBandSystem, and any future UI/camera/input systems)
```

---

### Task 2.2 — Convert `Object.Instantiate` to ECB / object pooling

**Finding:** P2
**Severity:** Major
**Effort:** High (1-2 weeks total, ~1h per call site)
**Current state:** 21 `Object.Instantiate` calls in systems (was 15 — got worse)
**Goal:** 0 `Object.Instantiate` calls inside `OnUpdate` methods (one-time setup calls are acceptable)

**Per-pattern plan (from the audit):**

**Pattern A — Pure visual prefabs (buildings, roads, decorations):**
- `BuildingDefinitionSystem.cs:679,680`
- `BuildingDestroyedVisualSystem.cs:50`
- `RoadBuildDefinitionProjectionSystem.cs:38`
- `RoadSpecialVisualSystem.cs:187,269,533,742`
- `BuildingPlacementVisualSystem.cs:33`
- `MapBuildingPlacementSpawnSystem.cs:175`
- `RuntimeCityVisualSystem.cs:82,84`
- `RuntimeDecorationSpawnerSystem.cs:150`
- `RuntimeGridBlockerSystem.cs:354`

Steps per file:
1. Move the prefab reference into a baker that registers it as an entity prefab (`Baker.GetEntity()` with `Prefab` tag)
2. In the system, use `EntityCommandBuffer.Instantiate(entityPrefab)` instead of `Object.Instantiate`
3. Attach mesh/material via `MaterialMeshInfo` component in the same ECB
4. Remove the `GameObject` field from the authoring component
5. If the visual must remain a GameObject (e.g. for skinned meshes), use Pattern B instead

**Pattern B — Markers and selection visuals (need object pooling):**
- `SelectionOrderMarkerSystem.cs:685,708,729,907`
- `BuildingSelectionMarkerSystem.cs:189`

Steps per file:
1. In `OnCreate`, pre-instantiate a pool of N marker GameObjects (N = expected max active markers, e.g. 64)
2. Store pooled objects in a `NativeHashMap<Entity, GameObject>` (active) and a `NativeQueue<GameObject>` (free)
3. In `OnUpdate`, deactivate pooled objects that no longer have a matching entity; activate pooled objects for new entities
4. If the pool is exhausted, grow it by a fixed batch size (e.g. +16) — do NOT instantiate per-frame
5. Reference: `UnitSelectionMarkerSystem` already implements a partial pool pattern

**Pattern C — One-time setup (acceptable, leave as-is):**
- `DayNightSystem.cs:213` (skybox material)
- `UIScreenRouteFlowUiSystemHelper.cs:110` (UI screen prefab)

**Safety notes:**
- Before converting any Pattern A site, verify the prefab has a baker. If it's authoring-only (no subscene bake), you'll need to create the baker first.
- Pool sizes must be sized for the worst case or grow dynamically — a fixed pool that's too small will silently drop markers.
- Track the new pool allocations in `OnCreate`, not `OnUpdate`, to avoid per-frame GC.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
# Count Object.Instantiate inside OnUpdate methods (rough heuristic)
grep -rn "Object\.Instantiate" --include="*.cs" Assets/Game/Scripts/Systems/ Assets/Game/Scripts/Environment/ Assets/Game/Scripts/Rendering/Systems/ | wc -l
# Goal: <= 2 (only the Pattern C one-time setup sites)
```

---

### Task 2.3 — Replace 10 managed `class IComponentData` with unmanaged alternatives

**Finding:** P3
**Severity:** Major
**Effort:** Medium (2-4 days)
**Current state:** 10 managed `class IComponentData` across 6 files
**Goal:** 0 managed `class IComponentData` (all unmanaged structs)

**Files and components:**

| File | Line | Component | Holds | Replacement |
|---|---|---|---|---|
| `Composition/MatchSceneReferenceComponent.cs` | 3 | `MatchSceneReferenceComponent` | Scene MonoBehaviour refs | Move to a single managed system field, access via `SystemAPI.GetSingleton` — NOT an ECS component |
| `Components/UnitVisualComponents.cs` | 232 | `UnitAttachedLightSet` | Light prefab refs | `int` indices into a static `LightRegistry` |
| `Components/UnitVisualComponents.cs` | 252 | `UnitAttachedLightRuntime` | Runtime light instances | `int` indices into a static `LightInstanceRegistry` (managed by a single SystemBase) |
| `Components/RuntimeCameraReferenceComponent.cs` | 4 | `RuntimeCameraReferenceComponent` | Camera ref | `Entity` ref to a camera entity, resolved via `EntityManager.GetComponentData<CameraData>` |
| `Components/UnitPoseMeshesSetup.cs` | 5 | `UnitPoseMeshesSetup` | Mesh data | `BlobAssetReference<UnitPoseMeshesBlob>` |
| `Components/CombatComponents.cs` | 173 | `UnitAttackImpactVfxReference` | VFX prefab refs | `int` indices into `VfxRegistry` |
| `Components/CombatComponents.cs` | 178 | `UnitMuzzleFlashVfxReference` | VFX prefab refs | `int` indices into `VfxRegistry` |
| `Components/CombatComponents.cs` | 289 | `GroundMissileLauncherVfxReferenceComponent` | VFX prefab refs | `int` indices into `VfxRegistry` |
| `Components/CombatComponents.cs` | 429 | `AirMissileLauncherVfxReferenceComponent` | VFX prefab refs | `int` indices into `VfxRegistry` |
| `RuntimeState/PerformanceDiagnosticsReferenceComponent.cs` | 3 | `PerformanceDiagnosticsReferenceComponent` | Diagnostics object | Static managed field on a SystemBase, NOT an ECS component |

**Step-by-step (template for the VFX registry pattern — apply to all VFX/light components):**

1. Create `Assets/Game/Scripts/Components/VfxRegistry.cs`:
   ```csharp
   public static class VfxRegistry {
       private static readonly Dictionary<int, GameObject> _prefabs = new();
       public static void Register(int id, GameObject prefab) => _prefabs[id] = prefab;
       public static GameObject Get(int id) => _prefabs.TryGetValue(id, out var p) ? p : null;
       public static void Clear() => _prefabs.Clear();
   }
   ```
2. Replace the managed component with an unmanaged struct:
   ```csharp
   public struct UnitMuzzleFlashVfxRef : IComponentData {
       public int PrefabId;
   }
   ```
3. In the baker, register the prefab and store the ID:
   ```csharp
   int id = prefab.GetInstanceID();
   VfxRegistry.Register(id, prefab);
   baker.AddComponent(new UnitMuzzleFlashVfxRef { PrefabId = id });
   ```
4. In systems that instantiate the VFX, use `[BurstDiscard]` for the managed lookup:
   ```csharp
   [BurstDiscard]
   private static void SpawnVfx(int prefabId, float3 position) {
       var prefab = VfxRegistry.Get(prefabId);
       if (prefab != null) Object.Instantiate(prefab, position, quaternion.identity);
   }
   ```
   (Note: this still uses `Object.Instantiate` — pair it with Task 2.2's pooling for the VFX specifically.)
5. Remove the old `class IComponentData` definition
6. Find all references to the old component (grep) and update them
7. Compile-check

**For `MatchSceneReferenceComponent` and `PerformanceDiagnosticsReferenceComponent`:**
- These should NOT be ECS components at all. Move the managed references to fields on the system that owns them. Access via `SystemAPI.GetSingleton<MySingletonTag>()` if cross-system access is needed, but the actual managed object lives on a single SystemBase field.

**Safety notes:**
- Static registries persist across domain reloads in the editor — call `VfxRegistry.Clear()` in `[InitializeOnLoad]` or in a `[BurstDiscard]` `OnCreate` to avoid stale IDs after recompile.
- Blob assets (`BlobAssetReference<T>`) must be built in bakers, not in systems. They're immutable after build.
- Camera reference: if multiple systems need the camera, prefer a singleton entity with a `CameraReference` unmanaged component holding an `Entity` to the camera entity. The camera entity itself can have a managed `CameraAuthoring` component on a SystemBase that owns the actual `Camera` reference.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -rln "class\s\+\w\+\s*:\s*IComponentData" --include="*.cs" Assets/Game/Scripts/
# Goal: no output (0 files)
```

---

### Task 2.4 — Split `TransportBoardingCommandSystem.cs` (3,875-line god system)

**Finding:** P5
**Severity:** Major
**Effort:** High (1 week)
**Current state:** `Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs` is 3,875 lines, 89 methods
**Goal:** 5 focused systems, each <800 lines, with magic numbers extracted to constants

**Target split:**

| New file | Responsibility | Methods to move |
|---|---|---|
| `TransportBoardingRequestSystem.cs` | Boarding request validation + goal assignment | `TryResolveSelectedBoardTransport`, `TryIssueBoardNearestSoldierOrders`, `TryFindTransportBoardingGoal` |
| `TransportPlaneRampSystem.cs` | Plane ramp approach logic | `TryFindPlaneRampApproachCell`, ramp distance calculations |
| `TransportAirdropSystem.cs` | Airdrop logic | All methods using `DropIntervalSeconds` |
| `TransportCapacityCheckSystem.cs` | Capacity validation | Methods checking passenger counts |
| `TransportDisembarkSystem.cs` | Disembark/landing logic | All `Disembark*` methods |

**Magic numbers to extract (from the audit):**

| Line | Value | Constant name |
|---|---|---|
| 2287 | `* 100` | `CellDistancePenaltyMultiplier = 100` |
| 2547 | `1000` | `DirectionPenaltyWorst = 1000` |
| 2940 | `0.8f` | `DropIntervalSecondsHeavy = 0.8f` |
| 3323 | `0.65f` | `DropIntervalSecondsLight = 0.65f` |

**Step-by-step:**

1. Read the entire `TransportBoardingCommandSystem.cs` to map method→system ownership
2. Create the 5 new files above in `Assets/Game/Scripts/Systems/`
3. Add a `public static class TransportConstants` (or a `TransportConfig` ScriptableObject) at the top of `TransportBoardingRequestSystem.cs` with the 4 constants
4. Move methods one system at a time, updating their access modifiers:
   - Methods called only within one new system → `private`
   - Methods called across systems → `internal static` on the owning system, called as `TransportPlaneRampSystem.TryFindPlaneRampApproachCell(...)`
5. Update `MatchBootstrapSystem` to create each new system if it currently creates only the original
6. Preserve `[UpdateBefore(...)]` / `[UpdateAfter(...)]` attributes so the execution order matches the original
7. After each system is extracted, compile-check and run `Assets/Tests/Editor/` transport tests (10 systems, 83% coverage per audit)
8. Delete the original `TransportBoardingCommandSystem.cs` only after all 5 new systems compile and tests pass

**Safety notes:**
- The original system shares private state (fields, caches) across methods. When splitting, either:
  - Pass the shared state as method parameters, OR
  - Move the shared state to a singleton entity component that all 5 systems read/write via `SystemAPI`
- Do NOT split in one commit. Do one system at a time with a compile-check after each, so regressions are isolated.
- The transport tests are the safety net — if they pass after each split, the refactor is behavior-preserving.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
wc -l Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs 2>/dev/null
# Goal: file does not exist (deleted after split)
wc -l Assets/Game/Scripts/Systems/TransportBoardingRequestSystem.cs \
     Assets/Game/Scripts/Systems/TransportPlaneRampSystem.cs \
     Assets/Game/Scripts/Systems/TransportAirdropSystem.cs \
     Assets/Game/Scripts/Systems/TransportCapacityCheckSystem.cs \
     Assets/Game/Scripts/Systems/TransportDisembarkSystem.cs
# Goal: 5 files, each < 800 lines
```

---

### Task 2.5 — Reduce `foreach` over managed dictionaries

**Finding:** P8
**Severity:** Minor
**Effort:** Medium (ongoing)
**Current state:** 292 `foreach` loops (was 199 — got worse)
**Goal:** No `foreach` over `Dictionary<K,V>.Keys`/`.Values`/`KeyValuePair` in hot-path `OnUpdate` methods

**Step-by-step:**

1. Find hot-path offenders first:
   ```bash
   cd /Users/farhad/Projects/WarlineCapture-Clone
   # foreach inside OnUpdate methods (rough heuristic — manually verify each hit)
   grep -rn "foreach" --include="*.cs" Assets/Game/Scripts/Systems/ | grep -iE "Dictionary|KeyValuePair"
   ```
2. Known offender: `Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs:36,59` — `foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingDictionary)`
3. For each offender, choose one of:
   - **Convert to `NativeHashMap<K,V>`** if the dictionary holds ECS data — eliminates managed enumerator GC and enables Burst
   - **Cache keys, iterate by index** if the dictionary must stay managed:
     ```csharp
     // BEFORE:
     foreach (var pair in runtimeBuildingDictionary) { ... }
     // AFTER:
     var keys = runtimeBuildingDictionary.Keys; // cached field, not per-frame
     for (int i = 0; i < keys.Count; i++) {
         var key = keys.ElementAt(i); // or cache keys as List<K> field
         var value = runtimeBuildingDictionary[key];
         ...
     }
     ```
   - **Use `for` over a cached `List<K>`** if you can maintain a parallel key list

**Safety notes:**
- `foreach` over `List<T>` and arrays does NOT allocate in modern .NET — only `Dictionary<K,V>.Enumerator` allocates. Focus on dictionary foreach only.
- `NativeHashMap<K,V>` iteration order is NOT guaranteed — if the original code relied on insertion order, you must sort the results after iteration.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -rn "foreach" --include="*.cs" Assets/Game/Scripts/ | grep -ciE "Dictionary|KeyValuePair"
# Goal: 0 in Systems/ directory (managed dictionaries moved out of hot paths)
```

---

## PHASE 3 — Architectural Changes (High effort, long-term)

These are multi-week efforts. Plan them as dedicated milestones, not background tasks.

---

### Task 3.1 — Migrate 50 hot-path `SystemBase` → `ISystem`

**Finding:** A1, P6
**Severity:** Major
**Effort:** High (2-4 weeks for top 50)
**Current state:** 248 `SystemBase` (managed) vs 112 `ISystem` (struct); 50/112 `ISystem` have `[BurstCompile]`
**Goal:** 80%+ `ISystem` coverage on per-frame systems; 80%+ `[BurstCompile]` on `ISystem`

**Step-by-step (per system):**

1. Find migration candidates (systems that run every frame and don't touch managed types):
   ```bash
   cd /Users/farhad/Projects/WarlineCapture-Clone
   for f in $(grep -rl ': SystemBase' Assets/Game/Scripts/Systems/ --include="*.cs"); do
     if ! grep -q 'Object\.Instantiate\|GetComponent<\|class.*IComponentData' "$f"; then
       echo "$f"
     fi
   done
   ```
2. For each candidate, change the declaration:
   ```csharp
   // FROM:
   public partial class MySystem : SystemBase {
       protected override void OnUpdate() { ... }
   }
   // TO:
   [BurstCompile]
   public partial struct MySystem : ISystem {
       [BurstCompile]
       public void OnUpdate(ref SystemState state) { ... }
   }
   ```
3. Replace `EntityManager` → `state.EntityManager` / `SystemAPI` (in `ISystem`, `EntityManager` is accessed via `state.EntityManager`)
4. Replace `EntityQuery` creation → `state.GetEntityQuery`
5. Replace `GetComponent<>()` → `SystemAPI.GetComponent<>()`
6. Remove `public` fields — `ISystem` is a struct; use fields with care (they're value-type, copied per access)
7. Add `[BurstCompile]` to `OnCreate` and `OnUpdate`
8. If the system uses managed types, either:
   - Split into a Burst-compiled `ISystem` for data + a thin `SystemBase` for managed access, OR
   - Remove the managed dependency (see Task 2.3)
9. Update `MatchBootstrapSystem` if it creates the system via `GetOrCreateSystemManaged<T>()` → change to `state.World.GetOrCreateSystem<T>()`
10. Compile-check + run matching tests

**Priority order:** Start with systems in `Systems/` that run every frame (no `Enabled = false` in `OnCreate`) and don't touch managed components.

**Safety notes:**
- `ISystem` structs are stored by value in the world — large fields cause copy overhead. Keep `ISystem` fields small (entity queries, lookups, small caches).
- `EntityCommandBuffer` in `ISystem` must be unmanaged (`EntityCommandBuffer`, not `ManagedCommandBuffer`).
- Some `SystemBase` systems use `Entities.ForEach` — this is NOT available in `ISystem`. Convert to `IJobEntity` first, then migrate to `ISystem`.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
echo "SystemBase:" && grep -rl ': SystemBase' --include="*.cs" Assets/Game/Scripts/ | wc -l
echo "ISystem:" && grep -rl ': ISystem' --include="*.cs" Assets/Game/Scripts/ | wc -l
echo "ISystem w/ Burst:" && for f in $(grep -rl ': ISystem' --include="*.cs" Assets/Game/Scripts/); do grep -q BurstCompile "$f" && echo "$f"; done | wc -l
# Goals (after 50 migrations): SystemBase <= 198, ISystem >= 162, ISystem w/ Burst >= 100
```

---

### Task 3.2 — Add test coverage for untested subsystems

**Finding:** Q1
**Severity:** Major
**Effort:** High (2-4 weeks)
**Current state:** 19% system coverage (82/435 tested)
**Goal:** 80%+ coverage on critical subsystems

**Priority test files to create (in order):**

| New test file | Target subsystem | Current coverage | Systems to test |
|---|---|---|---|
| `Assets/Tests/Editor/RuntimeCityGenerationTests.cs` | City Generation | 0% (42 systems) | Building counts, grid layout validity, spawn positions within bounds |
| `Assets/Tests/Editor/UnitCombatTests.cs` | Unit Combat | 0% (8 systems) | Attack resolution, death state transitions, damage application |
| `Assets/Tests/Editor/BuildingPlacementTests.cs` | Building Placement | ~3% (30+ systems) | Validation, grid snapping, wall run logic, session lifecycle |
| `Assets/Tests/Editor/RoadBuildTests.cs` | Road Build | ~4% (27 systems) | Road path planning, visual variant selection, intersection handling |
| `Assets/Tests/Editor/BuildingRuntimeTests.cs` | Building Runtime | ~5% (20+ systems) | Production queues, upgrade state, destruction state |
| `Assets/Tests/Editor/UnitRenderBudgetTests.cs` | Unit Render Budget | ~4% (25+ systems) | LOD band assignment, distance sorting, culling |
| `Assets/Tests/Editor/CitizenPopulationTests.cs` | Citizen Population | 13% (15 systems) | Spawn/despawn, path assignment, state transitions |
| `Assets/Tests/Editor/MatchLifecycleTests.cs` | Match Lifecycle | 0% (5 systems) | Match start, victory conditions, defeat conditions |

**Test template (EditMode):**
```csharp
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

[Test]
public class UnitCombatTests_AttackReducesTargetHealth_WhenDamageApplied
{
    [Test]
    public void AttackReducesTargetHealth_WhenDamageApplied()
    {
        var world = new World("Test");
        var system = world.GetOrCreateSystem<UnitAttackSystem>();
        var em = world.EntityManager;

        var target = em.CreateEntity(typeof(UnitHealth));
        em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });

        // Apply damage via the system's public API or by setting components
        // and running the system update
        system.Update();

        var health = em.GetComponentData<UnitHealth>(target);
        Assert.IsTrue(health.Current < 100, "Health should decrease after attack");
        world.Dispose();
    }
}
```

**Safety notes:**
- Use `world.Dispose()` in a `[TearDown]` or at the end of each test to avoid world leaks.
- Some systems require singleton entities (e.g. `RespawnQueueComponent`) — create them in the test setup.
- For systems that read `SystemAPI.Time.DeltaTime`, the test world's `Time` may be zero — set it explicitly if needed via `world.SetTime(new TimeData(0.016f, 1.0))`.

**Verification:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
# Run all tests via Unity CLI
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/test-results.xml -quit
# Parse /tmp/test-results.xml for pass/fail counts
```

---

### Task 3.3 — Add PlayMode integration tests

**Finding:** Q3
**Severity:** Major
**Effort:** High (1-2 weeks)
**Current state:** 3 PlayMode test files
**Goal:** Full gameplay-loop integration coverage

**Test files to create:**

| File | Integration loop |
|---|---|
| `Assets/Tests/PlayMode/MatchFlowIntegrationTests.cs` | Match start → unit spawn → combat → victory |
| `Assets/Tests/PlayMode/BuildingPlacementIntegrationTests.cs` | Placement → production → combat chain |
| `Assets/Tests/PlayMode/TransportIntegrationTests.cs` | Boarding → movement → disembark |
| `Assets/Tests/PlayMode/SceneLoadingIntegrationTests.cs` | Scene load → bootstrap sequencing → systems ready |

**Template:**
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MatchFlowIntegrationTests
{
    [UnityTest]
    public IEnumerator MatchStart_SpawnsInitialUnits()
    {
        yield return SceneManager.LoadSceneAsync("Match");
        yield return null; // wait one frame for systems to initialize

        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;
        var query = em.CreateEntityQuery(typeof(UnitHealth));
        Assert.IsTrue(query.CalculateEntityCount() > 0, "Match start should spawn units");
    }
}
```

**Safety notes:**
- PlayMode tests run in a real player loop — they're slower than EditMode. Keep them focused on integration, not unit-level assertions.
- Use `[UnitySetUp]` / `[UnityTearDown]` for scene load/unload to avoid cross-test contamination.

---

## PHASE 4 — Continuous hygiene (no end state, ongoing)

---

### Task 4.1 — Maintain zero managed `class IComponentData`

After Task 2.3 is complete, add a CI check that fails any PR introducing a new managed `IComponentData`:

```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
if [ $(grep -rln "class\s\+\w\+\s*:\s*IComponentData" --include="*.cs" Assets/Game/Scripts/ | wc -l) -gt 0 ]; then
  echo "FAIL: managed class IComponentData found"
  exit 1
fi
```

### Task 4.2 — Maintain zero `Object.Instantiate` in `OnUpdate`

After Task 2.2 is complete, add a CI check (grep-based heuristic for `Object.Instantiate` inside `OnUpdate` methods).

### Task 4.3 — Maintain Burst coverage ratchet

Track the `ISystem w/ [BurstCompile]` count in CI. Fail if it decreases.

---

## Verification — Run after every task

### Per-task compile check
```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath /Users/farhad/Projects/WarlineCapture-Clone \
  -quit \
  -logFile /tmp/compile-check.txt
echo "EXIT: $?"
# Expect: EXIT: 0
grep -iE "error CS|Compilation failed" /tmp/compile-check.txt
# Expect: no output
```

### Full metric re-check
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
echo "P1 .Run() in scripts: $(grep -rh '\.Run()' --include='*.cs' Assets/Game/Scripts/ | wc -l | tr -d ' ')"
echo "P2 Object.Instantiate in systems: $(grep -rh 'Object\.Instantiate' --include='*.cs' Assets/Game/Scripts/Systems/ Assets/Game/Scripts/Environment/ Assets/Game/Scripts/Rendering/Systems/ | wc -l | tr -d ' ')"
echo "P3 managed class IComponentData: $(grep -rln 'class\s\+\w\+\s*:\s*IComponentData' --include='*.cs' Assets/Game/Scripts/ | wc -l | tr -d ' ')"
echo "P5 TransportBoardingCommandSystem lines: $(wc -l Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs 2>/dev/null | awk '{print $1}')"
echo "P6 SystemBase: $(grep -rl ': SystemBase' --include='*.cs' Assets/Game/Scripts/ | wc -l | tr -d ' ')"
echo "P6 ISystem: $(grep -rl ': ISystem' --include='*.cs' Assets/Game/Scripts/ | wc -l | tr -d ' ')"
echo "P6 ISystem w/ Burst: $(for f in $(grep -rl ': ISystem' --include='*.cs' Assets/Game/Scripts/); do grep -q BurstCompile "$f" && echo "$f"; done | wc -l | tr -d ' ')"
echo "P7 WithChangeFilter: $(grep -rh 'WithChangeFilter' --include='*.cs' Assets/Game/Scripts/ | wc -l | tr -d ' ')"
echo "P8 foreach in systems: $(grep -rh 'foreach' --include='*.cs' Assets/Game/Scripts/Systems/ | wc -l | tr -d ' ')"
echo "Q2 EditorApplication.Exit in tests: $(grep -rl 'EditorApplication\.Exit' Assets/Tests/ --include='*.cs' | wc -l | tr -d ' ')"
echo "Q3 PlayMode test files: $(find Assets/Tests/PlayMode -name '*.cs' 2>/dev/null | wc -l | tr -d ' ')"
echo "Q4 CS0618 pragmas: $(grep -rln 'CS0618' --include='*.cs' Assets/Game/Scripts/ | wc -l | tr -d ' ')"
echo "Q5 .DS_Store in Assets: $(find Assets -name '.DS_Store' 2>/dev/null | wc -l | tr -d ' ')"
echo "A4 empty placeholder folders: $(for d in Assets/Game/Scripts/Bootstrap Assets/Game/Scripts/Rewards Assets/Game/Scripts/Profile; do [ -d "$d" ] && echo x; done | wc -l | tr -d ' ')"
echo "A5 scriptingBackend platforms: $(grep -A3 'scriptingBackend:' ProjectSettings/ProjectSettings.asset | grep -c ': 1')"
echo "A7 runInBackground: $(grep 'runInBackground:' ProjectSettings/ProjectSettings.asset | awk '{print $2}')"
echo "A6 quality tiers: $(grep -c '    name:' ProjectSettings/QualitySettings.asset)"
```

### Expected end-state metrics (after all phases complete)

| Metric | Goal |
|---|---|
| P1 `.Run()` in scripts | <= 3 (only genuinely single-threaded jobs) |
| P2 `Object.Instantiate` in systems | <= 2 (only Pattern C one-time setup) |
| P3 managed `class IComponentData` | 0 |
| P5 `TransportBoardingCommandSystem.cs` | deleted (5 split systems, each <800 lines) |
| P6 `SystemBase` count | <= 198 |
| P6 `ISystem` count | >= 162 |
| P6 `ISystem w/ [BurstCompile]` | >= 100 |
| P7 `WithChangeFilter` | >= 6 |
| P8 `foreach` over managed dictionaries in `Systems/` | 0 |
| Q1 system test coverage | >= 80% on critical subsystems |
| Q2 unguarded `EditorApplication.Exit` in tests | 0 |
| Q3 PlayMode test files | >= 7 |
| Q4 `CS0618` pragmas | 0 |
| Q5 `.DS_Store` in Assets | 0 |
| A4 empty placeholder folders | 0 |
| A5 IL2CPP platforms | 3 (Android, iOS, Standalone) |
| A6 quality tiers | 4 (Low, Mobile, PC, Ultra) |
| A7 `runInBackground` | 1 |

---

**Plan created:** 2026-06-18
**Source audit:** `2026-06-18_audit_unity-ecs-architecture-performance-quality.md`
**First pass completion:** Q5, A4, A5, A7, P1 (2 sites), P7 (1 site) — verified with Unity batch mode exit 0