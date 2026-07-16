# Current Operation-Map Compatibility Runtime Activation

Date: 2026-07-16
Result: Passed
Scope: Loader-neutral activation of the existing `Match.unity` map identity

## Implemented

- `MatchSceneView` serializes the compatibility catalog plus map, scenario, and mission ids.
- Match startup publishes one `OperationMapRootComponent` through the existing `OperationMapRuntimeBootstrapSceneSystemHelper` before startup consumers resolve camera/minimap metadata.
- Publication is one-shot and metadata-ready; no map scene loading, unloading, Addressables, generator, or recurring update loop was added.
- Invalid map/scenario/mission configuration fails publication and cannot silently select another map.
- Match teardown removes the operation-map root and disposes the owned metadata blob.
- The Phase 0 ownership catalog classifies `operationMapCatalog` as `SharedConfig`; serialized reference count is now `29`.

## Canonical Binding

- Scene: `Assets/Game/Scenes/Match.unity`
- Catalog: `Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset`
- Operation map: `opmap.skirmish.desert_base_01`
- Scenario: `scenario.skirmish.desert_base_standard`
- Mission: `skirmish`
- Match scene SHA-256: `a99e575cb8bc1f8a7c101025eca8ed84c22a89f5a6549b27b246a9efc021b89c`

## Static Presentation Safety

- The first approved canonical Match bake found `16,542` sources and `514` chunks.
- Across all `1,033` tracked generated files, only `StaticMapPresentationManifest.asset` changed.
- Manifest SHA-256 changed from `3940dcac3d42c703f47cf11f134b183c4554f9944629925f7b38957e08d93746` to the combined latest-main value `b389013b4753e73e65424e1ca06737507aa6fc6a6be429903b2da683b052ec01`.
- Canonical dependency hash is now `2b20b20900b73c1b456c04f9173c33a7`.
- Presentation content hash remains `9eebc7c8aa774d5f505cb684099d133a`.
- The second bake reported `scenesWritten=0`, `staleScenesDeleted=0`; all `1,033` generated files were byte-identical to the first result.

## Validation

- Before rebasing unrelated main changes, focused ownership, compatibility, static-map wiring, Android-resolution, source-growth, and naming EditMode tests: `86 / 86` passed.
- Final regenerated ownership evidence shape/hash tests: `26 / 26` passed.
- Post-hardening ownership, source-growth, and naming gates: `50 / 50` passed.
- After rebasing latest main and refreshing its changed canonical dependency, compatibility, scene-wiring, and Android-resolution EditMode tests: `36 / 36` passed.
- Latest-main source-growth gates pass `21 / 24`; the three failures concern only separately merged `RuntimeCity*` R&D files not changed by this slice. No compiler error is present.
- Existing Menu -> Match -> Menu PlayMode lifecycle test: `1 / 1` passed.
  - exactly one active operation-map root after Match load;
  - configured operation-map/scenario/mission ids matched;
  - existing Match HUD composition initialized;
  - operation-map root count returned to zero after Match unload.
- Compatibility definition rebuild passed and was byte-identical.
- `git diff --check` passed.

Logs and hashes:

- `/private/tmp/opmap-compat-binding.log`
- `/private/tmp/opmap-compat-validation-tests.log`
- `/private/tmp/opmap-compat-playmode.log`
- `/private/tmp/opmap-compat-static-bake-1.log`
- `/private/tmp/opmap-compat-static-bake-2.log`
- `/private/tmp/opmap-compat-generated-before.sha256`
- `/private/tmp/opmap-compat-generated-after-1.sha256`
- `/private/tmp/opmap-compat-generated-after-2.sha256`
- `/private/tmp/opmap-compat-post-rebase-bake-1.log`
- `/private/tmp/opmap-compat-post-rebase-bake-2.log`
- `/private/tmp/opmap-compat-post-rebase-relevant.log`
- `/private/tmp/opmap-compat-post-rebase-playmode.log`

## Deferred

- The staged operation-map scene is not loaded at runtime.
- Physical map extraction from `Match.unity`, concrete loading/unloading, Addressables, runtime generation, and future-map rollout remain deferred pending the map-direction decision and their existing parity gates.
