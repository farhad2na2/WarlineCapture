# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

| Metric | Value |
|---|---:|
| Observation seconds | 4.00 |
| Frame count | 800 |
| Average frame ms | 5.02 |
| P95 frame ms | 7.19 |
| Editor P95 frame budget ms | 50.00 |
| Editor P95 frame budget passed | yes |
| P99 frame ms | 8.51 |
| Max frame ms | 18.63 |
| Current-thread allocated bytes | 0 |
| Current-thread allocation budget bytes | 0 |
| Units | 740 |
| Minimum units | 700 |
| Runtime buildings | 630 |
| Minimum runtime buildings | 600 |
| Projectiles | 0 |
| Markers | 99 |
| Visible model estimate | 50 |
| Minimum visible model estimate | 40 |

## Runtime Status

- Accepted baseline: `Design/Architecture/performance_regression_accepted_baseline.json`
- Metrics artifact: `Design/AgentReports/performance_regression_match_baseline.json`
- Ready: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`
- Stable: `playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=740`

The editor P95 budget is intentionally lenient and catches large regressions only; Android device development/release lanes remain the mobile rendering-performance gates.
