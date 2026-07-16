# M01 Visual Map Generation Implementation Tracker

**Status:** Editor-only M01 R&D visual prototype generated and validated; production promotion and canonical integration remain blocked by FirstLaunch Phase 10R / Gate 9R

**Created:** 2026-07-15

**Workflow:** Pull request with independent review

**Pilot map:** M01 First Contact / Old Market (`opmap.ch01.district_edge_01`)
**Program target:** Exactly 12 physical operation maps total: the current existing map, dedicated M01, and ten additional maps

## 1. Purpose

This tracker defines the first visual-quality iteration of the dedicated M01 operation map and its eventual use of the accepted editor-generation pipeline. The project-owner R&D exception recorded below authorizes an isolated, editor-only visual scene and supporting review tooling while the M01 production hold remains active. It does not authorize a canonical source scene, bake, campaign handoff, or player-facing integration.

The first iteration is an **editor-viewable visual prototype only**. It does not need campaign loading, units, navigation, mission logic, or playability. Its purpose is to let the project owner judge composition, density, scale, lighting, and environmental storytelling before canonical promotion.

Approved campaign maps are stable, reviewed, editor-baked scenes. Deterministic editor tooling may accelerate authoring; physical topology must never be generated at runtime.

## 2. Authority, Hold, And Dependencies

This tracker is subordinate to:

- [3D Single-Map Gameplay Direction](../3D_SingleMap_Gameplay_Direction.md)
- [M01 First Contact Production Contract](../M01_FirstContact_Production_Contract.md)
- [M01 Metric Scale And Readability Contract](../M01_Metric_Scale_Readability_Contract.md)
- [Campaign Mission High-Level Design Catalog](../Campaign_Mission_High_Level_Design_Catalog.md)
- [Operation Map Scene Split And Generator Tracker](operation_map_scene_split_and_generator_tracker.md)
- [Performance Regression Contract](performance_regression_contract.md)
- [Accepted Performance Baseline](performance_regression_accepted_baseline.json)
- [Agent Pull Request Review And Merge Workflow](agent_pull_request_review_merge_workflow.md)

### Gate 9R Production Stop Rule And R&D Exception

The 2026-07-11 M01 hold remains authoritative for production work. FirstLaunch Phase 10R must pass Gate 9R before canonical M01 generation, promotion, loading, or player-facing integration begins.

On 2026-07-15, the project owner explicitly directed that scene construction may proceed as R&D and must not be blocked by the production rules. This exception is limited to:

- the isolated `M01_VisualPrototype.unity` editor scene;
- editor-only prototype generation, review-camera, capture, and validation tooling;
- prototype-only materials, lighting, visual dressing, and review evidence;
- visual iteration that does not alter Match, Demo, Demo2, runtime loading, mission logic, Addressables, or build content.

The hold continues to block all production-facing work, including:

- canonical generator extraction, source packs, configs, source scenes, and bakes intended for production;
- canonical M01 source scene, metadata, bakes, Addressables content, loading, mission integration, and promotion;
- edits to runtime behavior, campaign flow, Android content, build configuration, or the accepted operation-map pipeline.

Gate 9R release is necessary but not sufficient for production promotion. Each production phase still requires its listed entry and exit evidence. The R&D exception does not mark canonical phases complete and does not release Gate 9R.

## 3. Accepted Direction

- Build M01 as dedicated Old Market map `opmap.ch01.district_edge_01`, not as a crop or rename of the current large map.
- Use the accepted deterministic, editor-only operation-map generation contracts after Gate 9R.
- Save and review output as a normal canonical Unity source scene before downstream bakes.
- Use [Match.unity](../../Assets/Game/Scenes/Match.unity), [Demo.unity](../../Assets/Game/Scenes/Demo.unity), and [Demo2.unity](../../Assets/Game/Scenes/Demo2.unity) as read-only quality and asset references.
- Use the [M01 Candidate B concept](../NarrativeVision/FirstLaunch/ArtReview/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-04_M01Handoff_CandidateB.png) as the primary composition target.
- Reuse only pure, deterministic portions of the legacy runtime city generator through an explicitly reviewed extraction or adapter.
- Do not reactivate or depend on the legacy generator at runtime.
- Do not duplicate reference scenes, generated scene chunks, shared binary art, or Unity `.meta` identities.
- Keep authored map identity in reviewed source/config data; do not create a parallel M01 generation framework.

## 4. Ownership Boundary

Another task owns operation-map scene separation, catalogs, Addressables, loading, baking, and the canonical runtime contract. This tracker only sequences the future M01 visual slice against that accepted architecture.

### Owned By The Approved R&D Slice

- `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity`
- `Assets/Game/Scripts/Editor/MapPrototypes/M01VisualMapPrototypeEditorUtility*.cs`
- `Assets/Tests/Editor/M01VisualMapPrototypeEditorUtilityTests.cs`
- `Assets/Game/Art/MapPrototypes/M01/`
- `Design/ArtReview/OperationMaps/M01/`

These paths are noncanonical prototype evidence. They must not be consumed by runtime map loading or treated as the final production recipe without a later promotion review.

### Owned By Future M01 Slices After Gate 9R

- M01 visual brief, reviewed source-pack/config data, and acceptance checklist.
- Dedicated M01 visual source/prototype content in paths assigned by the coordinator.
- M01-specific inputs and authored overrides expressed through accepted operation-map contracts.
- M01 review captures, evidence, and visual iteration log.
- Portfolio planning for the ten unassigned physical maps.

### Read-Only References

- `Assets/Game/Scenes/Match.unity`
- `Assets/Game/Scenes/Demo.unity`
- `Assets/Game/Scenes/Demo2.unity`
- Existing prefabs, materials, terrain assets, lighting profiles, and VFX.
- Legacy runtime city generator and tests until a separately reviewed extraction is authorized.

### Excluded From This Track

- Current map content, `MatchSubScene`, `GeneratedStaticMapPresentation`, and `GeneratedCombinedMeshes`.
- Operation-map catalog/loading, scene resolver, streamer, and loading shell.
- Static presentation, navigation, minimap, occlusion, surface, blocker, and combined-mesh bakes.
- Mission logic, objectives, spawns, encounters, HUD, Jenkins, and CI changes.
- Runtime city startup, readiness, minimap, loading-gate behavior, or any runtime physical-map generation.

Any edit outside a coordinator-issued allowlist stops for an ownership check.

## 5. Visual Target For M01

### Narrative Read

M01 is the player's first Old Market combat space after the bombing and blackout. Before gameplay is added, the environment should communicate a civilian district disrupted by a recent hostile event.

### Required Composition

- Strong central road intersection or corridor.
- Dense market cluster with courtyards, awnings, stalls, walls, service alleys, and small props.
- Fortified utility or authority compound across or beside the main corridor.
- Damaged or obstructed lane with wreckage, fire, smoke, debris, or emergency dressing.
- At least three visual anchors that establish location and facing without UI.
- Sparse, intentionally grouped palms and vegetation.
- Coherent desert edge and mountain or elevated horizon closure.
- Consistent 1.8 m soldier and approximately 2.3 m door scale.

### Quality Bar

The accepted pass must approach Match, Demo, and Demo2 in density, authored rhythm, material coherence, prop layering, and lighting. It must include:

1. **Macro:** road hierarchy, district silhouette, compounds, horizon, and focal axes.
2. **Meso:** building clusters, walls, alleys, courtyards, market groups, wreck cluster, and vegetation groups.
3. **Micro:** signs, cables, crates, barriers, cloth, trash, decals, rubble, small lights, and story props concentrated around focal areas.

Uniform random scatter is not acceptable for meso or micro placement.

## 6. Legacy Generator Reuse Rules

Legacy logic is a source to audit, not an alternate architecture. Each dependency must be classified as:

- **Reuse directly:** pure deterministic planning with no scene/runtime side effects.
- **Extract:** valuable deterministic logic currently coupled to runtime context.
- **Adapt:** useful output converted into accepted operation-map source/config data.
- **Reject:** runtime-only, stateful, or visually unsuitable behavior.

Candidate areas include deterministic seeds, bounds, roads, plots, footprints, landmarks, walls, gates, and curated decoration candidates. Reject coroutine scheduling, startup/readiness, ECS road commits, runtime spawn/delete bridges, gameplay health, minimap publication, Match singletons, runtime surface mutation, and uncurated random decoration.

No extraction may add a manager, controller, facade, service locator, broad shell, new updating `MonoBehaviour`, or runtime generator path.

## 7. Canonical Generation Architecture

The parent tracker has already accepted the generation types. This tracker must not introduce `VisualMapRecipe`, `VisualMapPlan`, `M01VisualMapGenerator`, or equivalents.

### Exact Type And Data Mapping

| M01 concern | Accepted owner/type | M01 mapping |
|---|---|---|
| Reviewed recipe-like data | Versioned source-pack/config/data consumed by `OperationMapGenerationInput` | Store seed/version, map id, bounds, source paths, palettes, zones, exclusions, and output ownership as reviewed serialized data. Extend the accepted schema only when a required M01 field cannot be represented. |
| Generation request | `OperationMapGenerationInput` | Map `opmap.ch01.district_edge_01`, seed/version, bounds, reviewed source-pack paths, owned output paths, and dry-run policy directly into this value. |
| Deterministic metadata/source preparation | `OperationMapTextureMaskGenerator` | Convert reviewed texture/mask and config inputs into canonical metadata and source-scene inputs. Any reused city-planning adapter is editor-only and feeds this boundary; it is not a peer generator. |
| Auditable generation output | `OperationMapGenerationResult` | Record written/reused/stale counts, hashes, metadata bytes, validation result, and exact output paths. No parallel plan/result model is allowed. |
| Scene construction | `OperationMapSourceSceneBuilder` | Consume accepted generation output and deterministically create/update canonical M01 source scene/subscene content while referencing shared assets. |
| Authored adjustment | Canonical source scene plus reviewed config/data | Preserve hand-authored roots/overrides through explicit ownership fields or data extensions accepted in the parent architecture. Do not hide overrides in a second recipe class. |

Expected future paths are the canonical parent paths, subject to coordinator assignment after Gate 9R:

- Source scene: `Assets/Game/Scenes/OperationMaps/Chapter01/opmap_ch01_district_edge_01.unity`
- Metadata/config: `Assets/Game/Data/OperationMaps/Chapter01/opmap.ch01.district_edge_01.asset`
- Review captures: `Design/ArtReview/OperationMaps/M01/`

Generated scene objects must use one map-owned root. Regeneration may update only that root, must preserve shared asset GUIDs and approved authored overrides, and must report no writes or stale deletions for identical input. Static presentation outputs remain owned exclusively by the downstream baker.

## 8. Implementation Phases

### Phase 0 - Documentation Scope And Baseline

- [x] Confirm the current task is documentation-only and nonplayable.
- [x] Record Gate 9R as a blocker for every M01 prototype and implementation phase.
- [x] Reconcile M01 generation to the accepted operation-map types.
- [x] Reconcile the portfolio to exactly 12 physical maps.
- [x] Audit reference scenes, M01 concept, design contracts, and legacy generator sources read-only.
- [ ] Receive independent review and merge approval for this tracker.

**Exit:** Tracker merged with no code, asset, config, scene, generated-output, or runtime change. This exit does not release Gate 9R.

### Phase 1 - Generator Reuse Spike

**Entry blocker:** Gate 9R release evidence must be recorded before any spike, extraction, adapter, test, source pack, or config is created.

- [ ] Record Gate 9R release evidence and coordinator allowlist.
- [ ] Build dependency and reuse/extract/adapt/reject matrices.
- [ ] Map every retained output into `OperationMapGenerationInput` or accepted source-pack/config data.
- [ ] Prove fixed-seed deterministic road/plot candidates through the accepted generator boundary.
- [ ] Add focused same-input equality, bounds, overlap, ownership, and no-op tests.
- [ ] Reject any proposed parallel generation type or runtime dependency.

**Exit:** Accepted editor-only contracts can produce auditable M01 inputs/results without starting Match runtime systems.

### Phase 2 - M01 Authored Blueprint

**Entry blocker:** Gate 9R release evidence and Phase 1 acceptance.

- [ ] Lock dimensions and gameplay-camera scale reference.
- [ ] Mark central road, market, utility compound, damaged lane, entrance, and horizon zones.
- [ ] Choose at least three primary visual anchors.
- [ ] Curate M01 prefab/material/VFX palettes from shared reference assets.
- [ ] Define density, repetition, exclusion, and authored-override data.

**Exit:** Reviewed blueprint/config data matches the Old Market concept and accepted input schema.

### Phase 3 - Canonical Editor Generator Foundation

**Entry blocker:** Gate 9R release evidence and accepted Phases 1-2.

- [ ] Create only coordinator-assigned M01 source-pack/config and source-scene paths.
- [ ] Invoke `OperationMapTextureMaskGenerator` with `OperationMapGenerationInput`.
- [ ] Pass `OperationMapGenerationResult` to `OperationMapSourceSceneBuilder`.
- [ ] Support dry-run, generate, clear-owned-output, and deterministic regenerate operations.
- [ ] Preserve prefab links, stable `.meta` identities, Undo where scene editing applies, and authored overrides.
- [ ] Refuse writes outside the map-owned root/output paths.
- [ ] Record schema/version, source hashes, seed, result counts, and validation outcome.

**Exit:** Repeated generation is deterministic, scoped, auditable, and no-op safe through canonical types.

### Phase 4 - Macro And Meso Composition

**Entry blocker:** Gate 9R release evidence and Phase 3 acceptance.

- [ ] Generate road hierarchy and terrain/map boundary.
- [ ] Place market, compound, residential/service, and damaged-lane clusters.
- [ ] Add walls, gates, alleys, courtyards, and landmark silhouettes.
- [ ] Establish horizon closure and intentional edge sightlines.
- [ ] Remove grid monotony and repeated prefab patterns.
- [ ] Check soldier, door, road, wall, and building scale.

**Exit:** M01 Old Market reads clearly from gameplay height before micro dressing.

### Phase 5 - Visual Storytelling

**Entry blocker:** Gate 9R release evidence and Phase 4 acceptance.

- [ ] Add curated market props, cloth, signs, crates, cables, and service clutter.
- [ ] Build damaged-lane wreck, rubble, fire, smoke, and emergency clusters.
- [ ] Give the utility compound distinct security/logistics dressing.
- [ ] Add sparse vegetation and regional accents in intentional groups.
- [ ] Add decals/material variation and remove visible repetition.
- [ ] Balance focal density with navigable visual breathing room.

**Exit:** Captures approach reference quality and read as authored rather than uniformly procedural.

### Phase 6 - Lighting And Captures

**Entry blocker:** Gate 9R release evidence and Phase 5 acceptance.

- [ ] Establish warm desert lighting without flattening material contrast.
- [ ] Keep market, compound, and damaged lane readable.
- [ ] Validate smoke, fire, and local lights from gameplay height.
- [ ] Capture top-down, gameplay, low-oblique, and horizon views.
- [ ] Capture 16:9 and 20:9 framing without HUD dependence.
- [ ] Record Unity version, input schema/version, seed, hashes, and exact commit.

**Exit:** Reproducible evidence is ready for visual and performance review.

### Phase 7 - Human Visual Acceptance

**Entry blocker:** Gate 9R release evidence and Phase 6 completion.

- [ ] Review concept fidelity, scale, composition, and reference-scene quality parity.
- [ ] Record concrete composition/dressing changes.
- [ ] Regenerate and hand-tune without losing accepted overrides.
- [ ] Obtain explicit project-owner visual acceptance.

**Exit:** The project owner accepts the visual base. This does not authorize production promotion.

### Phase 8 - Pre-Promotion Performance Acceptance

**Entry blocker:** Gate 9R release evidence, visual acceptance, and parent-tracker performance baseline readiness.

- [ ] Pass the Editor performance gate in Section 10.
- [ ] Pass the Android development-device gate in Section 10.
- [ ] Pass the clean ARM64 IL2CPP Android release gate in Section 10.
- [ ] Record draw calls, triangles, loaded memory, package/install size, frame/GC, and thermal/device evidence.
- [ ] Resolve every `measurement-required` budget through its parent-tracker owner; do not substitute a local estimate.
- [ ] Obtain independent performance evidence acceptance at the exact candidate commit.

**Exit:** All applicable budgets are active and pass. Missing, stale, dirty, incomparable, or measurement-required evidence fails closed.

### Phase 9 - Twelve-Map Production Contract

**Entry blocker:** Gate 9R release evidence and accepted M01 method. Documentation planning may precede implementation, but no M01 or follow-on implementation is authorized by this phase.

- [ ] Record reusable canonical generator/config modules and shared palettes.
- [ ] Define reviewed input/config identity for the ten unassigned physical maps.
- [ ] Assign mission-to-map relationships without assuming one map per scenario.
- [ ] Define visual identity, landmarks, palette, and seed for all 12 physical maps.
- [ ] Estimate authored override effort and per-map performance/package cost.
- [ ] Define per-map captures, acceptance ownership, and small PR slices.

**Exit:** The exact 12-map portfolio preserves uniqueness and uses one canonical generation architecture.

### Phase 10 - Deferred Promotion And Integration

**Entry blocker:** Gate 9R release, Phases 1-8 accepted, parent architecture integration readiness, and a separately reviewed coordinator assignment.

- [ ] Reconfirm Gate 9R release evidence at the promotion commit.
- [ ] Reconcile source/config paths and data with the current accepted parent tracker.
- [ ] Promote accepted content into canonical M01 operation-map ownership.
- [ ] Add metadata, navigation, blockers, mission anchors, bakes, Addressables, and loading only in separate reviewed slices.
- [ ] Re-run Editor and Android performance/package gates against the integrated candidate.
- [ ] Remove prototype-only data only after canonical parity and rollback verification.

**Exit:** M01 becomes a production operation map through the canonical pipeline with all hold, architecture, visual, performance, package, and integration gates green.

## 9. Visual Acceptance Matrix

| Area | Pass condition | Reviewer |
|---|---|---|
| Concept | Old Market, central corridor, market, compound, damaged lane, and horizon read immediately | Project owner |
| Composition | Primary route and at least three anchors are clear from gameplay height | Project owner |
| Scale | Soldiers, doors, roads, walls, props, and buildings are mutually consistent | Project owner / design |
| Density | Macro, meso, and micro layers approach Match/Demo/Demo2 quality | Project owner / art |
| Repetition | No dominant grid, prefab row, identical rotation, or uniform scatter | Art review |
| Storytelling | Bombing/blackout aftermath reads without explanatory UI | Project owner / narrative |
| Lighting | Focal zones remain readable on target mobile framing | Art review |
| Edges | Boundaries and horizon look intentional in all review views | Art review |
| Determinism | Identical input, seed/version, and source hashes produce identical result hashes/transforms | Engineering |
| Isolation | Generation changes only declared M01 source/config paths and map-owned roots | Engineering |
| Runtime | No runtime physical-map generation path, startup dependency, or recurring loop exists | Engineering |

Visual acceptance never waives gameplay, architecture, performance, package, or loading acceptance.

## 10. Fail-Closed Performance Acceptance

This section is a pre-promotion gate, not a request to invent M01-local budgets. The authoritative values and ownership remain in the [parent operation-map tracker](operation_map_scene_split_and_generator_tracker.md), [performance contract](performance_regression_contract.md), and [accepted baseline JSON](performance_regression_accepted_baseline.json). The candidate must use the same scenarios, device tiers, build identity, warmup, sample duration, and tooling as its accepted comparison.

| Evidence | Required acceptance before promotion |
|---|---|
| Editor render | Same-camera baseline/candidate captures of draw calls, batches/SetPass, triangles, vertices, visible renderers, and quality settings. Candidate passes only an active parent-approved budget; absent render budgets remain measurement-required and block promotion. Batchmode zero counters are not evidence. |
| Editor runtime | Load time, average/p95/p99/max frame time, loaded/retained memory, texture/mesh memory where available, and warmed GC. Current inherited Editor Match p95 is `<= 20 ms`; candidate must also show no unexplained regression against the accepted parent baseline. |
| Android development | Real supported-device capture of startup/load, sustained average/p95/p99/max frame time, recurring GC, loaded/peak/retained memory, draw calls, triangles, and process stability. Android p95 remains `< 33 ms` for baseline/recommended and `< 25 ms` for high-end unless the parent ratchet tightens it. |
| Android release | Clean exact-commit ARM64 IL2CPP build with BuildReport, hashes, APK/AAB bytes, installed bytes, Addressables Build Layout, Entities stream/archive bytes, map-owned payload, and shared-dependency duplication. Active ceilings are APK `<= 463359198` bytes and AAB `<= 426399778` bytes; installed size remains measurement-required until the parent owner accepts a limit. |
| GC | Structured warmup and measured samples. The inherited Match steady-state acceptance is `<= 1024` bytes over 300 measured frames after 180 warmup frames; any recurring post-warmup allocation or weakened parent gate fails. |
| Memory | Loaded, peak allocated, retained-after-unload, texture, mesh, native, and graphics memory evidence on the same device. Null/measurement-required limits in the baseline JSON are blockers, not permission to omit metrics or self-approve estimates. |
| Thermal/device | Device model/tier, OS, chipset, RAM, resolution, refresh rate, quality profile, battery/power state, ambient/start/end temperature, Android thermal status, sustained duration, FPS stability, and process survival. Throttling, rising retained memory, thermal instability, or incomplete provenance fails. |
| Comparison integrity | Exact clean commit, Unity/package lock identity, build target/backend, map/input hashes, baseline artifact identity, commands, raw artifacts, and reviewer disposition. Dirty, stale, mismatched, shortened, headless-render, or incomparable evidence fails. |

The candidate may tighten an active parent budget but cannot loosen it. A `null` or `measurement-required` limit must be resolved and accepted by its named parent owner before Phase 8 can pass. One-map, representative two-map, and all-approved-map package costs remain parent-tracker gates; the provisional `80-110 MB` per-map range is planning evidence, not acceptance.

## 11. Exact Twelve-Map Portfolio

This register counts physical maps, not missions or scenarios. It contains exactly 12 rows: one current existing map, one dedicated M01 map, and ten additional maps. The current map and M01 are both inside the total; neither creates a thirteenth map. The current-map slot is settled, but its candidate id remains provisional pending parent-tracker Phase 3 approval.

| # | Physical map | Portfolio role | Generation/config state | Promotion state |
|---:|---|---|---|---|
| 01 | `opmap.skirmish.desert_base_01` (provisional) | Current existing large map | Existing authored source; candidate id pending parent-tracker Phase 3 approval | Pending parent Phase 3 id approval and parent gates |
| 02 | `opmap.ch01.district_edge_01` | Dedicated M01 Old Market | Noncanonical R&D visual prototype available; production generation blocked by Gate 9R | Blocked by Gate 9R and Phases 1-8 |
| 03 | To be assigned | Additional map 1 of 10 | Not started | Not started |
| 04 | To be assigned | Additional map 2 of 10 | Not started | Not started |
| 05 | To be assigned | Additional map 3 of 10 | Not started | Not started |
| 06 | To be assigned | Additional map 4 of 10 | Not started | Not started |
| 07 | To be assigned | Additional map 5 of 10 | Not started | Not started |
| 08 | To be assigned | Additional map 6 of 10 | Not started | Not started |
| 09 | To be assigned | Additional map 7 of 10 | Not started | Not started |
| 10 | To be assigned | Additional map 8 of 10 | Not started | Not started |
| 11 | To be assigned | Additional map 9 of 10 | Not started | Not started |
| 12 | To be assigned | Additional map 10 of 10 | Not started | Not started |

Shared scenarios, variants, or mission assignments do not add physical maps to this register.

## 12. Validation

### Approved R&D Prototype Evidence

| Check | Result |
|---|---|
| Scope | Isolated editor-only scene and tooling; no Match, Demo, Demo2, runtime loading, mission logic, Addressables, Android, Jenkins, or CI changes |
| Unity generation and capture | Passed in Unity `6000.5.2f1`; `[M01VisualMap] result=Passed` |
| Generator identity | `M01VisualPrototype_2026-07-15_v6`, seed `26071501` |
| Semantic regeneration | Passed with fingerprint `962F4AE1EF66319621E41C120F1CEDB200F94B6B452B3CE6089606FF4A426B36` |
| Focused Editor validation | Passed `3/3`: asset palette, scene structure/density, and capture/manifest integrity |
| Architecture boundary | Passed `31/31`; Unity crashed during shutdown only after emitting the pass marker |
| Current-main compatibility | Ported onto base `2873e4dd7`; R&D assets are byte-identical to the validated commit and compile against current-main `Game.Editor` references with `0` warnings and `0` errors |
| Capture set | Four `1600x900` review PNGs, one contact sheet, and a generated manifest under `Design/ArtReview/OperationMaps/M01/` |
| Capture readability | Average luminance: overview `137.6`, Old Market `135.7`, aftermath `110.4`, top-down `106.2`; minimum guard is `45` |
| Visual disposition | Internal iteration complete; project-owner visual acceptance remains pending |

Unity scene YAML is not used as the determinism identity because reconstruction assigns fresh local serialization IDs. The semantic fingerprint covers hierarchy paths, prefab identities, transforms, material assets, camera settings, and lights.

### This Documentation PR

- Confirm every repository link/path referenced as existing resolves in `HEAD`.
- Confirm the tracker blocks all M01 prototype/implementation work before Gate 9R.
- Confirm canonical type mapping contains no parallel recipe, plan, result, builder, or generator abstraction.
- Confirm the physical-map register has exactly 12 entries: one provisional-id current-map slot + M01 + ten additional.
- Confirm performance acceptance covers Editor and Android render, memory, package/install, frame/GC, and thermal/device evidence.
- Confirm no code, asset, config, scene, generated output, Addressables, runtime, build, or CI file changed.
- Run `git diff --check` and focused terminology/count searches.
- Require independent review before merge.

### Future Implementation PRs

- Gate 9R release evidence and coordinator allowlist are present before the first implementation change.
- Unity editor compile and focused EditMode tests pass.
- Identical `OperationMapGenerationInput` produces an equivalent `OperationMapGenerationResult` and no-op scene update.
- Output ownership, `.meta` stability, shared references, and authored overrides pass.
- Scene opens without missing scripts, materials, shaders, or prefab references.
- No runtime physical-map generation, new recurring loop, or Match runtime dependency is introduced.
- Review captures include input version, seed, hashes, exact commit, and camera/settings identity.
- Phase 8 performance evidence passes before promotion; full Editor and Android evidence is mandatory, including for an isolated visual candidate intended for promotion.
- `git diff --check` passes.

## 13. Risks And Decisions

| Risk or decision | Current position | Resolution gate |
|---|---|---|
| Gate 9R remains active | Approved noncanonical editor R&D may continue; production generation, promotion, and integration remain blocked | Phase 1 production entry and every later canonical phase |
| Legacy logic depends on runtime | Reuse only pure deterministic behavior through accepted editor boundaries | Phase 1 review |
| M01 data needs an unsupported field | Extend accepted source-pack/config schema narrowly and map it explicitly | Parent architecture review before code |
| Parallel architecture appears | Reject `VisualMapRecipe`, `VisualMapPlan`, M01-specific generator/builder/result types, or equivalents | Architecture review |
| Generated output looks generic | Require authored zones, landmarks, overrides, and storytelling passes | Visual acceptance |
| Performance budgets are missing | Preserve measurement-required status and fail closed until parent owner accepts limits | Phase 8 |
| Twelve-map count drifts | Fixed formula is current map + M01 + ten additional maps = 12 | Portfolio review |

## 14. Progress And Checklist Audit

Checklist state in this revision: **64 total, 5 complete, 59 open**. The five complete items are documentation findings in Phase 0. Canonical production items remain open; the approved R&D prototype and its evidence are tracked separately below.

| Phase | Checklist | Status | Notes |
|---|---:|---|---|
| R&D visual prototype | Evidence-only | Generated and validated | Noncanonical editor scene; project-owner visual acceptance pending |
| 0. Documentation scope and baseline | 5/6 | In review | Merge approval open; no implementation authorized |
| 1. Generator reuse spike | 0/6 | Blocked | Gate 9R |
| 2. M01 authored blueprint | 0/5 | Blocked | Gate 9R and Phase 1 |
| 3. Canonical editor generator | 0/7 | Blocked | Gate 9R and Phases 1-2 |
| 4. Macro and meso | 0/6 | Blocked | Gate 9R and Phase 3 |
| 5. Visual storytelling | 0/6 | Blocked | Gate 9R and Phase 4 |
| 6. Lighting and captures | 0/6 | Blocked | Gate 9R and Phase 5 |
| 7. Human visual acceptance | 0/4 | Blocked | Gate 9R and Phase 6 |
| 8. Performance acceptance | 0/6 | Blocked | Gate 9R, visual acceptance, parent budgets |
| 9. Twelve-map contract | 0/6 | Blocked for implementation | Documentation planning only until prerequisites |
| 10. Promotion and integration | 0/6 | Blocked | Gate 9R plus all prior acceptance |

## 15. Implementation Log

| Date | Change | Result |
|---|---|---|
| 2026-07-15 | Audited design contracts, references, concept, and legacy generator; created tracker | Documentation baseline; no implementation started |
| 2026-07-15 | Reconciled Gate 9R, canonical generator types, exact 12-map portfolio, and fail-closed Editor/Android performance acceptance | Coordinator findings addressed in documentation only; all M01 implementation remains blocked |
| 2026-07-15 | Project owner explicitly approved isolated M01 scene construction as R&D outside the production gate | Editor-only visual prototype work authorized; canonical production phases remain blocked |
| 2026-07-15 | Implemented and iterated the isolated M01 Old Market builder, materials, lighting, story dressing, review cameras, and capture pipeline | Version 6 generated a readable four-view review set without runtime or canonical map integration |
| 2026-07-15 | Added semantic regeneration and focused Editor validation | Determinism passed with fingerprint `962F4AE1...26B36`; focused validation passed `3/3` and architecture boundary passed `31/31` |
| 2026-07-15 | Ported the validated R&D tree onto current-main base `2873e4dd7` and compiled the editor partials against current-main Unity references | Asset tree remained byte-identical; compatibility compile passed with `0` warnings and `0` errors |
