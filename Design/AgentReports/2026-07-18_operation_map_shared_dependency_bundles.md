# Operation Map Shared Dependency Bundles

Date: 2026-07-18

## Scope

- Preserved all 100 deterministic presentation partitions.
- Promoted dependencies used by at least eight distinct partitions into `Operation Maps - Shared`.
- Excluded scenes, scripts, generated presentation output, package assets, and assets explicitly owned by another Addressables group.
- Split promoted assets into deterministic type/GUID shards using `Pack Together By Label` instead of one monolithic shared bundle.
- Added exact membership, stable address, role-label, shard-label, and schema validation.

## Measured Result

| Measurement | Before | After |
|---|---:|---:|
| Shared dependency entries | 0 | 124 |
| Shared shards | 0 | 24 |
| Presentation partitions | 100 | 100 |
| Addressables closure | 603,274,171 bytes | 258,360,913 bytes |
| Duplicate GUID rows | 944 | 819 |
| Attributed duplicate bytes | 3,222,187,922 | 361,897,362 |
| Android APK | approximately 938 MiB | 639,085,523 bytes (approximately 609 MiB) |
| Unity build-size total | 4,261,986,907 bytes | 3,572,242,114 bytes |

## Validation

- Layout builder/validator/report tests: `9 / 9` passed (`/private/tmp/warline-opmap-shared-dependency-results.xml`).
- Non-ECS naming architecture gate: `1 / 1` passed (`/private/tmp/warline-opmap-shared-naming-results.xml`).
- Two layout runs produced byte-identical settings/group/schema hashes.
- Real Android-target Addressables build passed (`/private/tmp/warline-opmap-shared-content-build.log`).
- Android APK build passed (`/private/tmp/warline-opmap-shared-dependency-android-build.log`).
- Editor and editor-test assemblies compiled with zero errors.
- `git diff --check` passed.

## Disposition

The bounded shared-dependency topology is accepted because it removes approximately 345 MB from the map bundle closure and approximately 329 MB from the APK without reducing presentation partition granularity. The 609 MiB APK is not the final release-size acceptance. `Match.unity` still embeds the full compatibility map while the extracted source scene is also bundled; thin-shell cutover remains required before the final package gate.
