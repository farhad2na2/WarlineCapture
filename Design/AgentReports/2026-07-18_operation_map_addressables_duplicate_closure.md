# Operation Map Addressables Duplicate Closure

Date: 2026-07-18
Status: Passed

## Implementation

- Shared-dependency discovery now counts the source scene and every presentation partition.
- Any shareable dependency used by at least two bundles is promoted into the existing 24 deterministic shared shards.
- The content-build command runs Unity's `Check Duplicate Bundle Dependencies` Analyze rule before producing a fresh Build Layout.
- Build Layout publication fails closed when any duplicate dependency is project-owned, unresolved, or outside `Packages/`.

## Measured Result

- Shared entries: 938; shared shards: 24; presentation partitions: 100.
- One-map bundle closure: 148,222,351 bytes across 131 bundles, down from 258,360,913 bytes.
- Stable operation-map addresses: 1,460.
- Unity Analyze object-level findings: 58.
- Build Layout duplicate GUID rows: 5; attributed duplicate bytes: 14,165,708.
- All five remaining rows are Unity package shaders/materials; duplicated project assets: 0.
- A second layout generation was byte-identical; the final content build published `wrote=0` for unchanged evidence.

## Validation

- Focused layout, validator, report, and duplicate-policy tests: 12/12 passed.
- Real Android-target local Addressables Analyze/build/report command: passed.
- Unity compile markers: 0 C# errors.
- `git diff --check`: passed.
- Logs: `/private/tmp/opmap-addressables-analyze-tests.log`, `/private/tmp/opmap-addressables-analyze-final-build.log`.
- Results: `/private/tmp/opmap-addressables-analyze-tests.xml`.

## Scope

This accepts duplicate ownership and exact one-map local package membership. APK/installed size, device launch, Entities artifact linkage, and the final release-size budget remain separate gates.
