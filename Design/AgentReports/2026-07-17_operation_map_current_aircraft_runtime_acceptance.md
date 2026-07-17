# Current Operation Map Aircraft Runtime Acceptance

## Scope

This slice validates the committed `opmap.skirmish.desert_base_01` infrastructure records through the existing runtime bootstrap and aircraft consumers. It adds no map loading, Addressables, generator behavior, scene changes, update loop, or second aircraft authority.

## Integration Correction

The acceptance test exposed a real boundary mismatch: runtime building publication normalizes the helipad id to `building_helipad`, while the first binder revision matched `Building_Helipad`. Synthetic binder tests had used the non-production casing and hid the defect. The binder and focused tests now use the same normalized identifier as `BuildingRuntimeProcessingCompositionSystemHelper` and `BuildingSpawnCompositionSystemHelper`.

The navigation ownership probe now directly hashes both `OperationMapRunwayReadModelUtility.cs` and `OperationMapHelipadReadModelUtility.cs`, so future binder drift invalidates the evidence rather than surviving behind the composition caller hash.

## Accepted Behavior

- The committed current-map definition publishes through `OperationMapRuntimeBootstrapSceneSystemHelper`.
- Its runway record initializes a faction-1 fixed-wing unit through `FixedWingRunwayHomeInitializationSystem` with exact takeoff and landing thresholds.
- Its three helipad records bind to normalized runtime production rows and resolve through the existing building spawn consumer at exact map centers.
- Teardown removes active metadata; compatibility republish paths retain non-map runway and helipad rows.
- Existing focused regressions cover runway taxi/takeoff pitch and early liftoff, return approach/go-around and landing, helipad occupancy, initial helipad spawn, and produced-unit return behavior.

## Architecture And Performance

- Immutable infrastructure identity remains in `OperationMapBlob`; mutable occupancy and production-slot ownership remain in existing ECS read models.
- Runtime code remains allocation-free on the dirty publication boundary and adds no recurring managed work.
- No manager, controller, facade, service locator, broad replacement shell, or updating `MonoBehaviour` was introduced.
- The acceptance fixture may use managed collections because it is Editor-only test code; no player assembly changed except the normalized fixed-string constant.

## Validation

- Corrected current-map and binder focused tests: `7 / 7` passed.
- Combined current-map, lifecycle, ownership, naming, and source-growth suite: `85 / 85` passed.
- Unity compilation completed with zero C# errors.
- Navigation ownership payload: `8009c700ca359a19c5d76a1c5121549d04a641dda6540947113ad17f68326152`.
- Deterministic report SHA-256: `28a6f7fd508a8b3c6736a3629f6a71419381c43b9aecd574dbdb1cb56229730c`.
- `git diff --check` passed.

## Result

The shared Phase 6 runway/helipad checklist item is complete. Objective focus and approved current-map faction deployment/spawn transforms remain separate open Phase 6 work.
