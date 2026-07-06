# Performance Regression Match Baseline

Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

| Metric | Value |
|---|---:|
| Observation seconds | 4.00 |
| Frame count | 903 |
| Average frame ms | 4.43 |
| P95 frame ms | 6.52 |
| P99 frame ms | 8.00 |
| Max frame ms | 17.19 |
| Current-thread allocated bytes | 0 |
| Units | 740 |
| Runtime buildings | 630 |
| Projectiles | 0 |
| Markers | 99 |
| Visible model estimate | 50 |

## Runtime Status

- Ready: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`
- Stable: `playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=740`

Budget assertions are intentionally deferred to the next Phase 11 slice after this capture path is accepted.
