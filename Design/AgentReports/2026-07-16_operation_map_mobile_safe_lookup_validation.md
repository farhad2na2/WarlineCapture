# Operation Map Mobile-Safe Lookup Validation

Date: 2026-07-16

## Scope

Added focused regression coverage for the loader-neutral operation-map lookup
paths used during match updates. The tests warm each path before measuring and
require zero managed allocations while repeatedly resolving:

- the active operation map's minimap projection through the existing cached
  HUD data-source adapter;
- active operation-map camera bounds through the existing RTS camera request
  system and cached ECS queries;
- immutable typed operation-map anchors; and
- compact single-layer map-surface samples.

This slice changes no runtime source, scene, prefab, generated presentation
output, loading policy, or Addressables behavior.

## Validation

- Focused EditMode tests: `3 / 3` passed.
  - Results: `/private/tmp/opmap-mobile-safe-lookups-escalated.xml`
  - Log: `/private/tmp/opmap-mobile-safe-lookups-escalated.log`
- Source-growth and non-ECS architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-mobile-safe-architecture.xml`
  - Log: `/private/tmp/opmap-mobile-safe-architecture.log`
- Unity compiled the affected test assembly with zero C# compiler errors.
- `git diff --check` passed.

The first sandboxed Unity attempt was blocked before compilation by the known
Licensing Client timeout. The documented out-of-sandbox rerun produced the
passing focused result. Unity then emitted a native shutdown crash artifact
after writing that complete passing NUnit result; the subsequent independent
architecture run completed and passed normally.
