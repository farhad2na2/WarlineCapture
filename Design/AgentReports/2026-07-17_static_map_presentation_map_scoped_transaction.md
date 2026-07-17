# Static Presentation Map-Scoped Transaction

Date: 2026-07-17

## Result

Passed. Bake journaling now requires the active operation-map id, its derived output root, explicit manifest path, exact integrity-ledger path, and map-owned mutable scene set. Each accepted asset and its `.meta` file is captured and restored together. Foreign-map paths fail before journal creation.

All three production transaction entry points now pass explicit ownership rather than relying on global current-map authorization. The compatibility overload remains for existing current-map tests only.

## Validation

- Output ownership and AssetDatabase rollback fixtures: `48 / 48` passed, zero compiler errors. Result: `/private/tmp/opmap-transaction-tests.xml`; log: `/private/tmp/opmap-transaction-tests.log`.
- Alternate-map tests verify foreign-map rejection and byte restoration of both scene and `.meta` after simulated failure.
- Ownership evidence fixtures: `59 / 59` passed. Result: `/private/tmp/opmap-transaction-ownership-tests.xml`.
- Canonical bake: passed with `chunks=514`, `scenesWritten=0`, `staleScenesDeleted=0`, and `reuseRejectionReason=none`. Log: `/private/tmp/opmap-transaction-noop-bake.log`.
- `git diff --check`: passed.

Architecture gates passed `22 / 26`; the same four unrelated narrative/helper ratchet failures already present on `origin/main` remain. No failure names a file changed by this slice. Result: `/private/tmp/opmap-transaction-architecture.xml`.

No generated output, source scene, Addressables setting, or runtime loader changed.
