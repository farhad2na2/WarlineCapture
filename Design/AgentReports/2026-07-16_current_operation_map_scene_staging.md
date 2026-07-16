# Current Operation Map Scene Staging

Date: 2026-07-16

## Scope

Created the canonical current-map staging folder and an exact serialized
duplicate of `Assets/Game/Scenes/Match.unity` through
`AssetDatabase.CopyAsset`:

`Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`

This is a non-destructive staging step only. The source `Match.unity` remains
unchanged and fully authoritative; no roots have moved and no runtime route,
build setting, subscene, config, manifest, chunk, or renderer ownership changed.

## Identity Evidence

- Source scene SHA-256:
  `dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`
- Staged scene SHA-256: same value.
- Source scene GUID: `cc4f48a57793d4597b4ffac2906c515e`.
- Staged scene GUID: `ca1f2d7f265d8495f8c815441d68fda0`.
- Serialized staged scene size: approximately `53 MiB`.
- Operation-map source scenes are stored through the existing Git LFS policy
  family, so the Git commit carries an LFS pointer rather than another 53 MiB
  normal Git blob. This does not alter Unity/build serialization.
- OperationMaps folder GUID: `d27005f8aaf7044aa9f2a330f938ecc8`.
- Skirmish folder GUID: `f3ebcfac38bab47ce909e0bd7f70a57f`.

At initial staging, `OperationMapCurrentCompatibilitySceneStager` required the
new scene bytes to exactly match the source and required a distinct GUID. After
the accepted root-extraction step, rerunning the stager preserves the existing
distinct-GUID staged scene without overwriting its extracted content.

## Validation

- Stager and Phase 0 baseline EditMode tests: `10 / 10` passed.
  - Results: `/private/tmp/opmap-current-scene-stage-tests.xml`
  - Log: `/private/tmp/opmap-current-scene-stage-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-current-scene-stage-architecture.xml`
  - Log: `/private/tmp/opmap-current-scene-stage-architecture.log`
- Unity compilation: zero compiler errors.
- `git diff --check`: passed.

Root extraction, staged subscene ownership, placement regeneration, metadata
rebinding, presentation baking, and runtime cutover remain open Phase 4 work.
