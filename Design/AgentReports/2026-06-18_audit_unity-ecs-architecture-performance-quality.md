# Unity ECS Audit — Architecture, Performance, Quality

**Date:** 2026-06-18  
**Project:** `/Users/farhad/Projects/WarlineCapture-Clone`  
**Unity:** 6000.4.0f1 · URP 17.4.0 · Entities 6.4.0  
**Scope:** 723 C# files · ~162,688 lines · 14 game assemblies + 2 test assemblies  
**Verification:** Unity batch mode compilation — all 15 assemblies compiled with zero CS errors, exit code 0  

---

## How to Use This Document

Each finding has:
- **File:** exact path and line number(s)
- **Problem:** what is wrong and why it matters
- **Solution:** what the correct pattern looks like
- **How to fix:** step-by-step instructions an agent can follow
- **Severity:** Critical / Major / Minor
- **Effort:** Low (<1h) / Medium (1-4h) / High (>4h)

---

## PERFORMANCE FINDINGS

---

### P1 — 88% of ECS Jobs Run on Main Thread (.Run() instead of .ScheduleParallel())

**Severity:** Major  
**Effort:** Medium (1-2h per system)

**Problem:**
15 of 17 job-bearing systems call `.Run()` which executes on the main thread. Only 2 use `.ScheduleParallel()`. This wastes Unity's worker threads and creates a CPU bottleneck on the main thread.

**Affected files and lines:**

| File | Line | Current |
|---|---|---|
| `Assets/Game/Scripts/Systems/UnitHealthBarSystem.cs` | 26 | `.Run()` |
| `Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs` | 127 | `.Run()` |
| `Assets/Game/Scripts/Systems/UnitRuntimeHealthBarSystem.cs` | 35 | `.Run()` |
| `Assets/Game/Scripts/Systems/UnitDeathSystem.cs` | 56 | `.Run()` |
| `Assets/Game/Scripts/Systems/UnitDeathSystem.cs` | 87 | `.Run()` |
| `Assets/Game/Scripts/Systems/UnitAnimationIndexSystem.cs` | 43 | `.Run()` |
| `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs` | 31 | `.Run()` |
| `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs` | 37 | `.Run()` |
| `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs` | 42 | `.Run()` |
| `Assets/Game/Scripts/Systems/VehicleWreckCleanupSystem.cs` | 30 | `.Run()` |
| `Assets/Game/Scripts/Systems/UnitDestroyedVisualSystem.cs` | 30 | `.Run()` |
| `Assets/Game/Scripts/Rendering/Systems/UnitSelectionMarkerSystem.cs` | 97 | `.Run()` |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetDistanceSystem.cs` | 52 | `.Run()` |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSortSystem.cs` | 16 | `.Run()` |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetBandSystem.cs` | 70 | `.Run()` |

**Already correct (reference examples):**
- `Assets/Game/Scripts/Systems/VehicleSlopeAlignmentSystem.cs:28` — `.ScheduleParallel()` ✅
- `Assets/Game/Scripts/Systems/UnitLookAtTargetSystem.cs:23` — `.ScheduleParallel()` ✅

**Solution:**
Replace `.Run()` with `.ScheduleParallel()` in systems that do data-parallel work over entity queries. `.Run()` should only be used when the system inherently must run on the main thread (UI creation, camera manipulation, input reading).

**How to fix (per system):**

1. Open the file at the line listed above
2. Find the `}.Run();` call at the end of the `Entities.ForEach` or `IJobEntity` scheduling block
3. Replace `.Run()` with `.ScheduleParallel()`
4. If the system uses `SystemBase` with `Entities.ForEach`, ensure no managed-type access (strings, GameObjects, managed components) inside the lambda — managed access forces `.Run()`. If managed access is present, the system must be refactored to split managed/unmanaged paths (see P3 for managed component removal)
5. If the system is `ISystem` with `[BurstCompile]`, ensure `[BurstCompile]` is on the `OnUpdate` method and no `WithoutBurst()` is in the chain
6. Compile and run Unity batch mode to verify:
   ```
   "/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -quit -logFile /tmp/compile-check.txt
   ```

**Priority order:** Start with the render budget systems (they run every frame on the most entities):
1. `UnitRenderBudgetDistanceSystem.cs:52`
2. `UnitRenderBudgetSortSystem.cs:16`
3. `UnitRenderBudgetBandSystem.cs:70`
4. `UnitHealthBarSystem.cs:26`
5. `UnitAnimationIndexSystem.cs:43`

---

### P2 — Object.Instantiate Inside ECS Systems (15 calls)

**Severity:** Major  
**Effort:** High (1-2 weeks total, ~1h per call site)

**Problem:**
`Object.Instantiate` creates managed GameObjects directly inside ECS system `OnUpdate` methods. Each call causes:
- GC allocation (managed heap)
- Main-thread sync point (cannot be Burst-compiled)
- Breaks ECS structural change rules (should use `EntityCommandBuffer.Instantiate`)

**Affected files and lines:**

| File | Line(s) | What is instantiated |
|---|---|---|
| `Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs` | 679, 680 | Building visual template / prefab |
| `Assets/Game/Scripts/Systems/BuildingDestroyedVisualSystem.cs` | 50 | Destroyed building visual prefab |
| `Assets/Game/Scripts/Systems/RoadBuildDefinitionProjectionSystem.cs` | 38 | Road definition prefab (temp) |
| `Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs` | 187, 269, 533, 742 | Road / intersection visual GameObjects |
| `Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs` | 33 | Building placement preview prefab |
| `Assets/Game/Scripts/Systems/SelectionOrderMarkerSystem.cs` | 685, 708, 729, 907 | Move/attack order marker prefabs |
| `Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnSystem.cs` | 175 | Map building visual |
| `Assets/Game/Scripts/Systems/BuildingSelectionMarkerSystem.cs` | 189 | Selection marker prefab |
| `Assets/Game/Scripts/Environment/RuntimeCityVisualSystem.cs` | 82, 84 | City visual mesh / prefab |
| `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs` | 150 | Decoration instance |
| `Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs` | 354 | Grid blocker visual |
| `Assets/Game/Scripts/Environment/DayNightSystem.cs` | 213 | Skybox material (one-time, acceptable) |
| `Assets/Game/Scripts/UI/Shell/UIScreenRouteFlowSystem.cs` | 110 | UI screen prefab (UI layer, acceptable) |

**Solution:**
Convert prefabs to ECS entity prefabs via baking/subscene. Use `EntityCommandBuffer.Instantiate()` to spawn entity copies, then use a rendering system to associate visual meshes. For markers and UI-adjacent GameObjects that must remain as GameObjects, batch the instantiation in a managed system outside the ECS update loop, or use an object pool.

**How to fix (per pattern):**

**Pattern A — Pure visual prefabs (buildings, roads, decorations):**
1. Move the prefab reference to a blob asset or static registry
2. In the baker, register the prefab as an entity prefab (`Baker.GetEntity()` with `Prefab` tag)
3. In the system, use `EntityCommandBuffer.Instantiate(entityPrefab)` instead of `Object.Instantiate`
4. Use `EntityManager.AddComponents` or ECB to attach mesh/material via `MaterialMeshInfo` component
5. Remove the `GameObject` field from the component

**Pattern B — Markers and selection visuals (SelectionOrderMarkerSystem, BuildingSelectionMarkerSystem):**
1. Create an object pool initialized in `OnCreate` (pre-instantiate N marker GameObjects)
2. In `OnUpdate`, activate/deactivate pooled objects instead of instantiating
3. Track active markers in a `NativeHashMap<Entity, GameObject>` for recycling
4. See `UnitSelectionMarkerSystem` for partial implementation of this pattern already

**Pattern C — One-time setup (DayNightSystem, UIScreenRouteFlowSystem):**
These are acceptable. One-time instantiation in setup/init is fine. Leave as-is.

---

### P3 — 10 Managed IComponentData Classes (Blocks Burst, Causes GC)

**Severity:** Major  
**Effort:** Medium (2-4 days)

**Problem:**
`class IComponentData` (managed components) cannot be used in Burst-compiled systems. They allocate on the managed heap and cause GC pressure during structural changes.

**Affected files:**

| File | Line | Component | Holds |
|---|---|---|---|
| `Assets/Game/Scripts/Composition/MatchSceneReferenceComponent.cs` | 3 | `MatchSceneReferenceComponent` | Scene references (MonoBehaviour) |
| `Assets/Game/Scripts/Components/UnitVisualComponents.cs` | 232 | `UnitAttachedLightSet` | Light prefab refs |
| `Assets/Game/Scripts/Components/UnitVisualComponents.cs` | 252 | `UnitAttachedLightRuntime` | Runtime light instances |
| `Assets/Game/Scripts/Components/RuntimeCameraReferenceComponent.cs` | 4 | `RuntimeCameraReferenceComponent` | Camera reference |
| `Assets/Game/Scripts/Components/UnitPoseMeshesSetup.cs` | 5 | `UnitPoseMeshesSetup` | Mesh data |
| `Assets/Game/Scripts/Components/CombatComponents.cs` | 173 | `UnitAttackImpactVfxReference` | VFX prefab refs |
| `Assets/Game/Scripts/Components/CombatComponents.cs` | 178 | `UnitMuzzleFlashVfxReference` | VFX prefab refs |
| `Assets/Game/Scripts/Components/CombatComponents.cs` | 289 | `GroundMissileLauncherVfxReferenceComponent` | VFX prefab refs |
| `Assets/Game/Scripts/Components/CombatComponents.cs` | 429 | `AirMissileLauncherVfxReferenceComponent` | VFX prefab refs |
| `Assets/Game/Scripts/RuntimeState/PerformanceDiagnosticsReferenceComponent.cs` | 3 | `PerformanceDiagnosticsReferenceComponent` | Diagnostics object |

**Solution:**
Replace managed components with unmanaged alternatives:
- **VFX/Light prefab refs →** `BlobAssetReference<T>` containing indices into a static registry, or `Entity` references to prefab entities
- **Camera ref →** `Entity` reference to a camera entity, resolved via `EntityManager.GetComponentData<CameraData>` or a shared static pointer
- **Scene refs →** Store in a single managed system field (not as a component), access via `SystemAPI.GetSingleton`
- **Diagnostics →** Static managed field, not an ECS component

**How to fix (example for VFX references):**

1. Create a static registry class:
   ```csharp
   // Assets/Game/Scripts/Components/VfxRegistry.cs
   public static class VfxRegistry {
       private static readonly Dictionary<int, GameObject> _prefabs = new();
       public static void Register(int id, GameObject prefab) => _prefabs[id] = prefab;
       public static GameObject Get(int id) => _prefabs.TryGetValue(id, out var p) ? p : null;
   }
   ```
2. Replace the managed component with an unmanaged struct:
   ```csharp
   public struct UnitMuzzleFlashVfxRef : IComponentData {
       public int PrefabId; // index into VfxRegistry
   }
   ```
3. In the baker, register the prefab and store the ID:
   ```csharp
   int id = prefab.GetInstanceID();
   VfxRegistry.Register(id, prefab);
   baker.AddComponent(new UnitMuzzleFlashVfxRef { PrefabId = id });
   ```
4. In the system, look up via `VfxRegistry.Get(prefabId)` — this works in Burst with `[BurstDiscard]` on the lookup method
5. Remove the old `class IComponentData` definition
6. Update all systems that referenced the old component
7. Compile-check with Unity batch mode

---

### P4 — Per-Frame Managed Allocations in Hot Systems

**Severity:** Major  
**Effort:** Low (2-4h total)

**Problem:**
Several systems allocate `new List<>` or `new Dictionary<>` inside `OnUpdate`, causing GC allocations every frame.

**Affected files and lines:**

| File | Line | Allocation | Fix |
|---|---|---|---|
| `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs` | 267 | `new List<Vector2Int>()` | Cache in field, clear with `.Clear()` each frame |
| `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs` | 308 | `new List<Vector2Int>()` | Same |
| `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs` | 329 | `new List<WallRun>()` | Same |
| `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs` | 367 | `new List<WallRun>()` | Same |
| `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs` | 135 | `new Dictionary<byte, RectInt>()` | Cache in field, `.Clear()` each frame |
| `Assets/Game/Scripts/Systems/BuildingVisualSystem.cs` | 95 | `new List<AnimatedPart>()` | Cache in field |
| `Assets/Game/Scripts/Systems/SelectionUiReadModelLookup.cs` | 460 | `new List<string>()` | Cache in field |
| `Assets/Game/Scripts/Systems/AttackOrderCommandSystem.cs` | 481 | `new List<Entity>()` | Cache in field |
| `Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs` | 763 | `new List<...>(count)` | Pre-allocate with max capacity |

**Solution:**
Move all collection allocations to `OnCreate` as instance fields. In `OnUpdate`, call `.Clear()` then populate. This reuses the same allocated memory every frame.

**How to fix (template for BuildingPlacementInputSystem):**

1. Open `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs`
2. Add cached fields at class level:
   ```csharp
   private readonly List<Vector2Int> _originsScratch = new(64);
   private readonly List<WallRun> _wallRunsScratch = new(32);
   ```
3. In `OnCreate`, optionally set initial capacity:
   ```csharp
   _originsScratch.EnsureCapacity(64);
   _wallRunsScratch.EnsureCapacity(32);
   ```
4. In `OnUpdate` at lines 267, 308, 329: replace `var origins = new List<Vector2Int>();` with `_originsScratch.Clear();` and use `_originsScratch` in place of `origins`
5. Same pattern for line 135 of `BuildingBarrierSystem.cs`:
   ```csharp
   // field:
   private readonly Dictionary<byte, RectInt> _perimetersScratch = new();
   // OnUpdate:
   _perimetersScratch.Clear();
   ```
6. Repeat for each file in the table above
7. Compile-check with Unity batch mode

---

### P5 — TransportBoardingCommandSystem.cs God System (3,875 lines)

**Severity:** Major  
**Effort:** High (1 week)

**Problem:**
Single file with 3,875 lines, 89 methods, 13 `Try*` methods covering boarding, airdrop, ramp approach, capacity, pathfinding, and validation. Unmaintainable. Contains inline magic numbers.

**File:** `Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs`

**Magic numbers at:**

| Line | Value | Meaning |
|---|---|---|
| 2287 | `* 100` | Cell distance penalty multiplier |
| 2547 | `1000` | Direction penalty worst-case |
| 2940 | `0.8f` | Drop interval seconds (one unit type) |
| 3323 | `0.65f` | Drop interval seconds (another unit type) |

**Solution:**
Split into 5 focused systems and extract magic numbers to named constants or config.

**How to fix:**

1. Create new files in `Assets/Game/Scripts/Systems/`:
   - `TransportBoardingRequestSystem.cs` — boarding request validation and goal assignment (methods `TryResolveSelectedBoardTransport`, `TryIssueBoardNearestSoldierOrders`, `TryFindTransportBoardingGoal`)
   - `TransportPlaneRampSystem.cs` — plane ramp approach logic (methods `TryFindPlaneRampApproachCell`, ramp distance calculations)
   - `TransportAirdropSystem.cs` — airdrop logic (methods with `DropIntervalSeconds`)
   - `TransportCapacityCheckSystem.cs` — capacity validation (methods checking passenger counts)
   - `TransportDisembarkSystem.cs` — disembark/landing logic

2. Extract magic numbers to constants at the top of each split file:
   ```csharp
   private const int CellDistancePenaltyMultiplier = 100;
   private const int DirectionPenaltyWorst = 1000;
   private const float DropIntervalSecondsHeavy = 0.8f;
   private const float DropIntervalSecondsLight = 0.65f;
   ```
   Or better: move to a `TransportConfig` ScriptableObject or `IBlobAssetReferenceData` for designer tuning.

3. Move the relevant `Try*` methods into the appropriate new system file
4. Update `MatchBootstrapSystem` to create each new system if needed
5. Compile-check with Unity batch mode

---

### P6 — Low Burst Coverage (49 of 112 ISystem files = 44%)

**Severity:** Major  
**Effort:** Medium (ongoing)

**Problem:**
Only 49 of 112 `ISystem` files have `[BurstCompile]`. 63 ISystem files lack Burst, meaning they run interpreted. Combined with 248 `SystemBase` files (which cannot be Burst-compiled at all), total Burst coverage is ~14% of all systems.

**How to find unbursted ISystem files:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
# List ISystem files WITHOUT BurstCompile
for f in $(grep -rl ': ISystem' Assets/Game/Scripts/Systems/ --include="*.cs"); do
  if ! grep -q 'BurstCompile' "$f"; then
    echo "$f"
  fi
done
```

**Solution:**
Add `[BurstCompile]` to the `OnUpdate` method of each ISystem file. Ensure all data accessed inside `OnUpdate` is unmanaged (no managed components, no string interpolation, no `Debug.Log`).

**How to fix (per file):**

1. Open the ISystem file
2. Add `[BurstCompile]` above `public void OnUpdate(ref SystemState state)`
3. If compilation fails, identify the managed access:
   - Replace `Debug.Log` with `[BurstDiscard]` method or remove
   - Replace string interpolation with `FixedString` utilities
   - Replace managed component access with unmanaged equivalents (see P3)
   - Replace `EntityManager` calls with `SystemAPI` equivalents
4. If the system uses `EntityCommandBuffer`, ensure it's `EntityCommandBuffer` not `ManagedCommandBuffer`
5. Compile-check

---

### P7 — Only 1 WithChangeFilter Usage (Severely Underused)

**Severity:** Minor  
**Effort:** Low (4h)

**Problem:**
`WithChangeFilter<T>()` tells ECS to skip chunks where component `T` hasn't changed since the last update. With only 1 usage across the entire codebase, systems that poll for changes run every frame even when nothing changed.

**Current usage:**
- `Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs:357` — `.WithChangeFilter<UnitGrid>()` ✅

**Systems that should add WithChangeFilter:**

| File | Component to filter on | Why |
|---|---|---|
| `Assets/Game/Scripts/Systems/UnitHealthBarSystem.cs` | `Health` | Health bars only need updating when health changes |
| `Assets/Game/Scripts/Systems/UnitRuntimeHealthBarSystem.cs` | `Health` | Same |
| `Assets/Game/Scripts/Systems/UnitAnimationIndexSystem.cs` | `UnitAnimationState` | Animation only changes on state transitions |
| `Assets/Game/Scripts/Systems/UnitDeathSystem.cs` | `Health` | Death check only triggers when health changes |
| `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs` | `LocalTransform` | Minimap markers only update on position change (may need different filter) |

**How to fix (per system):**

1. Open the system file
2. Find the `Entities.ForEach` or `IJobEntity` scheduling block
3. Add `.WithChangeFilter<YourComponent>()` to the chain:
   ```csharp
   Entities.WithAll<Health>()
           .WithChangeFilter<Health>()  // <-- add this
           .ForEach(...)
           .ScheduleParallel();
   ```
4. Compile-check

---

### P8 — 199 foreach Loops, Many Over Managed Dictionaries

**Severity:** Minor  
**Effort:** Medium (ongoing)

**Problem:**
`foreach` over `Dictionary<K,V>` allocates a managed enumerator (24 bytes GC alloc per iteration on some runtimes). In hot paths called every frame, this adds up.

**Known offender:**
- `Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs:36` — `foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingDictionary)`
- `Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs:59` — same pattern

**Solution:**
Use `NativeHashMap<K,V>` instead of `Dictionary<K,V>` for ECS data, which avoids managed enumerator allocations. If managed dictionaries must stay (e.g., for managed asset lookup), cache the key array and iterate by index.

**How to fix:**

1. For `BuildingRunwaySystem.cs`:
   - If `runtimeBuildingDictionary` can be a `NativeHashMap<int, RuntimeBuildingEntity>`, convert it
   - If it must stay managed, change iteration to:
     ```csharp
     var keys = runtimeBuildingDictionary.Keys; // cached, not allocated per-frame
     foreach (var key in keys) {
           var pair = runtimeBuildingDictionary[key];
           // ...
     }
     ```
   - Better: use `for (int i = 0; i < keys.Count; i++)` with indexed access

---

### P9 — Shadow Cascades = 4, Distance = 240 (Overkill for Mobile RTS)

**Severity:** Minor  
**Effort:** Low (5 minutes)

**Problem:**
4 shadow cascades split at {0.123, 0.293, 0.536} with 240m distance. For an RTS with many on-screen units viewed from above, 4 cascades is excessive GPU cost. 2 cascades at a shorter distance gives nearly identical visual quality at half the shadow pass cost.

**File:** `ProjectSettings/QualitySettings.asset`

Current (both Mobile and PC tiers):
```yaml
shadowCascades: 4
shadowDistance: 240
shadowCascade4Split: {x: 0.12299999, y: 0.2926, z: 0.53599995}
```

**How to fix:**

1. Open `ProjectSettings/QualitySettings.asset` in a text editor
2. For the Mobile tier (first quality block), change:
   ```yaml
   shadowCascades: 2
   shadowDistance: 150
   shadowCascade2Split: 0.5
   ```
3. For the PC tier, optionally keep 4 cascades but reduce distance to 180:
   ```yaml
   shadowCascades: 4
   shadowDistance: 180
   ```
4. Save the file
5. Open Unity to verify shadows still look acceptable
6. Alternatively, change via `QualitySettings.shadowCascades = 2; QualitySettings.shadowDistance = 150;` in a runtime init script

---

## ARCHITECTURE FINDINGS

---

### A1 — 248 SystemBase vs 112 ISystem (66% Managed)

**Severity:** Major  
**Effort:** High (2-4 weeks for top 50)

**Problem:**
248 systems inherit from `SystemBase` (managed class). Only 112 use `ISystem` (struct, Burst-compatible). Managed SystemBase cannot be Burst-compiled, has managed overhead, and allocates on the heap.

**How to find candidates for migration:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
# List SystemBase files that do NOT reference managed types
# (heuristic: no Object.Instantiate, no managed component access)
for f in $(grep -rl ': SystemBase' Assets/Game/Scripts/Systems/ --include="*.cs"); do
  if ! grep -q 'Object\.Instantiate\|GetComponent<\|GetComponentData<.*Managed\|class.*IComponentData' "$f"; then
    echo "$f"
  fi
done
```

**Solution:**
Migrate hot-path `SystemBase` files to `ISystem` (struct-based).

**How to fix (per system):**

1. Change class declaration:
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
2. Replace `EntityManager` with `state.EntityManager` / `SystemAPI`
3. Replace `EntityQuery` creation with `state.GetEntityQuery`
4. Replace `GetComponent<>()` with `SystemAPI.GetComponent<>()`
5. Remove any `public` fields — ISystem is a struct, use fields with care
6. Add `[BurstCompile]` to `OnCreate` and `OnUpdate`
7. If the system uses managed types, either:
   - Split into a Burst-compiled ISystem for data processing + a thin SystemBase for managed access
   - Or remove the managed dependency (see P3)
8. Update `MatchBootstrapSystem` if it creates the system via `GetOrCreateSystemManaged<T>()` — change to `GetOrCreateSystem<T>()` or `state.World.GetOrCreateSystem<T>()`
9. Compile-check

**Priority:** Start with systems in `Systems/` that run every frame and don't touch managed components. Use the heuristic above to find candidates.

---

### A2 — Assembly Graph Is Clean (No Circular Dependencies) ✅

**Severity:** N/A — this is a positive finding

**Assembly dependency graph:**
```
Leaf (no game deps):
  Game.Catalog.Contracts
  Game.UI.Contracts
  Game.Rendering.Contracts

Mid-level:
  Game.Components → (Unity ECS)
  Game.Configs → Game.Components, Game.Catalog.Contracts
  Game.Authoring → Game.Components, Game.Configs
  Game.Rendering → Game.Components, Game.Configs, Game.Rendering.Contracts
  Game.UI.Runtime → Game.UI.Contracts, Game.Catalog.Contracts
  Game.UI.Shell.Contracts.Ecs → Game.UI.Contracts
  Game.UI.Shell.Ecs → Game.UI.Contracts, Game.UI.Shell.Contracts.Ecs

Runtime:
  Game.Runtime → Game.Components, Game.Configs, Game.Rendering.Contracts, Game.UI.Contracts

Orchestration:
  Game.Composition → (13 game assemblies — expected for composition root)

Editor-only:
  Game.Editor → (all, Editor platform only) ✅
```

**No action needed.** Assembly separation is clean.

---

### A3 — MatchBootstrapSystem Fragile Null-Conditional Access

**Severity:** Major  
**Effort:** Low (2-4h)

**File:** `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs` (1,179 lines)

**Problem:**
20+ null-conditional property accessors of the form `sceneView != null ? sceneView.WorldCamera : null`. If `sceneView` becomes null at the wrong time, systems silently receive null references and fail later with unclear NullReferenceExceptions.

**Solution:**
Add a validation step at the start of bootstrap that fails fast with a clear error if the scene view or its dependencies are not ready.

**How to fix:**

1. Open `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
2. At the beginning of the initialization flow (after scene view is expected to be available), add:
   ```csharp
   if (sceneView == null)
       throw new InvalidOperationException("MatchBootstrapSystem: sceneView is null. Scene must be loaded before bootstrap.");
   if (sceneView.WorldCamera == null)
       throw new InvalidOperationException("MatchBootstrapSystem: sceneView.WorldCamera is null. Camera setup incomplete.");
   ```
3. Replace null-conditional accessors with direct access:
   ```csharp
   // FROM:
   public Camera WorldCamera => sceneView != null ? sceneView.WorldCamera : null;
   // TO:
   public Camera WorldCamera => sceneView.WorldCamera; // validated at init
   ```
4. Compile-check

---

### A4 — Empty Placeholder Folders

**Severity:** Minor  
**Effort:** Low (1 min)

**Folders:**
- `Assets/Game/Scripts/Bootstrap/` — 0 .cs files
- `Assets/Game/Scripts/Rewards/` — 0 .cs files
- `Assets/Game/Scripts/Profile/` — 0 .cs files

**How to fix:**
- Either delete the folders (and their `.meta` files) if unused
- Or add a `.gitkeep` file + keep the `.meta` if planned for future use
- Verify `.meta` files exist for Unity folder tracking

---

### A5 — Scripting Backend Defaults to Mono (IL2CPP Not Configured)

**Severity:** Major  
**Effort:** Low (5 min to change, requires full rebuild)

**File:** `ProjectSettings/ProjectSettings.asset`

Current:
```yaml
scriptingBackend:
```
(empty = defaults to Mono)

**How to fix:**

1. Open `ProjectSettings/ProjectSettings.asset` in a text editor
2. Change to:
   ```yaml
   scriptingBackend: 1
   ```
   (1 = IL2CPP, 0 = Mono)
3. Or set via Unity Editor: Edit → Project Settings → Player → Other Settings → Scripting Backend → IL2CPP
4. Do a full clean build to verify IL2CPP compilation succeeds
5. **Warning:** IL2CPP increases build time significantly. Set this for release builds, keep Mono for dev iteration

---

### A6 — Only 2 Quality Tiers (Mobile + PC), No Low/Ultra

**Severity:** Minor  
**Effort:** Low (1h)

**File:** `ProjectSettings/QualitySettings.asset`

**Current tiers:**
- Tier 0: "Mobile" — shadow res Medium, AA off, LOD bias 1, skin weights 2
- Tier 1: "PC" — shadow res High, AA 4x, LOD bias 2, skin weights 4

**Problem:** No "Low" tier for weaker mobile devices. No "Ultra" tier for high-end desktop. RTS with many on-screen units needs aggressive scaling.

**How to fix:**

1. In Unity Editor: Edit → Project Settings → Quality
2. Add 2 more tiers:
   - **"Low"** — shadow cascades 1, shadow distance 80, shadow res Low, AA off, LOD bias 0.7, skin weights 1, pixel light count 0
   - **"Ultra"** — shadow cascades 4, shadow distance 300, shadow res Very High, AA 8x, LOD bias 3, skin weights 4, pixel light count 8
3. Assign Low tier to low-end mobile devices in platform quality matrix
4. Or edit `QualitySettings.asset` directly to add the tier blocks

---

### A7 — runInBackground = 0 (Simulation Pauses on Alt-Tab)

**Severity:** Minor  
**Effort:** Low (1 min)

**File:** `ProjectSettings/ProjectSettings.asset`

**How to fix:**
Set `runInBackground: 1` for single-player. Keep `0` only if multiplayer fairness requires deterministic pausing.
```yaml
runInBackground: 1
```

---

## CODE QUALITY FINDINGS

---

### Q1 — 81% of Systems Have No Test Coverage (353/435 untested)

**Severity:** Major  
**Effort:** High (ongoing)

**Test coverage by subsystem:**

| Subsystem | Systems | Tested | Coverage |
|---|---|---|---|
| AI | 12 | 10 | 83% ✅ |
| Transport/Boarding | 12 | 10 | 83% ✅ |
| Missiles | 4 | 4 | 100% ✅ |
| Selection (core) | 8 | 6 | 75% ✅ |
| Building Combat | 5 | 3 | 60% |
| **Building Placement** | 30+ | 1 | ~3% 🔴 |
| **Road Build** | 27 | 1 | ~4% 🔴 |
| **City Generation** | 42 | 0 | 0% 🔴 |
| **Unit Combat** | 8 | 0 | 0% 🔴 |
| **Unit Render Budget** | 25+ | 1 | ~4% 🔴 |
| **Citizen Population** | 15 | 2 | 13% 🔴 |
| **Building Runtime** | 20+ | 1 | ~5% 🔴 |
| **Match Lifecycle** | 5 | 0 | 0% 🔴 |

**Total test files:** 111 Editor (EditMode) + 3 PlayMode = 114 files, 847 `[Test]` methods, 5,700 `Assert` calls.

**How to fix (priority order):**

1. **City Generation (42 systems, 0 tests):** Create test file `Assets/Tests/Editor/RuntimeCityGenerationTests.cs`. Test that city generation produces expected building counts, grid layout validity, spawn positions within bounds.
2. **Unit Combat (8 systems, 0 tests):** Create `Assets/Tests/Editor/UnitCombatTests.cs`. Test attack resolution, death state transitions, damage application.
3. **Building Placement (30+ systems, 1 test):** Create `Assets/Tests/Editor/BuildingPlacementTests.cs`. Test validation, grid snapping, wall run logic, session lifecycle.
4. **Road Build (27 systems, 1 test):** Create `Assets/Tests/Editor/RoadBuildTests.cs`. Test road path planning, visual variant selection, intersection handling.

**Test template:**
```csharp
[Test]
public void SystemName_ExpectedBehavior_WhenCondition()
{
    var world = new World("Test");
    var system = world.GetOrCreateSystem<MySystem>();
    // set up entities
    var entity = world.EntityManager.CreateEntity(typeof(MyComponent));
    world.EntityManager.SetComponentData(entity, new MyComponent { Value = 42 });
    // run system
    system.Update();
    // assert
    var result = world.EntityManager.GetComponentData<MyComponent>(entity);
    Assert.AreEqual(42, result.Value);
    world.Dispose();
}
```

---

### Q2 — EditorApplication.Exit() in 55 of 111 Test Files

**Severity:** Major  
**Effort:** Medium (1-2 days)

**Problem:**
55 test files contain `EditorApplication.Exit(0)` or `EditorApplication.Exit(1)` calls. These double as CLI validation runners. When run in the Unity Test Runner window, they **kill the Unity Editor process**.

**How to find affected files:**
```bash
cd /Users/farhad/Projects/WarlineCapture-Clone
grep -rl 'EditorApplication.Exit' Assets/Tests/ --include="*.cs"
```

**Solution:**
Separate CLI validation runner logic from NUnit `[Test]` classes.

**How to fix (per file):**

1. Open each affected test file
2. Find the `RunFocusedValidation()` static method (or similar)
3. Guard the `EditorApplication.Exit` call:
   ```csharp
   [MenuItem("Validation/Run MySystem Tests")]
   public static void RunFocusedValidation()
   {
       var result = RunAllTests();
   #if UNITY_EDITOR
       if (System.Environment.GetCommandLineArgs().Contains("-runTests"))
       {
           EditorApplication.Exit(result ? 0 : 1);
       }
   #endif
   }
   ```
4. Alternatively, move `RunFocusedValidation()` methods into a separate `ValidationRunner.cs` file that is excluded from the Test assembly
5. Or use `#if !UNITY_INCLUDE_TESTS` guard on the Exit calls

---

### Q3 — Only 3 PlayMode Tests

**Severity:** Major  
**Effort:** High (1-2 weeks)

**Problem:**
111 EditMode tests but only 3 PlayMode tests. No integration tests for:
- Match start → unit spawn → combat → victory flow
- Building placement → production → combat chain
- Transport boarding → movement → disembark
- Scene loading and bootstrap sequencing

**How to fix:**

1. Create `Assets/Tests/PlayMode/MatchFlowIntegrationTests.cs`:
   ```csharp
   [UnityTest]
   public IEnumerator MatchStart_SpawnsInitialUnits()
   {
       yield return SceneManager.LoadSceneAsync("Match");
       // verify units spawned
   }
   ```
2. Create `Assets/Tests/PlayMode/CombatIntegrationTests.cs`
3. Create `Assets/Tests/PlayMode/TransportIntegrationTests.cs`
4. Each PlayMode test should test a full gameplay loop, not just one system

---

### Q4 — #pragma Warning Disable CS0618 (Deprecated API)

**Severity:** Minor  
**Effort:** Low (30 min)

**File:** `Assets/Game/Scripts/Rendering/UnitImpostorRenderSystem.cs:428`

**Problem:** Using deprecated Unity API suppressed with `#pragma warning disable CS0618`.

**How to fix:**
1. Open the file at line 428
2. Identify which API is deprecated (check the `#pragma` comment or the method being called)
3. Look up the current replacement in Unity 6 docs
4. Replace the deprecated call
5. Remove the `#pragma warning disable CS0618` line
6. Compile-check

---

### Q5 — .DS_Store in Assets Folder

**Severity:** Minor  
**Effort:** Low (1 min)

**File:** `Assets/Game/Scripts/.DS_Store`

**How to fix:**
1. Delete the file: `rm "Assets/Game/Scripts/.DS_Store"`
2. Add to `.gitignore`:
   ```
   # macOS
   .DS_Store
   Assets/**/.DS_Store
   ```
3. If the `.meta` file exists for it, delete `Assets/Game/Scripts/.DS_Store.meta` too

---

### Q6 — No TODO/FIXME/HACK Comments ✅

**Positive finding.** Zero TODO/FIXME/HACK comments across all game scripts. Code is clean of technical debt markers. Maintain this discipline.

---

### Q7 — Entity Query Caching Done Correctly ✅

**Positive finding.** 171 `GetEntityQuery` calls, all in `OnCreate` (0 in `OnUpdate`). 167 `RequireForUpdate` attributes. 835 EntityCommandBuffer references. This is textbook-correct ECS practice. No action needed.

---

## QUICK WINS (Do First, High Impact, Low Effort)

| # | Finding | File(s) | Effort | Impact |
|---|---|---|---|---|
| 1 | Cache reusable collections | P4 table above | 2-4h | Eliminates per-frame GC |
| 2 | Shadow cascades 4→2, distance 240→150 | P9 | 5 min | GPU savings |
| 3 | Add WithChangeFilter to 5 polling systems | P7 table above | 4h | Skip 90% of frames |
| 4 | Remove empty folders | A4 | 1 min | Cleaner project |
| 5 | Delete .DS_Store, add .gitignore | Q5 | 1 min | Cleaner repo |
| 6 | Set IL2CPP scripting backend | A5 | 5 min | Production builds |
| 7 | Convert 5 `.Run()` → `.ScheduleParallel()` | P1 priority list | 1 day | Worker thread utilization |
| 8 | Add `[BurstCompile]` to 10 hottest ISystem files | P6 | 1 day | 2-10x speedup |

---

## MEDIUM-TERM RECOMMENDATIONS

| # | Finding | Effort | Impact |
|---|---|---|---|
| 1 | Replace 10 managed IComponentData with unmanaged alternatives (P3) | 2-4 days | Enables Burst for 15-20 more systems |
| 2 | Split TransportBoardingCommandSystem into 5 systems (P5) | 1 week | Maintainability + per-subsystem parallelism |
| 3 | Migrate 50 hot-path SystemBase → ISystem (A1) | 2-4 weeks | Major CPU improvement |
| 4 | Convert Object.Instantiate to ECB/pooling (P2) | 1 week | Eliminates GC spikes |
| 5 | Add test coverage for City Gen, Unit Combat, Building Placement (Q1) | 2-4 weeks | Regression safety |
| 6 | Add 3-4 quality levels (A6) | 1 day | Device tiering |
| 7 | Fix EditorApplication.Exit in test files (Q2) | 1-2 days | Tests runnable in Editor |
| 8 | Add PlayMode integration tests (Q3) | 1-2 weeks | Integration regression safety |

---

## LONG-TERM ARCHITECTURE VISION

1. **80%+ ISystem coverage** — All per-frame systems should be ISystem (struct). SystemBase only for bootstrap/composition.
2. **Zero managed IComponentData** — All components unmanaged structs. Use BlobAssetReference for asset refs, static registries for managed object refs.
3. **80%+ Burst coverage** — Every ISystem touching only unmanaged data should be `[BurstCompile]`.
4. **Job-parallel everything** — `.ScheduleParallel()` for all data-parallel work. `.Run()` only for UI/camera/input.
5. **Subscene/Entity baking** — Use Subscenes for the Match scene for better baking control and streaming readiness.
6. **Addressables** — No Addressables detected. Adopt for asset management to reduce memory pressure on mobile.
7. **Full test coverage** — Target 80%+ system test coverage. Current: 19%.

---

## VERIFICATION

**Unity batch mode compilation:**
```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath /Users/farhad/Projects/WarlineCapture-Clone \
  -quit \
  -logFile /tmp/compile-check.txt
```

**Result:** Exit code 0. All 15 game assemblies + 2 test assemblies compiled with zero CS errors. Script compilation time: 2.586s.

Compiled assemblies (verified):
```
Game.Authoring.dll ✅
Game.Catalog.Contracts.dll ✅
Game.Components.dll ✅
Game.Composition.dll ✅
Game.Configs.dll ✅
Game.Editor.dll ✅
Game.Rendering.Contracts.dll ✅
Game.Rendering.dll ✅
Game.Runtime.dll ✅
Game.Tests.Editor.dll ✅
Game.Tests.PlayMode.dll ✅
Game.UI.Contracts.dll ✅
Game.UI.Runtime.dll ✅
Game.UI.Shell.Contracts.Ecs.dll ✅
Game.UI.Shell.Ecs.dll ✅
```

---

**Audit performed:** 2026-06-18  
**Methodology:** Static analysis via grep across 723 C# files, Unity batch mode compilation, assembly dependency graph mapping, test coverage mapping by system name matching