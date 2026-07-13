# APH-803 Android Runtime Recorder

Date: 2026-07-12
Tracker item: `APH-803`
Status: Stable runtime-recorder slice; one diagnostic reference-device run completed, while clean acceptance evidence remains required.

## Result

- Added a plain, flag-gated recorder, now generalized as `AndroidPerformanceRecorder` for APH-803/804 compatibility; it does not add a `MonoBehaviour`, `SystemBase`, `ISystem`, or second player-loop owner.
- `PerformanceDiagnosticsSystemHelper` delegates one sample call from its existing frame boundary and exposes one Match-ready forwarding call.
- `MenuBootstrapCompositionSystemHelper` marks the exact first `MatchHud` transition.
- The recorder activates only in a development build with `-warlineAndroidPerformanceGate`.
- Disabled runs allocate none of the recorder's frame-timing or sustained-sample buffers.
- Enabled runs allocate fixed-capacity frame, CPU, and GPU sample buffers once, wait for 60 seconds of active focused Match time, then capture 600 seconds without per-frame collection growth.
- Completion writes one JSON artifact under `Application.persistentDataPath/WarlineCapture/Diagnostics/aph803_android_development_recorder.json`.
- The artifact records the process-relative Match-ready time, exact per-frame deltas, recomputed p95/p99/maximum frame time, p95 CPU/GPU time, and peak Unity allocated/Mono memory.
- Early disposal or sample-capacity exhaustion writes `complete=false`; the evidence gate rejects incomplete, timingless, mismatched, missing, or tampered recorder artifacts.
- Jenkins now runs an unconditional offline APH-803 contract preflight immediately after checkout and before resolving or invoking Unity. The preflight validates the profile/schema identity, runs the existing Python evidence suite, generates a revision-bound collection contract with a non-acceptance placeholder APK hash, requires the exact contract marker and valid JSON output, and archives the contract plus log. It invokes neither Unity nor ADB; physical-device acceptance remains a separate serialized lane.

## Architecture And Performance Contracts

- Existing diagnostics ownership remains unchanged; no new update owner polls runtime state.
- No runtime class uses a forbidden broad role suffix such as `Controller`, `Player`, `Manager`, or `Coordinator`.
- The recorder is a managed diagnostics edge because JSON/file I/O is intentionally deferred until capture completion, outside the measured frame loop.
- Normal builds retain one disabled method branch and no recorder arrays.
- `PerformanceDiagnosticsSystemHelper` remains below its frozen `909 lines / 39,101 bytes` ceiling at `907 / 39,098` after APH-804 generalization.
- `MenuBootstrapCompositionSystemHelper` remains at `917` lines and `37,959` bytes, below its approved `932 / 38,015` ceiling.

## Validation

- `python3 -m unittest Tools.CI.tests.test_android_development_performance_gate`: passed `18/18`.
- Python syntax compilation passed with `PYTHONPYCACHEPREFIX=/private/tmp/warline-pycache`.
- Offline CI source-contract and evidence tests passed `25/25`, covering stage ordering, fail-closed Python resolution/nonzero propagation, profile/schema/gate inputs, required JSON and pass marker, artifact archival, and the prohibition on Unity/ADB invocation.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`: passed with zero errors.
- `dotnet build Game.Composition.csproj --no-restore -v:q -clp:ErrorsOnly`: passed with zero errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`: passed with zero errors.
- Task-owned `git diff --check`: passed.
- The original focused Unity recorder validation passed `5/5`; the generalized APH-803/804 compatibility suite now passes `16/16` with zero compiler errors. Logs: `/private/tmp/warline-aph803-recorder-focused-r2.log` and `/private/tmp/warline-aph804-recorder-focused-final.log`.
- Existing diagnostics allocation regression passed `4/4`. Log: `/private/tmp/warline-aph803-performance-diagnostics-regression.log`.
- Assembly boundary validation passed `31/31`. Log: `/private/tmp/warline-aph803-assembly-boundary.log`.
- Source growth reports only the separately owned 548-line audio presentation file and Commander UI growth; no recorder-owned file violates a ratchet. Log: `/private/tmp/warline-aph803-source-growth.log`.
- The first focused Unity attempt hit a transient Bee/IL post-processing race outside the recorder assembly; the serialized retry passed. Log: `/private/tmp/warline-aph803-recorder-focused.log`.

## Diagnostic Reference-Device Run

- Device: Xiaomi `24090RA29G` (`malachite`), ARM64, Android 16.
- Revision: `8f75852919014c7dbc60a27646fbc690b281074c` plus a dirty worktree; this is diagnostic evidence and not an accepted revision baseline.
- APK: development IL2CPP profiler build, SHA-256 `cda77ed0b2edad4b56fe1afb6741cbb41135512deba7b78b615c8788aa177d35`.
- Launch: `-warlineAutoStartMatch -warlineProfilerMarkers -warlineAndroidPerformanceGate`; Match HUD ready in `19,005.75 ms` from process launch.
- Recorder: `complete=true`, empty failure, 60.02-second warmup, 600.01-second capture, 27,788 frame/CPU samples, and 21,959 GPU samples.
- Frame time: average `21.59 ms`, p95 `32.83 ms`, p99 `41.61 ms`, maximum `65.61 ms`; CPU p95 `31.83 ms`, GPU p95 `25.59 ms`.
- Memory: peak Unity allocated `1,084.15 MB` and peak Mono `60.38 MB`; Unity allocation exceeds the provisional `967.5 MB` limit.
- Thermal: aggregate status remained `0`, but the live skin sensor reached `51.77 C` and status `3`; the run is thermally contaminated and cannot establish an accepted sustained baseline.
- Survival and visuals: the process remained alive through recorder completion and the final screenshot retained a coherent, nonblank Match world and HUD.
- Local evidence: `/private/tmp/warline-aph803-android-recorder.json` (SHA-256 `f3dae395b87db7617e7486bc0246974d101ef24d1454e0a3c240f174cd8d9e52`), `/private/tmp/warline-aph803-final.png` (SHA-256 `7ff024ca65cbebe233e1c7dff54777b24bf94b5cbaf069f61bfa4c5fa8f4fbf8`), `/private/tmp/warline-aph803-thermal-after.txt`, and `/private/tmp/warline-aph803-android-device-full.log`.
- Outcome: recorder/device integration passed; development performance acceptance failed for provenance, incomplete five-cold/five-warm startup evidence, peak memory, thermal contamination, and intentionally unset startup/p99 limits.

## Remaining Work

1. Return the worktree to an accepted clean revision, rebuild the ARM64 IL2CPP development APK, and cool the reference device before capture.
2. Collect five cold starts, five warm starts, and a thermally valid 60-second warmup plus 600-second sustained run with thermal snapshots, process survival, raw log, and screenshot evidence.
3. Approve p99 and startup p95 limits from repeated reference-device evidence; both intentionally remain `measurement-required`.
4. Run the fail-closed evidence gate and only then close `APH-803`.
