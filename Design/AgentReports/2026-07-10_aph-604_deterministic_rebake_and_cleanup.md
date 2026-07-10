# APH-604 Deterministic Rebake and Manifest-Owned Cleanup

Date: 2026-07-10

Baseline: `9280ead856fd0bf117fdb3601cc2216c3a35e0f4`

## Result

- The one-time layer/integrity migration wrote all 525 generated presentation scenes and produced content hash `393591d2855b764bce260888e6f5fa20`.
- The immediately repeated identical bake reused all 525 scenes with `reusedScenes=1`, `scenesWritten=0`, and `staleScenesDeleted=0`.
- A SHA-256 integrity ledger covers every generated scene and `.meta`; missing, corrupt, wrong-GUID, or stale output cannot be reused.
- Scene, manifest, ledger, and `.meta` writes/deletes are journaled under `Library` and rolled back after ordinary bake failures.
- Stale cleanup accepts only prior-manifest paths matching the direct generated-scene naming contract. It does not enumerate or delete arbitrary folder contents.
- An unlisted sentinel, expected scenes, malformed paths, disk-present/database-missing scenes, missing stale paths, failed reuse conditions, and real AssetDatabase deletion/rollback are covered by focused tests.

## Implementation

1. Capture schema, canonical path, chunk size, content identity, and owned scene paths into plain managed values before opening Match. Unity scene loading can unload the native manifest object without invalidating ownership state.
2. Reload the manifest only when saving. An existing on-disk manifest that cannot import, resolve a GUID, or load as the expected type fails closed before scene writes or cleanup.
3. Include `GameObject.layer` in each source dependency identity while keeping presentation content identity separate from canonical source provenance.
4. Traverse direct dependencies and stop at generated-output nodes. Descendants reachable only through `Assets/Game/GeneratedStaticMapPresentation/` cannot re-enter canonical provenance or create the APH-606 self-invalidating hash cycle.
5. Validate reusable scene and `.meta` bytes against the deterministic integrity ledger after synchronizing AssetDatabase state.
6. Journal every mutable manifest-owned file and `.meta` before writes/deletes. Ordinary exceptions restore original bytes and GUID mappings before AssetDatabase refresh.
7. Delete stale scenes only from `prior owned paths - expected paths`; physical manifest-owned files are removed even if their AssetDatabase GUID state is stale, while unlisted files remain untouched.

## Validation

- Ownership/reuse EditMode tests: `23/23` passed in `/private/tmp/warline-aph604-ownership-escalated.xml`.
- Real AssetDatabase scene delete/rollback/bytes/GUID restoration: `1/1` passed in `/private/tmp/warline-aph604-rollback-integration-rerun-escalated.xml`.
- Migration bake: `/private/tmp/warline-aph604-migration-bake-escalated.log` reported `sources=17564 chunks=525 scenesWritten=525`.
- Identical reuse bake: `/private/tmp/warline-aph604-identical-reuse-escalated.log` reported `reusedScenes=1 scenesWritten=0 staleScenesDeleted=0`.
- Full APH-605 parity: `2/2` passed over 17,564 renderers and 525 scenes in `658.45 s`; artifact `/private/tmp/warline-aph605-structural-final-escalated.xml`.
- Game Editor and Editor-test .NET builds: zero errors.
- Stable presentation content hash: `393591d2855b764bce260888e6f5fa20`.
- Canonical `Assets/Game/Scenes/Match.unity`: unchanged.
- Second identical bake produced zero scene writes.

## Rejected Behavior Found During Validation

The APH-603 baker rewrote all 525 scenes during an identical bake, changing only Unity-local object file IDs and creating 312,777 inserted/deleted YAML lines. The accepted implementation performs one explicit integrity/layer migration, then skips all scene serialization when managed ownership, content identity, and scene/meta integrity match.

## Residual Risk

Abrupt editor termination or power loss cannot execute in-process rollback. APH-606 must use the generated-output-excluding canonical provenance contract when wiring the manifest into Match; runtime loading and Android build inclusion remain outside APH-604.
