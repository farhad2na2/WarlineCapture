# Static Map Presentation Selected Manifest Wiring

Date: 2026-07-17

## Scope

Changed editor scene wiring from the compatibility manifest constant to the
operation-map id and catalog serialized by `MatchSceneView`.

## Result

- The selected id must resolve through the serialized catalog.
- The manifest path is deterministically derived from the selected map id.
- Manifest schema, map owner, canonical scene, chunks, and content hash fail
  closed before wiring.
- The real wiring command resolved the approved map's 514-chunk manifest and
  produced no `Match.unity` diff.
- Focused wiring/path tests: `16 / 16` passed with zero compiler errors.
- Phase 0 ownership/evidence suite: `157 / 157` passed.
- Architecture gates remained at the existing upstream baseline: `22 / 26`
  passed with the same four unrelated source-growth ratchet failures; no
  finding names a changed file in this slice.

## Evidence

- Focused results: `/private/tmp/opmap-selected-manifest-tests.xml`
- Wiring log: `/private/tmp/opmap-selected-manifest-wire.log`
- Ownership results: `/private/tmp/opmap-wiring-ownership-tests.xml`
- Architecture results: `/private/tmp/opmap-wiring-architecture.xml`
