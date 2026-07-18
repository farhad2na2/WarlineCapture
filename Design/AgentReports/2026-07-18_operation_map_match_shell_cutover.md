# Operation Map Match Shell Cutover

Date: 2026-07-18

## Result

Passed for the Editor-authored one-map shell cutover and Android APK creation. Device launch, final package-size acceptance, deterministic failure unwind, and sequential map switching remain open.

## Ownership Result

- `Assets/Game/Scenes/Match.unity` is a 20,275-byte shell with six roots: bootstrap, camera, volume, two lights, and `MatchRuntimeSubScene`.
- `Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity` is a 9,379-byte shell-owned Entities subscene containing the shared `UnitPrefabRegistryAuthoring` only.
- `opmap.skirmish.desert_base_01` owns the extracted map source, map subscene, placements, spatial metadata, and static-presentation manifest/chunks.
- The deterministic presentation bake contains 16,542 sources and 514 chunks. A second bake wrote zero scenes and deleted zero scenes.
- Manifest SHA-256: `405915e7cb3ff0f434f1d8113d79e94627014c0d14e6b3cc4307a7324a0fbf39`.
- Integrity SHA-256: `0f4bdb334f6100ecc68a3db7c69cb82854a60e026ec32119d3d880d79a119768`.

## Runtime Result

- The source scene and presentation manifest load through retained local Addressables handles.
- The map Entities subscene is awaited through `SceneSystem` state rather than `SubScene.IsLoaded` editor state.
- Exactly one `OperationMapSceneView` publishes the active ids, bounds, metadata blob, and readiness.
- All seven readiness flags must belong to the current generation before gameplay starts.
- The existing static-presentation streamer binds the loaded manifest and world camera.
- Menu-to-match-to-menu PlayMode lifecycle passed, including operation-map readiness, UI binding, presentation drain, and return to menu.

## Validation

- Readiness EditMode: `12 / 12` passed.
- Shell and shared-subscene ownership EditMode: `20 / 20` passed.
- Android build-scene resolver EditMode: `26 / 26` passed.
- Menu/match/menu PlayMode lifecycle: `1 / 1` passed in 27.867 seconds.
- Non-ECS architecture gate: `9 / 9` passed.
- Addressables build: one map, 100 partitions, 646 addresses, 258,354,315 bytes, zero Entities artifacts manually addressed.
- APK build succeeded and ZIP integrity passed.
- APK path: `Build/AndroidAPK/WarlineCapture.apk`.
- APK size: 589,097,020 bytes (562 MiB).
- APK SHA-256: `3710bf9425ad13c11361c62d4601f0d49fa7e78cc15bc5b16c4447ae19e4fd45`.

## Remaining Gates

- Launch and complete the lifecycle on Android hardware from local bundles.
- Measure installed size, peak/steady memory, load time, and sustained gameplay performance.
- Reduce or explicitly accept the APK growth from the previous approximately 382 MB baseline.
- Prove typed failure unwind, canonical renderer restoration, complete teardown ownership, retry, and sequential switching without leaked handles or map ECS state.
