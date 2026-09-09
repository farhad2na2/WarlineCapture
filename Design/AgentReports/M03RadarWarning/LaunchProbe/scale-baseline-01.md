# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

- Exact commit: `b6b8153d241786b8e368a3910f07fb435396805e`
- Environment identity SHA-256: `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`
- Dirty at capture start: `true`
- Quality: `Mobile` (index `1`)
- Resolution: `1870x1002`
- Instrumentation: `frameSampler=stopwatch profilerEnabled=true deepProfiling=false instrumentationOffControl=not-required-stopwatch-only`
- Target frame rate: `-1`; vSync count: `0`

| Metric | Value |
|---|---:|
| Observation seconds | 4.00 |
| Frame count | 541 |
| Average frame ms | 7.41 |
| P95 frame ms | 8.77 |
| Editor P95 frame budget ms | 20.00 |
| Editor P95 frame budget passed | yes |
| P99 frame ms | 11.08 |
| Max frame ms | 23.77 |
| Current-thread allocated bytes | 0 |
| Current-thread allocation budget bytes | 0 |
| Units | 5082 |
| Minimum units | 700 |
| Runtime buildings | 4977 |
| Minimum runtime buildings | 600 |
| Projectiles | 0 |
| Markers | 264 |
| Visible model estimate | 47 |
| Minimum visible model estimate | 40 |

## Runtime Status

- Accepted baseline: `Design/Architecture/performance_regression_accepted_baseline.json`
- Metrics artifact: `Design/AgentReports/performance_regression_match_baseline.json`
- Ready: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1 matchStart=status:Started,pending:0,started:1,progress:1.00,detail:Gameplay start completed from loaded Match scene. scene=Matc matchView=present,bound:1,mapReady:1,mapProgress:1.00,mapFailureCode:None,mapFailure:none,presentation:EntityScene`
- Stable: `playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=5082 performanceFixture=ready addedBuildings=0 addedUnits=83 sourceEntities=5082 buildings=4977 renderStates=105 culledUnits=59`

The editor P95 budget is intentionally lenient and catches large regressions only; Android device development/release lanes remain the mobile rendering-performance gates.
