# Operation Map Local Addresses, Labels, And Partitions

Date: 2026-07-17

## Scope

Assigned the accepted one-map Core and Presentation assets without inventing a
map raster or directly addressing the Entities subscene.

## Result

- Core contains the extracted source scene, map surface, static manifest, and
  compatibility building/vehicle placements.
- Presentation contains all `514` manifest-owned chunk scenes.
- Every assigned entry has stable map/local/pack labels and exactly one role.
- Every presentation entry has exactly one deterministic spatial partition
  label. Five-by-five chunk regions contain at most `25` scenes per bundle.
- The source scene retains its subscene through Unity's supported dependency
  chain; the subscene is not independently addressed.
- The required minimap-raster address remains unresolved because no accepted
  map-specific raster asset exists. No UI texture was substituted.

## Validation

- Address/label/partition topology: `1 / 1` passed.
- Phase 0 ownership regression: `157 / 157` passed.
- Presentation entry count: `514`.
- Second-run Addressables data SHA-256 comparison: byte-identical.
- Zero compiler errors; `git diff --check` passed.
- Build log: `/private/tmp/opmap-addressables-entries-build.log`.
- No-op log: `/private/tmp/opmap-addressables-entries-noop.log`.
- Results: `/private/tmp/opmap-addressables-entries-tests.xml`.
- Phase 0 results: `/private/tmp/opmap-addressables-entries-phase0.xml`.
