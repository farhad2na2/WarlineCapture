# Operation Map ECS Presentation And Dense City Editor Bake Implementation Tracker

Date: 2026-07-21
Status: Implementation started; Phase 0 complete; Phase 0A transactional candidate Bake All and accepted-source-to-candidate-to-baked owner-matrix/transformed-renderer-bounds parity complete (production still StaticSceneChunks; production Addressables untouched); Addressables-loaded runtime parity, fixed-camera parity, Editor lifecycle, and Android acceptance next; previously generated dense city cleared by project owner
Parent tracker: `operation_map_scene_split_and_generator_tracker.md`
Related contracts: `file_naming_architecture_contract.md`, `gameplay_solid_ecs_contract.md`, `performance_regression_contract.md`, `operation_map_runtime_ownership_chain.md`, `operation_map_scene_split_rollback_recipe.md`
Related R&D only: `runtime_operation_map_generation_rnd_implementation_tracker.md`

## 1. Objective

Migrate all existing visual content on `opmap.skirmish.desert_base_01` and all future generated dense-city content to one ECS-first map SubScene presentation path. Existing and generated buildings, vehicles, roads, bridges, terrain, mountains, vegetation, props, infrastructure, and background visuals must bake from editor GameObjects to runtime entities without shipping or streaming their GameObject hierarchies.

The migration preserves the existing military base, handmade city, placement behavior, surface/blocker data, camera/minimap/runway/helipad ownership, and deterministic map identity. The current 514-scene static-presentation package remains rollback evidence until ECS visual/runtime parity is accepted, then leaves the production Addressables/build path.

The production result is hybrid:

```text
Editor authoring scene
  -> classify and migrate existing map visuals to protected SubScene entity-authoring roots
  -> deterministic dense-city generation under one logical disposable ownership set
  -> generation-time semantic ownership and collider removal
  -> all existing and generated visual placements baked to ECS entities and Entities Graphics
  -> coarse ECS surface/blocker data plus compact entity-scene presentation data
  -> thin generated runtime-binding scene
  -> local Addressables package

Runtime
  -> load thin operation-map binding scene additively
  -> load the map SubScene containing existing and generated gameplay/render-only entities once
  -> publish immutable map/surface/blocker ECS data
  -> use Entities Graphics batching, frustum culling, and LOD without per-camera scene streaming
  -> unload all map entities, map ECS state, subscene, and binding scene in the accepted order
```

This tracker does not authorize runtime procedural city generation. Existing-map migration and dense-city generation are editor operations. Runtime only loads deterministic baked entity output.

## 2. Accepted Decisions

- Regenerate the city correctly instead of converting the previously generated 25,000-object hierarchy.
- Migrate the current map's existing renderers to the same ECS presentation path before adding the dense city.
- Treat the existing 451 building and 29 vehicle placement records as migration inputs, not permanent justification for managed runtime visual GameObjects.
- Move existing map visual authoring into protected map SubScene roots through a transactional editor migration; do not manually reparent thousands of objects.
- Classify content while generating it. A post-pass validates ownership but never guesses ownership from names.
- Keep every generated object beneath one of two scene-scoped roots in the same disposable generation ownership set: immutable/proxy source in the operation-map authoring scene and damageable building authoring in the existing map SubScene.
- Keep migrated military, handmade city, runway, resources, mountains, roads, and authored mission content outside disposable generated ownership roots but inside protected authored ECS presentation ownership.
- Keep persistent hand edits outside the generated ownership roots under an authored override root.
- Remove collider and rigidbody components from generated instances immediately. Do not merely disable them.
- Reject every production map bake that contains any `Collider`, `Collider2D`, `Rigidbody`, or `Rigidbody2D` in map-owned build content.
- Use simplified semantic bake proxies for terrain, roads, bridges, ramps, water exclusion, and blockers. Never rasterize detailed presentation meshes as gameplay navigation geometry.
- Treat every generated house/building that can receive damage as an ECS gameplay entity with Entities Graphics presentation. Do not create or retain a runtime building GameObject for it.
- Bake intact and destroyed render hierarchies to entity children and switch entity visual state when health reaches zero. Runtime building destruction must not instantiate a replacement GameObject.
- Treat roof equipment, interior dressing, shop goods, tent contents, signs, awnings, lamps, furniture, and every other visual attached to a damageable building as part of that building's presentation ownership. Bake those descendants beneath either its intact or destroyed visual root so the complete intact set disappears and the complete destroyed set appears in one destruction transition.
- Bake only independent trees, rocks, lights, props, modular road/sidewalk segments, bridge segments, vegetation items, and background modules as render-only ECS entities sharing mesh/material assets. A prop that must disappear with a building is never an independent render-only entity.
- Bake unique generated terrain, canal, mountain, road, or bridge visuals as one or more render-only entities unless measured Android evidence approves a specific heavyweight exception.
- Do not pool permanent map visuals. Pooling remains for transient spawned effects; permanent placements are baked entity instances with no spawn/despawn loop.
- Keep all generated city entities resident for the lifetime of the loaded map. Camera movement changes rendering visibility through Entities Graphics only; it does not load/unload generated city scenes or simulation entities.
- Apply the same resident entity/culling rule to existing map visuals. Existing and generated visual entities load once and unload once with the map.
- Reuse the accepted operation-map runtime binding scene, map SubScene, local Addressables layout, readiness, failure, and teardown owners.
- Keep the existing static-presentation manifest/streamer only during migration rollback. After ECS parity acceptance, `opmap.skirmish.desert_base_01` uses entity-scene presentation mode, its manifest/514 chunk scenes leave production Addressables/build ownership, and the streamer is not bound for this map.
- Do not introduce another map loader, streamer, runtime-generation host, update-loop `MonoBehaviour`, manager, controller, facade, service, provider, or service locator.
- Do not ship existing-map or dense-city authoring GameObject hierarchies. Ship the thin runtime binding scene and baked ECS entity scene data/metadata with shared art dependencies.
- Preserve the accepted Editor placement exactly through migration, baking, Addressables packaging, and runtime load. The authoritative source world matrix for every renderer owner must match its candidate-SubScene authoring matrix, baked ECS matrix, and Addressables-loaded runtime matrix within the tolerances in Section 12.0A. Runtime systems must not recompute, offset, normalize, ground-snap, or otherwise overwrite permanent map-visual transforms.

## 3. Baseline Audit

### 3.1 Existing Map Baseline

Accepted current-map evidence reports:

- `16,542` static-presentation sources serialized into `514` generated scene chunks;
- `451` building and `29` vehicle placement records;
- map-specific static manifest, integrity ledger, local Addressables groups, and camera-driven scene streamer;
- existing building placement startup that creates managed `RuntimeBuildingEntity`/GameObject presentation;
- existing vehicle/unit authoring that already demonstrates ECS prefab/render conversion and must be audited rather than reimplemented;
- a thin runtime binding scene plus one map SubScene that currently owns grid/initial-unit authoring but not the full visual map.

These are migration baselines and rollback inputs, not the accepted final runtime representation.

### 3.2 Dense City Audit

The first giant-city generation was cleared and is retained only as audit evidence. It produced:

| Metric | Before generation | After generation | Delta |
|---|---:|---:|---:|
| Authoring scene bytes | `45,755,844` | `225,703,659` | `+179,947,815` (`+393.3%`) |
| Scene GameObjects | `129` | `25,411` | `+25,282` |
| Scene MeshRenderers | `5` | `13,928` | `+13,923` |
| Prefab instances | `13,313` | `49,282` | `+35,969` |
| Generated buildings | N/A | `5,759` | `+5,759` |
| Generated road tiles | N/A | `4,650` | `+4,650` |
| Generated road chunks | N/A | `83` | `+83` |
| Generated parks | N/A | `15` | `+15` |
| Enabled MeshColliders in saved scene | N/A | `931` | unacceptable |
| Enabled BoxColliders in saved scene | N/A | `161` | unacceptable |
| New generated bake-group ownership markers | `0` | `0` | generated content unclassified |

Additional findings:

- The generator used 21 `DenseCity_*` roots that mixed presentation, surface, and blocker responsibilities.
- The generated city extent was approximately `2560 x 1280`, while the active gameplay grid was `2048 x 1024` at `1 m` cells.
- The generator extended approximately `512 m` west and `128 m` north/south outside the active grid.
- Expanding the gameplay grid to the full generated rectangle would increase cells from `2,097,152` to `3,276,800`, approximately `56%`.
- Surface authoring used `2 x 2` samples per cell. Full-grid expansion would increase nominal sample positions from approximately `8.39 million` to `13.11 million`.
- Static presentation currently uses `32 m` chunks. A `2560 x 1280` rectangle permits up to `3,200` logical chunk coordinates before occupancy filtering.
- Generated facade material variants already enabled GPU instancing, but renderer, shadow, entity-scene, Entities Graphics batch, and LOD budgets were not validated.
- The static-presentation baker's `LODGroup` source exclusion does not govern generated city entity presentation. Generated LOD authoring must instead prove correct Entities Graphics baking and device behavior.

## 4. Scope

### 4.1 In Scope

- Existing-map renderer/placement ownership inventory and explicit ECS migration classification.
- Transactional movement of existing map visual authoring into protected map SubScene roots.
- ECS conversion of current authored buildings, vehicles, roads, bridges, terrain, mountains, vegetation, props, infrastructure, and background visuals.
- Conditional operation-map content validation/readiness for entity-scene presentation without a required static manifest.
- Retirement of the current map's 514 static presentation scenes, manifest binding, source suppression, and camera-streamer dependency after accepted rollback-safe cutover.
- Dense-city generator semantic hierarchy.
- Disposable generated-root identity and deterministic regeneration.
- Authored-content protection and persistent override ownership.
- Zero-collider enforcement for generated and map-owned production content.
- Generation-time surface and blocker records.
- Simplified surface/blocker proxy generation.
- Dense-city bake-readiness validation.
- One-button integration with `Game/Operation Maps/Bake Current Map (All)`.
- ECS entity-scene ownership, shared render-asset validation, Entities Graphics batching, LOD/impostor policy, and entity-scene size/runtime budgets.
- Isolation from and eventual retirement of the existing static-presentation package so no existing or generated renderer is serialized twice.
- Thin runtime binding scene and local Addressables packaging already owned by the operation-map pipeline.
- ECS building authoring/baking, health/destruction state, entity visual switching, and target/blocker integration.
- Editor, PlayMode, Android, build-layout, deterministic-output, and performance validation.

### 4.2 Out Of Scope

- Runtime procedural city generation for production.
- A second physical operation map.
- Remote Addressables delivery.
- Manual reparenting of thousands of generated objects.
- Name-substring or prefab-name ownership inference.
- Unity Physics, collider-based pathing, raycast-based map blocking, or Rigidbody gameplay.
- Shipping the authoring scene as the runtime map scene.
- Replacing the existing operation-map loader. The existing streamer remains implemented for legacy/future compatibility but is unbound from this ECS-presented map after cutover.
- A GameObject pool for permanent generated map visuals.
- Generated-city static-presentation scene chunks without a separately approved, measured heavyweight-content exception.

## 5. Ownership And Scene Hierarchy

The required logical authoring hierarchy spans the two existing editor source scenes because only objects authored in a SubScene are converted to entity scene data:

```text
Operation-map authoring scene
  OperationMapSceneView
  MapSurfaceAuthoring
  GridAuthoringConfig
  EmptyLegacyPlacementRootsDuringMigration
  MissionAndInfrastructureMetadataNotOwnedByTheSubScene

  AuthoredCityOverrides
    PersistentPresentation
    PersistentSurfaceAuthoring
    PersistentGameplayAuthoring
    GenerationExclusions

  Generated_GiantDenseMiddleEasternCity_MapBakeSource
    BakeSources
      Terrain
      Roads
      Bridges
      Ramps
      Blockers

Existing map SubScene authoring scene
  Grid
  InitialUnitsSpawnerAuthoring
  AuthoredOperationMapEntityPresentation
    GameplayBuildings
      MilitaryBase
      HandmadeCity
      Infrastructure
    GameplayVehicles
    RenderOnly
      Terrain
      RoadsAndBridges
      Mountains
      Vegetation
      Props
      Infrastructure
      Horizon
  Generated_GiantDenseMiddleEasternCity_EntityPresentation
    GameplayBuildings
      Buildings
      CivicAndMarket
    RenderOnly
      Infrastructure
      Vegetation
      Props
      Horizon
```

Rules:

- `AuthoredOperationMapEntityPresentation` is identified by `OperationMapEntityPresentationRootAuthoring`, belongs to the current operation-map id, and is never deleted by dense-city regeneration.
- Existing visual migration is transactional and stable-identity-driven. It moves/copies only reviewed renderer/placement owners, validates the candidate SubScene, and leaves the accepted source/static package untouched until cutover.
- Existing objects with unresolved gameplay, script, light, animation, or cross-scene reference ownership fail migration and require an explicit disposition. Names, prefab filenames, and renderer appearance are not classification evidence.
- After cutover, existing authored building/vehicle visuals and placements are owned by SubScene entities. Legacy roots may remain empty only while serialized compatibility fields exist; they do not spawn runtime GameObjects.
- Both generated roots are identified by `DenseCityGeneratedRootAuthoring`, a shared generation id, and distinct `DenseCityGeneratedRootRole` values, not by names alone.
- Exactly one `MapBakeSource` root must exist in the operation-map authoring scene and exactly one `EntityPresentationSource` root must exist in its referenced map SubScene.
- Regeneration deletes only those two marked roots and recreates them transactionally as one ownership set.
- `AuthoredCityOverrides` is never deleted or reparented by generation.
- Existing map roots are immutable protected-area inputs. Generation may inspect their precomputed renderer bounds during the editor operation but may not move, disable, rename, or modify them.
- Every damageable generated building belongs beneath `EntityPresentationSource/GameplayBuildings`, has explicit entity-building authoring metadata, and is excluded from static-presentation source collection.
- Every generated non-gameplay visual belongs beneath `EntityPresentationSource/RenderOnly` and bakes to renderer entities with no gameplay components or update system.
- No generated city renderer may exist beneath `MapBakeSource`, the operation-map authoring scene, or any static-presentation source root.
- Every generated surface proxy belongs beneath exactly one `BakeSources` role root.
- Proxy roots may contain `MeshFilter` data needed by the surface baker, but must not contain `MeshRenderer`, collider, Rigidbody, gameplay component, or runtime update behavior.
- Detailed presentation objects must not exist beneath a terrain, road, bridge, ramp, or blocker bake root.
- `OperationMapSceneView.BuildingAuthoringRoot`, `VehicleAuthoringRoot`, `MapBuildingPlacementConfig`, and `MapVehiclePlacementConfig` are migration inputs. In `EntityScene` presentation mode, current-map startup does not spawn from them and validation does not require non-empty placement configs.

## 6. Semantic Generation Contract

### 6.1 Existing Map Migration Contract

| Existing content | Final SubScene ownership | Runtime treatment |
|---|---|---|
| Authored map building | `AuthoredOperationMapEntityPresentation/GameplayBuildings` | ECS health/faction/footprint/targeting plus intact/destroyed Entities Graphics children; no `RuntimeBuildingEntityLink` or visual GameObject |
| Authored map vehicle | `AuthoredOperationMapEntityPresentation/GameplayVehicles` | Reuse current unit/vehicle ECS Baker and render-prefab path; prove no source visual GameObject remains or is duplicated |
| Terrain, road, sidewalk, bridge, runway visual | `AuthoredOperationMapEntityPresentation/RenderOnly` | Render-only entities; existing ECS surface/runway metadata remains authoritative |
| Mountain, rock, vegetation, prop, infrastructure, horizon visual | `AuthoredOperationMapEntityPresentation/RenderOnly` | Render-only entities sharing mesh/material assets |
| Building/vehicle placement configs | migration evidence only after cutover | No current-map runtime spawning in `EntityScene` mode; retained temporarily for parity/rollback, then archived or removed through accepted ownership cleanup |
| Existing collider/Rigidbody | none | Removed from map-owned instances; ECS surface/blocker/occupancy remains authoritative |
| Existing script/light/animation behavior | explicit reviewed ECS, baked-lighting, or approved managed-boundary disposition | Never silently stripped or converted by renderer-only inference |

Existing static-presentation manifest source identity and 514 scene chunks remain byte-stable rollback evidence until entity-scene visual/gameplay parity passes. They are not regenerated from migrated ECS source content.

### 6.2 Generated City Contract

The generator knows semantic intent at creation time and must place output accordingly.

| Generated content | Presentation ownership | Surface/blocker output | Gameplay treatment |
|---|---|---|---|
| Dense building shell | `EntityPresentation/GameplayBuildings/Buildings` | coarse footprint in `BakeSources/Blockers` | ECS simulation entity plus Entities Graphics intact/destroyed children |
| Building foundation/ground patch | `EntityPresentation/RenderOnly/Infrastructure` | simplified walkable patch in `BakeSources/Terrain` | render-only entity |
| Asphalt road | `EntityPresentation/RenderOnly/Infrastructure` | road deck in `BakeSources/Roads` | render-only entity plus surface metadata |
| Sidewalk | `EntityPresentation/RenderOnly/Infrastructure` | terrain or road mask according to approved movement policy | render-only entity plus surface metadata |
| Road shoulder/natural patch | `EntityPresentation/RenderOnly/Infrastructure` | `BakeSources/Terrain` | render-only entity |
| Bridge visual | `EntityPresentation/RenderOnly/Infrastructure` | deck in `BakeSources/Bridges`; approach in `BakeSources/Ramps` | render-only entity plus typed bridge surface |
| Canal water | `EntityPresentation/RenderOnly/Infrastructure` | non-ground exclusion in `BakeSources/Blockers` until water movement exists | render-only entity; no Unity Physics |
| Canal bank/park ground | `EntityPresentation/RenderOnly/Infrastructure` | `BakeSources/Terrain` | render-only entity |
| Courtyard wall/solid perimeter | gameplay-building child when damageable; otherwise `EntityPresentation/RenderOnly/Infrastructure` | coarse wall footprint in `BakeSources/Blockers` | entity; no collider |
| Courtyard props/wells | `EntityPresentation/RenderOnly/Props` | blocker only when gameplay clearance requires it | render-only entity |
| Rocks inside playable bounds | `EntityPresentation/RenderOnly/Props` | simplified blocker footprint | render-only entity; no collider |
| Rocks outside playable bounds | `EntityPresentation/RenderOnly/Horizon` | none | render-only entity |
| Horizon mountains | `EntityPresentation/RenderOnly/Horizon` | none unless they intersect playable bounds | render-only entity; measured unique-mesh exception only if required |
| Trees, bushes, grass | `EntityPresentation/RenderOnly/Vegetation` | no blocker by default | repeated render-only entities sharing art assets |
| Power poles, lines, streetlights | `EntityPresentation/RenderOnly/Infrastructure` | no blocker by default | repeated render-only entities sharing art assets |
| Roof, interior, shop, or tent props attached to a damageable building | child of the owning building's intact or destroyed visual root | none | explicit building identity and visual-state ownership at generation/migration time; intact descendants disappear atomically with the intact shell |
| Independent rooftop tanks and utility props | `EntityPresentation/RenderOnly/Props` only when they are proven independent of every damageable building | none | explicit independent identity; proximity or hierarchy name is not ownership evidence |
| Civic/market hero building | `EntityPresentation/GameplayBuildings/CivicAndMarket` | coarse blocker footprint | ECS building entity with explicit health/destruction policy |

No semantic decision may be recovered from `GameObject.name`, prefab filename, material name, hierarchy substring, or renderer shape after generation.

## 7. Collider-Free Contract

This game does not use Unity colliders for operation-map gameplay.

Generation requirements:

- Immediately after any prefab or primitive is instantiated, remove `Collider`, `Collider2D`, `Rigidbody`, and `Rigidbody2D` components from that instance hierarchy.
- Removal is instance-scoped. Never mutate a shared third-party prefab asset during city generation.
- Generated primitives must be created without retaining their automatically added colliders.
- Do not keep disabled collider components as documentation or future fallback.
- Surface height, walkability, blocking, building placement, vehicle occupancy, soldier pathing, and air clearance continue through ECS grid/surface data.

Bake requirements:

- Dense-city readiness fails if any prohibited physics component exists beneath either generated ownership root.
- Operation-map readiness fails if any prohibited physics component exists in the source map hierarchy or runtime binding scene.
- Build validation scans the dependency closure of production operation-map scenes and generated presentation scenes. Imported third-party assets that are not part of the build closure do not fail this gate.
- Tests must cover inactive objects, nested prefab instances, removed-component prefab overrides, generated SubScene authoring, baked entity-scene dependencies, and the frozen rollback static-presentation set while it exists.

## 8. Hand-Authored Content And Override Contract

Persistent map edits must not be made inside the disposable generated ownership roots unless losing them during regeneration is intentional.

`AuthoredCityOverrides` owns:

- persistent handmade buildings and props;
- deliberate removal/exclusion regions;
- replacement landmarks;
- mission-specific objectives and staging areas;
- authored roads or district corrections;
- typed spawn, deployment, camera, runway, helipad, and objective anchors;
- persistent authored ECS buildings and legacy interactive production-building/vehicle placements.

Generation behavior:

1. Validate that the current scene is `opmap_skirmish_desert_base_01` and that exactly one `OperationMapSceneView` exists.
2. Capture protected bounds from existing military, handmade, runway, resource, mountain, road, and override owners before deleting generated content.
3. Validate override/exclusion records for finite values, stable ids, grid intersection, and duplicates.
4. Delete only the two marked roots in the accepted generation ownership set.
5. Generate only in cells not reserved by protected or override records.
6. Fail rather than move or suppress existing authored content.
7. Save generation evidence containing the protected-owner counts and overlap result.

Manual edit workflow:

- Use the generated city for broad procedural composition.
- Promote any persistent change into `AuthoredCityOverrides` before regenerating.
- Keep mission/gameplay authoring outside generated presentation.
- Regenerate and run readiness validation after override changes.
- Run Bake All only after generation and readiness pass.

## 9. Architecture And Assembly Contract

No new asmdef is approved.

| Assembly | Responsibility | Allowed changes | Forbidden changes |
|---|---|---|---|
| `Game.Authoring` | passive generated-root and override authoring markers | serialized data only; no updates | scene loading, Addressables, editor API, runtime policy |
| `Game.Editor` | city generation, semantic hierarchy, proxy creation, bake validation, budget reports | editor-only static builders/validators and immutable generation records | player runtime behavior, update loops, runtime generation |
| `Game.Components` | existing unmanaged surface, blocker, grid, map metadata | extend only if current blobs cannot represent required data | Unity objects, managed collections, Addressables |
| `Game.Rendering` | Entities Graphics integration plus existing compatibility static-presentation metadata | reuse shared render data and current rendering contracts | mission policy, map selection, loading lifecycle |
| `Game.Composition` | existing runtime binding/SubScene loading and presentation-kind selection | consume the accepted entity scene/runtime binding output; retain the existing streamer only for maps explicitly using `StaticSceneChunks` | city generation, source hierarchy inspection, another streamer |
| `Game.Configs` | existing operation-map definition and small immutable metadata references | per-map bake settings only when required | concrete rendering dependencies or hot runtime policy |
| `Game.Tests.Editor` | generation, ownership, deterministic, bake, build-layout, architecture tests | focused test fixtures | production behavior hidden in tests |
| `Game.Tests.PlayMode` | runtime bind, entity readiness, rendering, teardown, and allocation evidence | production-route probes | alternate runtime path |

Assembly direction remains:

```text
Game.Editor -> Game.Authoring / Game.Components / Game.Configs / Game.Rendering / Game.Composition
Game.Composition -> Game.Rendering / Game.Runtime / Game.Components / Game.Configs
Game.Rendering -> Game.Components / Game.Configs
Game.Runtime -> Game.Components / Game.Configs / Game.Rendering.Contracts
```

Runtime assemblies must never reference `Game.Editor`.

## 10. Existing Types To Reuse Or Extend

| Existing type | Kind / assembly | Required disposition |
|---|---|---|
| `DenseMiddleEasternCityEditModeBuilder` | internal static editor class / `Game.Editor` | Extend generation so every creation call receives explicit presentation and bake-record ownership. Split responsibilities into new bounded editor collaborators before this file grows further. |
| `RuntimeCityRAndDEditModeBuilder` | internal static editor class / `Game.Editor` | Keep as the editor command coordinator. It creates/replaces only the two marked roots in the generation ownership set and invokes validation after generation. |
| `RuntimeCityRAndDMapViewEditor` | sealed custom Editor / `Game.Editor` | Keep buttons thin. `Build Giant Dense City` calls the coordinator; no bake logic in inspector GUI. |
| `MapBakeGroupAuthoring` | sealed passive MonoBehaviour / `Game.Authoring` | Reuse for `IgnoredDecoration`, `Terrain`, `Road`, `Bridge`, `Ramp`, and `Blocker` ownership. Do not add dense-city-specific duplicate role components. |
| `MapSurfaceAuthoringEditor` | static editor class / `Game.Editor` | Continue consuming explicit nearest-group ownership. Do not add name detection. |
| `MapSurfaceBakeSystem` | sealed editor class / `Game.Editor` | Continue spatially indexed surface baking. Feed simplified proxy meshes, not detailed city art. |
| `OperationMapDefinition` | sealed ScriptableObject / `Game.Configs` | Add explicit presentation kind. Require static manifest/legacy placements only for `StaticSceneChunks`; require the authored SubScene and reject static chunk ownership for `EntityScene`. |
| `OperationMapCanonicalPresentationMode` | byte enum / `Game.Rendering` | Add `EntityScene` for renderer-free binding scenes whose visual ownership is the loaded map SubScene rather than static chunk scenes. |
| `OperationMapCurrentMapBaker` | public static editor class / `Game.Editor` | Add existing-map migration/readiness before dense-city readiness, surface, entity baking, and Addressables build. Preserve transactional report behavior. |
| `StaticMapPresentationBaker` | public static editor class / `Game.Editor` | Freeze current 514-scene output as rollback evidence during migration. In accepted `EntityScene` mode, skip source baking and verify zero production Addressables/build ownership rather than regenerating chunks. |
| `OperationMapRuntimeBindingSceneBuilder` | public static editor class / `Game.Editor` | Generate the thin renderer-free runtime binding scene in `EntityScene` mode and bind the map SubScene. Never copy either authoring hierarchy. |
| `OperationMapAddressablesLayoutBuilder` | public static editor class / `Game.Editor` | Package runtime binding, map SubScene entity data/dependencies, metadata, and surface. In `EntityScene` mode, remove static manifest/chunk labels and legacy placement references from production ownership. |
| `OperationMapSceneView` | sealed passive MonoBehaviour / `Game.Composition` | Validate presentation-kind-specific references. `EntityScene` accepts empty legacy roots/configs and requires the protected entity-presentation root in the referenced SubScene. |
| `UnitHealth` | unmanaged `IComponentData` / `Game.Components` | Reuse for generated building current/max health. Do not create a parallel managed health model. |
| `UnitDestroyedVisualReference` | unmanaged `IComponentData` / `Game.Components` | Reuse the alive/destroyed render-root entity references and visible scales. |
| `UnitDestroyedVisualSystem` | Burst `ISystem` / `Game.Runtime` | Reuse initialization of intact/destroyed entity visual state. Extend only through general ECS data, never through building GameObjects. |
| `UnitGridAuthoring.Baker` | existing Baker pattern / `Game.Authoring` | Use as the reference pattern for converting intact/destroyed GameObject authoring hierarchies into render entities. Do not attach `RuntimeBuildingEntityLink`. |
| `RuntimeBuildingEntityLink` and `BuildingDestroyedVisualPresentationSystemHelper` | legacy managed building presentation / `Game.Runtime` | Retain only as rollback/migration code until existing map buildings pass ECS parity. No existing or generated map-authored building may use this path after cutover. |
| `OperationMapSceneLoadingSceneSystemHelper` | managed composition boundary / `Game.Composition` | Branch on presentation kind: load/await entity SubScene ownership and do not request a static manifest/streamer for `EntityScene`. |
| `StaticMapPresentationStreamer` | managed presentation boundary / `Game.Composition` | Remain available for other presentation kinds but stay unbound for `opmap.skirmish.desert_base_01` after ECS cutover. |
| `StaticMapPresentationManifest` | sealed ScriptableObject / `Game.Rendering` | Preserve the current map artifact only as rollback evidence after cutover; it is not an `EntityScene` content requirement. |

## 11. Approved New Types

Names below are normative. A different name, kind, file, or assembly requires updating this tracker before implementation. No new type may use `Manager`, `Controller`, `Facade`, `Service`, `Provider`, `Runtime`, `Utility`, or a bare non-ECS `System` suffix.

| Type | C# kind | Namespace / assembly | Planned file | Responsibility |
|---|---|---|---|---|
| `OperationMapPresentationKind` | public byte enum | `Game.Configs` / `Game.Configs` | `Assets/Game/Scripts/Configs/OperationMapPresentationKind.cs` | Closed `StaticSceneChunks` and `EntityScene` content ownership modes used by definition, build, validation, and loading policy. |
| `OperationMapEntityPresentationRole` | public byte enum | `Game.Authoring` / `Game.Authoring` | `Assets/Game/Scripts/Authorings/OperationMapEntityPresentationRootAuthoring.cs` | Closed authored `GameplayBuildings`, `GameplayVehicles`, and `RenderOnly` role identities. |
| `OperationMapEntityPresentationRootAuthoring` | `sealed MonoBehaviour`, no update methods | `Game.Authoring` / `Game.Authoring` | same file | Marks the protected authored ECS presentation root with operation-map id, schema, and deterministic migration hash. |
| `OperationMapEntityPresentationMigrationRecord` | internal readonly struct | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/OperationMapEntityPresentationMigrationEditor.cs` | Stable source identity, approved role, transform, prefab/asset identity, component disposition, and destination identity for one existing map owner. |
| `OperationMapEntityPresentationMigrationEditor` | internal static editor class | `Game.Editor` / `Game.Editor` | same file | Creates and validates a candidate SubScene migration transaction without mutating accepted source/static rollback artifacts before cutover. |
| `OperationMapEntityPresentationReadinessValidator` | public static editor class | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/OperationMapEntityPresentationReadinessValidator.cs` | Fail-closed existing-map entity ownership, parity, duplicate-asset, legacy-spawn, manifest retirement, and rollback validation with menu/batch entry points. |
| `DenseCityGeneratedRootRole` | public enum | `Game.Authoring` / `Game.Authoring` | `Assets/Game/Scripts/Authorings/DenseCityGeneratedRootAuthoring.cs` | Closed `MapBakeSource` and `EntityPresentationSource` ownership roles. |
| `DenseCityGeneratedRootAuthoring` | `sealed MonoBehaviour`, no update methods | `Game.Authoring` / `Game.Authoring` | same file | Marks one scene-scoped root in the disposable generation set and stores role, shared generation id, generator schema/version, seed, and deterministic generation hash. No scene search or policy. |
| `DenseCityAuthoredOverrideAuthoring` | `sealed MonoBehaviour`, no update methods | `Game.Authoring` / `Game.Authoring` | `Assets/Game/Scripts/Authorings/DenseCityAuthoredOverrideAuthoring.cs` | Marks persistent override/exclusion roots with stable id, local center/size, and exclusion flags without colliders. |
| `DenseCityPresentationCategory` | internal enum | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/MapPrototypes/DenseCityGenerationRecords.cs` | Closed presentation categories used at the generation call site; never inferred from names. |
| `DenseCitySurfaceRecordKind` | internal enum | `Game.Editor` / `Game.Editor` | same file | Closed terrain, road, bridge, ramp, and blocked proxy kinds. |
| `DenseCityRecordIdentity` | internal readonly struct | `Game.Editor` / `Game.Editor` | same file | Generator schema, seed, district id, kind, deterministic sequence, source GUID/local id, and stable key. |
| `DenseCityBuildingBakeRecord` | internal readonly struct | `Game.Editor` / `Game.Editor` | same file | Building transform, footprint, foundation, blocker, frontage, presentation, and gameplay disposition. |
| `DenseCitySurfaceBakeRecord` | internal readonly struct | `Game.Editor` / `Game.Editor` | same file | Surface kind, deterministic polygon/rectangle geometry, elevation, movement mask, layer, and chunk identity. |
| `DenseCityPresentationBakeRecord` | internal readonly struct | `Game.Editor` / `Game.Editor` | same file | Prefab/mesh/material identities, transform, ECS gameplay/render-only disposition, shadow policy, batching eligibility, LOD importance, and stable spatial identity. |
| `DenseCityGenerationRecordSet` | internal sealed editor-lifetime class | `Game.Editor` / `Game.Editor` | same file | Owns pre-sized, stable-ordered record lists for one editor generation and clears them before disposal. It is never serialized into or referenced by player runtime code. |
| `DenseCitySemanticHierarchyBuilder` | internal static editor class | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/MapPrototypes/DenseCitySemanticHierarchyBuilder.cs` | Creates and validates the generated hierarchy and its `MapBakeGroupAuthoring` roles. Does not generate districts. |
| `DenseCityPhysicsComponentStripper` | internal static editor class | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/MapPrototypes/DenseCityPhysicsComponentStripper.cs` | Removes prohibited physics components from newly instantiated instance hierarchies and reports counts. Never edits shared assets. |
| `DenseCitySurfaceProxyBuilder` | internal static editor class | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/MapPrototypes/DenseCitySurfaceProxyBuilder.cs` | Converts generation records into simplified terrain/road/bridge/ramp/blocker proxy meshes grouped by spatial chunk and role. |
| `DenseCityBakeReadinessValidator` | public static editor class | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/DenseCityBakeReadinessValidator.cs` | Fail-closed validation for root identity, ownership, colliders, mixed roles, bounds, records/proxies, protected content, and deterministic hashes. Exposes menu and batch entry points. |
| `DenseCityPresentationBudgetValidator` | public static editor class | `Game.Editor` / `Game.Editor` | `Assets/Game/Scripts/Editor/DenseCityPresentationBudgetValidator.cs` | Produces deterministic authoring/entity/render-batch/mesh/material/triangle/shadow/entity-scene-size reports and enforces accepted Android budgets. |
| `OperationMapBuildingAuthoring` | `sealed MonoBehaviour` with nested `Baker`, no update methods | `Game.Authoring` / `Game.Authoring` | `Assets/Game/Scripts/Authorings/OperationMapBuildingAuthoring.cs` | Serializes stable id, faction, health, footprint, destruction/blocker policy, and intact/destroyed render roots. Recursively bakes each root's shell and attached roof/interior/shop/tent prop descendants into the same entity visual state; bakes only unmanaged ECS data and render-entity references. |
| `OperationMapBuildingBlockerPolicy` | public byte enum | `Game.Components` / `Game.Components` | `Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs` | Closed blocker disposition. Initial production value is `RubbleRemainsBlocked`; passable destruction remains rejected until dynamic-grid ownership is implemented. |
| `OperationMapBuildingComponent` | unmanaged `IComponentData` | `Game.Components` / `Game.Components` | `Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs` | Stable map-building identity and deterministic destruction/blocker policy. No Unity object, string, or managed collection. |
| `OperationMapBuildingDestroyedComponent` | unmanaged tag `IComponentData` | `Game.Components` / `Game.Components` | same file | Marks completed destruction so the transition executes once. |
| `OperationMapBuildingDestructionSystem` | Burst-compatible `ISystem` | `Game.Runtime` / `Game.Runtime` | `Assets/Game/Scripts/Systems/OperationMapBuildingDestructionSystem.cs` | Observes `UnitHealth`, switches baked entity visuals, applies the approved blocker disposition, and records destroyed state without GameObject creation/destruction. |

The approved runtime addition is limited to presentation-kind selection, ECS building state, and its Burst destruction transition. All persistent existing/generated map rendering uses Entities Graphics data produced by baking; no custom per-frame instance renderer, managed registry, GameObject pool, or new scene streamer is approved. The existing static-presentation streamer remains available for `StaticSceneChunks` maps but is unbound from this map after cutover.

## 12. Migration And Generation-Time Data Contract

### 12.0 Existing Map Migration Record

Every accepted existing owner requires a committed deterministic disposition containing:

- repository-relative source scene and stable serialized object identity;
- current authoritative owner and approved `OperationMapEntityPresentationRole`;
- source prefab GUID/local id or mesh/material identities;
- exact world/local transform and hierarchy dependency;
- gameplay components, scripts, lights, animation, cross-object references, and collider removal disposition;
- destination SubScene path and stable destination identity;
- building/vehicle placement-config identity when applicable;
- expected ECS root/render-child/archetype ownership;
- rollback source/static-manifest/chunk identity;
- explicit decision owner for every mixed or unresolved case.

The migration cannot classify through object names, prefab filenames, material names, or renderer shape. Mixed/unresolved rows block candidate publication.

### 12.0A Editor-To-Runtime Transform Parity Contract

The current static bake/Addressables path is known to allow model positions at runtime to differ from their accepted Editor placements. The EntityScene migration must fix this defect rather than reproduce it. Visual similarity alone is not acceptance evidence.

For every migrated or generated gameplay/render-only presentation owner, Bake All must emit one deterministic transform-parity row keyed by stable source identity. Each row contains:

- authoritative accepted Editor source world matrix and local matrix;
- source parent stable identity and the ordered parent transform chain needed to explain the world matrix;
- prefab GUID/local id, renderer-relative transform, and mesh pivot/bounds identity;
- candidate SubScene authoring world/local matrices after transactional copy or generation;
- baked ECS root/render-child matrices, including `LocalTransform`, `Parent`, and `PostTransformMatrix` contributions where present;
- Addressables-loaded runtime world matrix after entity-scene readiness and one settled frame, before camera culling affects visibility;
- position, rotation, scale, and matrix residuals for each boundary;
- an explicit reason code for any approved non-visual helper/proxy that is exempt from renderer parity.

Required default tolerances are position `<= 0.001 m`, rotation `<= 0.01 degrees`, per-axis scale `<= 0.0001`, and world-matrix element residual `<= 0.0001`. A larger tolerance or intentional transform change requires a named, stable-identity exception in this tracker with before/after evidence; no broad category exception is allowed.

Validation must fail closed when:

- a source row has zero or multiple candidate/baked/runtime matches;
- hierarchy flattening changes the resulting world matrix;
- a nested prefab, negative/non-uniform scale, pivot, or `PostTransformMatrix` contribution is lost;
- a building/vehicle legacy placement config or startup system applies a second offset after ECS load;
- ground snapping, terrain sampling, floating-origin logic, or runtime normalization moves permanent presentation entities;
- an Addressables-loaded entity differs from the same candidate baked directly in Editor;
- a renderer bounds center/corners move beyond tolerance even when the owning root matrix appears equal.

The validator must compare matrices and transformed renderer bounds numerically, then capture fixed-camera screenshots as secondary visual proof. Production cannot flip to `EntityScene`, and rollback static presentation cannot be retired, until this gate passes twice on the same candidate revision in Editor and once on the user-triggered Android build.

Every generated placement must emit explicit records before visual realization:

### 12.1 Common Record Identity

- generator schema version;
- deterministic seed;
- stable district id;
- stable record kind;
- deterministic sequence index within kind/district;
- source prefab GUID and local file id when applicable;
- world transform encoded with invariant finite values;
- presentation category;
- gameplay-grid relationship: inside, intersecting, or outside;
- protected-area decision and reason code;
- stable hash input excluding timestamps, instance ids, absolute paths, and editor session data.

### 12.2 Building Record

- footprint center, half extents, orientation, minimum/maximum height;
- presentation prefab identity and visual scale;
- foundation terrain patch dimensions;
- blocker footprint dimensions and clearance margin;
- damageable/static disposition, max health, faction, and destroyed-blocker policy;
- intact and destroyed visual-root identities;
- stable identities and state ownership for every attached roof, interior, shop, tent, sign, awning, lamp, furniture, and utility visual; each belongs to exactly one building and exactly one visual state;
- frontage/road relationship;
- map-grid inclusion.

### 12.3 Surface Record

- role: terrain, road, bridge, ramp, or blocked;
- polygon/rectangle vertices in deterministic winding order;
- layer id and movement mask;
- minimum/maximum elevation and slope contract;
- source stable key;
- chunk coordinates.

### 12.4 Presentation Record

- mesh/prefab identity;
- material identities and approved overrides;
- transform;
- shadow policy;
- Entities Graphics batching eligibility;
- near/far importance class;
- stable spatial cell used for diagnostics and optional future entity-section analysis;
- renderer exclusion reason when not baked.

Records are editor-transition data. They do not become managed runtime lists. All generated presentation records become baked entity data in the map SubScene; surfaces/blockers become existing immutable map metadata. Generated ECS buildings do not become `MapBuildingPlacementConfigEntry` objects, and generated render-only placements do not become static-presentation source records.

## 13. Proxy Geometry Contract

- Proxy geometry must be deterministic and generated from records, not renderer bounds rediscovered after generation.
- Detailed prefab meshes are forbidden as `Blocker` sources.
- Building blockers use footprint rectangles or reviewed convex outlines, not wall-by-wall mesh triangles.
- Adjacent coplanar road and terrain rectangles are merged within one spatial bucket when semantic metadata matches.
- Proxy meshes are partitioned by role, layer id, movement mask, and chunk.
- A single proxy mesh must not cross operation-map ownership or incompatible movement masks.
- Proxy meshes carry `MeshFilter` only. They have no `MeshRenderer`, collider, Rigidbody, Animator, AudioSource, particle system, or script other than the owning bake-group marker on an ancestor.
- Generated proxy asset paths are map-scoped and transaction-owned. Candidate output is validated before replacing accepted output.
- Mesh vertex/index format is selected deterministically; use 16-bit indices where the partition permits it.
- Proxy output must be idempotent and byte-stable across two runs from unchanged input.

## 14. ECS Presentation Optimization Contract

The complete current operation-map runtime representation is ECS-first:

- existing and generated damageable buildings: map SubScene gameplay entities rendered by Entities Graphics;
- existing and generated vehicles: current ECS unit/vehicle entity rendering path;
- existing and generated trees, props, vegetation, modular infrastructure, roads, sidewalks, bridges, mountains, terrain, water, and background pieces: map SubScene render-only entities;
- transient explosion/smoke VFX: existing pooled managed VFX boundary where required;
- production static-presentation GameObject chunks for this map: none after accepted cutover.

All map visual entities load once with the map SubScene and remain loaded until map teardown. Initial acceptance relies on compact entity-scene serialization, shared mesh/material assets, Entities Graphics batching, Burst/jobified culling, and reviewed LOD/impostor data rather than camera-driven scene loading. Render-only entity section streaming is deferred and requires a measured design amendment.

### 14.1 ECS Building Runtime Contract

- Existing and generated building GameObjects exist only in the editor map SubScene as selectable authoring inputs. The player consumes Unity entity-scene data produced by baking; it never loads those authoring GameObjects.
- Each building root entity owns stable map identity, `LocalTransform`, `UnitHealth`, faction, grid cell/footprint, and `OperationMapBuildingComponent` destruction/blocker policy.
- Intact and destroyed model hierarchies bake to render entities. `UnitDestroyedVisualReference` points to their entity roots; the intact root starts visible and the destroyed root starts hidden.
- Every building-attached prop is a descendant render entity of exactly one of those visual roots. Intact roof/interior/shop/tent props must not be separately parented, independently streamed, or classified as global render-only props. Destroyed-state debris or replacement props belong only beneath the destroyed root.
- `OperationMapBuildingDestructionSystem` performs a one-time Burst-compatible state transition when health reaches zero: hide the complete intact entity hierarchy including all attached props, show the complete destroyed entity hierarchy, mark `OperationMapBuildingDestroyedComponent`, and apply the explicit blocker policy.
- If an intact attached prop has no destroyed equivalent, it simply disappears with the intact hierarchy. No orphan visual may remain at the building position. A genuinely independent gameplay object requires its own explicit entity identity and cannot be inferred from proximity.
- The default blocker policy is `RubbleRemainsBlocked`: the precomputed ECS blocker footprint is unchanged after destruction. A passable-after-destruction policy is forbidden until it has a stable building-to-dynamic-grid mapping and focused pathing tests.
- Persistent buildings do not use `RuntimeBuildingEntity`, `RuntimeBuildingEntityLink`, `BuildingVisualSystem`, `BuildingDestroyedVisualPresentationSystemHelper`, `Object.Instantiate`, or `Object.Destroy`.
- Temporary explosion, fire, and smoke may cross the existing pooled VFX presentation boundary. Those effects do not own building health, identity, transforms, or persistent destroyed state.
- Building render entities share baked mesh/material assets. No mesh, material, or texture is cloned per placement.
- All building simulation entities remain available to targeting, damage, save/load, and scenario systems even when offscreen. Camera visibility affects rendering only.
- A future render-only section split may partition entity presentation by spatial section only after Android evidence proves whole-map entity presentation residency is unacceptable. It must keep simulation state resident and restore the correct intact/destroyed visual state when a render section becomes visible.

### 14.2 Render-Only Entity Contract

- Existing and generated permanent visual placements are baked directly into entity scene chunks. They are not instantiated from a GameObject pool and have no per-frame gameplay system.
- Repeated placements share imported mesh, material, texture, and approved render configuration assets. Per-placement runtime data is limited to entity identity required by Unity, transform/hierarchy, render bounds, LOD data, and Entities Graphics references.
- Modular roads, sidewalks, bridges, trees, rocks, lights, props, vegetation, and skyline modules remain separate placements when transforms permit asset sharing. Do not combine them merely to reduce authoring object count.
- Unique continuous terrain/canal/road geometry may be deterministically combined into bounded entity-render meshes when it reduces renderer count without duplicating source assets or creating a whole-city mesh.
- Render-only entities receive no health, faction, target, occupancy, managed component, authoring marker, or update behavior unless their semantic record explicitly promotes them to gameplay ownership.
- Precomputed ECS surface/blocker data remains authoritative. Render entities do not provide collision, raycast, navigation, or placement geometry.
- The player must contain no map visual GameObject hierarchy, map-visual chunk scene handle, or current-map production entry from `StaticMapPresentationManifest` after cutover.

### 14.3 Shared Asset And Package Contract

- A repeated prefab placement must reference the same baked mesh/material assets as every compatible placement; no per-placement mesh/material/texture copy is allowed.
- Addressables Build Layout must report each existing/generated map art dependency in one accepted local production ownership location. Rollback static artifacts must not be labeled, cataloged, or bundled with the entity-scene package; any duplicate production dependency bytes fail readiness.
- The generated SubScene source `.unity` file is editor authoring input. Runtime packages contain its entity scene output and referenced art dependencies, not its GameObject YAML hierarchy.
- Entity scene data, shared dependencies, and catalog entries are bundled locally with the application for the initial release and require no network access.
- Reintroducing a static-presentation chunk for this map requires a tracker amendment naming the unique asset, measured entity-residency failure, expected memory saving, ownership path, unload policy, and Android validation. Repetition alone never justifies an exception.

Required source policy:

- Include every existing renderer beneath `AuthoredOperationMapEntityPresentation` and every generated renderer beneath `EntityPresentationSource`; prove each bakes to exactly one gameplay or render-only entity hierarchy.
- Exclude authored/generated entity-presentation roots and their dependencies from static-presentation source collection/manifest ownership.
- Exclude bake proxies, authoring gizmos, override volumes, gameplay placement previews, and runtime binding objects.
- Exclude every prohibited physics component from generated authoring and baked dependency closure.
- Preserve shared mesh/material/texture references. Do not duplicate third-party binary art into entity scene outputs.
- Keep repeated mesh/material combinations Entities Graphics batching-compatible; do not clone mesh/material assets per entity.
- Merge unique road, shoulder, foundation, terrain, or ground geometry only when measured renderer reduction exceeds the loss of repeated-asset sharing.
- Do not combine the entire city into one giant mesh.

LOD/impostor policy:

- Source `LODGroup` or approved entity LOD authoring must bake to Entities Graphics-compatible LOD data and pass focused conversion tests.
- Near tier keeps repeated entity meshes and required hero detail.
- Mid tier removes rooftop props, wires, small vegetation, and minor facade detail.
- Far tier uses entity-rendered district aggregate meshes or impostors with no shadow casting.
- Exactly one visual tier is active for a placement/district at a time.
- LOD/impostor generation must preserve deterministic bounds, hashes, shared material ownership, and intact/destroyed building state.
- LOD/impostor behavior is not accepted until real Android camera traversal proves transitions, batching, visual continuity, and memory residency.

## 15. Authoring Scene And Runtime Package Separation

The following paths retain their current ownership:

- Authoring source scene: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`
- Thin runtime binding output root: `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01`
- Frozen rollback-only static presentation root after cutover: `Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01`

Rules:

- The authoring scene is an editor source and must not be an Addressable runtime source-scene entry.
- `OperationMapAddressablesLayoutBuilder.SourceScenePath` remains `OperationMapRuntimeBindingSceneBuilder.OutputPath`.
- The runtime binding scene remains renderer-free in `EntityScene` mode.
- The map SubScene owns every existing/generated map gameplay and render-only entity.
- The static manifest, integrity ledger, and 514 scenes remain frozen rollback artifacts on disk until closeout; they are absent from current-map production Addressables labels, catalogs, dependencies, Android build scenes, and runtime loading.
- Map surface, minimap raster, definition, runtime binding, and entity SubScene data are package-owned according to the revised `EntityScene` Addressables contract. Legacy placement configs remain migration evidence, not runtime content references.
- Build Layout validation must prove that source-scene GameObject YAML is absent, entity scene data is included once, shared art dependencies are not duplicated, and no map visual hierarchy appears in runtime binding or static chunk scenes.

## 16. One-Button Bake Sequence

`Game/Operation Maps/Bake Current Map (All)` remains the authoritative command. Its accepted sequence becomes:

1. Save/confirm modified scenes using current editor behavior.
2. Open the operation-map authoring scene and referenced map SubScene transactionally.
3. Run `entity-presentation-readiness` and validate complete existing-map migration/parity with zero mixed or unresolved owners.
4. Run `dense-city-readiness` when a generated ownership set exists; otherwise record an explicit not-generated state.
5. Validate legacy 451/29 placement parity against authored ECS entities without publishing runtime placement spawns.
6. Run `surface-data` from existing and generated semantic proxy groups.
7. Validate zero prohibited physics components and save accepted authoring/SubScene candidates.
8. Close source scenes, force entity-scene baking, and validate expected gameplay/render-only/render-child entities.
9. Validate the frozen static manifest/chunk rollback set without regenerating it.
10. Run `presentation-budget` across all existing/generated entity/render/batch/dependency data.
11. Run `runtime-definition` with `OperationMapPresentationKind.EntityScene` and no required static manifest/legacy placement runtime references.
12. Run `minimap-raster`.
13. Run `spatial-bindings`.
14. Run `runtime-binding-scene` in `EntityScene` mode and validate renderer-free/collider-free output.
15. Run `addressables-layout`, removing current-map static manifest/chunk and legacy placement runtime entries.
16. Run `local-addressables` and inspect Build Layout/entity-scene/shared-art dependencies for single ownership and zero current-map static scenes.
17. Publish a passed report only after every stage succeeds.

Failure behavior:

- Fail before mutating accepted generated output when readiness fails.
- Existing transaction owners restore prior definition, Addressables layout, runtime binding, SubScene candidate, manifest/integrity/chunk rollback set, and metadata on publication failure.
- Never leave a stale successful report after failure.
- Restore the editor scene setup in `finally`.
- Never delete the authoring source or authored override root.
- A failed bake does not silently load prior stale city output as current evidence.

## 17. Runtime Loading And Teardown

No new runtime lifecycle is introduced.

Accepted load order:

1. Resolve scenario and `OperationMapDefinition`.
2. Load the thin runtime binding scene through local Addressables.
3. Resolve exactly one `OperationMapSceneView`.
4. Require `OperationMapPresentationKind.EntityScene` and load/await the map SubScene containing all existing/generated gameplay and render-only entities.
5. Bind map surface and immutable operation-map metadata.
6. Validate expected gameplay/render-only/render-child entity counts, stable identities, vehicle state, and intact building visual state.
7. Assert no current-map static manifest or presentation streamer binding exists.
8. Publish readiness only after required metadata and entity scene data pass.
9. Begin gameplay.

Accepted teardown order:

1. Clear gameplay readiness and stop map-dependent work.
2. Clear map-owned ECS entities, including all existing/generated render-only and intact/destroyed building visual entities, buffers, anchors, and blobs through their accepted owners.
3. Unload/release SubScene content.
4. Unload/release the thin runtime binding scene handle.
5. Release definition/map asset handles.
6. Verify no stale map id, entity, scene, static-presentation handle, or retained authoring reference remains.

The authoring scene never participates in runtime load or teardown.

## 18. Determinism And Hashing

The following must be deterministic for unchanged source/config/seed:

- generated hierarchy paths;
- generated record ordering;
- protected-owner counts and decisions;
- prefab selection sequence;
- transforms and material variants;
- semantic role assignment;
- proxy mesh vertices and indices;
- generated-root content hash;
- surface payload hash;
- authored migration record/source/destination hashes;
- authored/generated entity presentation source hash, entity scene dependency identity, and shared-art ownership set;
- frozen rollback manifest/chunk identities and proof that production Addressables contain none of them;
- runtime binding ledger;
- minimap raster hash;
- Build Layout ownership set.

Forbidden hash inputs:

- wall-clock time;
- absolute workstation/worktree paths;
- Unity instance ids;
- editor session ids;
- unordered dictionary/hash-set enumeration;
- transient progress values;
- random GUID creation during repeat generation.

Two complete generate-and-bake runs from identical input must produce byte-identical accepted artifacts and a clean second `git status` for generated ownership paths.

## 19. Android Performance And Size Gates

No ECS migration or city budget is accepted without baseline and target-device evidence. Phase 0 records the current shipped map baseline first. Existing-map cutover and dense-city acceptance use both absolute device limits from `performance_regression_contract.md` and explicit deltas from that baseline.

Required metrics:

- compressed APK/AAB and installed size;
- operation-map local Addressables bytes by group and bundle;
- duplicate dependency GUIDs/bytes;
- authoring scene exclusion proof;
- runtime binding scene bytes;
- production static manifest/chunk count, required to be zero for this map after cutover, plus frozen rollback bytes reported separately;
- existing/generated gameplay/render-only/render-child entity counts, archetype/entity-chunk counts, shared mesh/material count, and entity-scene bytes;
- SubScene entity integration/GPU upload average, p95, p99, and maximum;
- peak transition memory and retained post-load memory;
- CPU main/render thread average, p95, p99, and maximum;
- GPU frame average, p95, p99, and maximum;
- sustained FPS and thermal behavior;
- draw calls, SetPass calls, visible renderers, triangles, and shadow casters for representative cameras;
- steady-state GC allocation and transition allocation spikes;
- surface bake duration and peak editor memory;
- Addressables build duration and peak editor memory.

Mandatory qualitative gates:

- `0 B/frame` managed allocation from operation-map orchestration after readiness.
- No frame-time spike caused by integrating or uploading thousands of building render entities in one frame.
- No existing-map or generated-city source GameObject hierarchy resident at runtime.
- No collider or Rigidbody in production map dependency closure.
- No visible LOD/impostor holes, duplicate buildings, missing roads, or entity-section seams.
- No map-visual scene load/unload or visibility pop caused by camera travel.
- No navigation through buildings, canals, walls, or mountains.
- No blocked roads caused by presentation-only props.
- No expansion of the gameplay grid until missions require and device gates accept it.

Optimization order when a gate fails:

1. Remove accidental source duplication and prohibited components.
2. Verify every repeated visual shares baked mesh/material data and forms expected Entities Graphics batches.
3. Add/fix entity LOD/impostors and reduce shadow casters/small distant detail.
4. Deterministically combine only unique continuous terrain/road geometry where batching cannot help.
5. Reduce city density only after entity representation optimizations are measured.
6. Propose render-only entity section streaming only if measured residency remains unacceptable; never partition simulation state by camera.
7. Propose a static-presentation exception only for named unique heavyweight content with measured memory benefit and no repeated-asset alternative.

Runtime procedural generation is not the fallback for a rendering or packaging failure.

## 20. Progress Summary

Progress is checklist based. Every checkbox counts. Update the count, phase status, and validation log in the same stable change that completes an item.

Current progress: **46 / 148 (31%)**

| Phase | Status |
|---|---|
| Phase 0: Baseline and contracts | Complete |
| Phase 0A: Existing map ECS presentation migration | Transactional candidate Bake All and direct baked matrix/bounds parity complete; Addressables runtime, fixed-camera, lifecycle, and Android acceptance open |
| Phase 1: Generated ownership authoring | In progress; semantic hierarchy and fail-closed role/scene ownership validation complete |
| Phase 2: Generator semantic output | In progress; building/attachment, infrastructure, canal/park, civic/market, and combined courtyard/detail records integrated and validated |
| Phase 3: Collider-free enforcement | Not started |
| Phase 4: Surface and blocker proxies | Not started |
| Phase 5: Readiness and Bake All integration | Not started |
| Phase 6: ECS presentation optimization | Not started |
| Phase 7: Runtime package separation | Not started |
| Phase 8: Runtime lifecycle validation | Not started |
| Phase 9: Android acceptance | Not started |
| Phase 10: Documentation and closeout | Not started |

## 21. Implementation Checklist

### Phase 0: Baseline And Contracts

- [x] Record a clean post-clear authoring-scene hash, size, hierarchy counts, renderer counts, and existing bake-group counts.
- [x] Record current accepted surface payload, static manifest, chunk count, source count, integrity ledger, minimap, runtime binding, and Addressables Build Layout identities.
- [x] Capture current Android APK/installed size, runtime memory, draw calls, frame timings, GC, load time, and unload time on the target device.
- [x] Confirm the authoring scene is excluded from current Addressables/runtime build ownership.
- [x] Confirm existing military, handmade, runway, road, mountain, resource, placement, and override roots that generation must preserve.
- [x] Approve generated hierarchy names and exact semantic ownership table from this tracker.
- [x] Approve whether city content outside the current grid is presentation-only; default is presentation-only.
- [x] Add focused test fixtures that can build a small deterministic city without opening or modifying the full map scene.

**Exit:** Regressions can be measured against a clean, cleared-city baseline.

### Phase 0A: Existing Map ECS Presentation Migration

- [x] Inventory all current static-presentation sources by repository-relative scene/object identity, prefab GUID/local id, mesh/material identity, transform, components, and current static chunk ownership. Historical pre-clear baseline was `16,542` sources / `514` chunks; post-clear accepted package is measured by the inventory probe.
- [x] Join all current building and vehicle placement identities to their exact authored source objects and reject zero, multiple, reused, mixed, or unresolved matches. Historical pre-clear counts were `451` / `29`; post-clear counts are measured by the inventory probe.
- [x] Classify every existing owner as gameplay building, gameplay vehicle, render-only entity, map metadata/proxy, approved managed boundary, or rejected/unresolved without name-based inference.
- [x] Audit scripts, lights, animation, particles, cross-object references, and source suppression behavior; record an explicit ECS/baked-lighting/VFX/metadata disposition for each non-renderer dependency.
- [x] Prove which current vehicle placements already produce ECS entities/render entities and identify only the missing conversion/duplication cleanup.
- [x] Inventory every managed `RuntimeBuildingEntity`/GameObject dependency required by current authored buildings and define equivalent ECS component/buffer/system ownership before visual cutover.
- [x] Inventory every roof, interior, shop, tent, sign, awning, lamp, furniture, and utility visual attached to each current damageable building; assign each stable identity to exactly one building and one intact/destroyed state without proximity or name inference.
- [x] Add `OperationMapPresentationKind` and set the candidate definition to `EntityScene` only after migration validation succeeds.
- [x] Extend `OperationMapCanonicalPresentationMode`, `OperationMapDefinition`, and content-reference validation for an entity-presented map with no required static manifest or runtime placement references.
- [x] Add `OperationMapEntityPresentationRootAuthoring` and exact protected authored hierarchy roots in the existing map SubScene.
- [x] Add deterministic `OperationMapEntityPresentationMigrationRecord` capture and `OperationMapEntityPresentationMigrationEditor` candidate transaction ownership.
- [x] Move/copy reviewed existing render-only visual authoring into the candidate SubScene while preserving world transforms, prefab/source asset references, hierarchy semantics, and shared art identity.
- [x] Convert all current authored building placements to `OperationMapBuildingAuthoring` ECS ownership with health, faction, footprint, targeting, production/interaction parity where applicable, and intact/destroyed entity visuals.
- [x] Convert/verify all current authored vehicle placements through the existing unit/vehicle Baker path and eliminate duplicate source/static/runtime GameObject visuals.
- [x] Remove prohibited physics components from migrated instances without mutating shared third-party prefab assets.
- [x] Bake the candidate SubScene and validate expected gameplay/render-only/render-child entity counts, finite transforms/bounds, archetypes, and zero managed map visual components.
- [x] Prove repeated meshes/materials/textures have one shared package owner and placements contribute only compact entity/transform/render-reference data.
- [x] Add deterministic source-to-candidate-to-baked transform parity records and fail-closed Editor validation for every gameplay/render-only owner, including nested prefab, hierarchy, pivot, negative/non-uniform scale, `Parent`, and `PostTransformMatrix` cases.
  - Stable identity foundation and existing-candidate backfill passed on 2026-07-21: building, vehicle, and render-only candidate populators emit one passive `OperationMapEntityPresentationIdentityAuthoring`, which bakes to unmanaged `OperationMapEntityPresentationIdentity` data containing operation-map id, exact source `GlobalObjectId`, role, and placement index where applicable. The protected candidate contains and bakes exactly 9,544 unique identities. Backfill proved that all 9,090 old render-only candidates had source-world-matrix mismatches because the old root-copy path serialized source-local transforms as candidate-world transforms.
  - Accepted-source-to-candidate repair passed on 2026-07-21: render-only migration now mirrors deterministic transform-only source parent chains and parents each cloned owner with its original local transform, preserving inherited shear and negative/non-uniform scale without approximation. The transaction rebuilt all 9,090 render-only owners and validated owner matrices plus every cloned renderer matrix/world bounds at matrix `0.0001` and bounds `0.001 m` tolerances before save. In-memory ECS bake and all nine Bake All stages remain green at 9,544 identities / 14,212 render meshes / zero non-finite transforms / zero managed map visual companions. Remaining work is deterministic source/candidate/baked matrix and transformed-renderer-bounds report rows for all roles, special baked `Parent`/`PostTransformMatrix` fixtures, and the real Addressables-loaded runtime comparison.
  - Source/candidate/baked owner-matrix manifest landed fail-closed on 2026-07-21: all 9,544 stable identities join exactly once and every accepted-source → candidate → baked owner world matrix passes the `0.0001` residual after explicit ECS `Parent * LocalTransform * PostTransformMatrix` reconstruction. The first bounds pass correctly rejected 938 rows and exposed two defects rather than weakening acceptance: package-generated render entities needed deterministic authored-renderer ownership matching, and candidate vehicles had lost accepted scene-instance child overrides by being rebuilt from prefab defaults.
  - Direct baked visual parity passed twice on 2026-07-21 after repairing those defects. The validator now matches every accepted authored renderer bake entry to its material entity using Entities Graphics mesh/skinned-mesh bounds semantics and deterministic world-matrix/local-bounds keys; a bounded join tolerance is used only to recover package-generated render-entity ownership and never changes the `0.0001` owner-matrix or `0.001 m` transformed-bounds acceptance tolerances. Vehicle migration clones accepted scene instances to preserve child overrides, then reapplies authoritative placement root transforms. Final evidence is 9,544 candidate identities / 9,544 baked identities / 14,209 render meshes / zero rejected rows / zero non-finite transforms / zero managed map visual companions. A full transactional candidate Bake All also passes with production cutover disabled. The Addressables-loaded runtime comparison remains a separate open acceptance item below; Android parity is user-triggered and does not block other Editor work.
  - Runtime-comparison manifest foundation passed on 2026-07-21: parity schema v2 records all 14,209 baked render entities with deterministic world matrices, local bounds, transformed world bounds, and accepted owner identity where the package retains one. The manifest intentionally includes all 315 package-generated rows without an authored owner so the runtime gate must reproduce the complete render-entity multiset rather than silently discarding extras. This is dependency evidence only; the real Addressables-loaded candidate comparison remains open and checklist progress does not advance.
- [ ] Capture fixed-camera visual parity for terrain, roads, bridges, buildings, vehicles, mountains, vegetation, props, infrastructure, lighting, and minimap before changing production ownership.
- [x] Extend runtime binding/loading/readiness/teardown so `EntityScene` loads the map SubScene once, skips static-manifest/streamer binding, and unloads all map entities once.
- [x] Extend `OperationMapSceneView` and startup validation so empty legacy placement roots/configs are accepted only in validated `EntityScene` mode and cannot trigger managed map visual spawning.
- [x] Extend Bake All with fail-closed entity migration/readiness/bake/budget stages and rollback of source scenes, SubScene, definition, runtime binding, and Addressables layout.
- [x] Build a candidate local Addressables layout containing one runtime binding, one map entity-scene ownership path, shared art dependencies, and zero current-map static manifest/chunk/legacy-placement runtime entries.
- [ ] Run Editor load, gameplay interaction, building damage/destruction, vehicle behavior, camera traversal, menu return, and two-cycle teardown with zero map visual GameObjects or static handles.
- [ ] Run Android offline package, size, memory, frame, batching, visual parity, navigation, and two-cycle lifecycle acceptance against the exact candidate revision.
- [ ] After acceptance, remove the 514 static scenes/manifest/integrity artifacts from production labels/build ownership and delete them from tracked generated output only in a separately reviewable cleanup commit with rollback archive/hash evidence.

**Exit:** The existing physical map is fully presented by SubScene entities, no current-map static scene or managed map visual is loaded, and the dense-city generator can target the same accepted path.

### Phase 1: Generated Ownership Authoring

- [x] Add `DenseCityGeneratedRootAuthoring` and its Unity `.meta` file in `Game.Authoring`.
- [x] Add `DenseCityAuthoredOverrideAuthoring` and its Unity `.meta` file in `Game.Authoring`.
- [x] Add `OperationMapBuildingAuthoring` and bake intact/destroyed authoring roots to entity references using the existing `UnitGridAuthoring.Baker` pattern.
- [x] Make the building Baker recursively include shell and attached-prop descendants under their declared visual root; reject descendants shared across buildings, present under both states, outside both states, or parented to an independent render-only owner.
- [x] Add unmanaged `OperationMapBuildingComponent` and `OperationMapBuildingDestroyedComponent` data in `Game.Components`.
- [x] Add finite-value, stable-id, duplicate-id, size, and scene-ownership validation for all generated/override/building authoring types.
- [x] Add `DenseCitySemanticHierarchyBuilder` in `Game.Editor`.
- [ ] Create the exact proxy hierarchy in the operation-map authoring scene and gameplay/render-only entity presentation hierarchy in the existing map SubScene with identity transforms.
- [ ] Add exactly one nearest `MapBakeGroupAuthoring` role owner to each proxy semantic group; do not classify generated render entities as static-presentation inputs.
- [x] Reject duplicate/missing role roots, mismatched generation ids, misplaced override roots, role overlap, and roots outside the active operation-map scene pair.
- [x] Prove regeneration deletes only the two marked role roots and preserves all other objects in both scenes by identity/hash.

**Exit:** One logical disposable generation set spans two explicit scene-scoped roots, one persistent override domain remains protected, and generated building authoring has an explicit ECS bake contract.

### Phase 2: Generator Semantic Output

- [x] Add bounded `DenseCityGenerationRecords` value types and stable ordering rules.
- [x] Refactor building placement to emit ECS-building, foundation, blocker, intact-visual, and destroyed-visual records before realization.
- [x] Emit stable building-owned attachment records for roof/interior/shop/tent props during generation and realize them beneath the declared intact or destroyed root rather than the global render-only prop hierarchy.
- [x] Refactor road generation to emit distinct road, shoulder, terrain-patch, bridge, and ramp records.
- [x] Refactor canal/park generation to emit separate water exclusion, bridge, bank, terrain, vegetation, and light/prop records.
- [x] Refactor civic/market generation to separate hero presentation, terrain, road, and blocker records.
- [x] Refactor courtyard, wall, rock, mountain, vegetation, rooftop, power, and street-detail generation according to the semantic table.
- [ ] Place every damageable building under `EntityPresentation/GameplayBuildings` and every other generated visual under its explicit `EntityPresentation/RenderOnly` parent at creation time.
- [ ] Prove every repeated generated prefab placement shares source mesh/material identity and produces only transform/render entity instance data.
- [ ] Prove generated buildings never enter `MapBuildingPlacementConfig`, `RuntimeBuildingEntityLink`, or managed destroyed-visual instantiation.
- [ ] Remove all post-generation name/category detection from the production path.
- [ ] Preserve the exact deterministic seed and RNG call order unless a reviewed generator schema/version migration explicitly changes output.
- [ ] Add tests proving each generated feature kind emits the expected semantic records and hierarchy owner.

**Exit:** The generator never produces mixed semantic roots, building runtime ownership is ECS-only, and no ownership must be inferred later.

### Phase 3: Collider-Free Enforcement

- [ ] Add `DenseCityPhysicsComponentStripper` with instance-only removal semantics.
- [ ] Invoke physics-component removal immediately after every generated prefab/primitive realization path.
- [ ] Remove primitive-created colliders before parent assignment and record publication completes.
- [ ] Remove nested `Collider`, `Collider2D`, `Rigidbody`, and `Rigidbody2D` components, including inactive descendants.
- [ ] Add generated-root validation requiring zero prohibited physics components.
- [ ] Add operation-map source-scene validation requiring zero prohibited physics components in map-owned content.
- [ ] Add runtime binding and generated entity-scene dependency validation requiring zero prohibited physics components.
- [ ] Add build-dependency-closure validation for production scenes and Addressable map content.
- [ ] Add tests covering prefab instances, inactive children, primitive objects, removed-component overrides, and shared-prefab non-mutation.

**Exit:** No production operation-map path contains or depends on Unity collider/Rigidbody gameplay.

### Phase 4: Surface And Blocker Proxies

- [ ] Add `DenseCitySurfaceProxyBuilder` and map-scoped candidate-output ownership.
- [ ] Build simplified terrain proxy meshes from terrain/foundation/park records.
- [ ] Build road proxies separately from shoulders and natural ground patches.
- [ ] Build bridge deck and ramp proxies with correct layer and movement masks.
- [ ] Build canal/water exclusion data without colliders.
- [ ] Build coarse building/wall/rock blocker proxies from deterministic footprints.
- [ ] Merge compatible proxy geometry by semantic role and spatial chunk.
- [ ] Enforce deterministic winding, vertex/index ordering, finite values, and map bounds.
- [ ] Require zero renderers and zero prohibited components beneath proxy roots.
- [ ] Validate representative soldier, vehicle, building-placement, and aircraft-grounded surface queries against generated proxies.
- [ ] Validate generated content outside the current gameplay grid contributes presentation only unless explicitly approved otherwise.

**Exit:** ECS surface/blocker data represents city gameplay without detailed-mesh rasterization or Unity Physics.

### Phase 5: Readiness And Bake All Integration

- [ ] Add `OperationMapEntityPresentationReadinessValidator` with menu and batch entry points.
- [ ] Add `DenseCityBakeReadinessValidator` with menu and batch entry points.
- [ ] Validate exactly one generated root per required role, matching generation id/schema/version/seed, and deterministic content hashes.
- [ ] Validate zero unclassified generated renderers and zero detailed renderers beneath proxy roots.
- [ ] Validate each proxy has exactly one nearest bake-group owner.
- [ ] Validate protected authored content was not moved, disabled, renamed, deleted, or overlapped.
- [ ] Validate all legacy placement entries map one-to-one to accepted authored ECS entities and are absent from current-map runtime spawning/content references.
- [ ] Validate every generated ECS building has finite transform/footprint/health, one intact root, one destroyed root, shared mesh/material references, and no managed runtime link.
- [ ] Validate every attached prop resolves to exactly one building and one visual state, every intact attachment is hidden by the building transition, and no attachment is orphaned beneath an independent render-only owner.
- [ ] Insert transform-parity validation after candidate population, after ECS baking, and after candidate Addressables runtime load; any matrix/bounds mismatch must roll back candidate outputs and invalidate stale success evidence.
- [ ] Insert `entity-presentation-readiness` as the first content gate and `dense-city-readiness` as the conditional generated-content gate in `OperationMapCurrentMapBaker`.
- [ ] Insert `presentation-budget` after entity-scene baking and before runtime definition/Addressables publication; invoke the existing static-presentation bake only for definitions explicitly using `StaticSceneChunks`.
- [ ] Preserve stage report, stale-success invalidation, scene restoration, and transaction semantics.
- [ ] Add failure tests proving no accepted output changes after readiness, proxy, surface, presentation, or budget failure.
- [ ] Add two-run no-op validation for generation plus complete Bake All.

**Exit:** One button produces or rejects the complete map package without partial/stale success evidence.

### Phase 6: ECS Presentation Optimization

- [ ] Add `DenseCityPresentationBudgetValidator` and deterministic JSON evidence outside transient project folders.
- [ ] Report source/baked entity/render counts, unique meshes/materials/textures, triangles, shadows, batching eligibility, bounds, and spatial density.
- [ ] Report gameplay/render-only/render-child entity counts, archetype/entity-chunk counts, shared asset identities, entity-scene bytes, and intact/destroyed visual ownership.
- [ ] Report entity-scene/shared-dependency bytes, frozen rollback chunk bytes, duplicated dependency bytes, and production static-manifest/chunk counts required to be zero.
- [ ] Verify every existing/generated map visual is included through entity-scene ownership while proxy/source authoring objects and all current-map static chunk inputs are excluded.
- [ ] Preserve repeated building, tree, prop, road-module, bridge-module, vegetation, and infrastructure mesh/material references for Entities Graphics batching.
- [ ] Prove no existing/generated map mesh/material/texture binary is copied per entity, entity section, or retired presentation bundle.
- [ ] Combine only measured unique continuous terrain/road geometry; reject a whole-city mesh and combinations that destroy repeated-asset sharing.
- [ ] Add Burst `OperationMapBuildingDestructionSystem` and focused tests proving one-time intact-to-destroyed entity visual transition with no GameObject creation.
- [ ] Add destruction tests containing nested roof, interior, shop, and tent props; prove the complete intact hierarchy becomes hidden, the complete destroyed hierarchy becomes visible, missing destroyed equivalents leave no orphan, and the second update is a no-op.
- [ ] Keep rubble blockers unchanged by default; require an explicit per-record policy and deterministic dynamic-grid update before any destroyed building becomes passable.
- [ ] Add entity-compatible LOD/impostor authoring only where Android geometry/shadow evidence requires it.
- [ ] Add deterministic LOD/impostor source/output hashes and transition validation when introduced.
- [ ] Reject current-map static-presentation production chunks unless a named unique-heavyweight exception is added to this tracker with measured Android evidence.
- [ ] Validate shadows, small-detail stripping, transparent materials, and lighting against mobile budgets.

**Exit:** Presentation output is bounded, deterministic, visually complete, and suitable for Android profiling.

### Phase 7: Runtime Package Separation

- [ ] Prove both operation-map and map-SubScene source GameObject YAML hierarchies are absent from Addressables runtime entries and player build scenes.
- [ ] Prove `OperationMapAddressablesLayoutBuilder.SourceScenePath` remains the thin runtime binding scene.
- [ ] Regenerate the runtime binding scene and require renderer-free, collider-free `EntityScene` validation.
- [ ] Verify operation-map definition references the accepted runtime binding scene, surface, minimap, and entity SubScene with no required static manifest or current-map legacy placement runtime references.
- [ ] Verify the map SubScene is the sole runtime owner of every existing/generated map renderer and production Addressables contain zero current-map static manifest/chunk entries.
- [ ] Verify source meshes/materials/textures are shared by GUID and entity-scene render references rather than copied into placement-specific binary assets.
- [ ] Verify Build Layout reports no authoring hierarchy duplication, exactly one map entity-scene ownership path, and zero duplicated existing/generated map art dependencies.
- [ ] Verify local bundles remain included with the application and require no network access.

**Exit:** The player ships optimized runtime output, not the 225 MB-style editor hierarchy.

### Phase 8: Runtime Lifecycle Validation

- [ ] Run focused load, readiness, damage/destruction, vehicle behavior, failure, retry, teardown, and sequential-load tests with the complete ECS-presented map.
- [ ] Validate map readiness requires all expected existing/generated gameplay and render-only entities and no presentation preload queue.
- [ ] Load the candidate through its real Addressables/runtime-binding route and prove all permanent map-visual ECS matrices and transformed renderer bounds match the accepted Editor transform-parity manifest before gameplay systems update them.
  - Environment blocker recorded 2026-07-21: the transactional candidate local-content harness is implemented and compile-clean, but this Unity `6000.5.2f1` installation has no macOS Standalone Build Support playback engine. A macOS Editor-compatible packed build therefore fails in SBP before bundle output. Android candidate content is not accepted as a substitute because Android builds/validation are user-triggered. Owner: local Unity installation. Unblock by installing macOS Build Support for Unity `6000.5.2f1`; then rerun `Game/Operation Maps/EntityScene Migration/Build Candidate Runtime Parity Content`. Other non-Android tracker work continues.
- [ ] Validate camera traversal causes no map-visual scene load/unload; Entities Graphics culling/LOD changes visibility while entity identities remain stable.
- [ ] Validate map unload releases entity scene/subscene/map metadata without a static-presentation drain step.
- [ ] Validate two consecutive Deploy-to-Match-to-Menu cycles without retained map entities, static chunks, invalid handles, or stale ECS state.
- [ ] Validate `0 B/frame` managed allocation from operation-map orchestration after warmup.
- [ ] Validate no runtime hierarchy search, source-scene access, generator call, or collider query occurs.
- [ ] Validate no existing/generated map visual runtime GameObject, GameObject pool entry, `RuntimeBuildingEntityLink`, managed registry entry, or instantiated destroyed replacement exists.
- [ ] Validate editor PlayMode completely before any Android deployment.

**Exit:** Existing map/SubScene loading handles the complete ECS-presented physical map without a parallel lifecycle or static map-visual streamer.

### Phase 9: Android Acceptance

- [ ] Build local Addressables and Android APK/AAB from a clean checkout using the documented Unity wrapper/CI route.
- [ ] Verify application launch and Deploy entry with network disabled.
- [ ] Capture APK/AAB, installed size, bundle bytes, duplicate dependencies, and authoring-scene exclusion.
- [ ] Capture map load/preload/unload timings and progress behavior.
- [ ] Capture peak/retained memory and verify source authoring hierarchy is absent.
- [ ] Capture CPU/GPU frame distributions, draw/SetPass calls, visible renderers, triangles, and shadow casters across representative routes.
- [ ] Capture Entities Graphics batches, gameplay/render-only entity counts, culling/LOD behavior, entity-scene integration cost, and destruction-transition frame cost.
- [ ] Compare the Android Addressables-loaded transform/bounds parity report with the accepted Editor manifest and reject any moved, rotated, scaled, duplicated, or missing permanent map visual.
- [ ] Capture steady-state GC, transition allocations, thermal behavior, and sustained FPS.
- [ ] Validate navigation, placement, targeting, aircraft clearance, camera, minimap, runway, helipad, and scenario anchors.
- [ ] Validate entity LOD/impostor transitions, seams, missing geometry, and visual parity when applicable; confirm camera travel performs no map-visual scene streaming.
- [ ] Capture before/after destruction views for representative house, shop, tent, and military building variants and verify no intact shell, roof prop, interior dressing, sign, awning, or tent content remains visible after replacement.
- [ ] Compare against Phase 0 baseline and resolve every failed budget before acceptance.

**Exit:** The existing ECS-migrated map and giant city are accepted on the target Android device, not merely in Editor or by successful build.

### Phase 10: Documentation And Closeout

- [ ] Document the exact author workflow: edit overrides, clear/regenerate city, validate, Bake All, review report, device test.
- [ ] Document existing-map authored ECS editing, building/vehicle authoring, SubScene workflow, static rollback artifacts, and entity-scene cutover/rollback.
- [ ] Document generated versus authored ownership and how to preserve hand edits.
- [ ] Document every generated output owner and rollback path.
- [ ] Update the parent operation-map tracker with accepted dense-city implementation evidence.
- [ ] Update architecture/naming guardrails for all new types and exceptions, if any.
- [ ] Record final deterministic hashes, counts, package sizes, performance evidence, logs, and screenshots.
- [ ] Confirm all generated artifacts are committed through their accepted ownership policy and transient logs/build caches are excluded.
- [ ] Mark this tracker complete only after every required checklist item and Android gate passes.

**Exit:** Another agent can reproduce, validate, maintain, and roll back the complete workflow without undocumented assumptions.

## 22. Required Focused Tests

Planned test files in `Assets/Tests/Editor`:

- `DenseCityDeterministicFixtureTests.cs`
- `DenseCityGeneratedRootAuthoringTests.cs`
- `DenseCitySemanticHierarchyBuilderTests.cs`
- `DenseCityGenerationRecordTests.cs`
- `DenseCityPhysicsComponentStripperTests.cs`
- `DenseCitySurfaceProxyBuilderTests.cs`
- `DenseCityBakeReadinessValidatorTests.cs`
- `DenseCityPresentationBudgetValidatorTests.cs`
- `DenseCityBakeAllIntegrationTests.cs`
- `DenseCityRuntimePackageOwnershipTests.cs`
- `OperationMapBuildingAuthoringTests.cs`
- `OperationMapBuildingAttachedVisualOwnershipTests.cs`
- `OperationMapBuildingDestructionSystemTests.cs`
- `OperationMapEntityPresentationMigrationEditorTests.cs`
- `OperationMapEntityPresentationReadinessValidatorTests.cs`
- `OperationMapEntityPresentationTransformParityTests.cs`
- `OperationMapEntitySceneAddressablesOwnershipTests.cs`

Extend existing tests rather than duplicating them for:

- static presentation input, structural validation, ownership, rollback, integrity, and no-op behavior, including freeze/retirement and rejection of every existing/generated renderer after cutover;
- Entities Graphics baking for existing/generated gameplay and render-only placements, stable identities, intact/destroyed references, shared asset ownership, and map SubScene ownership;
- source/candidate/baked/runtime transform matrix and transformed-renderer-bounds parity, including nested hierarchy, pivot, negative/non-uniform scale, `Parent`, and `PostTransformMatrix` fixtures;
- operation-map Addressables layout and Build Layout validation;
- runtime binding scene generation/validation;
- scene/SubScene loading, complete entity readiness, absence of static presentation binding, handle lifetime, teardown, and sequential map loads;
- architecture naming, assembly direction, source growth, and ECS/Burst classification.

## 23. Required Validation Commands And Evidence

- Use `Tools/CI/invoke_unity_macos.sh` for every Unity batch validation.
- Keep Unity Hub open and signed in according to `AGENTS.md`.
- Store verbose logs under `/private/tmp`; record concise paths/results in the validation log.
- Run `git diff --check` before every stable commit.
- Run focused EditMode tests for the changed slice.
- Run affected architecture/naming/source-growth tests.
- Run compile validation with zero compiler errors.
- Run deterministic generate/bake twice and compare accepted output hashes.
- Run PlayMode lifecycle before Android.
- Run Android build/device validation according to the phase gates.
- Never claim visual or performance acceptance from static tests alone.

## 24. Validation Log

| Date | Slice | Evidence | Result | Notes |
|---|---|---|---|---|
| 2026-07-21 | Initial audit and tracker | Scene YAML/source audit; editor generation log | Design only | Previous generated city was cleared. No implementation or bake output is accepted by this tracker yet. |
| 2026-07-21 | ECS-first generated presentation decision | Sections 1-16, 19, and 21-25 of this tracker; parent tracker direction link | Design only | All generated permanent visuals target map SubScene entities with shared Entities Graphics assets. Generated-city GameObject chunks/pools are rejected; the existing static package is frozen rollback evidence pending the full-map cutover. |
| 2026-07-21 | Full current-map ECS presentation decision | Existing-map 16,542-source / 514-chunk / 451-building / 29-vehicle accepted baseline; revised sections 1-25 | Design only | Existing and generated map visuals now share one ECS SubScene presentation target. Current static scenes remain frozen rollback evidence during migration and leave production package/runtime ownership after accepted cutover. |
| 2026-07-21 | Building-attached visual ownership decision | Building authoring/data/runtime contracts plus Phases 0A, 1, 2, 5, 6, and 9 | Design only | Roof, interior, shop, tent, sign, awning, lamp, furniture, and utility visuals attached to a damageable building belong to exactly one intact/destroyed entity hierarchy and transition atomically with it. |
| 2026-07-21 | Editor-to-runtime transform parity defect accepted | Section 12.0A; Phase 0A/5/7/9 gates; audit of `OperationMapBuildingCandidateMigrationEditor`, `OperationMapVehicleCandidateMigrationEditor`, and `OperationMapRenderOnlyCandidateMigrationEditor` | Design gate added; implementation pending | The current candidate path has no source-to-baked-to-Addressables matrix proof. Render-only copies preserve world transforms, while building/vehicle candidates reconstruct placement transforms. Production cutover is now blocked until deterministic matrix and transformed-bounds parity passes twice in Editor and once on the user-triggered Android build. |
| 2026-07-21 | Phase 0 deterministic editor baseline | `OperationMapPhase0BaselineProbeTests`: 8/8 passed; two `/private/tmp/warline-phase0-baseline-*.json` captures differed only by their declared report path; concise logs at `/private/tmp/warline-phase0-baseline-tests.log` and `/private/tmp/warline-phase0-probe-{a,b}.log` | Partial pass | Schema v2 recorded the clean canonical authoring scene and map SubScene identities, 15,792 GameObjects, 15,671 renderers, 15,584 mesh renderers, 13,317 prefab-instance roots, 27 bake groups, zero generated roots, zero prohibited physics components, 269 static chunks, 11,892 static sources, protected-root candidate GlobalObjectIds, and current surface/manifest/integrity/minimap/runtime-binding identities. The checked-in Addressables Build Layout predates this manifest state, so package identity and authoring-scene exclusion rows remain open pending a post-commit rebuild. Android evidence and protected-root approval also remain open. |
| 2026-07-21 | Phase 0 post-commit Addressables baseline | `OperationMapAddressablesBuildReportBuilder.Run`; `/private/tmp/dense-city-phase0-addressables.log`; `/private/tmp/dense-city-phase0-post-addressables.json`; `OperationMapAddressables*`: 17/17 passed | Passed | Initial rebuild against `d4ad9cc38` proved authoring-scene exclusion for the 269-chunk/11,892-source package. Later superseded on `main` by `8f93dafe0` shader-bundle dedupe: build hash `872c797d60c0cbd26e4e430bb1b210f1`, 119 bundles, 119,993,045 aggregate bundle bytes. Android device evidence remains open. |
| 2026-07-21 | Isolated deterministic-city fixture | `DenseCityDeterministicFixtureTests`: 1/1 passed; `/private/tmp/dense-city-deterministic-fixture-tests.xml`; `/private/tmp/dense-city-deterministic-fixture-tests.log` | Passed | Built and cleared a bounded one-city fixture twice from an in-memory config clone, produced identical hierarchy hashes, and verified the canonical operation-map scene SHA-256 did not change. The fixture uses an isolated batch scene or additive saved-scene path and creates no project asset. |
| 2026-07-21 | Phase 0 protected-root confirmation | `Design/AgentReports/2026-07-21_dense_city_phase0_protected_roots_and_approvals.md`; baseline probe protected-root GlobalObjectIds | Passed | Confirmed eight present protected roots (`Buildings`, `Vehicles`, `Runways`, `Roads`, `Mountains` x2, `ResourceAreas`, `DenseCity_GradingArchive`) by GlobalObjectId. `AuthoredCityOverrides` is absent and remains a Phase 1 create-once root. The hierarchy/semantic and outside-grid decisions were subsequently approved by the project owner. |
| 2026-07-21 | Phase 0A inventory, GPT 5.6 cutover review, and non-mutating planner | Focused inventory/record/planner/root-marker tests: 37/37 passed; inventory SHA-256 `a0b1c332ce715a5346785c0727cee9dad1b70f78e1895a2618df682edfa8c66d`; dry-run record-set hash `6e771d490511963753ad32cc8018f8952de947b6d56bf71e9c1badc1d84bdda2`; placement-join set hash `fd4679d4f07d2a82e058c9617e467ce9120fa151fdaa19ba35ef11eaa4c20709`; `Design/AgentReports/2026-07-21_gpt56_phase0a_inventory_review_and_grok_handoff.md` | Passed | Resolved all 11,892 renderer-component identities, partitioned them into 9,090 non-overlapping migration owners, and joined all current 432 building / 22 vehicle placements with zero unresolved, ambiguous, mixed, or reused identities. Classified 10,683 static render-only and 1,209 protected authored sources. Dispositioned 696 controller-free inert Animators for omission; found zero blocking dependencies or external scene-object references. The real inventory produces `StaticOwnersReadyGameplayOwnersPending`; no accepted scene, SubScene, presentation mode, Addressables, or rollback artifact was mutated. Production mutation remains blocked on the Android baseline and later GPT 5.6 cut-point reviews. |
| 2026-07-21 | Phase 0 owner design approvals | Project-owner decisions captured in `Design/AgentReports/2026-07-21_dense_city_phase0_protected_roots_and_approvals.md` | Passed | Approved tracker sections 5 and 6 as the generated hierarchy/semantic ownership contract and approved presentation-only as the default for generated city content outside the gameplay grid. Android current-revision device evidence is the only remaining Phase 0 exit item. |
| 2026-07-21 | Phase 0 Android current-revision baseline | `adb install -r -d` Success on `R4M7PZEQZ58T59ZH`; `Tools/CI/dense_city_phase0_android_baseline_capture.py`; `Design/AgentReports/2026-07-21_dense_city_phase0_android_baseline.{md,json}`; APK SHA-256 `dab470fd296b9f2ca2866ba6042940137cce36beabaa886bdf2ce7dab9f8bc44` | CharacterizationCaptured | Dirty `8f93dafe0` release APK 459,188,380 bytes; installed ~631.6 MiB via `du` base.apk+lib. Match TOTAL PSS 2,489,021 KiB; Graphics 1,096,532 KiB; menu-after-unload 1,268,879 KiB. SurfaceFlinger 60-window average FPS 58.42 (min 30.59). Match-ready upper bound 40.79 s; unload-to-menu 16.08 s. Release logcat lacked draw/GC diagnostic markers (null). Phase 0 exit complete; not Phase 9 acceptance. |
| 2026-07-21 | Phase 0A vehicle ECS already-produced proof | `OperationMapVehicleEcsConversionInventoryProbeTests`: 4/4 passed; probe result `AllPlacementsAlreadyProduceEcs`; `Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.{md,json}` | Passed | All 22 vehicle placements exact-join to authored sources, resolve prefabs with `UnitGridAuthoring` vehicle motion + Model renderers + destroyed visuals, and hide authoring after spawn. Zero unresolved joins and zero conversion cleanup rows. Runtime path is `MapVehiclePlacementSpawnPrefabSystemHelper` → `RuntimeUnitPrefabSystem` → `UnitGridAuthoring.UnitGridBaker`. No scene/SubScene/Addressables/presentation-mode mutation. |
| 2026-07-21 | Phase 0A RuntimeBuildingEntity dependency inventory | `OperationMapRuntimeBuildingEntityDependencyInventoryProbeTests`: 5/5 passed; probe result `AllPlacementsRequireManagedRuntimeBuildingEntity`; `Design/AgentReports/2026-07-21_dense_city_phase0a_runtime_building_entity_dependency_inventory.{md,json}` | Passed | All 432 building placements exact-join and still require managed `RuntimeBuildingEntity` presentation. Catalogued 10 managed dependency surfaces with ECS ownership proposals. Feature counts: destroyed visuals 266, production 20, resource 70, runway 1. No scene/SubScene/Addressables/presentation-mode mutation. |
| 2026-07-21 | Phase 0A building attachment ownership inventory | `OperationMapBuildingAttachmentOwnershipInventoryProbeTests`: 6/6 passed; probe result `AttachmentOwnershipInventoryComplete`; `Design/AgentReports/2026-07-21_dense_city_phase0a_building_attachment_ownership_inventory.{md,json}` | Passed | Assigned 1,324 intact + 272 destroyed renderer attachments across 432 exact building joins with zero orphans, shared claims, or dual-state conflicts. Ownership uses exact source ancestry and configured destroyed-prefab references only; no name/proximity role inference. No scene/SubScene/Addressables/presentation-mode mutation. |
| 2026-07-21 | Phase 0A six-role owner classification | `OperationMapEntityPresentationOwnerClassificationProbeTests`: 11/11 passed; probe result `OwnerClassificationComplete`; `Design/AgentReports/2026-07-21_dense_city_phase0a_owner_classification.{md,json}`; mutation handoff `Design/AgentReports/2026-07-21_gpt56_phase0a_mutation_ready_handoff.md` | Passed | Classified 9,544 owners: 432 GameplayBuilding, 22 GameplayVehicle, 9,090 RenderOnlyEntity, plus MapMetadataProxy/ApprovedManagedBoundary catalogs; zero RejectedUnresolved. Authored buildings/vehicles are classified from exact placement joins because they are absent from the static-presentation manifest. All 432 buildings require managed RuntimeBuildingEntity until GPT ECS cutover. |
| 2026-07-21 | Phase 0A presentation-validation scaffolding + mutation readiness gate | `OperationMapPresentationKindContractTests`: 3/3 passed; `OperationMapEntityPresentationMutationReadinessTests`: 3/3 passed; `Design/AgentReports/2026-07-21_dense_city_phase0a_mutation_readiness_gate.md` | Passed | Confirmed fail-closed `EntityScene` content-reference/canonical-mode scaffolding remains inactive on production. Added `TryEvaluateMutationReadiness`; live Phase 0A evidence evaluates to `CandidateTransactionReadyPendingMutation`. No scene/SubScene/Addressables/presentation-mode mutation. |
| 2026-07-21 | Phase 0A protected candidate + building ECS transaction implementation | Roslyn compile using Unity Bee response files: `Game.Components`, `Game.Configs`, `Game.Authoring`, `Game.Runtime`, `Game.Editor`, and `Game.Tests.Editor` passed; accepted scene SHA-256 `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; accepted SubScene SHA-256 `eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d`; handoff `Design/AgentReports/2026-07-21_gpt56_phase0a_candidate_building_conversion_grok_handoff.md` | Implementation passed; mutation blocked | Added a fail-closed separate-GUID candidate scene transaction, exact 432-placement building copy transaction, explicit intact/destroyed baked visual roots, ECS identity/combat/resource/production data, and a static-blocker-safe destruction visual transition. The required wrapper reached Unity but failed before `executeMethod` because headless licensing rejected protocol `1.18.1`; no candidate asset was created and production remains `StaticSceneChunks`. Checklist count remains 17/144 until Unity executes, bakes, and validates the candidate. |
| 2026-07-21 | Phase 0A candidate mutation retry + render-only plan | `/private/tmp/dense-city-candidate-hierarchy.log`; restored inventory SHA-256 `a0b1c332ce715a5346785c0727cee9dad1b70f78e1895a2618df682edfa8c66d`; `OperationMapRenderOnlyCandidateMigrationPlanner(.cs)` + tests; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_mutation_retry_and_render_only_plan.md` | Mutation still blocked; planner ready | Re-ran `CreateProtectedCandidateHierarchy` through `invoke_unity_macos.sh`; same headless licensing protocol `1.18.1` failure before `executeMethod`. Restored the accepted inventory onto the probe default tmp path. Added a fail-closed Map-child-folder → `RenderOnly/*` bucket planner covering all 9,090 owners (Props 6880, Vegetation 928, RoadsAndBridges 825, Terrain 442, Mountains 13, Horizon 2). Accepted scene/SubScene hashes unchanged; candidate folder still absent; progress remains 17/144. |
| 2026-07-21 | Phase 0A protected candidate + 432 building populate | Non-batchmode Unity executeMethod logs `/private/tmp/dense-city-candidate-hierarchy-gui.log`, `/private/tmp/dense-city-populate-buildings-gui.log`; focused EditMode `/private/tmp/dense-city-candidate-tests-gui4.xml` 18/18; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_hierarchy_and_buildings_executed.md` | Passed | Stopped stuck batchmode; GUI licensing path succeeded. Created candidate GUID `0f9ecd54a7f0f467fa35556af7d28f1d` ≠ source `d50925a18e9164ce782536576cb833d8`; populated 432 `OperationMapBuildingAuthoring` with 432 IntactVisual + 266 DestroyedVisual; zero Collider/Rigidbody in candidate YAML; accepted scene/SubScene hashes unchanged; production not referenced. Fixed four focused test fixtures (canonical opmap ids, job Complete, untitled-scene SetUp). Production remains `StaticSceneChunks`. |
| 2026-07-21 | Phase 0A candidate render-only owner copy | `OperationMapRenderOnlyCandidateMigrationEditor.PopulateCandidateRenderOnlyOwners`; `/private/tmp/dense-city-populate-render-only-gui.log`; planner tests 3/3; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_render_only_owners_copied.md` | Passed | Copied all 9,090 render-only owners into candidate `RenderOnly/*` buckets (Props 6880, Vegetation 928, RoadsAndBridges 825, Terrain 442, Mountains 13, Horizon 2, Infrastructure 0) using authored Map-child-folder identity only. Preserved 432 buildings; candidate ~35 MiB; zero Collider/Rigidbody; accepted hashes unchanged; productionCutover=0. |
| 2026-07-21 | Phase 0A candidate bake validation | `OperationMapEntityPresentationCandidateBakeValidator.BakeAndValidateCandidateEntityPresentation`; `/private/tmp/dense-city-candidate-bake-validate3.log`; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_validation.json` | Passed | In-memory bake: `CandidateBakeValidationPassed` — gameplay buildings 432, presentation roots 3, building presentations 432, building render children 698, render-mesh entities 13,909, non-finite transforms 0, managed building companions 0. Accepted hashes unchanged; productionCutover=0. |
| 2026-07-21 | Phase 0A candidate vehicles + shared art ownership | `PopulateCandidateGameplayVehicles`; `/private/tmp/dense-city-populate-vehicles-gui.log`; `ProveSharedArtOwnership`; shared-art tests 1/1; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_vehicles_populated.md`; `Design/AgentReports/2026-07-21_dense_city_phase0a_shared_art_ownership.{json,md}` | Passed | Populated 22 GameplayVehicles prefab instances on the UnitGridAuthoring baker path; GameplayVehicles has 22 children. Shared art proof: 11,892 sources → 670 meshes / 39 materials / 671 prefabs, 0 missing assets, `SharedArtOwnershipProven`. Production remains `StaticSceneChunks`. |
| 2026-07-21 | Phase 0A EntityScene binding/load/streamer skip scaffolding | Non-batchmode EditMode `/private/tmp/dense-city-entityscene-scaffold-tests-gui4.xml` 19/19; `Design/AgentReports/2026-07-21_dense_city_phase0a_entityscene_binding_scaffolding.md` | Passed | Added `OperationMapEntityScenePresentationPolicy`; SceneView accepts empty placements only for EntityScene; loading helper skips manifest load and rejects bound static manifest GUIDs; ownership Initialize already clean-skips; MenuBootstrap skips streamer bind and treats EntityScene preload ready. Production definition unchanged (`StaticSceneChunks`). |
| 2026-07-21 | Phase 0A candidate EntityScene definition + Addressables ownership layout | Non-batchmode executeMethod `/private/tmp/dense-city-candidate-entityscene-addressables-gui9.log`; ownership tests `/private/tmp/dense-city-candidate-entityscene-addressables-tests2.xml` 2/2; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.{json,md}` | Passed | Candidate definition set to `EntityScene`; candidate runtime binding points at entity scene GUID `0f9ecd54…`; layout plan 1846 entries (1841 shared art) with zero static-manifest/chunk/legacy-placement runtime rows; production Addressables mutated=0; production remains `StaticSceneChunks`. |
| 2026-07-21 | Phase 0A transactional candidate Bake All implementation | `OperationMapEntitySceneCandidateBakeAll`; focused `/private/tmp/warline-phase0a-bake-all-focused-fix.log` 8/8; successful complete runs `/private/tmp/warline-phase0a-bake-all-run4.log` and `/private/tmp/warline-phase0a-bake-all-run5.log`; `git diff --check`; `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_all.{json,md}` | Passed | Recovered Unity licensing IPC with explicit approval. Fixed empty batch scene-setup restoration and preserved the candidate runtime-binding `.meta` GUID across rebuilds. Settled repeated runs are byte-identical for candidate SubScene, definition, runtime binding, and runtime-binding meta. Budgets pass at 432 buildings, 22 vehicles, 9,090 render-only owners, 3 roots, 14,212 render meshes, 1,841 shared dependencies, zero production cutover. Accepted scene/SubScene hashes remain `f210e53d...` / `eff3ce69...`; production remains `StaticSceneChunks`. Exact transform parity, fixed-camera parity, Editor lifecycle, and Android acceptance remain open. Progress is 29/148 (20%). |
| 2026-07-21 | Phase 0A per-owner transform identity foundation | `OperationMapEntityPresentationIdentityAuthoringTests.RunFocusedValidation`: `/private/tmp/warline-operation-map-identity-contract-after-reset.log` 5/5; `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: `/private/tmp/warline-operation-map-identity-architecture.log` 31/31; `EcsBurstHotPathArchitectureTests.RunFocusedValidation`: `/private/tmp/warline-operation-map-identity-ecs-architecture.log` 11/11; isolated Unity Bee/Roslyn compile of `Game.Components`, `Game.Authoring`, `Game.Editor`, and `Game.Tests.Editor`; `git diff --check` | Passed; parity gate remains open | Added a passive deterministic identity marker to all three candidate population paths and baked it into an unmanaged ECS component. Strict validation rejects malformed operation-map/source identity, unknown role, and role/placement mismatch. No runtime system, managed registry, service locator, hierarchy search, update loop, production definition, Addressables ownership, source scene, or candidate scene changed. Initial headless and windowed validations hit the known licensing protocol loop; the project recovery script was run after explicit user authorization, then all focused gates passed. Progress remains 29/148 (20%) until the full source/candidate/baked/runtime matrix-and-bounds parity checkbox is accepted. |
| 2026-07-21 | Phase 0A existing-candidate identity backfill and bake gate | Backfill `/private/tmp/warline-operation-map-identity-backfill-execute-4.log`; in-memory bake `/private/tmp/warline-operation-map-identity-bake-validation.log`; schema-v2 full Bake All `/private/tmp/warline-operation-map-identity-bake-all-schema2.log`; identity tests `/private/tmp/warline-operation-map-identity-backfill-focused-5.log` 8/8; assembly boundaries `/private/tmp/warline-operation-map-identity-backfill-architecture.log` 31/31; ECS/Burst `/private/tmp/warline-operation-map-identity-backfill-ecs-architecture.log` 11/11; candidate SHA-256 `9009f26b...18adc5`; accepted source/SubScene SHA-256 `f210e53d...` / `eff3ce69...`; `git diff --check` | Passed identity recovery; parity rejected pending correction | Transactionally backfilled exactly 9,544 unique source identities into the existing candidate (432 buildings, 22 vehicles, 9,090 render-only), then proved all 9,544 bake to unmanaged ECS components with 14,212 render meshes, zero non-finite transforms, and zero managed map visual companions. Full Bake All reran with the identity stage idempotently `AlreadyComplete`; all stages passed and productionCutover remained 0. The diagnostic join proved the old render-only copy path uses source-local transform values as candidate-world values: 9,090/9,090 render-only owners mismatch accepted source world matrices. This is now a quantified fail-closed migration defect and the next priority. Progress remains 29/148 (20%). |
| 2026-07-21 | Phase 0A render-only source-to-candidate transform repair | Transaction `/private/tmp/warline-operation-map-transform-repair-execute.log`; identity/hierarchy tests `/private/tmp/warline-operation-map-transform-repair-focused-elevated.log` 10/10; in-memory ECS bake `/private/tmp/warline-operation-map-transform-repair-bake-validation-2.log`; full Bake All `/private/tmp/warline-operation-map-transform-repair-bake-all.log`; assembly boundaries `/private/tmp/warline-operation-map-transform-repair-architecture.log` 31/31; ECS/Burst `/private/tmp/warline-operation-map-transform-repair-ecs.log` 11/11; candidate SHA-256 `b36cfb4d...ca7081f7`; accepted source/SubScene SHA-256 `f210e53d...` / `eff3ce69...`; `git diff --check` | Passed source-to-candidate render-only parity; full parity gate remains open | Replaced the lossy root-copy route with deterministic transform-only mirrors of the accepted source parent chains, preserving local transforms beneath those chains instead of decomposing world matrices. Transactionally rebuilt all 9,090 render-only owners and fail-closed validated each owner matrix plus cloned renderer matrices/world bounds before save. Candidate still bakes 9,544 identities and 14,212 render meshes with zero non-finite transforms or managed map visual companions; all nine Bake All stages pass; productionCutover remains 0. Baked matrix/bounds manifest rows and real Addressables-loaded runtime parity remain the next priority. Progress remains 29/148 (20%). |
| 2026-07-21 | Phase 0A source/candidate/baked transform manifest | `OperationMapEntityPresentationTransformParityValidator`; `/private/tmp/warline-operation-map-baked-transform-parity-5.log`; `Design/AgentReports/2026-07-21_dense_city_phase0a_transform_parity.json`; `git diff --check` | Owner matrices passed; renderer bounds rejected fail-closed | Emitted 9,544 deterministic identity-keyed rows with accepted source local/world matrices and parent identity chain, candidate local/world matrices, reconstructed baked world/local/`PostTransformMatrix` data, and transformed renderer bounds. Candidate and baked identity joins are both exactly 9,544. All owner matrices pass `0.0001`; 938 bounds rows remain rejected (9 GameplayVehicles and 929 RenderOnly: 777 residual, 161 presence). Production remains `StaticSceneChunks`, productionCutover=0, and Android validation is deferred without blocking further Editor work. Progress remains 29/148 (20%). |
| 2026-07-21 | Phase 0A direct baked renderer ownership and bounds parity | Candidate vehicle refresh `/private/tmp/warline-operation-map-refresh-candidate-vehicles-2.log`; consecutive parity passes `/private/tmp/warline-operation-map-baked-transform-parity-final-2.log` and `/private/tmp/warline-operation-map-baked-transform-parity-final-3.log`; full transactional Bake All `/private/tmp/warline-operation-map-candidate-bake-all-parity-passed.log`; assembly boundaries `/private/tmp/warline-operation-map-parity-architecture.log` 31/31; ECS/Burst `/private/tmp/warline-operation-map-parity-ecs.log` 11/11; `Design/AgentReports/2026-07-21_dense_city_phase0a_transform_parity.json`; `git diff --check` | Passed direct source/candidate/baked matrix and transformed-bounds parity twice; runtime parity remains open | Deterministically matched package-generated render entities back to accepted authored renderer bake entries using world matrix plus local bounds, without weakening the `0.0001` owner-matrix or `0.001 m` transformed-bounds acceptance tolerances. Rebuilt 22 candidate vehicles from accepted scene instances so per-instance child transforms survive ECS conversion, while placement config remains authoritative for each vehicle root. Final proof: 9,544 candidate identities, 9,544 baked identities, 14,209 render meshes, zero rejected rows, zero non-finite transforms, and zero managed map visual companions. Full candidate Bake All passes with productionCutover=0; production remains `StaticSceneChunks`. Addressables-loaded runtime/fixed-camera/lifecycle gates remain open, and Android remains user-triggered. Progress is 30/148 (20%). |
| 2026-07-21 | Phase 0A runtime-comparison render manifest foundation | Direct candidate bake `/private/tmp/warline-operation-map-runtime-manifest-direct-bake.log`; assembly boundaries `/private/tmp/warline-operation-map-runtime-manifest-architecture.log` 31/31; ECS/Burst `/private/tmp/warline-operation-map-runtime-manifest-ecs.log` 11/11; `Design/AgentReports/2026-07-21_dense_city_phase0a_transform_parity.json`; `dotnet build Game.Editor.csproj --no-restore`; `git diff --check` | Passed dependency foundation; Addressables runtime comparison remains open | Advanced the transform-parity report to schema v2 and recorded all 14,209 baked render entities with world matrix, local bounds, transformed world bounds, and accepted owner identity where available. The 315 package-generated unowned rows remain explicit, so the future runtime validator must match the complete multiset. Direct parity remains 9,544/9,544 identities with zero rejected rows. No runtime system, production Addressables entry, source scene, candidate scene, presentation mode, or production cutover changed. Progress remains 30/148 (20%). |
| 2026-07-21 | Phase 8 transactional candidate runtime-content harness | `OperationMapEntitySceneCandidateRuntimeContentBuilder`; `OperationMapEntitySceneBuildAdditions.UseCurrentProcessSceneOverride`; compile `/private/tmp/warline-operation-map-candidate-runtime-builder-compile.log`; Android-target content probe `/private/tmp/warline-operation-map-candidate-runtime-content-build.log`; macOS-target rejection `/private/tmp/warline-operation-map-candidate-runtime-content-standalone-build.log`; active-target restore `/private/tmp/warline-restore-android-active-target-no-build.log`; `git diff --check` | Implementation compile-clean; packed Editor runtime validation blocked by missing macOS Standalone Build Support | Added a scoped build-process-only EntityScene override and a temporary local Addressables group transaction for the candidate definition/runtime binding. The transaction restores Addressables settings byte-for-byte and never moves or relabels production entries. An initial content-only probe inherited the Android active target and was discarded as acceptance evidence; no APK/AAB/player build ran. The corrected harness now fails immediately unless the target is StandaloneOSX and the macOS playback engine is installed. This Unity installation lacks that module, so real packed Editor runtime parity remains open with owner `local Unity installation`; other non-Android work continues. Progress remains 30/148 (20%). |
| 2026-07-21 | Phase 1 generated ownership authoring contracts | Focused EditMode `/private/tmp/warline-dense-city-authoring-contract-tests-2.xml` 3/3; assembly boundaries `/private/tmp/warline-dense-city-authoring-contract-architecture.log` 31/31; Unity compile `/private/tmp/warline-dense-city-authoring-contract-tests-2.log`; `git diff --check` | Passed | Added passive `DenseCityGeneratedRootAuthoring` with closed MapBakeSource/EntityPresentationSource roles, shared generation identity, schema/version, seed, and lowercase SHA-256 generation hash. Added collider-free `DenseCityAuthoredOverrideAuthoring` with stable identity, finite positive local bounds, and explicit presentation/surface/blocker exclusions. Both live in `Game.Authoring`, have no Update methods or runtime ownership, and include Unity metadata plus focused malformed-input rejection. Progress is 32/148 (22%). |
| 2026-07-21 | Phase 1 operation-map building ECS state contract | Focused EditMode `/private/tmp/warline-operation-map-building-components-tests.xml` 2/2; assembly boundaries `/private/tmp/warline-operation-map-building-components-architecture.log` 31/31; Unity compile `/private/tmp/warline-operation-map-building-components-tests.log`; `git diff --check` | Passed | Audited the existing `OperationMapBuildingAuthoring` root-reference baker and advanced it to the approved ECS contract. Added closed `OperationMapBuildingBlockerPolicy` with only `RubbleRemainsBlocked`, unmanaged stable `OperationMapBuildingComponent`, and an enableable `OperationMapBuildingDestroyedComponent` baked disabled to avoid a future per-destruction structural add. Unsupported blocker policy fails authoring validation. No destruction behavior or production presentation mode changed; recursive descendant ownership validation remains open. Progress is 34/148 (23%). |
| 2026-07-21 | Phase 1 building visual hierarchy ownership | Focused EditMode `/private/tmp/warline-operation-map-building-ownership-tests.xml` 4/4; protected candidate bake `/private/tmp/warline-operation-map-building-ownership-candidate-bake.log`; assembly boundaries `/private/tmp/warline-operation-map-building-ownership-architecture.log` 31/31; `git diff --check` | Passed | The building baker now explicitly includes every transform beneath intact/destroyed roots and fails closed on overlapping state roots, renderers outside both states, nested/shared building owners, incorrect presentation-role ancestry, and independent or mismatched descendant presentation identities. The actual protected candidate remains valid with 432 gameplay buildings, 698 building render children, 9,544 identities, 14,209 render entities, zero non-finite transforms, zero managed map visual companions, and `productionCutover=0`. Progress is 35/148 (24%). |
| 2026-07-21 | Phase 1 semantic hierarchy builder | Focused EditMode `/private/tmp/warline-dense-city-semantic-hierarchy-tests-3.xml` 2/2; assembly boundaries `/private/tmp/warline-dense-city-semantic-hierarchy-architecture.log` 31/31; Unity compile `/private/tmp/warline-dense-city-semantic-hierarchy-tests-3.log`; `git diff --check` | Passed | Added editor-only `DenseCitySemanticHierarchyBuilder` with explicit operation-map and entity-presentation scene inputs. It creates identity-transformed marked roots, the five exact `MapBakeGroupAuthoring` proxy roles, generated gameplay-building buckets, and render-only buckets; validates shared deterministic generation identity and nearest proxy ownership; and rejects same-scene use or overwrite of an existing marked root. It does not generate districts, save scenes, delete accepted content, search at runtime, or change production presentation. Progress is 36/148 (24%). |
| 2026-07-22 | Phase 1 authoring and scene-ownership readiness | Focused validator `/private/tmp/warline-dense-city-authoring-readiness-tests-2.xml` 4/4; authoring regression `/private/tmp/warline-dense-city-authoring-contract-regression-tests.xml` 17/17; assembly boundaries `/private/tmp/warline-dense-city-authoring-readiness-architecture.log` 31/31; Unity logs; `git diff --check` | Passed; full Phase 5 readiness remains open | Centralized strict source `GlobalObjectId` validation; added finite/nonzero transform and positive-size validation for generated roots, authored overrides, and buildings; and added an editor-only scene-pair ownership validator that rejects duplicate override ids, duplicate building source/placement identities, misplaced buildings, wrong generated-root identities, and invalid building presentation ownership. This validates the Phase 1 authoring/ownership slice only; collider, proxy, bake-budget, package, and full readiness checks remain explicit Phase 3-5 work. Progress is 37/148 (25%). |
| 2026-07-22 | Phase 1 fail-closed semantic role ownership | Focused EditMode `/private/tmp/warline-dense-city-semantic-ownership-tests-2.xml` 4/4; assembly boundaries `/private/tmp/warline-dense-city-semantic-ownership-architecture.log` 31/31; Unity compile logs; `git diff --check` | Passed | Tightened semantic-path validation from first-name lookup to exact direct-child traversal, so duplicate path segments cannot be accepted. Proxy groups now require one component on the exact role node and reject overlapping ancestor role ownership. Existing generation-id, root-role, scene-pair, override-placement, and generated-building owner checks remain fail-closed. No accepted scene, candidate scene, production package, or runtime system changed. Progress is 38/148 (26%). |
| 2026-07-22 | Phase 1 transactional generated-root replacement | Focused EditMode `/private/tmp/warline-dense-city-semantic-replacement-tests-2.xml` 5/5; assembly boundaries `/private/tmp/warline-dense-city-semantic-replacement-architecture.log` 31/31; Unity compile logs; `git diff --check` | Passed in isolated scene pair; accepted assets intentionally unchanged | Added the approved editor coordinator operation that accepts only zero roots or one already-valid paired generation set, destroys only the two marked roots through one Undo transaction, recreates and validates the replacement pair, marks both scenes dirty, and reverts the transaction on failure. The focused fixture saves both scenes and proves unmarked roots retain their exact `GlobalObjectId` while both marked roots are replaced. This is mutation-safety evidence, not authorization to alter the protected accepted source/candidate before generator semantic output is wired. Progress is 39/148 (26%). |
| 2026-07-22 | Phase 2 bounded semantic generation records | Focused EditMode `/private/tmp/warline-dense-city-generation-records-tests-final.xml` 5/5; assembly boundaries `/private/tmp/warline-dense-city-generation-records-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-generation-records-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed | Added the tracker-approved editor-only closed presentation/surface categories, deterministic record identity, immutable building/surface/presentation bake records, and editor-lifetime `DenseCityGenerationRecordSet`. The set requires explicit bounded capacities, rejects duplicate stable identities across all record kinds, cannot be read before sealing or mutated afterward, sorts each record kind ordinally by deterministic identity, exposes copied collections read-only, and clears owned collections on disposal. Geometry, asset identity, transform, health, blocker policy, and attachment ownership validate fail-closed. No generator realization, RNG order, runtime assembly, scene, SubScene, or package changed. Progress is 40/148 (27%). |
| 2026-07-22 | Phase 2 atomic building-record transaction foundation | Focused EditMode `/private/tmp/warline-dense-city-building-record-batch-tests.xml` 7/7; assembly boundaries `/private/tmp/warline-dense-city-building-record-batch-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-building-record-batch-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generator placement integration remains open | Added an editor-only atomic building-record group operation covering simulation, explicit foundation ownership, explicit blocker ownership, intact presentation, and destroyed presentation. Capacity, identity, category, and duplicate checks complete before mutation; failed preflight leaves the record set unchanged, and explicit group removal supports rollback after a later realization rejection without name or stable-key-prefix inference. No generator, RNG order, scene, SubScene, runtime assembly, Addressables ownership, or production presentation mode changed. The full building-placement checklist item remains incomplete until `SpawnBuilding` emits and rolls back these records around realization. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 explicit placement semantic input | Focused EditMode `/private/tmp/warline-dense-city-placement-semantic-tests-2.xml` 2/2; assembly boundaries `/private/tmp/warline-dense-city-placement-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-placement-semantic-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; record emission remains open | Made placement presentation category a required `PrefabFootprint` input and assigned it explicitly where palettes and direct placements are constructed: houses, shops, halls, clock towers, and landmarks are gameplay-building intact inputs; trees are vegetation; fountains are props. Unsupported/unclassified categories fail construction. No prefab-name detection, default category, generated output, RNG call, realization order, scene, SubScene, or production path changed. This removes the mixed-use `SpawnBuilding` ambiguity before record emission is introduced. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 deterministic building-record factory | Focused EditMode `/private/tmp/warline-dense-city-building-record-factory-tests.xml` 2/2; assembly boundaries `/private/tmp/warline-dense-city-building-record-factory-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-building-record-factory-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; asset extraction and placement integration remain open | Added a pure editor factory that converts explicit generator schema/seed/district/sequence, source prefab identities, material identities, world transform, footprint, foundation, blocker, faction, health, movement, layer, and chunk inputs into one linked five-record group. It preserves separate intact/destroyed source identities, emits a deterministic rotated world-space footprint polygon, and commits through the already-validated atomic record-set operation. No AssetDatabase lookup, generator realization, RNG call/order, scene, SubScene, runtime system, or production ownership changed. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 stable visual asset metadata extraction | Focused EditMode `/private/tmp/warline-dense-city-visual-asset-metadata-tests.xml` 2/2 against the real `SM_Bld_Shop_04` asset; assembly boundaries `/private/tmp/warline-dense-city-visual-asset-metadata-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-visual-asset-metadata-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; destroyed-visual policy and placement integration remain open | Added an editor-only extraction boundary for persistent prefab GUID/local ID and ordinal unique material GUIDs. It rejects scene-only objects, missing persistent materials, and ambiguous material subassets that share a GUID but require local-id identity, avoiding runtime asset discovery and name inference. The extractor does not inspect or mutate scenes, instantiate visuals, alter RNG, or change runtime/Addressables ownership. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 explicit generated-building role/destruction policy | Production-config EditMode `/private/tmp/warline-dense-city-generated-building-policy-tests.xml` 1/1; footprint-role EditMode `/private/tmp/warline-dense-city-placement-role-tests.xml` 3/3; assembly boundaries `/private/tmp/warline-dense-city-generated-building-policy-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-generated-building-policy-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; placement record emission remains open | Added the closed `GeneratedCityBuildingRole` contract and required every gameplay-building footprint to declare House, Shop, Civic, or Other while forbidding building roles on vegetation/props. The existing runtime-city config now owns explicit serialized destroyed-visual prefab references for all four roles and resolves them through one fail-closed API. Production config tests verify exact persistent assets. This removes destroyed-visual and role name inference without changing generated transforms, RNG order, scenes, SubScenes, runtime presentation, or Addressables ownership. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 deterministic material-variant selection | Focused EditMode `/private/tmp/warline-dense-city-material-selector-tests.xml` 3/3; assembly boundaries `/private/tmp/warline-dense-city-material-selector-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-material-selector-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; material asset library/application and placement integration remain open | Extracted the existing position/seed material decision into a pure editor-only selector with an explicit generated-building role and exact material-family inputs. It preserves the established hash salt, six facade tint choices, Shop_05 20% original-material rule, and five recolor tones while rejecting unclassified/non-finite inputs and skipping civic or unsupported material families. The selector performs no hierarchy scan, name inference, AssetDatabase lookup, material mutation, RNG consumption, scene mutation, or runtime work. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 persistent building-material library extraction | Focused EditMode `/private/tmp/warline-dense-city-material-library-tests-2.xml` 2/2; deterministic fixture `/private/tmp/warline-dense-city-material-library-fixture.xml` 1/1 over two builds; assembly boundaries `/private/tmp/warline-dense-city-material-library-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-material-library-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; pre-realization placement integration remains open | Moved the persistent source loading, exact source/variant family classification, six-by-three facade material update, five Shop_05 recolored texture/material update, and selected-material resolution into one editor-only library. The builder's post-pass now delegates to the pure selector and exact material identity instead of Shop_05 wrapper-name detection, while preserving Hall exclusion, palette counters, visible-slot fail-closed checks, checked-in asset paths, source-art sharing, and mixed Shop_05/generic-facade slot behavior. The isolated fixture built identically twice and left the canonical map unchanged. No runtime assembly, RNG order, generated transform, scene/SubScene, Addressables ownership, or production presentation changed. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 building placement record transaction | Focused EditMode `/private/tmp/warline-dense-city-building-placement-transaction-tests.xml` 3/3; assembly boundaries `/private/tmp/warline-dense-city-building-placement-transaction-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-building-placement-transaction-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generator call-graph integration remains open | Added one editor-only coordinator that atomically adds the linked building/foundation/blocker/intact/destroyed record group before invoking realization, keeps it only after an accepted realization, removes all five records after a rejected realization, and removes all five before rethrowing a realization exception. Tests prove all three outcomes and prevent orphan semantic records. The coordinator performs no scene lookup, hierarchy scan, asset discovery, runtime work, or visual cleanup; generator integration remains responsible for cleaning partially realized visuals and supplying stable deterministic inputs. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 bounded generation transaction context | Focused EditMode `/private/tmp/warline-dense-city-generation-transaction-context-tests.xml` 2/2; assembly boundaries `/private/tmp/warline-dense-city-generation-transaction-context-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-generation-transaction-context-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; builder call-graph wiring remains open | Added one disposable editor-generation context that exclusively owns the bounded record set and deterministic five-record sequence allocation per explicit district. Accepted placements retain one complete group; rejected attempts consume their deterministic attempt sequence but leave no records, preventing later identities from depending on realization acceptance side effects. The context has no static state, hierarchy/asset discovery, inferred district/default surface data, runtime dependency, or visual ownership. Placement sites must still supply their exact district, transform, surface, chunk, metadata, and realization cleanup behavior. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 pre-realization resolved material metadata | Focused EditMode `/private/tmp/warline-dense-city-visual-asset-resolver-tests.xml` 4/4 against persistent production art/material assets; assembly boundaries `/private/tmp/warline-dense-city-visual-asset-resolver-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-visual-asset-resolver-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; full placement input construction remains open | Extended the existing editor-only visual metadata extractor with an explicit material resolver. It now records the sorted unique persistent GUIDs of the materials selected for a placement before realization, while preserving the original prefab GUID/local-id identity and rejecting null or nonpersistent resolved materials. A production Shop_04 test proves the selected checked-in facade variant GUID is captured without instantiating or scanning a generated hierarchy. No name inference, asset creation, scene mutation, runtime lookup, RNG consumption, or production presentation change was introduced. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 complete pre-realization building record input | Focused EditMode `/private/tmp/warline-dense-city-placement-record-builder-tests.xml` 1/1 against the production runtime-city config, Shop_04 source prefab, configured Shop destroyed visual, and selected checked-in facade material; assembly boundaries `/private/tmp/warline-dense-city-placement-record-builder-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-placement-record-builder-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; `SpawnBuilding` integration remains open | Added an editor-only bridge that accepts every semantic placement field explicitly, extracts persistent intact/destroyed prefab and material identities before realization, and produces the pure linked five-record group. It carries generator schema/seed/district/sequence, transform, footprint, foundation, blocker bounds, frontage, faction, health, movement mask, surface layer, and chunk without guessed defaults. The production-asset test verifies configured destruction policy and selected material identity. No hierarchy scan, scene mutation, runtime work, RNG consumption, or production presentation change was introduced. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 exact pre-realization building placement plan | Focused EditMode `/private/tmp/warline-dense-city-placement-plan-focused.log` 2/2; assembly boundaries `/private/tmp/warline-dense-city-placement-plan-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-placement-plan-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; `SpawnBuilding` transaction wiring remains open | Added a pure editor-only planner that reproduces the existing grid-origin clamp, footprint-cell sizing, quarter-turn orientation, authored foundation elevation, frontage-edge alignment, wrapper world matrix, blocker bounds, footprint dimensions, frontage direction, and presentation chunk before any visual instantiation. It rejects non-finite, non-quarter-turn, and out-of-grid inputs. This gives the transaction an exact semantic placement candidate that realization can verify before records are retained; it does not instantiate objects, inspect hierarchies, consume RNG, mutate scenes, or change runtime/Addressables ownership. Progress remains 40/148 (27%). |
| 2026-07-22 | Phase 2 atomic building-placement integration | Full in-memory generation `/private/tmp/warline-dense-city-building-transaction-integration-12.log`; planner `/private/tmp/warline-dense-city-placement-plan-focused-4.log` 4/4; destroyed-visual policy `/private/tmp/warline-dense-city-building-policy.log` 2 configs; assembly boundaries `/private/tmp/warline-dense-city-building-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-building-ecs.log` 11/11; accepted scene SHA-256 before/after comparison; `git diff --check` | Passed; building placement record emission complete | Wired the existing `SpawnBuilding` call graph through one bounded generation transaction: every accepted gameplay building now commits its linked ECS-building, foundation, blocker, intact-presentation, and destroyed-presentation records before visual realization, while rejected or exceptional realization removes the complete group and destroys partial wrappers. The full map-wide fixture produced 5,759 district buildings and 150 civic/market placements for exactly 5,909 linked building groups, 11,818 surface records, and 11,818 presentation records with zero placement-parity rejection. The disposable generated root now moves to scene-root ownership and resolves to an exact identity world transform before regeneration, preventing the transformed `Map` parent from introducing Editor/runtime placement offsets or one-ULP matrix drift. Realization uses the precomputed parent-local transform once, validates matrix residual plus conservative blocker containment, and does not re-snap permanent visuals after record commitment. The accepted operation-map scene bytes remained unchanged, production remains `StaticSceneChunks`, Addressables ownership was untouched, and no Android build was run. Progress is 41/148 (28%). |
| 2026-07-22 | Phase 2 building-attachment ownership and transaction foundation | Focused owner registry `/private/tmp/warline-dense-city-owner-registry-4.xml` 3/3; focused attachment transaction `/private/tmp/warline-dense-city-attachment-transaction.xml` 4/4; assembly boundaries `/private/tmp/warline-dense-city-attachment-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-attachment-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generated attachment realization integration remains open | Accepted building transactions now publish an explicit editor-lifetime owner entry containing the committed building identity, source prefab, declared building role, and intact presentation root. The urban attachment collector consumes that registry instead of rediscovering owners and House/Shop roles from generated wrapper names. Added a bounded attachment transaction that validates the owner is already committed, publishes one attachment presentation before acceptance, and removes it on rejected or exceptional realization. Presentation capacity now explicitly reserves bounded attachment headroom. No generated attachment path is switched yet, no scene/SubScene/Addressables/runtime ownership changed, and progress remains 41/148 (28%). |
| 2026-07-22 | Phase 2 generated building-owned attachment integration | Focused attachment suite `/private/tmp/warline-dense-city-owned-attachments-focused-3.xml` 5/5; full in-memory map generation `/private/tmp/warline-dense-city-owned-attachments-integration-2.log`; two-build deterministic fixture `/private/tmp/warline-dense-city-owned-attachments-determinism.xml` 1/1; assembly boundaries `/private/tmp/warline-dense-city-owned-attachments-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-owned-attachments-ecs.log` 11/11; accepted scene hash preserved; `git diff --check` | Passed; generated building-owned attachment item complete | Added district-stable attachment identity allocation, persistent prefab/material metadata extraction, record-to-realized-transform tracking, exact matrix parity checks, and immediate-parent ownership validation. All generated shop roof caps, rooftop water tanks, rooftop utility props, shop-wall props, and civic cloth-cover/umbrella tent props now instantiate beneath the exact committed intact building root and emit `BuildingAttachmentIntact` presentations; the former global rooftop/shop attachment roots were removed. Full generation produced 5,909 semantic buildings and 2,781 owned attachments for 14,599 total presentations with no orphan, owner, or transform mismatch. The generator has no standalone interior-detail pass; interior renderers embedded in source prefabs remain part of the intact prefab asset and therefore share its declared building ownership instead of being duplicated as inferred records. No destroyed-state generated attachments currently exist, so the context rejects that category until an explicit destroyed root is introduced. Production remains `StaticSceneChunks`, scenes/SubScenes/Addressables were not mutated, no Android build ran, and progress is 42/148 (28%). |
| 2026-07-22 | Phase 2 infrastructure transaction foundation | Focused EditMode `/private/tmp/warline-dense-city-infrastructure-transaction-recovered.xml` 5/5; assembly boundaries `/private/tmp/warline-dense-city-infrastructure-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-infrastructure-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generator road emission remains open | Added an atomic surface-plus-infrastructure-presentation transaction for terrain patches, roads, bridges, and visually realized ramps plus a separate surface-only rollback transaction so approach-ramp proxies never require invented visuals. Both operations preflight category, semantic kind, capacity, and duplicate identity before mutation and remove complete committed state after rejected or exceptional realization. The initial unsupported-protocol licensing loop was recovered only after confirming no Editor was running, using the guarded repository IPC reset with `--confirm-no-editors --quit-hub`, and reopening Hub; no active Editor was interrupted. No generator output, RNG order, scene, SubScene, package, runtime path, or production ownership changed. Progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 deterministic infrastructure record factory | Focused EditMode `/private/tmp/warline-dense-city-infrastructure-record-factory.xml` 3/3; assembly boundaries `/private/tmp/warline-dense-city-infrastructure-record-factory-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-infrastructure-record-factory-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generator road emission remains open | Added a pure editor-only factory that converts explicit schema, seed, district, sequence, semantic kind, source prefab identity, material identity, exact world matrix, surface dimensions/elevation, movement mask, layer, and chunk into a linked surface-plus-infrastructure-presentation pair. A separate ramp-only operation emits exactly one surface proxy and rejects non-ramp use, preventing fake render records. Rotated footprint tests prove the surface polygon is derived from normalized world axes while presentation preserves the exact realized matrix. No AssetDatabase lookup, hierarchy scan, realization, RNG consumption, scene/SubScene mutation, runtime code, or production ownership changed. Progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 pre-realization infrastructure asset metadata | Focused production-asset EditMode `/private/tmp/warline-dense-city-infrastructure-placement-record-builder.xml` 2/2; assembly boundaries `/private/tmp/warline-dense-city-infrastructure-placement-record-builder-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-infrastructure-placement-record-builder-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generator road emission remains open | Added an editor-only placement-record builder that extracts persistent source prefab GUID/local-id and resolved persistent material GUIDs before realization, then delegates to the pure infrastructure factory. Production tests verify the asphalt road prefab records its own persistent identity/materials and a natural ground patch records the explicit checked-in ground-variation material rather than inspecting the generated instance after material replacement. The same boundary supports bridge visuals and surface-only ramps with an explicit source identity. No generated hierarchy scan, name inference, transient material identity, RNG consumption, realization, scene/SubScene mutation, runtime code, or production ownership changed. Progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 bounded infrastructure sequence context | Focused EditMode `/private/tmp/warline-dense-city-infrastructure-context.xml` 4/4; assembly boundaries `/private/tmp/warline-dense-city-infrastructure-context-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-infrastructure-context-ecs.log` 11/11; Unity compile logs; `git diff --check` | Passed; generator road emission remains open | Extended the disposable generation context with one explicit per-district infrastructure identity stream. Visualized placements reserve two sequence values and surface-only placements reserve one; rejected attempts consume their reserved range while rollback removes every record, so later identities cannot depend on realization acceptance. The stream is instance-owned, bounded, cleared on disposal, and separate from building/attachment identity streams. No static state, hierarchy/asset discovery, inferred district, realization-order change, scene/SubScene mutation, runtime code, or production ownership changed. Progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 transactional road-tile record integration | Full in-memory generation `/private/tmp/warline-dense-city-road-record-integration.log`; two-build deterministic fixture `/private/tmp/warline-dense-city-road-record-determinism.xml` 1/1; assembly boundaries `/private/tmp/warline-dense-city-road-record-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-road-record-ecs.log` 11/11; accepted-scene byte-hash guard; `git diff --check` | Passed; terrain-patch, bridge, shoulder, and ramp emission remain open | Moved the bounded generation transaction context before road generation and wired every existing road tile through pre-realization semantic commitment. Persistent metadata is cached per source prefab; each tile emits one `Road` surface and one `Infrastructure` presentation from its exact planned world matrix, cell footprint, movement mask, layer, and chunk before instantiation. Realization preserves the existing dictionary iteration, prefab selection, placement, elevation, scale, naming, hierarchy, and RNG behavior, validates every matrix element against the committed presentation, and destroys partial visuals on exceptions. Full generation retained 5,909 buildings and 2,781 owned attachments while adding exactly 4,650 road surfaces/presentations: totals moved from 11,818/14,599 to 16,468/19,249. The accepted scene hash remained unchanged; no SubScene, Addressables, runtime system, or production ownership changed. Progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 transactional road terrain-patch integration | Full in-memory generation `/private/tmp/warline-dense-city-road-patch-integration.log`; two-build deterministic fixture `/private/tmp/warline-dense-city-road-patch-determinism.xml` 1/1; assembly boundaries `/private/tmp/warline-dense-city-road-patch-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-dense-city-road-patch-ecs.log` 11/11; accepted-scene byte-hash guard; `git diff --check` | Passed; bridge, shoulder, and ramp emission remain open | Split natural ground patch creation into deterministic plan and realization operations. The plan resolves the existing hash-selected persistent prefab, persistent ground-variation material, exact y-axis rotation, nonuniform scale, and vertical renderer-bound alignment before instantiation; the existing helper now delegates to the same operations for all non-road callers. Every accepted road patch commits one `Terrain` surface and one `Infrastructure` presentation before realization, validates exact matrix parity, and removes records plus partial visuals on failure. Full generation added exactly 4,650 patch surfaces/presentations, bringing totals to 21,118 surfaces and 23,899 presentations while retaining 5,909 buildings and 2,781 owned attachments. Visual selection/hash inputs, hierarchy, iteration/RNG order, accepted-scene bytes, SubScenes, Addressables, runtime systems, and production ownership remain unchanged. Progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 transactional bridge and approach-ramp integration | Focused bridge transaction `/private/tmp/dense-city-bridge-transaction-final.xml` 9/9; bridge factory `/private/tmp/dense-city-bridge-factory-final.xml` 5/5; shared sequence context `/private/tmp/dense-city-bridge-context-final.xml` 5/5; full in-memory generation `/private/tmp/dense-city-bridge-integration-final-2.log`; two-build deterministic fixture `/private/tmp/dense-city-bridge-determinism-final.xml` 1/1; assembly boundaries `/private/tmp/dense-city-bridge-architecture-final.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-bridge-ecs-final.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; generated editor/test project compile 0 errors; `git diff --check` | Passed; shoulder emission remains open | Added one atomic bridge group containing a typed bridge surface, one infrastructure presentation, and two explicit surface-only approach ramps. Preflight and rollback cover all four records, while the generation context reserves one contiguous four-slot district-local infrastructure sequence range even after rejection. Bridge planning now resolves persistent prefab/material metadata, exact source-local renderer bounds including inactive prefab assets, root transform, semantic deck footprint, approach footprints, elevation, movement mask, layer, and chunks before realization. Realization preserves the accepted bridge placement and validates exact matrix plus transformed renderer-center parity, destroying partial visuals on failure. Full generation emitted 14 bridge presentations, 14 bridge surfaces, and 28 ramp surfaces, moving totals from 21,118/23,899 to 21,160 surfaces and 23,913 presentations while retaining 5,909 buildings and 2,781 owned attachments. No scene, SubScene, Addressables, runtime system, RNG order, production ownership, or Android build changed. The combined road/shoulder/terrain-patch/bridge/ramp checklist item remains incomplete only for explicit shoulder records, so progress remains 42/148 (28%). |
| 2026-07-22 | Phase 2 transactional authored road-shoulder integration | Focused road factory/transaction/context/production-prefab planner `/private/tmp/dense-city-road-shoulder-focused-final-3.xml` 29/29; full in-memory generation `/private/tmp/dense-city-road-shoulder-integration.log`; two-build deterministic fixture `/private/tmp/dense-city-road-shoulder-determinism.xml` 1/1; assembly boundaries `/private/tmp/dense-city-road-shoulder-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-road-shoulder-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined road semantic-output item complete | Added one variable-sized atomic road group containing the typed road surface, one prefab-root infrastructure presentation, and zero or more typed terrain shoulder surfaces. The planner consumes the existing pre-realization authored `RoadFootprintKind.Sidewalk` bounds, sorts them geometrically for stable sequence allocation, records the exact transformed upper walkable elevation and footprint, and emits no shoulder for the dirt-only road family. Rejected or exceptional realization removes the road, presentation, and every shoulder together; declared-count mismatch fails before advancing the district-local sequence. Full generation emitted 11,612 explicit shoulder surfaces, moving totals from 21,160/23,913 to 32,772 surfaces and 23,913 presentations while retaining 5,909 buildings and 2,781 owned attachments. Presentation count is unchanged because the existing prefab-root presentation owns its baked road and sidewalk child renderers, avoiding duplicate visuals. Road realization, hierarchy, prefab/material sharing, RNG order, scenes/SubScenes, Addressables, runtime systems, production ownership, and Android builds were unchanged. Progress is 43/148 (29%). |
| 2026-07-22 | Phase 2 canal-water semantic transaction foundation | Focused canal/generation/building/infrastructure/context EditMode `/private/tmp/dense-city-canal-water-foundation.xml` 33/33; assembly boundaries `/private/tmp/dense-city-canal-water-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-canal-water-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; generator realization integration remains open | Added one atomic canal-water group with a non-traversable blocker surface plus separate canal-bed and canal-water infrastructure presentations. The group preflights capacity, category, and all three identities before mutation; rejected or exceptional realization removes the complete group, and the generation context reserves one contiguous three-slot infrastructure sequence range. Blocker records now require `MovementMask.None`, while every traversable surface still requires a nonzero movement mask; generated building blockers were corrected to the same invariant without changing stable identity or visual realization. The foundation has no asset lookup, transient primitive identity, scene mutation, generator integration, RNG change, SubScene/Addressables/runtime change, or Android validation. Progress remains 43/148 (29%) until canal water, bank/park terrain, vegetation, and light/prop records are integrated and parity-validated. |
| 2026-07-22 | Phase 2 transactional canal-water generator integration | Full in-memory map generation `/private/tmp/dense-city-canal-water-integration.log`; focused canal plus two-build deterministic fixture `/private/tmp/dense-city-canal-water-final-tests.xml` 5/5; assembly boundaries `/private/tmp/dense-city-canal-water-integration-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-canal-water-integration-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; bank/park terrain, vegetation, and light/prop integration remain open | Replaced transient cube water/bed realization with two instances of one persistent checked-in water-plane prefab while preserving separate checked-in bed and water materials. Every tile now plans persistent asset/material identities and exact root world transforms before realization, commits one non-ground blocker plus separate bed/water presentations atomically, removes partial visuals on failure, validates both realized matrices, and fails the full build unless tile/exclusion/bed/water counts match exactly. Full generation emitted 304 exclusions and 608 presentations, moving totals from 32,772/23,913 to 33,076 surfaces and 24,521 presentations while retaining 5,909 buildings, 2,781 owned attachments, and 11,612 road shoulders. No source scene, SubScene, Addressables/runtime ownership, RNG order, production mode, or Android build changed. Progress remains 43/148 (29%) until the complete canal/park semantic-output checklist item is accepted. |
| 2026-07-22 | Phase 2 bank/park terrain-visual transaction foundation | Focused terrain/canal/infrastructure/context EditMode `/private/tmp/dense-city-terrain-visual-foundation.xml` 28/28; assembly boundaries `/private/tmp/dense-city-terrain-visual-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-terrain-visual-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; bank/park generator integration remains open | Added one bounded variable-size atomic group containing a traversable terrain surface and 1-16 explicit persistent infrastructure presentations. Capacity, category, source identity, and duplicate identity checks complete before mutation; rejection or realization failure removes the entire group. The generation context reserves one contiguous shared infrastructure sequence range and rejects declared-presentation-count mismatch before advancing that sequence. This supports the existing three-patch organic canal banks and five-patch pocket parks without collapsing distinct source assets into a fake presentation. No generator, hierarchy, asset lookup, visual realization, RNG, scene/SubScene, Addressables/runtime ownership, or Android build changed. Progress remains 43/148 (29%) until bank/park integration and remaining canal details are accepted. |
| 2026-07-22 | Phase 2 transactional canal-bank generator integration | Full in-memory map generation and exact semantic parity `/private/tmp/dense-city-canal-bank-final-integration.log`; terrain transaction plus two-build deterministic fixture `/private/tmp/dense-city-canal-bank-determinism.xml` 5/5; assembly boundaries `/private/tmp/dense-city-canal-bank-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-canal-bank-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; pocket-park terrain, vegetation, and light/prop integration remain open | Replaced every transient cylinder bank base with a second planned instance of the same selected persistent round-ground prefab at the existing 104% footprint and lower elevation, retaining the organic three-patch layering and green material override. Each bank segment now plans one non-buildable traversable terrain surface plus six explicit persistent presentations before realization, commits them atomically, removes partial visuals on failure, and validates every realized matrix. Full generation preserved 1,104 bank patches and emitted exactly 368 terrain surfaces, 1,104 base presentations, and 1,104 top presentations, moving totals from 33,076/24,521 to 33,444 surfaces and 26,729 presentations. The build fails closed on any patch/base/top/terrain count mismatch. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 43/148 (29%) until the complete canal/park semantic-output item is accepted. |
| 2026-07-22 | Phase 2 transactional canal pocket-park generator integration | Full in-memory map generation and exact semantic parity `/private/tmp/dense-city-canal-park-integration.log`; terrain transaction plus two-build deterministic fixture `/private/tmp/dense-city-canal-park-determinism.xml` 5/5; assembly boundaries `/private/tmp/dense-city-canal-park-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-canal-park-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; vegetation and light/prop integration remain open | Replaced the pocket parks' transient cylinder bases and unrecorded round-ground instances with ten preplanned persistent prefab presentations under one traversable, non-buildable terrain record per park. The existing five-patch organic layout, hash-selected source prefabs, green material override, elevations, hierarchy names, reserved cells, and detail placement remain unchanged. Each park commits its terrain, five base presentations, and five top presentations atomically before realization, removes the complete park hierarchy on failure, validates every realized matrix, and fails the full build on any park/terrain/base/top count mismatch. Full generation preserved 2 parks and emitted exactly 2 terrain surfaces plus 20 presentations, moving totals from 33,444/26,729 to 33,446 surfaces and 26,749 presentations while retaining 5,909 buildings and 2,781 owned attachments. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 43/148 (29%) until vegetation and light/prop records close the combined canal/park semantic-output item. |
| 2026-07-22 | Phase 2 render-only presentation transaction foundation | Focused render-only/canal/terrain EditMode `/private/tmp/dense-city-render-only-foundation-2.xml` 17/17; assembly boundaries `/private/tmp/dense-city-render-only-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-render-only-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; canal vegetation and light/prop generator integration remain open | Added a single-record transactional ownership path for independent render-only presentations without inventing gameplay terrain or misusing building attachments. The factory and record set accept only `Infrastructure`, `Vegetation`, `Prop`, or `Horizon`; gameplay-building and building-attachment categories are rejected. The transaction commits before realization and removes the presentation after rejection or exception. The generation context allocates the record from the existing district-local infrastructure sequence so stable identities remain collision-free across roads, surfaces, canal groups, vegetation, and props. No generator call site, hierarchy, asset lookup, visual realization, RNG, source scene, SubScene, Addressables/runtime ownership, or Android build changed. Progress remains 43/148 (29%) until canal detail integration is accepted. |
| 2026-07-22 | Phase 2 transactional canal vegetation and light integration | Full in-memory map generation and exact semantic parity `/private/tmp/dense-city-canal-detail-integration.log`; render-only transaction plus terrain and two-build deterministic fixture `/private/tmp/dense-city-canal-detail-determinism.xml` 14/14; assembly boundaries `/private/tmp/dense-city-canal-detail-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-canal-detail-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined canal/park semantic-output item complete | Preloaded persistent prefab/material metadata for the existing canal tree, bush, and streetlight assets, then routed every bank-side and pocket-park detail through the render-only transaction. Grounding is now planned from persistent prefab-local renderer bounds before realization while preserving the existing selected prefab, yaw, uniform scale, support elevation, object name, and parent hierarchy. Trees and bushes emit `Vegetation` records; streetlights emit `Infrastructure` records. Every realized root validates exact matrix parity and is removed with its record after rejection or exception. Full generation emitted exactly 85 tree, 81 bush, and 61 light presentations, moving presentation totals from 26,749 to 26,976 while surfaces remained 33,446 and buildings/attachments remained 5,909/2,781. Full-build parity now fails on any mismatch across water, bank, park, vegetation, or light counts. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. The combined canal/park item is complete; progress is 44/148 (30%). |
| 2026-07-22 | Phase 2 civic/market semantic ownership integration | Focused building transaction/context/deterministic fixture `/private/tmp/dense-city-civic-semantic-focused.xml` 12/12; full in-memory generation and exact semantic parity `/private/tmp/dense-city-civic-semantic-integration-final.log`; assembly boundaries `/private/tmp/dense-city-civic-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-civic-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; civic/market semantic-output item complete | Preserved the existing atomic five-record gameplay-building transaction and added source-time civic identity for the hall and roadside bazaar buildings: building simulation, foundation terrain, blocker, intact presentation, and destroyed presentation are labeled together without hierarchy/name inference. Explicit civic-road cells now carry civic road and terrain-patch identity through the existing road transaction while retaining shoulder ownership and the same road prefab, transform, iteration, and realization path. Full generation emitted exactly 24 civic building groups and 78 civic roads; generation fails closed unless all five building records match and every civic road/terrain record has its paired presentation. Generic buildings and roads retain their existing identities. No RNG order, visual output, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress is 45/148 (30%). |
| 2026-07-22 | Phase 2 transactional horizon-mountain semantic integration | Deterministic fixture `/private/tmp/dense-city-horizon-semantic-focused.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-horizon-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-horizon-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-horizon-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item remains open | Replaced temporary-instance planning with deterministic prefab-local renderer-bound planning for the existing horizon perimeter. Scale, yaw, outward retry offsets, protected-area rejection, grounding, names, hierarchy, collider stripping, and visual output remain unchanged. Every accepted mountain now commits one persistent `Horizon` presentation record before realization and validates exact realized world-matrix parity; rejected protected-area attempts emit nothing. Full generation emitted exactly 53 horizon presentations, moving presentation totals from 26,976 to 27,029 while surfaces/buildings remained 33,446/5,909. Generation fails closed on realized/semantic count mismatch. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. The broader courtyard/wall/rock/mountain/vegetation/rooftop/power/street-detail checkbox remains open, so progress remains 45/148 (30%). |
| 2026-07-22 | Phase 2 transactional boulevard and sidewalk-detail semantic integration | Deterministic fixture `/private/tmp/dense-city-street-detail-semantic-focused.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-street-detail-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-street-detail-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-street-detail-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item remains open | Cached persistent prefab/material metadata for boulevard median trees and shared streetlights, then routed the existing boulevard-tree, boulevard-light, and sidewalk-light placements through the render-only transaction. The existing sorted road-cell traversal, hashes, side resolution, scales, rotations, support elevations, reserved areas, names, and hierarchy are unchanged. Trees emit `Vegetation`; both light paths emit `Infrastructure`. Each record commits before realization, validates exact grounded matrix parity, and rolls back with a rejected or failed instance. Full generation emitted exactly 155 boulevard trees, 74 boulevard lights, and 482 sidewalk lights, moving presentations from 27,029 to 27,740 while surfaces/buildings remained 33,446/5,909. Generation fails closed on any semantic/realized count mismatch. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 45/148 (30%) until all categories in the combined detail item are accepted. |
| 2026-07-22 | Phase 2 transactional free-ground landscaping semantic integration | Deterministic fixture `/private/tmp/dense-city-landscaping-semantic-focused.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-landscaping-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-landscaping-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-landscaping-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item remains open | Added a shared transactional free-detail path that computes persistent prefab-local renderer bounds and the exact planned grounded world matrix before realization, performs the existing authored-core, road, building, and reserved-area rejection without creating a temporary object, then commits and realizes atomically. Main-street bushes and free-ground grass now use this path as `Vegetation` records while preserving the existing grid traversal, hashes, placement chances, side attempts, rotations, scales, occupied-area reservations, names, and hierarchy. Full generation emitted exactly 2,049 grass and 262 main-street-bush presentations, moving presentations from 27,740 to 30,051 while surfaces/buildings remained 33,446/5,909. Generation fails closed on semantic/realized count mismatch. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 45/148 (30%) until all categories in the combined detail item are accepted. |
| 2026-07-22 | Phase 2 transactional roadside power-network semantic integration | Deterministic fixture `/private/tmp/dense-city-power-semantic-focused.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-power-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-power-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-power-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item remains open | Cached persistent pole/line asset metadata and prefab-local renderer bounds in the existing utility result shared by all sorted road corridors. Pole grounding and wire height are now derived from the exact planned transformed bounds before realization; each line derives its position, rotation, and z scale from the planned adjacent pole endpoints and persistent source length. Both paths commit `Infrastructure` records before instantiation, validate exact realized matrix parity, and roll back failed instances/records together. Existing road-run discovery, side resolution, spacing, chain flushing, reserved areas, names, hierarchy, and visual output are unchanged. Full generation emitted exactly 1,390 poles and 1,198 connecting lines, moving presentations from 30,051 to 32,639 while surfaces/buildings remained 33,446/5,909. Generation fails closed on semantic/realized count mismatch. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 45/148 (30%) until all categories in the combined detail item are accepted. |
| 2026-07-22 | Phase 2 atomic visual-blocker transaction foundation | Focused generation-context EditMode `/private/tmp/dense-city-visual-blocker-foundation.xml` 8/8; assembly boundaries `/private/tmp/dense-city-visual-blocker-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-visual-blocker-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; courtyard-wall generator integration remains open | Added a dedicated bounded record group for a coarse non-traversable blocker plus one render-only `Infrastructure` presentation. The new factory creates deterministic paired identities and an oriented rectangular blocker footprint from explicit size and transform inputs. Record-set capacity/category/kind/identity checks complete before mutation, rejection or realization failure rolls back both records, and the shared district infrastructure sequence advances by exactly two. Existing terrain/road infrastructure continues to reject blocker surfaces, preserving that semantic invariant. No generator realization, visual output, RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 45/148 (30%) until courtyard walls and the remaining combined detail categories are integrated. |
| 2026-07-22 | Phase 2 transactional courtyard semantic integration | Deterministic fixture `/private/tmp/dense-city-courtyard-semantic-compile.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-courtyard-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-courtyard-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-courtyard-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item remains open | Cached persistent asset/material metadata for the existing courtyard wall, pillar, well, and bush prefabs and planned every transform from prefab-local renderer bounds before realization. Each wall now atomically commits one oriented coarse non-traversable blocker and one render-only `Infrastructure` presentation; pillars emit `Infrastructure`, wells emit `Prop`, and bushes emit `Vegetation` records through the render-only transaction. Existing courtyard selection, side attempts, dimensions, gate segmentation, hashes, rotations, scales, grounding elevations, names, hierarchy, collider stripping, ground-detail cleanup, and reservations remain unchanged. Full generation emitted exactly 760 wall blockers and 760 wall presentations, 380 pillars, 152 wells, and 680 bushes, moving totals from 33,446/32,639 to 34,206 surfaces and 34,611 presentations. Generation fails closed on any realized/semantic count mismatch. No RNG order, source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 45/148 (30%) until rooftop, general prop/tree/rock, and remaining combined detail categories are accepted. |
| 2026-07-22 | Phase 2 transactional street-prop, tree, and urban-rock semantic integration | Deterministic fixture `/private/tmp/dense-city-natural-detail-semantic-compile.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-natural-detail-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-natural-detail-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-natural-detail-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item remains open | Cached persistent metadata for the existing street-prop, dense-tree, and urban-rock arrays. Exact prefab-local renderer bounds now drive pre-realization road/building/reserved-area rejection. Street props emit `Prop` records, trees emit `Vegetation`, and each playable urban rock atomically emits one coarse non-traversable blocker plus one `Infrastructure` presentation; realized transforms validate against the planned matrices. Existing grid/cluster traversal, hashes, placement chances, positions, rotations, scales, names, hierarchy, overlap rejection, and collider stripping remain unchanged. Full generation emitted exactly 4,812 street-prop presentations, 1,918 tree presentations, and 25 rock blocker/presentation pairs, moving totals from 34,206/34,611 to 34,231 surfaces and 41,366 presentations. Generation fails closed on any realized/semantic count mismatch. Rooftop caps, tanks, utilities, and shop-wall props were re-audited and are already covered by the validated building-attachment transaction. Civic fountains and open-ground terrain visuals remain direct realization paths, so the combined item is intentionally not closed. Progress remains 45/148 (30%). |
| 2026-07-22 | Phase 2 transactional civic-fountain semantic correction | Deterministic fixture `/private/tmp/dense-city-civic-fountain-semantic-focused.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-civic-fountain-semantic-integration.log`; assembly boundaries `/private/tmp/dense-city-civic-fountain-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-civic-fountain-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; open-ground terrain visuals remain before combined item closure | Re-audited the previously accepted civic/market slice and found its two standalone plaza fountains were still direct realizations while cloth covers and umbrellas were already hall-owned attachments. Both fountain prefab/material identities are now extracted before placement and each fountain commits one render-only `Prop` presentation before exact grounded realization. Existing positions, support elevation, rotation, scale, names, parent hierarchy, collider stripping, and plaza random ordering remain unchanged. Full generation emits exactly two civic-fountain presentations and fails closed otherwise, moving presentations from 41,366 to 41,368 while surfaces remain 34,231. No source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress remains 45/148 (30%). |
| 2026-07-22 | Phase 2 transactional open-ground terrain integration and combined-detail closure | Deterministic fixture `/private/tmp/dense-city-open-ground-semantic-focused.xml` 1/1; full in-memory generation and exact semantic parity `/private/tmp/dense-city-open-ground-semantic-integration-2.log`; assembly boundaries `/private/tmp/dense-city-open-ground-semantic-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-open-ground-semantic-ecs.log` 11/11; accepted-scene SHA-256 before/after `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`; Unity compile 0 errors; `git diff --check` | Passed; combined courtyard/detail semantic-output item complete | Delayed open-ground realization until the existing courtyard and boulevard exclusion areas are known, while snapshotting pre-urban building footprints at the original generation point so later roof/wall attachments cannot alter placement. Every surviving round-ground patch now atomically commits one traversable terrain surface and one persistent-material `Infrastructure` presentation before realization, validates exact world-matrix parity, and rolls back as a unit. The prior final visual count is preserved exactly: 3,729 original candidates minus 183 courtyard and one boulevard overlap equals 3,545 surviving patches. Full semantic totals are 5,909 buildings, 37,776 surfaces, and 44,913 presentations. The combined category audit also confirms rooftop caps/tanks/utilities/shop-wall props are building-owned attachments; courtyard walls/props, mountains, street details, landscaping, power, street props, trees, rocks, and civic fountains now have explicit transactional records. No source scene, SubScene, Addressables/runtime ownership, production mode, or Android build changed. Progress is 46/148 (31%). |
| 2026-07-22 | Phase 2 typed presentation-hierarchy routing foundation | Focused EditMode `/private/tmp/dense-city-presentation-hierarchy-context-3.xml` 3/3; assembly boundaries `/private/tmp/dense-city-presentation-hierarchy-context-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-presentation-hierarchy-context-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; generator call-site routing remains open | Added a fail-closed editor-only hierarchy context over the marked `EntityPresentationSource` root. It resolves gameplay buildings to `GameplayBuildings/Buildings` or `GameplayBuildings/CivicAndMarket` from the explicit generated building role, and resolves Infrastructure, Vegetation, Prop, and Horizon records to their exact `RenderOnly` buckets. Every required path must exist exactly once with identity transforms. Building attachments are deliberately rejected from independent routing and must remain beneath their declared intact/destroyed building visual-state owner. This adds no post-generation inference, scene mutation, runtime owner, static service, Addressables change, RNG change, or production cutover. The full creation-time placement checkbox remains open until all realization call sites consume this context. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 semantic render-only realization transaction | Focused hierarchy/transaction EditMode `/private/tmp/dense-city-presentation-hierarchy-transactions-3.xml` 8/8; assembly boundaries `/private/tmp/dense-city-presentation-hierarchy-transactions-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-presentation-hierarchy-transactions-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; generator call-site routing remains open | Extended the bounded generation transaction context with a hierarchy-aware render-only realization overload. The committed presentation category selects its exact semantic parent before the callback creates an object; the returned root must be a direct child in the same scene and its world matrix must match the committed record within `0.0001`. Wrong-parent, nested-owner, null, rejected, exceptional, or transform-drift realization rolls back the semantic record and destroys the partial returned object. Existing legacy transaction overloads remain unchanged for the accepted editor-preview generator until all categories can switch atomically, avoiding a callable mixed-hierarchy candidate path. No source/candidate scene, runtime owner, RNG order, Addressables ownership, production mode, or Android build changed. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 record-driven render-only candidate realizer | Focused EditMode `/private/tmp/dense-city-render-only-realizer.xml` 2/2; assembly boundaries `/private/tmp/dense-city-render-only-realizer-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-render-only-realizer-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; full record-set replay remains open | Added an editor-only realizer for one explicit render-only presentation record. It resolves and verifies the persistent prefab GUID plus local file id, instantiates directly beneath the category-selected semantic parent, decomposes only finite non-sheared TRS matrices, applies the exact recorded world transform, verifies all 16 matrix elements within `0.0001`, verifies the sorted persistent material GUID set, and retains the prefab connection so repeated placements share source mesh/material assets. Unsupported mesh-only records, missing assets, source mismatch, material drift, shear, hierarchy mismatch, or transform drift fail closed and remove the partial instance. Tests prove exact prefab/material/transform/parent parity and mismatch cleanup. No production scene, candidate asset, runtime owner, Addressables ownership, RNG order, production mode, or Android build changed. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 transactional render-only record-set replay | Focused `/private/tmp/dense-city-render-only-replay-focused.log` 2/2; assembly boundaries `/private/tmp/dense-city-render-only-replay-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-render-only-replay-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; building/attachment replay and generator integration remain open | Added a bounded editor-only replay transaction over the sealed, stable-identity-sorted presentation records. It realizes only independent render-only categories through the record-driven realizer, skips building-owned presentation records for their separate ownership transaction, preserves deterministic record order, and destroys every object created by the replay in reverse order if any later source, material, transform, or hierarchy validation fails. Tests prove cross-category stable replay into exact semantic parents and complete rollback after a later material mismatch. The tracker summary was corrected to the measured 46 checked items out of 148; no checklist item was newly closed by this dependency step. No accepted/candidate scene, source/static rollback artifact, runtime owner, Addressables ownership, RNG order, production mode, or Android build changed. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 generated-building stable identity foundation | Focused `/private/tmp/dense-city-generated-building-identity-focused-2.log` 4/4; assembly boundaries `/private/tmp/dense-city-generated-building-identity-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-generated-building-identity-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; building visual-root realization remains open | Extended the existing `OperationMapBuildingAuthoring` and unmanaged building identity/state components with one canonical stable id while preserving the exact authored `SourceGlobalObjectId` field used by accepted-source migration and parity joins. Existing buildings automatically use their valid Unity GlobalObjectId as the stable id; generated buildings instead require a mutually exclusive `densecity.<64 lowercase SHA-256>` id derived from the complete deterministic generation record key. Missing, malformed, or mixed authored/generated identities fail closed. Dense-city readiness now rejects duplicate canonical stable ids rather than treating an intentionally empty generated source GlobalObjectId as ownership. Tests prove authored fallback, generated acceptance, mixed rejection, deterministic hashing, bounded length, and lowercase format. No scene/prefab serialization, source/candidate scene, runtime system, Addressables ownership, RNG order, production mode, or Android build changed. No checklist item closes until generated building visuals and attachments realize beneath this authoring owner. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 generated-building authoring configuration boundary | Focused `/private/tmp/dense-city-generated-building-configure-focused.log` 5/5; assembly boundaries `/private/tmp/dense-city-generated-building-configure-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-generated-building-configure-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; building visual-root realization remains open | Added one `UNITY_EDITOR`-only configuration method to the existing `OperationMapBuildingAuthoring`. The future record realizer can now assign operation-map id, generated stable id, placement index, faction, origin cell, building definition, blocker policy, and immediate intact/destroyed visual roots directly through the owning authoring contract instead of reflection, a duplicate authoring type, or a managed runtime bridge. The method clears authored `SourceGlobalObjectId` ownership and leaves all acceptance to the same fail-closed `TryValidate` path. Tests prove complete field assignment and valid intact/destroyed ownership. No source/candidate scene, prefab, runtime system, ECS hot path, Addressables ownership, RNG order, production mode, or Android build changed. No checklist item closes until the record-driven building and attachment realization transaction consumes this boundary. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 generated-building gameplay record ownership | Focused record/authoring EditMode `/private/tmp/warline-generated-building-record-tests.xml` 19/19; generated authoring entry point `/private/tmp/warline-generated-building-gameplay-values.log` 6/6; assembly boundaries `/private/tmp/warline-generated-building-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/warline-generated-building-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; building visual-root realization remains open | Preserved deterministic grid origin and footprint cells from `DenseCityBuildingPlacementPlan` through the immutable placement request, building record input, and committed building bake record instead of dropping them before authoring. Generated `OperationMapBuildingAuthoring` now requires and exposes the committed footprint and maximum health as mutually exclusive generated gameplay values; its Baker consumes those values for grid, blocker, footprint, and health components. Existing authored buildings remain definition-driven and reject stale generated overrides. Tests prove record propagation, editor configuration, generated acceptance, malformed override rejection, authored fallback, and unchanged factory/material behavior. No source/candidate scene, prefab, runtime system, hot-loop work, Addressables ownership, RNG order, production mode, or Android build changed. No checklist item closes until record-driven building visual roots and attachments realize atomically beneath this owner. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 explicit generated-building role and definition identity | Focused record/transaction EditMode `/private/tmp/dense-city-building-role-definition.xml` 28/28; assembly boundaries `/private/tmp/dense-city-building-role-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-building-role-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; building visual-root realization remains open | Added explicit `GeneratedCityBuildingRole` and persistent building-definition config GUID to every immutable generated-building request/input/record. A bounded editor-only definition library resolves House/Other, Shop, and Civic roles to the existing House, Shop, and Hall config assets once per generation context; missing assets fail before placement. The future replay can now select `GameplayBuildings/Buildings` versus `CivicAndMarket` and configure the existing building authoring from record data without hierarchy-name, prefab-name, or identity-kind inference. Tests prove role/GUID propagation and persistent asset resolution while preserving semantic record counts and generator RNG order. No source/candidate scene, runtime system, Addressables ownership, production mode, or Android build changed. No checklist item closes until the record-driven building and attachment replay consumes this metadata. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 record-driven generated-building presentation realizer | Focused building/render-only EditMode `/private/tmp/dense-city-building-realizer.xml` 4/4; assembly boundaries `/private/tmp/dense-city-building-realizer-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-building-realizer-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; sealed record-set building/attachment replay remains open | Added an editor-only realizer for one committed generated-building group. It creates one owner directly beneath the role-selected `GameplayBuildings` parent, applies the exact recorded world matrix, resolves the persistent definition config by recorded GUID, instantiates prefab-connected intact and destroyed visual roots as immediate children, validates source/material/world-matrix parity, and configures the existing `OperationMapBuildingAuthoring` with stable id, faction, origin, footprint, and health. A late mismatch destroys the complete partial owner hierarchy. The shared matrix helper now correctly decomposes parent-relative TRS while retaining world-matrix parity for existing render-only replay. No source/candidate scene, runtime owner, Addressables ownership, generator integration, production mode, or Android build changed. No checklist item closes until the sealed building/attachment replay and generator routing use this realizer transactionally. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 transactional generated-building and attachment replay | Focused building/attachment replay EditMode `/private/tmp/dense-city-building-replay.xml` 6/6; assembly boundaries `/private/tmp/dense-city-building-replay-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-building-replay-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; candidate generator integration remains open | Added a sealed-record-set replay transaction for generated gameplay buildings. It indexes strictly stable-sorted presentation records, resolves each building's exact linked intact/destroyed records, realizes one authoring owner through the existing building realizer, and instantiates every building-owned attachment directly beneath the declared intact or destroyed visual-state root. Attachment source, material, parent, scene, and world-matrix parity are validated. Any missing/mismatched building record or later attachment failure destroys all previously realized building owners in reverse order, including their visual descendants. No attachment is routed through global render-only ownership. No source/candidate scene, runtime owner, generator integration, Addressables ownership, production mode, or Android build changed. The hierarchy checklist remains open until generation/candidate replacement invokes both building and independent render-only replays atomically. Progress remains 46/148 (31%). |
| 2026-07-22 | Phase 2 atomic complete-presentation replay boundary | Focused `/private/tmp/dense-city-presentation-replay-focused.log` 2/2; assembly boundaries `/private/tmp/dense-city-presentation-replay-architecture.log` 31/31; ECS/Burst guardrails `/private/tmp/dense-city-presentation-replay-ecs.log` 11/11; Unity compile 0 errors; `git diff --check` | Passed; candidate generator integration remains open | Added one editor-only coordinator over the existing generated-building/attachment and independent render-only replay transactions. A sealed record set now has a single all-or-nothing realization boundary: buildings realize first under their explicit gameplay owners, render-only records then realize under their typed semantic parents, and any later render-only mismatch removes both its own partial output and every previously realized building owner in reverse order. Tests prove successful mixed-category replay and cross-transaction rollback after a late material-identity failure. No accepted/candidate scene, generator call site, runtime owner, Addressables ownership, production mode, or Android build changed. The hierarchy checklist remains open until the candidate replacement path consumes this combined transaction. Progress remains 46/148 (31%). |

## 25. Completion Rule

This tracker is complete only when:

- all current existing map visual owners have accepted stable-identity migration dispositions with no mixed/unresolved row;
- existing military, handmade, building, vehicle, road, bridge, terrain, mountain, vegetation, prop, infrastructure, and horizon visuals bake to the protected map SubScene;
- the city regenerates under one disposable ownership set with exactly one accepted root in each required source scene;
- authored military, handmade, infrastructure, placement, and override behavior/transform identity remains semantically and visually preserved through migration;
- every permanent map visual's candidate authoring, baked ECS, and Addressables-loaded runtime matrix and transformed renderer bounds match the accepted Editor source manifest within the Section 12.0A tolerances, with no runtime placement system applying a second offset;
- production map content has zero colliders/Rigidbodies;
- ECS surface/blocker data is generated from simplified deterministic records/proxies;
- every damageable existing/generated building exists at runtime only as ECS simulation/render entities with intact/destroyed entity visuals;
- every building-attached roof/interior/shop/tent visual has one explicit building/state owner and transitions atomically with that building, leaving no intact or orphan prop after destruction;
- every other existing/generated visual exists at runtime only as an Entities Graphics render entity sharing source art assets;
- current-map production Addressables/build/runtime ownership contains zero static manifest/chunk scene entries unless a measured named exception was approved in this tracker;
- the 514-scene static package is frozen for rollback during migration and retired/deleted through the accepted cleanup only after parity acceptance;
- operation-map and SubScene source GameObject hierarchies are absent from the runtime package;
- Bake All is deterministic, transactional, no-op on a second run, and fail-closed;
- Addressables load/readiness/unload behavior passes twice consecutively with no map-visual camera streaming or static handles;
- Editor acceptance precedes Android acceptance;
- Android size, memory, frame, GC, visual, navigation, and lifecycle gates pass;
- final evidence and rollback instructions are recorded.
