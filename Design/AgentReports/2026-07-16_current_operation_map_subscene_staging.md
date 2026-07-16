# Current Operation Map SubScene Staging

Date: 2026-07-16

## Scope

Created a distinct map-owned compatibility subscene at:

`Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity`

The staged subscene retains exactly the ordered map-specific and temporary
compatibility roots:

1. `Grid`
2. `InitialUnitsSpawnerAuthoring`

`UnitPrefabRegistryAuthoring` remains only in the canonical Match subscene
because its accepted ownership is `SharedConfig`; it is not duplicated into
the map-owned subscene.

The staged operation-map scene now references the new subscene GUID
`d50925a18e9164ce782536576cb833d8`. The canonical Match scene continues to
reference the original subscene GUID `8d5e3c3f2ef84b61a4d61472c40c9a11`.

## Compatibility

The source scenes remain unchanged:

- `Match.unity`: `dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`
- `MatchSubScene.unity`: `bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8`

The staging tool is idempotent and fails on unexpected root drift, duplicate
GUIDs, missing assets, or incorrect source/staged subscene references.

## Validation

- Focused EditMode staging tests: `3 / 3` passed.
  - Results: `/private/tmp/opmap-subscene-tests.xml`
  - Log: `/private/tmp/opmap-subscene-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-subscene-architecture.xml`
  - Log: `/private/tmp/opmap-subscene-architecture.log`
- Unity compilation: zero compiler errors.
- No Addressables loading, unloading, generation, or runtime cutover changed.
