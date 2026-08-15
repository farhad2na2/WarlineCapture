# M01DC-038 First Gameplay QA

- Date: 2026-08-14
- Operator kind: Agent
- Operator: Codex
- Human participant: none
- Device: Samsung SM-S918B, hardware serial `R5CTC1J02VB`, Android 16 / API 36
- Package: `com.warlinecapture.game` 0.1.0 (1)
- Tested APK source: `89865295b006a39680b75476ba7702d948acc51c`
- Tested APK SHA-256: `e2f02601c41efb9a6824038395e3db4d7f8ccb0b150a3b5201381530710719e2`
- Session: clean application data, English, Full guidance, real ADB touch/key input

The language choice, narrative panels, identity choice, and Full-guidance selection were legible and understandable. The run then exposed a P1 first-play blocker: committing guidance persisted `EnterMission` and `HandoffPending`, but no existing `EnterMatch` route request was published. The loading cover therefore remained visible because the Match world was never loaded. A cold restart reproduced `ResumeHandoff` and the same loading cover; no fatal exception or ANR was present.

The bounded correction publishes exactly one existing `EnterMatch` request from the established FirstLaunch shell boundary. It adds no new route, payload bridge, mission writer, scene loader, or protected content change. Focused boundary validation passed 4/4, FirstLaunch Gate 8/9 passed 56/56, M01 source growth passed 17/17 inside the existing 87-line/3386-byte ceiling, consolidated M01 architecture passed, and M01 PlayMode lifecycle passed 18/18. The P1 remains open until the corrected exact-head APK is built, installed, and replayed on this Samsung.

The corrected clean pushed head `893f07168c94391563cffadd91cc24d63b7c76af` subsequently built through `Game.Editor.BuildScript.BuildProductionAndroidApk` with `[ProductionAndroidPackage] result=Passed`. The APK is 558,883,165 bytes with SHA-256 `ad667e6d770fdebe706707210881a3ecb3034761ed3b04d1fc552fe7cad873be`; the build report recorded `dirty=false`, IL2CPP, ARM64, and that exact commit. Installation did not execute: after Unity restarted ADB, the Samsung's prior `192.168.2.33:35491` transport stopped responding; mDNS found the same pairing GUID and hardware serial at `192.168.2.33:33675`, but that endpoint actively refused the connection. No other device was contacted. The fresh package remains valid and device-only replay remains honestly open.

Samsung later reconnected as the sole ADB device and that exact package installed, cleared, and cold-launched. Real-input Full-guidance replay proved the first correction: the shell submitted the deferred Match scene and gameplay-start requests, and `LoadingGate` reached `gameplayInitialized=1`, `playRequested=1`, `simulationActive=1`. It also exposed `M01DC038-P1-002`: the final FL-P18 presentation remained indefinitely because production had no campaign-catalog projection or correlated logical M01 map binding, leaving mission acceptance pending over the live compatibility Match. The rejected replay recording is `%TEMP%/m01dc038_replay_full_01.mp4`, 175,669,048 bytes, SHA-256 `95eef5048bbfdb278583b92db1890de8daee680f89d1e9be92301b536d74cbba`; the final frame is `%TEMP%/m01dc038_replay_13.png`, 2,892,626 bytes, SHA-256 `09365a8984c38155b9614f248221e3bdf7dc8190af290fc9c194a8c39f76e439`. No crash or ANR occurred.

The bounded second correction serializes only the already-authored M01 mission/scenario/Chapter01 logical map catalog on Menu, projects it before launch, and reuses its correlated metadata in Match only when mission/scenario/map/source identities match the accepted compatibility physical source. `MatchSceneView`, dense-city/VRP/Skirmish content, capacities, hashes, Addressables, settings, and writers are unchanged. Checked final-source validation passes focused handoff `7 / 7` (log SHA-256 `d70c867cffc50729b8b642e8eca412406ffa6e0c1a4310061e99055d3796f18e`), consolidated contract/source-growth/architecture `23 / 17 / 23` (log SHA-256 `1d626417b6759024bcb4550297052a0a28aa43ac8551525d5a21476e1ed788ae`), and runtime PlayMode `18 / 18` with zero failed/skipped/inconclusive (log SHA-256 `fa94b5bda1abdbcc4ae3ecfae74e1086c2bd4329af7e9dd40a2ea2065e188879`; XML SHA-256 `8f2ed9b0ba1ad1693cbc1b7e9e21f3a43e9d1fc981d012e0a1e043a22ac4fb0f`). The P1 remains open until a fresh exact-source package is built, installed, and replayed.

Clean pushed head `5e14918d981fa6c517917ce0f657cdad623f1f90` then built a fresh production APK through the checked wrapper with `[ProductionAndroidPackage] result=Passed`: 558,887,145 bytes, SHA-256 `12b242c0bda94eb3434ec7eaee8762d794065cafa5740b0caed71d29d29aadc2`; build-log SHA-256 `fb788fc47705543dc29c1585806313d6dba11010293beed21b892d6fcf12d859`, `dirty=false`, IL2CPP/ARM64. The Samsung endpoint `192.168.2.33:39901` remained TCP-open, but after Unity restarted ADB the Unity SDK daemon/handshake did not complete in repeated bounded attempts, including a clean daemon and isolated local server port. No install or replay is claimed and no other device was contacted. Device-only closure remains open.

## Initial scores (1-5)

| Dimension | Score | Observation |
|---|---:|---|
| Usability | 3 | Setup and guidance choices were clear; the handoff blocker prevented mission use. |
| Simplicity | 4 | The initial flow required few, understandable choices. |
| Fun | 2 | Presentation established context, but the blocker prevented gameplay. |
| Pacing | 2 | Narrative pacing was serviceable until the indefinite loading cover. |
| Smoothness | 1 | FirstLaunch-to-Match continuity failed. |
| Audio | 3 | Media volume was nonzero and the audio route remained active; acoustic quality is not yet closed. |
| Comprehension | 4 | Language, identity, and guidance choices were understood without a human participant. |
| Accessibility | 4 | Large controls and readable presentation supported automated real input; later mission accessibility is untested. |
| Recovery | 2 | Restart preserved the handoff state but could not complete it in the tested package. |
| Bugs | 1 | One reproducible P1 blocked all mission gameplay. |

No M01DC-038 acceptance is claimed by this initial record. Corrected-package Full, Contextual, Minimal, recovery, gameplay, and audio scoring remain required.

## HUD restriction finding and source correction (2026-08-15)

The canonical-Samsung Campaign replay reached live M01 and exposed `M01DC038-P2-006`: the tactical HUD still presented Materials, Oil, Fuel, BUILD, and SUPPORT even though the authoritative M01 definition disables economy, building, production, transport, and air. The evidence frame is `%TEMP%/m01dc038_campaign_route_live2.png` (1,654,742 bytes, SHA-256 `efb101b2ba26e7c2f4831c228df1876d85f7039c5e76d7ef40a57721da91263f`) from the agent-operated real-input recording `%TEMP%/m01dc038_campaign_route_corrected.mp4` (393,342,839 bytes, SHA-256 `9e823ab077f457cac6aa72e4a5faf50dededd3150a00200d2ef7e3988aa9e735`). The package was the Samsung-only route-fix candidate sourced from `9e1fc42ca`, 558,884,777 bytes, SHA-256 `f67cb1520c2fc8d542faf37a50f54acf1270372c30a1bb0b2220efa67078362c`.

The bounded correction adds a read-only optional UI projection of the already-authored restriction flags. Active Campaign M01 now hides Materials/Oil/Fuel and disables their exchange action, hides BUILD and SUPPORT, and preserves Civilian Risk, zoom, tactical commands, and no-active-Campaign/Skirmish defaults. It adds no gameplay writer and changes no mission restriction, map/city source, prefab, VRP database/capacity/hash, package setting, threshold, or protected output. Focused validation passes `3 / 3` (log SHA-256 `e3d58e13de651903f8f7fc211bc55e2bd9a2a94b637066f205cd33a9117840f`), the consolidated architecture/source-growth gate passes `23 / 17 / 23` (log SHA-256 `b3f42eca9c4b4b407aac9db62520cb407504b823d9999f882956a7b086d45219`), and M01 runtime PlayMode passes `18 / 18` with zero failed/skipped (log SHA-256 `bb1882174c231c517de51c8643322674e6804a169e7caffe4436cab2b6f513eb`; XML SHA-256 `2ac27baa92c73c2f7d7274aea058941e656a888e8a57affe7a8b4b69037f3081`). The finding remains open until a fresh exact-source APK is built, installed, and replayed on Samsung with an after frame.

Clean pushed head `306b129af348f2030dfdb8db63e9e8d61f37bc75` then built a fresh production APK through the checked wrapper with `[ProductionAndroidPackage] result=Passed`: 558,889,905 bytes, SHA-256 `2b7d8f7cc2952c57c1e1270cc1f6094d8b080e2d7c2cdf10e9711b3c19a25c9d`; build-log SHA-256 `626c44cad8a6169913706f121e9956f3715293cb23b81b9a9fa9cb63e4cd79c6`, `dirty=false`, IL2CPP/ARM64. The Samsung was not connected after the build, so installation and replay did not execute and no other device was contacted. The wrapper-generated architecture package reports and operation-map Addressables report were restored to their exact pre-build tracked state. Samsung-only after-capture and finding closure remain open.
