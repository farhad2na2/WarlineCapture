# Static Presentation Generated Output Hash Exclusion

Date: 2026-07-17

## Result

Passed. Canonical source hashing excludes the complete generated static-presentation root, including every current or future operation-map output root beneath `OperationMaps`, before traversing dependencies or reading files. The predicate now rejects non-normalized traversal and backslash paths rather than allowing them to hide source input.

## Validation

- Output ownership/hash fixtures: `53 / 53` passed, zero compiler errors. Result: `/private/tmp/opmap-source-hash-tests.xml`; log: `/private/tmp/opmap-source-hash-tests.log`.
- Tests cover the current map, an arbitrary alternate map, map manifests, integrity ledgers, chunk scenes, sibling paths, traversal, and backslashes.
- Ownership evidence fixtures: `59 / 59` passed. Result: `/private/tmp/opmap-source-hash-ownership-tests.xml`.
- Canonical bake: passed with unchanged content hash `ec51367ab8853815a6c44d99f4fc3d6d`, `chunks=514`, `scenesWritten=0`, and `staleScenesDeleted=0`. Log: `/private/tmp/opmap-source-hash-noop-bake.log`.
- `git diff --check`: passed.

Architecture gates passed `22 / 26`; the same four unrelated narrative/helper ratchet failures already present on `origin/main` remain. No failure names a file changed by this slice. Result: `/private/tmp/opmap-source-hash-architecture.xml`.

No generated output, source scene, Addressables setting, or runtime loader changed.
