# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

| Metric | Value |
|---|---:|
| Observation seconds | 4.01 |
| Frame count | 557 |
| Average frame ms | 7.20 |
| P95 frame ms | 11.48 |
| Editor P95 frame budget ms | 20.00 |
| Editor P95 frame budget passed | yes |
| P99 frame ms | 13.16 |
| Max frame ms | 34.98 |
| Current-thread allocated bytes | 0 |
| Current-thread allocation budget bytes | 0 |
| Units | 733 |
| Minimum units | 700 |
| Runtime buildings | 628 |
| Minimum runtime buildings | 600 |
| Projectiles | 0 |
| Markers | 22 |
| Visible model estimate | 46 |
| Minimum visible model estimate | 40 |

## Runtime Status

- Accepted baseline: `Design/Architecture/performance_regression_accepted_baseline.json`
- Metrics artifact: `Design/AgentReports/performance_regression_match_baseline.json`
- Ready: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`
- Stable: `playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=733 performanceFixture=ready addedBuildings=177 addedUnits=76 sourceEntities=733 buildings=628 renderStates=105 culledUnits=59`

The editor P95 budget is intentionally lenient and catches large regressions only; Android device development/release lanes remain the mobile rendering-performance gates.
