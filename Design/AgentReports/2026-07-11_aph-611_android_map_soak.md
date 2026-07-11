# Android Static-Map Ten-Minute Soak

Date: 2026-07-11
Tracker item: `APH-611`
Result: map-integrity soak passed; performance acceptance remains open.

## Scope

This run validates static-map presentation integrity and process survival on Android for more than ten minutes in Match. It is not release-performance evidence because the development-profiler build had high memory overhead and the device reached a thermal skin warning during the run.

| Field | Value |
|---|---|
| Source | `2653d43ea` plus deterministic static-map manifest refresh |
| APK | ignored `Build/AndroidProfiler/WarlineCapture-Profiler.apk` |
| APK SHA-256 | `959da0fbc8c6e82073370fc18c41cd0a21896e20c283e7dc8b72bd8717ebc4ea` |
| Device | Xiaomi 24090RA29G, Android 16, serial `R4M7PZEQZ58T59ZH` |
| Launch | `-warlineAutoStartMatch -warlineProfilerMarkers` |
| Process | PID `24307` remained alive, resumed, and foreground |
| Match diagnostics | 21:48:41.070 through 22:01:23.433, `762.363` seconds |

## Visual Review

The valid 60-second, 600-second, and post-soak captures preserve the tested military-base view: terrain and roads remain continuous; buildings, tents, walls, watchtowers, vehicles, props, and HUD remain present and aligned; no visible culling hole, floating or buried prop, lighting discontinuity, missing interior-facing structure, or terrain-quality regression was observed.

The 300-second `adb screencap` is rejected as invalid readback evidence. It contains large black tiles across both the world and HUD rather than map-chunk boundaries. The device logged a system Gralloc mapper-load failure around that capture. The 600-second capture and three immediate two-second repeat captures are coherent, which rules out accepting the tiled image as a game presentation result.

Final ten-minute capture SHA-256: `fcc21874fa606609baa0e063b4d5307ac1672cc08dd491eb01d16c168769be2d`.

## Runtime Evidence

Across 12 minutes 42 seconds of available Match diagnostics, 477 two-second samples were captured across the two log snapshots. The final log window contained 235 samples over 470.502 seconds.

| Metric | Final-window result |
|---|---:|
| FPS | min `29.5`, mean `45.54`, max `48.1` |
| Average frame | min `20.8 ms`, mean `22.04 ms`, max `33.9 ms` |
| Valid GPU samples | `197 / 235`, mean `20.26 ms`, max `28.1 ms` |
| Draw calls | `74-80` |
| SetPass | `41-44` |
| Triangles | `820,397-832,786` |
| Allocated memory | `1,063-1,064 MB` |
| Reserved memory | `1,221 MB` |
| Mono memory | `52-59 MB` |

Every diagnostic sample reported `focused=1`, `playRequested=1`, and `simulationActive=1`. The logs contain no static-map load/cleanup failure, missing-reference exception, fatal exception, application ANR, `SIGSEGV`, or `SIGABRT`.

At final capture, Android reported total PSS `2,445,302 KB`, graphics PSS `1,090,797 KB`, and total RSS `2,580,513 KB`. These development-profiler values are recorded for transparency and are not accepted release-memory budgets.

## Thermal Boundary

Android's aggregate thermal status remained `0`, with CPU/GPU at approximately `64.1 C`, but the HAL reported skin `51.548 C` at status `3`. No cooling device was active in the earlier soak snapshot. Therefore this run proves map integrity and survival only; it must not be used to close FPS, sustained-performance, or release-memory acceptance in `APH-609` or `APH-803`.

## Evidence

- `/private/tmp/aph611_60s.png`
- `/private/tmp/aph611_300s.png` (rejected invalid readback)
- `/private/tmp/aph611_600s.png`
- `/private/tmp/aph611_repeat1.png`
- `/private/tmp/aph611_repeat2.png`
- `/private/tmp/aph611_repeat3.png`
- `/private/tmp/aph611_match_10min.png`
- `/private/tmp/aph611_device_log.txt`
- `/private/tmp/aph611_device_log_10min.txt`
- `/private/tmp/aph611_thermal_10min.txt`
- `/private/tmp/aph611_meminfo_10min.txt`

## Acceptance

`APH-611` is complete for the tested camera and Android device. Full visual-matrix comparison, thermally controlled release metrics, cold startup, package size, and release memory remain required by `APH-609` and `APH-803`.
