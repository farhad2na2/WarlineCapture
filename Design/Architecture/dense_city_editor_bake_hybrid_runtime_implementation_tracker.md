# Operation Map ECS Presentation And Dense City Editor Bake Implementation Tracker

Date: 2026-07-21
Status: Implementation started; Phase 0 baseline and contracts in progress; previously generated dense city cleared by project owner
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

Current progress: **1 / 144 (1%)**

| Phase | Status |
|---|---|
| Phase 0: Baseline and contracts | In progress |
| Phase 0A: Existing map ECS presentation migration | Not started |
| Phase 1: Generated ownership authoring | Not started |
| Phase 2: Generator semantic output | Not started |
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
- [ ] Record current accepted surface payload, static manifest, chunk count, source count, integrity ledger, minimap, runtime binding, and Addressables Build Layout identities.
- [ ] Capture current Android APK/installed size, runtime memory, draw calls, frame timings, GC, load time, and unload time on the target device.
- [ ] Confirm the authoring scene is excluded from current Addressables/runtime build ownership.
- [ ] Confirm existing military, handmade, runway, road, mountain, resource, placement, and override roots that generation must preserve.
- [ ] Approve generated hierarchy names and exact semantic ownership table from this tracker.
- [ ] Approve whether city content outside the current grid is presentation-only; default is presentation-only.
- [ ] Add focused test fixtures that can build a small deterministic city without opening or modifying the full map scene.

**Exit:** Regressions can be measured against a clean, cleared-city baseline.

### Phase 0A: Existing Map ECS Presentation Migration

- [ ] Inventory all `16,542` current static-presentation sources by repository-relative scene/object identity, prefab GUID/local id, mesh/material identity, transform, components, and current static chunk ownership.
- [ ] Join all `451` building and `29` vehicle placement identities to their exact authored source objects and reject zero, multiple, reused, mixed, or unresolved matches.
- [ ] Classify every existing owner as gameplay building, gameplay vehicle, render-only entity, map metadata/proxy, approved managed boundary, or rejected/unresolved without name-based inference.
- [ ] Audit scripts, lights, animation, particles, cross-object references, and source suppression behavior; record an explicit ECS/baked-lighting/VFX/metadata disposition for each non-renderer dependency.
- [ ] Prove which current vehicle placements already produce ECS entities/render entities and identify only the missing conversion/duplication cleanup.
- [ ] Inventory every managed `RuntimeBuildingEntity`/GameObject dependency required by current authored buildings and define equivalent ECS component/buffer/system ownership before visual cutover.
- [ ] Inventory every roof, interior, shop, tent, sign, awning, lamp, furniture, and utility visual attached to each current damageable building; assign each stable identity to exactly one building and one intact/destroyed state without proximity or name inference.
- [ ] Add `OperationMapPresentationKind` and set the candidate definition to `EntityScene` only after migration validation succeeds.
- [ ] Extend `OperationMapCanonicalPresentationMode`, `OperationMapDefinition`, and content-reference validation for an entity-presented map with no required static manifest or runtime placement references.
- [ ] Add `OperationMapEntityPresentationRootAuthoring` and exact protected authored hierarchy roots in the existing map SubScene.
- [ ] Add deterministic `OperationMapEntityPresentationMigrationRecord` capture and `OperationMapEntityPresentationMigrationEditor` candidate transaction ownership.
- [ ] Move/copy reviewed existing render-only visual authoring into the candidate SubScene while preserving world transforms, prefab/source asset references, hierarchy semantics, and shared art identity.
- [ ] Convert all current authored building placements to `OperationMapBuildingAuthoring` ECS ownership with health, faction, footprint, targeting, production/interaction parity where applicable, and intact/destroyed entity visuals.
- [ ] Convert/verify all current authored vehicle placements through the existing unit/vehicle Baker path and eliminate duplicate source/static/runtime GameObject visuals.
- [ ] Remove prohibited physics components from migrated instances without mutating shared third-party prefab assets.
- [ ] Bake the candidate SubScene and validate expected gameplay/render-only/render-child entity counts, finite transforms/bounds, archetypes, and zero managed map visual components.
- [ ] Prove repeated meshes/materials/textures have one shared package owner and placements contribute only compact entity/transform/render-reference data.
- [ ] Capture fixed-camera visual parity for terrain, roads, bridges, buildings, vehicles, mountains, vegetation, props, infrastructure, lighting, and minimap before changing production ownership.
- [ ] Extend runtime binding/loading/readiness/teardown so `EntityScene` loads the map SubScene once, skips static-manifest/streamer binding, and unloads all map entities once.
- [ ] Extend `OperationMapSceneView` and startup validation so empty legacy placement roots/configs are accepted only in validated `EntityScene` mode and cannot trigger managed map visual spawning.
- [ ] Extend Bake All with fail-closed entity migration/readiness/bake/budget stages and rollback of source scenes, SubScene, definition, runtime binding, and Addressables layout.
- [ ] Build a candidate local Addressables layout containing one runtime binding, one map entity-scene ownership path, shared art dependencies, and zero current-map static manifest/chunk/legacy-placement runtime entries.
- [ ] Run Editor load, gameplay interaction, building damage/destruction, vehicle behavior, camera traversal, menu return, and two-cycle teardown with zero map visual GameObjects or static handles.
- [ ] Run Android offline package, size, memory, frame, batching, visual parity, navigation, and two-cycle lifecycle acceptance against the exact candidate revision.
- [ ] After acceptance, remove the 514 static scenes/manifest/integrity artifacts from production labels/build ownership and delete them from tracked generated output only in a separately reviewable cleanup commit with rollback archive/hash evidence.

**Exit:** The existing physical map is fully presented by SubScene entities, no current-map static scene or managed map visual is loaded, and the dense-city generator can target the same accepted path.

### Phase 1: Generated Ownership Authoring

- [ ] Add `DenseCityGeneratedRootAuthoring` and its Unity `.meta` file in `Game.Authoring`.
- [ ] Add `DenseCityAuthoredOverrideAuthoring` and its Unity `.meta` file in `Game.Authoring`.
- [ ] Add `OperationMapBuildingAuthoring` and bake intact/destroyed authoring roots to entity references using the existing `UnitGridAuthoring.Baker` pattern.
- [ ] Make the building Baker recursively include shell and attached-prop descendants under their declared visual root; reject descendants shared across buildings, present under both states, outside both states, or parented to an independent render-only owner.
- [ ] Add unmanaged `OperationMapBuildingComponent` and `OperationMapBuildingDestroyedComponent` data in `Game.Components`.
- [ ] Add finite-value, stable-id, duplicate-id, size, and scene-ownership validation for all generated/override/building authoring types.
- [ ] Add `DenseCitySemanticHierarchyBuilder` in `Game.Editor`.
- [ ] Create the exact proxy hierarchy in the operation-map authoring scene and gameplay/render-only entity presentation hierarchy in the existing map SubScene with identity transforms.
- [ ] Add exactly one nearest `MapBakeGroupAuthoring` role owner to each proxy semantic group; do not classify generated render entities as static-presentation inputs.
- [ ] Reject duplicate/missing role roots, mismatched generation ids, misplaced override roots, role overlap, and roots outside the active operation-map scene pair.
- [ ] Prove regeneration deletes only the two marked role roots and preserves all other objects in both scenes by identity/hash.

**Exit:** One logical disposable generation set spans two explicit scene-scoped roots, one persistent override domain remains protected, and generated building authoring has an explicit ECS bake contract.

### Phase 2: Generator Semantic Output

- [ ] Add bounded `DenseCityGenerationRecords` value types and stable ordering rules.
- [ ] Refactor building placement to emit ECS-building, foundation, blocker, intact-visual, and destroyed-visual records before realization.
- [ ] Emit stable building-owned attachment records for roof/interior/shop/tent props during generation and realize them beneath the declared intact or destroyed root rather than the global render-only prop hierarchy.
- [ ] Refactor road generation to emit distinct road, shoulder, terrain-patch, bridge, and ramp records.
- [ ] Refactor canal/park generation to emit separate water exclusion, bridge, bank, terrain, vegetation, and light/prop records.
- [ ] Refactor civic/market generation to separate hero presentation, terrain, road, and blocker records.
- [ ] Refactor courtyard, wall, rock, mountain, vegetation, rooftop, power, and street-detail generation according to the semantic table.
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
- `OperationMapEntitySceneAddressablesOwnershipTests.cs`

Extend existing tests rather than duplicating them for:

- static presentation input, structural validation, ownership, rollback, integrity, and no-op behavior, including freeze/retirement and rejection of every existing/generated renderer after cutover;
- Entities Graphics baking for existing/generated gameplay and render-only placements, stable identities, intact/destroyed references, shared asset ownership, and map SubScene ownership;
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
| 2026-07-21 | Phase 0 deterministic editor baseline | `OperationMapPhase0BaselineProbeTests`: 8/8 passed; two `/private/tmp/warline-phase0-baseline-*.json` captures differed only by their declared report path; concise logs at `/private/tmp/warline-phase0-baseline-tests.log` and `/private/tmp/warline-phase0-probe-{a,b}.log` | Partial pass | Schema v2 recorded the clean canonical authoring scene and map SubScene identities, 15,792 GameObjects, 15,671 renderers, 15,584 mesh renderers, 13,317 prefab-instance roots, 27 bake groups, zero generated roots, zero prohibited physics components, 269 static chunks, 11,892 static sources, protected-root candidate GlobalObjectIds, and current surface/manifest/integrity/minimap/runtime-binding identities. The checked-in Addressables Build Layout predates this manifest state, so package identity and authoring-scene exclusion rows remain open pending a post-commit rebuild. Android evidence and protected-root approval also remain open. |

## 25. Completion Rule

This tracker is complete only when:

- all current existing map visual owners have accepted stable-identity migration dispositions with no mixed/unresolved row;
- existing military, handmade, building, vehicle, road, bridge, terrain, mountain, vegetation, prop, infrastructure, and horizon visuals bake to the protected map SubScene;
- the city regenerates under one disposable ownership set with exactly one accepted root in each required source scene;
- authored military, handmade, infrastructure, placement, and override behavior/transform identity remains semantically and visually preserved through migration;
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
