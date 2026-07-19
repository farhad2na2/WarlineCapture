# Operation Map Android Performance Characterization

## Scope

- Commit: `7bf67ae789f9afd6114adb65255f4800653c4fee`
- APK SHA-256: `0a06746cea8fd03a7d22927f2bbb6a0d1ed5d9393746eb09adb55f671ce7b713`
- APK bytes: 484,546,484
- Device: `24090RA29G`, Android 16, `arm64-v8a`
- Connection: wired USB
- Scenario: Skirmish using `opmap.skirmish.desert_base_01`
- Device networking was not modified.

## Runtime Evidence

| Measurement | Menu | Live match | Match delta |
|---|---:|---:|---:|
| Total PSS | 1,254,734 KiB | 3,050,416 KiB | 1,795,682 KiB |
| Total RSS | 1,398,592 KiB | 3,192,656 KiB | 1,794,064 KiB |
| Graphics | 516,544 KiB | 1,250,168 KiB | 733,624 KiB |
| Android Unknown | 437,620 KiB | 1,175,360 KiB | 737,740 KiB |
| Native heap | 53,480 KiB | 63,120 KiB | 9,640 KiB |

- SurfaceFlinger steady-state sample count: 60 one-second windows.
- Average FPS: 38.14.
- Minimum FPS: 31.79.
- 10th percentile FPS: 33.92.
- 90th percentile FPS: 39.73.
- Maximum FPS: 39.74.
- Average maximum queue interval: 35.03 ms.
- Maximum queue interval: 50.14 ms.
- Match was visibly ready at the 40-second checkpoint. This is only an upper bound, not an accepted exact load time.
- Game diagnostics after readiness reported approximately 0.8-1.0 ms average update time, 15-20 ms main-thread time, 23-25 ms GPU time, 74 draw calls, and approximately 1.04 million triangles in the observed view.

## Package Expansion Evidence

- Operation-map Addressables: 125 bundles, 145,078,641 packed bytes.
- Unique serialized dependency payload: 395,345,545 bytes across 2,029 asset paths.
- Largest serialized categories:
  - Prefabs: 117,539,488 bytes.
  - Textures: 108,555,983 bytes.
  - Models: 65,377,301 bytes.
  - Generated static presentation: 32,976,141 bytes.
  - Art: 23,391,184 bytes.
  - Scenes: 18,685,154 bytes.
- The largest Core bundle is 86,530,720 packed bytes and represents approximately 191.8 MiB of serialized assets. Its largest category is Prefabs at approximately 112.1 MiB.

## Findings

1. The match memory increase is not primarily ECS persistent-container or managed-heap growth. Native heap grows by less than 10 MiB while graphics and Android Unknown account for approximately 1.40 GiB of the match delta.
2. The evidence points to render-asset residency and driver allocations as the first optimization target. This is an inference from Android memory categories and Addressables build layout; a Unity Memory Profiler capture is still required for object-level attribution.
3. The observed 38 FPS is GPU-limited in the sampled view. Gameplay update time remains comparatively small.
4. The current package, memory, load-time, and sustained-performance gates remain open. This report is characterization evidence, not acceptance.

## Recommended Next Slice

1. Capture a development-player Unity Memory Profiler snapshot at menu and at the same match camera pose.
2. Rank resident textures, meshes, animation textures, render targets, and duplicated source/presentation objects by retained size.
3. Verify whether the editor-authored source scene retains render assets after canonical source renderers are hidden.
4. Verify the number of loaded static-presentation chunks and shared bundles at the sampled camera footprint.
5. Optimize the largest confirmed owner, then repeat this exact device sample before changing quality or streaming policy.

## Transient Evidence

- `/private/tmp/opmap-perf-menu-meminfo.txt`
- `/private/tmp/opmap-perf-match-meminfo.txt`
- `/private/tmp/opmap-perf-steady-meminfo.txt`
- `/private/tmp/opmap-perf-match-logcat.txt`
- `/private/tmp/opmap-perf-steady-logcat.txt`
- `/private/tmp/opmap-perf-steady-gfxinfo.txt`
- `/private/tmp/opmap-perf-match-40s-preview.png`

