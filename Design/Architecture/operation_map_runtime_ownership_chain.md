# Operation-Map Runtime Ownership Chain

Status: Approved shared foundation
Date: 2026-07-16

## Canonical Chain

```text
Mission / game-mode selection
  -> ScenarioSetupConfig
  -> OperationMapDefinition
  -> selected source content and optional ECS subscene
  -> map-owned surface / placement / presentation metadata
  -> generation-scoped active-map ECS state
  -> existing gameplay, camera, minimap, movement, and presentation consumers
```

This chain is independent of whether accepted source content is authored in the Editor or produced through a later runtime scene-based route. It defines ownership and validation only; it does not select Addressables, direct scene loading, a generator, or remote delivery.

## Ownership

### Mission Or Game-Mode Selection

- Selects one stable scenario id.
- Does not reference a scene, subscene, map root, manifest, surface asset, loader address, hierarchy path, or generated output.
- Does not derive map identity from display text or mission number.

### `ScenarioSetupConfig`

- Owns scenario policy: scenario id, operation-map id, starting state, objectives, rewards, restrictions, feature gates, and ARIA hooks.
- Resolves exactly one operation-map id after validation; multiple scenarios may reuse one map.
- Does not own physical map content, renderer data, source paths, or loading policy.

### `OperationMapDefinition`

- Owns stable operation-map id, schema/content versions, source/content/generated-metadata hashes, bounds, camera records, minimap projection, and typed anchors.
- Later stores only lazy heavyweight references after the delivery decision; it never stores a concrete `Game.Rendering.StaticMapPresentationManifest` type or hot runtime policy.
- Does not own objectives, rewards, mission progression, or scenario variants.

### Selected Source Content And Optional Subscene

- Own map-authored roots and direct authoring references required to instantiate or bind the selected physical map.
- A direct scene view, if required, is a non-updating serialized-reference boundary only. It cannot select maps, search globally, self-register through an update loop, or own gameplay policy.
- Optional ECS subscene readiness is required only when the selected definition declares such content.

### Map-Owned Heavy Metadata

- `MapSurfaceBlob` remains the only large height/surface/path payload.
- Building/vehicle placement configs, runtime-grid source metadata, blocker source data, runways, helipads, minimap raster, static presentation manifest, and generated chunks remain map-owned.
- The operation-map metadata blob contains only bounded cameras, anchors, minimap projection, ids, versions, and hashes. It never duplicates surface cells, blocker grids, meshes, textures, or manifest source entries.

### Composition Boundary

- Resolves and validates scenario and map definitions once before beginning a transition.
- Later owns concrete source/subscene/heavy-asset handles and deterministic failure unwind after the delivery route is approved.
- Publishes one generation of active ids, bounds, readiness, and immutable metadata into ECS.
- Owns and disposes only the operation-map blob it creates; the existing map-surface bootstrap remains sole owner of `MapSurfaceBlob`.
- Does not add a recurring map-loading `ISystem` or a new updating `MonoBehaviour` loop.

### ECS Consumers

- Read `ActiveOperationMapComponent`, `OperationMapBoundsComponent`, `OperationMapMetadataComponent`, `OperationMapReadinessComponent`, and the existing `MapSurfaceComponent`.
- Compare bounded fixed strings or typed enums/reason codes, never managed strings, scene paths, hierarchy names, or diagnostic text for control flow.
- Camera, minimap, objectives, movement, placement, and aircraft systems consume published data without acquiring Unity assets or loading content.

## Validation Order

1. Validate scenario identity and its one operation-map id.
2. Resolve one operation-map definition and validate identity, versions, hashes, bounds, camera ids, minimap id, anchors, and required metadata references.
3. Validate selected source identity against the definition before binding direct references.
4. Publish a new generation only after required source, metadata, surface, authored conversion, and selected presentation flags are ready.
5. Gate gameplay on `ReadyFlags & RequiredFlags == RequiredFlags` with no failed flag for that generation.
6. On interruption, failure, switch, or exit, invalidate the generation, drain presentation ownership, dispose owned metadata, clear ECS state, and then release concrete content in the approved order.

## Current Compatibility Map Identity

The current large desert/base map is approved as:

```text
opmap.skirmish.desert_base_01
```

The id describes the reusable physical/logical Skirmish map. It does not encode `Match.unity`, a mission variant, art revision, generated seed, static-manifest hash, or delivery address. This decision authorizes loader-neutral registration work only; it does not authorize guessed bounds/anchors, scene moves, shell stripping, Addressables, or a generator.
