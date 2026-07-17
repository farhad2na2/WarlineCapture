# Static Map Presentation Two-Map Mutation Isolation

Date: 2026-07-17

## Scope

Validated output isolation with two synthetic map owners. This does not create
or package a second physical operation map.

## Result

- Focused ownership suite: `55 / 55` passed.
- Phase 0 ownership/evidence suite: `157 / 157` passed after publication.
- A committed map-B bake transaction deleted map B's stale scene and `.meta`.
- Map A's scene and `.meta` remained present and byte-identical.
- Existing negative coverage also rejects map-A paths from map-B transaction
  journals and rejects manifests owned by another map.
- Zero compiler errors; `git diff --check` passed.

## Evidence

- Focused results: `/private/tmp/opmap-two-map-isolation-tests.xml`
- Unity log: `/private/tmp/opmap-two-map-isolation-tests.log`
- Ownership results: `/private/tmp/opmap-isolation-ownership-tests.xml`
