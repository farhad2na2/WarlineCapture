# Current Operation Map Compatibility Definition

Date: 2026-07-16
Status: Accepted load-strategy-neutral compatibility metadata

## Implemented

- Added one validated `OperationMapDefinition` at
  `Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset`.
- Kept heavyweight assets out of the definition. Grid and map-surface references
  are bounded GUID/hash/count metadata, not `UnityEngine.Object` payloads.
- Published the same bounded grid/surface identity data into the immutable
  `OperationMapBlob` for allocation-free ECS reads.
- Added an editor-only compatibility builder that resolves accepted scene
  objects by exact local file id and measures heights from the real
  `MapSurfaceBlob`.

## Exact Current Metadata

- Operation-map id: `opmap.skirmish.desert_base_01`
- Grid: GUID `b201000000000000000000000000000b`, origin `(0,0,0)`,
  dimensions `2048 x 1024`, cell size `1`, authored blocked cells `0`
- Surface: GUID `12f517deb32ab49698acbfdaf7c3eac7`, runtime hash
  `8f661e49fcdbb96314ff03d48bbb3993`, surfaces `2,097,152`, payload
  version/encoding `3 / 1`
- Measured surface height range: `-6.9545445 .. 40.375454`
- World bounds: `(0,-6.9545445,0) .. (2048,100,1024)`
- Camera bounds: `(0,15,0) .. (2048,100,1024)`
- Active camera position: `(870.0283,42.030247,325.60086)`
- Minimap projection: origin `(0,0,0)`, size `(2048,1024)`
- Compatibility boundary anchors preserve exact scene-local `Start` and `End`
  transform identities.
- Definition asset SHA-256: `f1a1662143b05463aacb60333d72861812d44b9d3ffad72fb7857590139bc4f0`

## Validation

- Unity generation and accepted-input hash gate:
  `/private/tmp/opmap-definition-final-build.log`, exit `0`
- Focused EditMode: `42 / 42` passed,
  `/private/tmp/opmap-definition-final-tests.xml`
- Source-growth and non-ECS naming gates: `24 / 24` passed,
  `/private/tmp/opmap-definition-architecture.xml`
- Second Unity generation produced the identical asset SHA-256.
- `git diff --check`: passed.
- No Match scene, subscene, static-presentation manifest, generated chunk, map
  surface, grid, placement config, prefab, or build-setting file changed.

This slice adds no Addressables field, loader, load/unload behavior, editor map
generator, remote-content policy, or physical future-map rollout.
