# Dense City Virtualized Render Proxy Android 60 FPS Implementation Tracker

Date: 2026-07-28
Status: Design amendment approved for candidate implementation; baseline evidence accepted; implementation not started
Parent tracker: `dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`
Related contracts: `performance_regression_contract.md`, `gameplay_solid_ecs_contract.md`, `operation_map_runtime_ownership_chain.md`, `operation_map_scene_split_rollback_recipe.md`

## 1. Objective

Raise the dense-city candidate on Xiaomi `24090RA29G` from the measured `15.7-16.3 FPS` to a sustained 60-FPS-class Android result without reducing gameplay ownership, deleting accepted city placements, loading authoring GameObjects, or partitioning simulation state by camera.

The current candidate contains `82,797` baked render rows. Of those, `68,722` rows come from repeated generated presentation entries using only `377` repeated presentation signatures and `57` repeated prefab sources. Shared meshes and materials already prevent geometry/material duplication, but every placement still exists as a runtime render entity. The corrected Android run is CPU-main-bound at approximately `56.7-59.8 ms`, while GPU time is approximately `15.6-16.4 ms`, CPU render-thread time is approximately `1.8-2.3 ms`, and instrumented gameplay update time is approximately `0.4 ms`.

This tracker changes the representation of repeated permanent presentation:

```text
Editor authoring and generation
  -> preserve every accepted/generated stable placement and gameplay owner
  -> extract deterministic shared render prototypes and renderer-part recipes
  -> bake immutable placement, transform, color, bounds, state, and spatial-cell data
  -> bake a fixed-capacity set of generic ECS render-proxy slots
  -> remove the virtualized source render rows from player EntityScene output

Runtime
  -> load the existing thin binding scene and one map EntityScene once
  -> keep all simulation/gameplay entities and the immutable placement database resident
  -> keep one preallocated render-proxy slot set resident
  -> use the existing ECS camera snapshot to select nearby spatial cells
  -> use Burst jobs to retain/release/rebind proxy slots
  -> enable only the required render slots before Entities Graphics consumes them
  -> perform no GameObject instantiation, no scene streaming, and no steady-state structural changes
```

The target is not runtime procedural city generation. The complete city remains deterministic editor-baked data. Runtime only virtualizes which baked visual records are materialized as render entities.

## 2. Why This Amendment Is Authorized

The parent tracker originally selected whole-map render-entity residency and explicitly deferred render-only partitioning until Android evidence proved that representation unacceptable. That evidence now exists:

- exact APK: `555,817,853` bytes, SHA-256 `dec15d022c10eda2f3e3e4cb397f37871fac69656af5d7cdccd77741b32ae079`;
- exact device: Xiaomi `24090RA29G`, Android 16/API 36, ARM64;
- dense candidate EntityScene: `c00140f2e94a04c3084c8dcb0c18cbd0`;
- stable FPS: `15.7-16.3`;
- stable frame time: `61.5-63.7 ms`;
- typical CPU main: `56.7-59.8 ms`;
- GPU: `15.6-16.4 ms`;
- CPU render thread: `1.8-2.3 ms`;
- gameplay instrumentation: approximately `0.4 ms`;
- render rows in the packed candidate: `82,797`;
- repeated generated render rows: `68,722`;
- shared render-mesh arrays: `1`;
- repeated presentation signatures: `377`;
- repeated prefab sources: `57`.

The Android result proves that sharing mesh/material assets and relying only on Entities Graphics frustum culling is insufficient for this map. This amendment supersedes the parent's no-pooling/whole-render-residency clauses only for a candidate whose definition explicitly selects virtualized render residency and passes every gate in this tracker.

It does not supersede these parent decisions:

- all gameplay/simulation entities remain resident for the loaded map lifetime;
- no existing/generated authoring GameObject hierarchy ships or loads at runtime;
- no runtime city generation;
- no second map loader, static map-visual streamer, manager, service, controller, or mutable static registry;
- no production cutover before candidate Android acceptance;
- no mutation or deletion of the frozen static rollback package;
- no weakening of collider, ownership, deterministic-output, matrix/bounds parity, readiness, or teardown gates.

## 3. Success Definition

The feature is complete only when the exact candidate revision passes all of the following on the target Android device:

- average FPS `>= 58` after warmup over each representative 120-second route;
- 10th-percentile FPS `>= 55`;
- average frame time `<= 17.2 ms`;
- p95 frame time `<= 20 ms`;
- p99 frame time `< 25 ms`;
- CPU-main average `<= 12 ms` and p95 `<= 16 ms`;
- GPU average `<= 16 ms` and p95 `<= 18 ms`;
- steady-state operation-map managed allocation `0 B/frame`;
- render-virtualization overflow count exactly `0`;
- render-virtualization rebuild count changes only when the camera envelope or visual state requires it;
- no synchronous job completion attributed to the virtualization systems during steady camera traversal;
- no visible holes, stale proxy state, duplicate visuals, camera-edge popping, destroyed/intact leakage, or material/color leakage;
- all simulation entities remain resident and gameplay-equivalent;
- no map-visual scene load/unload occurs during camera travel;
- offline load, two-cycle load/unload, failure/reset/retry, and package ownership remain accepted;
- a final 10-minute thermal route remains within the tracked Android product budget from `performance_regression_contract.md`.

The 60-FPS goal is stronger than the existing product p95 budget. A result that passes the older `<25 ms` high-end p95 gate but fails the targets above does not complete this tracker.

Virtualization primarily addresses CPU-main work. The current GPU time is already close to the 60 FPS boundary, so final acceptance may also require the existing parent-tracker LOD, shadow, transparency, and small-detail optimizations. Those changes require separate evidence and may not be hidden inside a proxy-pool commit.

## 4. Non-Negotiable Architecture Decisions

### 4.1 Simulation And Presentation Ownership

- `OperationMapBuildingComponent`, health, faction, grid occupancy, navigation blockers, targeting identity, selection identity, production state, and destroyed state remain on stable gameplay entities.
- Render-proxy slots never become gameplay targets or authoritative owners.
- Minimap, navigation, AI, placement, targeting, runway, helipad, and scenario-anchor logic must read canonical ECS/map data, never proxy presence.
- A proxy binding is disposable presentation state. Losing a binding must not lose gameplay state.
- The complete immutable render-placement database remains resident until map teardown.
- The fixed proxy-slot entities load once with the map EntityScene and unload once with it.

### 4.2 No Runtime Instantiation Loop

- Do not instantiate/destroy an entity when the camera moves.
- Do not add/remove `RenderMeshArray`, `RenderFilterSettings`, `MaterialMeshInfo`, `LocalToWorld`, `RenderBounds`, or color components during steady-state traversal.
- Use pre-baked slot entities and enable/disable rendering through the enableable `MaterialMeshInfo` component.
- Rebind slots by writing existing unmanaged components.
- Do not use `Disabled` on gameplay owners.
- Do not add/remove `DisableRendering` every frame.
- Do not create a GameObject pool.

### 4.3 No New Streaming Owner

- Keep `OperationMapPresentationKind.EntityScene`.
- Add a render-residency mode beneath EntityScene presentation; do not add another presentation kind or map loader.
- Camera travel changes proxy assignments only. It cannot issue `SceneSystem.LoadSceneAsync`, Addressables operations, or static-presentation requests.
- The source scene, map EntityScene, metadata, and thin binding keep the existing load/readiness/failure/teardown owner.

### 4.4 Candidate-Only Until Accepted

- `OperationMapRenderResidencyMode.ResidentEntities` is the default.
- `OperationMapRenderResidencyMode.VirtualizedProxyPool` is fail-closed and valid only with a matching database schema, pool layout, candidate definition, and packed EntityScene contract.
- Production remains `StaticSceneChunks` until the parent tracker authorizes the separate production cutover.
- The accepted dense candidate and frozen rollback assets must be checkpointed by the existing transactional owner before any regeneration.

## 5. Eligibility Policy

Virtualize in this order:

1. repeated generated `Vegetation` and `Prop` presentation;
2. repeated generated noninteractive building attachments;
3. repeated generated `Infrastructure` that has no gameplay, animation, light, particle, or unique material-property owner;
4. repeated intact/destroyed building visual hierarchies after state synchronization passes;
5. repeated accepted-map render-only presentation after dense generated content is accepted;
6. named unique content only when a separate report proves benefit and parity.

Keep resident initially:

- vehicles and animated unit presentation;
- particles, trails, line renderers, skinned meshes, and GPU-animation owners;
- runtime lights, reflection probes, decals, audio, Animator, scripts, or mutable material-property owners;
- unique terrain, water, horizon, canal, mountain, runway, and bridge content until separately classified;
- selection/placement/order markers;
- any unresolved mixed gameplay/presentation owner;
- any renderer whose material/submesh/filter policy cannot be represented by the closed prototype schema.

Every exclusion requires a stable reason code in the virtualization report. Names, hierarchy guesses, renderer shape, or “looks static” are not valid classification.

## 6. Runtime Data Contract

### 6.1 Explicit Configuration

Add:

```text
Assets/Game/Scripts/Configs/OperationMapRenderResidencyMode.cs
```

with the closed values:

```csharp
public enum OperationMapRenderResidencyMode : byte
{
    ResidentEntities = 0,
    VirtualizedProxyPool = 1
}
```

Extend `OperationMapDefinition` with a serialized render-residency mode. Validation rules:

- `StaticSceneChunks` requires `ResidentEntities`;
- `EntityScene + ResidentEntities` retains the current behavior;
- `EntityScene + VirtualizedProxyPool` requires the virtualization database authoring reference in the SubScene, exact matching operation-map id/schema/content hash, at least one pool bucket, zero capacity deficit, and zero virtualized source render rows in packed output;
- unknown enum values fail;
- production cutover and candidate package builders must report the mode explicitly.

Do not use scripting defines, environment variables, platform checks, or map-id special cases as the ownership switch.

### 6.2 Blob Schema

Add unmanaged blob/component definitions in:

```text
Assets/Game/Scripts/Components/OperationMapRenderVirtualizationComponents.cs
```

The first accepted schema must contain:

```csharp
public struct OperationMapRenderDatabaseBlob
{
    public FixedString64Bytes OperationMapId;
    public FixedString128Bytes ContentHash;
    public int SchemaVersion;
    public float CellSize;
    public float3 GridOrigin;
    public int2 GridDimensions;
    public BlobArray<OperationMapRenderPrototypeBlob> Prototypes;
    public BlobArray<OperationMapRenderPrototypePartBlob> Parts;
    public BlobArray<OperationMapRenderPlacementBlob> Placements;
    public BlobArray<OperationMapRenderCellBlob> Cells;
    public BlobArray<int> CellPlacementIndices;
    public BlobArray<OperationMapRenderPoolBucketBlob> PoolBuckets;
}
```

Required logical fields:

| Record | Required fields |
|---|---|
| Prototype | collision-checked `ulong2` content identity; first part; part count; combined local bounds; semantic category; eligibility flags |
| Prototype part | renderer-path hash; mesh-array index; material-array index; submesh; local-to-placement matrix; local bounds; linear base color; fixed render-policy bucket; LOD/shadow flags |
| Placement | collision-checked `ulong2` stable identity hash; prototype index; exact world matrix; cell index; state-owner index or `-1`; required visual state; priority; semantic category |
| Cell | deterministic integer coordinate; world AABB; first placement-index entry; placement-index count |
| Pool bucket | closed render-filter policy; first slot; capacity; computed peak required count; headroom; report identity |

Rules:

- Keep exact `float4x4` placement and part matrices in schema 1. Quantization requires a later matrix/bounds parity report and schema bump.
- Compute proxy world matrices as `placementWorld * partLocalToPlacement`.
- Store local renderer bounds; do not rediscover bounds from runtime mesh objects.
- Store linear base color explicitly. Do not instantiate a material to carry per-instance color.
- `MaterialMeshInfo` selects a mesh/material/submesh from the one shared sorted `RenderMeshArray`.
- A prototype fingerprint includes renderer hierarchy path, mesh GUID/local id, material GUID/local id, submesh, local matrix, local bounds, base color, render-filter policy, shadow flags, and LOD policy.
- A placement identity hash derives from the existing stable identity using a documented SHA-256-to-`ulong2` projection. The builder must retain the full stable id in the editor report and reject any 128-bit collision.
- A renderer logical-row identity derives from placement identity plus prototype renderer-path identity. The editor report retains the full source identity/path for parity.
- Arrays are sorted deterministically; timestamps, absolute paths, instance ids, entity indices, and editor-session data are forbidden hash inputs.

### 6.3 Compound Buildings, Roofs, Walls, And Interiors

A shared building prototype is a part recipe, not a prefab instantiated at runtime.

```text
Building placement
  -> intact prototype
       -> wall-shell part
       -> roof part
       -> window/sign part
       -> interior/tent-content parts
  -> destroyed prototype
       -> rubble-shell part
       -> destroyed attachment parts
```

Related requirements:

- intact and destroyed visual records use one deterministic state-owner index belonging to the gameplay building;
- roof equipment, interiors, goods, signs, awnings, lamps, furniture, and tent contents inherit exactly one intact/destroyed state requirement;
- no attached part becomes an independent always-visible placement;
- independently damageable/selectable parts require their own canonical gameplay owner and state index;
- state changes update the canonical state array even while the building is off-camera;
- a newly assigned proxy must read current canonical state before becoming render-enabled;
- recycling must reset transform, mesh/material selection, color, bounds, binding identity, LOD state, and enable state.

### 6.4 Runtime Components

Add these unmanaged components/buffers with exact single ownership:

| Type | Owner | Purpose |
|---|---|---|
| `OperationMapRenderDatabaseComponent` | one map EntityScene entity | Blob reference, schema, map generation, content hash |
| `OperationMapRenderProxySlotComponent` | every fixed slot entity | Stable slot index, fixed pool-bucket index, current placement/part binding, assignment generation |
| `OperationMapRenderVirtualizationStateComponent` | one runtime state entity | Initialized flag, initial-view-applied flag, active envelope, camera signature, active/dirty/overflow counters |
| `OperationMapVirtualizedBuildingPresentationComponent` | canonical gameplay building | Dense state-owner index; replaces render-entity root ownership in virtualized mode |
| `OperationMapRenderStateChangeComponent` | one map-owned bounded buffer | Rare authoritative intact/destroyed state transitions |
| `OperationMapRenderVirtualizationMetricsComponent` | one map-owned diagnostics entity | Capacity, enabled slots, retained/released/rebound counts, overflow, rebuild reason |

Do not add managed collections or UnityEngine object references to these components.

### 6.5 Fixed Render-Policy Buckets

`RenderFilterSettings` is shared data and must not be changed when a slot is rebound. The bake therefore creates a fixed slot range for each accepted policy bucket. The initial closed buckets are:

- opaque, shadows on;
- opaque, shadows off;
- alpha-clipped, shadows on;
- alpha-clipped, shadows off;
- transparent, shadows off;
- always-resident exception.

Layer, rendering-layer mask, motion-vector mode, shadow casting, receive-shadows behavior, and static-shadow policy are part of the bucket identity. Unknown combinations fail candidate bake or remain resident with a named exclusion.

Transparent and alpha-clipped content do not silently fall into the opaque bucket.

## 7. Spatial Index And Capacity Contract

### 7.1 Cells

- Start with deterministic `32 m` X/Z cells, matching the prior static-presentation diagnostic scale.
- Derive coordinates from the accepted operation-map origin using floor division.
- Assign a placement to every cell intersected by its combined transformed bounds, not only its pivot cell.
- Deduplicate placement indices inside each cell.
- Sort cells by `(z, x)` and placement indices by stable identity.
- Keep large multi-cell placements resident initially unless measured evidence approves virtualized multi-cell ownership.

### 7.2 Camera Envelope

Reuse `RuntimeCameraSnapshotComponent`. Do not read `Camera.main`, call scene searches, or access a `Camera` from a Burst system.

The active envelope is:

- the camera frustum projected against the operation-map presentation height range;
- expanded by one full visible safety cell;
- prefetched by one additional guard cell;
- clamped to operation-map camera bounds.

Rebuild when:

- the required frustum footprint is no longer contained by the current guard envelope;
- rotation/projection/orthographic mode changes enough to expose a new cell;
- camera teleport/tactical-follow change invalidates the envelope;
- the map generation changes;
- a currently bound building state changes;
- a test explicitly forces a rebuild.

Do not rebuild merely because `Time.frameCount` advanced.

### 7.3 Capacity

The editor capacity validator must sweep:

- every canonical operation-map camera pose;
- normal, build, fullscreen-map, maximum-zoom, and tactical-follow projection limits;
- every spatial cell as an envelope center;
- all accepted yaw/pitch/projection extrema;
- safety and guard margins.

For each render-policy bucket:

```text
capacity = ceil(maximumSimultaneouslyRequiredPartRows * 1.20)
```

Capacity must be deterministic and checked into the report. A manual capacity constant without the sweep report is forbidden.

Provisional design limits:

- packed runtime entities carrying enabled/disabled map `MaterialMeshInfo`: `<= 24,000`;
- simultaneously enabled map proxy rows in representative gameplay views: `<= 8,000`;
- headroom in every bucket: `>= 20%`;
- runtime overflow: exactly `0`.

Changing a limit requires a report with before/after Android CPU, GPU, memory, visual, and overflow evidence.

### 7.4 Deterministic Overflow Safety

Candidate validation must make overflow unreachable for accepted routes. Runtime still needs a bounded safety policy:

1. retain gameplay-building presentation;
2. retain selected/targeted or damage-transition presentation;
3. retain infrastructure required for spatial readability;
4. retain nearest opaque presentation;
5. retain vegetation/props last.

Within a priority, sort by distance then stable identity. Increment an overflow counter and emit one gated development diagnostic. Do not allocate, throw every frame, or spam logs. Any overflow rejects Android acceptance and production cutover.

## 8. Bake And Package Pipeline

### 8.1 Authoring Asset

Add:

```text
Assets/Game/Scripts/Configs/OperationMapRenderDatabaseBakeConfig.cs
Assets/Game/Scripts/Authorings/OperationMapVirtualizedPresentationAuthoring.cs
```

`OperationMapRenderDatabaseBakeConfig` is a generated ScriptableObject and deterministic bake input containing sorted Unity asset references plus serialized logical records. It is not queried at player runtime.

Candidate output path:

```text
Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/
```

The exact file names and ownership must be added to `dense_city_generated_output_ownership.md` before first mutation.

### 8.2 Editor Builder

Add:

```text
Assets/Game/Scripts/Editor/OperationMapRenderDatabaseBuilder.cs
Assets/Game/Scripts/Editor/OperationMapRenderDatabaseValidator.cs
Assets/Game/Scripts/Editor/OperationMapRenderVirtualizationReportBuilder.cs
```

The builder must:

1. consume accepted migration/generation stable records;
2. resolve renderer ownership without names or geometry guesses;
3. classify eligible/excluded renderer rows;
4. group eligible rows into deterministic prototypes;
5. preserve renderer-local part matrices and local bounds;
6. produce logical placements and state-owner relationships;
7. create the deterministic cell index;
8. compute policy-bucket capacity;
9. write the bake config transactionally;
10. emit a machine-readable report before candidate replacement;
11. run twice and require identical logical hash, counts, ordering, and serialized bytes.

### 8.3 Baker And Pool Slots

Add:

```text
Assets/Game/Scripts/Authorings/OperationMapVirtualizedPresentationAuthoring.cs
Assets/Game/Scripts/Rendering/Baking/OperationMapRenderVirtualizationBakingSystem.cs
```

The authoring baker must:

- register the blob using the supported baking blob-asset ownership API;
- build the sorted shared mesh/material arrays;
- create the database component entity;
- create exactly the reported number of slot entities per bucket;
- apply fixed `RenderFilterSettings` and the shared `RenderMeshArray`;
- add `MaterialMeshInfo`, `LocalToWorld`, `RenderBounds`, `URPMaterialPropertyBaseColor`, and `OperationMapRenderProxySlotComponent`;
- initialize `MaterialMeshInfo` disabled;
- avoid `Parent`/`Child`/`LocalTransform` on generic leaf slots;
- avoid one proxy root hierarchy per logical placement.

The post-baking system must:

- match every eligible converted source render row to exactly one logical database row;
- mark eligible source-only render entities `BakingOnlyEntity` or strip their rendering ownership without deleting canonical gameplay entities;
- replace virtualized building render-root references with `OperationMapVirtualizedBuildingPresentationComponent`;
- retain excluded render rows unchanged;
- reject any eligible packed source render row that survives;
- reject a source row removed without a matching logical record;
- reject a proxy slot missing required render components or using an unreported policy bucket.

### 8.4 Package Ownership

The built package must contain:

- the existing thin runtime binding;
- the map EntityScene with simulation entities, immutable render database, fixed proxy slots, and named resident exceptions;
- the accepted map surface/minimap/metadata;
- transitive mesh/material assets required by the shared `RenderMeshArray`;
- zero source/candidate authoring hierarchy;
- zero explicit runtime dependency on `OperationMapRenderDatabaseBakeConfig` when the baked blob already owns the data;
- zero legacy static-presentation ownership in the candidate.

The runtime-content report must add resident-render rows, virtualized logical rows, prototypes, parts, cells, policy buckets, total slots, per-bucket capacity, packed database bytes, source rows removed, excluded rows by reason, and source-hierarchy counts.

## 9. Runtime Scheduling Contract

### 9.1 Systems And Groups

Add real ECS systems in `Game.Rendering`:

```text
Assets/Game/Scripts/Rendering/Systems/OperationMapRenderVirtualizationInitializationSystem.cs
Assets/Game/Scripts/Rendering/Systems/OperationMapRenderCellSelectionSystem.cs
Assets/Game/Scripts/Rendering/Systems/OperationMapRenderProxyAssignmentSystem.cs
Assets/Game/Scripts/Rendering/Systems/OperationMapRenderProxyApplySystem.cs
Assets/Game/Scripts/Rendering/Systems/OperationMapRenderStateSyncSystem.cs
Assets/Game/Scripts/Rendering/Systems/OperationMapRenderVirtualizationMetricsSystem.cs
```

Ordering:

```text
RuntimeCameraReferenceSystem snapshot publication
  -> OperationMapRenderStateSyncSystem
  -> OperationMapRenderCellSelectionSystem
  -> OperationMapRenderProxyAssignmentSystem
  -> OperationMapRenderProxyApplySystem
  -> EntitiesGraphicsSystem
```

Use explicit update-group/order attributes. Do not rely on file order.

### 9.2 Initialization

Initialization runs once per map generation and:

- validates one database and one state owner;
- validates operation-map id, schema, content hash, residency mode, slot counts, and slot indices;
- allocates system-owned persistent native state with exact bounded capacities;
- builds slot-to-binding and logical-row-to-slot maps;
- initializes canonical building state once through a Burst job;
- performs the first envelope selection and assignment;
- sets `OperationMapRenderVirtualizationStateComponent.InitialViewApplied` only after required first-view slots are applied;
- leaves `OperationMapReadinessFlags`, acceptance/failure codes, and final readiness publication exclusively owned by the existing `Game.Composition` readiness path, which validates the mode-specific virtualization state;
- records integration timing separately from steady-state frame timing.

Dispose persistent native containers when map generation changes, unload begins, or the system is destroyed. Complete dependencies only at these lifecycle boundaries.

### 9.3 Cell Selection Jobs

Use Burst `IJob`/`IJobFor` work over the precomputed cell index:

- compute required cells from the immutable camera snapshot and active guard envelope;
- gather placement indices from only required cells;
- deduplicate multi-cell placements with a bounded native bitset/hash set;
- filter by current building visual state;
- expand selected placements to logical renderer-part keys;
- sort deterministically only when assignment order requires it.

Do not scan all `82,797` logical rows per frame.

### 9.4 Assignment Job

Maintain:

- active logical-row key to slot index;
- slot index to logical-row key;
- per-bucket deterministic free-slot stacks;
- assignment generation;
- dirty slot bitset;
- active placement/cell bitsets.

On rebuild:

1. retain existing assignments still required;
2. release assignments no longer required;
3. assign newly required rows from the matching bucket;
4. generate one fixed-size slot command per changed slot;
5. leave unchanged slots untouched;
6. publish bounded counters and overflow.

Run assignment in a Burst job. It may be single-threaded inside the job when deterministic hash/free-list mutation requires it; the point is to remove that work from the main thread. Do not run the algorithm as a managed loop.

### 9.5 Parallel Apply Job

Apply commands with `IJobChunk` or an equivalent parallel job where every job iteration owns one unique slot:

- write `LocalToWorld`;
- write `RenderBounds`;
- write `MaterialMeshInfo`;
- write `URPMaterialPropertyBaseColor`;
- write proxy binding/generation;
- set the enableable `MaterialMeshInfo` state;
- fully reset released slots.

The slot entity must not carry `Parent`, `Child`, or `LocalTransform`. Flattened leaf `LocalToWorld` avoids transform-hierarchy work for proxy presentation.

Set `state.Dependency` and let Entities Graphics consume the dependency. A steady-state path may not call `Complete()`, `CompleteDependency()`, `EntityManager.CompleteAllTrackedJobs()`, or synchronously read job output.

### 9.6 Stable Camera Fast Path

When the current required envelope is contained by the guard envelope and there are no visual-state changes:

- do not schedule selection/assignment/apply jobs;
- do not allocate temporary native containers;
- do not query all placement or render entities;
- do not write slot components;
- only update bounded diagnostics at its throttled cadence.

This no-op path is a required performance test.

## 10. Building State And Gameplay Integration

### 10.1 Canonical State

`OperationMapBuildingDestroyedComponent` remains authoritative. Add a deterministic dense state-owner index to each virtualized gameplay building.

Adapt `OperationMapBuildingDestructionSystem`:

- resident mode keeps the existing render-root scale behavior;
- virtualized mode changes canonical destroyed state and appends one `OperationMapRenderStateChangeComponent` buffer record;
- it does not search proxy bindings or write render transforms;
- it does not instantiate a destroyed visual;
- it remains Burst compiled.

### 10.2 State Synchronization

`OperationMapRenderStateSyncSystem`:

- initializes the state array once from all virtualized buildings;
- consumes the bounded state-change buffer;
- marks only affected logical placement rows/cells dirty;
- releases intact assignments and assigns destroyed assignments when currently visible;
- retains off-camera state without a render slot;
- clears consumed events through the accepted ECS dependency/ECB path.

### 10.3 Gameplay Independence Tests

Required cases:

- destroy a visible generated house and verify complete intact-to-destroyed transition;
- destroy an off-camera house, travel to it, and verify only destroyed parts appear;
- destroy a building, recycle all its former slots through unrelated buildings, return, and verify no stale intact/color/material state;
- select/target an off-camera building through canonical gameplay data without a proxy dependency;
- minimap markers remain present when their world visuals have no assigned proxy;
- navigation/blocking does not change as proxy assignments change;
- camera traversal changes zero gameplay entity counts and zero gameplay component values.

## 11. Diagnostics And Performance Instrumentation

Add profiler markers for:

- `OperationMapRenderVirtualization.Initialize`;
- `OperationMapRenderVirtualization.SelectCells`;
- `OperationMapRenderVirtualization.AssignSlots`;
- `OperationMapRenderVirtualization.ApplySlots`;
- `OperationMapRenderVirtualization.SyncState`.

Structured metrics:

- logical placements/parts and resident exceptions;
- total slot capacity and per-bucket capacity;
- enabled/disabled slots;
- retained/released/new assignments;
- active cells and placements;
- rebuild reason;
- overflow count and highest deficit;
- initialization/integration duration;
- average/p95/p99/max CPU time for each named system/job boundary;
- managed allocation after readiness;
- Entities Graphics batches and render entity counts;
- CPU main/render thread and GPU frame distributions.

Diagnostics must be gated before string construction and sampled no more often than the existing performance-diagnostics cadence. No direct per-frame `Debug.Log*`.

## 12. Validation Contract

### 12.1 Pure And EditMode Validation

Required new tests:

```text
Assets/Tests/Editor/OperationMapRenderDatabaseBuilderTests.cs
Assets/Tests/Editor/OperationMapRenderDatabaseValidatorTests.cs
Assets/Tests/Editor/OperationMapRenderVirtualizationBakingTests.cs
Assets/Tests/Editor/OperationMapRenderVirtualizationCapacityTests.cs
Assets/Tests/Editor/OperationMapRenderVirtualizationDeterminismTests.cs
Assets/Tests/Editor/OperationMapRenderResidencyModeTests.cs
```

Cover:

- stable sorting and hashing;
- collision rejection;
- prototype deduplication;
- compound part matrices;
- nonuniform/negative scale parity;
- material/submesh/color preservation;
- bounds and multi-cell membership;
- policy-bucket classification;
- capacity sweep and headroom;
- source-row-to-logical-row bijection;
- slot composition and disabled initial state;
- unknown schema/mode/bucket rejection;
- two-run byte equality;
- accepted/frozen asset isolation.

### 12.2 Runtime And PlayMode Validation

Required new tests:

```text
Assets/Tests/PlayMode/OperationMapRenderVirtualizationPlayModeTests.cs
Assets/Tests/PlayMode/OperationMapRenderVirtualizationPackedPlayModeTests.cs
```

Cover:

- first-view readiness;
- stable-camera no-op;
- slow pan, fast pan, zoom, rotation, and teleport;
- cell-edge guard band;
- retention before reassignment;
- deterministic slot reuse;
- zero overflow;
- visible logical-row matrix/bounds/color parity;
- state transitions and off-camera destruction;
- no map-visual scene requests during travel;
- no runtime GameObject map visuals;
- failure/reset/retry;
- two load/unload cycles;
- all persistent native containers disposed;
- zero virtualized source render rows after packed load;
- unchanged simulation entity counts during camera travel.

### 12.3 Parity Amendment

The existing parity contract currently expects every permanent render row to exist as a runtime entity before culling. For `VirtualizedProxyPool`, replace that boundary with two exact gates:

1. logical parity: every accepted source renderer maps to exactly one resident exception or one immutable database logical row with matching world matrix, local bounds, mesh/material/submesh, color, state owner, and semantic identity;
2. materialized parity: for every canonical camera, every required logical row maps to exactly one enabled proxy whose matrix/bounds/material/color match the logical row, and no nonrequired row is enabled outside the guard policy.

Off-camera absence of a proxy is valid only after logical parity passes. Missing logical data is never culling.

### 12.4 Android Routes

Capture the same exact revision and package across:

- fixed initial gameplay pose;
- dense downtown slow pan;
- maximum-speed pan across district boundaries;
- zoom minimum to maximum and back;
- camera rotation/tactical-follow where supported;
- selected building plus destruction;
- minimap/fullscreen-map transition;
- Menu -> Match -> Menu -> Match -> Menu;
- 120-second steady route for iteration;
- 10-minute thermal route for final acceptance.

Record raw profiler data before and after. Acceptance metrics must come from diagnostics-disabled release-equivalent runs; profiler-enabled captures are attribution evidence only.

## 13. File And Type Ownership Plan

No new assembly definition is planned.

| Assembly | Files/types |
|---|---|
| `Game.Components` | blob schema, proxy slot/state/metrics, building presentation state index, state-change buffer |
| `Game.Configs` | `OperationMapRenderResidencyMode`; `OperationMapRenderDatabaseBakeConfig`; `OperationMapDefinition` field/validation |
| `Game.Authoring` | virtualized-presentation root authoring/baker; add only the required `Unity.Entities.Graphics` reference if compilation proves it necessary |
| `Game.Rendering` | baking cleanup system; initialization, cell selection, assignment, apply, state sync, metrics systems |
| `Game.Editor` | deterministic database builder/validator, capacity sweep, report, Bake All integration |
| `Game.Tests.Editor` | schema, builder, capacity, determinism, bake/package tests |
| `Game.Tests.PlayMode` | live and packed lifecycle/parity/performance behavior |

Naming rules:

- only real ECS implementations use the `System` suffix;
- editor analyzers use `Builder`, `Validator`, or `ReportBuilder`;
- no `Manager`, `Service`, `Controller`, `Facade`, `Provider`, or mutable static runtime registry;
- helper extraction is allowed only when it owns one cohesive algorithm and does not hide lifecycle state.

## 14. Implementation Phases And Checklist

Difficulty labels:

- `[TERRA]`: bounded additive schema, pure algorithm, report, test, or documentation work with no runtime ownership switch;
- `[SOL]`: bake ownership mutation, hot-path scheduling, gameplay presentation migration, packed lifecycle, acceptance policy, or production cutover.

Terra must stop and request Sol when the next dependency-ready item is marked `[SOL]`.

### Phase 0: Evidence And Amendment

- [x] `VRP-000 [TERRA]` Record the exact corrected-APK Android FPS/CPU/GPU/memory evidence. Depends on: none.
- [x] `VRP-001 [SOL]` Approve candidate-only render virtualization under the parent's measured-failure escape clause. Depends on: `VRP-000`.
- [ ] `VRP-002 [SOL]` Recover one raw Android Unity profile and name the dominant PlayerLoop child owners before representation mutation. Depends on: `VRP-000`.
- [ ] `VRP-003 [TERRA]` Emit exact eligible/excluded counts by semantic category, prototype signature, renderer type, policy bucket, and gameplay ownership. Depends on: `VRP-000`.
- [ ] `VRP-004 [TERRA]` Add a fixed-camera/route baseline report that reconciles current `82,797` render rows and Android measurements at one exact revision. Depends on: `VRP-002`, `VRP-003`.

**Exit:** The before state and dominant CPU owner are reproducible; eligibility is not guessed.

### Phase 1: Additive Schema And Pure Contracts

- [x] `VRP-010 [TERRA]` Add `OperationMapRenderResidencyMode` with default resident behavior and closed validation tests. Depends on: `VRP-001`.
- [ ] `VRP-011 [TERRA]` Add unmanaged blob/component schema with field-level validation and no runtime use. Depends on: `VRP-010`.
- [ ] `VRP-012 [TERRA]` Add stable 128-bit identity projection, collision detection, and deterministic sorting tests. Depends on: `VRP-011`.
- [ ] `VRP-013 [TERRA]` Add pure prototype fingerprinting for mesh/material/submesh/path/matrix/bounds/color/filter/LOD inputs. Depends on: `VRP-011`.
- [ ] `VRP-014 [TERRA]` Add pure cell assignment and multi-cell deduplication algorithms with boundary tests. Depends on: `VRP-011`.
- [ ] `VRP-015 [TERRA]` Add pure policy-bucket classification with fail-closed unknown combinations. Depends on: `VRP-011`.
- [ ] `VRP-016 [TERRA]` Add deterministic camera-capacity sweep inputs/results and 20% headroom calculation tests. Depends on: `VRP-014`, `VRP-015`.
- [ ] `VRP-017 [TERRA]` Add schema/report serialization and reject missing/default/negative metrics. Depends on: `VRP-012` through `VRP-016`.

**Exit:** All new data algorithms compile and pass without changing candidate or production bake output.

### Phase 2: Deterministic Database Builder

- [ ] `VRP-020 [TERRA]` Add bake-config schema and generated-output ownership documentation; create no asset yet. Depends on: `VRP-017`.
- [ ] `VRP-021 [SOL]` Join accepted/generated stable owners to exact renderer rows and emit a source-row inventory. Depends on: `VRP-003`, `VRP-020`.
- [ ] `VRP-022 [SOL]` Build shared prototype/part recipes with exact compound child transforms and bounds. Depends on: `VRP-021`.
- [ ] `VRP-023 [SOL]` Build logical placements and intact/destroyed state-owner relationships. Depends on: `VRP-022`.
- [ ] `VRP-024 [TERRA]` Build the deterministic cell index from logical placement bounds. Depends on: `VRP-023`.
- [ ] `VRP-025 [TERRA]` Compute per-policy capacity and provisional entity/active-slot budgets. Depends on: `VRP-024`.
- [ ] `VRP-026 [SOL]` Write the candidate bake config transactionally and prove accepted/frozen/production isolation. Depends on: `VRP-025`.
- [ ] `VRP-027 [TERRA]` Run unchanged input twice and require identical record hash, ordering, counts, and serialized bytes. Depends on: `VRP-026`.
- [ ] `VRP-028 [SOL]` Expand the logical database and prove source matrix/bounds/mesh/material/submesh/color/state parity before any source render row is removed. Depends on: `VRP-027`.

**Exit:** A complete deterministic logical rendering database exists beside unchanged current render entities.

### Phase 3: Candidate Pool Baking

- [ ] `VRP-030 [TERRA]` Add root authoring/baker scaffolding that bakes only the database component; source rendering stays unchanged. Depends on: `VRP-028`.
- [ ] `VRP-031 [SOL]` Build one sorted shared `RenderMeshArray` and validate all logical mesh/material/submesh indices. Depends on: `VRP-030`.
- [ ] `VRP-032 [SOL]` Bake fixed leaf proxy slots per policy bucket with disabled `MaterialMeshInfo`. Depends on: `VRP-031`.
- [ ] `VRP-033 [TERRA]` Validate slot indices, bucket ranges, required components, absent hierarchy components, and disabled initial state. Depends on: `VRP-032`.
- [ ] `VRP-034 [SOL]` Mark only eligible source render rows baking-only/stripped while preserving canonical gameplay entities and named resident exceptions. Depends on: `VRP-033`.
- [ ] `VRP-035 [SOL]` Replace virtualized building render-root ownership with the state-index component only after exact source-to-logical parity. Depends on: `VRP-034`.
- [ ] `VRP-036 [SOL]` Extend readiness/package gates for residency mode, database, slots, zero eligible packed source rows, and exact resident exceptions. Depends on: `VRP-035`.
- [ ] `VRP-037 [TERRA]` Extend the packed-content report with database/slot/source-removal metrics and fail-closed defaults. Depends on: `VRP-036`.
- [ ] `VRP-038 [SOL]` Run direct bake and packed candidate validation twice with production cutover disabled. Depends on: `VRP-037`.

**Exit:** The candidate package contains compact logical data and fixed proxy slots instead of eligible permanent render rows, with no runtime assignment yet.

### Phase 4: Jobified Runtime Assignment

- [ ] `VRP-040 [SOL]` Make camera snapshot publication a single ordered frame boundary with a version/signature usable by Burst systems. Depends on: `VRP-038`.
- [ ] `VRP-041 [SOL]` Implement map-generation initialization and bounded persistent native state. Depends on: `VRP-040`.
- [ ] `VRP-042 [TERRA]` Implement/test the pure guard-envelope rebuild decision. Depends on: `VRP-040`.
- [ ] `VRP-043 [SOL]` Implement Burst cell selection/gather/dedup/state-filter jobs. Depends on: `VRP-041`, `VRP-042`.
- [ ] `VRP-044 [SOL]` Implement deterministic retain/release/assign logic inside a Burst job. Depends on: `VRP-043`.
- [ ] `VRP-045 [SOL]` Implement parallel slot apply with unique writes and enableable `MaterialMeshInfo`. Depends on: `VRP-044`.
- [ ] `VRP-046 [SOL]` Order apply before Entities Graphics without a hot-path synchronous completion. Depends on: `VRP-045`.
- [ ] `VRP-047 [TERRA]` Add stable-camera no-op, zero-allocation, no-component-write, and no-rebuild tests. Depends on: `VRP-046`.
- [ ] `VRP-048 [TERRA]` Add bounded metrics/profiler markers with gated formatting. Depends on: `VRP-046`.
- [ ] `VRP-049 [SOL]` Prove load/unload disposes all persistent native ownership and leaves no job using unloaded blob/entity data. Depends on: `VRP-047`, `VRP-048`.

**Exit:** Repeated render-only proxy presentation follows camera movement through jobs with zero steady-state structural changes.

### Phase 5: Render-Only Pilot

- [ ] `VRP-050 [TERRA]` Select the largest safe repeated `Vegetation`/`Prop` pilot from the inventory and freeze its exact stable identities/count. Depends on: `VRP-003`, `VRP-049`.
- [ ] `VRP-051 [SOL]` Enable virtualization only for that pilot in the candidate definition/output. Depends on: `VRP-050`.
- [ ] `VRP-052 [SOL]` Validate slow/fast pan, zoom, rotation, teleport, guard bands, deterministic reuse, and zero overflow in packed PlayMode. Depends on: `VRP-051`.
- [ ] `VRP-053 [SOL]` Capture Android before/after CPU main, render thread, GPU, FPS, memory, active slots, and visible parity on the same route. Depends on: `VRP-052`.
- [ ] `VRP-054 [SOL]` Accept expansion only if CPU-main p95 improves materially, visual/state gates pass, and no new dominant main-thread owner offsets the gain. Depends on: `VRP-053`.

**Exit:** Android evidence proves the representation change helps before broader ownership migration.

### Phase 6: Building And Attachment State

- [ ] `VRP-060 [SOL]` Add deterministic state-owner indices to virtualized gameplay buildings. Depends on: `VRP-054`.
- [ ] `VRP-061 [SOL]` Adapt `OperationMapBuildingDestructionSystem` for resident versus virtualized presentation without changing canonical destruction semantics. Depends on: `VRP-060`.
- [ ] `VRP-062 [SOL]` Add bounded state-change events and jobified state synchronization. Depends on: `VRP-061`.
- [ ] `VRP-063 [SOL]` Virtualize one complete building family including walls, roof, interiors, intact attachments, and destroyed recipe. Depends on: `VRP-062`.
- [ ] `VRP-064 [SOL]` Pass visible destruction, off-camera destruction, recycle/return, and state-leak tests. Depends on: `VRP-063`.
- [ ] `VRP-065 [SOL]` Prove selection, targeting, minimap, navigation, and blockers remain independent of proxy assignment. Depends on: `VRP-064`.
- [ ] `VRP-066 [SOL]` Expand to all eligible repeated generated building families with exact exclusion report. Depends on: `VRP-065`.
- [ ] `VRP-067 [SOL]` Capture Android destruction-transition frame cost and visual matrix for house/shop/tent/military variants. Depends on: `VRP-066`.

**Exit:** Complete building compound presentation recycles safely while gameplay state stays resident and authoritative.

### Phase 7: Coverage, LOD, And GPU Headroom

- [ ] `VRP-070 [SOL]` Expand to eligible generated infrastructure and noninteractive attachments. Depends on: `VRP-067`.
- [ ] `VRP-071 [SOL]` Classify alpha-clipped/transparent buckets and keep unsupported content resident by stable reason. Depends on: `VRP-070`.
- [ ] `VRP-072 [SOL]` Add/fix LOD, shadows, and small-detail policy only from Android GPU/visual evidence. Depends on: `VRP-071`.
- [ ] `VRP-073 [TERRA]` Regenerate exact resident/logical/prototype/slot/bucket/cell/package metrics. Depends on: `VRP-072`.
- [ ] `VRP-074 [SOL]` Tune cell/envelope/capacity only through before/after route evidence; retain 20% headroom and zero overflow. Depends on: `VRP-073`.
- [ ] `VRP-075 [SOL]` Require packed runtime map `MaterialMeshInfo` entities `<= 24,000` and representative enabled proxies `<= 8,000`, or amend with measured evidence. Depends on: `VRP-074`.

**Exit:** The complete candidate rendering representation is bounded for CPU and has enough GPU headroom for 60 FPS.

### Phase 8: Lifecycle, Determinism, And Parity

- [ ] `VRP-080 [SOL]` Pass direct logical parity for every source renderer and resident exception. Depends on: `VRP-075`.
- [ ] `VRP-081 [SOL]` Pass materialized parity for every canonical camera and state variant. Depends on: `VRP-080`.
- [ ] `VRP-082 [SOL]` Pass first readiness, failure cleanup, reset/retry, and two packed load/unload cycles. Depends on: `VRP-081`.
- [ ] `VRP-083 [SOL]` Prove camera travel issues zero map scene/Addressables/static-streamer operations. Depends on: `VRP-082`.
- [ ] `VRP-084 [SOL]` Prove camera travel changes zero simulation entity counts and gameplay state. Depends on: `VRP-082`.
- [ ] `VRP-085 [TERRA]` Prove two full Bake All runs produce identical virtualization logical hashes/counts/ordering/bytes. Depends on: `VRP-082`.
- [ ] `VRP-086 [SOL]` Pass five fixed-camera screenshot sets plus camera-edge traversal video/image evidence with no holes or stale state. Depends on: `VRP-083` through `VRP-085`.

**Exit:** The candidate is deterministic, visually equivalent when materialized, and lifecycle-safe.

### Phase 9: Android 60 FPS Acceptance

- [ ] `VRP-090 [SOL]` Build the exact clean candidate APK through the documented Unity wrapper and record revision/SHA-256/package ownership. Depends on: `VRP-086`.
- [ ] `VRP-091 [SOL]` Install and capture all representative 120-second routes with diagnostics disabled. Depends on: `VRP-090`.
- [ ] `VRP-092 [SOL]` Pass FPS/frame/CPU/GPU targets from Section 3 on every representative route. Depends on: `VRP-091`.
- [ ] `VRP-093 [SOL]` Pass zero allocation, zero overflow, bounded rebuild, and no synchronous-completion gates. Depends on: `VRP-091`.
- [ ] `VRP-094 [SOL]` Capture peak/retained/graphics memory and compare exact Phase 0/current-candidate baselines. Depends on: `VRP-091`.
- [ ] `VRP-095 [SOL]` Pass Android visible/off-camera destruction and proxy-recycling state tests. Depends on: `VRP-091`.
- [ ] `VRP-096 [SOL]` Pass offline Menu -> Match -> Menu -> Match -> Menu lifecycle with full unload. Depends on: `VRP-091`.
- [ ] `VRP-097 [SOL]` Pass the final 10-minute thermal route without dropping below accepted budgets. Depends on: `VRP-092` through `VRP-096`.

**Exit:** 60-FPS-class dense-city presentation is accepted on the real target Android device.

### Phase 10: Cutover And Closeout

- [ ] `VRP-100 [TERRA]` Update architecture/workflow/output-ownership documentation with exact accepted types, hashes, counts, and rollback steps. Depends on: `VRP-097`.
- [ ] `VRP-101 [SOL]` Update parent parity/residency clauses to accepted virtualized semantics and close superseded candidate-only assumptions. Depends on: `VRP-100`.
- [ ] `VRP-102 [SOL]` Authorize production `EntityScene + VirtualizedProxyPool` only after every parent production prerequisite also passes. Depends on: `VRP-101` and parent gates.
- [ ] `VRP-103 [SOL]` Validate production package, Android artifact, and rollback restore from the exact cutover revision. Depends on: `VRP-102`.
- [ ] `VRP-104 [SOL]` Retire frozen static production ownership only when the parent tracker separately authorizes deletion; otherwise retain it untouched. Depends on: `VRP-103`.

**Exit:** Production uses the accepted representation with a tested rollback path and complete evidence.

## 15. Required Validation Commands

Follow repository `AGENTS.md`.

Windows:

```powershell
$unityExe = & Tools/CI/ResolveUnityEditor.ps1

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe $unityExe `
  -ProjectPath (Get-Location).Path `
  -ExecuteMethod Game.Tests.Editor.OperationMapRenderVirtualizationValidation.RunFocusedValidation `
  -LogFile "$env:TEMP\warline-render-virtualization-focused.log" `
  -RequiredPassMarker "[OperationMapRenderVirtualizationValidation] result=Passed" `
  -TimeoutSeconds 900
```

Use `Tools/CI/InvokeUnity.ps1` for Unity Test Framework/PlayMode runs. Every run needs an explicit log, timeout, exact filter/marker, and nonzero/missing-marker failure handling. Confirm no active Unity process owns this project first. Do not terminate an existing Editor.

macOS:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/warline-render-virtualization-focused.log -- \
  -quit -executeMethod Game.Tests.Editor.OperationMapRenderVirtualizationValidation.RunFocusedValidation
```

Never add `-batchmode` on macOS and never bypass the wrapper.

For every stable step also run:

```text
git diff --check
git status --short
```

Do not claim a Unity, Android, profiler, package, or device validation that did not run to completion.

## 16. Agent Execution Protocol

On every continuation:

1. Re-read `AGENTS.md`, this tracker, and the parent tracker clauses touched by the selected item.
2. Inspect branch/worktree and preserve unrelated changes.
3. Select exactly one dependency-ready unchecked item.
4. Respect its `[TERRA]`/`[SOL]` label.
5. Do not combine schema, ownership mutation, runtime scheduling, Android tuning, and production cutover in one commit.
6. Add or update focused validation with the implementation.
7. Run only the validations actually needed for that bounded step.
8. Update the checkbox, progress summary, and validation log honestly.
9. Commit only the stable step and push it.
10. Stop when the next item is `[SOL]` and the active model is Terra.

Forbidden shortcuts:

- reducing city density before representation optimization is measured;
- hiding source render rows without a complete logical parity row;
- using `Disabled` on simulation entities;
- invoking `Schedule(...).Complete()` in a steady-state virtualization path;
- using per-frame LINQ, managed lists/dictionaries, string formatting, scene searches, reflection, runtime asset loading, or mutable static state;
- creating/destroying proxy entities during camera travel;
- changing shared render components during camera travel;
- enabling production mode from a scripting define or map-id special case;
- weakening overflow, matrix/bounds, ownership, readiness, package, or lifecycle gates;
- mutating accepted/frozen production assets without the existing transactional authorization.

## 17. Progress Summary

| Phase | Status |
|---|---|
| Phase 0: Evidence and amendment | In progress; Android failure and design authorization recorded; raw profile/inventory open |
| Phase 1: Additive schema and pure contracts | In progress; default-safe render-residency mode accepted |
| Phase 2: Deterministic database builder | Not started |
| Phase 3: Candidate pool baking | Not started |
| Phase 4: Jobified runtime assignment | Not started |
| Phase 5: Render-only pilot | Not started |
| Phase 6: Building and attachment state | Not started |
| Phase 7: Coverage, LOD, and GPU headroom | Not started |
| Phase 8: Lifecycle, determinism, and parity | Not started |
| Phase 9: Android 60 FPS acceptance | Not started |
| Phase 10: Cutover and closeout | Not started |

Checklist progress: `3 / 80` complete.

## 18. Validation Log

| Date | Step | Validation | Result | Notes |
|---|---|---|---|---|
| 2026-07-27 | `VRP-000` corrected APK Android evidence | Exact APK install/cold launch; dense definition/EntityScene resolution; 30-second stable observation; `dumpsys meminfo`; `top -H`; screenshots; opt-in marker rerun; structured JSON | Passed as diagnostic baseline; performance acceptance rejected | `15.7-16.3 FPS`, CPU main `56.7-59.8 ms`, GPU `15.6-16.4 ms`, render thread `1.8-2.3 ms`, gameplay update approximately `0.4 ms`; raw profile not recovered after wireless ADB disconnected |
| 2026-07-28 | `VRP-001` measured design amendment | Parent-contract/evidence audit; packed asset-sharing report; architecture/type/assembly inventory | Design accepted for candidate implementation | Simulation remains resident; only render materialization is virtualized; production and frozen rollback remain unchanged |
| 2026-07-28 | Naming and SOLID/ECS alignment correction | `file_naming_architecture_contract.md`; `gameplay_solid_ecs_contract.md`; proposed type/file/readiness-owner audit; checklist/dependency consistency audit; `git diff --check` | Passed documentation contract alignment; implementation remains open | All proposed ECS components now use `*Component`, the buffer record no longer uses forbidden `*Element`, the generated ScriptableObject uses `*Config`, every bare `*System` remains a real ECS system, and `Game.Composition` remains the exclusive operation-map readiness publisher |
| 2026-07-28 | `VRP-010` default-safe render-residency mode | Windows GUI-licensing wrapper `%TEMP%\warline-render-virtualization-vrp010-host.log`; `[OperationMapRenderVirtualizationValidation] result=Passed tests=6`; wrapper exit code `0`; `git diff --check` | Passed; additive contract accepted | Added the closed `ResidentEntities = 0` / `VirtualizedProxyPool = 1` enum and serialized definition field. Existing definitions remain resident without asset resaves, unknown values fail closed, static chunks reject virtualization, and EntityScene virtualization remains rejected until the database/pool contract exists. The first sandboxed wrapper attempt timed out on denied BIOS/licensing access and its wrapper-owned process tree was cleaned after timeout; the identical host-access wrapper compiled and passed. No candidate, accepted, frozen, production, Addressables, EntityScene, or Android artifact changed. |

## 19. Evidence References

- `Design/AgentReports/2026-07-27_dense_city_candidate_android_minimap_performance_rerun.json`
- `Design/AgentReports/2026-07-25_dense_city_packed_asset_sharing.json`
- `Design/AgentReports/2026-07-27_dense_city_candidate_android_runtime_content.json`
- `Design/AgentReports/2026-07-21_dense_city_phase0_android_baseline.md`
- `Design/AgentReports/2026-07-19_operation_map_android_performance_characterization.md`
- `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`
- `Design/Architecture/performance_regression_contract.md`
