# Current Operation Map Spatial Binding Validation

Date: 2026-07-16

## Scope

Added an editor-only, read-only validation boundary for the staged current
operation map. The staged scene-view tool now fails closed unless the staged
map retains all accepted spatial bindings:

- exact `Map`, `Buildings`, `Runways`, airport, and helipad roots;
- map-owned `MapSurfaceAuthoring` with grid and surface GUIDs matching the
  staged `OperationMapDefinition`;
- the current authored static-blocker baseline;
- the existing lighting-data and two reflection-probe dependencies;
- runway render geometry, one airport placement, and three helipad placements;
- valid bounds, camera, minimap, navigation, and map metadata through the
  existing definition contract.

This validator adds no runtime lookup, hierarchy scan, loader behavior, or
update loop. The current `Match.unity` route and source assets remain intact.

## Validation

- Focused staged scene/spatial EditMode tests: `10 / 10` passed.
  - Results: `/private/tmp/opmap-staged-spatial-tests.xml`
  - Log: `/private/tmp/opmap-staged-spatial-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-staged-spatial-architecture.xml`
  - Log: `/private/tmp/opmap-staged-spatial-architecture.log`
- Idempotent scene-view staging completed successfully.
- Staged hashes remained byte-identical:
  - definition: `97626b92a0b7ef7e6563c2df60cf09b95532fe04f195831f165f8f3c40901926`
  - scene: `50e2be4eb679516e73e17c396388cb5f4fea26e9116c7357de419b8f38e17133`
- Canonical source hashes remained unchanged:
  - `Match.unity`: `dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`
  - `MatchSubScene.unity`: `bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8`
- Unity compilation completed with zero compiler errors.
