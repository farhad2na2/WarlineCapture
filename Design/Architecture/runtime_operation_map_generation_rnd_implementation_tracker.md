# Runtime Operation Map Generation R&D Implementation Tracker

**Status:** Active quality goal; R48 adds deterministic aftermath-anchor reserves, three-layer district-ground blending, and a compact frustum-validated loading presentation while retaining continuous terrain, grounded structures, occupied road terminals, and zero measured edge outliers across all three fixed seeds. Desktop generation, focused, architecture, naming-assertion, and scene-validation gates pass. Project-owner visual acceptance, cancellation/fallback policy, Android evidence, gameplay integration, and production adoption remain pending.

**Created:** 2026-07-15

**Workflow:** Direct work on `main`; no feature branch or pull request requested for this R&D

**Pilot:** M01 First Contact / Old Market visual exploration

**Reference scene:** `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity`

**Active goal:** Bring the M01 runtime-generated map to strong, visually accepted quality while making generation part of the player experience.

**Continuation:** Thread heartbeat `m01-runtime-map-quality` reopens this tracker every 30 minutes until all quality gates are complete. The heartbeat must stop after this tracker reaches Complete.

## 1. Objective

Prove whether Warline Capture can construct visually acceptable operation maps at runtime by extending the existing `RuntimeCity*` generator instead of creating a parallel map generator.

The experiment is motivated by three potential benefits:

- reduce per-map scene, bake, and serialized topology content in Android builds;
- support many deterministic map variants from a compact recipe and shared art palette;
- turn generation time into a visible loading experience rather than hiding a long blocking step.

The current M01 editor prototype is the mandatory first visual-quality target. Early technical slices may be visibly incomplete while the adapters are being proven, but the initial R&D goal is not complete until runtime output reaches the editor prototype's overall composition, density, material coherence, landmark readability, and environmental storytelling. After that parity gate passes, the generator must improve the known weaknesses in the editor map rather than treating that scene as the final quality ceiling.

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
            -> compact accepted-editor transform/prefab recipe
            -> three district module records with bounded child paths for incremental realization
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

### Phase 4 - Visual Quality Iterations

- [x] Capture the first runtime output from the M01 review camera.
- [x] Compare macro silhouette, road hierarchy, density, palette, landmarks, and story beats with the accepted M01 editor scene.
- [x] Extract the accepted editor composition into 248 standalone entries plus three compact, incrementally realized district module records as a temporary parity bridge.
- [x] Replace uniform placement with reviewed district intent using extensions to existing planner inputs.
- [x] Add Old Market zones: market, authority/utility compound, damaged corridor, residential edge, and horizon closure.
- [x] Curate prefab palettes and repetition limits without changing shared prefab assets.
- [x] Add deterministic grouped clutter and damage storytelling.
- [x] Match the editor prototype's market cluster, utility compound, damaged corridor, road hierarchy, horizon closure, and three primary visual anchors in the accepted runtime seed.
- [x] Review at least three fixed seeds before accepting a rule change.

**Exit:** The project owner accepts runtime output as reaching the editor prototype's visual-quality level. A merely functional or generically populated city does not pass.

### Phase 4B - Improve Beyond The Editor Prototype

- [x] Record every visual issue still present in the accepted editor prototype.
- [x] Classify each issue as a reusable generator rule, M01 authored constraint, palette problem, or presentation problem.
- [x] Guarantee a continuous, visibly readable ground surface beneath the full playable composition and camera footprint.
- [x] Reject or relocate large rocks, dunes, hills, and other terrain dressing that intersects structures or primary roads.
- [x] Add coherent district aprons, road shoulders, and terrain transitions so modules do not read as disconnected islands.
- [ ] Rebalance dense clusters and dead zones while retaining clear landmark silhouettes and tactical breathing room.
- [x] Orient visual-only generated buildings toward their road circulation intent where the source prefab allows it.
- [x] Improve horizon closure and framing without placing large silhouettes behind or through foreground structures.
- [x] Keep the accepted seed deterministic and publish surface/clearance validation counts in the runtime evidence.
- [x] Fix reusable issues in generator/config logic rather than hand-adjusting one generated run.
- [ ] Improve district transitions, repetition control, road edges, building orientation, empty areas, horizon composition, and damaged-area storytelling.
- [x] Regenerate the accepted seed and the three-seed review set after each placement/story rule change; presentation-only R45 reproduced the R44 accepted-seed transform hash.
- [x] Prevent improvements from reducing determinism, generation responsiveness, or cleanup reliability.

**Exit:** Runtime M01 is visibly stronger than the original editor prototype and the improvements generalize to future runtime maps where appropriate.

### Phase 5 - Loading Experience

- [x] Define a recipe-owned camera path and reveal order while construction runs.
- [x] Give each visible stage a readable minimum duration and reveal terrain, roads, anchors, districts, story dressing, and horizon in that order.
- [x] Expose player-facing stage labels independently from the technical diagnostics text.
- [x] Keep first visible geometry under 250 ms while allowing the complete high-quality assembly to take multiple seconds.
- [ ] Replace technical overlay with loading-shell progress events after visual acceptance.
- [x] Prioritize ground, primary roads, skyline anchors, and near-camera districts.
- [x] Add a measured frame-time budget in addition to the fixed item-count safety cap.
- [ ] Add cancellation when the player leaves loading.
- [ ] Add failure recovery and a deterministic fallback map policy.

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
| Deterministic regeneration | Compact recipe passed byte-for-byte regeneration with SHA-256 `B474CB9B3B95EA1116481F5056DB37386113EDC0D056DC690E39B42EC69ADF8A`; editor fingerprint remains `07FF5A66618BDCE2C662DD0EAF5C7C5E21373B4A670182C4E7A5EB9BD7EB42EC` |
| Runtime warnings targeted by R19 | no generation exceptions, missing ground, large district-rock penetrations, or negative-scale collider warnings |
| Visual evidence | `Logs/M01_RuntimeGenerationReveal_Market.png`, `Logs/M01_RuntimeGenerationBaseline.png`, and `Logs/M01_RuntimeGenerationTopDown.png` |

These desktop measurements pass the initial first-geometry and total-generation budgets. Android, repeated-run determinism, frame-time percentile, memory, and build-size measurements remain open.

### Current Algorithmic Evidence

| Measurement | R48 current presentation and three-seed placement/story baseline |
|---|---|
| Runtime-generation version | `RuntimeCityRuntimeGeneration_R48_2026-07-16` |
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
| Repeat determinism | R45 independently reproduced its complete seed-01 fingerprint and counts. R48 preserves deterministic ordering and passes fixed-seed structural checks; the formal 10-run R48 gate remains open. |
| Loading presentation | Six serialized camera poses; player-facing stage labels; aftermath hold `2.5s`; reveal stays active through Horizon; a compact top status band fits inside the 38-degree camera frustum; formatted debug text changes only with displayed stage/count data. Evidence capture primes temporal render history and emits separate loading, clean perspective, and top-down images. |
| Focused and scene validation | R48 prototype build and scene validation passed; 25 of 25 runtime-city focused tests passed. |
| Broad regressions | 19 of 19 SystemBase/passive-view tests passed. The broad runtime naming assertion passed 1 of 1, followed by a reproducible Unity 6000.5.2f1 batch-shutdown crash after the pass marker; the assertion result is green but the process-exit defect remains recorded. The accepted-recipe smoke remains green from R45 with 7,099 entries, 10,279 renderers, 202 clearance suppressions, 13.058 ms worst visual batch, and 41 budget yields. |
| Performance/GC source audit | R46 trims and reserves anchor slots once at generation completion without LINQ or temporary collections. R47 creates four additional bounded 28-segment district surfaces once per generation. R48 capture-history priming is editor-only, while runtime status formatting remains change-gated. No recurring frame-path allocation was added; Android managed-GC, native-memory, retained-memory, and instantiation-spike attribution gates remain open. |
| Visual evidence | Loading/perspective/top-down PNGs are `Logs/M01_AlgorithmicSeed_<seed>_{Loading,Perspective,TopDown}.png`. Perspective SHA-256: seed 01 `9DFD33C826BE136A3B9D156AEA0DF1B7C24A4875E3CC848076BFAFD86AD5E597`, seed 02 `352C49EC4BE902741F611F5AB08DC19EBB5250EB7C3263A6D99A8656078B6FCF`, seed 03 `385C887257EB508BFACDF28DFC206FAD3C5FAEA726300C0C1BEE133C71922B85`. Top-down SHA-256: seed 01 `49D63C72827EFBDB22335A97CB67F63D8BE86F7A895C5349465453F82F85A5C8`, seed 02 `0D11B59CB673F84512A64BE8F0861B35BA9917C3E7554D41948C702B6DC34058`, seed 03 `4F94410332880DEC481EFA08FFF03C3F9073267B95342CA1AEB4344D81991C7F`. Internal review accepts this as a stable feedback build; project-owner visual acceptance remains open. |

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

1. [x] Diagnose occupancy and add a bounded visual-only terminal-branch policy. R36-R41 reduce the fixed seed from 108 to 93 road cells; all 14 terminal branches have nearby occupation and production road planning remains on the default policy.
2. [x] Relocate the two extreme damage structures and the exposed east-yard satellite. R41 reports `edgeOutliers=none/0` while retaining 69 visual buildings and a southwest damage centroid.
3. [x] Make aftermath readable through a recipe-owned algorithmic aftermath camera/reveal pose and bounded district fallback anchors. R45 evidence keeps the damage apron visible through its `2.5s` hold.
4. [x] Regenerate loading, clean perspective, and top-down evidence for fixed seeds `26071501`, `26071502`, and `26071503`. R48 passes structural checks, retains occupied terminal branches, reports zero measured edge outliers, and has been visually inspected.
5. [x] Add algorithmic reveal choreography and player-facing loading progress while preserving `RuntimeCityRAndDMapSystem` as the only frame owner. R48 keeps runtime overlay formatting change-gated and validates the compact status band against the capture frustum.
6. [x] Run the broad SystemBase/passive-view and runtime naming suites plus the accepted-recipe smoke to prove no regression outside algorithmic mode. R48 passes 19/19 architecture assertions and 1/1 naming assertion; Unity's post-pass batch-shutdown crash is tracked separately from the green assertion result.
7. [ ] Obtain project-owner visual acceptance, then add cancellation/fallback policy and profile Android generation, memory, frame spikes, GC, thermals, and package-size savings before proposing production integration.
