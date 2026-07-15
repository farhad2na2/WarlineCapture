# M01 Visual Map Generation Implementation Tracker

**Status:** Planning ready; visual implementation not started

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

## 7. Proposed Prototype Structure

These paths are provisional and must be checked against the operation-map architecture branch before implementation begins:

- Prototype scene: `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity`
- Recipe asset: `Assets/Game/Configs/MapPrototypes/M01_VisualRecipe.asset`
- Shared recipe model: `Assets/Game/Scripts/MapPrototypes/VisualMapRecipe.cs`
- Pure plan model: `Assets/Game/Scripts/MapPrototypes/VisualMapPlan.cs`
- Editor generator: `Assets/Game/Scripts/Editor/MapPrototypes/M01VisualMapGenerator.cs`
- Editor menu: `Game/Operation Maps/M01/Generate Visual Prototype`
- Captures: `Design/ArtReview/OperationMaps/M01/`

Generated scene objects should live under one clearly named root such as `_M01VisualGenerated`. Regeneration must update or replace only that owned root. Hand-authored override roots and review cameras must remain separate.

The recipe should be able to record at least:

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

- [ ] Build a dependency map from generation orchestration to pure planning and runtime side effects.
- [ ] Complete the reuse/extract/adapt/reject matrix for all generator helpers used by the prototype.
- [ ] Define a serializable `VisualMapPlan` independent from GameObjects and runtime systems.
- [ ] Prove deterministic road and plot output for a fixed M01 recipe and seed.
- [ ] Extend focused editor tests for same-seed equality, bounds, overlap rules, and no-op regeneration.
- [ ] Decide whether shared pure code remains in the existing runtime assembly or moves behind a shared assembly boundary.

**Exit:** A reviewed technical spike can produce a deterministic M01 plan without starting Match runtime systems.

### Phase 2 - M01 Authored Blueprint

- [ ] Lock prototype dimensions and the gameplay-camera scale reference.
- [ ] Mark the central road, market, utility compound, damaged lane, map entrance, and horizon zones.
- [ ] Choose at least three primary visual anchors.
- [ ] Create curated M01 prefab palettes from the reference scenes.
- [ ] Define density and repetition limits per zone.
- [ ] Record authored overrides that generation must preserve.

**Exit:** A top-down blueprint and palette review clearly match the Old Market concept before detailed generation starts.

### Phase 3 - Editor Generator Foundation

- [ ] Create the isolated prototype scene and recipe asset.
- [ ] Add the editor command under the `Game` menu hierarchy.
- [ ] Generate into one owned scene root with Undo support.
- [ ] Preserve prefab links and deterministic transforms.
- [ ] Add preview, generate, clear-owned-output, and regenerate commands.
- [ ] Refuse to modify objects outside the owned generated root.
- [ ] Store generator version and recipe identity in the generated root.

**Exit:** Repeated generation is deterministic, scoped, undoable, and leaves reference and hand-authored content untouched.

### Phase 4 - Macro And Meso Composition

- [ ] Generate the road hierarchy and terrain/map boundary.
- [ ] Place market, compound, residential/service, and damaged-lane clusters.
- [ ] Add walls, gates, alleys, courtyards, and landmark silhouettes.
- [ ] Establish mountain/horizon closure and intentional map-edge sightlines.
- [ ] Remove obvious grid monotony and repeated prefab patterns.
- [ ] Check all structures against soldier and door scale references.

**Exit:** The map reads as M01 Old Market from gameplay height even before micro dressing.

### Phase 5 - Visual Storytelling Pass

- [ ] Add curated market props, cloth, signs, crates, cables, and service clutter.
- [ ] Build the damaged-lane wreck, rubble, fire, smoke, and emergency detail cluster.
- [ ] Dress the utility compound with a distinct security and logistics vocabulary.
- [ ] Add sparse vegetation and regional accents in intentional groups.
- [ ] Add decals and material variation to break visible repetition.
- [ ] Balance focal density against navigable visual breathing room.

**Exit:** Screenshots approach the reference-scene density and look hand-authored rather than uniformly procedural.

### Phase 6 - Lighting And Review Captures

- [ ] Match the warm desert visual direction without flattening material contrast.
- [ ] Establish readable focal lighting for market, compound, and damaged lane.
- [ ] Validate smoke, fire, and local lights from gameplay height.
- [ ] Capture top-down plan, gameplay camera, low oblique, and horizon views.
- [ ] Capture both 16:9 and 20:9 framing without relying on HUD.
- [ ] Record Unity version, recipe version, seed, and commit in the capture notes.

**Exit:** A reproducible capture set is ready for project-owner visual review.

### Phase 7 - Human Visual Acceptance

- [ ] Review concept fidelity.
- [ ] Review Match/Demo/Demo2 quality parity.
- [ ] Record required changes as concrete composition or dressing tasks.
- [ ] Regenerate and hand-tune without losing accepted overrides.
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
| 01 | M01 Old Market | Not started | Not started | Not started | Blocked by M01 hold and integration readiness |
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
- Regeneration is a no-op when input and generator version are unchanged.
- Prefab references remain valid and no assets are duplicated from reference scenes.
- Generator edits only the owned prototype root.
- Scene opens without missing scripts, materials, shaders, or prefab references.
- Review captures are attached with recipe version, seed, and commit.
- `git diff --check` passes.

Full Android builds are not required for the isolated visual gate. They become required after promotion into the canonical map/loading path.

## 12. Risks And Open Decisions

| Risk or decision | Current position | Resolution gate |
|---|---|---|
| Legacy planning code may still depend on runtime context | Reuse only after the Phase 1 dependency audit | Generator reuse spike review |
| Pure code assembly ownership is undecided | Prefer the smallest extraction that both editor tests and runtime can reference | Phase 1 review |
| Prototype dimensions are not locked | Derive from concept framing, metric contract, and Match camera reference | Blueprint review |
| Reference-scene palette may be too broad | Curate a small M01-specific palette with repetition limits | Palette review |
| Generated output may look generic | Require authored zones, landmarks, overrides, and a dedicated storytelling pass | Visual acceptance |
| Prototype paths may conflict with parallel architecture work | Treat all proposed paths as provisional until ownership check | Before first code change |
| M01 production hold remains active | Keep work isolated and non-player-facing | Phase 9 entry |
| Twelve maps may drift toward one visual identity | Give each physical map a distinct recipe, palette, silhouette, and acceptance set | Twelve-map contract review |

## 13. Progress Summary

| Phase | Status | Notes |
|---|---|---|
| 0. Scope and baseline | In review | Audit and tracker complete; merge approval pending |
| 1. Generator reuse spike | Not started | Initial source audit recorded above |
| 2. M01 authored blueprint | Not started | Candidate B is the primary concept |
| 3. Editor generator foundation | Not started | Paths are provisional |
| 4. Macro and meso composition | Not started | Visual-only prototype |
| 5. Visual storytelling pass | Not started | Human-authored overrides required |
| 6. Lighting and captures | Not started | 16:9 and 20:9 review set |
| 7. Human visual acceptance | Not started | Project owner is final visual approver |
| 8. Twelve-map production contract | Not started | Begins after M01 method is accepted |
| 9. Promotion and integration | Blocked | Requires hold release and architecture readiness |

## 14. Implementation Log

| Date | Change | Result |
|---|---|---|
| 2026-07-15 | Audited design contracts, reference scenes, M01 concept, and legacy runtime city generator; created this implementation tracker | Planning baseline ready for independent review; no runtime or scene implementation started |
