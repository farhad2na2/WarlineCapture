# S026 retry drift phase trace

This recipe adds opt-in snapshots at the existing source-host `path` and
`movement` phase boundaries. It observes one stable actor, `role.rifle.1.0.5`,
for ticks 220–310, before and after each phase. Fields include grid cell,
transform, target, pending path request, path range/follow index, and retry
cooldown. The trace is observational: phase boundaries do not isolate a
specific system's causal contribution.

Prepare from the S026 resume receipt overlay manifest:

```sh
python3 -B Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_drift_phase_trace/prepare_manifest.py \
  --manifest 'T Logs/SkirmishProduction/s026-resume-receipt-overlay-20261113-r1/source-manifest.json' \
  --output /private/tmp/s026-retry-drift-phase-manifest.json
python3 -B Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_drift_phase_trace/generate.py \
  --repo /Volumes/T7/Projects/WarlineCapture \
  --output /private/tmp/s026-retry-drift-phase-overlay
```

Build the prepared manifest through the ordinary source-only CLI:

```sh
python3 Tools/CI/run_skirmish_source_simulation.py build \
  --mission S026 --seed 104755 \
  --manifest /private/tmp/s026-retry-drift-phase-manifest.json \
  --output /private/tmp/s026-retry-drift-phase-build
```

The build invokes the declared generator and compiles
`S026RetryDriftPhaseOverlay.cs`. For a later bounded diagnostic, pass that
build's `build.json` as `--build-receipt`, the matching pinned data bundle, and
`--max-ticks 310` to the normal `run` command. Enable with
`WARLINE_S026_RETRY_DRIFT_PHASE_TRACE=1`; output is one bounded JSON line on
stderr prefixed `[S026RetryDriftPhaseTrace]`. The prepared manifest pins the
actual resume overlay's paid-owner adapter. Run only after building that
manifest through the ordinary source-simulation CLI; do not treat this trace
as native parity or readiness evidence. The instrumented run is not for
performance comparisons.

Focused generator tests:

```sh
python3 -B -m unittest discover -s Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_drift_phase_trace -p 'test_*.py' -v
```
