# Dense City Terrain And Natural Layout Progress Tracker

## Scope

Improve the edit-mode city generated in `opmap_skirmish_desert_base_01` so roads sit on continuous graded surfaces, buildings sit on valid foundations, empty buildable land receives an intentional use, and the result reads like a naturally developed Middle Eastern city.

This tracker covers visual city layout only. ECS conversion, runtime generation, navigation, simulation, Android validation, and optimization are out of scope.

## Overall Progress

**Status:** In Progress  
**Completion:** 68%

## Non-Negotiable Rules

- Never place a rejected building footprint at fallback `Y=0`.
- Never place a road from a single center-height sample.
- Every road intersection must use one shared solved elevation.
- Developed land must be flat or deliberately terraced.
- Mountains and severe slopes remain natural terrain and are not covered by city blocks.
- The city footprint is an authored graded zone: interior dune/hill relief and conflicting ground clutter may be disabled while the existing flat map base is retained.
- Preserve the undeveloped perimeter and mountain band; never modify shared Polygon Military FBX meshes.
- Every large empty buildable area must have an intentional role: road, parcel, plaza, park, bazaar, compound, parking, or reserved terrain.
- Reuse the existing road visual families and the Polygon Military demo assets before creating substitutes.
- Do not modify ECS, runtime gameplay, or optimization as part of this tracker.

## Stage 0 - Baseline And Failure Audit

**Status:** Complete  
**Progress:** 100%

- [x] Capture full-map city proof.
- [x] Capture civic/bazaar oblique proof.
- [x] Capture close bazaar proof.
- [x] Confirm roads currently sample only their center height.
- [x] Confirm invalid building footprints fall back to `Y=0`.
- [x] Confirm random fringe-road omission creates unexplained road gaps.
- [x] Record baseline output: 7,083 buildings, 12 parks, 2,615 road tiles, 60 road chunks.

**Evidence**

- `Design/VisualLockLayered/_OperationMapDenseCity/dense_city_full_map.png`
- `Design/VisualLockLayered/_OperationMapDenseCity/dense_city_civic_bazaar_oblique.png`
- `Design/VisualLockLayered/_OperationMapDenseCity/dense_city_bazaar_close.png`

## Stage 1 - Terrain Viability Map

**Status:** Complete  
**Progress:** 100%

- [x] Sample center, corners, and edge midpoints for every candidate road tile.
- [x] Sample every candidate building footprint using its actual rotated dimensions.
- [x] Classify terrain as flat, terraceable, road-only, or unsuitable.
- [x] Cache evaluations for deterministic repeatable editor builds.
- [x] Record counts and height-delta ranges in the editor build log.
- [x] Keep severe mountain cells outside developed districts.

**Validation result:** Unity compile passed. Initial audit reported `flat=3481`, `terraceable=1654`, `roadOnly=1051`, `unsuitable=4933`, and `maxPatchDelta=8.43m`. The classification thresholds were then widened for ordinary desert grading while preserving severe-slope rejection; the calibrated distribution requires regeneration validation.

**Acceptance**

- The builder can report exactly why a road or parcel is accepted, terraced, rerouted, or rejected.
- No placement decision depends on a single height sample.

## Stage 2 - Continuous Road Grading And Foundations

**Status:** Complete  
**Progress:** 100%

- [x] Remove random fringe-road cuts.
- [x] Solve connected road elevations from multi-point terrain samples.
- [x] Enforce a maximum visual road grade of 3-5%.
- [x] Give all intersections one shared elevation.
- [x] Add initial road foundations down to terrain.
- [x] Confirm transition ramps are unnecessary inside the single flat city grade; reserve them for later boundary roads only.
- [x] Raise road surfaces above the maximum sampled patch height to prevent terrain penetration.
- [x] Keep severe mountain crossings out of the road graph unless an authored pass is explicitly added.
- [x] Confirm the scene has a retained flat base mesh and that interior variation comes from separate relief prefabs.
- [x] Add an idempotent interior grading pass that suppresses dune, hill, mountain, rock, grass, and concrete conflicts without touching shared mesh assets.
- [x] Archive removed terrain outside the `MapBakeGroupAuthoring` hierarchy so it remains reversible and cannot be included by inactive-child surface baking.
- [x] Make road elevation planning use the authored city grade rather than stale pre-grading surface samples.
- [x] Validate flat-grade foundation appearance; block-like sides are not visible on the retained base.

**Validation result:** Regeneration produced `2,706` road tiles across `61` chunks. All `11,119` developed cells resolved to the shared authored grade with zero unsuitable cells. The oblique proof shows no terrain penetration or unexplained road elevation steps.

**Acceptance**

- No visible terrain penetrates a road surface.
- Adjacent road segments have no unexplained vertical steps or gaps.
- Roads remain connected across viable developed terrain.

## Stage 3 - Building Pads And Terraces

**Status:** In Progress  
**Progress:** 85%

- [x] Replace fallback `Y=0` placement in this edit-mode builder with an explicit placement result.
- [x] Create a level pad for every accepted building footprint.
- [ ] Add retaining walls or skirts where a terrace rises above surrounding terrain.
- [x] Reject footprints that exceed the terrace limit.
- [x] Align the visible building base to the pad elevation.
- [x] Place accepted city buildings against the authored flat grade so stale surface data cannot lift them above the retained base.
- [ ] Keep entrances facing the nearest road or pedestrian lane.
- [ ] Run building foundation visual and height audits after calibrated regeneration.

**Validation result:** Regeneration produced `6,944` buildings and `12` parks. Buildings resolve against the authored `Y=0` grade with a `0.035m` visible base offset. Visual proofs show no systematic floating or sinking; a numerical sample audit remains pending.

**Acceptance**

- No building visibly floats or sinks into terrain.
- Building base-to-pad error is at most 0.1m in validation samples.

## Stage 4 - Dirt-Road Infill And Connectivity

**Status:** In Progress  
**Progress:** 60%

- [x] Find unexplained empty buildable regions in the regenerated full-map and oblique proofs.
- [ ] Add connected unpaved local loops and dead-end lanes.
- [x] Reuse `SM_Env_DirtRoad_Straight_01`, `Corner_01`, and `End_01` for the current fringe network.
- [ ] Add deliberate `Exit_01/02` transitions where local dirt lanes meet collectors.
- [ ] Use slope transition pieces only at the developed-area boundary.
- [x] Preserve the existing sidewalk road family for collectors and civic roads.
- [x] Ensure every current generated block is bounded by road or pedestrian access.

**Acceptance**

- No large flat developed region is isolated from the road hierarchy.
- Dirt roads transition deliberately into paved collectors.

## Stage 5 - Parcel-Based District Layout

**Status:** In Progress  
**Progress:** 70%

- [x] Replace uniform rectangular filling with variable-size road-bounded blocks and road-facing frontages.
- [x] Mix narrow shop parcels, houses, civic plots, dense interiors, and open park blocks.
- [x] Vary frontage spacing and interior density by civic, inner-city, residential, and fringe districts.
- [x] Keep the bazaar dense and pedestrian-first.
- [x] Use larger blocks at the fringe and civic district, and denser smaller parcels toward the inner city.
- [ ] Replace periodic park selection with explicit district parcel roles and add compound/courtyard archetypes.

**Acceptance**

- The city does not read as repeated identical subdivision blocks.
- Large open spaces have a visible and documented purpose.

## Stage 6 - Natural City Detail Pass

**Status:** In Progress  
**Progress:** 40%

- [ ] Reuse demo-scene walls, stalls, awnings, utility props, trees, lights, barriers, and clutter.
- [ ] Add retaining stairs and pedestrian connections between terraces.
- [ ] Add road shoulders and believable transitions into undeveloped terrain.
- [ ] Introduce landmark and skyline variation without repeating one prefab excessively.
- [ ] Validate visual density from gameplay camera heights.

## Stage 7 - Final Validation

**Status:** Pending  
**Progress:** 0%

- [x] Unity batch compile succeeds.
- [x] Edit-mode city regeneration succeeds.
- [x] Road penetration visual audit passes.
- [ ] Building foundation audit passes.
- [ ] Road connectivity audit passes.
- [ ] Empty-region audit passes.
- [x] Capture updated full-map and civic/bazaar proofs.
- [ ] Capture dedicated residential, fringe, and terrain-transition proofs.
- [ ] Compare representative screenshots against `Assets/PolygonMilitary/Scenes/Demo.unity` and the approved city references.

**Current regeneration evidence:** `suppressedInteriorObjects=1010`, `flat=11119`, `unsuitable=0`, `buildings=6944`, `parks=12`, `roadTiles=2706`, `roadChunks=61`, `authoredCoreRenderers=772`.

## Current Blockers

None. The user explicitly authorized changing/flattening map areas used by the city. Android or device validation is not required for this visual edit-mode work. Runtime surface data must be rebaked after the layout is final; doing it earlier would create churn while roads and parcels are still changing.

## Next Task

Add parcel-aware dirt-lane infill to the remaining oversized empty blocks, then run numerical building-base and road-connectivity audits before the natural-detail pass.
