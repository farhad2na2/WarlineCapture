# Operation Map Infrastructure Anchor Foundation

## Scope

Added loader-neutral, allocation-free resolution for typed `Runway` and `Helipad` records in the active immutable `OperationMapBlob`.

## Contract

- Lookup is exact by anchor kind, faction id, and lane index.
- One exact match is required; duplicates fail closed.
- Id, position, rotation, radius, and grid containment are validated.
- No match is a distinct compatibility-fallback result.
- Other anchor kinds are rejected.
- The lookup does not search scene objects, allocate collections, or run per frame.

## Acceptance Boundary

This is enabling infrastructure only. Existing building-derived runway and helipad behavior remains authoritative because the current compatibility definition has no approved infrastructure anchors and the anchor schema does not yet define runway endpoint or helipad slot semantics. No transform or endpoint was guessed, and the Phase 6 behavior checkbox remains open.

## Validation

- `OperationMapMetadataUtilityTests`: `16 / 16` passed.
- Covered exact resolution, compatibility fallback, duplicate ambiguity, invalid radius, and out-of-grid position.
- Unity compilation completed without compiler errors.
- Production source-growth architecture gate: `17 / 17` passed.
- Non-ECS naming architecture gate: `9 / 9` passed.
- Ownership evidence regenerated twice byte-identically: SHA-256 `e17644447cf4727bbf24b58fccfb1caad9310351d1a1acc951c894fde869909a`.
- `git diff --check` passed before integration.
