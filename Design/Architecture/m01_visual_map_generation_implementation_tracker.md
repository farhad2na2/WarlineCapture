# M01 Visual Map Generation Implementation Tracker

**Status:** M01 visual prototype generated and validated; project-owner visual acceptance pending

**Created:** 2026-07-15

**Workflow:** Pull request with independent review

**Pilot map:** M01 First Contact / Old Market
**Program target:** 12 unique, reviewed, editor-baked operation maps for the current chapters

## 1. Purpose

This tracker defines the first visual-quality iteration of the M01 operation map and the reusable editor-generation foundation that can later support the other 11 current-chapter maps.

The first iteration is an **editor-viewable visual prototype only**. It does not need to load through the campaign, support units, satisfy navigation, run mission logic, or be playable. Its purpose is to let the project owner judge whether the composition, density, scale, lighting, and environmental storytelling are good enough to become the base for M01.

The map must be authored and reviewed as a stable scene result. Procedural logic may accelerate authoring, but approved campaign maps must be baked in the editor and must not generate randomly at runtime.

## 2. Authority And Dependencies

This tracker is subordinate to the following project contracts:

- [3D Single-Map Gameplay Direction](../3D_SingleMap_Gameplay_Direction.md)
- [M01 First Contact Production Contract](../M01_FirstContact_Production_Contract.md)
- [M01 Metric Scale And Readability Contract](../M01_Metric_Scale_Readability_Contract.md)
- [Campaign Mission High-Level Design Catalog](../Campaign_Mission_High_Level_Design_Catalog.md)
- [Operation Map Scene Split And Generator Tracker](operation_map_scene_split_and_generator_tracker.md)
- [Agent Pull Request Review And Merge Workflow](agent_pull_request_review_merge_workflow.md)

The 2026-07-11 hold in the M01 production contract remains authoritative. This tracker permits planning, generator investigation, and an isolated art prototype. It does not authorize player-facing M01 implementation, campaign integration, or promotion into the canonical operation-map pipeline before the hold is released.

## 3. Accepted Direction

- Build M01 as a dedicated Old Market map, not as a crop of the current large Match map.
- Use an authored, deterministic editor generator to accelerate placement and iteration.
- Save and review the generated result as a normal Unity scene.
- Use [Match.unity](../../Assets/Game/Scenes/Match.unity), [Demo.unity](../../Assets/Game/Scenes/Demo.unity), and [Demo2.unity](../../Assets/Game/Scenes/Demo2.unity) as read-only quality and asset references.
- Use the [M01 Candidate B concept](../NarrativeVision/FirstLaunch/ArtReview/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-04_M01Handoff_CandidateB.png) as the primary composition target.
- Reuse the useful planning portions of the old runtime city generator as a starting point.
- Do not reactivate the old generator as a runtime campaign dependency.
- Do not duplicate the three reference scenes or copy their generated scene chunks.
- Share a visual grammar and generator modules across 12 maps while giving every physical map an authored recipe, seed, landmarks, palette, and silhouette.

## 4. Parallel Ownership Boundary

Another task owns operation-map loading, scene separation, Addressables integration, and the canonical map runtime contract. This work must stay independent until both tracks are ready for an explicit integration review.

### Owned By This Track

- M01 visual brief and acceptance checklist.
- Isolated M01 visual prototype scene.
- Prototype-only editor generator and recipe.
- Reusable deterministic planning extraction or adapter, limited to what the prototype needs.
- M01 review captures and visual iteration log.
- A follow-up proposal for scaling the accepted method to the remaining 11 maps.

### Read-Only References

- `Assets/Game/Scenes/Match.unity`
- `Assets/Game/Scenes/Demo.unity`
- `Assets/Game/Scenes/Demo2.unity`
- Existing prefabs, materials, terrain assets, lighting profiles, and VFX used by those scenes.
- The old runtime city generator and its tests until a reviewed extraction is approved.

### Excluded From This Track

- `Match.unity`, `MatchSubScene`, and current production map content.
- `GeneratedStaticMapPresentation` and `GeneratedCombinedMeshes`.
- Addressables groups, map catalog loading, scene resolver, streamer, and loading shell.
- Static map baking, navigation baking, minimap baking, occlusion baking, and gameplay blockers.
- Mission logic, objective placement, unit spawns, combat encounters, and HUD integration.
- Jenkins or other CI pipeline changes.
- Runtime city startup, readiness, minimap, or loading-gate behavior.

Any required edit outside the owned paths must stop for an ownership check with the operation-map architecture task.

## 5. Visual Target For M01

### Narrative Read

M01 is the player's first Old Market combat space after the bombing and blackout. Before gameplay is added, the environment should already communicate a civilian district disrupted by a recent hostile event.

### Required Composition

- A strong central road intersection or corridor that immediately organizes the scene.
- A dense market cluster with courtyards, awnings, stalls, walls, service alleys, and small props.
- A fortified utility or authority compound across or beside the main corridor.
- One damaged or obstructed lane with wreckage, fire, smoke, debris, or emergency response dressing.
- At least three distinct visual anchors that let a player identify location and facing without UI.
- Sparse palms and vegetation used as accents, not as uniform scatter.
- A coherent desert edge and mountain or elevated horizon treatment that closes the map visually.
- Clear 1.8 m soldier and approximately 2.3 m door scale throughout the prototype.

### Quality Bar

The prototype should approach the density, hand-authored rhythm, material coherence, prop layering, and lighting quality visible in Match, Demo, and Demo2. The older sparse terrain-mask output may provide technical lessons but is not the direct visual target.

The first accepted pass must include three levels of authored detail:

1. **Macro:** road hierarchy, district silhouette, compounds, horizon, and focal axes.
2. **Meso:** building clusters, walls, alleys, courtyards, market groups, wreck cluster, and vegetation groups.
3. **Micro:** signs, cables, crates, barriers, cloth, trash, decals, rubble, small lights, and story props concentrated around focal areas.

Uniform random scatter is not sufficient for the meso or micro pass.

## 6. Legacy Runtime City Generator Assessment

### Current State

The legacy generator is orchestrated by [RuntimeCityCompositionSystemHelper.cs](../../Assets/Game/Scripts/Environment/RuntimeCityCompositionSystemHelper.cs) and [RuntimeCityGenerationCompositionSystemHelper.cs](../../Assets/Game/Scripts/Environment/RuntimeCityGenerationCompositionSystemHelper.cs). Its current config, [Game_RuntimeCitySpawner_Config.asset](../../Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset), has `cityCount: 0`, so the old city path is presently disabled.

It uses a deterministic `Unity.Mathematics.Random` seed and already separates several planning states from presentation bridges. Existing focused tests cover city-center exclusion handling and connected town-road/autobahn planning in [RuntimeCityGenerationFocusedTests.cs](../../Assets/Tests/Editor/RuntimeCityGenerationFocusedTests.cs).

### Candidate Logic To Reuse Or Extract

| Area | Existing starting point | Prototype use |
|---|---|---|
| Deterministic seed and config snapshot | `RuntimeCityConfigCompositionSystemHelper` | Stable regeneration and reviewable recipes |
| City bounds and center planning | `RuntimeCityLayoutState` | Clamp the authored district into prototype bounds |
| Road strokes and connected exits | `RuntimeCityRoadLayoutState` | Generate a base road graph before authored overrides |
| City ingress and chained districts | `RuntimeCityIngressUtilitySystemHelper`, `RuntimeCityChainUtilitySystemHelper` | Optional secondary street/corridor planning |
| Roadside and corridor plots | `RuntimeCityBuildingPlotState` | Produce candidate building frontage positions |
| Bulk plot categories | `RuntimeCityBulkPlotPlanUtilitySystemHelper` | Separate central, roadside, rural, and decoration candidates |
| Prefab selection and footprint cache | `RuntimeCityPrefabSelectionState` | Deterministic selection from curated M01 palettes |
| Reserved footprints and spacing | `RuntimeCityWalkabilityState` | Prevent obvious overlaps during visual generation |
| Landmarks and civic center | landmark, hall, archway helpers | Seed authored focal anchors |
| Walls, gates, and yards | yard-wall and gate planning helpers | Build market and compound boundaries |
| Decoration grouping | decoration group and scatter helpers | Starting candidates for curated prop clusters |

### Logic That Must Not Become The Editor Generator Core

- Coroutine lifecycle and `generationYieldInterval` frame scheduling.
- Runtime startup and loading-readiness checks.
- Deferred road ECS synchronization and runtime road commits.
- Runtime building spawn/delete bridges and gameplay building health.
- Minimap publication and runtime read-model events.
- Runtime surface mutation and dependencies on Match scene singletons.
- Automatic random bulk decoration without authored clustering and review overrides.

### Required Reuse Rule

Phase 1 must classify each dependency as one of:

- **Reuse directly:** pure deterministic planning with no scene/runtime side effects.
- **Extract:** valuable algorithm currently coupled to runtime context.
- **Adapt:** useful result, but output must be converted into an editor plan or curated placement.
- **Reject:** runtime-only or visually unsuitable behavior.

The editor tool should produce an intermediate plan first, then apply that plan to scene objects. This keeps layout testing independent from prefab instantiation and makes regeneration auditable.

## 7. Implemented Prototype Structure

The visual spike stays inside isolated prototype and review paths and does not modify the canonical operation-map pipeline:

- Prototype scene: `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity`
- Editor generator: `Assets/Game/Scripts/Editor/MapPrototypes/M01VisualMapPrototypeBuilder.cs`
- Review, lighting, capture, and semantic evidence partial: `Assets/Game/Scripts/Editor/MapPrototypes/M01VisualMapPrototypeBuilder.Review.cs`
- Focused tests: `Assets/Tests/Editor/M01VisualMapPrototypeBuilderTests.cs`
- Generated M01 materials/profile: `Assets/Game/Art/MapPrototypes/M01/`
- Editor menus: `Game/Operation Maps/M01/Generate Visual Prototype` and `Generate And Capture Visual Prototype`
- Captures and manifest: `Design/ArtReview/OperationMaps/M01/`

For this visual-only iteration, the recipe is deliberately encoded in the versioned builder constants and curated placement methods rather than introducing a shared runtime/editor recipe model before the parallel operation-map architecture is settled. A formal recipe asset and pure shared plan model remain Phase 8 follow-up work if this visual method is accepted.

Generated scene objects live under `_M01VisualGenerated`; story overrides live under `_M01AuthoredStoryOverrides`; review cameras live under `_M01ReviewCameras`. Regeneration replaces only the isolated prototype scene and its owned generated artifacts.

The current in-code recipe records the generator version, seed, curated palettes, zones, authored placements, density loops, exclusions, lighting, and review cameras. A future formal recipe asset should additionally record:

- Generator schema version and deterministic seed.
- Prototype bounds, grid scale, street hierarchy, and map edge treatment.
- Curated prefab/material/VFX palettes by category.
- Required landmark slots and authored placement overrides.
- Density targets by district or zone.
- Rotation, scale, spacing, and repetition limits.
- Terrain, vegetation, lighting, weather, and time-of-day choices.
- Disabled categories and explicit exclusions.

## 8. Implementation Phases

### Phase 0 - Scope And Baseline

- [x] Confirm first iteration is visual-only and nonplayable.
- [x] Confirm 12 unique editor-baked maps as the program direction.
- [x] Record the operation-map architecture ownership boundary.
- [x] Audit Match, Demo, Demo2, the M01 concept, and relevant design contracts.
- [x] Perform an initial source audit of the old runtime city generator.
- [ ] Receive independent review and merge approval for this tracker.

**Exit:** The tracker is merged without changing runtime or scene content.

### Phase 1 - Generator Reuse Spike

- [x] Build a dependency map from generation orchestration to pure planning and runtime side effects.
- [x] Complete the reuse/extract/adapt/reject matrix for the legacy helpers considered by the prototype.
- [ ] Define a serializable `VisualMapPlan` independent from GameObjects and runtime systems.
- [x] Prove deterministic visual-plan output for the fixed M01 recipe and seed through a semantic scene fingerprint.
- [x] Add focused editor checks for required assets, owned scene roots, anchors, density, captures, and manifest identity.
- [x] Keep the spike in `Game.Editor`; do not extract shared runtime code before visual acceptance and architecture reconciliation.

**Implementation note:** Pure legacy planners were audited and remain viable future inputs, but the first visual prototype adapts their deterministic-seed and zoning concepts without invoking `RuntimeCityCompositionSystemHelper` or any runtime startup, ECS road, minimap, or loading-readiness behavior. Unity scene YAML is not byte-stable because fresh local file IDs are assigned during reconstruction, so determinism is validated through the stable semantic fingerprint instead.

**Exit:** A reviewed technical spike can produce a deterministic M01 plan without starting Match runtime systems.

### Phase 2 - M01 Authored Blueprint

- [x] Lock prototype dimensions and review-camera scale references.
- [x] Mark the central road, market, utility compound, damaged lane, map entrance, and horizon zones.
- [x] Choose at least three primary visual anchors.
- [x] Create curated M01 prefab palettes from the reference scenes.
- [x] Define density and repetition limits per zone.
- [x] Record authored story layers separately from generated district modules.

**Exit:** A top-down blueprint and palette review clearly match the Old Market concept before detailed generation starts.

### Phase 3 - Editor Generator Foundation

- [x] Create the isolated prototype scene; keep the first recipe version in editor-only code.
- [x] Add editor commands under the `Game` menu hierarchy.
- [x] Generate into clearly separated generated, authored-story, lighting, and review-camera roots.
- [x] Preserve prefab links and deterministic transforms.
- [ ] Add non-destructive preview, clear-owned-output, and Undo workflows if the prototype is promoted.
- [x] Rebuild only the isolated prototype scene and owned artifact paths.
- [x] Store generator version, seed, and semantic identity in the scene and capture manifest.

**Exit:** Repeated generation is semantically deterministic, scoped to the isolated scene, and leaves reference and canonical map content untouched. Undo and in-place owned-root regeneration remain promotion work.

### Phase 4 - Macro And Meso Composition

- [x] Generate the road hierarchy and terrain/map boundary.
- [x] Place market, compound, residential/service, and damaged-lane clusters.
- [x] Add walls, gates, alleys, courtyards, and landmark silhouettes.
- [x] Establish dune/horizon closure and intentional map-edge sightlines.
- [x] Remove obvious grid monotony and repeated prefab patterns through authored Demo modules and curated placement.
- [x] Retain the scale of the hand-authored Demo modules and existing production prefabs.

**Exit:** The map reads as M01 Old Market from gameplay height even before micro dressing.

### Phase 5 - Visual Storytelling Pass

- [x] Add curated market props, cloth, signs, crates, cables, and service clutter.
- [x] Build the damaged-lane wreck, rubble, fire, smoke, and emergency detail cluster.
- [x] Dress the utility compound with a distinct security and logistics vocabulary.
- [x] Add sparse vegetation and regional accents in intentional groups.
- [x] Add custom road, curb, paint, sand, concrete, turquoise, and rust materials to break visible repetition.
- [x] Balance focal density against intentional visual breathing room.

**Exit:** Screenshots approach the reference-scene density and look hand-authored rather than uniformly procedural.

### Phase 6 - Lighting And Review Captures

- [x] Match the warm desert visual direction without flattening material contrast.
- [x] Establish readable focal lighting for market, compound, and damaged lane.
- [x] Validate smoke, fire, and local lights from gameplay height.
- [x] Capture top-down plan, gameplay overview, Old Market approach, and bombing-aftermath views.
- [ ] Add a separate 20:9 review set if required after project-owner visual review; the current evidence is 16:9 without HUD.
- [x] Record Unity version, generator version, seed, scene path, semantic fingerprint, and capture identity in the generated manifest.

**Exit:** A reproducible capture set is ready for project-owner visual review.

### Phase 7 - Human Visual Acceptance

- [x] Complete an internal concept-fidelity review against Candidate B.
- [x] Complete an internal quality review against Match, Demo, and Demo2 references.
- [x] Record and resolve concrete composition, horizon, smoke, lighting, exposure, and camera-framing issues across six generator revisions.
- [x] Regenerate and iterate without modifying reference scenes or runtime loading content.
- [ ] Obtain explicit visual acceptance from the project owner.

**Exit:** The project owner accepts the prototype as the visual base for M01.

### Phase 8 - Twelve-Map Production Contract

- [ ] Record which generator modules and palettes proved reusable.
- [ ] Define one authored recipe per remaining physical map.
- [ ] Assign mission-to-map relationships without assuming one unique physical map per scenario.
- [ ] Define visual identity, landmarks, palette, and seed for all 12 maps.
- [ ] Estimate hand-authored override effort per map.
- [ ] Define per-map review captures and acceptance ownership.
- [ ] Split future work into small map-specific pull requests.

**Exit:** The 12-map plan preserves uniqueness while reusing the accepted editor-generation foundation.

### Phase 9 - Deferred Promotion And Integration

- [ ] Confirm the M01 implementation hold has been released.
- [ ] Reconcile prototype paths and data with the accepted operation-map architecture.
- [ ] Promote accepted scene content into the canonical M01 operation map.
- [ ] Add navigation, blockers, mission anchors, bakes, Addressables, and loading integration in separately reviewed work.
- [ ] Remove or archive prototype-only tooling only after canonical output is verified.

**Exit:** M01 becomes a production operation map through the canonical pipeline. This phase is explicitly outside the first visual iteration.

## 9. Visual Acceptance Matrix

| Area | Pass condition | Reviewer |
|---|---|---|
| Concept fidelity | Old Market, central corridor, market, compound, damaged lane, and desert horizon read immediately | Project owner |
| Composition | Primary focal route and three anchors are clear from gameplay height | Project owner |
| Scale | Soldier, doors, roads, walls, props, and buildings feel mutually consistent | Project owner / design |
| Density | Macro, meso, and micro layers approach Match/Demo/Demo2 quality | Project owner / art |
| Repetition | No dominant repeated grid, prefab row, identical rotation, or uniform scatter pattern | Art review |
| Storytelling | Recent bombing/blackout aftermath is visible without explanatory UI | Project owner / narrative |
| Lighting | Focal zones remain readable on target mobile framing | Art review |
| Edges | Map boundaries and horizon look intentional in all review views | Art review |
| Determinism | Same recipe, seed, and generator version produce the same plan and transforms | Engineering |
| Isolation | Prototype generation changes only owned prototype paths and roots | Engineering |

Visual acceptance does not imply gameplay, navigation, performance, or loading acceptance.

## 10. Twelve-Map Rollout Register

The exact mission-to-map mapping remains owned by the operation-map architecture and campaign catalog work. This register tracks physical-map production only.

| Physical map | Identity | Recipe | Visual prototype | Visual approval | Production promotion |
|---|---|---|---|---|---|
| 01 | M01 Old Market | In-code recipe v6, seed `26071501` | Generated and validated | Project-owner review pending | Blocked by M01 hold and integration readiness |
| 02 | To be assigned | Not started | Not started | Not started | Not started |
| 03 | To be assigned | Not started | Not started | Not started | Not started |
| 04 | To be assigned | Not started | Not started | Not started | Not started |
| 05 | To be assigned | Not started | Not started | Not started | Not started |
| 06 | To be assigned | Not started | Not started | Not started | Not started |
| 07 | To be assigned | Not started | Not started | Not started | Not started |
| 08 | To be assigned | Not started | Not started | Not started | Not started |
| 09 | To be assigned | Not started | Not started | Not started | Not started |
| 10 | To be assigned | Not started | Not started | Not started | Not started |
| 11 | To be assigned | Not started | Not started | Not started | Not started |
| 12 | To be assigned | Not started | Not started | Not started | Not started |

## 11. Validation

### Tracker Pull Request

- Confirm every referenced repository path exists in `HEAD`.
- Confirm this tracker does not override the M01 hold or operation-map architecture tracker.
- Confirm no scene, asset, config, runtime, Addressables, or CI file changed.
- Run `git diff --check`.
- Require independent review before merge.

### Future Prototype Pull Requests

- Unity editor compile has no new errors.
- Focused EditMode tests pass.
- Fixed recipe and seed produce deterministic plan and transform snapshots.
- Semantic regeneration fingerprint remains unchanged when the recipe, seed, assets, and generator version are unchanged.
- Prefab references remain valid and no assets are duplicated from reference scenes.
- Generator edits only the owned prototype root.
- Scene opens without missing scripts, materials, shaders, or prefab references.
- Review captures are attached with recipe version, seed, and commit.
- `git diff --check` passes.

Full Android builds are not required for the isolated visual gate. They become required after promotion into the canonical map/loading path.

### M01 Prototype Evidence - 2026-07-15

| Check | Result |
|---|---|
| Unity generation and capture | Passed in Unity `6000.5.2f1`; `[M01VisualMap] result=Passed` |
| Generator identity | `M01VisualPrototype_2026-07-15_v6`, seed `26071501` |
| Semantic regeneration | Passed; fingerprint `962F4AE1EF66319621E41C120F1CEDB200F94B6B452B3CE6089606FF4A426B36` |
| Focused Editor validation | Passed 3 checks: asset palette, scene structure/density, and complete capture set/manifest |
| Architecture boundary | Passed 31 checks; Unity crashed during shutdown after emitting the pass marker, with no failed architecture assertion |
| Source-growth policy | M01 files are split to 600 and 434 lines, but the repository-wide gate remains red on four pre-existing unrelated `*SystemHelper.cs` paths in narrative/materials code |
| Capture readability | Overview `137.6`, Old Market `135.7`, aftermath `110.4`, top-down `106.2` average luminance; automated minimum is `45` |
| Review artifacts | Four `1600x900` PNGs, one `3254x1854` contact sheet, and generated manifest under `Design/ArtReview/OperationMaps/M01/` |
| Reference-scene isolation | `Match.unity`, `Demo.unity`, `Demo2.unity`, runtime map loading, Addressables, mission logic, and Jenkins remain untouched |

The raw `.unity` file SHA is not used as the determinism contract because Unity assigns new local serialization IDs when a scene is reconstructed. The semantic fingerprint covers hierarchy paths, prefab identities, transforms, material assets, camera settings, and lights.

## 12. Risks And Open Decisions

| Risk or decision | Current position | Resolution gate |
|---|---|---|
| Legacy planning code is coupled to runtime context | The prototype does not call runtime composition; reconsider pure planner extraction only after visual acceptance | Phase 8 generator contract |
| Pure shared-plan assembly ownership is undecided | Keep this spike editor-only until operation-map architecture is reconciled | Phase 8 review |
| Prototype dimensions and framing may need gameplay adjustment | Current dimensions are visually locked only; gameplay metrics remain outside this gate | Production promotion review |
| Reference-scene palette may be too broad | Current output uses three curated Demo-authored modules plus focused M01 story props | Project-owner visual review |
| Generated output may still need owner-directed art changes | Preserve the deterministic recipe and make changes as explicit composition/dressing tasks | Visual acceptance |
| Prototype paths may conflict with parallel architecture work | Keep all work under isolated prototype paths until explicit integration review | Before Phase 9 |
| M01 production hold remains active | Keep work isolated and non-player-facing | Phase 9 entry |
| Twelve maps may drift toward one visual identity | Give each physical map a distinct recipe, palette, silhouette, and acceptance set | Twelve-map contract review |

## 13. Progress Summary

| Phase | Status | Notes |
|---|---|---|
| 0. Scope and baseline | Complete in tracker branch | Independent merge approval for the original tracker remains separate |
| 1. Generator reuse spike | Complete for visual spike | Runtime orchestration rejected; deterministic concepts adapted in editor-only tooling |
| 2. M01 authored blueprint | Complete | Old Market, compound, damaged lane, roads, anchors, and dunes are locked for owner review |
| 3. Editor generator foundation | Complete for visual gate | Formal recipe asset, preview, clear, and Undo remain promotion work |
| 4. Macro and meso composition | Complete for visual gate | Three Demo-authored district modules plus M01-specific roads and zones |
| 5. Visual storytelling pass | Complete for visual gate | Market micro-detail, utility vocabulary, and bombing aftermath generated |
| 6. Lighting and captures | Complete for 16:9 gate | Deterministic four-view review set and luminance guard; 20:9 deferred |
| 7. Human visual acceptance | Awaiting project owner | Internal visual iteration complete; owner is final approver |
| 8. Twelve-map production contract | Deferred | Begins after the M01 method is accepted |
| 9. Promotion and integration | Blocked | Requires hold release and architecture readiness |

## 14. Implementation Log

| Date | Change | Result |
|---|---|---|
| 2026-07-15 | Audited design contracts, reference scenes, M01 concept, and legacy runtime city generator; created this implementation tracker | Planning baseline ready for independent review; no runtime or scene implementation started |
| 2026-07-15 | Audited legacy generator boundaries and the Demo, Demo2, and Match visual palettes | Pure deterministic planner concepts retained; runtime composition, ECS, minimap, and loading dependencies rejected for this spike |
| 2026-07-15 | Implemented isolated editor-only M01 builder, curated material/profile assets, scene roots, menus, and capture pipeline | Reproducible visual scene generated without touching canonical scenes or map loading |
| 2026-07-15 | Iterated mountains, district density, south composition, smoke ownership, lighting, exposure warm-up, and camera framing | Six versioned revisions produced the final readable four-view set |
| 2026-07-15 | Added semantic regeneration fingerprint and focused Editor validation | Determinism passed with fingerprint `962F4AE1...26B36`; focused checks passed `3/3` |
| 2026-07-15 | Ran repository assembly-boundary validation | Passed `31/31`; Unity process crashed only during shutdown after the pass marker |
| 2026-07-15 | Split the editor builder from review/capture/evidence logic and ran source-growth validation | M01 source is 600/434 lines; repository gate still fails on four unrelated pre-existing narrative/materials helper paths |
