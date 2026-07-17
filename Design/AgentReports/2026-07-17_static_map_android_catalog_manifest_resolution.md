# Static Map Android Catalog Manifest Resolution

Date: 2026-07-17

## Scope

Changed Android build-scene resolution from one compatibility manifest
constant to the manifest set selected by the operation-map catalog serialized
by the canonical Match scene.

## Result

- The Match scene must resolve exactly one valid operation-map catalog.
- Every catalog definition resolves its deterministic map-owned manifest path.
- Map ids, canonical scene dependency hashes, per-map integrity ledgers, chunk
  ownership, duplicate paths, missing scenes, and stale enabled chunks fail
  closed.
- Synthetic multi-map tests do not create or package a second physical map.
- Focused resolver suite: `26 / 26` passed.
- Current-project validation passed for the one approved map and 514 chunks.
- Phase 0 ownership/evidence suite: `157 / 157` passed.
- Architecture gates remained at the existing upstream baseline: `22 / 26`
  passed with the same four unrelated source-growth ratchet failures; no
  finding names a changed file in this slice.
- Zero compiler errors; `git diff --check` passed.

## Evidence

- Focused results: `/private/tmp/opmap-android-catalog-tests.xml`
- Current-project probe: `/private/tmp/opmap-android-catalog-current.log`
- Ownership results: `/private/tmp/opmap-android-ownership-tests.xml`
- Architecture results: `/private/tmp/opmap-android-architecture.xml`
