# Operation Map Addressables Content Build Integration

Date: 2026-07-18
Status: Passed for editor/CI entry-point integration

## Change

- `BuildScript.BuildAndroid` now performs the operation-map Addressables content build after Android target configuration and before static-map scene resolution or `BuildPipeline.BuildPlayer`.
- Both profiler APK entry points use the same gate through their shared private build method.
- The content gate fails closed when settings output, local catalog, catalog hash, fresh Build Layout, layout validation, or operation-map report publication is absent.
- Jenkins inherits the gate through its existing `Game.Editor.BuildScript.BuildAndroid` invocation; no parallel CI-only content path was added.

## Validation

- Focused integration/report/layout/naming matrix: `19 / 19` passed.
- Real cached Android-target Addressables content build: passed.
- Required `settings.json`, `catalog.bin`, `catalog.hash`, and fresh Build Layout were present.
- Deterministic report remained byte-identical and reported `wrote=0`.
- Compiler errors: `0`.
- Full APK/AAB player build was not run in this slice.
- Logs: `/private/tmp/opmap-content-build-integration.log`, `/private/tmp/opmap-content-build-integration-real.log`.
