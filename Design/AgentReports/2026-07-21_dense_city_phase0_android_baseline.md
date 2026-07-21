# Dense City Phase 0 Android Baseline

## Scope

- Commit: `8f93dafe06098e62ef029260ed4c6a732ba2ee1b` (dirty worktree)
- APK SHA-256: `dab470fd296b9f2ca2866ba6042940137cce36beabaa886bdf2ce7dab9f8bc44`
- APK bytes: 459,188,380
- Installed approximate bytes (`du -sb` base.apk + lib): 631,573,700
- Device: Xiaomi `24090RA29G`, serial `R4M7PZEQZ58T59ZH`, Android 16, `arm64-v8a`
- Connection: wired USB
- Capture script: `Tools/CI/dense_city_phase0_android_baseline_capture.py`
- Machine-readable report: `Design/AgentReports/2026-07-21_dense_city_phase0_android_baseline.json`
- Transient raw dumps: `/private/tmp/dense-city-phase0-android-baseline/`

## Runtime Evidence

| Measurement | Value |
|---|---:|
| Match TOTAL PSS | 2,489,021 KiB |
| Match TOTAL RSS | 2,626,948 KiB |
| Match Graphics | 1,096,532 KiB |
| Match Native Heap | 63,460 KiB |
| Steady TOTAL PSS | 2,492,220 KiB |
| Menu after unload TOTAL PSS | 1,268,879 KiB |
| Match-ready upper bound | 40.79 s |
| Unload force-stop → menu | 16.08 s |
| SurfaceFlinger sample windows | 60 |
| Average FPS | 58.42 |
| Minimum FPS | 30.59 |
| 10th percentile FPS | 60.50 |
| 90th percentile FPS | 60.98 |
| Maximum FPS | 61.03 |
| Average max queue interval | 34.00 ms |
| Maximum queue interval | 49.26 ms |

## Notes

1. This is Phase 0 characterization for the current static-presentation revision, not Phase 9 acceptance.
2. Installed size uses `du -sb` on `base.apk` + `lib` because MIUI `dumpsys package` omits `codeSize`/`dataSize`/`cacheSize`.
3. FPS uses SurfaceFlinger BLAST-layer `--latency` (median inter-frame interval after clearing Long.MAX_VALUE pending slots). Unity `gfxinfo` remains empty on this device.
4. Draw-call, triangle, update/main/GPU ms, and GC markers were not present in release-APK logcat for this capture; those fields remain null and are not claimed as measured.
5. Versus the 2026-07-19 characterization (~3.05M KiB match PSS / ~38 FPS), this revision shows lower match PSS and higher sampled FPS under the same device and auto-start match path. Camera pose and diagnostic overlays were not locked to the July 19 view.

## Recommended Next Slice

1. Continue Phase 0A non-mutating work (vehicle ECS already-produced proof, `RuntimeBuildingEntity` dependency inventory, attached-visual scaffolding).
2. Keep GPT-only gates for first scene ownership mutation, gameplay conversion, and production cutover.
