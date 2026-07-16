# AM-001 Entry Prerequisite Review

## Decision

**Core Architecture Lane entry: accepted.**

**Release Certification Lane: deferred. Release certification is not claimed.**

The prerequisite hardening tracker is closed with `107 / 107` explicit dispositions: `92` accepted tasks and `15` release-only obligations transferred without acceptance. Its compiler, architecture, critical behavior, Editor performance, and GC closeout gate groups were accepted for the early-development closeout. The deferred obligations remain fail closed and measurement-required, so they do not block current Core Architecture Lane work and do block any Android release-certification claim.

Current commit-bound environment, dashboard, performance, and GC evidence is still required from the assigned later Phase 0 tasks before Phase 0 exit.

## Artifact Identity

| Field | Value |
|---|---|
| Schema version | `1` |
| Task | `AM-001` |
| Review method | Repository evidence review |
| Review date | 2026-07-16 |
| Reviewed branch | `codex/am-001-prerequisite-review` |
| Reviewed baseline commit | `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` |
| Reviewed `origin/main` commit | `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` |
| Baseline relationship | Exact match |
| Reviewed tree | `521f2cae9d608bbc44b413b7b0172b16301a6b9a` |
| Accepted closeout integration | `8caf7b00c1d2f3b154e366853bf8a864fbb864339` |
| Architecture implementation | `98cb2bb6d4321132574ebc5247aa3f2c93359eac` |
| Tracker-recorded last verification | `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` |
| Closeout documentation | `b31107d3dbf0fdd4da8dc7bec056a548b14218c1` |

The reviewed baseline postdates the accepted closeout measurements and includes the Editor frame-pacing correction in `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713`. AM-001 verifies the closed prerequisite record; it does not represent the five core gates as freshly rerun at that baseline.

## Scope

Writes are limited to:

- `Design/AgentReports/ArchitectureMaturity/entry_prerequisite_review.json`
- `Design/AgentReports/ArchitectureMaturity/entry_prerequisite_review.md`

No shared tracker, operation-map, FirstLaunch, audio, UI visual-lock, production code, scene, prefab, package, or `ProjectSettings` path was changed. Production behavior is unchanged.

## Core Gates

| Gate ID | Gate | Status | Accepted result | Repository evidence |
|---|---|---|---|---|
| `CORE-ARCHITECTURE` | Architecture | Accepted at prerequisite closeout; not recaptured by AM-001 | Integrated architecture `23 / 23`; source growth `15 / 15`; ECS/Burst `10 / 10` with no snapshot debt; assembly boundary `31 / 31` | `Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json`; `Design/AgentReports/architecture_performance_hardening_final_report.md`; `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` |
| `CORE-COMPILER` | Compiler | Accepted at prerequisite closeout; not recaptured by AM-001 | All 12 first-party assemblies built with `0` errors | `Design/AgentReports/architecture_performance_hardening_final_report.md`; `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` |
| `CORE-CRITICAL-BEHAVIOR` | Critical behavior | Accepted at prerequisite closeout; not recaptured by AM-001 | Five critical PlayMode flows `5 / 5`; static-map structural parity `2 / 2` | `Design/AgentReports/architecture_performance_hardening_final_report.md`; `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` |
| `CORE-EDITOR-PERFORMANCE` | Editor performance | Accepted at prerequisite closeout; not recaptured after the reviewed baseline's frame-pacing change | 733 units, 628 buildings, 557 frames; `7.20 ms` average, `11.48 ms` p95, `13.16 ms` p99, zero measured allocation; p95 budget `20 ms` | `Design/AgentReports/architecture_performance_hardening_final_report.md`; `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`; `Design/Architecture/performance_regression_accepted_baseline.json` |
| `CORE-GC-CLOSEOUT` | GC closeout | Accepted at prerequisite closeout; not recaptured by AM-001 | `292 / 1,024` player-relevant bytes over 300 measured frames after 180 warmup frames | `Design/AgentReports/architecture_performance_hardening_final_report.md`; `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`; `Design/Architecture/performance_regression_accepted_baseline.json` |

The covered critical flows are Menu-to-Match-to-Menu, selection/move/attack, placement/production, boarding/disembark, and resource exchange.

## Deferred Release Gaps

Every row below remains unpassed. `Deferred` means the obligation has moved to the inactive pre-release backlog; it does not mean accepted or waived.

| Source ID | Truthful status | Remaining obligation | Retained evidence |
|---|---|---|---|
| `APH-311` | Deferred, measurement-required | Separate 10-minute Android 30 FPS and 60 FPS sessions with frame, GPU, memory, thermal, and visual results | Short 45 FPS development diagnostic only; `Design/AgentReports/2026-07-10_perf_WarlineCapture_candidate_android_steady_summary.md` |
| `APH-501` | Deferred, measurement-required | Installed-size and absolute peak, texture, mesh, audio, graphics-driver, and total runtime memory budgets from accepted same-device release evidence | APK/AAB ceilings tracked; device limits remain null and fail closed; `Design/AgentReports/2026-07-13_aph-501_product_budget_evidence_handoff.md`; `Design/Architecture/performance_regression_accepted_baseline.json` |
| `APH-502` | Deferred, measurement-required | Final included/excluded texture categories from a complete same-revision BuildReport and residency inventory | Provisional 3,464-importer classifier; `Design/AgentReports/2026-07-10_aph-502_texture_importer_classification.json`; `Design/AgentReports/architecture_performance_content_residency_baseline.json` |
| `APH-504` | Deferred, measurement-required | Authorize or reject the mip-streaming pilot using accepted visual, memory, and I/O evidence | `pilot_ready=false`, `mutation_authorized=false`, `expansion_authorized=false`; `Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.json` |
| `APH-505` | Deferred, evidence-required | Accepted identical near/medium/far before-and-after streaming comparisons | Capture contract only; `Design/AgentReports/2026-07-13_aph-809_visual_capture_matrix.json`; `Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.md` |
| `APH-506` | Deferred, measurement-required | 10-minute camera pan/zoom memory and I/O run on the pinned device, including thermal and visual acceptance | Strict collector and tests exist; no accepted device run; `Tools/CI/aph506_texture_streaming_device_collection.py` |
| `APH-508` | Deferred, measurement-required | Generated animation texture residency, CPU-copy retention, precision safety, and unload behavior | Six RGBAHalf textures retain concrete risk; `Design/AgentReports/2026-07-11_aph-508_animation_texture_audit.md` |
| `APH-509` | Deferred, proof-required | Isolated import/compile/test/build/device proof before any package removal | Usage inventory exists; no package removal accepted; `Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.json` |
| `APH-510` | Deferred, measurement-required | Accepted same-revision package, residency, frame, startup, and I/O category deltas | Package-size improvement accepted; remaining release comparisons incomplete; `Design/AgentReports/architecture_performance_android_apk_build_report.json`; `Tools/CI/aph510_android_category_comparison.py` |
| `APH-601` | Deferred, measurement-required | Exact accepted-map CPU/GPU mesh memory and peak startup allocation | Historical/candidate structural metrics only; `Design/AgentReports/2026-07-14_aph-601_aph-609_current_map_evidence_matrix.md` |
| `APH-609` | Deferred, measurement and visual-review-required | Normalized canonical/chunked/GPU Resident Drawer metrics and current-map visual review | Short comparison only; `Design/AgentReports/2026-07-11_gpu_instancing_android_comparison.md`; `Design/AgentReports/2026-07-14_aph-601_aph-609_current_map_evidence_matrix.md` |
| `APH-803` | Deferred, certification-required | Clean Android development qualification covering startup, sustained frame, memory, thermal, process, crash, and visual evidence | Gate, recorder, collector, schemas, and rejected diagnostics retained; `Design/AgentReports/2026-07-11_aph-803_android_development_gate_plan.md`; `Design/AgentReports/2026-07-12_aph-803_android_runtime_recorder.md` |
| `APH-804` | Deferred, certification-required | Clean Android release 30 FPS qualification with repeated startup, sustained timing, profiler counters, memory, installed size, thermal, crash, and visual evidence | Timing-capable APK exists; final 600-second diagnostic emitted `complete=false` and is rejected; `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`; `Design/AgentReports/architecture_performance_android_apk_build_report.json` |
| `APH-809` | Deferred, evidence-required | Complete and approve the graphics-tier, Day/Night, static-map, and mip-streaming visual matrix | `0 / 26` slots satisfied; dark 23:00 readability and near-map black/missing surfaces remain rejected findings; `Design/AgentReports/2026-07-13_aph-809_visual_capture_matrix.json` |
| `APH-902` | Deferred, certification-required | Final same-device development/release reports including separate 10-minute thermal sessions | Diagnostics and schemas retained; no final certification report accepted; `Design/AgentReports/architecture_performance_hardening_final_report.md` |

The authoritative one-to-one transfer and activation criteria are in `Design/Architecture/pre_release_performance_certification_backlog.md`. The original status and implementation history remain in `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`.

## Evidence Health

| Evidence | Status | Consequence |
|---|---|---|
| `Design/AgentReports/architecture_performance_dashboard.json` | Requires AM-005 refresh: `0` current, `5` stale, `2` revision-unknown, `7` requiring attention at dashboard revision `e9dcf183eb95637bb1975c97b11f5c43a1cb9e79` | Does not reverse the accepted historical closeout; does prevent treating the dashboard as current baseline evidence |
| `Design/Architecture/performance_regression_accepted_baseline.json` | Budget authority, version `4`; p95 budget `20 ms`, GC budget `1,024` bytes; still labels the historical `269,482`-byte capture `red-baseline` | AM-004/AM-005 must reconcile the historical status with the closeout's accepted `292`-byte result without weakening the budget |
| Reviewed baseline `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` | Postdates closeout evidence and changes Editor frame pacing | Later Phase 0 owners must produce exact-baseline environment, dashboard, performance, and GC evidence before Phase 0 exit |

## Residual Risks

| ID | Status | Risk |
|---|---|---|
| `RISK-001` | Open, Core Phase 0 | Dashboard inputs are stale or revision-unknown; AM-005 must refresh them without weakening freshness rules. |
| `RISK-002` | Open, Core Phase 0 | The budget JSON's historical red GC status and the authoritative closeout's 292-byte pass need provenance/status reconciliation while retaining the 1,024-byte budget. |
| `RISK-003` | Open, Core Phase 0 | The reviewed baseline includes a post-closeout Editor frame-pacing change; no five-gate recapture was performed by AM-001. |
| `RISK-004` | Deferred, release-only | Sustained 30 FPS release acceptance and the separate 60 FPS observation are uncertified. |
| `RISK-005` | Deferred, release-only | Installed size and absolute peak/category memory budgets are measurement-required. |
| `RISK-006` | Deferred, release-only | The full visual matrix is unaccepted and includes two known visual findings. |
| `RISK-007` | Deferred, release-only | Mip streaming remains fail closed and unauthorized. |
| `RISK-008` | Deferred, release-only | Six generated animation textures retain CPU-copy/residency risk. |
| `RISK-009` | Deferred, release-only | Candidate-unused packages remain installed pending isolated removal proof. |
| `RISK-010` | Deferred, release-only | Current-map mesh memory and normalized canonical/chunked/GPU Resident Drawer comparisons remain uncertified. |

## Untested Paths

- Android device, build, thermal, startup, memory, and visual certification.
- Editor compiler, architecture, PlayMode, performance, and GC reruns at the reviewed baseline commit.
- Operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, and `ProjectSettings`.

## Conclusion

The closed prerequisite record satisfies AM-001's Core Architecture Lane entry condition. This is an early-development architecture/code-quality acceptance only. It neither activates the Release Certification Lane nor certifies Android release performance.
