# Static Map Presentation Baker Compatibility Refactor

Date: 2026-07-17
Result: Passed

## Scope

- Routed `StaticMapPresentationBaker.Bake()` through immutable `StaticMapPresentationBakeInput` data.
- Preserved the canonical Match-map constants only in `CreateCurrentCompatibilityInput()`.
- Threaded source scene/root, output ownership, manifest/integrity paths, and chunk size through the baker internals.
- Kept alternate map ownership fail-closed until the remaining per-map manifest, integrity, and cleanup owners are migrated.

## Validation

- Focused bake-input validation: `9 / 9` passed.
- Canonical bake run 1: `16,542` sources, `514` chunks, `0` scene writes, `0` deletes, content hash `9eebc7c8aa774d5f505cb684099d133a`.
- Canonical bake run 2: identical counts/hash, `0` scene writes, `0` deletes.
- Android build-scene resolver: `2 / 2` passed.
- Non-ECS naming architecture gate: `9 / 9` passed.
- `git diff --check`: passed.
- Generated assets and canonical scenes remained unchanged.

Logs:

- `/private/tmp/opmap-baker-compat-refactor-focused.log`
- `/private/tmp/opmap-baker-compat-refactor-bake1.log`
- `/private/tmp/opmap-baker-compat-refactor-bake2.log`
- `/private/tmp/opmap-baker-compat-refactor-resolver.log`
- `/private/tmp/opmap-baker-compat-refactor-naming.log`

## Boundary

This slice does not enable a second map bake. Local Addressables and one physical editor-authored map are now the selected delivery direction, but alternate outputs remain rejected until all destructive/output-owning helpers are map-scoped and validated.
