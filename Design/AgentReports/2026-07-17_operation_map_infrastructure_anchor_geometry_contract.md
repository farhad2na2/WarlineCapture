# Operation Map Infrastructure Anchor Geometry Contract

## Runway

A typed `Runway` anchor uses:

- `Position`: runway surface center.
- `Rotation * forward`: runway travel direction projected onto XZ and normalized.
- `Radius`: runway half-length in world units.
- `Position - forward * Radius`: first runway threshold.
- `Position + forward * Radius`: opposite runway threshold.
- `FactionId` and `LaneIndex`: exact ownership and deterministic runway identity.

Vertical/degenerate forward axes, non-finite transforms, empty ids, and non-positive half-lengths fail closed. Runtime aircraft may choose the nearer threshold for takeoff, matching the current `BuildingFactionRunwayReadModel` behavior.

## Helipad

A typed `Helipad` anchor uses:

- `Position`: static landing-area center.
- `Rotation`: authored pad orientation.
- `Radius`: positive world-space clearance radius.
- `FactionId` and `LaneIndex`: exact ownership and deterministic pad identity.

The anchor does not own mutable production-slot reservation or occupancy. Those remain in `BuildingFactionProductionSpawnPointReadModel` and the existing production-slot systems.

## Performance And Architecture

Geometry resolution is pure value math over `OperationMapAnchorBlob`, performs no scene search, creates no managed collections, and allocates zero managed bytes after warmup. It introduces no loader policy, update loop, manager, controller, facade, rendering dependency, or generated map output.

## Validation

- `OperationMapMetadataUtilityTests`: `19 / 19` passed.
- `OperationMapSpatialConfigTests`: `26 / 26` passed.
- Covered rotated runway thresholds, degenerate vertical runway rejection, helipad center/orientation/radius, positive infrastructure radius authoring, and zero-allocation lookup math.
- Production source-growth architecture gate: `17 / 17` passed.
- Non-ECS naming architecture gate: `9 / 9` passed.
- Ownership evidence regenerated twice byte-identically: SHA-256 `bb10570f93001fe87a99a98058aae0775e518fde9590227128242ed9c0ebd81e`.
- `git diff --check` passed.
