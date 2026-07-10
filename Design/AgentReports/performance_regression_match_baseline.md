# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

| Metric | Value |
|---|---:|
| Observation seconds | 4.00 |
| Frame count | 1613 |
| Average frame ms | 2.49 |
| P95 frame ms | 3.82 |
| Editor P95 frame budget ms | 50.00 |
| Editor P95 frame budget passed | yes |
| P99 frame ms | 5.83 |
| Max frame ms | 14.52 |
| Current-thread allocated bytes | 0 |
| Current-thread allocation budget bytes | 0 |
| Units | 733 |
| Minimum units | 700 |
| Runtime buildings | 628 |
| Minimum runtime buildings | 600 |
| Projectiles | 0 |
| Markers | 98 |
| Visible model estimate | 46 |
| Minimum visible model estimate | 40 |

## Runtime Status

- Accepted baseline: `Design/Architecture/performance_regression_accepted_baseline.json`
- Metrics artifact: `Design/AgentReports/performance_regression_match_baseline.json`
- Ready: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`
- Stable: `playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=733`

The editor P95 budget is intentionally lenient and catches large regressions only; Android device development/release lanes remain the mobile rendering-performance gates.
