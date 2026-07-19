# Operation Map Hand-Edit Bake

## Scope

- Source scene: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`
- Source commit: `a0e3a3b6b` (`Map`)
- Editor command: `Game/Operation Maps/Bake Current Map (All)`
- Batch entry point: `Game.Editor.OperationMapCurrentMapBaker.Run`

## Baked Output

- Building placements: 814
- Vehicle placements: 22
- Surface cells: 2,097,152
- Static presentation sources: 42,127
- Static presentation chunks: 501
- Presentation content hash: `e138fba4ea5bc7576378bd2e5297a6f8`
- Stale chunks removed during refresh: 99
- Local Addressables map bundles: 127
- Local Addressables map bytes: 145,245,659

## Validation

- One-button bake: passed all 8 stages.
- Deterministic presentation rerun: 501 chunks, 0 scene writes, 0 stale deletions.
- Addressables layout: 501 manifest chunks, 501 generated scenes, 501 presentation entries.
- Focused EditMode tests: 60 passed, 0 failed.
- Current staged spatial-binding validator: passed.
- `git diff --check`: passed.

## Android Package Follow-Up

- Integrity metadata migration preserves the committed 501 scene files and refreshes only metadata hashes after normalization.
- Deterministic migration bake: 501 reused scenes, 0 scene writes, 0 stale deletions.
- Integrity, Android resolver, and map-surface bootstrap EditMode tests: 89 passed, 0 failed.
- APK build: passed with 20 warnings.
- APK bytes: 484,546,484.
- APK SHA-256: `0a06746cea8fd03a7d22927f2bbb6a0d1ed5d9393746eb09adb55f671ce7b713`.
- APK ZIP integrity: passed.
- Android hardware install: passed over wired USB on device `24090RA29G`.
- Android Skirmish launch: passed through the live match HUD; the previous 95% operation-map surface-binding failure did not recur.
- Android game exceptions after cold launch: 0. Matched log messages were device-vendor graphics/audio warnings only.
- Installed base APK: 473,660 KiB.
- Live-match memory: 3,056,858 KiB total PSS and 3,196,456 KiB total RSS. This remains above the final device-performance acceptance budget and requires optimization evidence before release acceptance.
- Strict no-network launch remains open because device network state was intentionally not modified during this validation.
- Architecture/naming guardrail: 49 of 58 tests passed. The 9 failures reference pre-existing files outside this change set; none reference the operation-map files in this slice.

Detailed transient logs and reports are stored under `/private/tmp`:

- `opmap-bake-current-map-all-final.log`
- `operation-map-current-map-bake-report.json`
- `opmap-map-edit-focused-tests.xml`
- `opmap-map-edit-spatial-validator.log`
- `opmap-static-metadata-migration.log`
- `opmap-integrity-fix-tests-final.xml`
- `opmap-current-android-build.log`
- `opmap-current-android-build-runtime-surface-metadata.log`
- `opmap-final-focused-tests-escalated.xml`
- `opmap-final-focused-tests-escalated.log`
- `opmap-final-device-match.png`
- `opmap-final-device-errors.txt`
- `opmap-final-architecture-tests.xml`
- `opmap-final-architecture-tests.log`
