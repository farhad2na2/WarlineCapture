# AM-001 Entry Prerequisite Review

## Decision

**Core Architecture Lane entry: accepted.**

**Release Certification Lane: deferred. Release certification is not claimed.**

The bounded authorities record `107 / 107` explicit prerequisite dispositions, with `92` accepted and `15` transferred without acceptance to pre-release certification. They record zero red architecture gates, zero red performance gates, and accepted compiler, architecture, critical behavior, Editor performance, and GC closeout groups. The 15 release-only obligations remain unpassed and fail closed, so they do not block current Core Architecture Lane work and do block any release-certification claim.

## Artifact Identity

| Field | Value |
|---|---|
| Schema version | `1` |
| Task | `AM-001` |
| Review method | Bounded repository evidence review |
| Review date | 2026-07-16 |
| Reviewed branch | `codex/am-001-prerequisite-review` |
| Reviewed baseline commit | `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` |
| Reviewed `origin/main` commit | `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` |
| Baseline relationship | Exact match |
| Reviewed tree | `521f2cae9d608bbc44b413b7b0172b16301a6b9a` |

## Bounded Authorities

- `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`
- `Design/AgentReports/architecture_performance_dashboard.json`
- `Design/AgentReports/architecture_performance_hardening_final_report.md`
- `Design/AgentReports/performance_regression_match_baseline.json`
- `Design/AgentReports/performance_regression_match_baseline.md`
- `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` (`Progress Snapshot` and `Program Closeout` only)
- `Design/Architecture/pre_release_performance_certification_backlog.md`

No recursive repository evidence search is part of this revision.

## Revision Boundaries

| Identity | Revision | Meaning |
|---|---|---|
| AM-001 review baseline | `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` | Exact repository state reviewed by AM-001 |
| Tracker last verified | `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` | Historical tracker verification identity, not the AM-001 review baseline |
| Tracker architecture implementation | `98cb2bb6d4321132574ebc5247aa3f2c93359eac` | Historical architecture implementation identity, not an AM-001 measurement |
| Final-report audit baseline | `b66453e979d847c3f05d61155266b166650b8df5` | Original hardening comparison baseline |
| Final-report closeout working baseline | `1548ee09318305f885b90b9c9e122782f9e7c62f` | Historical closeout working identity, not the AM-001 review baseline |
| Dashboard revision | `e9dcf183eb95637bb1975c97b11f5c43a1cb9e79` | Dashboard generation identity, not a measurement revision |
| Direct Editor measurement revision | Unknown | `performance_regression_match_baseline.json/.md` declares no commit |
| Direct GC measurement revision | Unknown | The GC call-stack report declares no commit |

AM-001 does not represent historical closeout measurements as measurements of `1a0f1ece...`. Later Phase 0 owners must produce exact-commit environment and refreshed gate/dashboard evidence before Phase 0 exit.

## Scope

Writes are limited to:

- `Design/AgentReports/ArchitectureMaturity/entry_prerequisite_review.json`
- `Design/AgentReports/ArchitectureMaturity/entry_prerequisite_review.md`

No shared tracker, operation-map, FirstLaunch, audio, UI visual-lock, production code, scene, prefab, package, or `ProjectSettings` path was changed. Production behavior is unchanged.

## Core Gates

| Gate ID | Gate | Status | Accepted closeout result | Authority |
|---|---|---|---|---|
| `CORE-ARCHITECTURE` | Architecture | Accepted at prerequisite closeout; not rerun by AM-001 | Integrated architecture `23 / 23`; source growth `15 / 15`; ECS/Burst `10 / 10`; assembly boundary `31 / 31` | Final report; tracker Progress Snapshot and Program Closeout |
| `CORE-COMPILER` | Compiler | Accepted at prerequisite closeout; not rerun by AM-001 | All 12 first-party assemblies built with `0` errors | Final report; tracker Program Closeout |
| `CORE-CRITICAL-BEHAVIOR` | Critical behavior | Accepted at prerequisite closeout; not rerun by AM-001 | Critical PlayMode flows `5 / 5`; static-map structural parity `2 / 2` | Final report; tracker Program Closeout |
| `CORE-EDITOR-PERFORMANCE` | Editor performance | Accepted at prerequisite closeout; not remeasured at review baseline | 733 units, 628 buildings, 557 frames; `7.203 ms` average, `11.477 ms` p95, `13.16 ms` p99, zero current-thread allocation; `20 ms` p95 budget passed | Direct baseline JSON/Markdown; final report; tracker Progress Snapshot |
| `CORE-GC-CLOSEOUT` | GC closeout | Accepted at prerequisite closeout; not remeasured at review baseline | `292 / 1,024` player-relevant bytes over 300 frames after 180 warmup frames | GC call-stack report; final report; tracker Progress Snapshot |

The critical flows are Menu-to-Match-to-Menu, selection/move/attack, placement/production, boarding/disembark, and resource exchange.

## Evidence Health

The direct Editor baseline artifact reports 557 frames at `7.203 / 11.477 / 13.16 ms` average/p95/p99 with a `20 ms` p95 budget. The dashboard instead embeds an older 1,613-frame snapshot at `2.486 / 3.824 / 5.826 ms` with a `50 ms` p95 budget. The dashboard marks that input revision unknown and reports `0` healthy inputs, `0` current inputs, `5` stale inputs, `2` unknown-revision inputs, and `7` inputs requiring attention.

The GC report records a pass at `292 / 1,024` bytes and an internal capture timestamp of 2026-07-15, while its repository filename carries `2026-06-11`; it also declares no commit. The metric is accepted by the tracker and final report, but its direct artifact provenance is not current-baseline proof.

These freshness/provenance gaps do not reverse the historical prerequisite closeout. They prevent AM-001 from claiming exact-baseline remeasurement.

## Deferred Release Gaps

Every row remains unpassed. The authoritative obligations and activation criteria are in `Design/Architecture/pre_release_performance_certification_backlog.md`; the tracker Progress Snapshot supplies the exact transferred ID set.

| Source ID | Truthful status | Remaining obligation | Retained status |
|---|---|---|---|
| `APH-311` | Deferred, measurement-required | Separate 10-minute Android 30 FPS and 60 FPS sessions | Historical device baselines only |
| `APH-501` | Deferred, measurement-required | Installed-size and absolute runtime memory/category budgets | APK/AAB ceilings and fail-closed schema retained |
| `APH-502` | Deferred, measurement-required | Final texture categories from complete same-revision BuildReport/residency evidence | Provisional 3,464-importer inventory retained |
| `APH-504` | Deferred, measurement-required | Authorize/reject mip-streaming pilot from quality and memory evidence | Selector remains fail closed; no importer mutation authorized |
| `APH-505` | Deferred, evidence-required | Near/medium/far streaming comparisons | Capture contract only |
| `APH-506` | Deferred, measurement-required | 10-minute camera pan/zoom memory and I/O collection | Collector/contracts only |
| `APH-508` | Deferred, measurement-required | Animation texture residency, CPU-copy retention, and unload behavior | Six-texture static audit only |
| `APH-509` | Deferred, proof-required | Isolated import/compile/test/build/device proof before package removal | Inventory retained; no package removed |
| `APH-510` | Deferred, measurement-required | Same-revision package, residency, frame, startup, and I/O deltas | Tooling and package-size evidence only |
| `APH-601` | Deferred, measurement-required | Exact accepted-map CPU/GPU mesh memory and peak startup allocation | Instrumentation and structural measurements only |
| `APH-609` | Deferred, measurement and visual-review-required | Normalized canonical/chunked/GRD metrics and visual review | Matrix and short device comparison only |
| `APH-803` | Deferred, certification-required | Clean Android development artifact qualification | Fail-closed gate/recorder/collector retained |
| `APH-804` | Deferred, certification-required | Clean Android release artifact qualification for 30 FPS | Final 600-second diagnostic emitted `complete=false` and is rejected |
| `APH-809` | Deferred, evidence-required | Complete 26-slot graphics/Day-Night/map/streaming visual matrix | `0 / 26` satisfied; two known rejected visual findings |
| `APH-902` | Deferred, certification-required | Final same-device development/release reports with thermal sessions | Schemas/diagnostics only |

## Residual Risks

| ID | Status | Risk |
|---|---|---|
| `RISK-001` | Open, Core Phase 0 | Direct Editor and GC artifacts declare no commit; the GC filename date also differs from its internal capture date. |
| `RISK-002` | Open, Core Phase 0 | The dashboard has no healthy/current inputs and embeds an older Editor snapshot than the direct baseline artifact. |
| `RISK-003` | Open, Core Phase 0 | AM-001 did not rerun compiler, architecture, critical behavior, Editor performance, or GC gates at the review baseline. |
| `RISK-004` | Deferred, release-only | Sustained 30 FPS release and separate 60 FPS observation are uncertified. |
| `RISK-005` | Deferred, release-only | Installed-size and absolute peak/category memory budgets are measurement-required. |
| `RISK-006` | Deferred, release-only | The full visual matrix is unaccepted and includes two known findings. |
| `RISK-007` | Deferred, release-only | Mip streaming remains fail closed and unauthorized. |
| `RISK-008` | Deferred, release-only | Six animation textures retain CPU-copy/residency risk. |
| `RISK-009` | Deferred, release-only | Candidate-unused packages remain installed pending isolated proof. |
| `RISK-010` | Deferred, release-only | Current-map mesh memory and normalized canonical/chunked/GRD comparisons remain uncertified. |

## Untested Paths

- Android device, build, thermal, startup, memory, and visual certification.
- Editor compiler, architecture, PlayMode, performance, and GC reruns at the review baseline commit.
- Operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, and `ProjectSettings`.

## Conclusion

The bounded closeout record satisfies AM-001's Core Architecture Lane entry condition. This is an early-development architecture/code-quality acceptance only. It neither activates the Release Certification Lane nor certifies Android release performance.
