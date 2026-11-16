# S026 retry lifecycle trace

This recipe adds a diagnostic-only overlay to the current original retry code. It observes retry creation, retry cooldown clearing, and manual or long-distance path request restoration for the four stable IDs adjacent to the first retained S026 baseline drift: `role.rifle.2.6.5` through `.8`. It does not edit production sources or change job query filters or retry decisions.

The trace is opt-in with `WARLINE_S026_RETRY_TRACE=1`. It writes one bounded JSON record to stderr at process exit, prefixed by `[S026RetryLifecycleTrace]`. Fields contain stable ID, event, engine frame, resume frame, and goal cell. Overflow is explicit. Event ordering within a frame is unspecified because the existing jobs execute in parallel. The trace does not include simulation tick; correlate engine frames with validator snapshots, and do not interpret frame alignment as proof of causality.

Prepare an overlay from the current S026 plan manifest:

```sh
python3 -B Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_lifecycle_trace/prepare_manifest.py \
  --manifest Tools/CI/SkirmishSourceSimulation/source_manifest.s026.plan.json \
  --output /private/tmp/s026-retry-trace-manifest.json
python3 -B Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_lifecycle_trace/generate.py \
  --repo /Volumes/T7/Projects/WarlineCapture --output /private/tmp/s026-retry-trace-overlay
```

The generated files are `SourceUnitPathRetryTrace.cs` and `SourceUnitManualMoveRetryTrace.cs`; use them as the overlay outputs declared by the prepared manifest in a fresh source-only build. Pin the build, bundle, overlay receipt, request, trace stderr, and validator snapshots together. Run a bounded diagnostic through the ordinary source simulation wrapper with the same pinned S026 seed/configuration and a window covering the target validator snapshot. Compare against the retained baseline prefix; this is diagnostic evidence only and does not establish native parity or readiness. Do not compare runtime performance from this instrumented run.

Run the focused generator checks without launching Unity or a simulation:

```sh
python3 -B -m unittest discover -s Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_lifecycle_trace -p 'test_*.py' -v
```
