# Operation Map Local Addressables Group Topology

Date: 2026-07-17

## Scope

Created the approved local one-map Addressables group topology without moving
the Match scene, map payloads, generated presentation chunks, or runtime load
ownership.

## Result

- `Operation Maps - Catalog` contains only the small catalog and current map
  definition records.
- `Operation Maps - Shared` is explicit and empty because only one physical
  map exists and no cross-map GUID evidence can yet justify shared admission.
- The current map has empty Local Core and Local Presentation group shells.
- All four groups use Local Build/Load paths, LZ4, CRC/cache validation, and
  hash-appended bundle names.
- Presentation is prepared for deterministic label partitioning; no chunks are
  assigned yet.
- The builder is byte-stable on a second run.

## Validation

- Topology test: `1 / 1` passed.
- Phase 0 ownership regression: `157 / 157` passed.
- Second-run Addressables data SHA-256 comparison: byte-identical.
- Zero compiler errors; `git diff --check` passed.
- Build log: `/private/tmp/opmap-addressables-groups-build.log`.
- No-op log: `/private/tmp/opmap-addressables-groups-noop-final.log`.
- Results: `/private/tmp/opmap-addressables-groups-tests-final.xml`.
- Phase 0 results: `/private/tmp/opmap-addressables-groups-phase0.xml`.

Runtime Addressables loading, map scene extraction, stable core/presentation
addresses, labels, and content builds remain later Phase 2A slices.
