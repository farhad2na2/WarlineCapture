# Runtime Operation Map Generation R&D Implementation Tracker

**Status:** Active. R71 is the stable pre-Android narrow-frontage candidate. The plus-shaped autobahn, perpendicular crossing, and imported major-road/runway content remain removed; the only authored inter-district route is one connected three-segment `3.2 m` dusty lane with no segment longer than `36 m` and zero imported or authored solid-prop intersections. Three road-facing civilian houses and bounded courtyards reduce the central seam, while a continuous frontage clearance removes penetrating rocks and oversized vegetation without exposing ground. Exact editor/runtime manifests and same-camera renders match, lifecycle recovery passes, all 28 focused runtime-city tests, all 3 M01 source/evidence tests, and all 17 production source-growth checks pass. Overall owner visual acceptance remains open; desktop generation-time improvement, Android profiling, gameplay integration, and production adoption also remain open.

**Created:** 2026-07-15

**Workflow:** Direct work on `main`; no feature branch or pull request requested for this R&D

**Pilot:** M01 First Contact / Old Market visual exploration

**Reference scene:** `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity`

**Active goal:** Reconstruct the accepted M01 editor result at runtime first: same visible prefab content, transforms, active state, materials, ground, lighting, framing, and rendered composition. After parity is proven, fix shared defects such as penetrating rocks and uncovered ground, then evaluate controlled procedural variation.

**Continuation:** Thread heartbeat `m01-runtime-map-quality` reopens this tracker every 30 minutes until all quality gates are complete. The heartbeat must stop after this tracker reaches Complete.

## 1. Objective

Prove whether Warline Capture can reconstruct an accepted editor-authored operation map through the runtime generation experience without changing its visual result. Exact editor-to-runtime replay is Gate 1. Updating the existing `RuntimeCity*` procedural generator is a later experiment and cannot substitute for Gate 1.

The experiment is motivated by three potential benefits:

- reduce per-map scene, bake, and serialized topology content in Android builds;
- support many deterministic map variants from a compact recipe and shared art palette;
- turn generation time into a visible loading experience rather than hiding a long blocking step.

The current M01 editor prototype is the mandatory golden source. The first runtime milestone is not "similar quality": the runtime result must reproduce the editor-authored visible content and matched-camera image closely enough that every remaining difference is measured and explained. The project owner is not a substitute for internal visual QA. No runtime output is sent for owner review until structural parity and matched-camera evidence pass. After parity, the same source and runtime result are improved together so penetrating rocks, uncovered ground, and other known weaknesses are removed without losing parity.

## 2. R&D Authority And Containment

The project owner explicitly authorized runtime-generation R&D after reviewing the first editor-built M01 scene. This supersedes older prototype text that prohibited runtime physical-map generation for this experiment only.

This tracker does not promote runtime generation to the campaign architecture. Production adoption requires separate visual, gameplay, loading, Android, determinism, memory, and build-size evidence.

The R&D slice must:

- leave `Match.unity`, campaign loading, mission logic, navigation, HUD, and Addressables unchanged;
- leave the accepted editor-built M01 prototype intact as the comparison target;
- use an isolated runtime prototype scene and config;
- keep production RuntimeCity startup and ECS bridges as the default path;
- make visual-only behavior explicit at the bridge boundary;
- avoid modifying shared art merely to make the experiment run;
- remain removable without changing production gameplay behavior.
- preserve the gameplay performance contract: target `0 B/frame` managed allocation after warmup, with any recurring residual profiler-backed, justified, documented, and below `1 KB/frame`;
- keep generation allocations inside explicit, bounded construction batches; hot camera, progress, diagnostics, and presentation ticks must not use LINQ, closure capture, temporary managed collections, boxing-prone interface enumeration, scene searches, or string formatting before a diagnostics/change gate;
- collect Android frame average/p95/p99/max, per-frame GC and spike attribution, peak and retained memory, renderer/draw/triangle counts, thermal behavior, and generation timings before performance acceptance.

## 3. Existing Generator Audit

The legacy generator is already decomposed enough to reuse. It is not necessary to recreate city planning.

| Existing owner | Decision | R&D use |
|---|---|---|
| `RuntimeCityConfigCompositionSystemHelper` | Reuse | Read deterministic seed, counts, density, spacing, and prefab palettes from an isolated config clone. |
| `RuntimeCityLayoutUtilitySystemHelper` | Reuse unchanged | Clamp city centers, calculate town radius, and respect bounds. |
| `RuntimeCityRoadLayoutUtilitySystemHelper` | Reuse; visual-only policy extension | Generate town road strokes and inter-city paths. Visual prototype mode trims configured radial endpoints and outer branches while production uses the original default policy and RNG sequence. |
| `RuntimeCityIngressUtilitySystemHelper` | Reuse unchanged | Create city layouts and entrance corridors. |
| `RuntimeCityChainUtilitySystemHelper` | Reuse, later slice | Retain multi-city and autobahn planning after the one-city adapter is proven. |
| `RuntimeCityRoadCommitCompositionSystemHelper` | Reuse | Commit planned strokes through a visual-only road adapter instead of Match road ECS. |
| `RuntimeCityBuildingPlotUtilitySystemHelper` | Reuse unchanged | Produce central, outer, entry, roadside, and rural candidates. |
| `RuntimeCityWalkabilityUtilitySystemHelper` | Reuse unchanged | Preserve road overlap, spacing, footprints, yards, and corridor exclusions. |
| `RuntimeCityPrefabSelectionPrefabSystemHelper` | Reuse; visual-only policy extension | Select palette prefabs and estimate footprints. Visual prototype mode caps consecutive multi-option selections while production keeps the original default. |
| RuntimeCity hall/landmark/building helpers | Reuse unchanged | Place the same configured structures using a visual-only spawn bridge. |
| `RuntimeCityRuralBuildingSpawnPrefabSystemHelper` and `RuntimeCityFreeScatterDecorationPrefabSystemHelper` | Reuse; visual-only envelope extension | Preserve production radius and RNG ordering. Visual mode applies allocation-free Manhattan and axial limits to avoid isolated perimeter satellites while retaining diagonal density. |
| RuntimeCity wall/gate/decoration helpers | Reuse unchanged initially | Preserve existing sequencing; curate quality after parity is stable. |
| `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper` | Extend observability only | Keep its staged yields and expose each group as visible progress. |
| `RuntimeCityLifecycleCompositionSystemHelper` | Extend observability only | Retain one `MoveNext` per frame, cancellation, and lifecycle ownership. |
| `RuntimeCityGenerationCompositionSystemHelper` | Extend observability only | Publish deterministic stage and work progress without changing RNG calls or placement order. |
| `RuntimeCityVisualPresentationSystemHelper` | Reuse and extend | Instantiate visual building prefabs under an isolated runtime root. |
| `RuntimeCitySpawnBridgePrefabSystemHelper` | Adapt | Add an explicit visual-only adapter alongside the unchanged production building/ECS adapter. |
| `RuntimeCityRoadBuildBridgeCompositionSystemHelper` | Adapt through existing callback context | Route road strokes to R&D GameObject presentation while leaving the production road path unchanged. |
| `RuntimeCityStartupSystemHelper` | Bypass in isolated host | Match readiness, initial units, and ECS grid checks are not relevant to a standalone visual prototype. |
| `RuntimeCityReadinessQueryCompositionSystemHelper` | Bypass in isolated host | Supply a local `GridConfig` directly. |
| `RuntimeCityMinimapEventUiSystemHelper` | No-op | The R&D scene has no Match minimap. |
| Match building health, ownership, delete requests, blockers, and gameplay entities | Reject for first slice | Add only after visual acceptance proves the experiment worth continuing. |

## 4. Target R&D Architecture

```text
RuntimeCityRAndDMapView (passive MonoBehaviour)
  -> serialized config, grid, material, generated-root, camera, and status-text references
  -> lifecycle binding and generate/restart/clear intent forwarding only
RuntimeCityRAndDMapSystem (managed SystemBase)
  -> owns the runtime update, staged-camera advancement, and presentation state applied to the view
  -> RuntimeCityRAndDMapCompositionSystemHelper
       -> isolated RuntimeCity config + local GridConfig
       -> existing deterministic planner graph and staged lifecycle
       -> plan-only road and building adapters for accepted-seed parity runs
       -> RuntimeCityReadModel stage, progress, seed, city, and work counts
       -> RuntimeOperationMapVisualRecipePresentationSystemHelper
            -> versioned export of the accepted editor composition
            -> unambiguous prefab/subobject identity; duplicate names are never addresses
            -> exact ground, transform, material, active-state, lighting, and effect replay
            -> staged runtime GameObject realization under one disposable root
```

The view must not own `Update`, `LateUpdate`, `FixedUpdate`, coroutines, `OnGUI`, generator state, logging policy, or cleanup. The managed ECS system is the only frame owner for this R&D scene. Plain managed collaborators use an approved reason suffix and may not use application-layer role names.

The adapter boundary is the central rule. Planning and placement remain shared. Only realization differs:

- production realization creates road/building gameplay state and ECS data;
- R&D realization creates visual GameObjects under one disposable root.

No second road planner, plot planner, city planner, or decoration planner is permitted. The compact visual recipe is a temporary parity bridge: it proves that the accepted editor composition can be reconstructed at runtime without shipping its serialized hierarchy, while the existing RuntimeCity planner remains active and observable. It does not yet prove unlimited high-quality seeded variants. Recipe decisions must be replaced district by district with reusable generator rules during Phase 4.

## 5. Determinism Contract

For the same generator version, config content, prefab identities, grid, and seed:

- road-cell sequences must match;
- selected prefab sequence must match;
- building origins and footprints must match;
- generation stage order and final counts must match;
- restarting the prototype must clear all prior generated objects;
- no RNG call may be added only for UI, logging, timing, or presentation;
- progress reporting may observe work but must not influence generation decisions.

Every prototype run must display and log:

- runtime-generation version tag;
- seed;
- requested and generated city count;
- current stage;
- normalized approximate progress;
- elapsed frames and seconds;
- generated road strokes and visual buildings when available;
- completion, cancellation, or failure state.

## 6. Visible Generation Stages

| Stage | Visible result | Progress meaning |
|---|---|---|
| Idle | Ground, camera, and lighting only | Host is configured but generation has not begun. |
| Planning | Seed and map bounds are displayed | Layout and exclusions are being prepared. |
| Roads | Road strokes appear | City streets and later connectors are committed. |
| Landmarks | Hall and landmark silhouettes appear | Primary orientation anchors are placed. |
| Buildings | Shops, houses, gas stations, and other structures appear in groups | Existing bulk coroutine batches advance over frames. |
| Decorations | Walls, gates, cloth, arches, and decoration buildings appear | Existing decoration groups complete. |
| Finalizing | Deferred bridges close and counts are published | No new planning is allowed. |
| Completed | Full visual map remains visible | Read model is stable at 100%. |
| Cancelled | Partial output may remain until restart/clear | Host intentionally stopped the run. |
| Failed | Version tag, stage, and error are retained | Failure must not be presented as completion. |

Progress is intentionally approximate. The stage and exact work counts are authoritative; the percentage exists for loading presentation only.

## 7. Implementation Phases

### Phase 0 - Audit And Contract

- [x] Inspect the M01 editor prototype and quality-reference documents.
- [x] Audit the RuntimeCity composition, lifecycle, generation, bridge, visual, and helper graph.
- [x] Confirm existing generation already yields across frames.
- [x] Identify production-only startup/readiness/ECS dependencies.
- [x] Define the visual-only adapter boundary.
- [x] Create this implementation tracker.

**Exit:** A reuse-first path exists with no need for a parallel generator.

### Phase 1 - Observable Existing Generator

- [x] Add a public immutable generation progress snapshot and stage enum.
- [x] Publish progress through `RuntimeCityReadModelCompositionSystemHelper`.
- [x] Instrument stage boundaries without adding RNG calls or changing placement order.
- [x] Report cancellation and completion distinctly.
- [x] Add a version tag that identifies this runtime-generation slice in device logs.
- [x] Add focused tests for legal stage order, normalization, completion, and cancellation.

**Exit:** Match can continue using the same generator while an external observer can explain what it is doing.

### Phase 2 - Visual-Only Parity Adapters

- [x] Add a visual-only building spawn mode to `RuntimeCitySpawnBridgeState`.
- [x] Preserve upstream planner placement checks and the selected origin/footprint at the adapter boundary.
- [x] Assign stable run-local IDs so rejected placements can delete their visuals.
- [x] Route existing road-stroke callbacks to an isolated road visual owner.
- [x] Keep production ECS/building and road bridge behavior unchanged by default.
- [x] Add focused bridge tests for visual spawn/delete/clear and planner-only placement tracking.

**Exit:** The existing generator can realize roads and configured buildings without Match gameplay systems.

### Phase 3 - Standalone Runtime Prototype

- [x] Add `RuntimeCityRAndDMapView` as a passive serialized-reference and intent boundary.
- [x] Add `RuntimeCityRAndDMapSystem : SystemBase` as the runtime frame owner.
- [x] Tick generation from `SystemBase.OnUpdate` with one existing lifecycle step per rendered frame.
- [x] Create a dedicated M01 runtime prototype config cloned from shared prefab references.
- [x] Create `M01_RuntimeGenerationPrototype.unity` with ground, camera, lighting, host, and overlay.
- [x] Show generation stage, version, seed, time, and progress while geometry appears.
- [x] Support deterministic restart and clear.
- [x] Keep the scene out of production build settings until explicitly requested.

**Exit:** Entering Play Mode in the isolated scene visibly constructs one deterministic city over multiple frames.

### Phase 4 - Gate 1: Exact Editor-To-Runtime Replay

- [x] Capture the first recipe-based runtime output and record that it is not exact parity.
- [x] Diagnose the first replay defects: duplicate name-path ambiguity, 202 runtime suppressions, altered foundation, disabled effects, different lighting, different final camera, and different semantic fingerprints.
- [x] Replace name-based district slice lookup with unambiguous source-object identity, or realize the complete source prefab while exactness is established. R50 uses complete prefab realization as an exactness probe; incremental sibling-index identity remains required before performance acceptance.
- [x] Preserve editor-authored prefab content, transforms, scale, active renderer state, materials, lights, and effects without cleanup or procedural substitutions.
- [x] Reproduce the editor `DesertGround`, render settings, lighting rig, fill light, final camera transform, projection, FOV, and post-processing.
- [x] Emit editor and runtime visual manifests from the same inclusion rules and require equal counts and semantic hashes.
- [x] Capture editor and runtime perspective and top-down images from identical cameras and deterministic render settings.
- [x] Record pixel/perceptual deltas and inspect every material region difference; no unexplained difference passes.
- [x] Exercise restart/clear after parity and prove one generated root with no duplicates.
- [ ] Obtain project-owner confirmation only after internal parity evidence passes.

**Exit:** Runtime reconstructs the accepted editor M01 result. A deterministic result, a structurally valid result, or a result with similar object counts does not pass if it looks different.

### Phase 4B - Improve The Parity Result

The earlier checkmarks in this phase described changes to the rejected algorithmic map, not improvements to an editor-parity result. They are rescinded for this milestone.

- [~] Record every visual issue still present in the editor/runtime parity pair. The R71 internal audit below records the currently visible debt; project-owner review may add or reprioritize issues.
- [~] Classify each issue as a reusable replay rule, M01 authored constraint, palette problem, or presentation problem. R71 classifications are recorded below and remain provisional until owner review.
- [x] Guarantee a continuous, visibly readable ground surface beneath the full playable composition and camera footprint.
- [~] Reject or relocate large rocks, dunes, hills, and other terrain dressing that intersects structures or primary roads. R64 clears every local-road intersection and lowers 11 high-confidence rock/building penetrations; the broader intentional rock-foundation contact set remains a visual-review item.
- [~] Add coherent district aprons, road shoulders, and terrain transitions so modules do not read as disconnected islands. R71 retains the support-aware terrain cleanup and compact district placement, adds three road-facing civilian houses with bounded courtyards, and clears penetrating rocks and oversized vegetation across their continuous frontage. The route is constrained to `3.2 m` width and `36 m` maximum segment length while hard road/structure and primary-structure overlap gates remain at zero. Owner review is still required before this composition is accepted.
- [ ] Rebalance dense clusters and dead zones while retaining clear landmark silhouettes and tactical breathing room.
- [ ] Orient buildings toward their road circulation intent where the source prefab allows it.
- [ ] Improve horizon closure and framing without placing large silhouettes behind or through foreground structures.
- [x] Keep the accepted replay deterministic and publish surface/clearance validation counts in the runtime evidence.
- [x] Fix reusable issues in source/export/replay logic rather than patching one runtime instance.
- [ ] Improve district transitions, repetition control, road edges, building orientation, empty areas, horizon composition, and damaged-area storytelling.
- [x] Regenerate the accepted editor/runtime pair after every source or replay rule change.
- [x] Prevent improvements from reducing determinism, generation responsiveness, cleanup reliability, or parity.

#### R60 Known-Issue Classification

| Issue | Classification | R60 disposition |
|---|---|---|
| Two oversized perpendicular road slabs crossed district buildings and read as a four-way autobahn | M01 authored constraint | Removed from the shared editor source; highway curbs, markings, crosswalk, and distant exit closures removed with them. R68 retains only one short `3.2 m` dusty bent route and reports zero imported or authored solid-prop intersections. |
| A first local-route attempt crossed the residential module's dense authored rock field | Reusable source validation plus M01 authored constraint | South connector withdrawn; bombing moved to a bounded Old Market route. The generator now fails on any building or large-terrain overlap with a local-road corridor; R60 reports `0`. |
| Dark gaps between district aprons made connected areas read as uncovered ground | Presentation problem | Added lower transition aprons beneath the Old Market-to-compound and Old Market-aftermath links while retaining one horizon-scale foundation. |
| Destroyed truck, crater, barriers, smoke, and ruin did not communicate the bounded M01 civilian-route incident | M01 story composition | Regrouped at the blocked route endpoint and retargeted the aftermath review camera. |
| Remaining district-edge repetition, building orientation, dead-zone balance, horizon polish, and non-road terrain/structure clearance | Shared-source visual quality backlog | R64 removes imported major roads and the highest-confidence rock penetrations. Broader composition review remains open; no owner-acceptance checkbox is claimed. |

#### R71 Current Visual-Issue Audit

This audit is based on direct inspection of the matched editor/runtime perspective and top-down captures. It records visible debt rather than claiming owner acceptance or a Match-quality score.

| Visible issue | Classification | Next acceptance evidence |
|---|---|---|
| The gameplay view is heavily weighted toward the dense, cropped Old Market foreground while the center and east read much more sparsely. | M01 authored composition and camera presentation | Owner feedback on whether to reduce foreground density, tighten the playable footprint, or reframe around the central route. |
| The top-down footprint still reads as separate north-ruin, west-market, east-compound, and south-residential lobes connected by narrow seams. | M01 authored district composition | A revised top-down capture with a continuous settlement silhouette and intentional tactical open space rather than accidental gaps. |
| The three new frontage houses reduce the seam but remain isolated objects instead of a convincing street wall or courtyard sequence. | M01 authored transition constraint | Matched perspective/top-down evidence showing repeated frontage rhythm, entrances, yards, and small-prop density without road overlap. |
| Several inherited buildings expose side or rear elevations to the replacement circulation route. | Reusable orientation metadata plus M01 authored overrides | Per-building road-facing intent or explicit authored exceptions, validated without rotating complete imported modules destructively. |
| The rectilinear utility compound has a cleaner, more military material and spacing language than the organic village modules around it. | Palette and M01 composition problem | Owner decision on whether it should remain a deliberate gameplay landmark or receive denser civilian edge integration. |
| The narrow route no longer dominates, but its box/shoulder edges and endpoints remain more abrupt than the imported dirt-road language. | Reusable road-surface presentation rule | Close and overview captures showing gradual shoulder blending, grounded endpoints, and no new wide road slabs. |
| Isolated rocks and props remain visible around the outer horizon and weaken the authored settlement boundary. | Presentation and horizon-closure rule | A gameplay capture with deliberate skyline closure and no floating-looking perimeter silhouettes. |
| Most terrain and architecture occupy a narrow brown value range, reducing district and circulation readability. | Palette, lighting, and material hierarchy | Owner-approved color/value hierarchy that preserves the desert setting while separating road, ground, civilian, landmark, and damage regions. |
| The bombing aftermath is not clearly readable from the final gameplay overview despite being present and staged during generation. | M01 story composition and final-camera presentation | Owner feedback from the aftermath camera plus a final overview where the incident reads without oversized effects. |
| Broader intentional rock/foundation contacts remain beyond the high-confidence penetration gate. | Reusable terrain-clearance policy plus M01 visual review | Close inspection or renderer-bounds evidence distinguishing structural foundations from visible penetrations. |

R71 remains the fixed owner-review baseline while this table is reviewed. No Android optimization or procedural-variation work starts from these classifications before visual direction is confirmed.

**Exit:** Runtime M01 is visibly stronger than the original editor prototype and the improvements generalize to future runtime maps where appropriate.

### Phase 5 - Loading Experience

- [x] Define a recipe-owned camera path and reveal order while construction runs.
- [x] Give each visible stage a readable minimum duration and reveal terrain, roads, anchors, districts, story dressing, and horizon in that order.
- [x] Expose player-facing stage labels independently from the technical diagnostics text.
- [x] Keep first visible geometry under 250 ms while allowing the complete high-quality assembly to take multiple seconds.
- [ ] Replace technical overlay with loading-shell progress events after visual acceptance.
- [x] Prioritize ground, primary roads, skyline anchors, and near-camera districts.
- [x] Add a measured frame-time budget in addition to the fixed item-count safety cap.
- [x] Add cancellation when the player leaves loading.
- [x] Add failure recovery and a deterministic fallback map policy.

**Exit:** Longer generation reads as intentional map assembly and does not freeze interaction or obscure failure.

### Phase 5A - Architecture Gates For The Experience

- [x] Keep `RuntimeCityRAndDMapView` passive: serialized references, lifecycle binding, intent forwarding, and presentation application only.
- [x] Keep frame ownership in `RuntimeCityRAndDMapSystem : SystemBase`.
- [x] Keep new generation state and quality policy in narrow system helpers called from the ECS system.
- [x] Do not introduce runtime role names ending in `Builder`.
- [x] Add focused source-shape tests for any new view/system boundary.
- [x] Keep recipe and quality rules data-driven enough to reuse beyond M01; isolate M01-authored constraints in the M01 recipe/tooling.

**Exit:** The generation experience passes the project architecture tests and contains no hidden MonoBehaviour update loop or scene-specific runtime controller.

### Phase 6 - Gameplay And Bake Replacement Research

- [ ] Determine whether runtime navigation can be generated incrementally or must remain prebuilt.
- [ ] Generate blockers, road tags, minimap data, mission anchors, and spawn zones from the same accepted plan.
- [ ] Define save/load identity as generator version + recipe hash + seed, not serialized GameObjects.
- [ ] Test deterministic regeneration after suspend/resume and process death.
- [ ] Compare runtime combined-mesh, GPU instancing, Entities Graphics, and pooled GameObject presentation.
- [ ] Prove that generated maps meet mission readability and pathfinding requirements.

**Exit:** A runtime map is playable and reproducible without depending on a hidden editor bake.

### Phase 7 - Android And Build-Size Decision

- [ ] Record time to first geometry and total generation time on the target Android device.
- [ ] Record average, p95, and maximum generation-frame main-thread time.
- [ ] Record GC allocations, peak memory, retained memory, renderer count, draw calls, triangles, and texture memory.
- [ ] Record cold start, warm start, suspend/resume, and thermal behavior.
- [ ] Compare APK/AAB and installed size against an equivalent editor-baked scene.
- [ ] Separate shared-art cost from per-map topology, lightmap, nav, occlusion, and serialized-scene cost.
- [ ] Test at least three seeds and one intentionally dense stress config.

**Exit:** Runtime generation is adopted only if measured package/content savings justify generation time, complexity, and runtime memory.

## 8. Acceptance Budgets For The R&D Pilot

These are initial experiment targets, not production promises.

| Metric | Pilot target |
|---|---|
| Deterministic restart | Same seed produces the same final counts and transforms in 10 consecutive runs. |
| Time to first geometry | At most 0.5 seconds on desktop and 1.5 seconds on target Android. |
| Total one-city visual generation | At most 5 seconds on desktop and 15 seconds on target Android. |
| Worst generation frame | Below 50 ms on desktop; below 100 ms on target Android during R&D. |
| GC after warm-up | Target `0 B/frame` after presentation warmup. Attribute deliberate instantiation-batch spikes separately; any recurring residual must have profiler call-stack evidence, be justified and documented, and remain below `1 KB/frame`. Diagnostics must gate before formatting. |
| Failure visibility | Stage, version tag, seed, and reason remain visible and logged. |
| Cleanup | Restart leaves one generated root and no duplicate road/building visuals. |
| Production isolation | Match behavior, build settings, campaign loading, and shared configs remain unchanged. |

### Current Desktop Evidence

| Measurement | R19 result |
|---|---|
| Runtime-generation version | `RuntimeCityRuntimeGeneration_R19_2026-07-16` |
| Deterministic seed | `26071501` |
| Time to first runtime geometry | 0.076 seconds |
| Internal completion | 14.799 seconds; the reveal spans six readable stages and stays within the 15-second desktop pilot ceiling |
| Smoke wall time including staged market capture, 2.5-second render settle, perspective capture, and top-down capture | 21.046 seconds |
| Existing planner output | 1 city, 16 road strokes, 124 road cells, 81 planned buildings |
| Accepted visual recipe output | 248 standalone entries, 3 district modules, 6,851 district slice paths, 7,099 realized work items, 10,279 child renderers |
| Surface and clearance output | 1 horizon-scale foundation; 202 oversized or outside-footprint district objects suppressed |
| Frame pacing | 6 ms soft budget, 8-item safety cap, 133 budget-triggered yields, 31.371 ms worst visual batch |
| Recipe serialized size | 576,938 bytes versus 789,990 bytes for the editor prototype scene; shared referenced art is excluded from both topology figures |
| Runtime-city focused validation | 11 of 11 passed |
| SystemBase/MonoBehaviour architecture validation | 19 of 19 passed |
| Broad runtime naming validation | 1 of 1 passed |
| Deterministic regeneration | Compact recipe passed self-regeneration with SHA-256 `B474CB9B3B95EA1116481F5056DB37386113EDC0D056DC690E39B42EC69ADF8A`, but it does **not** match the editor fingerprint `07FF5A66618BDCE2C662DD0EAF5C7C5E21373B4A670182C4E7A5EB9BD7EB42EC`; this is deterministic non-parity. |
| Runtime warnings targeted by R19 | no generation exceptions, missing ground, large district-rock penetrations, or negative-scale collider warnings |
| Visual evidence | `Logs/M01_RuntimeGenerationReveal_Market.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png` |

These measurements describe the rejected compact replay and do not pass Gate 1. Timing evidence remains useful, but visual/structural parity, Android, repeated-run determinism, frame-time percentile, memory, and build-size measurements remain open.

### R50 Exact-Replay Retry Evidence

R50 replaces the ambiguous name-path district replay with temporary complete-prefab realization. This is an exactness probe, not the accepted runtime implementation.

| Measurement | R50 result |
|---|---|
| Runtime-generation version | `RuntimeCityEditorParity_R50_2026-07-16` |
| Recipe identity | `M01RuntimeVisualRecipe_2026-07-16_v13_exact_replay` |
| Runtime output | 251 work entries, 3 complete district modules, 10,268 active renderers, 1 exact editor foundation, and 0 cleanup suppressions |
| Visual composition | The runtime perspective and top-down captures contain the editor-authored west market, east utility/airport, south residential district, roads, terrain, and landmarks. The rejected sparse algorithmic composition is no longer on the render path. |
| Geometry comparison | The same-position perspective pair has 85.4% broad-edge overlap at a 5-pixel tolerance despite the known lighting mismatch. This is useful diagnostic evidence, not a parity threshold or acceptance result. |
| Generation timing | 11.890 seconds smoke wall time; complete-prefab realization produced a 373.548 ms worst visual batch and therefore fails the desktop frame-pacing contract. |
| Capture validity | Real graphics-device captures passed nonblank validation at average luminance `98.04 / 113.20 / 94.63`; `-nographics` captures were uniform gray and are rejected as evidence. |
| Effects | Runtime/player particles remain enabled. The explicit evidence camera temporarily disables particle renderers because forced `Camera.Render` crashed Unity in `ParticleSystemRenderer_RenderMultipleMeshes`; renderer state is restored after capture. |
| Camera diagnosis | Perspective uses the editor gameplay camera. The first R50 top-down capture incorrectly used `(0,280,0)` / size `145`; the tool is corrected to the editor-authored `(0,260,-4)` / size `116` / authored rotation, but a corrected pair is still required. |
| Lighting diagnosis | Runtime configured both directional lights while the editor configures only the first and stops. The generator and serialized R&D scene now stop after the first light; the corrected pair is still required. |
| Unity retry blocker | After the first successful real-graphics smoke, fresh Unity batch launches failed before project code because the Package Manager could not connect to its IPC stream. This does not pass or fail map parity; the matched editor/runtime rerender remains open. |

R50 proves that the accepted content can be reconstructed from shared prefab references without embedding the editor scene, and it identifies the remaining evidence and performance work. Gate 1 remains open until manifests match and corrected same-camera images have no unexplained differences.

### R51 Indexed-Replay Implementation State

- Replaced duplicate-name slice addresses with serialized sibling-index chains rooted at each district prefab.
- The editor exporter verifies every generated index chain resolves against the prefab asset before writing the recipe.
- Runtime resolution walks existing transforms by integer index without path strings, reflection, LINQ, or per-frame lookup allocations.
- Reusable collider and renderer component buffers replace the inherited per-slice `GetComponentsInChildren` arrays, preventing thousands of avoidable generation-time GC allocations.
- The PlayMode smoke now writes sorted editor/runtime visual manifests and requires equal hashes plus renderer, light, and particle-renderer counts before captures can pass. Entries compare stable visual state rather than hierarchy names: world transforms, mesh/material identity, active/enabled state, renderer settings, and light state.
- The same smoke now captures fresh editor perspective/top-down references before Play Mode, captures runtime from the matched cameras, and fails when RGB mean absolute error exceeds `3` or more than `1%` of pixels differ by over `8` in any channel. Exact structural manifest equality remains mandatory independently of this antialiasing tolerance.
- Bounded district slices retain the prior 64-renderer partition threshold, preserve particles, and continue yielding through the `RuntimeCityRAndDMapSystem : SystemBase` generation coroutine.
- `dotnet build WarlineCapture.sln --no-restore` and `dotnet build Game.Editor.csproj --no-restore` report Build succeeded with zero errors and zero warnings for the changed compile surface.
- `M01_RuntimeVisualRecipe.asset` intentionally remains the last generated R50 full-prefab asset until Unity can execute `BuildValidateAndExit`; R51 runtime timing, renderer counts, manifests, and images are therefore not yet evidence.
- A second normal Unity retry again failed before project code after 30 seconds because Package Manager could not connect to its newly requested IPC stream; the Hub process still had its 2026-07-15 start time.
- A third normal retry in the next goal continuation failed identically on `Upm-42240`; Unity exited before compilation or `BuildValidateAndExit`. This satisfies the blocked-audit threshold. Required external action: fully quit Unity and Unity Hub, reopen Hub, confirm the license is active, then resume this goal.

### Rejected Algorithmic Evidence

The project owner rejected the R48/R49 algorithmic output on 2026-07-16 because it is visibly unrelated to the accepted editor M01 composition and far below the Match quality reference. The previous internal designation of this output as a stable feedback build was incorrect and is withdrawn. These measurements are retained only as historical diagnostics; they are not an acceptance baseline and procedural iteration is deferred until Gate 1 passes.

| Measurement | R49 lifecycle/architecture closeout over the R48 three-seed visual baseline |
|---|---|
| Runtime-generation version | `RuntimeCityRuntimeGeneration_R49_2026-07-16` |
| Reviewed seeds | `26071501`, `26071502`, and `26071503` |
| Time to roads / internal completion | seed 01 `0.065s / 1.595s`; seed 02 `0.075s / 1.631s`; seed 03 `0.067s / 1.535s`; every run completed over 15 generation frames |
| Existing planner output | one city and 16 road strokes per seed; road cells `93 / 92 / 93`; visual buildings `69 / 66 / 68` |
| Visual composition | top-level visuals `139 / 147 / 149`; unique source prefabs `34 / 32 / 34`; maximum 2 consecutive choices in multi-option palettes |
| Terrain integration | 1 runtime-owned continuous foundation; 4 deterministic district cores, 4 transition bands, and 4 wider low-contrast outer blends aligned to market, utility, southeast residential, and southwest damage intent |
| Road integration | Connected asphalt nodes/links with continuous shoulders; all 14 terminal branches per seed have at least 2 nearby visuals and the former empty north/east/south antennae remain removed. |
| Placement quality | Every reviewed seed reports `edgeOutliers=none/0`. Visual-only rural/free-scatter envelopes and terminal trimming remain isolated from production defaults. |
| Measured district centroids | seed 01 market `-36.4,-6.0/28`, utility `43.5,48.8/4`, residential `36.7,-14.5/26`, damage `-46.4,-58.6/4`; seed 02 and 03 retain the same four directional district intents in their review logs. |
| Aftermath dressing | seed 01: 2 measured plus 2 reserved authored anchors, 4/4 groups, 17/20 props; seed 02: 1 measured plus 3 reserved authored anchors, 4/4 groups, 20/20 props; seed 03: 2 measured plus 2 reserved authored anchors, 4/4 groups, 18/20 props. All accepted props pass renderer-bounds collision clearance and the recipe-owned exposure arc. |
| Three-seed transform SHA-256 | seed 01 `4751620F5317D197E59CCEC9237DB9D4CC11FC6610868910620E9D449CB800E5`; seed 02 `CFB76194055AFC7A73D0216437A97B032BBA6E9493E9F67BBDF8D7ED41D8FFDD`; seed 03 `9DD74FAE949DB15CA76B0EC1AFC86F6CD948A97E5687B1D06132881CD3E416E2` |
| Repeat determinism | R45 independently reproduced its complete seed-01 fingerprint and counts. R49 preserves the R48 deterministic placement/presentation rules and passes fixed-seed structural checks; the formal 10-run R49 gate remains open. |
| Loading presentation | Six serialized camera poses; player-facing stage labels; aftermath hold `2.5s`; reveal stays active through Horizon; a compact top status band fits inside the 38-degree camera frustum; formatted debug text changes only with displayed stage/count data. Evidence capture primes temporal render history and emits separate loading, clean perspective, and top-down images. |
| Lifecycle recovery | PlayMode validation cancels the primary run during `Roads`, verifies the partial generated root is empty after deferred destruction, then forces `missingConfig`. The fresh failure reports `stage=Planning seed=26071501`; exactly one next-frame fallback realizes 7,099 work items with one foundation and completes under the R49 version marker. |
| Focused and scene validation | R49 prototype build and scene validation passed; 27 of 27 runtime-city focused tests passed. |
| Broad regressions | 19 of 19 SystemBase/passive-view tests, 1 of 1 broad naming assertions, and 15 of 15 production source-growth checks passed. The accepted-recipe smoke remains green from R45 with 7,099 entries, 10,279 renderers, 202 clearance suppressions, 13.058 ms worst visual batch, and 41 budget yields. |
| Performance/GC source audit | R49 adds only field/boolean checks to the steady `SystemBase` tick. Recovery scheduling is a one-shot transition with no LINQ, closure, temporary collection, scene search, boxing-prone enumeration, or recurring string formatting; terminal logs format only on cancel/failure/fallback transitions. Exact path/scope ceilings now cover every new or grown RuntimeCity R&D helper. Android managed-GC, native-memory, retained-memory, and instantiation-spike attribution gates remain open. |
| Visual evidence | Loading/perspective/top-down PNGs are `Logs/M01_AlgorithmicSeed_<seed>_{Loading,Perspective,TopDown}.png`. They prove the rejected path is sparse, fragmented, poorly road-aligned, repetitive, and materially below the editor and Match references. They must not be sent for owner acceptance again. |

R28 diagnosed the residential problem correctly: visual-only rural spillover moved the residential centroid from R27's `30.8,2.9/28` to `30.3,-25.5/31`, but sharing the southern half with damage reduced strict damage from 8 structures to 3. Rendered review rejected that combined composition.

R29 separated visual-only spillover into a southeast residential quadrant and strict aftermath into a southwest quadrant. The measured centroids matched that intent and strict damage recovered to 6 structures.

R30 added three deterministic small-prop groups anchored to existing strict damage placements. All 15 props realized after 10 collision rejections, but the fixed perspective PNG remained byte-identical to R29 (`795B309B79F6C7D73FD82213712C156F2E2585BCD36F0171D77A5818D5325749`), proving that the new story detail was not visible from the player-facing review camera.

R31 split the old combined southern apron into southeast residential and southwest damage cores and added a 15-percent larger low-contrast transition surface beneath every district core. Perspective and top-down captures both changed, and the terrain follows measured district intent more closely. Rendered review accepted this as an improved terrain direction, not as a visually accepted map.

R32 added editor-only category extents, near-road/free-scatter classification, and named edge-outlier evidence. It identified 33 edge visuals led by a free-scatter southern house, an eastern road-adjacent house, associated yard walls, and utility spillover. The first bounded policy reduced outliers to 2, but over-concentrated placement: visual buildings fell from 75 to 68, residential from 28 to 20, and utility from 6 to 3. R32 was rejected.

R33 widened the visual preferred band to the authored town radius and free scatter to `townRadius + 1`. It retained the 2-outlier result while recovering 73 visual buildings, 24 residential structures, 5 utility structures, and directional district centroids. Perspective review accepts the bounded density rule as a clear improvement; top-down review still rejects long empty road stubs, two extreme damage structures, and large unoccupied branches.

R34 moved aftermath scale into reusable serialized recipe data, increased M01 to four bounded groups, and realized 20 of 20 props with a 4.7 m maximum silhouette. The perspective changed only in a tiny central patch, so larger small props did not solve player-facing readability.

R35 added a recipe-owned exposure direction and placement arc; focused validation proves collision retries keep accepted props on the authored side of each anchor. Seed `26071501` realized 17 of 20 props with a 4.5 m maximum silhouette, but the R35-versus-R34 perspective diff contained only 256 materially changed pixels in the same small central patch. Directional exposure is retained as useful loading-stage data, while overview readability remains rejected. The rendered three-seed acceptance review remains open.

R36 added permanent editor-only terminal-branch occupancy diagnostics and a visual-only road terminal policy. The fixed seed dropped from 108 to 93 road cells and every terminal branch gained nearby occupation, but the first render exposed an east-edge house yard with nine outlier wall pieces plus the two existing damage outliers, so that intermediate composition was rejected.

R37 and R38 tested whole-radius reductions and were rejected at 59 and 67 visual buildings. Those runs proved that removing an entire Manhattan ring costs too much density.

R39 restored rural density and bounded free scatter to `townRadius + 2`; it retained 69 buildings but still reported one damage outlier and nine pieces from the east-edge yard. R40 introduced allocation-free visual-only axial insets while keeping production defaults and RNG ordering. It removed the east satellite and its yard, retained 69 buildings, and reduced the fixed seed to one south-edge ruin.

R41 tightened only the visual free-scatter axial inset. The fixed seed now reports zero edge outliers, 69 buildings, 139 visuals, 34 unique prefabs, 18 of 20 aftermath props, and 14 occupied terminal branches. Top-down review accepts the hard geometry improvement; perspective review still rejects final visual acceptance because the bombing aftermath is not player-readable and the overview framing gives too much weight to blank perimeter space.

R42 added six serialized algorithmic camera poses and stage-owned reveal timing without moving frame ownership out of `RuntimeCityRAndDMapSystem`. A tighter aftermath pose removed the blank overview foreground and made the southwest damage apron readable. R42 retained the R41 placement fingerprint.

R43 reserved four visual-only free-scatter slots while leaving the production archway budget unchanged. Seed `26071502` proved reservation alone was insufficient because three reserved placements could still be rejected by crowded geometry.

R44 added recipe-owned fallback anchor center/spacing data and fills missing anchors with value-type positions along the damage district's exposure tangent. The failed seed recovered from 1 group and 5 props to 4 groups and 20 props. All three fixed seeds then passed structural checks and rendered review with no measured building/road edge outliers.

R45 retained the R44 transform fingerprint while making algorithmic reveal status player-readable through Aftermath and Horizon. The SystemBase presentation cache now formats overlay text only when displayed data changes instead of every rendered frame. Prototype validation, all 24 focused tests, 19 architecture tests, broad naming validation, and the accepted-recipe smoke passed.

R46 introduced a recipe-owned minimum authored-anchor reserve. Dense seeds now deterministically trim the measured-anchor list in place before adding centered authored anchors, keeping at least two composed bombing-aftermath groups available without adding per-frame collection or formatting work.

R47 added a wider low-contrast outer transition beneath each district core and existing transition band. The one-time composition adds four bounded 28-segment surfaces, softening the district-to-foundation boundary while preserving the runtime root, deterministic ordering, and `SystemBase` ownership.

R48 compacted and frustum-validated the loading status presentation and split automated evidence into loading, clean perspective, and top-down captures. Editor-only capture now primes temporal render history before readback. All three fixed seeds complete in under 1.7 seconds with zero measured edge outliers, prototype validation passes, and the exact-version focused suite passes 25 of 25.

R49 adds a passive cancel intent, `SystemBase` exit cancellation, terminal progress preservation, clean partial-root disposal, and a one-frame-delayed deterministic fallback that can run only once. The lifecycle validator proves cancel-at-Roads cleanup and a `missingConfig` recovery to the accepted 7,099-item recipe; fresh attempts no longer inherit the previous terminal stage. Focused validation passes 27/27, SystemBase/passive-view passes 19/19, broad naming passes 1/1, source growth passes 15/15, and the solution builds with zero errors.

R40-R45 leak/source review moved immediate capture allocations out of the teardown window and found no runtime-generator stack in the remaining Unity editor shutdown diagnostics. These editor findings do not replace Android frame, managed-GC, native-memory, or retained-memory evidence.

## 9. Build-Size Hypothesis

Runtime generation does not automatically remove shared models, textures, shaders, audio, or materials. Those assets still contribute to the build when the runtime palette references them.

The likely savings are per-map serialized content:

- authored scene hierarchy and transforms;
- duplicate baked combined meshes;
- lightmaps and probes unique to each map;
- navigation data;
- occlusion data;
- duplicated terrain or mask outputs;
- Addressables catalog and bundle overhead for many physical scenes.

The build-size experiment must therefore compare equivalent content and report both:

1. total package/install size; and
2. incremental bytes added by each additional map after shared art is already present.

## 10. Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Existing city output is visibly below M01 quality | Treat technical generation as scaffolding only; require editor-prototype parity in Phase 4 and improvement beyond it in Phase 4B. |
| Runtime instantiation spikes frames and memory | Keep staged yields now; investigate frame budgets, pooling, instancing, and mesh batching after visual acceptance. |
| Different seeds produce unplayable layouts | Add deterministic validation and reject/fallback policy before gameplay integration. |
| Generator changes break Match | Keep production adapters as default and add focused tests around visual-mode selection. |
| Debug percentage implies false precision | Present stage and counts as authoritative; label normalized progress approximate. |
| Generated output cannot reproduce old saves after a rule change | Persist generator version and recipe hash; retain compatible versions or invalidate explicitly. |
| Runtime navigation is too expensive | Measure independently; allow hybrid topology recipes or compact precomputed navigation if necessary. |
| Many prefabs erase package-size savings | Measure incremental map bytes and curate shared chapter palettes. |
| Prototype code leaks into production | Keep one scene, config, host, and adapter boundary with a documented deletion path. |

## 11. Files Owned By This R&D

Planned or active paths:

- `Design/Architecture/runtime_operation_map_generation_rnd_implementation_tracker.md`
- `Assets/Game/Scripts/Environment/RuntimeCityGenerationProgressSystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityRAndDMapView.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityRAndDMapSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityRAndDMapCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeOperationMapGenerationRecoverySystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityRoadVisualPrototypeSystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityAlgorithmicDistrictPresentationSystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeOperationMapVisualRecipePresentationSystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeOperationMapVisualQualitySystemHelper.cs`
- `Assets/Game/Scripts/Environment/RuntimeOperationMapSurfaceGeometrySystemHelper.cs`
- `Assets/Game/Scripts/Configs/RuntimeOperationMapVisualRecipe.cs`
- existing narrow RuntimeCity composition, generation, lifecycle, read-model, diagnostics, spawn-bridge, and visual files where required;
- `Assets/Game/Scripts/Editor/MapPrototypes/M01RuntimeMapPrototypeEditorUtility.cs`
- `Assets/Game/Configs/MapPrototypes/M01_RuntimeCity_Config.asset`
- `Assets/Game/Configs/MapPrototypes/M01_RuntimeVisualRecipe.asset`
- `Assets/Game/Art/MapPrototypes/M01/M01_DesertSkybox.mat`
- `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_RuntimeGenerationPrototype.unity`
- focused Editor tests for progress and visual-only bridges.

Explicitly excluded unless the project owner expands scope:

- `Match.unity` and Demo scenes;
- production build settings;
- operation-map loader/catalog work owned by the parallel task;
- campaign mission content and HUD;
- shared art edits;
- the seven in-progress M01 material edits in the working tree.

## 12. Implementation Log

### 2026-07-15 - Reuse Audit

- Confirmed the old generator already owns deterministic layout, road, plot, footprint, landmark, building, yard, and decoration generation.
- Confirmed lifecycle advances one generation coroutine step per frame.
- Confirmed bulk building placement already yields after eleven meaningful placement groups.
- Identified Match startup/readiness and ECS realization as the coupling to bypass, not planning code to replace.
- Selected visual-only bridge adapters and an isolated host as the first runtime parity slice.
- Recorded the direct-main workflow requested by the project owner and preserved unrelated material edits.

### 2026-07-15 - R1 Technical Baseline

- Ran the existing generator with visual-only road and building adapters in the isolated prototype scene.
- Generated 16 road strokes, 124 road cells, and 81 building visuals in approximately 2.1 seconds.
- Rejected the result as a visual baseline because it was sparse, uniform, and materially below the accepted editor map.
- Kept the existing planner and adapters because the failure was presentation/district quality, not lifecycle or deterministic planning.

### 2026-07-15 - R2 Editor-Composition Parity Bridge

- Added an editor extraction step that records top-level prefab/primitive/light decisions from the accepted M01 prototype into a compact runtime recipe.
- Reconstructed all 224 recipe entries at runtime under one generated root while continuing to run the existing RuntimeCity planner in planner-only mode.
- Reached accepted-scene geometry and composition parity with 10,301 child renderers.
- Found negative-scale collider warnings on reused visual prefabs and disabled recipe-instance colliders because this R&D scene is visual-only.

### 2026-07-15 - R3 Concurrent Visible Generation

- Advanced the accepted visual recipe and existing deterministic planner concurrently so the map no longer waits for planning before showing geometry.
- Recorded first geometry at 0.055 seconds and full internal completion at 3.582 seconds over 29 generation frames.
- Passed the automated Play Mode smoke with seed `26071501`, all 224 recipe entries, 10,301 renderers, and stable planner counts.
- Confirmed no compiler errors, exceptions, or negative-scale collider warnings in the R3 smoke log.
- Passed all six focused Editor validations, including progress, cancellation, visual-only realization, and planner-only tracking.
- Preserved the seven unrelated in-progress M01 material edits; the remaining reference difference is warmth/color grading, not geometry or composition.

### 2026-07-15 - R4 Architecture Correction

- Removed the runtime and M01 editor-tool role names that violated the project naming contract.
- Split the runtime host into passive `RuntimeCityRAndDMapView`, managed `RuntimeCityRAndDMapSystem : SystemBase`, and narrow composition/presentation helpers.
- Removed the view-owned coroutine, generation state, cleanup, logging, IMGUI loop, and button policy.
- Moved the one-step-per-frame generation tick into `RuntimeCityRAndDMapSystem.OnUpdate`.
- Replaced the IMGUI overlay with a serialized `TextMesh` presentation reference updated by the ECS system through the view.
- Added a focused architecture regression test that rejects view loops, generation ownership in the view, and the retired role names.
- Regenerated the authoritative SystemBase inventory; `RuntimeCityRAndDMapSystem` is explicit managed presentation exception `P7-0424`, and the project remains below the approved exception cap.
- Passed 19 SystemBase/MonoBehaviour architecture tests, the broad runtime naming guard, and all seven runtime-city focused tests.
- Passed the final Play Mode smoke with first geometry at 0.042 seconds, completion at 3.000 seconds over 29 generation frames, 224 visual entries, and 10,301 renderers.
- Added a one-second editor-only capture settle so newly instantiated materials are fully presented before visual evidence is recorded.

### 2026-07-16 - R5-R10 Surface, Clearance, And Reveal Quality

- Replaced the finite prototype floor assumption with a data-driven horizon-scale foundation created first at runtime; this removes camera-visible ground holes and the black lower-skybox band without adding per-district cover objects.
- Added deterministic irregular district aprons and road shoulders so accepted demo modules sit on coherent local terrain rather than disconnected rectangular pads.
- Added a runtime visual-quality helper that suppresses oversized district rocks, hills, dunes, and terrain pads before presentation; the accepted run reports 183 cleanups and retains its landmark/building silhouettes.
- Added deterministic irregular-surface mesh realization with stable name hashing and runtime mesh cleanup.
- Staged the recipe through terrain/roads, district modules, market, compound, aftermath, and horizon with explicit minimum durations and one-frame yields during heavy prefab realization.
- Replaced the permanent technical wall with a compact versioned progress strip during generation; the completed map is unobstructed while detailed counts remain in structured logs.
- Added a procedural desert sky asset and matched the standalone runtime lighting/fog to the editor prototype.
- Passed the R10 Play Mode smoke at 0.042 seconds to first renderer and 5.614 seconds to internal completion, with 228 entries, 10,000 renderers, one foundation, and stable planner counts.
- Passed 8 of 8 runtime-city focused tests, 19 of 19 SystemBase/passive-view architecture tests, and the 1-test broad runtime naming guard.
- Preserved direct `SystemBase` frame ownership and kept all generation/quality policy in narrow managed helpers; no MonoBehaviour update loop or runtime `Builder` role was introduced.

### 2026-07-16 - R11 Road Exit And Top-Down Review

- Added a second editor-only completed-runtime capture from an orthographic top-down review camera so post-cleanup circulation, road endings, dead zones, and district outliers can be judged directly.
- Added four M01-authored checkpoint exit closures with a readable center lane instead of leaving the finite arterial meshes as unexplained hard stops.
- Extended the generic district quality rule to trim small rock/utility dressing outside the authored district footprint; the accepted seed increased from 183 to 195 deterministic cleanups.
- Kept the perspective composition intact at 10,002 active renderers and recorded 0.053 seconds to first geometry and 5.817 seconds to full internal assembly.
- Passed all 9 focused runtime-city tests, including the new outside-district dressing case.
- Top-down review still identifies detached sidewalk/utility frames and weak southern-to-central circulation; those remain open and prevent visual acceptance from being marked complete.

### 2026-07-16 - R12-R14 District Bounds And Southern Circulation

- Added optional recipe-owned cleanup bounds to visual entries and assigned explicit M01 footprints to the Old Market, utility compound, and southern residential modules.
- Applied those bounds only to terrain, roads, sidewalks, rocks, fences, and utility dressing; buildings and landmarks outside a cleanup bound are intentionally preserved.
- Added a prototype validation gate that fails unless all three M01 district entries serialize configured cleanup bounds.
- Added a reusable district-edge analysis batch command that reports candidate renderer names, world bounds, and distance from the authored module center.
- Removed detached demo-module road/terrain runs while retaining all three dense district silhouettes; the accepted run now reports 198 deterministic cleanup decisions.
- Added a secondary southern relief route between the western market arterial and residential road grid, then narrowed and recolored it as a dusty concrete service road after rejecting the first visually dominant asphalt pass.
- Reached 0.047 seconds to first geometry and 6.160 seconds to complete internal assembly with 251 entries and 9,999 active renderers.
- Passed deterministic editor regeneration with fingerprint `07FF5A66618BDCE2C662DD0EAF5C7C5E21373B4A670182C4E7A5EB9BD7EB42EC`.
- Passed the R14 Play Mode smoke, all 9 focused runtime-city tests, and all 19 SystemBase/passive-view architecture tests.

### 2026-07-16 - R15 Recipe-Owned Camera Choreography

- Added one validated camera pose for each of the six visual reveal stages to the compact runtime recipe, including position, look target, field of view, and transition duration.
- Kept the camera as a serialized reference on the passive view and advanced interpolation only from `RuntimeCityRAndDMapSystem.OnUpdate` through the existing managed composition helper.
- Added structured camera target and arrival logs so device evidence proves which version and reveal stage was presented.
- Added a required market-stage smoke capture; the smoke fails if the runtime system never observes that authored camera stage.
- Preserved the accepted final review framing after the staged flyover and retained the existing perspective and top-down completed-map evidence.
- Passed prototype regeneration/validation, the R15 Play Mode smoke, all 10 focused runtime-city tests, and all 19 SystemBase/passive-view architecture tests.
- Recorded 8.206 seconds to internal completion and 14.656 seconds to smoke completion including the staged reveal capture and completed-map captures.
- Observed an existing Unity 6000.5 batch shutdown instability after successful test reporting; validation results were already emitted before the editor shutdown/crash-handler path.

### 2026-07-16 - R16-R19 Frame Budget And Compact Incremental Districts

- Added a view-configured 6 ms soft visual-work budget while retaining the eight-item safety cap; the passive view only stores configuration and `RuntimeCityRAndDMapSystem.OnUpdate` remains the frame owner.
- Added an editor-only district hierarchy diagnostic and identified 48 safe building assembly roots; the reused accepted modules already contain deliberate varied yaws, so circulation-facing orientation remains a planner-data task rather than a destructive runtime transform guess.
- Published the worst visual batch and budget-triggered yield count through the runtime system and smoke evidence.
- Measured the original whole-module realization at approximately 697 ms for one indivisible district prefab, confirming that fixed entry counts could not protect frame time.
- Proved that bounded prefab subtrees reduce the worst visual batch below the 50 ms desktop pilot ceiling; the first direct-slice representation reached 22.797 ms but was rejected because its 4.98 MB recipe exceeded the editor scene and duplicated module metadata.
- Replaced duplicated slice entries with three compact module records containing one prefab/transform/cleanup record plus deterministic child paths.
- Reached 31.371 ms worst visual work, 133 budget yields, 0.076 seconds to first geometry, and 14.799 seconds to internal completion in R19.
- Reduced serialized runtime topology to 576,938 bytes, below the 789,990-byte editor prototype scene, while reconstructing 7,099 visual work items and 10,279 active child renderers.
- Preserved the accepted perspective, top-down, and staged-market compositions after compaction; the accepted seed reports one foundation and 202 deterministic clearance decisions.
- Passed byte-for-byte compact-recipe regeneration with SHA-256 `B474CB9B3B95EA1116481F5056DB37386113EDC0D056DC690E39B42EC69ADF8A`.
- Passed the R19 Play Mode smoke, all 11 focused runtime-city tests, and all 19 SystemBase/passive-view architecture tests.

### 2026-07-16 - R20 Rejected Algorithmic Baseline

- Added cardinal road-facing intent to roadside plot candidates and applied it only at the visual-only spawn bridge; production building realization remains unchanged.
- Added a visual-only consecutive-selection policy with no extra RNG draws and preserved the original production default.
- Captured fixed seeds `26071501`, `26071502`, and `26071503`; all completed deterministically, but visual review rejected the result because roads read as separated black tiles, the ground was editor-harness-owned, the city was sparse, and repeated walls obscured useful palette diagnostics.
- Preserved the rejected baseline as evidence rather than treating technical completion as visual acceptance.

### 2026-07-16 - R21-R22 Continuous Surface, Roads, Grounding, And Palette

- Moved the algorithmic foundation into the managed runtime composition so the map no longer depends on an editor-only temporary floor; the seed review now fails unless exactly one foundation exists.
- Replaced shrunken road-cell cubes with connected asphalt nodes/links and continuous shoulder nodes/links, eliminating deliberate road gaps while keeping the existing road planner and callback contract.
- Grounded each visual-only source prefab from its lowest renderer point, preventing below-grade structures and terrain/structure penetrations in the algorithmic path.
- Expanded the isolated M01 palette to the existing 12 shop, 7 house, and 2 hall variants and reduced yard-wall chance without editing shared prefab or material assets.
- Increased seed `26071501` from 26 to 43 unique prefabs and replaced the wall-child sequence proxy with the planner's authoritative multi-option repetition metric; the measured cap is 2.
- Passed all 16 R22 focused tests and prototype validation.

### 2026-07-16 - R23-R24 District Intent And Terrain Cohesion

- Extended the existing bulk plot plan with visual-only district ordering: market/shop candidates prioritize west, utility/other candidates east, and residential candidates south. Production mode receives the original central/outer lists and order.
- Tightened the isolated M01 town radius from 14 to 11 road cells while retaining higher building targets, producing a denser footprint without changing the shared radius formula.
- Added three M01-authored district-surface records to the passive scene view and realized them through `RuntimeCityAlgorithmicDistrictPresentationSystemHelper`, owned and disposed by the existing `SystemBase` composition.
- Kept the surface rule reusable and deterministic while isolating Old Market, utility compound, and damaged-residential offsets, sizes, colors, and seed offsets in scene data.
- Fixed seed `26071501` completed in 2.579 seconds with 108 road cells, 87 buildings, 146 top-level visuals, 43 unique prefabs, and transform SHA-256 `6225586F4E428649642A5CEF0194F67E9D3E263509BAD0C500F1EA1EC0E8A0AD`.
- Fixed seed `26071502` completed in 2.011 seconds with 108 road cells, 85 buildings, 163 top-level visuals, 43 unique prefabs, and transform SHA-256 `4B351601FCC2D547F228872BD53AB5A2880917C700C9CFC047281325544D3FFC`.
- Fixed seed `26071503` completed in 2.005 seconds with 111 road cells, 87 buildings, 163 top-level visuals, 38 unique prefabs, and transform SHA-256 `91BAF7AB5092E5BFC03EF750606AD2C82AE3F856BC6BB6FEBB04486DD51D9A6F`.
- A separate repeat of seed `26071501` reproduced the complete transform SHA-256 exactly.
- Every R24 seed reported repetition cap 2, one foundation, and three district surfaces.
- Passed prototype validation and all 18 focused tests, including road continuity, grounding, road-facing rotation/recentering, repetition, district ordering, surface creation/cleanup, and the passive-view/SystemBase architecture source-shape gate.

### 2026-07-16 - R25-R27 Damaged-Corridor Diagnostics And Curation

- R25 added a visual-only southward reflection for free-scatter damage candidates and a versioned district-centroid diagnostic. Production configuration explicitly disables the policy.
- The R25 accepted seed placed ten large damage structures around `z=-56.3`, but strict concentration exhausted available southern footprints and reduced total visual buildings from 87 to 77. The rendered direction was cleaner than broad scatter, but the count regression required investigation.
- R26 retried each untouched deterministic candidate with the same selected prefab when its preferred southern candidate failed. This recovered all 87 visual buildings without extra RNG draws, but rendered review rejected the ten recovered structures because they appeared as isolated perimeter outliers.
- Replaced filename heuristics with exact config-palette membership for market, utility, residential, and damage centroid reporting. Cloth covers and archways are excluded from the free-scatter damage measurement.
- R27 removed perimeter fallback, curated the isolated M01 decoration target from 24 to 14, and retained strict southern concentration. The accepted seed completed in 2.148 seconds with 75 visual buildings, 134 top-level visuals, 37 unique prefabs, eight measured free-scatter damage structures centered at `11.1,-60.0`, one foundation, and three district surfaces.
- R27 rendered perspective and top-down captures contain no blank evidence and no broad ruin outlier ring. They still reject overall visual acceptance because residential placement remains centered (`30.8,2.9`) and the map contains large dead zones.
- Passed R27 prototype validation and all 19 focused runtime-city tests. The passive view, `RuntimeCityRAndDMapSystem : SystemBase` frame owner, narrow helper ownership, and no-`Builder` runtime naming contract remain unchanged.

## 13. Immediate Next Slice

The implementation now proceeds in this order:

1. [x] Record the owner rejection and withdraw the algorithmic output from the acceptance path.
2. [x] Audit the compact recipe and identify concrete parity violations: duplicate name-path addressing, object suppression, altered foundation, disabled effects, lighting drift, camera drift, and unequal fingerprints.
3. [x] Replay the complete editor-authored district prefabs without cleanup as an exactness probe and implement bounded, unambiguous sibling-index slices to replace the 373.548 ms whole-prefab spike.
4. [x] Restore a functioning Unity licensing/Package Manager session, regenerate the indexed recipe/scene, and require every exported index chain to resolve.
5. [x] Finish the matched rerender after the R50 ground, first-directional-light, perspective-camera, and top-down-camera corrections; do not use the pre-fix lighting image as acceptance evidence.
6. [x] Generate comparable editor/runtime manifests and require equal visible-content counts and semantic hashes.
7. [x] Generate same-camera perspective/top-down captures and inspect measured image deltas before any owner review.
8. [x] Prove restart/clear, lifecycle recovery, architecture, focused tests, and performance/GC source contracts remain intact; require the indexed worst batch to return below the desktop budget.
9. [~] After Gate 1 passes, replace the inappropriate plus-shaped autobahn, remove its building intersections, fix penetrating rocks and uncovered ground in the shared editor/runtime source, and prove parity again. R71 removes every imported major road, constrains the replacement to one connected three-segment `3.2 m` dusty route with no segment above `36 m`, and fails generation on road/building or large-terrain intersections. Three road-facing civilian houses and bounded courtyards reduce the frontage seam; a continuous clearance removes penetrating rocks and oversized vegetation while one horizon-scale foundation and overlapping authored ground keep the surface covered. Exact editor/runtime parity, lifecycle recovery, and all `48/48` focused/guardrail tests pass. The R71 desktop candidate is ready for owner review; owner visual acceptance and broader composition work remain open.
10. [ ] Only then reconsider procedural variation, loading-shell integration, Android profiling, gameplay integration, and production adoption.

### 2026-07-16 Roadmap Correction

- The bad algorithmic screenshots were predictable from the existing evidence and should have been rejected internally.
- Asking the project owner to validate that output conflated structural diagnostics with visual acceptance.
- Gate 1 is now exact editor replay. Counts, determinism, ground coverage, or lack of exceptions cannot compensate for a visibly different map.
- The project owner should next see a same-camera editor/runtime comparison that already passes internal structural and visual parity checks.

### 2026-07-16 R50 Retry

- Complete-prefab runtime replay restored the accepted editor composition and removed all 202 compact-recipe suppressions.
- The retry self-rejected a 373.548 ms instantiation spike, a mismatched top-down camera, and an extra configured directional light.
- The next implementation uses sibling-index source identity to preserve exact content while yielding between bounded slices; duplicate names are never runtime addresses.
- Penetrating terrain and uncovered ground remain deliberately unchanged until Gate 1 proves the runtime is replaying the editor source exactly.
- R51 implements that sibling-index source path and compiles, but remains ungenerated and unmeasured until Unity licensing and Package Manager startup recover.

### 2026-07-16 R52 Exact Replay Gate

- Reconstructed the source through three deterministic parity prefab snapshots plus indexed entry snapshots with no nested source-prefab ambiguity.
- Editor and runtime manifests match at `10329` renderers, `11` lights, `71` particle renderers, and SHA-256 `C430A99A87D7E47799EDAE39451BE4F728D494AEC6FCEB03051156A7A3F00A02`.
- Same-camera image comparison measured perspective RGB MAE `0.011` and top-down RGB MAE `0.000`; the remaining perspective delta is below one RGB code value and no material region difference remains unexplained.
- Runtime completion took `18.780s`; smoke evidence completed in `24.390s` with `maxVisualBatchMs=6.212`, one budget yield, no suppressions, one foundation, and real graphics.
- Evidence: `Logs/M01_R52_VolumeParityPlayMode_Graphics.log`, `Logs/M01_EditorCurrentReference.png`, `Logs/M01_EditorCurrentTopDown.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- Owner review remains intentionally withheld until the shared visual source no longer contains the oversized plus-shaped road crossing district buildings.

### 2026-07-16 R60 Blocked Old Market Route Candidate

- Replaced the inappropriate `21 x 230 m` / `300 x 19 m` plus-shaped road slabs with one narrow Old Market-to-compound link and one short dusty civilian route ending at the bombing incident.
- Removed the highway-specific curbs, lane stripes, intersection crosswalk, shoulder slabs, and four remote road closures.
- Moved the crater, destroyed aid truck, fire, smoke, debris, barriers, fire light, and damaged corner composition onto the blocked route and retargeted the aftermath camera.
- Added two lower transition aprons so the local links remain grounded without creating another full-map road or floating district platform.
- Added a source-generation clearance gate covering local roads against buildings and large rocks, dunes, cliffs, hills, and boulders. `Logs/M01_R59_BlockedRouteClearance.log` reports `intersections=0`.
- The exporter retained the `132` current parity prefabs and removed `129` stale snapshots from the prior source layout; final scene/recipe validation passed in `Logs/M01_R60_BlockedRouteBuildValidate_CleanSnapshots.log`.
- Regenerated the exact recipe as `M01RuntimeVisualRecipe_2026-07-16_v16_blocked_market_route` under runtime marker `RuntimeCityM01BlockedRoute_R60_2026-07-16`.
- Editor and runtime manifests match at `10247` renderers, `11` lights, `71` particle renderers, and SHA-256 `B262FBD0329FBEF2CEFA1033A48FADB5277DEC095B1272643E429080DBB8F7B0`.
- Same-camera comparison measures perspective RGB MAE `0.011` and top-down RGB MAE `0.001`; direct inspection confirms the editor and runtime captures are visually identical.
- Runtime completed in `14.430s`; the full smoke completed in `18.826s` with `maxVisualBatchMs=9.272`, `16` budget yields, `0` suppressions, and one foundation. The worst batch passes the `<50ms` desktop R&D ceiling, while total time still exceeds the pilot target and remains an explicit post-visual-acceptance optimization task.
- Lifecycle recovery passed with one cancellation, one deterministic fallback, one attempt, `7017` recipe entries, and one foundation: `Logs/M01_R60_LifecycleRecovery.log`.
- Validation passed: runtime prototype build/scene validation, 28 runtime-city focused tests, and 15 production source-growth architecture tests.
- Evidence: `Logs/M01_R60_BlockedRouteBuildValidate_CleanSnapshots.log`, `Logs/M01_R60_BlockedRoutePlayMode_Graphics.log`, `Logs/M01_R60_FocusedTests.log`, `Logs/M01_R60_SourceGrowthTests_Retry.log`, `Logs/M01_EditorCurrentReference.png`, `Logs/M01_EditorCurrentTopDown.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- This is the first internally accepted owner-review candidate after the algorithmic rejection. No Android optimization or production adoption is implied.

### 2026-07-16 R64 Local Streets And Terrain Clearance Candidate

- Removed `27` active imported major-road objects across the three reused modules, including the remaining approximately `14 m`-wide compound road strip. The airfield/runway exclusion also removed `23` utility-module owners. Small authored dirt lanes remain inside the districts.
- Narrowed the authored asphalt route from `6.4 m` to `5.4 m`, narrowed the blocked civilian dirt route to `4.8 m`, added a dedicated dirt material, and retained source-generation road clearance as a hard zero-intersection gate.
- Added one continuous operation ground, three outer transition bands, three district aprons, and lower road/link shoulders so removed demo terrain cannot expose holes.
- Added editor-only terrain/structure diagnostics. A broad AABB scan recorded `238` contacts, most of which are intentional rock foundations or small architectural dressing; a stricter primary-structure rule identified and lowered `11` high-confidence penetrations. Generation now fails if any high-confidence penetration remains.
- Reduced the aftermath smoke scale and tightened the gameplay-overview framing. The editor source fingerprint is `F5E1A47D6F6611D7812C45018BFE194BE3B712C44CD56EAD4B393FEF63377E60`.
- Regenerated the exact recipe as `M01RuntimeVisualRecipe_2026-07-16_v17_local_streets_only` under runtime marker `RuntimeCityM01LocalStreets_R64_2026-07-16`.
- Editor and runtime manifests match at `10253` renderers, `11` lights, `71` particle renderers, and SHA-256 `56D8A3AAB7DF945A159281E768361C8BA09C84008464532BA0667EBE3DAE51C6`.
- Same-camera comparison measures perspective RGB MAE `0.026` and top-down RGB MAE `0.000`; direct inspection confirms the runtime captures reconstruct the corrected editor source.
- Runtime smoke passed with `7023` recipe entries, one foundation, `11.108 ms` worst visual batch, `27` budget yields, and `18.960s` total smoke time. The frame ceiling passes; total desktop generation time remains above the `5s` target and is intentionally deferred until owner visual review.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7023` recipe entries, and one foundation.
- All `28` focused runtime-city tests pass, including the passive-view/`SystemBase` architecture check. After rebasing onto the source-responsibility guardrail updates through `e5d173767`, the expanded production source-growth suite passes all `17` checks in `Logs/M01_R64_PostRebaseSourceGrowth.log`.
- Evidence: `Logs/M01_R64_TerrainClearanceGenerateCapture.log`, `Logs/M01_R64_LocalStreetsBuildValidate.log`, `Logs/M01_R64_LocalStreetsPlayModeSmoke.log`, `Logs/M01_R64_LifecycleRecovery.log`, `Logs/M01_R64_FocusedTests.log`, `Logs/M01_R64_PostRebaseSourceGrowth.log`, `Logs/M01_EditorCurrentReference.png`, `Logs/M01_EditorCurrentTopDown.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or owner visual acceptance is claimed.

### 2026-07-17 R65 Single Route And Civilian Edge Candidate

- Removed the two southbound local-road segments that still made the authored network converge as a cross. The remaining route is one bent Old Market-to-compound connection; asphalt width is reduced from `5.4 m` to `4.2 m`, and its total shoulder allowance is reduced from `2.4 m` to `1.6 m`.
- Regeneration excludes `27` imported major-road owners and `23` utility airfield/runway owners. The source-generation hard gate reports no active building or large-terrain intersection with the remaining local route.
- Added a restrained roadside civilian-aid edge with two refugee tents, medical props, supplies, one pole, and one warm point light. The exporter and editor manifest now share the same explicit story-layer list, preventing new authored groups from escaping parity evidence.
- Unified imported ground and horizon-dune renderer materials with the M01 palette, moved the three horizon dunes farther out and below grade, and preserved the continuous operation foundation. Direct capture review confirms the oversized plus/cross road is absent. Remaining open visual debt is district-island cohesion, central dead-zone balance, inherited building orientation, broad intentional terrain/foundation contacts, transition-band repetition, and final battle-camera framing.
- Regenerated the exact recipe as `M01RuntimeVisualRecipe_2026-07-16_v18_single_route_cohesion` under runtime marker `RuntimeCityM01SingleRoute_R65_2026-07-16`.
- Editor and runtime manifests match at `10260` renderers, `12` lights, `71` particle renderers, and SHA-256 `EBF407254001E148B08921C93C53A98B845C2F2534B09E25C29E96ACF373C3D4`.
- Same-camera comparison measures perspective RGB MAE `0.010` and top-down RGB MAE `0.000`; direct inspection confirms runtime reconstructs the corrected editor source.
- Runtime smoke passed with `7029` recipe entries, one foundation, `9.433 ms` worst visual batch, `8` budget yields, and `18.090s` elapsed generation. The desktop frame-batch ceiling passes; total generation remains above the `5s` pilot target and Android performance remains unmeasured.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7029` recipe entries, and one foundation. All `28` focused runtime-city tests, all `3` M01 source/evidence tests, and the post-rebase production source-growth suite's `17` checks pass.
- Evidence: `Logs/M01_R65_SingleRouteGenerateCapture_D3D11.log`, `Logs/M01_R65_SingleRouteBuildValidate_Retry.log`, `Logs/M01_R65_SingleRoutePlayModeSmoke_Retry2.log`, `Logs/M01_R65_LifecycleRecovery.log`, `Logs/M01_R65_FocusedTests.log`, `Logs/M01_R65_VisualSourceTests.log`, `Logs/M01_R65_PostRebaseSourceGrowth.log`, `Logs/M01_EditorCurrentReference.png`, `Logs/M01_EditorCurrentTopDown.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or owner visual acceptance is claimed.

### 2026-07-17 R66 Roadside Incident And Hard Clearance Candidate

- Retained the R65 circulation correction: one `4.2 m` bent Old Market-to-compound route with `1.6 m` total shoulder allowance. No plus-shaped autobahn, perpendicular crossing, southern branch, highway markings, or imported major-road/runway content was reintroduced.
- Relocated the bombing aftermath and civilian-aid props from the disconnected southern pocket to a road-adjacent civilian frontage, added overlapping frontage apron/transition ground, and retargeted the aftermath review/reveal cameras.
- Extended the source-generation road gate to authored solid story structures. The destroyed aid truck, bombed corner ruin, and both refugee tents now fail generation if their combined renderer bounds enter the route clearance corridor. `Logs/M01_R66_RoadClearanceAnalysis.log` reports `intersections=0 imported=0 authored=0`.
- Direct editor capture review confirms the route no longer dominates the map or crosses buildings. It also confirms this is not final composition acceptance: the utility, market, and residential modules still read as separated islands; the central dead zone, inherited building orientation, repeated transition silhouettes, and final battle-camera framing remain open Phase 4B work.
- Regenerated the exact recipe as `M01RuntimeVisualRecipe_2026-07-17_v19_roadside_incident` under the byte-ceiling-safe runtime marker `RuntimeCityM01RoadFix_R66_2026-07-17`.
- Editor and runtime manifests match at `10257` renderers, `12` lights, `71` particle renderers, and SHA-256 `979C209B0B13D5A4D185AE44B11C95EB28B9469C2D3941D68F59A86C7A6D1C31`.
- Same-camera comparison measures perspective RGB MAE `0.009` and top-down RGB MAE `0.000`; direct inspection confirms runtime reconstructs the corrected editor source.
- Final runtime smoke passed with `7026` recipe entries, one foundation, `8.610 ms` worst visual batch, `8` budget yields, and `17.924s` elapsed generation. The desktop frame-batch ceiling passes; total generation remains above the `5s` pilot target and Android performance remains deliberately unmeasured pending owner visual direction.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7026` recipe entries, and one foundation. All `28` focused runtime-city tests, all `3` M01 source/evidence tests, and all `17` production source-growth checks pass.
- Evidence: `Logs/M01_R66_RoadsideIncidentGenerateCapture.log`, `Logs/M01_R66_RoadClearanceAnalysis.log`, `Logs/M01_R66_RuntimeBuildValidate_Final.log`, `Logs/M01_R66_RuntimePlayModeSmoke_Final.log`, `Logs/M01_R66_LifecycleRecovery.log`, `Logs/M01_R66_RuntimeFocusedTests.log`, `Logs/M01_R66_VisualFocusedTests.log`, `Logs/M01_R66_PostRebase2SourceGrowth.log`, `Design/ArtReview/OperationMaps/M01/m01_gameplay_overview.png`, `Design/ArtReview/OperationMaps/M01/m01_top_down_plan.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or owner visual acceptance is claimed.

### 2026-07-17 R67 Support-Aware Terrain Candidate

- Preserved the R66 road correction: no plus-shaped autobahn, perpendicular crossing, southern branch, highway markings, or imported major-road/runway content. The only authored inter-district road remains a `4.2 m` two-segment route with `1.6 m` total shoulder allowance.
- Removed the Old Market, utility, and residential radial outer-transition/apron pairs. These six 28-segment ellipses amplified the reused modules into circular islands and did not provide structural support.
- Replaced the broad imported-ground material override with exact `(module, owner)` policy. Two utility ground owners proven to have no meaningful road/building support are disabled; retained road-only terrain uses `TransitionGround`, while identified Old Market building-support terrain retains `DistrictGround`. Generation reports exactly `2` unsupported-ground exclusions.
- Direct perspective and top-down inspection confirms the autobahn and generated circular halos are absent without introducing visible holes. It also confirms R67 is not overall visual acceptance: the north, east, and south modules remain too separated, so district placement and shared terrain continuity are the next Phase 4B slice.
- The source-generation road gate passes with `intersections=0 imported=0 authored=0`. The editor source fingerprint is `29ABF5963B8A3F6A1DAF10EA0064BADCC516662A451AC0CC842BBB9442BDE0A4`.
- Regenerated the exact recipe as `M01RuntimeVisualRecipe_2026-07-17_v21_support_aware_terrain` under runtime marker `RuntimeCityM01Terrain_R67_2026-07-17`; the exporter retained `134` parity snapshots and removed `30` stale snapshots.
- Editor and runtime manifests match at `10249` renderers, `12` lights, `71` particle renderers, and SHA-256 `4D07F1C39DE868726CCDDF1F33E9E814658450D7F200DC05572A3B18B9A77FC2`.
- Same-camera comparison measures perspective RGB MAE `0.006` and top-down RGB MAE `0.000`. Runtime smoke passed with `7018` recipe entries, one foundation, `13.885 ms` worst visual batch, `12` budget yields, and `18.771s` elapsed generation. The `<50 ms` desktop construction-batch ceiling passes; total generation remains above the `5s` target and is deferred until visual acceptance.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7018` recipe entries, and one foundation. All `28` focused runtime-city tests, all `3` M01 source/evidence tests, and all `17` production source-growth checks pass.
- Evidence: `Logs/M01_R67_SupportAwareTerrainGenerateCapture.log`, `Logs/M01_R67_SupportAwareRoadClearance.log`, `Logs/M01_R67_SupportAwareRuntimeBuildValidate.log`, `Logs/M01_R67_SupportAwareRuntimePlayModeSmoke_Final.log`, `Logs/M01_R67_SupportAwareLifecycleRecovery.log`, `Logs/M01_R67_RuntimeFocusedTests.log`, `Logs/M01_R67_VisualFocusedTests.log`, `Logs/M01_R67_SourceGrowthTests.log`, `Design/ArtReview/OperationMaps/M01/m01_gameplay_overview.png`, `Design/ArtReview/OperationMaps/M01/m01_top_down_plan.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or overall owner visual acceptance is claimed.

### 2026-07-17 R68 Dense District And Camera-Parity Candidate

- Preserved the hard no-autobahn constraint: no plus-shaped road, perpendicular crossing, southern branch, highway markings, or imported major-road/runway content. The remaining two-segment connection is shortened and presented as a `3.2 m` dusty local lane with `1.6 m` total shoulder allowance.
- Moved `UtilityCompound_East_DemoAuthored` from `x=49` to `x=23` and `Residential_South_DemoAuthored` from `z=-68` to `z=-54`. The Old Market-to-utility occupied-bounds gap falls from `32.58 m` to `6.58 m`; the Old Market/residential and utility/residential occupied bounds now touch. New hard gates reject meaningful primary-building overlap across reused modules and overlap between authored transition structures and reused primary buildings.
- Compacted `OldMarketUtilityGroundLink` from `90 x 34 m` to `56 x 28 m`, retained the continuous operation foundation, and added one bounded residential transition courtyard/house. The source-generation road gate reports `intersections=0 imported=0 authored=0`.
- Regenerated the shared editor scene as `M01VisualPrototype_2026-07-17_v23_dense_district_core` with source fingerprint `B96CAA5C7A1E5A4B3A091B9E0433BD6E918105A6FE0DF66081A527FF285CC71B`, then exported `M01RuntimeVisualRecipe_2026-07-17_v23_dense_district_core` under runtime marker `RuntimeCityM01Cohesion_R68_2026-07-17`.
- The first runtime smoke correctly failed because its capture camera still used the R67 target/FOV and top-down center. Runtime camera constants now match the accepted editor cameras. Final editor/runtime manifests match at `10251` renderers, `12` lights, `71` particle renderers, and SHA-256 `632CA84B2260F60D0D49D0773308121D8C5623FCD80CC37434A62FB9E7C1CF6A`.
- Final same-camera comparison measures perspective RGB MAE `0.020` with `0.026%` outliers and top-down RGB MAE `0.000`. Runtime smoke passed with `7020` recipe entries, one foundation, `7.519 ms` worst visual batch, `7` budget yields, and `18.121s` elapsed generation. The `<50 ms` desktop construction-batch ceiling passes; total generation remains above the `5s` target and is deferred until owner visual review.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7020` recipe entries, and one foundation. All `28` focused runtime-city tests, all `3` M01 source/evidence tests, and all `17` production source-growth checks pass.
- Evidence: `Logs/M01_R68_DenseCoreGenerateCapture_Iter7.log`, `Logs/M01_R68_DistrictLayoutAfter.log`, `Logs/M01_R68_DenseCoreRoadClearance.log`, `Logs/M01_R68_CameraParityRuntimeBuildValidate.log`, `Logs/M01_R68_CameraParityRuntimePlayModeSmoke.log`, `Logs/M01_R68_LifecycleRecovery.log`, `Logs/M01_R68_RuntimeCityFocusedTests.log`, `Logs/M01_R68_VisualMapFocusedTests.log`, `Logs/M01_R68_ProductionSourceGrowthTests.log`, `Design/ArtReview/OperationMaps/M01/m01_gameplay_overview.png`, `Design/ArtReview/OperationMaps/M01/m01_top_down_plan.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or overall owner visual acceptance is claimed.

### 2026-07-17 R69 Compact Civilian Block Review Candidate

- Rejected the oversized plus-shaped/autobahn composition as an M01 organizing device. The shared editor source now contains one `3.2 m` dusty market street in three connected segments, approximately `80.9 m` total length, with one gentle bend and no perpendicular crossing, highway markings, or southern branch.
- Utility-module curation now removes every imported road owner, including its long rectangular road-pad chain. It also removes airfield content, unsupported ground, and any imported building assembly intersecting the authored street. Generation reports `3` Old Market and `5` utility road-structure exclusions; the final hard road-clearance gate remains `0` imported and `0` authored intersections.
- Replaced the elongated utility island with a bounded `38 x 37 m` compound courtyard containing an operations hall, service building, guard tower, water tank, generator, supplies, checkpoint, lighting, and a compact perimeter wall/gate. The useful hand-authored Old Market and residential envelopes were retained after an internally rejected over-curated iteration proved visually sparse.
- Lowered and tightened the gameplay camera to emphasize the dense civilian block rather than empty module edges. The top-down review camera is also tightened for composition inspection. The regenerated source is `M01VisualPrototype_2026-07-17_v24_compact_civilian_block`, fingerprint `5347212FBC152C31CD6E066BD1CB61D9253342C649EE3BCE295A28EEA4E8500E`.
- Exported `M01RuntimeVisualRecipe_2026-07-17_v24_compact_civilian_block` under the on-screen/log marker `RuntimeCityM01Compact_R69_2026-07-17`. Editor and runtime manifests match at `10278` renderers, `12` lights, `71` particle renderers, and SHA-256 `9FCE4EE638F132211ADB86ABB6A28D72554777AA3F46E1AC78908C01ED542AFA`.
- Same-camera comparison measures perspective RGB MAE `0.012` and top-down RGB MAE `0.000`. Runtime smoke passed with `7037` recipe entries, one foundation, `8.638 ms` worst visual batch, `6` budget yields, and `17.191s` elapsed generation. This passes the `<50 ms` desktop construction-batch ceiling; total generation remains above the `5s` target and is deliberately deferred until owner visual review.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7037` recipe entries, and one foundation. The combined focused suite passed `48/48`: `28` runtime-city tests, `3` M01 visual-source/evidence tests, and `17` production source-growth architecture checks.
- Evidence: `Logs/M01_R69_Generate_04.log`, `Logs/M01_R69_RuntimeBuild.log`, `Logs/M01_R69_RuntimeSmoke.log`, `Logs/M01_R69_LifecycleRecovery.log`, `Logs/M01_R69_FocusedTests.xml`, `Design/ArtReview/OperationMaps/M01/m01_gameplay_overview.png`, `Design/ArtReview/OperationMaps/M01/m01_top_down_plan.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or owner visual acceptance is claimed.

### 2026-07-17 R71 Narrow Frontage Route Review Candidate

- Preserved the shared-source removal of the oversized plus-shaped/autobahn road and all imported major-road/runway content. Added a hard authored-route policy: every segment must be no wider than `3.2 m` and no longer than `36 m`; the final plan contains three connected dusty segments and no perpendicular crossing.
- Added three road-facing civilian houses, fences, carts, light poles, and two bounded courtyards across the central frontage. Existing road-clearance and transition-overlap gates include the new structures and report zero intersections.
- Identified the remaining foreground obstruction by source-owner projection rather than name guessing. A continuous frontage rule now removes penetrating rocks and oversized vegetation between the retained districts; Old Market, utility, and residential curation report `13`, `4`, and `30` frontage exclusions respectively, while the continuous authored foundation and courtyards prevent exposed ground.
- Regenerated the editor source as `M01VisualPrototype_2026-07-17_v26_narrow_frontage_route` with fingerprint `DE3E43200A459E4AE385F93E5575841FD564383B980889A2942752219D613416`, then exported `M01RuntimeVisualRecipe_2026-07-17_v26_narrow_frontage_route` under runtime marker `RuntimeCityM01NarrowRoute_R71_2026-07-17`.
- Editor and runtime manifests match at `10294` renderers, `12` lights, `71` particle renderers, and SHA-256 `4B91292B69F003B436BC8DF06DCB9CE8D26D5846A9879A0A1FDF54A5890B36AA`. Same-camera comparison measures perspective RGB MAE `0.016` with `0.023%` outliers and top-down RGB MAE `0.001` with `0.002%` outliers; direct inspection confirms the runtime reconstructs the corrected editor source.
- Runtime smoke passed with `7049` recipe entries, one foundation, `9.828 ms` worst visual batch, `9` budget yields, and `18.846s` total smoke time. The desktop construction-batch ceiling passes; total generation remains above the `5s` target and optimization is deliberately deferred until owner visual review.
- Lifecycle recovery passed with one cancellation, one deterministic missing-config fallback, one attempt, `7049` recipe entries, and one foundation. All `28` runtime-city focused tests, `3` M01 visual-source tests, and `17` production source-growth architecture checks pass (`48/48`).
- Evidence: `Logs/M01_R71_Generate_Gfx_03.log`, `Logs/M01_R71_RuntimeBuild.log`, `Logs/M01_R71_PlaySmoke.log`, `Logs/M01_R71_Lifecycle.log`, `Logs/M01_R71_FocusedTests.xml`, `Logs/M01_R71_SourceTests.xml`, `Design/ArtReview/OperationMaps/M01/m01_gameplay_overview.png`, `Design/ArtReview/OperationMaps/M01/m01_top_down_plan.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png`.
- No Android optimization, gameplay integration, production adoption, or owner visual acceptance is claimed.
