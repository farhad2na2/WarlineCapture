# First Launch Editor Performance Report

Status: Passed editor baseline; physical Android device profiling remains required before release lock.
Date: 2026-07-11
Unity: 6000.5.2f1
Platform: OSXEditor

| Measure | Result |
|---|---:|
| Cold FL-P01 Addressables load | 130.052 ms |
| Warm panel transition average | 0.235 ms |
| Warm panel transition maximum | 1.052 ms |
| Stable playback sample | 1800 ticks / 0.447 ms |
| Stable managed allocation after warmup | 0 bytes |
| Resident panel handles after transition | 2 (current + optional next, maximum 2) |
| Current decoded panel texture estimate | 0.88 MiB |
| Referenced temporary voice clips | 17 / 0.66 MiB runtime memory |

## Failure And Route Checks

- Missing optional voice reaches auto-advance without blocking.
- Runtime narrative assemblies contain no network TTS, HTTP, or Resources loading path.
- Development reviewer controls are hidden, non-interactable, and non-raycasting by default.
- Current/next panel Addressables residency remained between one and two handles across every opening transition.

## Scope

These are deterministic Editor measurements on the development Mac. They catch recurring managed allocations and residency regressions, but do not replace Android device frame-time, GPU-memory, thermal, and audio-start profiling.
