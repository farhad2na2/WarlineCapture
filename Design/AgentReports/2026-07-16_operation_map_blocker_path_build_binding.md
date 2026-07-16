# Operation Map Blocker, Path, And Build Binding

Date: 2026-07-16

## Scope

Completed the loader-neutral Phase 6 binding from the active operation-map navigation metadata to the existing runtime grid used by movement, pathfinding, and building placement.

## Implementation

- `OperationMapMetadataUtility` now resolves and validates typed grid/navigation metadata, including counts and capability byte ranges.
- `OperationMapGridStartupBinding` requires active maps to declare surface movement, dynamic blocker, and dynamic occupancy support.
- Active metadata must match the compatibility grid dimensions, origin, cell size, and authored blocked-cell count. A mismatch fails startup rather than silently publishing a different grid.
- The current authored blocked-cell array remains in `GridAuthoringConfig`; it is not duplicated into the small immutable operation-map blob.
- `RuntimeGridBootstrapStartupSystemHelper` delegates native storage and valid authored blocked-cell initialization to the narrow `RuntimeGridStorageInitialization` utility. Out-of-range entries are ignored consistently with authoring bounds behavior.
- No-active-map startup retains the existing serialized compatibility fallback.

## Runtime Ownership

- Movement and pathfinding continue to consume the existing `GridWalkable`, `DynamicBlockerComponent`, `DynamicOccupancyComponent`, and `MapSurfaceComponent` authorities.
- Placement continues to validate against the same grid/blocker state.
- No new update loop, managed gameplay service, manager, controller, facade, or per-frame allocation path was introduced.

## Validation

- `OperationMapGridStartupBindingTests.RunFocusedValidation`: passed 5/5.
- `RuntimeGridDeduplicationSystemTests.RunFocusedValidation`: passed 5/5.
- `UnitMovementBlockerValidationTests.RunBatchValidation`: passed.
- `BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation`: passed 19/19.
- `Aph805MenuMatchMenuLifecyclePlayModeTests`: passed 1/1; active-map startup and teardown completed.
- Camera/minimap ownership evidence regenerated twice byte-identically: SHA-256 `fff35a2d69ae80351162ddb11cbdc2cfca22df0528d19346407aa34faadb7996`.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed 9/9.
- `ProductionSourceGrowthArchitectureTests.RunFocusedValidation`: passed 17/17; the existing runtime-grid helper was reduced from its 137-line baseline to 106 lines.
- Unity script compilation: passed with no C# errors.
- `git diff --check`: passed.

Logs:

- `/private/tmp/opmap-grid-binding-focused-3.log`
- `/private/tmp/opmap-runtime-grid-focused.log`
- `/private/tmp/opmap-movement-blocker-validation.log`
- `/private/tmp/opmap-building-placement-validation-2.log`
- `/private/tmp/opmap-menu-match-menu.log`
- `/private/tmp/opmap-camera-ownership-refresh.log`
- `/private/tmp/opmap-camera-ownership-refresh-2.log`
- `/private/tmp/opmap-naming-validation.log`
- `/private/tmp/opmap-source-growth-final.log`
