# Full Voice Policy Android Audio Smoke

Date: 2026-07-11
Tracker item: `APH-408`
Result: objective device smoke passed; subjective listening confirmation remains open.

## Scope

The smoke validates the category-wide 163-clip on-demand Voice importer policy after the APH-405 pilot and APH-406 rollout. It does not reuse the pilot APK.

| Field | Value |
|---|---|
| Source commit | `2653d43ea` plus deterministic static-map manifest refresh |
| APK | ignored `Build/AndroidProfiler/WarlineCapture-Profiler.apk` |
| APK SHA-256 | `959da0fbc8c6e82073370fc18c41cd0a21896e20c283e7dc8b72bd8717ebc4ea` |
| Device | Xiaomi 24090RA29G, Android 16 |
| Launch | `-warlineAutoStartMatch` |
| Observation | 90 seconds after launch |

## Editor Gates

- Audio config/catalog/importer contract: `14/14` passed across all 234 catalog clips.
- Audio performance contract: `4/4` passed.
- Audio scene/listener binding contract: `6/6` passed.
- Runtime and Editor-test assemblies compile with zero errors.
- Complete Python CI suite: `49/49` passed.

## Device Evidence

- The process remained alive after the observation window.
- Match static-map ownership initialized and suppressed the 17,564 canonical presentation renderers as expected.
- Android opened the application audio output path at 24 kHz stereo.
- AudioTrack reported non-zero `fine` output intervals and non-zero maximum amplitude, proving that audible-range samples reached the device output path.
- No duplicate/missing AudioListener warning, missing clip error, fatal exception, ANR, or application crash was present in the captured log.

| AudioFlinger counter | Before | After | Delta |
|---|---:|---:|---:|
| Primary partial underruns | 0 | 0 | 0 |
| Primary empty underruns | 0 | 0 | 0 |
| Primary delayed writes | 0 | 0 | 0 |
| Existing device track underruns | 21 | 21 | 0 |

The one startup-time HAL write warning occurred while opening the device stream and did not increment the before/after delayed-write or underrun counters.

## Evidence Boundary

Objective playback-path, stability, listener, and underrun evidence passes. This agent cannot independently judge speaker/headphone clarity, clipping, or perceived artifacts. APH-408 remains active until a human listening confirmation is recorded against this APK or a later same-policy artifact.

Local transient evidence:

- `/private/tmp/warline-aph408-audio-performance.log`
- `/private/tmp/warline-aph408-audio-scene-binding.log`
- `/private/tmp/warline-aph408-android-build-r2.log`
- `/private/tmp/aph408-device-log.txt`
- `/private/tmp/aph408-audio-flinger-before.txt`
- `/private/tmp/aph408-audio-flinger-after.txt`
