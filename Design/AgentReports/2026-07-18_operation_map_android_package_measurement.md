# Operation Map Android Package Measurement

Date: 2026-07-18
Status: Measurement captured; comparison and final release/device acceptance remain open

## Artifact

- Path: `Build/AndroidAPK/WarlineCapture.apk`
- APK bytes: 479,081,838 (approximately 457 MiB)
- SHA-256: `42112004236afee2b0bc4aebde5eedaed11f233bd6c83d531b84b2b9766325ad`
- ZIP integrity: passed with no compressed-data errors.
- Unity Android build: passed with 25 warnings and no build failure.
- Build log: `/private/tmp/opmap-addressables-dedup-android-build.log`.

## Comparison Constraint

- The approximately 382 MB artifact is a Jenkins build, while this 479,081,838-byte artifact is a local build.
- Prior local artifacts were already above 400 MB, but no controlled pre-map local artifact with identical build settings is preserved for an exact delta.
- Jenkins and local APK sizes cannot be used as a valid before/after pair because build settings, cache state, compression, stripping, and pipeline environment may differ.
- The current Addressables one-map closure is independently measured at 148,222,351 bytes.

## Disposition

The provisional per-map planning range remains open. Acceptance requires controlled before/after artifacts produced by the same build pipeline and settings. The 479,081,838-byte local APK is valid current evidence but is not an accepted delta or final release-size target. Installed size, AAB, device startup, memory, sustained FPS, thermal behavior, and offline hardware launch remain open.
