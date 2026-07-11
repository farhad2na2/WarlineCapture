# Android Development Performance Gate Plan

Date: 2026-07-11
Tracker item: `APH-803`
Reference device: Xiaomi 24090RA29G, Android 16, 2712x1220.

## Finding

The project has reusable build, launch, profiler, and budget-validation pieces, but it cannot yet enforce a fail-closed current-head Android development gate. Frame evidence is fragmented, runtime diagnostics expose two-second averages rather than exact post-warmup per-frame samples, startup lacks a structured Match-ready timestamp, and accepted p99/startup thresholds are unset.

Historical or manually assembled Android reports must not satisfy this gate.

## Reusable Foundations

- `BuildScript.BuildAndroidProfilerApk`: ARM64 IL2CPP development APK with profiler support.
- `-warlineAutoStartMatch`: deterministic Menu-to-Match routing.
- `PerformanceDiagnosticsSystemHelper`: frame, CPU/GPU, allocation, memory, draw, SetPass, triangle, and vertex sampling.
- `ProfilerCaptureSummaryExporter`: p95/p99 calculation from raw profiler frames.
- `performance_regression_accepted_baseline.json`: authoritative budget source.
- `PerformanceProductBudgetValidator`: Android p95, memory, startup, and provenance validation foundation.
- `AndroidBuildReportGenerator`: commit, hash, and report conventions for clean release builds.

## Smallest Implementation

Add a flag-gated recorder delegated from the existing performance-diagnostics owner. It uses one preallocated sample buffer after warmup and writes a structured JSON sidecar containing per-frame deltas, p95/p99, CPU/GPU timing, peak Unity allocated/Mono memory, and process-to-Match-ready time.

Proposed new files:

1. `Assets/Game/Scripts/Diagnostics/AndroidDevelopmentPerformanceRecorder.cs`
2. `Assets/Tests/Editor/AndroidDevelopmentPerformanceRecorderTests.cs`
3. `Tools/CI/android_development_performance_gate.py`
4. `Tools/CI/android_development_performance_evidence.schema.json`
5. `Tools/CI/android_reference_device_profile.json`
6. `Tools/CI/tests/test_android_development_performance_gate.py`

Required narrow integrations:

- Delegate recorder sampling from `PerformanceDiagnosticsSystemHelper` without adding another player-loop owner.
- Emit the exact Match-ready transition from `MenuBootstrapCompositionSystemHelper`.
- Add approved Android p99 and startup limits to the accepted baseline.
- Extend `PerformanceProductBudgetValidator` and focused tests for the new limits and evidence schema.

The device runner performs five cold starts, five warm starts, and one sustained Match run. It polls thermal and memory state, retrieves the structured artifact, captures a screenshot and crash log, and emits deterministic JSON/Markdown evidence. Jenkins integration remains deferred until a reference-device agent has exclusive USB ownership.

## Fail-Closed Contract

Reject when any of the following is true:

- Device identity, APK hash, clean commit, ARM64/IL2CPP development type, resolution, quality tier, or frame mode differs from the declared profile.
- A required artifact, field, startup sample, screenshot, raw log, or provenance value is absent, stale, or malformed.
- Warmup or measurement duration is short, or startup frames contaminate frame percentiles.
- p95 is not strictly below 33 ms.
- p99 or startup limits remain unset or are exceeded.
- Peak allocated memory exceeds 967.5 MB, the accepted same-device 10% reduction target from the 1075 MB baseline ceiling.
- Thermal status or cooling-device value is nonzero, or thermal output cannot be parsed.
- The process exits, crash/fatal markers appear, or measurements come only from aggregate diagnostic log lines.

Temperatures are recorded but do not independently fail until an explicit temperature budget is approved.
