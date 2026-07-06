# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

| Metric | Value |
|---|---:|
| Observation seconds | 4.00 |
| Frame count | 874 |
| Average frame ms | 4.59 |
| P95 frame ms | 6.24 |
| Editor P95 frame budget ms | 50.00 |
| Editor P95 frame budget passed | yes |
| P99 frame ms | 7.65 |
| Max frame ms | 20.06 |
| Current-thread allocated bytes | 0 |
| Units | 740 |
| Runtime buildings | 630 |
| Projectiles | 0 |
| Markers | 99 |
| Visible model estimate | 50 |

## Runtime Status

- Ready: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`
- Stable: `playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=740`

The editor P95 budget is intentionally lenient and catches large regressions only; Android device development/release lanes remain the mobile rendering-performance gates.
