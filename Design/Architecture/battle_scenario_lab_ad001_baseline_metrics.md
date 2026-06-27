# AD-001 Baseline Metrics

This file records the current measured AD-001 behavior before any Phase 6 tuning.
It is a measurement artifact only; it does not change live combat balance.

- Scenario: `AD-001_AirMissileLauncher_InterceptIncomingGroundMissile_RadarComparison`
- Generated UTC: `2026-06-26T19:30:39.7823210Z`
- Fixed delta time: `0.05`
- Passed: `true`
- Failure reason: `None`

## Variant Metrics

| Variant | Radar | Detected | Detection | Locked | Lock | Launched | Launch | Intercepted | Intercept | Closest Distance | Effective Range | Effective Lock | Tracking Quality |
| --- | --- | --- | ---: | --- | ---: | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| `AD-001-A-NoSupport-Normal` | no | yes | 0.00s | yes | 0.90s | yes | 0.95s | yes | 1.85s | 5.142 | 140 | 0.90s | 0.75 |
| `AD-001-B-RadarNear-Normal` | yes | yes | 0.00s | yes | 0.45s | yes | 0.50s | yes | 1.45s | 8.692 | 230 | 0.45s | 0.95 |
| `AD-001-C-NoSupport-FastThreat` | no | yes | 1.20s | yes | 2.10s | yes | 2.15s | yes | 2.70s | 4.744 | 140 | 0.90s | 0.75 |
| `AD-001-D-RadarNear-FastThreat` | yes | yes | 0.00s | yes | 0.45s | yes | 0.50s | yes | 1.65s | 6.494 | 230 | 0.45s | 0.95 |

## Radar Comparisons

| Baseline | Supported | Detection Delta | Lock Delta | Detection Improved | Lock Improved | Outcome Improved/Matched |
| --- | --- | ---: | ---: | --- | --- | --- |
| `AD-001-A-NoSupport-Normal` | `AD-001-B-RadarNear-Normal` | +0.00s | -0.45s | yes | yes | yes |
| `AD-001-C-NoSupport-FastThreat` | `AD-001-D-RadarNear-FastThreat` | -1.20s | -1.65s | yes | yes | yes |

## Current Target Outcomes

- Normal radar-near variant should intercept.
- Radar-near normal should improve lock time and match or improve the no-support normal outcome.
- Radar-near fast-threat should improve detection and match or improve the no-support fast-threat outcome.
- No tuning is approved by this baseline capture.
