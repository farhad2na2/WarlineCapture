# Static Map Addressables Production Activation

Date: 2026-07-18

## Scope

- Made `StaticMapPresentationAddressablesSceneApi` the bounded streamer's production default.
- Added a real PlayMode load/unload smoke using one committed manifest address and one presentation chunk.
- Kept Android manifest, ownership, integrity, and stale-scene validation while excluding Addressable presentation scenes from `BuildPlayerOptions`.
- Stabilized the Android-derived mobile render-pipeline prefilter state and refreshed the compatibility manifest dependency hash.

## Validation

- EditMode streamer/API tests: `34 / 34` passed (`/private/tmp/opmap-addressables-streamer-activation-editmode.xml`).
- PlayMode real Addressables smoke: `1 / 1` passed (`/private/tmp/opmap-addressables-streamer-activation-playmode.xml`).
- Android resolver tests: `26 / 26` passed (`/private/tmp/warline-opmap-android-base-scenes-results.xml`).
- `Game.Editor.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.
- Android APK build: passed (`/private/tmp/warline-opmap-addressables-deduplicated-android-build.log`).
- `Build/AndroidAPK/WarlineCapture.apk`: 938 MiB compressed.
- Unity build-size total: 4,261,986,907 bytes.
- Local Addressables closure: 603,274,171 bytes, 100 partitions, 944 duplicated dependency GUID rows.

## Disposition

Runtime handle ownership and offline loading passed. Package-size acceptance did not pass. Removing the 514 presentation scenes from the player scene list reduced the APK by only about 2 MiB, proving the dominant duplication is shared map assets present in both the compatibility `Match.unity` closure and local Addressables bundles. The next packaging slice must deduplicate cross-partition dependencies, followed by the thin-Match cutover. The 938 MiB APK is evidence only and is not an accepted release artifact.
