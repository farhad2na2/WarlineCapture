# Operation Map Addressables Build Report

Date: 2026-07-18
Status: Passed for deterministic report publication; package budgets remain unresolved

## Scope

- Added the immutable `OperationMapAddressablesBuildReport` editor model.
- Added a separate builder that validates the one-map layout, performs a real local Addressables content build, reads Unity's Build Layout, and publishes deterministic JSON.
- Changed operation-map bundles from `AppendHash` to `OnlyHash`. Both are content-hash-derived; `OnlyHash` prevents macOS filename-component overflow caused by the required label set.

## Measured Output

- Map: `opmap.skirmish.desert_base_01`
- Stable addresses: `522`
- Map bundle closure: `105` bundles, `603,272,218` compressed bytes
- Presentation partitions: `100`
- Duplicate dependencies: `944` GUID rows, `3,222,187,922` attributed duplicate bytes
- Entities artifacts visible as named Build Layout subfiles: `0`
- Report SHA-256: `cd691e4c7b5b525bbeccd0179d3f8f409ec89d13fc7b885355d558bf3d8bd0a2`

The report is truthful evidence, not package-budget acceptance. The current 603 MB map closure exceeds the provisional 110 MB approval threshold, and the duplicate total confirms that presentation partition dependencies need explicit deduplication. Entities stream/archive linkage still requires the dedicated Editor/Android proof because Addressables exposes the current subscene payload as opaque bundle data in this Build Layout.

## Validation

- Focused report-model tests: `4 / 4` passed.
- Real Android-target Addressables content build: passed after hash-only bundle filenames removed the path-length failure.
- Two real report generations produced the same JSON SHA-256.
- Identical publication is a filesystem no-op (`wrote=0`).
- Logs: `/private/tmp/opmap-build-report-tests.log`, `/private/tmp/opmap-addressables-build-report-retry.log`, `/private/tmp/opmap-addressables-build-report-noop.log`.
