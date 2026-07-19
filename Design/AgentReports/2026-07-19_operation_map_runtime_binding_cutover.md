# Operation Map Runtime Binding Cutover

Date: 2026-07-19

## Scope

- Point the single approved local Addressables map entry at the lightweight runtime binding scene.
- Keep the hand-authored operation-map scene as the canonical bake and presentation source.
- Validate presentation-only runtime binding structure and prevent the authoring scene from entering the package.
- Preserve map-authored building placement behavior when the runtime scene intentionally contains no source renderers.
- Correct menu-to-match teardown so one retained owner polls the Addressables scene unload through completion.

## Runtime Ownership

The Match shell retains `MatchSceneView` while operation-map teardown is active, even after gameplay shutdown clears the ECS match reference. `OperationMapSceneLoadingSceneSystemHelper` remains the sole owner of the retained Addressables load/unload handles. The menu drains static presentation, stops gameplay consumers, begins the source-scene unload, polls it through completion, and only then queues Match shell unload. No independent `SceneManager` unload or second Addressables release is used.

The Addressables source entry resolves the generated presentation-only scene. That scene references the map-owned ECS subscene and small metadata/config assets. The hand-authored scene remains excluded from Addressables and remains the canonical input for map baking and future hand editing.

## Validation

- `Game.Tests.Editor` compile: passed, zero errors.
- `OperationMapSceneLoadingSceneSystemHelperTests`: `12 / 12` passed.
- `Aph805MenuMatchMenuLifecyclePlayModeTests`: `2 / 2` passed in 58.16 seconds.
- Single lifecycle regression: passed in 28.04 seconds.
- Runtime binding focused validation: `4 / 4` passed.
- Addressables layout focused validation: `6 / 6` passed.
- Renderer-free building placement fallback: `2 / 2` passed.
- Real local Addressables content build: passed; 137,585,693 bytes, 127 bundles, 96 presentation partitions, 1,265 addresses, and five approved package-owned duplicate rows.
- Repeated Addressables build hashes: byte-identical.
- `git diff --check`: passed.
- Architecture contract runner: `49 / 58` passed. The nine failures are existing repository debt outside this slice (default-assembly leakage, unrelated runtime presentation instantiates, UI implementation coupling, lookup/registry/logging debt, and UI naming). The operation-map building fallback instantiate is explicitly classified and is not among the violations.

Focused logs and NUnit results are stored under `/private/tmp/opmap-*`. No invalid operation-handle exception occurred in the accepted lifecycle runs.

## Remaining Gates

- Rebuild and launch the final combined package on Android hardware.
- Measure package delta, transition memory, live-match PSS, FPS, and steady-state GC against the accepted baseline.
- Complete strict offline-device acceptance.

No Phase 2A or Phase 10 checklist row closes from this cutover alone.
