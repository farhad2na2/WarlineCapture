# Static Map Presentation Per-Map No-Op Reuse

Date: 2026-07-17

## Scope

Closed the Phase 2 identical-second-bake contract without creating a second
physical map. Reuse validation now receives the active operation-map id and
output root instead of validating every scene against the compatibility map.

## Result

- Focused ownership suite: `54 / 54` passed.
- Phase 0 ownership/evidence suite: `157 / 157` passed.
- The regression covers the approved map and a synthetic alternate owner.
- Both authoritative current-map bakes reported `reusedScenes=1`,
  `scenesWritten=0`, and `staleScenesDeleted=0` for all 514 chunks.
- Pre/post SHA-256 lists for every generated static-presentation file were
  byte-identical.
- No second map scene, generated chunk set, or package was produced.
- Architecture gates remained at the existing upstream baseline: `22 / 26`
  passed, with four unrelated source-growth ratchet failures and no finding
  naming a changed file in this slice.

## Evidence

- Focused results: `/private/tmp/opmap-second-bake-tests.xml`
- First bake: `/private/tmp/opmap-second-bake-1.log`
- Second bake: `/private/tmp/opmap-second-bake-2.log`
- Pre-bake hashes: `/private/tmp/opmap-second-bake-before.sha256`
- Post-bake hashes: `/private/tmp/opmap-second-bake-after.sha256`
- Ownership results: `/private/tmp/opmap-noop-ownership-tests.xml`
- Architecture results: `/private/tmp/opmap-noop-architecture.xml`
