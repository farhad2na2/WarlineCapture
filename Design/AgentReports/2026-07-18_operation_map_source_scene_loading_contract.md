# Operation Map Source Scene Loading Contract

Date: 2026-07-18
Result: Passed; Match composition integration pending

## Scope

Added `OperationMapSceneLoadingSceneSystemHelper` as the bounded managed owner
of one local-Addressables operation-map source-scene load.

The helper:

- starts from the catalog-resolved `OperationMapDefinition.SourceSceneReference`;
- loads additively and activates through one retained Addressables handle;
- polls progress without completion callbacks or a new update loop;
- resolves exactly one map-id-matched view through
  `OperationMapSceneReferenceSceneSystemHelper`;
- requires `OperationMapSceneView.TryValidate` before reporting readiness;
- releases or unloads its operation exactly once on failure or disposal.

`Game.Composition` now explicitly references `Unity.ResourceManager`, which
owns `AsyncOperationHandle` and `SceneInstance`.

## Validation

- Focused Unity lifecycle validation: `5 / 5` passed.
- Covered pending progress, successful staged-scene resolution, failed load
  unwind, pending-load disposal, and idempotent disposal.
- Unity compilation: zero C# compiler errors.
- `git diff --check`: passed.

No scene, prefab, catalog, generated presentation output, Addressables settings,
or runtime composition binding changed. The Phase 5 load checkbox remains open
until the existing `MatchSceneView` lifecycle starts and ticks this helper and
publishes readiness/failure state.
