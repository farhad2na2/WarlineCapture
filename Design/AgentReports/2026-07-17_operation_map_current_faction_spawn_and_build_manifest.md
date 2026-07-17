# Current Operation Map Faction Spawn And Build Manifest

Date: 2026-07-17

## Scope

Completed the current compatibility map's loader-neutral faction deployment anchors and refreshed the static-map Android build evidence invalidated by the definition change.

## Implementation

- Added deterministic `Deployment` anchors for factions 1 and 2 from the canonical `Faction1` and `Faction2` volume transforms in `Match.unity`.
- The builder resolves exact serialized local IDs rather than scene-name heuristics.
- The committed metadata blob resolves initial cells `(949, 344)` and `(1686, 108)` for factions 1 and 2.
- Increased the current definition content version to 4 and retained existing debug, runway, and helipad anchors.
- Regenerated `StaticMapPresentationManifest.asset` after the operation-map definition dependency changed. The bake reused all 514 chunk scenes and wrote no chunk scenes.
- Updated the Jenkins PowerShell Unity wrapper to refresh the process after waiting and fail closed with exit code 1 when no process exit code is available.

## Build Failure Diagnosis

Jenkins Build 113 correctly stopped before APK creation because its committed manifest dependency hash did not match the current `Match.unity` dependency graph. The map-definition work changed a canonical dependency but the manifest had not been regenerated in that commit. After rebasing onto latest `main`, the refreshed manifest records dependency hash `452fe53e2b0102b0859f6b96fb2e2a09`; the real current-project Android scene resolver passes against that combined tree.

## Validation

- Current compatibility definition validation, including committed faction-spawn integration: passed 4/4.
- Android build scene resolver current-project validation: passed 2/2.
- Static presentation bake: passed; 16,542 sources, 514 chunks, 514 reused scenes, 0 scenes written.
- Production source-growth architecture: passed 17/17.
- Non-ECS naming/architecture: passed 9/9.
- Unity invocation wrapper contract: passed 2/2.
- Unity script compilation: passed with no C# errors.
- `git diff --check`: passed.

Logs:

- `/private/tmp/opmap-faction-anchor-build.log`
- `/private/tmp/opmap-faction-anchor-focused.log`
- `/private/tmp/opmap-static-resolver-rebased-final.log`
- `/private/tmp/opmap-faction-static-bake-rebased.log`
- `/private/tmp/opmap-faction-anchor-source-growth.log`
- `/private/tmp/opmap-faction-anchor-naming.log`
