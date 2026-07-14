# Operation Map Scene Split And Generator Implementation Tracker

Date: 2026-07-14
Status: Audited and planned; no implementation started
Design sources: `../3D_SingleMap_Gameplay_Direction.md`, `../Level_And_Mission_Content_Plan.md`, `../3D_Operation_Map_Texture_Mask_Workflow.md`, `../M01_FirstContact_Production_Contract.md`, `gameplay_solid_ecs_contract.md`, `performance_regression_contract.md`

## Objective

Replace the current single hardcoded Match-map assumption with a reusable, config-driven operation-map pipeline:

```text
Mission -> ScenarioSetup -> OperationMapDefinition -> OperationMap scene/subscene
                                                       |
                                                       +-> static presentation manifest/chunks

Menu shell -> Match.unity runtime shell -> selected OperationMap loaded additively
```

The current large desert/base map must remain playable and become the first reusable operation map. It must not be deleted, flattened into a giant mesh, or copied together with its generated presentation chunks. `Match.unity` becomes a stable runtime shell only after the existing map has a validated map-specific bake, load, camera, minimap, authoring-conversion, and rollback path.

## Audit Result: 2026-07-14

The original draft had the right product direction but understated the current bake/runtime coupling. The live map is not only `Match.unity`; it is the following owned set:

| Current Contract | Audited Value |
|---|---|
| Canonical source scene | `Assets/Game/Scenes/Match.unity` |
| ECS subscene | `Assets/Game/Scenes/Match/MatchSubScene.unity` |
| Static presentation manifest | `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset` |
| Manifest schema | `1` |
| Canonical dependency hash | `0a587783351110d16353575d15d1b5cd` |
| Presentation content hash | `9eebc7c8aa774d5f505cb684099d133a` |
| Chunk size | `32` world units |
| Generated chunk scenes | `514` |
| Manifest source entries | `16,542` |
| Integrity ledger | `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json` |
| Bake owner | `StaticMapPresentationBaker` |
| Runtime stream owner | `MenuBootstrapCompositionSystemHelper` through `StaticMapPresentationStreamer` |
| Android build owner | `StaticMapAndroidBuildSceneResolver` |
| Match reference binder | `MatchSceneView` |

These values are an inspection snapshot, not an accepted Phase 0 baseline. Phase 0 must reproduce them through Unity validation and record the current dependency state before scene edits.

### Corrections Made By This Audit

- Do not duplicate `Assets/Game/GeneratedStaticMapPresentation`. Generated chunks and the integrity ledger are manifest-owned outputs, not reusable source assets.
- Do not extract the map before the baker, manifest ownership, scene wiring, Android build resolver, and streamer can select a map-specific manifest.
- First register the current map as a logical compatibility operation map while it still lives in `Match.unity`; this creates no scene or bake churn.
- Generalize static presentation ownership per operation map before creating the extracted scene. A bake for one map must never delete or rewrite another map's outputs.
- A duplicated source scene must receive a new scene GUID. Shared meshes, materials, textures, prefabs, and any assets that are moved rather than duplicated must retain their existing `.meta` GUIDs.
- `MapBuildingPlacementConfig`, `MapVehiclePlacementConfig`, map-surface references, lightmaps/probes, `MatchSubScene`, and hierarchy-path-based authoring conversion are part of map ownership and must be migrated with explicit parity checks.
- Invalid operation-map ids fail closed with a typed reason. The current map is an explicit compatibility id, not a silent fallback that hides bad scenario data.
- The texture/mask operation-map generator produces canonical source/metadata inputs. The static presentation baker consumes a reviewed canonical map and produces streamable render chunks. Their output roots and cleanup ownership remain separate.

## Non-Negotiable Safety Rules

- Do not destructively move, delete, or strip map content from `Assets/Game/Scenes/Match.unity` or `Assets/Game/Scenes/Match/MatchSubScene.unity` until the extracted operation map passes parity and rollback gates.
- Do not copy, hand-edit, or rename files under `Assets/Game/GeneratedStaticMapPresentation`; use the authoritative baker only.
- Do not let a new per-map bake overwrite the current schema-v1 manifest, integrity ledger, or 514 chunk scenes during the compatibility period.
- Preserve Unity `.meta` GUIDs for moved assets. Create a new GUID for a duplicated scene or asset; never copy one `.meta` file to two live paths.
- Keep current map baking deterministic. An identical second bake must report zero scene writes and zero stale deletions.
- Keep manifest cleanup fail-closed and restricted to paths previously owned by the same operation-map manifest.
- Keep `Match.unity` as the enabled base match scene until the operation-map load route is accepted. Load operation maps additively through the existing scene lifecycle.
- Do not combine all missions into one giant scene or include every mission's generated chunks in every build without an explicit build-content policy and size evidence.
- Do not introduce managers, controllers, facades, service locators, broad replacement shells, or new updating `MonoBehaviour` loops.
- Keep runtime gameplay policy in ECS data and systems. Scene references may use a non-updating serialized-reference view; loading, authoring, baking, UI, and Unity-object projection remain narrow managed boundaries.
- Keep Campaign map generation editor-time, deterministic, reviewed, and committed. Runtime procedural generation is out of scope for this tracker.
- Do not begin player-facing M01 implementation while the hold in `../M01_FirstContact_Production_Contract.md` remains active.

## Current Map Disposition

The current large map becomes the first reusable operation map through two stages.

| Stage | Identity / Path | Rule |
|---|---|---|
| Compatibility | `opmap.skirmish.desert_base_01` points to the current `Match.unity` map roots and schema-v1 manifest | No scene duplication, no map-content movement, no bake churn. |
| Extracted target | `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity` plus an optional map-owned subscene | Created only after per-map bake ownership exists; generated outputs live under a map-specific generated root. |
| Match shell target | `Assets/Game/Scenes/Match.unity` | Keeps camera/runtime bootstrap and shell references; no longer owns map geometry or map-specific authored placements after cutover. |

Primary use remains Skirmish, air/armor sandbox, fuel/logistics validation, and base-defense testing. The map may support multiple `ScenarioSetup` definitions. It should not become M01 merely because it already exists; M01 uses a dedicated smaller map unless visual and tutorial-readability evidence explicitly accepts a bounded reuse.

## Target Ownership Contract

### Match Runtime Shell

`Match.unity` owns the world camera, match lighting/post-processing boundary, match bootstrap reference binder, input/HUD integration references, and session lifecycle. `MatchSceneView` remains a serialized-reference binder and forwards Unity lifecycle into existing composition; it does not choose map policy or search scenes.

### Operation Map

An operation-map source scene owns canonical map geometry, map lights/probes that are intentionally map-specific, map authoring roots, map surface, map-specific ECS subscene references, and one non-updating serialized-reference view when required. That view may expose references but must contain no `Update`, `LateUpdate`, gameplay decisions, scene search, or service-location behavior.

`OperationMapDefinition` owns typed metadata and references:

- stable operation-map id and schema/content version;
- canonical source scene and optional subscene reference;
- static presentation manifest reference;
- map bounds, camera bounds, grid size, surface/height metadata, and blocker/path metadata;
- minimap projection and planning/battle camera ids;
- typed objective, spawn, deployment, build, civilian, hostile, base, resource, runway, helipad, lane, and debug anchors;
- map-owned building/vehicle placement config references where the compatibility path still needs them.

Large blocked-cell and height datasets must be represented by immutable blobs or chunked/compressed assets suitable for Burst/ECS consumption. Do not copy multi-million-cell managed arrays into runtime read models.

### Scenario Setup

`ScenarioSetup` owns mission/skirmish policy: operation-map id, starting units/buildings/resources, enemy setup, objectives, stars, rewards, allowed catalog, command restrictions, ARIA hooks, encounters, and feature gates. It does not own scene paths or inspect scene objects.

### Static Map Presentation

The existing static presentation pipeline remains the render optimization path. It must evolve from one global hardcoded manifest into map-scoped manifests with:

- operation-map id;
- canonical source scene GUID/path and dependency hash;
- map-specific output root and integrity ledger;
- deterministic chunk ids, bounds, source identity, and content hash;
- stale cleanup restricted to that manifest's prior owned paths;
- build inclusion controlled by a validated operation-map catalog/content policy.

`MenuBootstrapCompositionSystemHelper` may continue owning the existing streamer lifecycle. Map selection should supply the resolved manifest and camera to the existing boundary rather than introduce another broad per-frame owner.

## Performance And Build Contract

- Keep static presentation chunked and streamable. Do not combine a whole operation map into one mesh or one always-loaded scene.
- Reuse source meshes, materials, textures, and prefabs across maps; generated map scenes reference shared assets and must not clone binary assets.
- Load only chunks near the current camera and retain the existing bounded scene-operation work per frame.
- Drain presentation chunks before unloading a map. Restore/suppress canonical renderers transactionally through the existing ownership path.
- Cache map-id resolution and immutable metadata. Do not perform broad scene searches, `Resources.FindObjectsOfTypeAll`, hierarchy walks, or asset loads in gameplay hot paths.
- Precompute minimap projection/raster data. Marker updates remain dirty/version-gated and must not rebuild the full map each frame.
- Camera clamp and ground/surface queries use map metadata/blobs or existing ECS surface data, not repeated physics scans.
- Define a build-content policy before adding a second shipping map: built-in map set, optional downloadable content, or an accepted equivalent. Do not automatically append all future maps/chunks to Android builds.
- Measure per-map loaded memory, peak transition memory, chunk load latency, sustained FPS, draw calls, triangles, GC, APK size, and installed size.
- Keep the accepted release/device gates in `performance_regression_contract.md`; a visually correct map migration cannot waive performance or package-size failures.

## Risk Register

| Priority | Risk | Required Mitigation |
|---|---|---|
| P0 | A second map bake deletes or rewrites the current 514 scenes. | Per-map output roots, manifest identity, integrity ledgers, and ownership tests precede extraction. |
| P0 | Android build resolver includes stale chunks or every future map. | Catalog-driven build inclusion with exact manifest/hash/integrity validation and package-size evidence. |
| P0 | `Match.unity` is stripped before the new map is loadable. | Compatibility registration first; shell stripping is a later atomic cutover with rollback. |
| P0 | Scene duplication changes GlobalObjectIds and invalidates presentation/authoring references. | New manifest and map-owned placement configs are baked from the duplicated source; compare source-path and entity parity. |
| P1 | Map-authored buildings/vehicles duplicate, disappear, or use wrong ownership. | Move map-specific configs with the operation map and validate conversion plus source hiding. |
| P1 | `MatchSubScene`, lightmaps, probes, or map surface remain coupled to the shell. | Classify every dependency before movement and add scene-reference validation. |
| P1 | Gameplay starts before the selected map/subscene/presentation is ready. | One explicit readiness gate covering source scene, subscene, metadata, and required presentation preload. |
| P1 | Transition temporarily holds two large maps in memory. | Drain/unload old presentation before loading the next map where route design permits; record peak memory. |
| P2 | Generator metadata creates managed memory/GC pressure. | Immutable Burst-readable blobs, chunking/compression, and no per-frame conversion. |

## Progress Summary

Overall implementation progress: 0% (0/138 checklist items complete).

Progress is checklist-based. Each checkbox below counts as one item. Update this summary and the validation log in the same stable implementation commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Reproducible baseline and rollback | Not started | 0 | 12 | 0% | Required before any scene or bake edit. |
| 1. Operation-map and scenario data contracts | Not started | 0 | 12 | 0% | Defines typed identity, metadata, and catalog ownership. |
| 2. Per-map static presentation ownership | Not started | 0 | 14 | 0% | Removes the single global bake/manifest assumption. |
| 3. Current-map compatibility registration | Not started | 0 | 10 | 0% | Makes the existing map addressable without moving it. |
| 4. Non-destructive scene ownership split | Not started | 0 | 14 | 0% | Extracts current map only after map-specific baking is safe. |
| 5. Runtime selection, loading, and teardown | Not started | 0 | 14 | 0% | Loads one selected operation map through existing lifecycle. |
| 6. Metadata, camera, minimap, and movement binding | Not started | 0 | 12 | 0% | Removes raw scene-name dependence from map behavior. |
| 7. M01 operation-map slice | Not started / gated | 0 | 10 | 0% | No player-facing integration before the M01 hold is released. |
| 8. Editor-time texture/mask generator | Not started | 0 | 12 | 0% | Produces canonical map/metadata inputs, not runtime maps. |
| 9. Mission and Skirmish scenario rollout | Not started | 0 | 10 | 0% | Reuses maps through config-driven setup. |
| 10. Full validation and rollout | Not started | 0 | 18 | 0% | Bake, parity, architecture, performance, build, and device gates. |

## Phase 0: Reproducible Baseline And Rollback

- [ ] Capture the exact Unity scene setup for `Match.unity` and `MatchSubScene.unity`.
- [ ] Reproduce the current manifest schema, canonical path/hash, content hash, chunk size, chunk count, and source count through an authoritative Unity probe.
- [ ] Hash the manifest, integrity ledger, every generated scene, and every generated `.meta` file.
- [ ] Record the authoritative bake, scene-wiring, structural-validation, and Android build-resolution commands and log paths.
- [ ] Inventory every `MatchSceneView` serialized reference and classify it as shell-owned, map-owned, shared config, or temporary compatibility data.
- [ ] Inventory root objects under `Match.unity` and classify map geometry, map authoring, camera, lighting, bootstrap, and runtime-only ownership.
- [ ] Inventory `Match_MapBuildingPlacement_Config` and `Match_MapVehiclePlacement_Config`, including hierarchy-path and source-hiding assumptions.
- [ ] Inventory minimap projection, camera clamp, initial camera, full-map bounds, and objective-focus sources.
- [ ] Inventory ground-height, map-surface, grid, blockers, terrain, runway, and helipad metadata sources.
- [ ] Inventory static presentation streamer, canonical-renderer suppression, teardown, and Android build-scene ownership.
- [ ] Capture current Editor launch, Android launch, load time, loaded memory, APK/installed size, sustained FPS, draw, and GC evidence accepted for comparison.
- [ ] Write an exact rollback recipe that restores scene setup, manifest/integrity files, generated scenes, scene references, configs, and build settings.

Exit criteria:

- Current behavior and outputs are reproducible from commands, not inferred from repository inspection.
- The migration can detect unintended scene, manifest, generated-output, config, or build-content changes.
- No scene, config, baker, manifest, or generated output changes occur in this phase.

## Phase 1: Operation-Map And Scenario Data Contracts

- [ ] Approve canonical operation-map ids: `opmap.<mode-or-chapter>.<slug>`.
- [ ] Approve canonical scenario ids: `scenario.<chapter>.<mission>.<slug>` and `scenario.skirmish.<slug>`.
- [ ] Add the `OperationMapDefinition` config/data contract without storing hot runtime policy in a managed asset.
- [ ] Add the `ScenarioSetup` config/data contract or align with an already accepted concrete config type before implementation.
- [ ] Add source-scene, optional subscene, static presentation manifest, and map-content version references.
- [ ] Add planning camera, battle camera, and minimap projection ids.
- [ ] Add typed ids for spawn, objective, deployment, build, civilian, hostile, base, resource, runway, helipad, lane, and debug anchors.
- [ ] Add map bounds, camera bounds, grid, surface/height, and blocker/path metadata references.
- [ ] Add source identity, schema version, content hash, and generated-metadata hash fields.
- [ ] Add an operation-map catalog/registry resolved once at launch or match transition, with no hot-path asset search.
- [ ] Add validation for unique ids, missing assets, invalid bounds, duplicate anchors, stale hashes, and unresolved scenario-to-map references.
- [ ] Update architecture docs with the exact `Mission -> ScenarioSetup -> OperationMapDefinition -> scene/subscene/manifest` ownership chain.

Exit criteria:

- A scenario resolves one validated operation map through typed data.
- Runtime code does not need hardcoded scene paths or raw object names to choose a map.

## Phase 2: Per-Map Static Presentation Ownership

- [ ] Introduce an editor bake input descriptor carrying operation-map id, source scene, source map root, output root, manifest path, integrity path, and chunk size.
- [ ] Refactor `StaticMapPresentationBaker` so the current hardcoded constants remain only a compatibility entry point during migration.
- [ ] Advance the manifest schema to include operation-map id and canonical scene GUID/path identity with an explicit migration test.
- [ ] Use one generated output root per operation map.
- [ ] Namespace generated scene names by operation map so chunk coordinates cannot collide across maps.
- [ ] Use one integrity ledger per operation map.
- [ ] Restrict stale cleanup to paths owned by the same map id, manifest, and output-root contract.
- [ ] Keep transaction journaling and rollback scoped to the active map's mutable files and `.meta` files.
- [ ] Exclude every generated operation-map output root from canonical source dependency hashing.
- [ ] Preserve zero-write/zero-delete behavior on an identical second bake for each map.
- [ ] Add a two-map test proving a bake of map B cannot modify or delete map A outputs.
- [ ] Make scene wiring validate the selected map's manifest rather than one global manifest path.
- [ ] Make Android build resolution validate a catalog-selected manifest set and reject stale/unowned chunk scenes.
- [ ] Extend ownership, integrity, rollback, structural parity, build resolver, and no-op reuse tests for multiple maps.

Exit criteria:

- The current map still produces its accepted output through the compatibility entry point.
- Two operation maps can coexist without shared mutable generated ownership.
- An identical rebake writes no scenes and deletes no scenes.

## Phase 3: Current-Map Compatibility Registration

- [ ] Confirm the logical id `opmap.skirmish.desert_base_01` or record the approved replacement.
- [ ] Register that id against the current `Match.unity`, `MatchSubScene.unity`, current manifest, and current map-specific configs without moving files.
- [ ] Create the compatibility `OperationMapDefinition` with exact current bounds, grid/surface references, camera ids, minimap id, and map anchors.
- [ ] Record current building/vehicle placement counts and source paths as map-owned compatibility data.
- [ ] Keep the current schema-v1 manifest readable during the map-specific schema transition.
- [ ] Launch the existing match through the compatibility operation-map id with no visual or gameplay behavior change.
- [ ] Run the current map bake twice and prove accepted parity followed by zero writes/deletes.
- [ ] Validate map-authored building, vehicle, aircraft, runway, helipad, blocker, and source-hiding behavior.
- [ ] Validate Android build-scene resolution includes only the compatibility map's accepted chunks.
- [ ] Update this tracker with exact paths, hashes, counts, commands, logs, and screenshots.

Exit criteria:

- The current map is addressable by operation-map id before extraction.
- Existing match behavior, baking, streaming, and Android inclusion remain stable.

## Phase 4: Non-Destructive Scene Ownership Split

- [ ] Finalize the shell-owned versus map-owned dependency inventory from Phase 0.
- [ ] Create `Assets/Game/Scenes/OperationMaps/Skirmish/` and its Unity folder `.meta` files through Unity/AssetDatabase-safe tooling.
- [ ] Create `opmap_skirmish_desert_base_01.unity` as a staged duplicate with a new scene GUID; do not copy generated chunks or their `.meta` files.
- [ ] Add a non-updating operation-map serialized-reference view only if direct scene references require it.
- [ ] Keep the original `Match.unity` fully functional until the extracted map passes all parity gates.
- [ ] Move/copy only classified map roots into the staged operation-map scene while shared binary assets remain referenced, not duplicated.
- [ ] Assign or create the map-owned subscene without breaking the original `MatchSubScene` compatibility path.
- [ ] Generate map-specific building and vehicle placement configs from the staged scene rather than reusing stale hierarchy assumptions blindly.
- [ ] Bind map surface, grid, blockers, lightmaps/probes, runways, helipads, bounds, and metadata from the staged scene.
- [ ] Bake a map-specific presentation manifest/chunk set from the staged operation-map scene.
- [ ] Validate source GlobalObjectIds, hierarchy paths, authored conversion counts, canonical renderer suppression, and entity parity.
- [ ] Remove map roots and map-specific references from `Match.unity` only in the atomic cutover commit after staged parity passes.
- [ ] Validate the stripped `Match.unity` remains a functional runtime shell and the extracted scene contains no shell bootstrap/HUD policy.
- [ ] Keep a revertable checkpoint that restores the original scene, configs, subscene reference, manifest binding, and build settings.

Exit criteria:

- The current large map exists as a separate operation-map scene with its own manifest and metadata.
- `Match.unity` is a runtime shell only after the new route is proven.
- No binary source art is duplicated and no unrelated generated map is changed.

## Phase 5: Runtime Selection, Loading, And Teardown

- [ ] Carry active mission, scenario, and operation-map ids in typed session/ECS state.
- [ ] Resolve the operation-map catalog entry before beginning the match load transition.
- [ ] Load the selected canonical operation-map scene additively through the existing scene lifecycle boundary.
- [ ] Load and await the selected map's optional ECS subscene through the accepted Entities scene path.
- [ ] Bind the map's serialized-reference view/config only after scene load succeeds.
- [ ] Publish active map ids, immutable metadata/blob references, and readiness state to ECS.
- [ ] Bind the existing static presentation streamer to the selected manifest and world camera.
- [ ] Gate `BeginGameplay` on source scene, subscene, metadata, authored conversion, and required presentation preload readiness.
- [ ] On exit/switch, stop map gameplay, drain presentation chunks, and then unload map/subscene content in a deterministic order.
- [ ] Restore canonical renderer ownership correctly on failure, teardown, or compatibility fallback.
- [ ] Clear map-specific ECS entities, singleton/buffer data, cached anchors, and scene references before a later map loads.
- [ ] Reject missing/stale map ids with typed reason codes; do not silently load the current map in production.
- [ ] Add tests for valid load, missing id, stale manifest, interrupted load, teardown, retry, and sequential map switching.
- [ ] Validate no new update-loop `MonoBehaviour`, manager, controller, facade, service locator, or broad shell is introduced.

Exit criteria:

- `Match.unity` can start one selected operation map by config and unload it cleanly.
- Failure is explicit and leaves no partially loaded map or suppressed renderer state.

## Phase 6: Metadata, Camera, Minimap, And Movement Binding

- [ ] Bind camera clamp bounds from active operation-map metadata.
- [ ] Bind planning, battle, and initial camera transforms from typed camera ids/anchors.
- [ ] Bind minimap projection and cached raster data from the active map.
- [ ] Bind objective focus/jump to typed operation-map anchor ids.
- [ ] Bind ARIA `Show Me` and camera intents to typed operation-map anchors.
- [ ] Bind friendly/hostile deployment and spawn anchors.
- [ ] Bind runway and helipad anchors for taxi, takeoff, return, and landing behavior.
- [ ] Bind blocker/path/build metadata to movement and placement validation.
- [ ] Bind ground-height/surface metadata for soldiers, vehicles, and aircraft clearance.
- [ ] Add validation for every anchor required by each active scenario.
- [ ] Add editor/debug overlays for bounds, cameras, anchors, blockers, lanes, surface samples, and minimap extents.
- [ ] Add mobile-safe caching and allocation tests for minimap, camera, surface, and anchor lookups.

Exit criteria:

- Camera, minimap, objectives, spawns, movement, aircraft, and placement use active map data.
- Gameplay systems do not depend on raw scene object names for map-critical behavior.

## Phase 7: M01 Operation-Map Slice

Implementation blocker: M01 planning and scaffolding may be studied, but M01 implementation and player-facing integration must not begin until `../M01_FirstContact_Production_Contract.md` releases its FirstLaunch Phase 10R / Gate 9R hold.

- [ ] Reconfirm the M01 implementation hold before changing player-facing routes or assets.
- [ ] Use the dedicated `opmap.ch01.district_edge_01` unless accepted evidence approves bounded reuse of the current map.
- [ ] Create the M01 operation-map source scene/subscene only after the hold is released.
- [ ] Create M01 `OperationMapDefinition` metadata with required bounds and surface/path data.
- [ ] Add `camera.ch01.m01.planning`, battle camera data, and `minimap.ch01.m01.projection`.
- [ ] Add M01 objective, spawn, patrol lane, tutorial move target, and camera-focus anchors.
- [ ] Add M01 `ScenarioSetup` with rifle squad, enemy patrol, restrictions, objectives, rewards, and ARIA hooks.
- [ ] Validate direct select/move/attack/objective behavior against map metadata.
- [ ] Validate camera bounds, minimap, objective jump, ARIA focus, and result/replay rebuild.
- [ ] Record gate release evidence and M01 visual/playable acceptance before player-facing integration.

Exit criteria:

- M01 has a dedicated, readable map contract after the hold is released.
- M01 does not require unrelated base, fuel, building, or current-big-map systems.

## Phase 8: Editor-Time Texture/Mask Generator

- [ ] Define the reviewed map-pack folder/manifest contract for base visual, blocker mask, height mask, tree/rock masks, and generation seed/version.
- [ ] Add an editor-only generator entry point under existing tooling conventions.
- [ ] Generate/update operation-map metadata without writing static presentation outputs directly.
- [ ] Generate blocker/grid metadata deterministically.
- [ ] Generate compressed/chunked height and surface samples deterministically.
- [ ] Generate tree, rock, and decoration placement candidates deterministically.
- [ ] Generate reserve zones, lanes, and connectivity metadata.
- [ ] Generate debug overlays for blockers, height, anchors, reserve zones, lanes, and camera bounds.
- [ ] Generate canonical source scene/subscene content in deterministic chunks while referencing shared assets.
- [ ] Preserve generated source/metadata file and `.meta` stability on an identical generation.
- [ ] Validate connected playable zones, blocked outer belts, clear build reserves, and map bounds.
- [ ] Log source hashes, seed/version, written/reused/stale counts, metadata size, scene count, and validation results.

Exit criteria:

- Reviewed map packs produce deterministic canonical operation-map source and metadata.
- The static presentation baker can consume the result without shared cleanup ownership or no-op churn.

## Phase 9: Mission And Skirmish Scenario Rollout

- [ ] Add a Skirmish/sandbox `ScenarioSetup` for `opmap.skirmish.desert_base_01`.
- [ ] Gate build, scan, support, aircraft, fuel logistics, resource exchange, and fabrication per scenario.
- [ ] Define objectives and star goals in scenario data, not scene branches.
- [ ] Define starting units, buildings, and resources in scenario data.
- [ ] Define enemy setup, AI profile ids, and encounter references in scenario data.
- [ ] Define Campaign rewards, unlocks, and consequences in scenario data.
- [ ] Publish UI read models for active mission/scenario/map identity.
- [ ] Validate objective/reward/feature references against operation-map anchors and catalogs.
- [ ] Add one preserved-current-map Skirmish probe and sequential reload probe.
- [ ] Add Campaign/M01 probes only after the M01 hold is released.

Exit criteria:

- Multiple scenarios can reuse one operation map without modifying its scene.
- Scenario data, not scene contents alone, controls game rules and starting state.

## Phase 10: Full Validation And Rollout

- [ ] Run `git diff --check` and scoped asset/meta integrity checks.
- [ ] Run architecture/naming guardrails for forbidden manager/controller/facade/service/runtime-loop drift.
- [ ] Run compile validation with zero new errors.
- [ ] Run operation-map/scenario/catalog/config validation tests.
- [ ] Run current and multi-map static presentation ownership, integrity, rollback, structural, no-op, and stale-cleanup tests.
- [ ] Run scene-reference validation for the shell, current map, subscenes, manifests, configs, and M01 only when allowed.
- [ ] Run original current-map launch before extraction and compatibility launch after each migration phase.
- [ ] Run extracted-current-map load, gameplay, teardown, retry, and sequential reload validation.
- [ ] Run camera bounds, initial camera, minimap, objective, and ARIA anchor validation.
- [ ] Run movement, surface grounding, aircraft clearance, runway/helipad, blockers, and building placement validation.
- [ ] Run map-authored building/vehicle/aircraft conversion and source-hiding regression validation.
- [ ] Run editor generator deterministic/no-op and connectivity validation.
- [ ] Run Editor performance comparison for load time, frame time, draw, triangles, memory, and GC.
- [ ] Run Android build-scene inclusion, APK/installed size, startup, memory, sustained FPS, and thermal validation.
- [ ] Verify only the approved built-in operation-map set is packaged and no stale generated scenes are included.
- [ ] Capture accepted screenshots for top-down, oblique, low-ground, minimap, bounds, and map transition states.
- [ ] Update `README.md`, `Design/README.md`, this percentage table, and exact command/log evidence.
- [ ] Commit and push only after each stable slice passes its owned gates; keep scene extraction and shell cutover independently revertable.

Exit criteria:

- The current large map is preserved and selectable as a separate operation map.
- `Match.unity` is a reusable shell with no hidden dependency on the current map.
- Per-map generation and static presentation baking are deterministic and no-op-safe.
- Build inclusion is explicit and Android package/performance evidence remains acceptable.
- M01 and future missions have a validated path without putting every level in one scene.

## Validation Log

| Date | Slice | Commands / Evidence | Result | Notes |
|---|---|---|---|---|
| 2026-07-14 | Initial tracker creation | `git diff --check` | Passed | Documentation only; no scene, code, prefab, bake, or asset migration. |
| 2026-07-14 | Architecture and bake audit | Repository inspection of current baker, manifest, integrity ledger, streamer, Android resolver, `MatchSceneView`, map placement configs, M01 hold, and map workflow; `git diff --check` | Passed | Corrected migration order, current 514-chunk/16,542-source baseline snapshot, per-map ownership requirement, build-size risk, scene GUID rules, and M01 gating. No Unity validation claimed. |

## Open Decisions

| Topic | Current Recommendation | Decision Gate |
|---|---|---|
| Current large map id | `opmap.skirmish.desert_base_01` | Approve in Phase 3 before catalog data is committed. |
| Extracted source path | `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity` | Confirm after Phase 0 dependency classification. |
| Current map subscene | Make it map-owned if its authored ECS content is map-specific; keep only truly shared runtime ECS setup in shell composition. | Decide from Phase 0 inventory, not filename alone. |
| Manifest schema migration | Keep schema-v1 compatibility while introducing map id/source GUID and map-scoped outputs in a new schema. | Approve with multi-map ownership and no-op tests in Phase 2. |
| Runtime loading | Keep `Match.unity` as enabled base scene and load one operation-map source/subscene additively through existing lifecycle. | Validate in Phase 5. |
| Build inclusion | Start with an explicit built-in map catalog; do not package every future map automatically. Evaluate downloadable content only when content volume requires it. | Prove APK/installed-size behavior before second shipping map. |
| M01 source | Dedicated `opmap.ch01.district_edge_01`. | Reconfirm after FirstLaunch releases the M01 hold. |
| Generator | Editor-time deterministic source/metadata generation only; static presentation remains a separate downstream bake. | Runtime procedural generation stays out of scope. |
