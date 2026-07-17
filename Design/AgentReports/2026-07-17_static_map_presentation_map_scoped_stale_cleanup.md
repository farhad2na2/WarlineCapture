# Static Presentation Map-Scoped Stale Cleanup

Date: 2026-07-17

## Result

Passed. Production stale-scene deletion now requires the active operation-map id, its derived output root, and the prior manifest. Schema-2 manifests must identify the same map; schema-1 cleanup is accepted only for the current compatibility map. Scene paths are validated against the supplied map namespace before any deletion callback runs.

## Validation

- Focused EditMode fixtures: `58 / 58` passed, zero compiler errors. Result: `/private/tmp/opmap-stale-cleanup-tests.xml`; log: `/private/tmp/opmap-stale-cleanup-tests.log`.
- Cross-map tests prove map-B cleanup deletes only map-B output and rejects a map-A manifest before deletion.
- Ownership evidence fixtures: `59 / 59` passed. Result: `/private/tmp/opmap-stale-cleanup-ownership-tests.xml`.
- Canonical bake: passed with `chunks=514`, `scenesWritten=0`, `staleScenesDeleted=0`, and `reuseRejectionReason=none`. Log: `/private/tmp/opmap-stale-cleanup-noop-bake.log`.
- `git diff --check`: passed.

The combined ownership/architecture run passed `81 / 85`; its four failures are upstream ratchets for unrelated narrative/helper source growth already present on `origin/main`. Details: `/private/tmp/opmap-stale-cleanup-final-tests.xml`. No failing assertion names a file changed by this slice.

No generated scene, `.meta`, source scene, Addressables setting, or runtime loader changed.
