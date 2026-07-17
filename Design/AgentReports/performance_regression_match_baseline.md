# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

- Exact commit: `cd6e764bd878c6d7cedcbaa3c5034f0f105825b6`
- Environment identity SHA-256: `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`
- Dirty at capture start: `false`
- Quality: `Mobile` (index `1`)
- Resolution: `640x480`
- Instrumentation: `frameSampler=stopwatch profilerEnabled=false deepProfiling=false instrumentationOffControl=not-required-stopwatch-only`
- Target frame rate: `-1`; vSync count: `0`

| Metric | Value |
|---|---:|
| Observation seconds | 4.00 |
| Frame count | 1442 |
| Average frame ms | 2.78 |
| P95 frame ms | 4.12 |
| Editor P95 frame budget ms | 20.00 |
| Editor P95 frame budget passed | yes |
| P99 frame ms | 4.50 |
| Max frame ms | 14.32 |
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
