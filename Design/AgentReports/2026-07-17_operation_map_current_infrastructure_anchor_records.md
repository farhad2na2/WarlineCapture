# Current Operation Map Infrastructure Anchor Records

## Scope

The current compatibility and staged Desert Base operation-map definitions now contain immutable runway and helipad records derived from the accepted map building-placement config and the same prefab geometry used by current runtime building definitions. No scene, placement config, map-surface data, grid data, loading strategy, or gameplay consumer changed.

## Authored Records

- One faction-1 runway: `anchor.skirmish.desert_base_01.runway.faction_1.lane_0`.
- Three faction-1 helipads: `anchor.skirmish.desert_base_01.helipad.faction_1.lane_0` through `lane_2`.
- The accepted current placement config contains no registered faction-0 airport or helipad, so no faction-0 infrastructure anchor was invented.
- The two existing compatibility debug anchors remain unchanged and precede the infrastructure records.

Runway center, orientation, and half-length come from `BuildingRunwaySystem` prefab-local runway extraction transformed through the placement record. Helipad center comes from the existing `Spawn_01` marker convention, orientation comes from the authored placement and marker, and clearance radius is half the smaller world-space prefab X/Z bound. Missing or duplicate markers, stale counts, duplicate source identities, missing prefabs, non-finite scale, and non-positive geometry fail closed.

## Determinism And Architecture

- Placements are filtered by exact category and sorted by ordinal source path.
- Lane indices are assigned per faction in that stable order.
- Generated metadata hashing includes every ordered anchor identity, type, transform, radius, faction, and lane using invariant round-trip float formatting.
- The compatibility content version is `3`; physical content and source identity hashes remain unchanged.
- Prefab geometry extraction moved to a small `BuildingRunwaySystem` partial file, shrinking the reviewed 500-line production source while preserving its public behavior.
- Runtime lookup remains allocation-free pure metadata math. Editor authoring performs no runtime work and adds no manager, controller, facade, service locator, update loop, loader policy, Addressables dependency, or generator output.

## Validation

- Compatibility definition tests: `3 / 3` passed.
- Staged scene/definition tests, isolated so Match stays closed: `10 / 10` passed.
- Spatial config tests: `26 / 26` passed.
- Production source-growth architecture gate: `17 / 17` passed.
- Non-ECS naming architecture gate: `9 / 9` passed.
- Camera/minimap ownership evidence regenerated twice byte-identically: SHA-256 `110696910ef5ea8f74c9a89c3b9f0993444e1235715f43e264323b9f0dedbf78`; the pre-existing report status remains `NeedsDecision` rather than being misreported as acceptance.
- Compatibility definition regenerated twice byte-identically: SHA-256 `33e2f58c2644533119310e76812e6cb29bc60778bf23a4260ef31623162eba17`.
- Staged definition regenerated twice byte-identically: SHA-256 `73ed9c963746f25d590e32b82d0193dbb897df65588ab260502f2dcc5d9884db`.
- Unity compile completed with no compiler errors.
- `git diff --check` passed.

## Remaining Work

The Phase 6 runway/helipad binding item remains open. Existing building-derived runway and production-slot read models stay authoritative until taxi, takeoff, return, and landing consumers are switched to exact active-map anchor lookup with tested compatibility fallback and teardown behavior.
