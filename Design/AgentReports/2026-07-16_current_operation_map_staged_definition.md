# Current Operation Map Staged Definition

Date: 2026-07-16

## Scope

Created a distinct staged `OperationMapDefinition` for the current operation
map. It preserves the accepted logical map id, source identity hash, and
content hash while binding navigation metadata to the distinct staged
map-owned subscene. A domain-separated deterministic metadata hash records the
compatibility metadata hash and staged navigation identity.

`OperationMapSceneView` now fails closed when its definition does not identify
the bound `SubScene`. Only the staged scene view was rebound; the current
`Match.unity` compatibility route remains unchanged.

This is enabling Phase 4 evidence. It does not complete the broader spatial
metadata binding checklist item, so tracker progress remains `37 / 177`.

## Validation

- Focused staged scene/definition EditMode tests: `9 / 9` passed.
  - Results: `/private/tmp/opmap-staged-definition-tests.xml`
  - Log: `/private/tmp/opmap-staged-definition-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-staged-definition-architecture.xml`
  - Log: `/private/tmp/opmap-staged-definition-architecture.log`
- Deterministic regeneration retained byte-identical hashes:
  - staged definition: `97626b92a0b7ef7e6563c2df60cf09b95532fe04f195831f165f8f3c40901926`
  - staged scene: `50e2be4eb679516e73e17c396388cb5f4fea26e9116c7357de419b8f38e17133`
- Canonical source hashes remain unchanged:
  - `Match.unity`: `dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`
  - `MatchSubScene.unity`: `bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8`
- Unity compilation completed with zero compiler errors.
- No Addressables, loading/unloading, runtime route, or update loop changed.
