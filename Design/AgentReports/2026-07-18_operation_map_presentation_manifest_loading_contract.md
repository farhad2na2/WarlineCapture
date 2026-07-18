# Operation Map Presentation Manifest Loading Contract

## Scope

- Load the selected static-presentation manifest through its local Addressables reference in parallel with the operation-map source scene.
- Keep both operations poll-based under the existing `MatchSceneView` lifecycle.
- Require matching map id, source-scene GUID/path, readable schema, identity, hashes, chunk size, chunks, and sources before readiness.
- Release the source-scene and manifest handles exactly once on failure or teardown.
- Project the loaded manifest through `MatchSceneView` only after readiness; preserve the serialized compatibility manifest otherwise.

## Validation

- Focused Unity EditMode validation: `7 / 7` passed.
- Existing operation-map bootstrap and static-presentation regression: `38 / 38` passed.
- Covered pending combined progress, source failure, manifest failure, identity mismatch, successful handoff, and idempotent teardown.
- Unity compiler errors: `0`.
- `git diff --check`: passed.

## Deferred

- Static-presentation chunk loading remains on the existing scene API until its Addressables scene API and retained-handle teardown are implemented.
- Thin-shell activation and staged-manifest publication remain an atomic later cutover.
