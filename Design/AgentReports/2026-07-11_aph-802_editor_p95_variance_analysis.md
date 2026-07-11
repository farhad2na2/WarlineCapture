# APH-802 Editor P95 Variance Analysis

Date: 2026-07-11
Scope: report-only review of existing tracked editor performance evidence
Decision: **Do not ratchet the current 50 ms editor p95 budget yet.**

> Resolution: this initial four-run historical analysis is superseded by the accepted same-revision series under `Design/AgentReports/aph802/2026-07-11_d2a41ac97/`. Five fresh-process captures at one exact commit established a `9.087-12.905 ms` p95 range and `14.2792%` coefficient of variation. Accepted-baseline version 4 therefore ratchets the Editor p95 budget to `20 ms`; the post-ratchet canonical gate passed at `4.495 ms` p95 with zero current-thread allocation.

## Requirement

`APH-802` permits the editor p95 budget to move below its intentionally lenient `50 ms` value only after at least five stable captures establish variance. A Markdown summary and its JSON representation describe one capture, not two independent samples.

## Comparability Criteria

A capture is accepted into the ratchet sample only when all of the following are known and consistent:

1. Editor Match lane, not Android/device, focused-system, terrain-only, startup, or user-recorded free-play data.
2. The same execute method and frame-time measurement semantics.
3. The Match HUD ready/stable gates pass before measurement.
4. A four-second steady observation with at least 700 units and 600 runtime buildings.
5. No current-thread allocation in the measured path.
6. An independent execution, rather than a duplicate JSON/Markdown rendering of one run.
7. An unchanged revision, Unity version, machine, quality configuration, and capture mode for the variance series.

The tracked artifacts currently establish criteria 1-6 for four historical runs of the current runner. They do not establish criterion 7 across those runs, so they are useful trend evidence but are not a completed stable variance series.

## Exact Current-Runner Inputs

All four entries use `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`, a roughly four-second observation, the required ready/stable Match state, at least 733 units, at least 628 runtime buildings, and zero current-thread allocated bytes.

| Capture artifact | Artifact commit and date | Units / buildings | Frames | Average ms | P95 ms | P99 ms | Max ms | Comparable status |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `performance_regression_match_baseline.json` at `e03e43e15290795896df00bee20f603a62c50a08` | 2026-07-06 16:25 +0200 | 740 / 630 | 897 | 4.468 | 6.443 | 7.954 | 18.370 | Candidate 1; exact runner, different revision |
| Same tracked JSON at `f426f3bf391323bcffbda2e2bcd91e6ef5297b70` | 2026-07-06 16:55 +0200 | 740 / 630 | 800 | 5.016 | 7.186 | 8.507 | 18.631 | Candidate 2; exact runner, different revision |
| Same tracked JSON at `ba3da6704842de77461501b5f943b06b45c78fc2` | 2026-07-10 00:04 +0200 | 733 / 628 | 1,173 | 3.415 | 5.222 | 6.873 | 18.126 | Candidate 3; exact runner after performance changes |
| Current tracked JSON, introduced at `77f7581da067306bee45dd4ae78cfc5997f05f66` | 2026-07-10 10:19 +0200 | 733 / 628 | 1,613 | 2.486 | 3.824 | 5.826 | 14.523 | Candidate 4; exact runner after further changes |

The current Markdown file, `Design/AgentReports/performance_regression_match_baseline.md`, is a presentation of candidate 4 and is not an additional capture.

## Observed Variance

For the four exact-runner candidates:

| Statistic | P95 frame time |
|---|---:|
| Sample count | 4 |
| Minimum | 3.824 ms |
| Maximum | 7.186 ms |
| Mean | 5.669 ms |
| Median | 5.833 ms |
| Sample standard deviation | 1.472 ms |
| Coefficient of variation | 25.97% |
| Absolute range | 3.362 ms |
| Range / mean | 59.31% |

These values must not be treated as stationary noise. The sequence crosses code and fixture-state changes: the first two captures contain 740 units and 630 buildings, while the last two contain 733 units and 628 buildings and follow performance/assistant implementation changes. The downward movement is therefore a product trend mixed with environment variance, not a controlled estimate of repeatability.

## Other Tracked Editor Evidence Considered

The following reports contain useful editor measurements but are excluded from the ratchet sample:

| Input | Measurements considered | Exclusion reason |
|---|---|---|
| `Design/AgentReports/ecs_burst_hot_path_baseline_2026-06-12.md` | Earlier `RunBaselineMetrics` stable Match capture: p95 `12.563 ms`, p99 `22.031 ms`, 745 units, 647 buildings | Earlier execute method and fixture; not the current budget-gate runner |
| `Design/AgentReports/2026-06-13_ecs-burst-hot-path-final-report.md` | Stable Match p95 values `11.928 ms` and `17.139 ms` | Optimization-in-progress revisions; rerun metadata is incomplete for a controlled variance series |
| `Design/AgentReports/2026-06-13_ecs-burst-max-coverage-final-validation.md` | Runtime rerun p95 values `29.771`, `13.293`, `13.365`, `11.883`, and `12.194 ms` | Earlier runner/fixture; first run is a documented outlier and individual runs do not record a complete environment/content identity |
| `Design/AgentReports/2026-06-11_perf_match-editor-profiler-baseline-analysis.md` | User-recorded Play Mode capture of about 14,235 frames | Free-play battle profile with different measurement semantics; no p95 is reported |

Android performance summaries, Android build reports, focused subsystem timings, and terrain comparison reports were inspected by category and excluded because they do not measure the editor Match p95 budget lane.

## Decision

There are **four exact-runner captures, not five**, and there are **zero documented five-run samples at one unchanged revision and environment**. The current evidence therefore does not satisfy APH-802. Keep the editor p95 budget at `50 ms`; this report does not propose or apply a replacement value.

## Next Capture Requirements

Create a new ratchet evidence set with at least five accepted independent executions under one frozen revision:

1. Use `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline` for every run.
2. Record the Git commit, Unity version, OS, machine identifier, graphics device, quality level, resolution, batch/windowed mode, and command line in every artifact.
3. Start each capture in a fresh Unity process and use the same project/cache state policy for all runs.
4. Require the existing Match HUD ready/stable gates, at least 700 units, at least 600 runtime buildings, four seconds of observation, and zero current-thread allocation.
5. Preserve each run as a separately named JSON/Markdown pair instead of overwriting the canonical latest-result files.
6. Reject runs with compiler errors, loading/startup frames inside the sample, incomplete content, editor interaction, thermal contention, or materially different background load; record every rejection.
7. After five accepted captures, calculate min, max, mean, median, sample standard deviation, coefficient of variation, p95-of-run-p95 values, and outliers using a declared rule. Ratchet only if the series is stationary and the proposed threshold retains explicit regression headroom above the worst accepted run.

Capturing seven runs is preferable: preserve all seven, declare any exclusions, and still require at least five accepted comparable samples. Android budgets remain independent and must not be inferred from this editor series.
