# Operation Map Typed Failures

Date: 2026-07-18
Scope: typed failure propagation and Phase 5 test-matrix closure
Result: passed

## Runtime Contract

`OperationMapSceneLoadingSceneSystemHelper` now retains the existing unmanaged `OperationMapLoadResultCode` alongside its detailed diagnostic string. `MatchSceneView` retains both values when a failure occurs before an Addressables operation is created, so UI and diagnostics do not lose the reason after helper disposal.

Accepted mappings include:

- malformed operation-map id -> `InvalidOperationMapId`
- absent catalog entry -> `MissingDefinition`
- missing source or manifest reference -> `MissingSourceContent`
- source-scene load failure -> `SourceLoadFailed`
- manifest load failure -> `PresentationPreloadFailed`
- scene-view or authored metadata mismatch -> `MetadataBindFailed`
- manifest/source identity mismatch -> `StaleContent`
- explicit abort -> `Interrupted`
- source-scene unload failure -> `SourceUnloadFailed`

No failure silently substitutes the compatibility map.

## Validation

- `OperationMapSceneLoadingSceneSystemHelperTests`: 12/12 passed.
- `OperationMapRuntimeBootstrapSceneSystemHelperTests`: 14/14 passed.
- Production sequential lifecycle remains 2/2 passed from the immediately preceding stable slice.
- `NonEcsSystemConversionArchitectureTests`: 9/9 passed.
- `Game.Runtime.csproj`: build passed with zero errors.
- `Game.Tests.Editor.csproj`: build passed with zero errors.
- Unity compilation produced no C# compiler errors.
- `git diff --check`: passed.

Unity runs used the documented out-of-sandbox macOS licensing path. The accepted test matrix covers valid load, missing/invalid id, stale manifest, interrupted load, deterministic teardown, retry, and two sequential shell-routed map loads.
