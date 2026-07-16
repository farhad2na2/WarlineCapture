# Architecture and Performance Hardening Implementation Tracker

## Purpose

Turn the 2026-07-09 architecture and performance audit into an executable, multi-agent remediation program. This tracker is the source of truth for program scope, task state, gates, and evidence. `Design/Architecture/agent_pull_request_review_merge_workflow.md` is the authority for branch, worktree, implementation/review ownership, PR revision, merge, tracker administration, and cleanup. An agent should be able to select one ready checklist item, implement it, validate it, and record evidence here without repeating the audit.

This tracker follows the completed historical program in `Design/Architecture/architecture_performance_audit_followup_tracker.md`. Do not reopen or rewrite that completed tracker. Carry forward its accepted baselines and lessons through this document.

**Workflow transition:** The Architecture and Performance Hardening coordinator assignment was already active when the pull request workflow was introduced. Its existing assigned scope through genuine `107 / 107` completion and closeout is one grandfathered direct-`main` program. Dependency-ready `APH-*` slices inside that scope keep the established coordinator integration procedure. Work outside this tracker scope, or substantive follow-up dispatched after this program closes, uses the pull request workflow.

## Authority and Baseline

| Field | Value |
|---|---|
| Baseline date | 2026-07-09 |
| Original audit baseline commit | `b66453e979d847c3f05d61155266b166650b8df5` |
| Current execution baseline | `a6af1335cb7270d8b87c60d9646ed78be7e97ac5` |
| Unity | `6000.5.2f1` |
| Canonical project root | `/Users/farhad/Projects/WarlineCapture` |
| Architecture rating | `6.5 / 10` |
| Performance rating | `6.0 / 10` |
| Current program status | Active - local integrated closeout is green; final device, visual, product-budget, and integration evidence remains |

### Protected Integrated Work

The following work belonged to the user when this tracker was created and is now integrated in commits `76d098a9a` and `845a56eb8`. Agents must preserve it and must not revert, overwrite, reformat, or include it in unrelated commits:

- `Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.cs`
- `Assets/Tests/Editor/MatchHudAssistantUiSystemHelperTests.cs`
- `Design/VisualLockLayered/POP-13_ARIACommandAssistant/`

If a tracker task genuinely needs one of these files, read and integrate the existing changes. Record the overlap in the task handoff before editing.

### Baseline Reconciliation

The tracker was authored at `b66453e97`, then entered `main` through `68b2da500`. Before implementation began, `main` also received the protected ARIA work and the match CPU performance repair in `a6af1335c`. The current execution baseline is therefore `a6af1335c`, not the original audit commit.

The FPS repair changed `BuildingDefenseAttackSystem` and related presentation, audio, warning, settings, diagnostics, and validation code. It reduced the focused 4,800 x 2,160 Editor benchmark with 733 units to an average `8.893 ms`, median `8.242 ms`, and p95 `12.516 ms`. This remains the accepted performance floor. `APH-207` through `APH-212` subsequently removed the four throttled target-acquisition snapshots, documented the remaining synchronization boundary, added phase profiler evidence, closed integrated combat coverage, and restored the ECS/Burst architecture gate. `APH-213` remains the required performance recapture.

Before changing production code, the Phase 0 agents must reproduce current-head compiler, architecture, performance, and residency evidence. If current results differ from the original audit snapshot, record both values; never rewrite the original evidence as though it had been measured at `a6af1335c`.

## Status Rules

- `[ ]` pending
- `[~]` in progress; at most one item per agent may use this status
- `[x]` complete with validation evidence recorded in this file
- `[!]` blocked with the exact blocker and next unblocking action recorded
- A task is not complete because code compiles. Its stated focused tests, architecture gates, and behavior checks must pass.
- The coordinator owns `Progress Snapshot`, task checkboxes, decisions, and `Implementation Log`. Implementation agents return evidence and do not self-accept these shared tracker sections. The coordinator normally adds the final tracker/evidence reconciliation as an administrative commit to the implementation PR.
- A future pull-request slice must pass the Stable Integration and Pull Request Gate and merge through its PR. A proposed `[x]` in an open PR becomes authoritative only when that PR reaches `main`. A slice inside this grandfathered hardening assignment instead uses the direct-main path in that same gate and becomes authoritative only after its validated commit reaches `main`; accepted changes that exist only in a worktree or unmerged branch are not complete.
- Never raise a budget, allowlist a violation, or weaken a test merely to obtain a pass. A budget or allowlist change requires a before/after report and explicit approval in the Decision Log.
- Run `git diff --check` after every implementation slice.
- Preserve Unity `.meta` files and serialized scene/prefab references.
- Do not combine gameplay balance changes, visual redesign, random-map generation, or unrelated UI work with this program.

## Multi-Agent Operating Model

The program may use multiple coding agents. During the grandfathered hardening assignment, the existing coordinator retains tracker state, integration, commit, and push ownership under `D-013` and `D-017`. For future pull-request tasks, the review/merge coordinator owns the critical path, tracker administration, independent acceptance, merge, and cleanup while the implementation agent owns substantive feature-branch work and revisions. Parallelism is allowed only for tasks with satisfied dependencies and non-overlapping file ownership.

### Roles and Initial Allocation

| Role | Initial assignment | Ownership |
|---|---|---|
| Review/merge coordinator | Program control and independent integration | tracker state, dependency checks, work allocation, findings-first review, final validation, administrative tracker/evidence commits, PR merge, and cleanup |
| Build agent | `APH-006` | sequential first-party build matrix and categorized warning report; no Unity lease required |
| Performance agent | `APH-007` | current-head Match performance and steady-state GC captures; exclusive Unity lease required |
| Residency agent | `APH-008` | machine-readable content-residency inventory and Markdown summary; Unity importer inspection requires the Unity lease |
| Budget analysis agent | prepare `APH-009` | draft measured budget recommendations only after `APH-007` and `APH-008` evidence exists; coordinator approves and integrates |

### Coordination Rules

1. The coordinator is the only agent that reserves tracker tasks, edits shared progress/decision/log sections, and accepts evidence. In this grandfathered program it also owns stable integration commits/pushes; for future PR tasks it alone merges while the implementation agent owns substantive commits, pushes, PR creation, and review revisions on `codex/<task-id>-<slug>`.
2. Workers receive a bounded task, an explicit file allowlist, required validations, and a prohibition on touching unrelated dirty work.
3. Workers must not edit the same production file, test file, generated artifact, scene, prefab, or importer concurrently. If overlap becomes necessary, stop one worker and serialize the work.
4. Every task outside this grandfathered `107 / 107` assignment that starts after PR-workflow activation uses an isolated shared-object git worktree and short-lived feature branch. The implementation agent commits/pushes only that branch, opens the PR, and never merges it.
5. Only one Unity Editor or Unity batchmode process may use a project workspace at a time. The coordinator grants an exclusive Unity lease. Other agents may continue non-Unity builds, source analysis, or report work while that lease is active.
6. An isolated Unity workspace may run concurrently only when it already exists, points at the same execution baseline, has no shared `Library` directory, and the coordinator records its path and result. Never create an ad hoc clone merely to bypass a lock or licensing failure.
7. Use `Tools/CI/invoke_unity_macos.sh` in windowed batchmode without `-nographics`. If licensing fails in the sandbox, rerun once with the documented escalated/out-of-sandbox workaround before reporting a blocker.
8. For future PR tasks, the coordinator reviews findings first and runs cross-slice validation on the final PR head and combined latest-main result. The implementation agent's focused pass is necessary evidence, not permission to skip integrated build and architecture gates. The grandfathered coordinator keeps its established handoff review and cross-slice validation before direct integration.
9. For future PR tasks, every substantive finding returns to the implementation agent. The coordinator may add only administrative tracker/evidence commits, then merges an accepted PR and deletes its remote branch and clean worktree/local branch. In the grandfathered program, the coordinator may integrate reviewed worker handoffs as already assigned. Never merge or push a known-broken slice or unrelated change.

### Worker Handoff Contract

Every implementation handoff must contain: role/context, task ID, workflow path, branch, worktree, baseline and tested head commits, PR URL, allowlist, files changed, files intentionally not touched, behavior and architecture statements, exact commands/pass markers, compiler/build result, artifact/log paths, metrics before/after, Unity/device/visual/Jenkins evidence when applicable, residual risks, untested paths, recommended next owner, and focused commit identities. The coordinator rejects incomplete handoffs and leaves the task pending or blocked. Use the complete field set in the authoritative PR workflow.

## Agent Start Procedure

The numbered procedure below applies to future PR tasks. For a dependency-ready slice inside the grandfathered hardening assignment, the existing coordinator claims disjoint ownership, uses its persistent integration checkout, accepts structured worker handoffs, runs the required focused and integrated gates, fetches and integrates latest `origin/main`, reruns every affected check, and commits/pushes only the stable task-owned slice directly to `main`. PR-only branch, template, review, merge, and cleanup steps are not applicable; the handoff records `Workflow path: grandfathered direct-main`.

1. Read this tracker and `Design/Architecture/agent_pull_request_review_merge_workflow.md` completely.
2. Confirm every phase dependency is complete and obtain a coordinator dispatch containing the task ID, allowlist, exclusions, required gates, Unity lease needs, branch, and worktree path.
3. The coordinator records the reservation in the dispatch/current-task context. The open draft PR is the visible in-progress claim; do not push an in-progress tracker mutation directly to `main`.
4. The implementation agent creates the isolated shared-object worktree from current `origin/main` on `codex/<task-id>-<slug>`, verifies `git status --short --branch`, and records the baseline commit.
5. The implementation agent pushes the feature branch and may open a draft PR before substantive edits, then implements the narrowest behavior-preserving slice within the allowlist.
6. Run task-specific commands and relevant Common Validation Matrix rows. Inspect Unity console output and capture required views while holding the assigned Unity lease.
7. Commit/push the substantive slice, complete the PR template, and return the full implementation handoff. Never merge or self-accept tracker state.
8. The independent coordinator reviews findings first. The implementation agent resolves substantive findings in new commits and reruns affected gates until accepted or blocked.
9. The coordinator runs the Stable Integration and Pull Request Gate on the final head and may add only the administrative Progress Snapshot, checklist, Decision Log, Implementation Log, and evidence reconciliation commit.
10. The coordinator merges the accepted PR, deletes its branch/worktree, and treats the proposed `[x]`/Implementation Log update as authoritative only after the merge reaches `main`.

## Current Evidence Snapshot

### Code and Content Scale

| Metric | Baseline |
|---|---:|
| First-party production C# under `Assets/Game/Scripts`, excluding Editor | `200,641` lines |
| First-party tests | `90,978` lines |
| First-party asmdefs including tests/tools | `18` |
| `ISystem` source files | `148` |
| `SystemBase` source files | `26` |
| `*SystemHelper.cs` files | `265` |
| EditMode `[Test]` methods | `1,571` |
| PlayMode `[Test]` methods | `16` |
| `Match.unity` serialized size | `58,822,096` bytes, approximately `56 MB` |
| `Match.unity` prefab instances | `16,978` |
| `MatchSubScene.unity` serialized size | `12,336` bytes |
| Android profiler APK | approximately `443-471 MB`, depending on captured build |
| Generated animation texture payload | six textures, `100,663,296` bytes total serialized image payload |

### Sustained Android Baseline

Reference device: Xiaomi `24090RA29G`, MediaTek `MT6878`, Android `16`.

Use the controlled 2026-07-06 10-minute foreground run as the representative baseline. Do not use the saved capture that reports only approximately 983 triangles as representative Match rendering evidence.

| Metric | Baseline |
|---|---:|
| Average frame | `21.68 ms`, approximately `47.7 FPS` |
| P95 frame | `26.2 ms` |
| P99 frame | `37.6 ms` |
| Worst sampled frame | `133.5 ms` |
| P95 CPU frame | `29.4 ms` |
| P95 GPU | `25.4 ms` |
| Draw calls | `69-154` |
| SetPass calls | `41-96` |
| Triangles | `756,153-1,530,800` |
| Allocated memory | `1,054-1,075 MB` |
| Mono memory | `27-32 MB` |
| Thermal result | status `0`, no cooling-device throttling |

Interpretation:

- The project passes its documented baseline/recommended Android p95 target of less than `33 ms`.
- It does not sustain 60 FPS and is slightly above the documented high-end p95 target of less than `25 ms`.
- Frame time is acceptable for the 30 FPS tier. Memory residency and package size are the largest unbudgeted risks.

### Current Red Gates

| Gate | Current result | Required end state |
|---|---|---|
| Architecture and ECS/Burst closeout | Green: integrated closeout passes `23/23`; source growth passes `15/15`; the unmanaged-system inventory and naming/assembly gates pass | keep green through final integration |
| Editor Match performance and GC | Green: 733 units and 628 buildings; `7.20 ms` average, `11.48 ms` p95, `13.16 ms` p99, zero measured allocation; steady-state player-relevant GC is `292 / 1,024` bytes | keep green through final integration |
| Critical PlayMode behavior | Green: Menu -> Match -> Menu plus selection/move/attack, placement/production, boarding/disembark, and resource-exchange fixtures pass `5/5` | keep green through final integration |
| Static-map structural parity | Green: full Match-scene structural validation passes `2/2` | keep green without modifying operation-map ownership |
| Product/device/visual acceptance | Red: clean same-artifact Android development/release thermal evidence and all required visual comparison slots remain incomplete | complete APH-311, APH-505, APH-803, APH-804, APH-809, and APH-902 |

There are no current architecture, ECS/Burst, Editor Match performance, GC, critical PlayMode, or static-map structural violations in the local integration candidate. The remaining red state is evidence completeness for device, visual, streaming, package, and product budgets.

Latest local steady-state GC evidence is `292 / 1,024` player-relevant bytes over 300 measured frames after 180 warmup frames, so the unchanged GC budget passes with `732` bytes of headroom. The previous recurring production-ID cache, resource-storage EntityQuery, and three-query hauler move-order allocation rows are absent. The six retained player-relevant samples total two 56-byte resource-hauler policy enumerations, one 56-byte assignment-signature enumeration, one 48-byte UI shell sample, one 48-byte path-result JIT sample, and one 28-byte audio diagnostic sample. The original baseline owner table remains in `Design/AgentReports/2026-07-09_aph-007_match-gc-steady-state_raw-metadata-final_failed.md`; the current result is in `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`.

## Program Outcomes

The program is complete only when all of the following are true:

1. Both currently red Unity guardrails pass without new allowlist debt.
2. Persisted settings are applied at application startup.
3. Android cannot request more than 60 FPS through settings; 30 and 60 remain supported.
4. One owner applies graphics quality, and one owner applies dynamic environment lighting/fog.
5. `VisualQualitySettingsSystem` performs no redundant per-frame global render-state writes.
6. The persistent audio path no longer forces unnecessary decompressed voice residency.
7. World texture streaming has a measured, quality-validated policy.
8. Match startup does not build duplicate static map meshes at runtime when equivalent editor-baked chunks are available.
9. The current 30 FPS Android p95 target remains green with no visual downgrade outside explicitly approved tier settings.
10. Same-device peak allocated memory is at least 10 percent lower than the validated pre-change baseline, or a stricter approved absolute budget is met.
11. Android release APK/AAB size and installed size have explicit tracked budgets.
12. Critical menu-to-match and gameplay flows have PlayMode coverage.
13. The architecture and performance ratings are re-audited and the evidence is recorded.
14. The steady-state Match GC capture passes the unchanged 1,024-byte player-relevant budget for 300 measured frames after 180 warmup frames.

## Non-Goals

- Do not convert every managed helper in one program.
- Do not add Burst attributes without eligibility analysis.
- Do not rewrite the road, city, terrain, or random-map generation systems.
- Do not move authored buildings, interiors, roads, vegetation, or map geometry.
- Do not trade terrain or UI quality for performance without side-by-side evidence and approval.
- Do not introduce a new service locator, global mutable registry, broad manager, presenter, controller, or facade.
- Do not migrate to UI Toolkit in this program.

## Fixed Architecture Decisions

| ID | Decision |
|---|---|
| `D-001` | The completed `architecture_performance_audit_followup_tracker.md` is historical and remains unchanged. |
| `D-002` | `Game.UI.Runtime` must not reference `Game.Configs`; text lookup reaches UI through a UI-facing contract injected by composition. |
| `D-003` | Do not add a static UI text service as a replacement for `GameText`. Use an injected resolver and an immutable null/fallback implementation for unbound previews/tests. |
| `D-004` | Resource exchange processing must preserve support for multiple matching exchange entities. Do not replace it with an undocumented singleton assumption. |
| `D-005` | The immediate building-defense repair removes per-frame array allocation without changing targeting order. Shared spatial indexing is a later measured phase. |
| `D-006` | Settings persistence stays in `SettingsService`; composition maps settings to runtime owners; `VisualQualitySettingsSystem` performs render-pipeline application. |
| `D-007` | `DayNightSystem` owns dynamic directional light, ambient, fog, skybox, and day/night volume values after initialization. |
| `D-008` | Android exposes 30 and 60 FPS. The 120 FPS option is hidden or clamped on Android. |
| `D-009` | Audio changes start with catalog-referenced voice residency. Existing cataloged music and ambience streaming behavior is preserved. |
| `D-010` | Static map optimization is editor-baked and transform-preserving. Runtime procedural relocation or regrouping of authored content is forbidden. |
| `D-011` | Unity MCP should be used for scene hierarchy inspection and visual validation when connected. If unavailable, use the named Unity capture/validation runners and record that limitation. |
| `D-012` | `a6af1335c` is the current execution baseline; the original `b66453e97` audit evidence remains historical and is not relabeled as current-head evidence. |
| `D-013` | One coordinator owns tracker state, task claims, integration validation, commits, and pushes. Parallel workers return structured handoffs instead of editing shared tracker state. |
| `D-014` | Parallel work requires satisfied dependencies and disjoint file ownership. Any newly discovered overlap is serialized by the coordinator. |
| `D-015` | Unity access is an exclusive per-workspace lease. Use the macOS wrapper in windowed batchmode without `-nographics`, then use the documented escalated licensing retry before declaring a licensing blocker. |
| `D-016` | `APH-006`, `APH-007`, and the non-Unity portion of `APH-008` may begin in parallel. `APH-009` waits for accepted `APH-007` and `APH-008` evidence. |
| `D-017` | Each accepted implementation slice is committed and pushed by the coordinator only after relevant builds, focused tests, diff checks, and compiler-error checks pass. Known-broken or unrelated work is never included. |
| `D-018` | `APH-007` accepts a reproducible baseline capture even when that baseline exposes a red GC budget. The 1,024-byte budget is not weakened; measured allocation owners become mandatory remediation input for Phases 7-9. |
| `D-019` | Reject direct 96 m and 32 m transient Match mesh-combine output. Phase 6 uses shared-mesh, overlay-safe presentation chunks and measured GPU culling instead. |
| `D-020` | APH-701/702 freeze the verified `9280ead85` baseline of 265 helper paths, 108 production files above 500 lines, and 27 production files above 1,000 lines. Shrinkage passes; additions or growth require an exact tracker task and Decision Log exception. |
| `D-021` | Static-map structural parity keeps object/asset/state identity exact while allowing only Unity serialization precision of `0.0005` for transforms and `0.005` for derived world bounds. |
| `D-069` | For tasks started after the PR workflow reaches `main`, `agent_pull_request_review_merge_workflow.md` supersedes the direct-integration portions of `D-013` and `D-017`: the implementation agent owns substantive branch commits/pushes and PR revisions; the independent coordinator owns tracker administration, final gates, merge, and cleanup. Already-active tasks are grandfathered. |

## Phase Dependency Order

| Phase | Goal | Depends on |
|---|---|---|
| 0 | Freeze baseline and budgets | none |
| 1 | Restore UI assembly boundary | Phase 0 |
| 2 | Restore ECS hot-path gate | Phase 0 |
| 3 | Unify settings and environment ownership | Phases 1 and 2 |
| 4 | Reduce audio residency | Phase 0; may run after Phase 2 if a separate agent owns it |
| 5 | Add texture/build/memory policy | Phase 0; importer edits wait for Phase 4 measurements |
| 6 | Pre-bake static map batching | Phases 2, 3, and 5 |
| 7 | Incremental domain decomposition | Phases 1, 2, and 3; measured priorities only |
| 8 | CI, PlayMode, and device gates | Phases 3-7 as relevant |
| 9 | Final validation and re-rating | all active phases |

### Recommended Execution Waves

| Wave | Parallel work | Integration barrier |
|---|---|---|
| 0A | `APH-006`, `APH-007`, and non-Unity `APH-008` inventory work | current-head build, performance, GC, and residency evidence accepted |
| 0B | Unity importer portion of `APH-008`; budget analysis after evidence handoff | `APH-009` accepted and Phase 0 complete |
| 1 | Phase 1 UI boundary, Phase 2A resource exchange, and Phase 2B building defense with disjoint owners | both red architecture gates pass together on the integrated head |
| 2 | Phase 3 settings/environment; Phase 4 audio measurements may run after Phase 2 under a separate owner | settings ownership and audio pilot evidence accepted |
| 3 | Phase 5 policy work; Phase 7 analysis-only reports that do not touch active Phase 5 files | memory/build budgets and measured decomposition priorities accepted |
| 4 | Phase 6 static-map work, then approved Phase 7 implementation slices | visual lock, startup, memory, and architecture gates remain green |
| 5 | Phase 8 CI/device gates | all required automated and device evidence accepted |
| 6 | Phase 9 final closeout | no required task remains |

## Progress Snapshot

| Field | Status |
|---|---|
| Checklist complete | `90 / 107` |
| Checklist percent complete | `84.1%` |
| Checklist in progress | `12 / 107`: `APH-311`, `APH-501`, `APH-502`, `APH-504`, `APH-506`, `APH-508`, `APH-509`, `APH-510`, `APH-601`, `APH-609`, `APH-803`, `APH-804` |
| Complete plus active coverage | `102 / 107` (`95.3%`); this is visibility only, not accepted completion |
| Current phase | Final device/visual/product-budget evidence and report closeout |
| Current task | Collect same-artifact APH-804 release-device memory/thermal evidence and APH-809 UI visual acceptance from the clean `449,902,496`-byte package-compliant APK |
| Red architecture gates | `0`: the rebased 23-suite closeout, bootstrap guard, source-growth ratchet, naming/static-registry rules, and `ISystem` inventory all pass |
| Red performance gates | `0`: the unchanged steady-state Match GC gate passes at `292 / 1,024` player-relevant bytes over 300 measured frames after 180 warmup frames. The latest canonical Match baseline passes over 557 frames at `7.20 ms` average, `11.48 ms` p95, `13.16 ms` p99, and zero measured allocation with 733 units and 628 buildings. |
| Red visual gates | `2`: 23:00 Match readability is too dark, and the current near static-map capture contains black/missing surface regions |
| Last verified commit | `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` on `origin/main`; architecture implementation commit `98cb2bb6d4321132574ebc5247aa3f2c93359eac` |
| Last update | 2026-07-16 - the clean supporting-UI candidate APK is installed on the Xiaomi reference device; release collection now uses permission-safe base-APK plus extracted-native-library bytes after protected `oat` traversal blocked whole-directory `du`, while memory/thermal and APH-809 visual acceptance remain open |

## Phase 0 - Baseline and Safety Freeze

Goal: preserve current evidence, make the program reproducible, and establish budgets before behavior or asset changes.

- [x] `APH-000` Create this implementation tracker and keep the prior completed tracker historical.
  - Evidence: this file.
- [x] `APH-001` Record baseline commit, Unity version, code/content scale, ratings, and Android soak metrics.
  - Evidence: Authority and Baseline plus Current Evidence Snapshot.
- [x] `APH-002` Record and protect the baseline dirty worktree.
  - Evidence: Protected Integrated Work and Baseline Reconciliation.
- [x] `APH-003` Reproduce the UI assembly-boundary failure.
  - Result: failed because `Game.UI.Runtime` references `Game.Configs`.
  - Log: `/private/tmp/warline-architecture-audit-validation.log`.
- [x] `APH-004` Reproduce the ECS hot-path architecture failure.
  - Result: the audit baseline failed with snapshot violations in two systems: building defense and resource exchange. Current-head reproduction is still required because the building-defense implementation changed in `a6af1335c`.
  - Log: `/private/tmp/warline-performance-architecture-audit-validation.log`.
- [x] `APH-005` Confirm core runtime compiler health.
  - Result: `Game.Runtime.csproj` built with 0 errors and 6 generated-project/reference warnings.
- [x] `APH-006` Run the sequential first-party build matrix.
  - Initial owner: Build agent. May run in parallel with `APH-007` and non-Unity `APH-008` work.
  - Build `Game.Components`, `Game.Configs`, `Game.Runtime.Pathfinding`, `Game.Runtime`, `Game.Rendering`, `Game.UI.Contracts`, `Game.UI.Runtime`, `Game.UI.Shell.Ecs`, `Game.Composition`, `Game.Editor`, `Game.Tests.Editor`, and `Game.Tests.PlayMode`.
  - Acceptance: 0 errors. Record warnings by category; do not paste repeated SDK reference-conflict noise into this tracker.
  - Result: all 12 assemblies passed sequentially with 0 errors. Only generated-project `MSB3277` reference-version conflicts were observed; no first-party compiler warning category was present.
  - Logs: `/private/tmp/warline-aph006-final-matrix-summary.log` and `/private/tmp/warline-aph006-warning-categories.log`.
- [x] `APH-007` Capture a fresh pre-change Match performance baseline on the current commit.
  - Initial owner: Performance agent. Requires the exclusive Unity lease.
  - Run the editor regression baseline and steady-state GC capture.
  - Preserve the generated JSON/Markdown artifacts before optimization.
  - Acceptance: capture includes at least 700 units, 600 runtime buildings, 180 warmup frames, and 300 measured frames.
  - Result: 733 units, 628 runtime buildings, 1,173 performance frames, 180 GC warmup frames, and 300 measured GC frames. Average `3.42 ms`, p95 `5.22 ms`, p99 `6.87 ms`, maximum `18.13 ms`.
  - GC result: raw-sample metadata attribution reconciled 4,826 samples and `269,482` player-relevant bytes. This is an accepted red baseline, not a green GC gate; the 1,024-byte budget remains unchanged.
  - Validation: 16 focused attribution tests and Editor/test builds passed with 0 errors; commit `ba3da6704` pushed to `main`.
- [x] `APH-008` Add a machine-readable content-residency inventory report.
  - Initial owner: Residency agent. File/dependency inventory may run in parallel; serialized importer inspection requires the exclusive Unity lease.
  - Required fields: build-included asset path, type, source size, imported size where available, dependency root, audio load type, texture dimensions/format/mipmap/streaming state, mesh read/write state, and animation texture payload.
  - Output: `Design/AgentReports/architecture_performance_content_residency_baseline.json` plus Markdown summary.
  - Result: 4,099 dependency-root-reachable assets across 12 roots; 2,680 imported-size measurements; 226 audio clips; 637 textures; 1,817 meshes; and 3 reachable animation textures with 50,331,648 payload bytes.
  - Validation: generator and 4 focused tests passed; `Game.Editor` and `Game.Tests.Editor` built with 0 errors; commit `929ab7bb3` pushed to `main`.
- [x] `APH-009` Freeze initial product budgets in a tracked config.
  - Initial owner: Budget analysis agent for the draft; coordinator owns approval and integration. Depends on accepted `APH-007` and `APH-008` evidence.
  - Keep existing Android p95 budgets: `<33 ms` baseline/recommended and `<25 ms` high-end.
  - Add same-device peak allocated-memory, APK/AAB, installed-size, startup-time, and visual-quality evidence requirements.
  - Initial memory improvement target: at least 10 percent below the validated same-device baseline; do not assert an absolute limit until `APH-008` identifies residency owners.
  - Result: accepted-baseline schema version 2 is the single authority for editor and initial product budgets. Known frame, GC, and relative memory targets are frozen; unsupported release package, installed-size, startup, and runtime-residency limits remain explicit measurement-required states with owner tasks.
  - Validation: strict config validator and 4 focused tests passed; `Game.Editor` and `Game.Tests.Editor` built with 0 errors; commit `1e20f1417` pushed to `main`.

## Phase 1 - Restore the UI Assembly Boundary

Goal: remove `Game.Configs` from `Game.UI.Runtime` while preserving every configured string and fallback.

Implementation contract:

- Add `IGameTextResolver` to `Game.UI.Contracts` with non-Unity-facing `Get`, `TryGet`, and formatting behavior needed by current UI callers.
- Add an immutable fallback resolver in `Game.UI.Contracts` for unbound prefab previews and focused tests.
- Add the concrete adapter in `Game.Composition`; it may call `Game.Configs.GameText` because composition already owns cross-assembly wiring.
- Inject the resolver through existing UI bootstrap/runtime adapter binding. Do not create a static mutable UI registry.
- Migrate only files compiled by `Game.UI.Runtime` in this phase. `Game.UI.Shell.Ecs` and gameplay-system text ownership are separate later decisions.

- [x] `APH-100` Add focused tests for resolver fallback, configured text, formatting success, invalid-format fallback, and missing-key fallback before production migration.
  - Result: five focused resolver cases pass through the UI contract.
- [x] `APH-101` Add `IGameTextResolver` and immutable fallback implementation under `Assets/Game/Scripts/UI/Contracts`.
  - Result: sealed stateless fallback added with no Unity, `Game.Configs`, or static mutable-registry dependency.
  - Validation: `[GameTextResolverValidation] result=Passed tests=5`; UI Contracts and Editor-test builds passed with 0 errors; commit `a6f48a8c5` pushed to `main`.
- [x] `APH-102` Add the composition-owned `GameTextResolverAdapter` wrapping the existing config-owned text source.
  - Result: stateless composition adapter delegates configured lookup, fallback, and formatting behavior without introducing a mutable registry.
  - Validation: `[GameTextResolverAdapterValidation] result=Passed tests=5`; UI Contracts, Composition, Runtime, Editor, and Editor-test builds passed with 0 errors; commit `72b9ecf5f` pushed to `main`.
- [x] `APH-103` Inject the resolver through Menu and Match UI runtime adapters without hierarchy search or static registration.
  - Result: Menu binds before router content installation; both Match initialization paths inject the resolver; unbound runtime roots retain the immutable fallback.
  - Validation: `[GameTextResolverInjectionValidation] result=Passed tests=4`; UI Contracts, UI Runtime, Composition, Runtime, Editor, EditMode-test, and PlayMode-test builds passed with 0 errors; commit `2f7c767fa` pushed to `main`.
- [x] `APH-104` Migrate the seven known `Game.UI.Runtime` text consumers listed in Current Red Gates.
  - Preserve every key and fallback string from commit `caaafe339`.
  - Preserve formatting culture and error fallback behavior.
  - Result: all 85 direct expressions now use the injected contract: 62 `Get` and 23 `Format`; 80 literal key/fallback pairs retain the baseline fingerprint and five dynamic expressions retain their original shapes. Existing runtime sink and command-wheel propagation use the same resolver without duplicate implementations or static state.
  - Validation: `[GameTextResolverConsumerMigrationValidation] result=Passed tests=4 gets=62 formats=23 expressions=85`; order-banner 16/16, command-feedback 15/15, and Build Drawer 25/25 focused cases passed; seven-assembly build matrix passed with 0 errors; commit `5358a0c1e` pushed to `main`.
- [x] `APH-105` Remove `using Game.Configs` from files compiled by `Game.UI.Runtime`.
  - Result: stale imports were removed from exactly the seven migrated consumers with no behavior change; migration counts, literal fingerprint, dynamic expressions, and resolver propagation remain locked.
  - Validation: `[GameTextResolverConsumerMigrationValidation] result=Passed tests=4 gets=62 formats=23 expressions=85`; UI Runtime, Composition, and Editor-test builds passed with 0 errors; commit `99f2ccee4` pushed to `main`.
- [x] `APH-106` Remove `Game.Configs` from `Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef`.
  - Result: `Game.UI.Runtime` no longer references `Game.Configs`; config-owned audio source files now live under the `Game.Configs` assembly root with their Unity GUIDs preserved.
- [x] `APH-107` Add a guard that enumerates UI runtime source files and fails on future `Game.Configs` imports or direct `GameText` calls.
  - Result: the ownership-aware guard enumerates files compiled by `Game.UI.Runtime`, excludes nested asmdef roots, rejects the forbidden asmdef reference, `using Game.Configs`, and direct `GameText` calls, while allowing real assembly-attribute files.
- [x] `APH-108` Run UI text focused tests, `Game.UI.Runtime`, `Game.Composition`, and `Game.Tests.Editor` builds, then run the 31-check assembly-boundary validation.
  - Acceptance: `[ScriptArchitectureBoundaryValidation] result=Passed tests=31`.
  - Validation: resolver contract `5/5`, adapter `5/5`, injection `4/4`, migration `4/4` with 62 gets, 23 formats, and 85 expressions; `Game.UI.Runtime`, `Game.Composition`, and `Game.Tests.Editor` built with 0 errors; assembly boundary passed `31/31` after POP-13 integration. Commits `fabfbc947` and `afe9e7d74` are pushed to `main`.

## Phase 2 - Restore the ECS Hot-Path Gate

Goal: remove all remaining synchronous hot-path query snapshots without changing gameplay behavior or losing the measured FPS repair.

### Phase 2A - Resource Exchange

Implementation contract:

- Replace `exchangeQuery.ToEntityArray(Allocator.Temp)` with direct `SystemAPI.Query` iteration over the required component and buffer set.
- Preserve support for multiple exchange entities and deterministic per-entity processing.
- Preserve request order, result order, wallet mutation, queue mutation, economy events, and summary publication.

- [x] `APH-200` Add a regression with two exchange entities proving both queues process in one update.
  - Result: deterministic two-entity request, wallet, queue, result, economy-event, and summary ordering is locked.
- [x] `APH-201` Add or extend zero-allocation coverage for a warmed resource-exchange update.
  - Result: 64 warmup and 512 measured two-entity updates report `productionUpdateAllocatedBytes=0`.
- [x] `APH-202` Replace the temporary entity array with direct ECS query iteration.
  - Result: direct multi-entity `SystemAPI.Query` iteration replaces the native snapshot; missing `AllowAiExchange` summary propagation is restored.
  - Validation: behavior tests passed 4/4; allocation tests passed 2/2; Runtime/Editor-test builds passed with 0 errors; commit `b0b5f7e08` pushed to `main`.
- [x] `APH-203` Confirm no new `EntityManager.CreateEntityQuery`, structural sync point, managed allocation, or singleton assumption was introduced.
  - Result: focused source guard locks direct multi-entity iteration and rejects query creation, dependency completion, snapshots, temporary allocation, singleton APIs, and common managed-allocation tokens in `OnUpdate`.
  - Validation: `[ResourceExchangeArchitectureGuardrail] result=Passed tests=6`; commit `5dd7f26d4` pushed to `main`.
- [x] `APH-204` Run `ResourceExchangeRequestValidationSystemTests.RunFocusedValidation` and `ResourceExchangeGcAllocationValidationTests.RunFocusedValidation`.
  - Result: behavior passed 4/4 and warmed allocation passed 2/2 across 64 warmup and 512 measured frames with two exchange entities and `productionUpdateAllocatedBytes=0`.
  - Logs: `/private/tmp/warline-aph204-resource-request-validation.log` and `/private/tmp/warline-aph204-resource-gc-validation.log`.

### Phase 2B - Building Defense

Current-head overlap:

- Commit `a6af1335c` changed this system to acquire targets every 0.12 seconds, copy query data into contiguous arrays, and preserve per-frame firing cooldown updates. It also added focused performance diagnostics and supporting tests.
- That repair is the performance floor for this phase. Preserve or improve its focused benchmark and do not restore per-frame target acquisition or random per-candidate `EntityManager` reads.
- Commit `b97e2a2d1` removed the one entity snapshot and three component snapshots while preserving the 0.12-second acquisition cadence. The remaining Phase 2B debt starts with explicit dependency completion and direct `EntityManager` access under `APH-209`.

Immediate implementation contract:

- Add a persistent native target scratch collection owned and disposed by `BuildingDefenseAttackSystem`.
- Rebuild it through direct ECS iteration without per-frame allocator use.
- Preserve the current target eligibility, nearest-target ordering, faction filtering, air-target exclusion, four-slot behavior, cooldowns, tracer cadence, damage, audio, VFX, and recent-attacker/health-bar effects.
- Remove explicit `state.Dependency.Complete()` only when lookups/query dependencies make direct access safe. Do not hide the sync in another helper.
- This immediate phase does not introduce the shared spatial index; that is `APH-704`.

- [x] `APH-205` Extend defense tests for neutral targets, aircraft exclusion, nearest-hostile ordering, four concurrent slots, destroyed targets, and target removal between updates.
  - Result: six focused regressions cover every required behavior and preserve slot order, cooldown, shot-count, damage, tracer, recent-attacker, audio/VFX-producing configuration, and reacquisition semantics.
  - Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=6`; Editor/test builds passed with 0 errors; commit `437587eea` pushed to `main`.
- [x] `APH-206` Add a warmed allocation test covering at least 32 towers and 740 candidate units.
  - Result: 32 towers, 740 candidates, 32 warmup updates, and 64 measured acquisition updates report `productionUpdateAllocatedBytes=0` with four-slot order preserved.
  - Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=7`; commit `cd7646340` pushed to `main`.
- [x] `APH-207` Add persistent target scratch storage with explicit `OnCreate`/`OnDestroy` lifetime.
  - Result: `BuildingDefenseAttackSystem` owns a persistent `NativeList<TargetCandidate>`, clears it at acquisition cadence, and disposes it explicitly in `OnDestroy`.
- [x] `APH-208` Replace `_targetQuery.ToEntityArray(Allocator.Temp)` and remove the per-frame snapshot.
  - Result: direct `SystemAPI.Query` iteration rebuilds one structure-of-record scratch list and excludes aircraft and debug targets without any query snapshots.
  - Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=9`; 32-tower/740-candidate warmed allocation remains zero; Runtime, Editor, and Editor-test builds passed with 0 errors; commit `b97e2a2d1` pushed to `main`.
- [x] `APH-209` Audit the explicit dependency completion and direct `EntityManager` calls; remove or document each unavoidable synchronization point with profiler evidence.
  - Result: 21 local `EntityManager` data operations were replaced with explicit component/entity lookups. One documented `state.Dependency.Complete()` remains before five uniquely guarded helper/structural boundaries because removing it now would move synchronization into helper-owned access rather than eliminate it.
  - Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=10`; the 32-tower/740-candidate warmed update remained at `productionUpdateAllocatedBytes=0`; commit `13ff44928` pushed to `main`.
  - Profiler evidence: the 240-frame capture passed at average `4.138 ms` and p95 `4.325 ms`. `BuildingDefenseAttackSystem` recorded `13.88 ms` total, `13.67 ms` self, `0.058 ms/frame` average total, `1.09 ms` maximum, and 238 calls. All nested completion/helper/ECB work therefore totaled at most `0.21 ms`, below `0.001 ms/frame`.
  - Artifact: `/private/tmp/warline-aph209-building-defense-summary.md` from `/private/tmp/warline-aph209-building-defense.raw`.
- [x] `APH-210` Add profiler markers for target collection, target selection, and effect application so the later spatial-index phase has comparable metrics.
  - Result: three static zero-allocation markers cover collection, per-tower selection, and immediate plus deferred effect application; the Match capture runner registers and records them deterministically.
  - Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=11`; warmed allocation remained zero; 240-frame Match capture passed at p95 `3.691 ms`; commit `190140146` pushed to `main`.
  - Marker metrics: collection average/max `0.121/0.135 ms`; selection `0.904/0.922 ms`; effect application `0.001/0.006 ms`.
- [x] `APH-211` Run defense, automatic-faction-targeting, combat, audio, and VFX focused validations.
  - Result: integration coverage now locks defense feedback overwrite behavior, configured weapon audio and muzzle/impact VFX payloads, recursive LOD exclusivity, current unit/aircraft combat audio, and air-missile launch/impact audio without structural changes inside ECS iteration.
  - Validation: Defense `13/13`, automatic faction targeting `3/3`, combat/death `6/6`, gameplay audio `8/8`, building audio `4/4`, and unit combat/VFX `3/3` passed. Runtime and Editor-test builds completed with 0 errors; commits `789fd4a03` and `e55729370` pushed to `main`.
- [x] `APH-212` Run `EcsBurstHotPathArchitectureTests.RunFocusedValidation`.
  - Acceptance: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10` and zero snapshot debt.
  - Result: the remaining resource-hauler temporary entity snapshot was replaced with reusable scratch populated from temporary chunk metadata that is disposed before structural changes; recurring audio singleton writes use `SystemAPI.GetSingletonRW`; all managed boundaries and the two remaining tracked hot paths are explicitly classified.
  - Validation: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`; 173 ECS `OnUpdate` implementations, 58 Burst, 45 job-backed, 115 non-Burst, two tracked hot paths, zero unclassified, and zero snapshot debt. Resource production/hauler passed `36/36`, audio request/state passed `5/5`, five first-party assemblies built with 0 errors, and the gate remained green after POP-13 integration. Commits `401bd1435`, merge `87339b802`, and `afe9e7d74` are pushed to `main`.
- [x] `APH-213` Re-run editor performance and steady-state GC baselines; reject the slice if frame p95 or player-relevant GC regresses beyond noise.
  - Result: the first capture exposed a POP-13 runtime archetype overflow caused by large assistant buffers sharing the already-large UI shell entity. All ten shell-owned assistant/objective buffers now use zero inline capacity, preserving their ECS API while moving row storage outside the chunk; a full-shell regression covers every buffer plus narration and control-owner state.
  - Performance: at 733 units and 628 runtime buildings, average improved from `3.42 ms` to `2.49 ms`, p95 from `5.22 ms` to `3.82 ms`, p99 from `6.87 ms` to `5.83 ms`, and maximum from `18.13 ms` to `14.52 ms`; current-thread allocation remained zero.
  - GC: player-relevant steady-state allocation improved from `269,482` to `234,324` bytes over 300 measured frames after 180 warmup frames. The unchanged `1,024`-byte gate remains red and is mandatory downstream debt.
  - Validation: assistant read models `9/9`, full assistant rollout `12/12`, assembly boundary `31/31`, ECS/Burst hot path `10/10`, and six first-party assembly builds passed with 0 errors. Commits `77f7581da` and `6cccb42c1` pushed to `main`.

## Phase 3 - Unify Settings, Frame Rate, and Environment Ownership

Goal: remove competing settings/render owners and apply persisted settings predictably.

Target ownership:

| State | Owner |
|---|---|
| PlayerPrefs load/save and settings change event | `SettingsService` |
| Startup and change routing | Menu/Match composition |
| Frame-rate and quality-level application | composition-owned runtime settings application path |
| URP asset, render scale, AA, post-processing tier | `VisualQualitySettingsSystem` |
| Dynamic sun, ambient, fog, skybox, day/night volume | `DayNightSystem` |

- [x] `APH-300` Add tests proving persisted settings are applied during app startup without opening the settings popup.
  - Result: the real `MenuBootstrapView.Awake` lifecycle loads persisted non-default audio, graphics, frame-rate, accessibility, and assistant values, projects audio and assistant settings through the runtime event bridge, and leaves the popup request buffer and hierarchy empty.
  - Validation: audio/settings startup `7/7`, Settings popup `8/8`, assistant persistence `3/3`, and `Game.Tests.Editor` built with 0 errors; commit `25b50b0bf` pushed to `main`.
- [x] `APH-301` Add Android tests proving 120 FPS is never applied on Android and 30/60 remain valid.
  - Result: one shared policy normalizes Android 120 FPS requests to 60 before target-frame-rate application and runtime event publication; 30 and 60 remain unchanged.
- [x] `APH-302` Add an editor/standalone test preserving 120 FPS where supported.
  - Result: non-Android defaults, normalization, and target resolution preserve the 120 FPS mode and target.
- [x] `APH-303` Change mobile defaults from 120 FPS to 60 FPS and clamp saved legacy 120 FPS values on Android.
  - Result: platform-aware defaults, load, save, and apply paths all use the same normalization policy; Android saves migrate 120 to 60 while non-Android persistence remains unchanged.
  - Validation: Android visual/frame-rate `8/8`, desktop/settings projection `8/8`, Settings popup `8/8`, assembly boundary `31/31`, and UI Runtime, Composition, and Editor-test builds passed with 0 errors; commit `74f42e192` pushed to `main`.
- [x] `APH-304` Apply `SettingsService.Load()` through the composition startup path exactly once after required runtime owners exist.
  - Result: audio and assistant ECS owners subscribe without independently loading settings; Menu composition defers until both owners exist, then applies persisted settings once per ECS world across repeated `Awake`, `OnEnable`, and `Update` entries.
  - Validation: settings integration `8/8`, Menu settings/audio smoke passed, Match settings/audio smoke passed at HUD ready, ECS/Burst hot path `10/10`, UI Shell ECS, Composition, and Editor-test builds passed with 0 errors; commit `7c13bf7e7` pushed to `main`.
- [x] `APH-305` Route settings-change events through composition to `VisualQualitySettingsSystem`; ensure subscriptions are removed on shutdown/domain reload.
  - Result: Match composition subscribes once to the settings runtime event, maps the graphics tier to the visual-quality runtime mode, applies the latest value when the managed visual owner becomes ready, and unsubscribes on shutdown; subsystem registration clears stale static subscribers across domain-reload-disabled sessions.
- [x] `APH-306` Give `VisualQualitySettingsSystem` an explicit apply-on-change API and remove its manual per-frame `Update` call from Match bootstrap.
  - Result: visual quality exposes `ApplyRuntimeMode`, reports its initialized/applied state for composition and validation, and is no longer polled from the Match bootstrap frame loop.
  - Validation: Android visual-quality `10/10`, Match settings/audio smoke passed at HUD ready, ECS/Burst hot path `10/10`, UI Runtime, Game Runtime, Composition, and Editor-test builds passed with 0 errors; commit `c92d3095e` pushed to `main`.
- [x] `APH-307` Split static tier application from dynamic environment application. After initialization, visual-quality code must not overwrite Day/Night sun, ambient, fog, skybox, or volume values every frame.
  - Result: visual quality now owns only pipeline, render scale, post-processing profile selection, camera post-processing/AA, ground variation, and a scalar shadow-budget policy. Day/Night remains the sole writer of sun, ambient, fog, skybox, volume values, and final light shadow strength; a changed tier triggers one Day/Night volume-profile rebind and current-hour reapplication.
- [x] `APH-308` Add an ownership regression that advances Day/Night, runs visual-quality handling, and proves the Day/Night values remain authoritative.
  - Result: the focused regression advances Day/Night to 22:00, switches Ultra to High through the production settings/composition path, and compares sun color/intensity/rotation/shadow, ambient mode/colors/intensity/reflection, fog mode/color/density, runtime skybox, volume weight, and representative color-adjustment, white-balance, and bloom values.
  - Validation: Android visual-quality `11/11`, Match settings/audio smoke passed at HUD ready, ECS/Burst hot path `10/10`, Unity compiled the final code with 0 C# errors, and the EditMode runtime-skybox cleanup warning was removed; commit `7167baa75` pushed to `main`.
- [x] `APH-309` Verify Low, Balanced/High, and Ultra mappings, including render pipeline, render scale, post-processing, AA, and shadow strength.
  - Result: one runtime matrix drives the real visual-quality system through Low, Medium/Balanced, High, and Ultra against the active project profile and verifies pipeline asset, render scale, volume profile, camera post-processing, camera AA, and Day/Night shadow cap for each mode.
  - Validation: `[AndroidVisualQualityValidation] result=Passed tests=12`, final Unity compilation had 0 C# errors, no project setting or render asset remained dirty, and commit `dcde564ca` pushed to `main`.
- [x] `APH-310` Run `SettingsPopupValidationTests.RunFocusedValidation`, `AndroidVisualQualityValidationTests.RunFocusedValidation`, Match smoke, and visual captures at day, dusk, and night.
  - Result: integrated settings/graphics/Match gates passed, and the capture utility now fixes project-correct 12:00 day, 21:00 dusk, and 23:00 night states with normal gameplay and max-zoom day views.
  - Validation: Settings popup `8/8`, Android visual quality `12/12`, Match settings/audio smoke passed at HUD ready, and current-profile Metal capture completed at 1920x1080; commit `fd10d99e6` pushed to `main`.
  - Visual review: day and dusk pass structural review; night is nonblank but remains a red readability finding at mean luma `22.34 / 255`. No lighting value was silently changed.
- [~] `APH-311` On Android, run a 10-minute 30 FPS tier capture and a 60 FPS tier capture. Record frame, GPU, memory, thermal, and visual results separately.
  - Current evidence: Xiaomi `24090RA29G` was unlocked and kept foreground for the transient 96 m combine candidate. Match reached static-map startup `3.24 s` after the deferred load request and gameplay-ready `6.60 s` after that request. The user observed `45 FPS`; the 2,000-frame development capture confirms steady avg `22.17 ms / 45.1 FPS`, p95 `26.80 ms`, p99 `31.13 ms`, GPU avg/p95 `20.23/20.87 ms`, `1,058,417` avg triangles, and thermal status `0`.
  - Remaining: this short candidate capture does not replace the required separate 10-minute 30 FPS and 60 FPS tier sessions. Run both only after an Android implementation passes loading, visual, and steady frame-time acceptance.

## Phase 4 - Reduce Audio Residency

Goal: reduce persistent audio memory without clipping, first-play stalls, or broken event routing.

Known baseline:

- Menu persistently references `AudioEventCatalogConfig.asset`.
- The catalog contains 226 clip references.
- There are 262 project WAV importers: 253 DecompressOnLoad/preloaded and 9 Streaming/not preloaded.
- Existing profile policy already specifies Streaming for cataloged Music and Ambience.
- Existing profile policy specifies DecompressOnLoad/preload for Voice. Voice is the first measured optimization candidate.

- [x] `APH-400` Extend the content-residency report to list only catalog-referenced audio clips by bus/category, duration, channels, frequency, import load type, compressed size, and estimated decoded size.
  - Result: schema-2 JSON and Markdown now list the 234 source-parity-guarded serialized catalog clips only, grouped and detailed by bus/category with event IDs, duration, channels, sample rate, active Android importer load type, measured compressed storage-memory size, and decoded PCM estimate; unreferenced legacy clips are excluded from the catalog section.
  - Validation: focused content-residency `8/8`, Android report generation passed with 4,110 included assets, 234/234 catalog clips, zero warnings, 580.358 seconds duration, 49.41 MiB measured compressed bytes, and 97.77 MiB estimated decoded bytes; runtime catalog parity passes as part of the `14/14` audio contract.
- [x] `APH-401` Capture Menu and Match audio memory before playback and after representative UI, combat, music, ambience, and ARIA voice playback.
  - Result: controlled batchmode Menu and Match captures begin from zero active sources, wait for each requested clip to load and catalog residency to stabilize, normalize event timing to one capture clock, and record clip/bus residency plus contextual process counters. Menu covers UI and music; Match covers small-arms combat, music, ambience, and ARIA voice.
  - Evidence: `Design/AgentReports/aph-401_audio-memory-playback-menu.{json,md}` and `Design/AgentReports/aph-401_audio-memory-playback-match.{json,md}` record exact commit `8e1f21c2a`, catalog SHA-256, raw-profiler size/hash, stable clip counts, and all requested events as `Presented`; raw captures remain transient under `/private/tmp` and are reproducible through the recorded invocation.
  - Validation: capture contract `7/7`, audio source/runtime catalog contract `14/14`, Menu `3/3` phases, Match `5/5` phases, two-pass code/evidence review with no remaining findings; implementation commits `c2ead0fa1`, `8e1f21c2a`, and evidence commit `0aecb723b` pushed to `main`.
- [x] `APH-402` Identify catalog drift: cataloged Music/Ambience clips that do not match the existing Streaming profile and unused legacy clips that do not affect runtime residency.
  - Result: all nine Music/Ambience references agree between source JSON and the serialized catalog and match Streaming/not-preloaded/background-load policy; zero catalog importer mismatches were found. Six unreferenced legacy files are isolated as build/cleanup inventory and do not affect runtime catalog residency.
- [x] `APH-403` Correct cataloged Music/Ambience drift without changing event IDs or playback behavior.
  - Result: no importer correction was necessary. The stale audio contract source paths were updated to the current `Configs/Audio` ownership location; runtime audio, clips, event IDs, catalogs, and importers remain unchanged.
  - Validation: `[AudioConfigContractValidation] result=Passed tests=13`, deterministic raw-file audit found `0 / 9` catalog mismatches, and commit `e2eb61b3f` pushed to `main`.
- [x] `APH-404` Change the Voice import profile pilot to `CompressedInMemory`, `preloadAudioData=false`, and `loadInBackground=true` for a representative ARIA subset.
  - Result: JSON-driven import profiles now define an explicit `VoicePilot` profile and an eight-clip ARIA allowlist spanning warnings, command instructions, command rejection, tactical feedback, and confirmation. The remaining Voice catalog retains the prior decompressed/preloaded policy until Android evidence accepts expansion.
  - Validation: importer application covered 234 catalog clips with exactly eight overrides; the bounded pilot contract passed `8/8`, and `AudioConfigContractTests.RunFocusedValidation` passed `14/14` in Unity.
- [x] `APH-405` Measure first-play latency, repeated-play latency, decoded memory, compressed memory, and audio glitches on Android for the pilot.
  - Result: the explicit development-build probe discovered and passed exactly eight on-demand compressed Voice clips on Xiaomi 24090RA29G. Cold audible-start latency measured `16.361-33.396 ms` (`27.456 ms` average), repeated latency measured `16.264-33.724 ms` (`20.236 ms` average), and total loaded runtime clip memory was `157,184` bytes versus `2,103,058` compressed inventory bytes and a `3,734,052`-byte decoded estimate.
  - Glitch evidence: primary AudioFlinger partial/empty underruns and delayed writes remained `0`; the existing device track-underrun counter remained `20` before and after. Probe summary passed `8/8` with no failed marker, fatal exception, or ANR. Subjective speaker/headphone quality remains owned by the later audible-smoke gate.
  - Validation: probe contract `6/6`, Android profiler APK build passed with zero compiler errors, deterministic static-map rebake reused all 525 scenes, and evidence is recorded in `Design/AgentReports/2026-07-11_aph-405_android_voice_pilot.md`.
- [x] `APH-406` If the pilot passes, apply the Voice policy through the existing JSON-driven audio importer workflow and update contract tests. If it fails, record the failing devices/clips and use a bounded prewarm set instead of reverting to global preload.
  - Result: all 163 cataloged Voice clips now inherit `CompressedInMemory`, mono, non-preloaded, background-loaded 44.1 kHz Vorbis settings from the category-level JSON profile. The temporary profile and eight per-clip overrides are removed; the original measured paths remain frozen in `validationSets.APH405VoicePilot`.
  - Validation: 155 newly changed importer metas plus eight already-compliant pilot metas pass the live 234-clip Unity contract `14/14`; rollout and catalog-analysis Python contracts pass `9/9`, the complete CI Python suite passes `49/49`, and repeated importer application is byte-idempotent. Evidence: `Design/AgentReports/2026-07-11_aph-406_voice_importer_rollout.md`.
  - Measurement cleanup: the completed one-shot development probe and its passive-view hook were retired because broad policy discovery would no longer represent the frozen eight-clip pilot. Production playback behavior is otherwise unchanged.
- [x] `APH-407` Evaluate splitting the persistent catalog into Core/Menu, Match, and Voice catalogs. Open a separate implementation slice only if importer changes do not meet the memory target.
  - Decision: do not open a catalog-split implementation now. Current evidence attributes `39.84 MiB` of the `43.34 MiB` loaded baseline to 163 Voice clips, while Core/Menu accounts for `0.81 MiB` and Match for `2.69 MiB`; the theoretical `42.53 MiB` Core/Menu-only reduction is not accepted because unload behavior is unproven.
  - Reopen condition: only if the measured full Voice importer policy still misses the accepted memory target. Evidence and deterministic tooling: `Design/AgentReports/2026-07-11_aph-407_audio_catalog_split_analysis.{json,md}` with focused tests `6/6`.
- [x] `APH-408` Run `AudioConfigContractTests.RunFocusedValidation`, `AudioPerformanceValidationTests.RunFocusedValidation`, audio scene binding tests, and an Android audible smoke pass.
  - Result: user explicitly approved the audio changes on 2026-07-14 after config/importer `14/14`, performance `4/4`, and listener/scene `6/6` passed; Android playback produced non-zero amplitude, survived 90 seconds, emitted no listener/clip/crash/ANR error, and added zero mixer underruns or delayed writes. Objective evidence: `Design/AgentReports/2026-07-11_aph-408_full_voice_android_smoke.md`.

## Phase 5 - Texture, Animation, Build, and Memory Policy

Goal: make residency and package size intentional while preserving visible quality.

- [x] `APH-500` Produce a BuildReport-based top-100 included-asset table for Android APK and AAB builds.
  - Tooling: release APK/AAB builds now request `DetailedBuildReport`, deterministically aggregate `BuildReport.packedAssets`, preserve attributed/unattributed/overhead accounting, and write top-100 JSON/Markdown evidence with commit, dirty-state, architecture, artifact size, and SHA-256. Jenkins archives each report with its matching artifact.
  - Validation: focused aggregation/evidence contract `6/6`, Unity compile clean, `git diff --check` clean; tooling commit `671a009aa` pushed to `main`.
  - Evidence: clean ARM64 IL2CPP release APK and AAB reports were generated and reviewed. APK artifact `463,359,198` bytes with SHA-256 `cb18f212...824f20`; AAB artifact `426,399,778` bytes with SHA-256 `c03558f2...7ac29`; evidence commits `a527e151e` and `ddfca3b27` are pushed to `main`.
- [~] `APH-501` Add tracked budgets for APK/AAB size, installed size, peak allocated memory, texture memory, mesh memory, audio memory, and graphics-driver memory.
  - Active result: schema 4 pins clean ARM64 IL2CPP release budgets at APK `<= 463,359,198` bytes and AAB `<= 426,399,778` bytes, including exact artifact commit, SHA-256, immutable evidence commit, and report path. The baseline clean `bcee23f3fceb76a33ad0863aaa4dbd22e3d779ad` APK is `512,082,081` bytes. The bounded BrightCommand policy applies Android ASTC 4x4 and removes screen-space mip chains from exactly 60 sprites; the supporting policy applies mipless Android ASTC 6x6 to 143 Match HUD/V15C sprite sources. Exact clean commit `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` produces a `449,902,496`-byte APK with SHA-256 `b4ce2922a33208bb348912b08160cf9aa5078144797b81b9b9b8ad31c7268eaf`: `62,179,585` bytes below the failing baseline and `13,456,702` below the accepted ceiling. Package size now passes without changing offline First Launch or operation-map content. A source audit found 72 scoped textures still CPU readable and modeled at most `6.31 MiB` of effective current-build CPU-copy residency; no production pixel-read dependency was found, but importer mutation is deferred until the current APK supplies measured release-device evidence. Device memory and visual acceptance remain pending. Installed size and absolute peak/texture/mesh/audio/graphics-driver memory limits remain fail-closed `null` until accepted release-device collection. Evidence: canonical APK BuildReport, focused policy XML under `/private/tmp`, and `Design/AgentReports/2026-07-15_aph-501_ui_texture_cpu_readability_audit.md`.
- [~] `APH-502` Classify texture importers into UI, world albedo, world normal/mask, VFX, impostor/atlas, generated source/reference, and excluded/unreferenced groups.
  - Active result: deterministic tooling provisionally classifies all 3,464 tracked importers by semantic path/YAML evidence and exposes 105 overlaps. BuildReport schema 1 now preserves the top-100 table while exporting every reported `Texture2D` path in a separate deterministic path-sorted table; the classifier accepts it only for an exact clean current-revision Android release report and fails closed on missing markers, malformed rows, duplicates, or unstable order. Focused BuildReport validation passes `10/10` and classifier validation passes `4/4`. Final inclusion/exclusion buckets remain explicitly unaccepted until a clean same-revision Unity residency inventory and regenerated complete Android BuildReport are available.
- [x] `APH-503` Add an editor guard preventing mip streaming on UI, font, animation-data, sprite-atlas, and generated reference/source textures.
  - Result: an editor `AssetPostprocessor` disables mip streaming only for protected UI/sprite, font, animation-data, sprite-atlas, and generated reference/source classes; malformed paths fail closed, the measured runtime impostor path remains explicitly allowed, and no recursive reimport API or unrelated importer mutation was added.
  - Validation: focused policy tests pass `18/18`, editor/test assemblies compile with zero errors, and the final Android profiler build completed after the one-time importer refresh.
- [~] `APH-504` Enable mip streaming for a representative world-texture subset with mipmaps; set a mobile memory budget and preserve full-resolution nearby textures.
  - Active result: a deterministic read-only pilot selector identifies `PolygonMilitary_Texture_01_A.png` and `PolygonMilitary_Texture_02_A.png` as the bounded representative pair and rejects unsafe expansion. Readiness remains fail-closed because the available build evidence is stale/incomplete, both source textures retain CPU-readable copies, normal-map importer evidence is incomplete, the global mip limit is `1`, and the required visual/memory evidence from `APH-505` and `APH-506` does not yet exist. Validation passes `11/11`; report generation is byte-deterministic; `--check` confirms `pilot_ready=false`, `mutation_authorized=false`, and `expansion_authorized=false`. Evidence: `Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.md`.
- [ ] `APH-505` Capture identical near, medium, and far camera screenshots before and after streaming. Reject blur, late pop, terrain seams, or missing vegetation detail.
- [~] `APH-506` Measure memory and I/O during a 10-minute camera-pan/zoom session. Expand streaming only after the pilot passes.
  - Active result: `Tools/CI/aph506_texture_streaming_device_collection.py` now enforces clean revision/APK/device provenance, 60-second warmup, 600-second alternating pan/zoom session, bounded foreground/PID/thermal/memory/I/O sampling, before/after screenshots and hashes, raw-log integrity, and fail-closed evidence output. Focused contracts pass `15/15`.
  - Remaining: run the collector on the pinned Android device with a clean candidate artifact and accept memory, I/O, thermal, and visual evidence before expanding streaming.
- [x] `APH-507` Audit Android texture overrides for ASTC size/quality and remove oversized 4K/8K limits only where the BuildReport proves inclusion and visual evidence proves no loss.
  - Accepted result: the clean deterministic audit covers all 60 current 4K/8K-limit candidates, records zero explicit Android overrides, preserves historical ASTC/build context, and blocks every limit reduction because no same-revision complete BuildReport plus hash-verified visual proof authorizes a candidate. No importer or asset was changed. Evidence: `Design/AgentReports/2026-07-15_aph-507_android_texture_override_audit.md` and `.json`.
  - Validation: focused contracts pass `14/14`, report `--check` passes on clean revision `ebdba0b4bb4c59963ee07ae84a96b2071eab9f8f`, and `limitReductionAuthorized=false` remains fail-closed.
- [~] `APH-508` Audit the six generated animation textures for actual runtime residency, duplication, clip coverage, precision requirements, and unload behavior. Do not change format based only on source file size.
  - Active result: deterministic audit evidence covers six distinct `RGBAHalf` textures totaling `100,663,296` payload bytes, finds no exact pixel duplicates, identifies only the three `CharactersBaked` textures in the truncated top-100 Android build evidence at approximately `48 MiB` packed, and records 430 animation entries across 33 character animators (`67.95%` current coverage). All six importers retain `m_IsReadable=1`, and the generator uses parameterless `Apply()`, so CPU-copy retention remains a concrete risk. Linear point-filtered `RGBAHalf` remains unchanged because lower precision is not proven safe.
  - Validation: deterministic audit coverage passes `13/13`. Build inclusion for the legacy three textures, runtime unload behavior, and device residency remain unproven.
  - Remaining: measure device residency, CPU-copy retention, and post-unload memory, then establish or reject an explicit production release path. Evidence: `Design/AgentReports/2026-07-11_aph-508_animation_texture_audit.md`.
- [~] `APH-509` Remove unused packages only after a package-usage report proves no source, serialized asset, build script, or editor workflow dependency.
  - Active result: a deterministic read-only inventory classifies 47 manifest packages, one embedded depth-zero manifest discrepancy, 20 ordinary lock-only transitives, 15 static candidate-unused packages, and two evidence-derived external-editor blind spots across source, serialized assets, build scripts, editor workflows, built-in component signatures, and ambiguity evidence. Explicit probes classify Cloth and Wind as static-only candidates, preserve Rider and Visual Studio as fail-closed external-workflow ambiguities, and retain Umbra because first-party occlusion usage exists. No package removal is accepted until isolated import/compile/test/build/device validation proves it safe.
  - Validation: 12 dedicated inventory tests, deterministic report regeneration, report freshness/count checks, and whitespace checks pass. Evidence: `Tools/CI/aph509_package_usage_inventory.py`, `Tools/CI/tests/test_aph509_package_usage_inventory.py`, and `Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md` plus `.json`.
- [~] `APH-510` Rebuild Android, compare BuildReport/memory/frame metrics, and record per-category deltas.
  - Active result: `Tools/CI/aph510_android_category_comparison.py` binds baseline/candidate evidence to exact revision, APK/AAB hashes, build profile, and device identity; compares package, installed, allocated/PSS, texture, mesh, audio, graphics-driver, frame, startup, and I/O metrics; and emits deterministic JSON/Markdown while null measurement-required limits remain explicit blockers. Focused contracts pass `9/9`. The accepted package candidate reduces the APK from `512,082,081` to `449,902,496` bytes, a `62,179,585`-byte reduction with `13,456,702` bytes of package-budget headroom.
  - Remaining: verify UI visual output, then produce AAB, release-device performance, category-residency, and I/O evidence and accept the comparison.

## Phase 6 - Editor-Baked Static Map Chunks

Goal: keep the exact authored Match appearance while replacing runtime hierarchy scanning and mesh combination with validated editor-baked presentation chunks that preserve shared meshes and support Unity 6 GPU Resident Drawer culling.

Quality lock:

- The canonical authored `Assets/Game/Scenes/Match.unity` remains the source.
- Do not move, rotate, scale, delete, procedurally regroup, or regenerate authored buildings, interiors, roads, props, vegetation, or terrain.
- Generated presentation chunks must preserve source meshes, world transforms, materials, lightmap indices/scale offsets, layers, shadows, probes, bounds, and culling behavior.
- Keep `MapSurfaceAuthoring`, `MapBakeGroupAuthoring` descendants needed by overlays, Buildings/Vehicles authoring roots, colliders, scripts, LOD hierarchies, property-block renderers, and unsupported GPU-driven renderers in the base Match scene.
- Direct `Mesh.CombineMeshes` output is rejected by `D-019`. Do not duplicate source vertex/index payloads in the accepted path.
- Every visual acceptance requires top-down, oblique, low-ground, and gameplay-camera captures. Use Unity MCP for hierarchy inspection/screenshots when available.

- [x] `APH-600` Add startup profiler markers and structured metrics around `StaticMapChunkBatchingPresentationSystemHelper.Initialize`.
  - Result: Initialize now exposes profiler markers plus structured scan, grouping, combine, disable, and total timing metrics while preserving the existing log fields and runtime behavior; commit `aef56c454` pushed to `main`.
- [~] `APH-601` Capture current startup duration, renderer scan count, eligible/skipped counts, generated vertices, CPU mesh memory, GPU mesh memory, and peak startup allocation.
  - Captured: canonical Match is `58,822,096` bytes with `21,433` GameObjects and `21,226` renderers. Runtime batching reported `2,501` eligible, `123` batches, `2,456` disabled, `1,004,672` generated vertices, `14,407` unreadable skips, `696` unsafe skips, and `167` oversized skips. Android native `Loading.Preload` saturates one core at 9%; pre-candidate memory was `1.525 GB` PSS with `704.8 MB` graphics after texture-policy reductions.
  - Candidate evidence: the ignored transient 96 m Android build generated `602` meshes with `9,835,246` vertices, reduced scene objects `21,433 -> 5,891`, reduced the scene file `58,822,096 -> 14,292,922` bytes, and loaded Match instead of stalling at 9%. Runtime static-map initialization scanned `4,409` renderers in `38.454 ms`, culled `227` renderers / `934,377` triangles, and settled around `1,077 MB` Unity allocated / `1,197 MB` reserved. The development APK SHA-256 is `b7736ccbc07c74b0612df4ec12a9dd87d9fba2a9e68573d883d062f79699736f`.
  - Finer-culling experiment: a 32 m transient variant generated `1,222` meshes / `9,479,784` vertices and reduced submitted gameplay geometry to about `0.875 M` triangles. It briefly reached about `49.9 FPS` with `18.4-19.6 ms` GPU time, then fell to `41-43 FPS` with `25.9-26.6 ms` GPU time on the already-warm device at `62.1 C`; Android thermal status and cooling-device values remained `0`. It still misses the 60 FPS gate and increases generation time to `314.6 s`.
  - Rejection: the steady profile is GPU-bound at `20.23 ms` average / `20.87 ms` p95 with about `1.06 M` submitted triangles and only `45.1 FPS`. The transient generator also removes source `MeshFilter` components beneath possible `MapBakeGroupAuthoring` ancestors, so it does not yet satisfy overlay/source-geometry preservation. Do not commit or ship this candidate.
  - Remaining: record exact CPU/GPU mesh memory and peak startup allocation for the accepted implementation. Preserve overlay-sensitive source geometry and require foreground load, settled memory, visual, and 60 FPS evidence before closing APH-601. Current-map reconciliation treats the older `525`-chunk / `17,564`-source measurements as historical-only; an exact clean descendant release revision must reproduce the audited `514`-chunk / `16,542`-source manifest hashes and counts. Evidence matrix: `Design/AgentReports/2026-07-14_aph-601_aph-609_current_map_evidence_matrix.md`.
- [x] `APH-602` Extract the current `BatchKey`, safety filters, chunk size, and mesh limits into editor-usable deterministic code without changing runtime behavior.
  - Result: `StaticMapChunkBatchingPolicy` owns deterministic key equality, renderer safety, source eligibility, chunk coordinates, and shared limits; focused policy/readback/Android preload tests pass `21/21`; commits `aef56c454` and `4c05a2da1` pushed to `main`.
- [x] `APH-603` Build an editor baker that writes additive shared-mesh presentation scenes under `Assets/Game/GeneratedStaticMapPresentation/` with a manifest containing source object IDs, source dependency hashes, chunk bounds, scene paths, and shared mesh/material IDs.
  - Result: the editor-only baker generated `17,564` shared-renderer source entries across `525` deterministic 32 m additive scenes and a schema-versioned manifest with canonical dependency hash, content hash, chunk bounds/paths, GlobalObjectIds, per-source hashes, and shared mesh/material GUID/local IDs.
  - Validation: Android visual/import `12/12`, runtime/editor .NET builds, Unity compilation, canonical-scene cleanliness, and full generated-output component/asset scans passed. Generated output is `63 MB`, contains zero embedded/generated mesh or material assets, zero colliders/scripts/prefab instances/static-batch flags, and zero duplicate source IDs. Evidence: `Design/AgentReports/2026-07-10_aph-603_static_map_presentation_bake.md`.
- [x] `APH-604` Add deterministic rebake and stale-output cleanup tests. Never delete output not listed in the manifest.
  - Result: direct-dependency provenance prunes generated branches transitively; layer changes affect source identity; a SHA-256 scene/meta integrity ledger rejects stale or corrupted output; manifest-owned writes/deletes are journaled and rolled back on failure; disk-present owned scenes cannot be orphaned by stale AssetDatabase state. Unity ownership tests pass `23/23`, the real AssetDatabase delete/rollback/GUID restoration test passes `1/1`, and the post-migration identical bake reused all `525` scenes with `scenesWritten=0` and `staleScenesDeleted=0`.
- [x] `APH-605` Add structural validation for source/chunk bijection, unchanged transforms, shared mesh/material identity, lightmap data, layers, shadows, probes, bounds, and an explicit zero-collider/zero-gameplay-script chunk contract.
  - Result: the exhaustive Unity validation passed `2/2` in `658.45 s`, proving the manifest/source/chunk bijection and full parity for all `17,564` renderers across `525` scenes, including transforms, assets, materials, renderer/lightmap/probe state, bounds, layers, and zero collider/MonoBehaviour presentation contracts.
- [x] `APH-606` Add a serialized presentation-manifest reference to `MatchSceneView` or an equivalent composition-owned config, and include every generated chunk scene in Android builds. Do not add a `GameObject.Find` fallback.
  - Result: `MatchSceneView` owns the serialized manifest reference; deterministic editor wiring updates the canonical Match scene without hierarchy-name lookup; both Android release and profiler build paths include enabled base scenes followed by the exact 525 manifest-owned chunks. The resolver fails closed for stale canonical dependencies, stale enabled output, missing base/chunk scenes, duplicate/non-owned chunks, schema/content drift, and integrity-ledger mismatch.
  - Validation: resolver `21/21`, scene wiring `2/2`, APH-700 `7/7`, APH-701/702 `15/15`, two identical post-wiring bakes with zero scene writes/deletes, and all 12 first-party assemblies with zero errors.
- [x] `APH-607` Add a managed presentation streamer that preloads the camera footprint plus one ring before Match-ready, then loads/unloads with hysteresis and one bounded operation queue. Unload every presentation chunk before unloading Match.
  - Result: one plain managed composition-owned streamer uses a 64-entry queue, one active operation, one start per frame, a one-ring preload, two-ring unload hysteresis, and at most 16 scene-state checks per update. Match start remains blocked until preload readiness, and Match unload remains blocked until all presentation chunks drain.
  - Lifecycle safety: route reversal waits for drain completion before rebinding; preload failures can still drain; detached operations retain scene ownership and clean up completed loads even if no replacement Match binds; repeated cleanup failure is fail-closed with explicit status.
  - Validation: focused behavior and composition tests pass `31/31`; production source-growth architecture validation passes `15/15`; APH-700 dependency validation passes `7/7`; all 12 first-party assemblies passed with zero errors before final review changes and the four changed-scope assemblies passed again with zero errors afterward. Independent review closed with no actionable findings.
- [x] `APH-608` Enable Unity 6 GPU Resident Drawer `InstancedDrawing` for the Mobile URP asset, retain BRG shader variants, disable Android static batching, and preserve a measured fallback for incompatible renderers. Do not add a new gameplay `ISystem` or `SystemBase` owner.
  - Result: Mobile Forward+ now uses `InstancedDrawing`; GPU occlusion and small-mesh culling remain disabled; BRG and standard instancing variants remain Keep All; Android static and dynamic batching are disabled. Unity retains incompatible renderers on the ordinary URP path without a custom gameplay owner.
  - Device evidence: instanced drawing improved stable FPS `32.6 -> 36.3`, GPU `28.82 -> 21.69 ms`, render thread `5.24 -> 3.07 ms`, and SetPass `49.2 -> 45.0` on Xiaomi 24090RA29G. Visual captures retained the map and HUD; no shader/BRG/crash errors occurred. The 60 FPS gate remains red.
- [~] `APH-609` Compare canonical/runtime-batched and shared-mesh chunked/GRD screenshots plus draw calls, triangles, CPU/GPU frame time, memory, package size, and startup time. Require pixel/visual review, not metrics alone.
  - Captured: same-source profiler comparison, final instanced screenshot, control screenshot, frame/GPU/render-thread/draw/SetPass/geometry metrics, development PSS, APK hash, thermal status, and crash/shader scan.
  - Remaining: cold-device repeat, release package/startup/memory evidence, canonical/runtime-batched comparison normalization, and full visual matrix before final acceptance. The fail-closed current-map matrix rejects all `525`-chunk evidence for current `514`-chunk acceptance and defines exact revision, manifest, artifact, device, thermal, and camera keys for the next collection. Evidence: `Design/AgentReports/2026-07-14_aph-601_aph-609_current_map_evidence_matrix.md`.
- [x] `APH-610` After acceptance, disable runtime mesh combination when a valid presentation manifest is present, remove duplicated base-scene visual renderers represented by validated chunk entries, and disable Read/Write only on meshes no accepted runtime path needs. Keep development-only stale/missing-manifest diagnostics.
  - Result: Android transactionally validates manifest shape and canonical renderer identity, suppresses all `17,564` manifest-owned canonical renderers, and skips the legacy runtime combiner. Editor sibling-index paths use a mesh/material/world-bounds identity fallback because Unity player stripping changes runtime sibling indices. Invalid manifests fail closed to the legacy path; teardown restores original renderer states.
  - Device evidence: the accepted run emitted `result=Presentation suppressed=17564` with no legacy batching marker, retained the complete gameplay/HUD view, reduced settled submitted geometry from `1.047 M` to `0.813 M` triangles, and improved the same-session last-ten average from `34.6` to `43.4 FPS`. Evidence: `Design/AgentReports/2026-07-11_map_presentation_ownership_android.md`.
  - Read/Write proof: the manifest uses 1,186 mesh sub-assets from 830 FBX importers. `758` are already unreadable. The remaining `72` stay readable because roads/decorations combine imported meshes at runtime or the accepted stale-manifest fallback can consume them. No speculative importer was changed. Evidence: `Design/AgentReports/2026-07-11_aph-610_static_map_mesh_read_write_proof.md`.
- [x] `APH-611` Run a full Android 10-minute soak and reject any missing interiors, floating/buried props, road changes, lighting changes, culling holes, or terrain-quality regression.
  - Result: Xiaomi 24090RA29G remained foreground and alive for `762.363` seconds of Match diagnostics. Valid captures at 60 seconds, 600 seconds, and after the extended run preserve terrain, roads, buildings, tents, walls, towers, vehicles, props, and HUD without an observed culling hole, floating/buried prop, lighting discontinuity, or terrain-quality regression at the tested camera.
  - Evidence boundary: the 300-second screenshot is rejected as an Android tiled readback artifact because black tiles crossed both HUD and world; the 600-second image and three immediate repeats are coherent. Skin thermal status reached `3`, so this run closes visual integrity and survival only, not release FPS, memory, or sustained-performance acceptance. Evidence: `Design/AgentReports/2026-07-11_aph-611_android_map_soak.md`.

## Phase 7 - Incremental Architecture Decomposition

Goal: reduce change risk in measured/high-change domains without a broad rewrite.

Priority order:

1. Combat/building defense, because Phase 2 exposes current targeting costs.
2. Resource exchange/economy, because current work recently added hot-path and cross-layer behavior.
3. Transport boarding, because `TransportBoardingCommandSystem` remains 3,226 lines.
4. UI shell, because `UiShellEcsGateway` remains 2,577 lines.
5. Selection/HUD, because several files exceed 1,500-2,000 lines.

The `APH-007` raw-metadata GC report is mandatory measured input for this phase. Its recurring scene-root, helper-construction, transport, UI shell, selection, command-query, audio, road, and pathfinding allocation owners must be removed or explicitly reassigned to another tracker task before `APH-711` can pass.

- [x] `APH-700` Add a generated domain dependency report listing every first-party assembly edge and top cross-domain type references.
  - Result: fail-closed deterministic JSON/Markdown reports now cover 19 assemblies, all 82 first-party edges, 99 external declarations, 1,208 owned source files, 2,573 visible types, and 30,514 resolved cross-domain occurrences. Context-aware scanning rejects comparison/member false positives while covering nested types/generics, casts, attributes, base lists, and using declarations; missing/stale reports and unsupported `.asmref` fail closed. Independent review found no remaining issue and Unity tests pass `7/7`.
- [x] `APH-701` Freeze new `*SystemHelper` and `*CompositionSystemHelper` files unless the architecture guard contains an approved task ID from this tracker.
  - Result: all 265 helper paths have exact line/byte ceilings and historical shrink/delete/recreation ratchets. Exceptions require one exact path, scope, line/byte ceiling, tracker task, decision ID, and unique marker in the canonical five-column Decision Log. Case, duplicate-property, malformed-history, and binary-numstat bypasses fail closed.
- [x] `APH-702` Add a size guard for new or growing production files: review required above 500 lines; no existing file above 1,000 lines may grow without an explicit tracker exception.
  - Result: all 108 reviewed production paths, including the 27 paths above 1,000 lines, have immutable line and byte ceilings that ratchet after accepted shrinkage and retirement. Jenkins performs a full-history/full-blob sparse clone including `Design`, runs EditMode unconditionally, and rejects missing, malformed, zero-test, incomplete, or failed results. The combined Unity guard passes `15/15`.
- [x] `APH-703` Inventory `World.DefaultGameObjectInjectionWorld` runtime call sites and the recurring `APH-007` allocation owners; classify composition edge, authoring/debug, presentation edge, hidden service-locator debt, or unrelated allocation debt.
  - Result: the accepted source inventory records 91 call sites across 44 runtime files as 37 composition edges, 24 presentation edges, 21 authoring/debug sites, and 9 hidden service-locator debts. It also maps all 13 recurring APH-007 owner methods covering 204,480 bytes / 75.9% of captured GC to explicit remediation tasks. Independent review reproduced every count and found no source-call-site overlap between the world-lookup set and allocation-owner set. Evidence: `Design/AgentReports/2026-07-10_aph-703_default-world-and-gc-owner-inventory.md`.
- [x] `APH-704` Design and benchmark a shared read-only unit spatial index for building defense, AI targeting, threat detection, and selection candidates. Adopt it only when it beats the Phase 2 direct-query baseline and preserves results.
  - Result: the second unmanaged `ISystem` shadow uses 256 fixed heads for 128-unit cells and compact 24-byte linked entries containing only entity, cell, source order, and next index. It stays `[DisableAutoCreation]`, preserves deterministic distance/source-order results and explicit stale/overflow/grid fallback, and adds no production Match work.
  - Acceptance: equivalence/fallback `9/9`; the post-rebase 740-unit/32-consumer benchmark measures direct/indexed complete acquisition at `2.723/0.684 ms`, a `74.88%` improvement, with zero warmed managed bytes and an `18,784`-byte payload. This passes the fixed `<1.025 ms`, `>=10%`, and `<=256 KiB` gates. Runtime adoption is deliberately deferred to a separately validated consumer-scheduling slice.
  - Rejected production candidate: the first building-defense consumer preserved behavior/fallback coverage at `18/18` and zero managed allocation, but every measured production bucket missed the fixed `<1.025 ms` acquisition gate. The best 128-cell run measured `1.258 ms` despite a `75.96%` relative improvement; 64 cells measured `1.260-1.571 ms`, and 32 cells measured `2.161-2.793 ms`. The candidate and its tests were removed, `[DisableAutoCreation]` remains, and Match runtime behavior is unchanged.
  - Evidence: `Design/AgentReports/2026-07-11_aph-704_shared_unit_spatial_index_plan.md`, `/private/tmp/warline-aph704-linked-equivalence-r2.log`, `/private/tmp/warline-aph704-linked-benchmark.{log,xml}`, `/private/tmp/warline-aph704-linked-ecs-burst.log`, `/private/tmp/warline-aph704-linked-non-ecs.log`, and `/private/tmp/warline-aph704-linked-boundary.log`.
- [x] `APH-705` Extract combat contracts/data required for a future `Game.Runtime.Combat` assembly without introducing a dependency cycle.
  - Result: shared observation ECS data remains in `Game.Components`; the implementation seam is isolated behind the cycle-free `Game.Runtime -> Game.Runtime.Combat -> Game.Components` direction, and deferred combat dependencies are documented. Evidence: `Design/AgentReports/2026-07-11_aph-705_combat_contract_extraction_plan.md`.
- [x] `APH-706` Split one cohesive combat slice and pass the full assembly boundary/build/test matrix before opening another domain split.
  - Result: `CombatDamageObservationBootstrapSystem` and `CombatDamageObservationUtility` now live in the dedicated combat assembly with their source GUID preserved. The regenerated dependency report contains the intended runtime/test edges and no reverse combat-to-runtime edge. Assembly boundary `31/31`, ECS/Burst `10/10`, source growth `15/15`, combat observation `9/9`, unit combat `3/3`, building defense `13/13`, ground missile `5/5`, air missile, and narrative regression validations pass with zero compiler errors. Evidence: `Design/AgentReports/2026-07-11_aph-706_combat_observation_assembly_split.md`.
- [x] `APH-707` Decompose `TransportBoardingCommandSystem` by its existing planning/routing/application seams while preserving public behavior and scenario tests.
  - Result: the original 3,226-line unmanaged owner is now a 181-line shell plus nine cohesive partials between 241 and 416 lines. Ordered partial reconstruction exactly matches all 16,571 original implementation tokens. Transport `78/78`, selection `60/60`, scenario `15/15`, extraction `33/33`, DR-001 `2/2`, gateway PlayMode `1/1`, performance/allocation, source growth `15/15`, ECS/Burst `10/10`, and assembly boundary `31/31` validations pass with zero compiler errors. Evidence: `Design/AgentReports/2026-07-11_aph-707_transport_boarding_decomposition.md`.
- [x] `APH-708` Decompose `UiShellEcsGateway` into route, read-model, action, and settings adapters behind existing contracts; views remain passive.
  - Result: the 2,802-line gateway is now a 252-line registered facade over private route, action, settings, and bounded read-model adapters; every new file remains below 500 lines and no view references an ECS adapter. Shell content `11/11`, assistant command `6/6`, assistant cache/allocation `2/2`, resource header `8/8`, resource routing `5/5`, lifecycle PlayMode `1/1`, source growth `15/15`, boundary `31/31`, broad-shell `1/1`, ECS/Burst `10/10`, and three project builds pass with zero compiler errors. Evidence: `Design/AgentReports/2026-07-12_aph-708_ui_shell_gateway_decomposition.md`.
- [x] `APH-709` Decompose selection/HUD files only where profiler or change-frequency evidence identifies risk; do not split for line count alone.
  - Result: only the profiler-backed tactical-follow camera query seam was extracted. A 47-line world-bound cache replaces two per-tick query constructions totaling 23,920 bytes in APH-007/213; a 300-tick warmed test allocates zero bytes, world-rebind behavior is covered, and both owner rows are absent from the fresh 180-warmup/300-frame raw capture. Camera `31/31`, selection `60/60`, selection/move/attack PlayMode `1/1`, source growth `15/15`, boundary `31/31`, ECS/Burst `10/10`, and Match p95 `4.31 ms` pass. Evidence: `Design/AgentReports/2026-07-12_aph-709_selection_camera_query_decomposition.md`.
- [x] `APH-710` Remove classified hidden world lookups and recurring per-frame allocation owners as each domain receives explicit World/EntityManager/query dependencies or cached non-allocating state.
  - Result: world/entity caches, idle-command gating, disabled-diagnostics call-site guards, and composition-bound query ownership removed both tactical-camera rows, the audio gameplay-state row, two scene-root rows, road/building command-query rows, both transport helper-construction rows, the path scheduling/result trace rows, the building source-key rows, the Match Intro and pathfinding pending-state hidden lookups, runtime-city readiness's hidden lookup/transient config queries, the runtime-city road-cell fallback's hidden lookup/transient grid query, selection building interaction and selection input state's hidden world access, and the gameplay loading gate's recurring readiness/diagnostic world lookups. The Resource Exchange popup's independent per-frame `Update` and full managed read/apply path are removed: one unmanaged projection fingerprint updates only when visible state, countdown second, or percentage changes; the existing shell presentation loop performs a version-only check before full conversion. The 701-line read-model owner is reduced to 580 lines through a bounded text-projection helper. Selection input state, UI commands, rectangle state, and focused-unit reads retain the composed camera-system world and fail safely when it is unavailable or disposed. Warmed unchanged hidden, focused, and multi-selection panel updates allocate zero managed bytes; suppressing only the nested panel profiler marker re-parents the same exact `256` bytes/two samples per refresh to the outer selection marker while the runtime probe remains zero, proving Editor profiler bookkeeping rather than production GC. Resource Exchange projection/allocation passes `5/5`, its architecture guard passes `7/7`, camera `31/31`, audio `8/8`, scene reference `5/5`, road command `11/11`, building production `26/26`, placement `16/16`, transport `79/79`, pathfinding `3/3`, pathfinding pending state `5/5`, Match Intro `4/4`, runtime-city readiness/road fallback `8/8`, runtime-city generation `2/2`, initial-spawn cells `2/2`, selection input `64/64`, selection summary `18/18`, selection/move/attack PlayMode `1/1`, Menu-to-Match-to-Menu PlayMode `1/1`, Match smoke, broad naming `1/1`, non-ECS naming `9/9`, boundary `31/31`, ECS/Burst `10/10`, and project builds pass where owned integration gates are clean. Evidence: `Design/AgentReports/2026-07-12_aph-710_audio_gameplay_state_query_cache.md`, `Design/AgentReports/2026-07-12_aph-710_match_scene_root_lookup.md`, `Design/AgentReports/2026-07-12_aph-710_road_command_entity_cache.md`, `Design/AgentReports/2026-07-12_aph-710_building_production_command_entity_cache.md`, `Design/AgentReports/2026-07-12_aph-710_building_placement_command_entity_cache.md`, `Design/AgentReports/2026-07-12_aph-710_transport_idle_helper_allocation.md`, `Design/AgentReports/2026-07-12_aph-710_path_diagnostics_allocation.md`, `Design/AgentReports/2026-07-12_aph-710_match_intro_world_binding.md`, `Design/AgentReports/2026-07-12_aph-710_pathfinding_pending_state_binding.md`, `Design/AgentReports/2026-07-12_aph-710_runtime_city_readiness_binding.md`, `Design/AgentReports/2026-07-12_aph-710_selection_building_world_binding.md`, `Design/AgentReports/2026-07-12_aph-710_gameplay_runtime_world_binding.md`, and `Design/AgentReports/2026-07-13_aph-710_selection_input_world_binding.md`.
  - Runtime-state result: the former static default-world/state-entity cache is now an unmanaged, explicitly bound `WorldUnmanaged` plus entity handle copied through Match, selection, building, road, and city contexts. Explicit unbinding at every retained-copy disposal boundary prevents disposed-world access during Match teardown. Runtime state `10/10`, selection `64/64`, road `11/11`, building composition smoke, runtime-city readiness `8/8`, Match smoke, boundary `31/31`, ECS/Burst `10/10`, and three project builds pass. Evidence: `Design/AgentReports/2026-07-12_aph-710_runtime_gameplay_state_binding.md` and `Design/AgentReports/2026-07-13_aph-710_selection_input_world_binding.md`.
  - Source-growth reconciliation: tactical-materials authored config, custom-game projection, and four-line movement integration are frozen by exact line/byte decisions. Faction startup projection leaves `InitialUnitsSpawnSystem` at `1,857 / 1,888` lines; vehicle route clearance leaves map placement at `575 / 576` and building hauling at `1,553 / 1,634`; Commander route lifecycle leaves shell content at `951 / 955`. Initial spawn `12/12`, tactical materials `7/7`, map placement `5/5`, building resources `38/38`, shell content `14/14`, and non-ECS architecture `9/9` pass with zero compiler errors. The source-growth gate proceeds to only the separately owned audio presentation violation.
  - Phase 7 inventory reconciliation: deterministic regeneration covers 197 production ECS declarations, including all 20 previously missing non-UI `ISystem` declarations and 15 newly discovered UI declarations. Counts are `173 ISystem`, `24 SystemBase`, `151 Converted`, `24` managed exceptions, and `22` UI out-of-scope, with an `87.8%` production `ISystem` share. `SystemBase` count and exception count do not increase. The Resource Exchange popup loop is removed and all 24 Scenario Lab coroutines are explicitly registered as non-shipping lab routines. The full gate advances through those 25 findings and now reports only the separately owned audio presentation `Update`.
- [x] `APH-711` Re-run compile time, domain reload time, architecture gates, focused tests, Match performance, and the unchanged steady-state GC capture after every physical asmdef split and final allocation-remediation slice.
  - Result: the final allocation-remediation integration rerun passes. Unity records `906 ms` script compilation and `684 ms` scripting domain reload. The clean current-`origin/main` integration compiles Runtime, Editor, and Editor-test assemblies with zero errors. Focused allocation coverage passes `44/44`, the final allocation review passes `12/12`, and the capture-classification/retry suite passes `41/41`; AIBuild selection no longer copies buffers or schedules a one-item TempJob, and building registration creates one live context graph per invocation instead of recursively rebuilding delegate/context chains. The unchanged steady-state GC gate passes at `930 / 1,024` player-relevant bytes over 300 measured frames after 180 warmup frames, with every production runtime probe at zero. Measurement windows containing building, production-transport, or drop-visual creation are discarded and retried after a fresh warmup. The latest canonical Match baseline passes over 766 frames at `5.23 ms` average, `8.16 ms` p95, `10.33 ms` p99, `25.79 ms` max, and zero allocation with 733 units and 628 buildings. Source growth passes `13/15` checks; its residual Campaign UI/audio growth and the Phase 7 audio runtime loop are separately owned architecture gates rather than allocation-remediation regressions. Implementation commits: `c47c73f62` and `86de3e054` on `codex/aph711-allocation-closeout`.
  - Evidence: `/private/tmp/warline-aph711-building-ai-focused-r16.xml`, `/private/tmp/warline-aph711-allocation-review-r22.xml`, `/private/tmp/warline-aph711-performance-r23.log`, `/private/tmp/warline-aph711-gc-classification-focused-r31.log`, `/private/tmp/warline-aph711-gc-steady-state-r32.log`, `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`, and `Design/AgentReports/performance_regression_match_baseline.md`.
- [x] `APH-712` Align the FirstLaunch narrative feature with the approved managed-edge naming, SOLID ownership, UI assembly, and shell ECS contracts in `Design/Architecture/first_launch_architecture_alignment_refactor_tracker.md`.
  - Result: GUID-preserving naming repair, narrow profile/shell/reviewer/panel/interactive presentation boundaries, dependency-free narrative contracts, pure deterministic sequence/route runtime, authored content policy, asynchronous bounded panel residency, and passive views are complete. FirstLaunch architecture `10/10`, integrated behavior `56/56`, broad naming `1/1`, non-ECS naming `9/9`, assembly boundary `31/31`, PlayMode `1/1`, and the dedicated 1,800-tick performance/residency validator pass; stable playback allocates zero managed bytes. Exact source-growth exceptions retain current line/byte ceilings and do not authorize growth or recreation.

## Phase 8 - CI, PlayMode, Visual, and Device Gates

Goal: prevent recurrence and cover behavior that source-scanning tests cannot prove.

- [x] `APH-800` Add both architecture execute-method validations to CI and fail the build on nonzero exit or missing pass marker.
  - Result: the unconditional Jenkins architecture stage runs both required execute methods sequentially through a fail-closed PowerShell wrapper, rejects nonzero Unity exit/missing log/missing exact marker, and archives both logs.
  - Validation: source-contract checks cover both methods/markers, stage ordering, escaped paths, timeout, log archival, and the prohibition on `-nographics`; PowerShell runtime execution remains owned by the Windows Jenkins agent.
- [x] `APH-801` Add the editor Match performance baseline and steady-state GC capture to a scheduled or pre-merge performance lane; fail when the unchanged 1,024-byte GC budget is exceeded.
  - Result: Jenkins runs the existing Match baseline and steady-state GC execute methods weekly or when `RUN_EDITOR_MATCH_PERFORMANCE=true`, reuses the fail-closed APH-800 wrapper, rejects missing/stale/malformed evidence, preserves the `1,024`-byte budget, and archives logs plus machine-readable summary evidence.
  - Validation: focused and combined CI Python tests pass `24/24`; source contract and syntax checks pass. Windows Jenkins execution remains the first live scheduled-lane proof.
- [x] `APH-802` Ratchet the editor p95 budget from the current lenient `50 ms` only after at least five stable captures establish variance.
  - Result: five accepted fresh-process captures at exact commit `d2a41ac97879f01f4285fdb92831dbe34276d42c` use the same Unity/OS/machine/GPU/quality/cache policy, 733-unit/628-building fixture, four-second runner, and zero-allocation gate. P95 values span `9.087-12.905 ms`, mean `10.6316 ms`, median `10.231 ms`, sample deviation `1.518106 ms`, and CV `14.2792%`; no outlier is declared.
  - Ratchet: accepted-baseline version 4 lowers the Editor p95 budget from `50 ms` to `20 ms`, retaining approximately 55% headroom above the worst accepted run. Append-only run pairs and summary are under `Design/AgentReports/aph802/2026-07-11_d2a41ac97/`; preservation/series contracts pass and reject mixed, stale, overwritten, or incomplete evidence. The post-ratchet canonical Unity gate passed at `4.495 ms` p95 with zero current-thread allocation and the full 733/628 fixture.
- [~] `APH-803` Add an Android development-build gate for p95/p99 frame, peak memory, startup time, and thermal status on the reference device profile.
  - Active result: the fail-closed non-Unity evidence layer enforces the reference device/APK/revision/build profile, exactly five cold and five warm starts, a 60-second warmup, 600-second sustained run with at least 9,000 structured samples, recomputed percentiles, peak memory, zero thermal/cooling status, hashed recorder/log/screenshot evidence, PNG dimensions, process survival, and raw crash scanning. A plain flag-gated recorder is now delegated from the existing diagnostics frame owner, preallocates only when requested, records the exact Match-ready transition plus per-frame/CPU/GPU/memory evidence, and writes once after capture. The gate rejects incomplete or timingless recorder output and requires its nested sustained result to exactly match submitted evidence. Jenkins runs an unconditional offline profile/schema/unit/contract preflight before Unity resolution, archives its machine-readable contract and log, and invokes neither Unity nor ADB. A tracked development-device collector now verifies a clean exact revision and pinned debuggable ARM64 APK before ADB access, performs the required five cold/five warm starts, enforces stale-recorder, PID, foreground, thermal, duration, crash, hash, and screenshot contracts, and then invokes the existing fail-closed gate. The gate intentionally reports `acceptanceReady=false` while p99 and startup limits remain unset; the collector passes `10/10` focused tests and the combined APH-803/APH-804 Python matrix passes `41/41`.
  - Device diagnostic: one dirty development-build run on the Xiaomi reference device completed 60.02 seconds of warmup plus 600.01 seconds of capture with 27,788 frame samples. Average/p95/p99/max were `21.59/32.83/41.61/65.61 ms`, CPU/GPU p95 were `31.83/25.59 ms`, Match-ready startup was `19,005.75 ms`, and peak Unity allocation was `1,084.15 MB`. Process survival and final visual integrity passed, but the run is rejected for acceptance because skin thermal status reached `3`, memory exceeded the provisional `967.5 MB` limit, provenance was dirty, and the required five cold/five warm starts were not collected.
  - Current device preflight: the operation-map owner refreshed the canonical static presentation manifest in `f850f3a5bdc6b3f4ffa8ad7ab453c07611fb5e8a`, and the clean exact-revision profiler APK now builds successfully. The artifact is `478,122,683` bytes with SHA-256 `dcd05f8a602333bb9f48812c1caa4e5d4a590c5304dd8d85eb3adf824834a69a`; device installation, cold launch, FirstLaunch-to-Menu routing, Campaign routing, Match loading, 60-second process survival, thermal status `0`, and bounded crash/ANR scanning pass. Sequentially decoded screen-recording frames are visually complete; intermittent black rectangles in direct ADB still captures/non-keyframe extraction are capture artifacts rather than reproduced live presentation loss. Formal acceptance remains open: the pinned device disconnected before the required five-cold/five-warm plus 60-second warmup and 600-second sustained collector could start, and p99/startup limits remain measurement-required.
- Evidence: `Design/AgentReports/2026-07-11_aph-803_android_development_gate_plan.md`, `Design/AgentReports/2026-07-12_aph-803_android_runtime_recorder.md`, `Tools/CI/android_development_performance_gate.py`, `Tools/CI/android_development_device_collection.py`, `/private/tmp/warline-aph803-profiler-apk-f850f3a5b-r3.log`, `/private/tmp/aph803-device-user-entered-match.png`, `/private/tmp/aph803-match-check.mp4`, and `/private/tmp/aph803-video-frame-4-decoded.png`. Focused recorder `5/5`, diagnostics allocation `4/4`, assembly boundary `31/31`, Python gate `18/18`, development collector `10/10`, combined collector/gate regression `41/41`, and three project builds pass. Remaining work is accepted p99/startup limits and live clean-reference-device proof.
- [~] `APH-804` Add Android release-build acceptance for the documented 30 FPS tier; keep 60 FPS as a separate high-end target.
  - Active result: a shared fail-closed Python evidence core preserves APH-803 compatibility and adds a release-specific profile, schema, wrapper, and tests. One generalized runtime recorder now provides explicit APH-803 development and APH-804 release modes from the existing diagnostics frame owner. APH-804 applies an unsaved Mobile/60 settings override, writes the exact release evidence shape, samples Android PSS and battery outside the frame-hot path, aggregates GC/render/CPU/GPU counters without per-frame managed allocation, and fails closed on dirty provenance or missing measurements. The Android build-report boundary rejects development/debug/profiler options, captures immutable Git provenance before build-target/settings mutation, and passes that snapshot into report generation; post-build workspace changes remain a separate audit. A post-build Jenkins stage binds the actual clean revision, APK hash/size, canonical report, and release identity before deployment. One serialized ADB runner now preflights that artifact before device access, verifies the pinned non-debuggable ARM64 installation, collects exactly five cold and five warm starts, enforces unplugged/idle-thermal/PID/foreground continuity, waits for the 60+600-second recorder, preserves its bytes, and validates hashed recorder/log/screenshot/thermal evidence through the existing gate. The gate blocks on `<33 ms` p95, APK `<=463,359,198` bytes, clean release provenance, duration/sample counts, process survival, and zero thermal/cooling activity; `<25 ms` remains a separate non-blocking observation.
  - Readiness: contract generation intentionally reports `acceptanceReady=false` until validated release-device evidence exists. Combined CI/gate/device Python tests pass `62/62`; focused Unity recorder/settings/evidence tests pass `16/16`, Android BuildReport focused coverage passes `12/12`, diagnostics allocation passes `5/5`, naming/non-ECS architecture passes `9/9`, and assembly boundary passes `31/31`. The current dirty/stale local APK is `89,122,066` bytes over the ceiling and is rejected before ADB access. Comparison rules out unchanged UI, 4K world textures, animation textures, audio, and package manifests as demonstrated causes; dirty combined static-map output is the leading but not yet quantified candidate. P99, startup, installed size, and absolute memory remain recorded, non-blocking, measurement-required limits. Evidence: `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md` and `/private/tmp/warline-aph804-prebuild-provenance-focused.log`.
  - Current candidate: clean commit `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` produces a provenance-bound ARM64 IL2CPP release APK and complete 6,434-asset BuildReport. The `449,902,496`-byte artifact passes the accepted `463,359,198`-byte ceiling with `13,456,702` bytes of headroom and retains local offline First Launch plus unchanged operation-map content. Device preflight exposed that release collection unconditionally uninstalled even when no package was installed; it now mirrors the development collector's exact package query and uninstalls only one exact matching installed package, retaining fail-closed ambiguous/failure handling. Installation now succeeds on the Xiaomi reference device. Its shell can measure the installed `base.apk` and extracted `lib` directory but cannot traverse the protected `oat` directory, so the collector records their permission-safe sum (`621,388,272` bytes on this install) instead of failing or silently discarding stderr; command failure details now preserve bounded stdout and stderr. Release/development collector regressions pass `28/28`. The installed-size value is an accessible-artifact measurement and does not authorize an absolute full-footprint budget while protected generated code remains unavailable. APH-804 remains active until the serialized collector completes five cold/five warm starts, 60-second warmup, 600-second sustained release capture, memory/installed-size measurements, process/crash integrity, and zero-thermal acceptance on this exact artifact.
- [x] `APH-805` Add a Menu -> Match -> Menu lifecycle PlayMode test covering serialized references, world creation, UI binding, and cleanup.
  - Result: the focused production-route PlayMode flow proves serialized references, persistent-world and single lifecycle-root ownership, HUD dependency binding, Match unload, UI removal, settings unsubscription, and helper cleanup. Listener authority now remains with Match until additive unload completes, and a project-owned teardown fence flushes pending GPU-animation structural changes before converted render entities are destroyed.
  - Validation: listener scene-binding tests pass `6/6`; teardown-fence tests pass `3/3`; the final lifecycle run passes `1/1` in `13.45 s` with zero duplicate-listener warnings and zero `MaterialAlphaSetup`/invalid-entity/ECB teardown exceptions.
- [x] `APH-806` Add deterministic PlayMode flows for selection/move/attack and building placement/production.
  - Validation: two production-gateway PlayMode flows pass `2/2`, covering committed rectangle selection, preserved focus, immediate move result/target, accepted attack result/engage target, valid placement commit, and exactly-once production queueing.
- [x] `APH-807` Add deterministic PlayMode flows for boarding/disembark and resource exchange.
  - Validation: two production-gateway PlayMode flows pass `2/2`, covering TB-001 boarding/disembark state transitions and export request reservation, settlement, credit award, and duplicate-settlement protection.
- [x] `APH-808` Add a scene/prefab missing-reference gate for the two enabled build scenes and runtime UI content prefabs.
  - Result: a deterministic Unity 6-compatible validator scans both enabled build scenes and all six direct runtime UI content prefabs, including inactive objects, missing scripts, and broken object references. A precise asset repair restored the authoritative status-chip sprite, cleared 17 obsolete broken Menu route-prefab entries, and cleared two inactive legacy Menu sprites without unrelated prefab layout churn.
  - Validation: focused validator tests pass `11/11`; Editor and Editor-test assemblies compile with zero errors; the live gate improved from 25 findings to `[APH-808 MissingSerializedReferenceValidation] result=Passed buildScenes=2 runtimeUiContentPrefabs=6`.
- [ ] `APH-809` Add required visual captures for graphics tiers, Day/Night, static map chunks, and mip streaming. A visual task cannot pass from logs alone.
  - Readiness result: a fail-closed visual matrix enumerates 26 required comparison slots and 32 exact PNG artifacts, validating PNG integrity, dimensions, hashes, clean provenance, uniqueness, and before/after pairing. All 26 slots currently remain unsatisfied, so no visual task is accepted from logs alone. Validation passes `11/11`; evidence: `Design/AgentReports/2026-07-13_aph-809_visual_capture_matrix.md`.
  - Current diagnostic: a 16:9 current-profile session at revision `829730c1aec16a663cc3b65eac31b7e4239e478f` emitted all 13 expected PNGs plus session metadata and the camera contract. It is rejected because the 23:00 battlefield is too dark and the near static-map frame contains black/missing surface regions. The untracked evidence is preserved at `/private/tmp/warline-aph809-current-16x9-staging`. Since `origin/main` advanced to `7f51f8e3ddbd378ece9f10183e334fc116172db6`, all four acceptance sessions must be recaptured from one exact clean revision after the visual and manifest blockers are resolved.
- [x] `APH-810` When Unity MCP is connected, require agents to inspect affected scene/prefab hierarchy, console, Play Mode state, and screenshots through MCP. When unavailable, record the fallback runner and screenshots used.
  - Result: fail-closed JSON schema, validator, and report template require exactly one evidence path. MCP evidence must name hierarchy, console, Play Mode, and screenshot tools; fallback evidence must record the failed probe/reason, exact runner command, log path, pass marker, and screenshots, with optional on-disk artifact/marker verification.
  - Validation: focused contract tests pass `12/12`; Python compilation, JSON schema parsing, CLI smoke, and whitespace checks pass.
- [x] `APH-811` Add a tracked performance dashboard generated from JSON artifacts rather than manually copied metrics.
  - Result: deterministic Python tooling derives a JSON/Markdown dashboard from seven tracked architecture, build, content-residency, runtime-performance, and audio JSON artifacts. Missing, invalid, stale, current, and revision-unknown inputs remain explicit rather than silently accepted.
  - Validation: dedicated dashboard tests pass `6/6`, Python/JSON checks pass, every configured input is tracked, and consecutive generation hashes are byte-identical.

## Phase 9 - Final Closeout

Goal: prove the complete program improved production readiness without gameplay or visual regression.

- [x] `APH-900` Run the complete first-party build matrix with 0 errors.
  - Accepted on rebased revision `ebdba0b4bb4c59963ee07ae84a96b2071eab9f8f`: Components, Configs, Runtime.Pathfinding, Runtime, Rendering, UI Shell Contracts ECS, UI Runtime, UI Shell ECS, Composition, Editor, Editor tests, and PlayMode tests all build with `0 Error(s)`.
- [x] `APH-901` Run architecture, ECS/Burst, focused EditMode, critical PlayMode, Match smoke, performance, GC, audio, graphics, and static-map validation gates.
  - Local candidate result: integrated architecture closeout `23/23`, source growth `15/15`, focused move-order `18/18`, focused lifecycle/allocation/resource fixtures, five critical PlayMode flows, Menu-to-Match performance/GC routes, full static-map structural parity `2/2`, all 12 first-party builds, and `218/218` CI Python contracts pass with zero compiler errors.
  - Performance: 733 units, 628 buildings, 557 measured frames, `7.20 ms` average, `11.48 ms` p95, `13.16 ms` p99, `34.98 ms` maximum, and zero measured allocation.
  - GC: 300 measured frames after 180 warmup frames pass at `292 / 1,024` player-relevant bytes. The recurring production-cache, storage-query, and hauler move-order query rows are removed.
  - Integration state: accepted after the task-owned feature branch reached `origin/main` by strict fast-forward at `8caf7b00c`.
  - Evidence: `/private/tmp/warline-aph901-closeout-after-gc.log`, `/private/tmp/warline-aph901-performance-after-query-cache.log`, `/private/tmp/warline-aph901-gc-steady-after-query-cache-r2.log`, `/private/tmp/warline-aph901-static-map-structural-final.xml`, `/private/tmp/warline-aph8*-final.xml`, `Design/AgentReports/performance_regression_match_baseline.md`, and `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`.
- [ ] `APH-902` Produce final same-device Android development and release reports, including 10-minute thermal sessions.
- [ ] `APH-903` Compare final architecture/performance metrics against this baseline and write `Design/AgentReports/architecture_performance_hardening_final_report.md`.
- [ ] `APH-904` Re-rate architecture and performance, list residual risks, update the root architecture index, and mark this tracker complete only when no required task remains.

## Stable Integration and Pull Request Gate

Select the path declared in the task handoff:

- **Future pull-request task:** run this gate on the final PR head and the actual combined result with latest `origin/main` before the coordinator merges. Record the latest-main SHA, PR-head SHA, combined integration commit/tree SHAs, clean validation workspace status, and exact evidence.
- **Grandfathered hardening slice:** run the same substantive diff, allowlist, compiler, architecture, behavior, GC/performance, Unity/device, Jenkins, and unrelated-work checks in the persistent integration checkout. Fetch latest `origin/main`; inspect overlap; integrate it before final acceptance; rerun every affected gate on the combined result; confirm the candidate commit and worktree are clean; then push the stable coordinator-owned commit directly to `main`. PR target, PR template, PR merge, and branch-cleanup items are `Not applicable - grandfathered direct-main`, not silently omitted.

1. For a PR task, confirm it targets `main` from the assigned `codex/<task-id>-<slug>` branch. For either path, inspect `git status --short`, the complete candidate diff, the allowlist, and all unrelated-work exclusions.
2. Fetch current `origin/main` and validate the actual combined result. For PR work, use an integration method authorized by the PR workflow and return content-changing conflicts to the implementation agent. For a grandfathered slice, the coordinator integrates latest main in its persistent checkout. Rerun every gate affected by the integration.
3. Run `git diff --check` against the exact combined result and confirm no unrelated file entered the candidate.
4. Build the owning assembly and every directly affected first-party dependent assembly with `0` errors. For shared runtime or composition changes, include `Game.Runtime`, `Game.Editor`, and the relevant test assembly at minimum.
5. Run every focused test and architecture gate required by the task. Unity-required slices must reach their execute method and required pass marker; handle licensing through the documented wrapper/escalation retry first.
6. Inspect the relevant Unity log or live Editor console for `error CS`, compiler failure, unhandled exception, test failure, or asset import failure attributable to the slice. Categorize existing unrelated warnings in the handoff.
7. Verify all required GC/performance, visual, device, package, and Jenkins evidence is tied to the final tested revision and satisfies the unchanged accepted contracts. Do not add GitHub Actions as a substitute.
8. For a PR task, return every substantive finding to the implementation agent; after resolution, the coordinator may add only administrative tracker/evidence reconciliation. For a grandfathered slice, preserve the existing coordinator-owned worker review and integration boundary. Rerun checks affected by every accepted content or administrative commit.
9. Confirm the remote candidate matches the reviewed/tested revision, risks and untested paths are explicit, and no required gate was weakened. For PR work, confirm the template, merge, PR URL, feature head, merge commit, and cleanup. For grandfathered work, record the direct-main commit/push and the explicit PR-only `Not applicable` fields.

If any required check fails, do not merge or push the candidate. Keep the task pending/in progress, or propose `[!]` only with the exact blocker and next unblocking action. For PR tasks, substantive fixes belong to the implementation agent and the coordinator does not repair product code under the administrative-commit exception. The grandfathered coordinator retains only the broader integration ownership already assigned before activation.

## Common Validation Matrix

Run only the focused rows relevant to a slice during development. Run every row in Phase 9.

### Build Commands

```bash
dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Configs.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Runtime.Pathfinding.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Rendering.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.UI.Contracts.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.UI.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.UI.Shell.Ecs.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Composition.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Tests.PlayMode.csproj --no-restore -v:q -clp:ErrorsOnly
git diff --check
```

### Unity Command Template

Replace `<Method>` and `<LogName>` only. The coordinator must grant the exclusive Unity lease before the command starts. Use the project wrapper in windowed batchmode and do not add `-nographics`. Use `-quit` only for execute methods that complete synchronously; asynchronous Play Mode capture methods must own their own exit lifecycle.

```bash
Tools/CI/invoke_unity_macos.sh \
  --project /Users/farhad/Projects/WarlineCapture \
  --timeout 900 \
  --log /private/tmp/<LogName>.log \
  -- -quit -executeMethod <Method>
```

If the sandboxed run fails during Unity licensing initialization, rerun the same wrapper command once with escalation/out-of-sandbox access. Record both log paths. A first licensing loop is not a final blocker.

### Required Execute Methods

| Scope | Method | Required marker |
|---|---|---|
| Assembly boundaries | `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` | `[ScriptArchitectureBoundaryValidation] result=Passed tests=31` |
| ECS hot paths | `EcsBurstHotPathArchitectureTests.RunFocusedValidation` | `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10` |
| Resource exchange | `ResourceExchangeRequestValidationSystemTests.RunFocusedValidation` | `[ResourceExchangeRequestValidation] result=Passed` |
| Resource exchange GC | `ResourceExchangeGcAllocationValidationTests.RunFocusedValidation` | `[ResourceExchangeGcAllocationValidation] result=Passed` |
| Building defense | `BuildingDefenseAttackSystemTests.RunFocusedValidation` | `[BuildingDefenseAttackSystemValidation] result=Passed` |
| Settings | `SettingsPopupValidationTests.RunFocusedValidation` | `[SettingsPopupValidation] result=Passed` |
| Android graphics | `AndroidVisualQualityValidationTests.RunFocusedValidation` | `[AndroidVisualQualityValidation] result=Passed` |
| Audio contracts | `AudioConfigContractTests.RunFocusedValidation` | `[AudioConfigContractValidation] result=Passed tests=13` |
| Audio performance | `AudioPerformanceValidationTests.RunFocusedValidation` | `[AudioPerformanceValidation] result=Passed tests=4` |
| Match smoke | `Game.Editor.MatchRuntimeShellSmokeValidation.Run` | `[MatchRuntimeShellSmokeValidation] result=Passed` |
| Match performance | `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline` | accepted baseline pass marker |
| Match steady GC | `Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState` | `[MatchGcAllocationCallstackCapture] result=Passed frames=300` |
| Match battle GC | `Game.Editor.MatchGcAllocationCallstackCapture.RunBattleState` | capture pass marker |

### Android Profiler Build and Launch

```bash
Tools/CI/invoke_unity_macos.sh \
  --project /Users/farhad/Projects/WarlineCapture \
  --timeout 3600 \
  --log /private/tmp/warline-architecture-performance-profiler-build.log \
  -- -quit -executeMethod Game.Editor.BuildScript.BuildAndroidProfilerApk
adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk
adb shell am force-stop com.warlinecapture.game
adb shell "am start -n com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity --es unity '-warlineAutoStartMatch -warlineProfilerMarkers'"
```

Android acceptance evidence must include:

- exact commit and APK hash
- device model, SoC, OS, resolution, refresh rate, quality tier, and frame-rate mode
- warmup length and sample duration
- average, p95, p99, and maximum frame time
- CPU and GPU timing
- peak allocated and Mono memory
- draw calls, SetPass, triangles, and vertices
- battery and thermal state before/after
- screenshots from the tested build
- crash/fatal log scan

## Visual Acceptance Matrix

| Change type | Required views |
|---|---|
| Graphics/DayNight | day, dusk, night; normal gameplay zoom and high tactical zoom |
| Texture streaming | near, medium, far; static and after rapid camera pan |
| Static map chunks | top-down full city, oblique city, low-ground interiors/roads, both faction bases, representative vegetation/terrain |
| UI text/settings | 16:9 and 20:9 settings plus affected Match HUD states |
| Audio | Menu, Match idle, combat burst, ARIA voice, music transition; headphones/device speaker note |

Reject visual work for any of the following:

- missing building shells or detached interiors
- floating or buried props
- road seams, rotation errors, or changed alignment
- missing grass, bushes, ground details, or terrain material changes
- blurry nearby textures or visible streaming pop that persists
- lighting/fog reset after a quality setting change
- text clipping, overlap, or wrong fallback text

## Implementation Log Template

Append one entry per completed or blocked task. Keep entries concise but include objective evidence.

```markdown
### YYYY-MM-DD - APH-### - Short title

- Status: Complete / Blocked
- Commit or worktree baseline: `<hash>`
- Workflow path: `pull request` or `grandfathered direct-main`
- PR/branch: `<PR URL>` from `<branch>`, or grandfathered path with reason
- Stable revision/merge: feature head `<hash>` merged as `<hash>`, or `not merged` with reason
- Files changed: `path`, `path`
- Behavior preserved/changed: exact statement
- Validation: command plus required pass marker; include Stable Integration and Pull Request Gate result
- Artifacts: log/report/screenshot paths
- Metrics before: values or `not applicable`
- Metrics after: values or `not applicable`
- Visual result: Passed / Failed / Not applicable; inspection method
- Residual risk: exact remaining risk
- Next ready task: `APH-###`
```

## Implementation Log

### 2026-07-09 - APH-000 through APH-005 - Audit baseline and tracker creation

- Status: Complete
- Commit or worktree baseline: `b66453e979d847c3f05d61155266b166650b8df5`
- Files changed: this tracker only
- Behavior preserved/changed: no runtime, scene, prefab, config, or asset behavior changed
- Validation: `Game.Runtime.csproj` built with 0 errors; Unity imported and compiled scripts
- Artifacts: `/private/tmp/warline-architecture-audit-validation.log`, `/private/tmp/warline-performance-architecture-audit-validation.log`
- Metrics before: recorded in Current Evidence Snapshot
- Metrics after: not applicable
- Visual result: not applicable
- Residual risk: both current architecture gates remain red by design until Phases 1 and 2
- Next ready task: `APH-006`

### 2026-07-09 - Program baseline reconciliation and multi-agent execution model

- Status: Complete tracker administration; no implementation task marked complete
- Commit or worktree baseline: `a6af1335cb7270d8b87c60d9646ed78be7e97ac5`
- Files changed: this tracker only
- Behavior preserved/changed: no runtime, scene, prefab, config, or asset behavior changed
- Validation: current commit and clean pre-edit worktree confirmed; current building-defense snapshot sites inspected; `git diff --check` passed after the documentation update
- Artifacts: current tracker and commit `a6af1335c` benchmark evidence
- Metrics before: original audit baseline remains recorded above
- Metrics after: current FPS repair evidence recorded under Baseline Reconciliation; not treated as Phase 2 completion
- Visual result: Not applicable
- Residual risk: current-head full build matrix, architecture gates, Match baseline, GC baseline, and content-residency report remain pending
- Next ready tasks: `APH-006`, `APH-007`, and `APH-008` may be assigned in parallel under the coordination rules

### 2026-07-09 - APH-006 - Sequential first-party build matrix

- Status: Complete
- Commit or worktree baseline: `7084805d771142706f340e9f2e52a68570bcb72b`
- Stable commit/push: `ed9901e5d` included in the coordinated `main` push
- Files changed: this tracker only; the build agent changed no files
- Behavior preserved/changed: no runtime, scene, prefab, config, asset, or test behavior changed
- Validation: all 12 Common Validation Matrix assemblies built sequentially with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph006-final-matrix-summary.log`, `/private/tmp/warline-aph006-warning-categories.log`, and 12 `warline-aph006-final-*` assembly logs
- Metrics before: not applicable
- Metrics after: 12/12 builds passed; warnings were generated-project `MSB3277` reference-version conflicts only
- Visual result: Not applicable
- Residual risk: Unity activity can rewrite `Temp/Bin`; future build matrices remain serialized against Unity import/build operations
- Next ready task: `APH-009` after `APH-007` acceptance

### 2026-07-09 - APH-008 - Content-residency inventory baseline

- Status: Complete
- Commit or worktree baseline: `7084805d771142706f340e9f2e52a68570bcb72b`
- Stable commit/push: `929ab7bb3` pushed to `main`
- Files changed: `Assets/Game/Scripts/Editor/ContentResidencyInventoryGenerator.cs`, focused Editor tests, Unity `.meta` files, JSON inventory, and Markdown summary
- Behavior preserved/changed: added editor-only reporting; runtime, scene, prefab, config, asset-import, and gameplay behavior are unchanged
- Validation: generator passed for 4,099 assets; 4 focused tests passed; `Game.Editor` and `Game.Tests.Editor` built with 0 errors; JSON invariants and `git diff --check` passed
- Artifacts: `Design/AgentReports/architecture_performance_content_residency_baseline.json`, matching Markdown summary, `/private/tmp/warline-aph-008-content-residency-final.log`, and focused-test log
- Metrics before: six project animation textures with 100,663,296 payload bytes were known, without build-root scope
- Metrics after: 4,099 reachable assets, 815,664,568 source bytes, 2,471,396,094 measured imported bytes across 2,680 assets, 217 DecompressOnLoad audio clips, 9 Streaming clips, 637 textures with 0 streaming-enabled, 1,817 meshes with 901 read/write-enabled, and 3 reachable animation textures with 50,331,648 payload bytes
- Visual result: Not applicable; editor inventory only
- Residual risk: dependency reachability is not exact APK/AAB contribution or simultaneous runtime residency; `APH-500` and `APH-508` own those measurements
- Next ready task: `APH-009` after `APH-007` acceptance

### 2026-07-09 - APH-007 - Current-head Match performance and GC baseline

- Status: Complete baseline capture; steady-state GC gate recorded red
- Commit or worktree baseline: `7084805d771142706f340e9f2e52a68570bcb72b`, validated again after tracker-only commits through `8a52df3e9`
- Stable commit/push: `ba3da6704` pushed to `main`
- Files changed: Match GC capture runner, 16 focused attribution tests, performance baseline JSON/Markdown, canonical GC report, and preserved failed-attribution reports
- Behavior preserved/changed: editor diagnostic attribution now binds bytes and call stacks to the same raw profiler sample; runtime and gameplay behavior are unchanged
- Validation: `Game.Editor` and `Game.Tests.Editor` built with 0 errors; `[MatchGcAllocationAttributionValidation] result=Passed tests=16`; 180 warmup and 300 measured frames completed; `git diff --check` and focused code review passed
- Artifacts: `Design/AgentReports/performance_regression_match_baseline.json`, matching Markdown, `Design/AgentReports/2026-07-09_aph-007_match-gc-steady-state_raw-metadata-final_failed.md`, focused log, full capture log, and raw profile under `/private/tmp`
- Metrics before: previous tracked editor baseline p95 `7.186 ms`; earlier GC attribution was nondeterministically smeared across hierarchy rows
- Metrics after: 733 units, 628 buildings, average `3.42 ms`, p95 `5.22 ms`, p99 `6.87 ms`, maximum `18.13 ms`; 4,826 GC samples and `269,482 / 1,024` player-relevant bytes with raw metadata attribution
- Visual result: Not applicable; diagnostic tooling and structured evidence only
- Residual risk: the unchanged steady-state GC gate is red; exact allocation owners are mandatory Phase 7 remediation and Phase 8/9 acceptance work
- Next ready task: `APH-009`

### 2026-07-09 - APH-009 - Initial product-budget authority

- Status: Complete
- Commit or worktree baseline: `e848eb24a0`
- Stable commit/push: `1e20f1417` pushed to `main`
- Files changed: accepted-baseline JSON schema, performance regression contract, editor budget validator, 4 focused tests, and Unity `.meta` files
- Behavior preserved/changed: no runtime or visual behavior changed; initial product budgets and measurement-required states are now machine-validated
- Validation: `Game.Editor` and `Game.Tests.Editor` built with 0 errors; `[PerformanceProductBudgetValidation] result=Passed tests=4`; JSON invariants and `git diff --check` passed
- Artifacts: `Design/Architecture/performance_regression_accepted_baseline.json`, `/private/tmp/warline-aph009-product-budget-focused-escalated.log`, and build logs
- Metrics before: schema version 1 with editor-only thresholds
- Metrics after: schema version 2 with `<33 ms` baseline/recommended and `<25 ms` high-end Android p95 budgets, `269,482 / 1,024` GC baseline/budget, `1054-1075 MB` same-device memory baseline, and at least 10 percent relative memory-reduction target
- Visual result: Not applicable; config and editor validation only
- Residual risk: release APK/AAB, installed size, startup, runtime residency, and absolute memory limits remain measurement-required under `APH-500`, `APH-501`, `APH-803`, and `APH-809`
- Next ready tasks: `APH-100`, `APH-200`, and `APH-205`

### 2026-07-09 - APH-205 - Building-defense behavior regressions

- Status: Complete
- Commit or worktree baseline: `428da26cf`
- Stable commit/push: `437587eea` pushed to `main`
- Files changed: `Assets/Tests/Editor/BuildingDefenseAttackSystemTests.cs`
- Behavior preserved/changed: production behavior unchanged; focused coverage now locks neutral filtering, aircraft exclusion, nearest-hostile order, four-slot order, destroyed targets, and removed-target reacquisition
- Validation: Editor/test builds passed with 0 errors; `[BuildingDefenseAttackSystemValidation] result=Passed tests=6`; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph205-building-defense-focused.log`
- Metrics before: 1 focused defense case
- Metrics after: 6 focused defense cases
- Visual result: Not applicable; deterministic ECS EditMode behavior
- Residual risk: warmed 32-tower/740-candidate allocation evidence remains `APH-206`; production snapshot removal remains `APH-207`/`APH-208`
- Next ready task: `APH-206`

### 2026-07-09 - APH-100 and APH-101 - UI text resolver contract

- Status: Complete
- Commit or worktree baseline: `17a8e44d5`
- Stable commit/push: `a6f48a8c5` pushed to `main`
- Files changed: UI Contracts resolver interface/fallback, focused tests, and Unity `.meta` files
- Behavior preserved/changed: no existing consumer behavior changed; a non-Unity UI-facing contract and immutable fallback now exist
- Validation: UI Contracts, UI Runtime, and Editor-test builds passed with 0 errors; `[GameTextResolverValidation] result=Passed tests=5`; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph100-game-text-resolver.log`
- Metrics before: no UI-facing resolver contract
- Metrics after: 5 focused contract cases
- Visual result: Not applicable
- Residual risk: composition adapter and runtime injection remain `APH-102`/`APH-103`
- Next ready task: `APH-102`

### 2026-07-09 - APH-200 through APH-202 - Multi-entity resource exchange without snapshots

- Status: Complete
- Commit or worktree baseline: `17a8e44d5`
- Stable commit/push: `b0b5f7e08` pushed to `main`
- Files changed: resource-exchange validation system plus behavior and warmed allocation tests
- Behavior preserved/changed: both exchanges process deterministically in one update; `AllowAiExchange` summary propagation corrected; native entity snapshot removed
- Validation: Runtime and Editor-test builds passed with 0 errors; behavior 4/4 and GC 2/2 markers passed; 512 measured updates reported 0 production-update managed bytes; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph202-resource-request-validation.log` and `/private/tmp/warline-aph202-resource-gc-validation.log`
- Metrics before: one `Allocator.Temp` entity snapshot per update and missing secondary AI-exchange summary flag
- Metrics after: zero query snapshots, zero measured production-update managed bytes, and both summary versions reached 576
- Visual result: Not applicable
- Residual risk: source/architecture guard and complete Phase 2A validation remain `APH-203`/`APH-204`
- Next ready task: `APH-203`

### 2026-07-09 - APH-206 - Building-defense warmed allocation gate

- Status: Complete
- Commit or worktree baseline: `437587eea`
- Stable commit/push: `cd7646340` pushed to `main`
- Files changed: `Assets/Tests/Editor/BuildingDefenseAttackSystemTests.cs`
- Behavior preserved/changed: production unchanged; test now measures 32 towers and 740 candidates at forced acquisition cadence
- Validation: Editor/test builds passed with 0 errors; `[BuildingDefenseAttackSystemValidation] result=Passed tests=7`; 64 measured updates reported 0 managed update bytes; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph206-building-defense-allocation.log`
- Metrics before: no production-scale warmed allocation fixture
- Metrics after: 32 warmup and 64 measured updates, `productionUpdateAllocatedBytes=0`
- Visual result: Not applicable
- Residual risk: four native query snapshots and explicit dependency completion remain production debt
- Next ready task: `APH-207`

### 2026-07-09 - APH-102 - Composition text resolver adapter

- Status: Complete
- Commit or worktree baseline: `59dbb6148`
- Stable commit/push: `72b9ecf5f` pushed to `main`
- Files changed: composition-owned resolver adapter, focused tests, and Unity `.meta` files
- Behavior preserved/changed: configured lookup, missing-key fallback, and formatting behavior are unchanged; no runtime consumer is migrated in this slice
- Validation: `[GameTextResolverAdapterValidation] result=Passed tests=5`; UI Contracts, Composition, Runtime, Editor, and Editor-test builds passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph102-text-resolver-validation.log`
- Metrics before: UI contract existed without a config-backed composition implementation
- Metrics after: five adapter behavior cases and one stateless composition implementation
- Visual result: Not applicable
- Residual risk: Menu and Match injection remains `APH-103`; seven runtime consumers remain `APH-104`
- Next ready task: `APH-103`

### 2026-07-09 - APH-203 and APH-204 - Resource Exchange architecture closeout

- Status: Complete
- Commit or worktree baseline: `59dbb6148`
- Stable commit/push: `5dd7f26d4` pushed to `main`
- Files changed: Resource Exchange architecture guardrail tests
- Behavior preserved/changed: production behavior unchanged; source guard now prevents snapshots, structural completion, singleton assumptions, temporary allocation, and common managed allocation in request validation `OnUpdate`
- Validation: architecture 6/6, request behavior 4/4, and warmed GC 2/2 passed; 64 warmup and 512 measured frames reported `productionUpdateAllocatedBytes=0`
- Artifacts: `/private/tmp/warline-aph203-resource-architecture-validation.log`, `/private/tmp/warline-aph204-resource-request-validation.log`, and `/private/tmp/warline-aph204-resource-gc-validation.log`
- Metrics before: Phase 2A direct iteration lacked a production-source regression gate
- Metrics after: Phase 2A behavior, architecture, and zero-allocation matrix is complete
- Visual result: Not applicable
- Residual risk: integrated full architecture gate remains scheduled with `APH-212` after Building Defense synchronization work
- Next ready task: Phase 2A complete; no Resource Exchange task remains in this phase

### 2026-07-09 - APH-207 and APH-208 - Building-defense snapshot removal

- Status: Complete
- Commit or worktree baseline: `59dbb6148`
- Stable commit/push: `b97e2a2d1` pushed to `main`
- Files changed: `BuildingDefenseAttackSystem` and its focused editor tests
- Behavior preserved/changed: target acquisition remains throttled at 0.12 seconds with the same eligibility, nearest ordering, four slots, cooldowns, damage, audio, and VFX; four temporary query snapshots are replaced by persistent unmanaged scratch
- Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=9`; 32-tower/740-candidate warmed update remains zero managed bytes; Runtime, Editor, and Editor-test builds passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph207-defense-validation.log`
- Metrics before: one entity and three component snapshots per acquisition pass
- Metrics after: zero target-acquisition query snapshots and one explicitly disposed persistent candidate list
- Visual result: Not applicable; deterministic ECS behavior
- Residual risk: explicit dependency completion, direct `EntityManager` calls, temporary command/effect containers, and effect-application synchronization remain under `APH-209`
- Next ready task: `APH-209`

### 2026-07-09 - APH-103 - Resolver injection through runtime roots

- Status: Complete
- Commit or worktree baseline: `8ce928759`
- Stable commit/push: `2f7c767fa` pushed to `main`
- Files changed: Menu and Match composition bootstraps, `MainMenuPlayUI`, `UIShellContentView`, focused injection tests, and Unity `.meta`
- Behavior preserved/changed: Menu binds the config-backed resolver before router installation; both Match runtime initialization paths inject it; unbound roots use the immutable fallback
- Validation: `[GameTextResolverInjectionValidation] result=Passed tests=4`; UI Contracts, UI Runtime, Composition, Runtime, Editor, EditMode-test, and PlayMode-test builds passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph103-game-text-resolver-injection.log` and `/private/tmp/warline-aph103-*.log`
- Metrics before: config-backed resolver existed without runtime-root injection
- Metrics after: Menu plus two Match initialization paths are explicitly covered
- Visual result: Not applicable; no screen content changed
- Residual risk: the seven direct `GameText` consumers remain `APH-104`
- Next ready task: `APH-104`

### 2026-07-09 - APH-209 - Building-defense synchronization audit

- Status: Complete
- Commit or worktree baseline: `8ce928759`
- Stable commit/push: `13ff44928` pushed to `main`
- Files changed: `BuildingDefenseAttackSystem` and focused defense tests
- Behavior preserved/changed: target and effect component access now uses explicit ECS lookups; one completion remains visible before five guarded legacy helper/structural boundaries
- Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=10`; warmed 32-tower/740-candidate update remained zero managed bytes; integrated seven-assembly build matrix passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph209-building-defense-focused.log`, `/private/tmp/warline-aph209-building-defense-profiler.log`, `/private/tmp/warline-aph209-building-defense.raw`, and `/private/tmp/warline-aph209-building-defense-summary.md`
- Metrics before: 21 local direct `EntityManager` data operations and one explicit completion without current profiler attribution
- Metrics after: zero local direct data operations; one documented completion; Match average `4.138 ms`, p95 `4.325 ms`; defense `0.058 ms/frame` average, `1.09 ms` maximum, with nested work below `0.001 ms/frame`
- Visual result: Not applicable; raw profiler and deterministic ECS validation
- Residual risk: target-collection, target-selection, and effect-application timing need separate markers under `APH-210`
- Next ready task: `APH-210`

### 2026-07-09 - APH-104 - UI runtime text consumer migration

- Status: Complete
- Commit or worktree baseline: `26c6177e0`
- Stable commit/push: `5358a0c1e` pushed to `main`
- Files changed: seven direct UI runtime consumers, existing runtime feedback sink, command-wheel propagation, `MainMenuPlayUI`, and focused migration tests
- Behavior preserved/changed: 85 configured text expressions now use the injected resolver while preserving exact keys, fallbacks, current-culture formatting, and invalid-format fallback; no static registry or hierarchy search was added
- Validation: migration 4/4 with 62 `Get`, 23 `Format`, and 85 total expressions; order banner 16/16, command feedback 15/15, Build Drawer 25/25; seven-assembly build matrix passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph104-game-text-consumers-final-recovered.log`, `/private/tmp/warline-aph104-order-banner.log`, `/private/tmp/warline-aph104-command-feedback.log`, and `/private/tmp/warline-aph104-build-drawer.log`
- Metrics before: seven runtime files with 85 direct config-owned text calls
- Metrics after: zero direct text calls in those consumers and one baseline fingerprint covering 80 literal plus five dynamic expressions
- Visual result: no visual assets changed; two additional prefab-content suites remain independently red on pre-existing camera-button/status-chip sprite assignments and are not resolver failures
- Residual risk: stale `Game.Configs` imports remain intentionally owned by `APH-105`
- Next ready task: `APH-105`

### 2026-07-09 - APH-210 - Building-defense phase profiler markers

- Status: Complete
- Commit or worktree baseline: `26c6177e0`
- Stable commit/push: `190140146` pushed to `main`
- Files changed: `BuildingDefenseAttackSystem`, Canvas Match profiler capture, and focused defense tests
- Behavior preserved/changed: static markers surround target collection, per-tower selection, and immediate/deferred effects without changing targeting, combat, audio, VFX, or allocation behavior
- Validation: `[BuildingDefenseAttackSystemValidation] result=Passed tests=11`; warmed 32-tower/740-candidate update remained zero managed bytes; Match capture passed at average `3.592 ms` and p95 `3.691 ms`; seven-assembly build matrix passed with 0 errors
- Artifacts: `/private/tmp/warline-aph210-building-defense-marker-registration.log`, `/private/tmp/warline-aph210-building-defense-profiler-registered.log`, and `/private/tmp/warline-aph210-building-defense-registered.raw`
- Metrics before: only whole-system defense timing (`0.058 ms/frame` average in APH-209)
- Metrics after: collection `0.121/0.135 ms` average/max; selection `0.904/0.922 ms`; effects `0.001/0.006 ms`
- Visual result: Not applicable; raw profiler and focused ECS validation
- Residual risk: defense-specific overwrite/audio/VFX integration and the full behavior matrix remain `APH-211`
- Next ready task: `APH-211`

### 2026-07-10 - APH-211 - Integrated defense/combat/audio/VFX matrix

- Status: Complete
- Commit or worktree baseline: `190140146`
- Stable commit/push: `789fd4a03` and `e55729370` pushed to `main`
- Files changed: air-missile fire/impact systems and focused defense, combat/death, and gameplay-audio tests
- Behavior preserved/changed: missile audio queues are established before ECS iteration; emitted event IDs, targeting, damage, VFX, LOD visibility, and defense feedback behavior are unchanged and now covered explicitly
- Validation: Defense `13/13`; automatic faction targeting `3/3`; combat/death `6/6`; gameplay audio `8/8`; building audio `4/4`; unit combat/VFX `3/3`; Runtime and Editor-test builds completed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/warline-aph211-defense.log`, `/private/tmp/warline-aph211-automatic-faction.xml`, `/private/tmp/warline-aph211-combat-death-final-retry.log`, `/private/tmp/aph211-gameplay-audio-rerun.log`, `/private/tmp/warline-aph211-building-audio.log`, and `/private/tmp/warline-aph211-unit-combat.log`
- Metrics before: the integrated matrix stopped after seven gameplay-audio cases because an audio singleton could be created during missile query iteration
- Metrics after: all 37 focused cases pass; missile launch and impact enqueue the dedicated air-missile launch and small-explosion payloads without structural-change exceptions
- Visual result: no visual assets changed; VFX request payloads are validated at the ECS boundary
- Residual risk: the combined ECS/Burst source architecture gate and performance recapture remain `APH-212` and `APH-213`
- Next ready task: `APH-212`

### 2026-07-10 - APH-106 through APH-108 - UI runtime config boundary closeout

- Status: Complete
- Commit or worktree baseline: `207f99142`
- Stable commit/push: `fabfbc947` and integrated guard commit `afe9e7d74` pushed to `main`
- Files changed: UI runtime asmdef, assembly-boundary guard, audio config source ownership, audio config builder namespace, and focused tests; Unity `.meta` GUIDs preserved
- Behavior preserved/changed: configured UI text behavior and audio config assets are unchanged; UI Runtime can no longer compile against `Game.Configs`, and its owned source is guarded against future config imports or direct `GameText` calls
- Validation: resolver contract `5/5`, adapter `5/5`, injection `4/4`, migration `4/4` with 62 gets, 23 formats, and 85 expressions; `[ScriptArchitectureBoundaryValidation] result=Passed tests=31`; `Game.UI.Runtime`, `Game.Composition`, and `Game.Tests.Editor` built with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/aph108-resolver-contract.log`, `/private/tmp/aph108-resolver-adapter.log`, `/private/tmp/aph108-resolver-injection.log`, `/private/tmp/aph108-resolver-migration.log`, and `/private/tmp/postmerge-assembly-boundary-v2.log`
- Metrics before: one forbidden asmdef dependency and no ownership-aware source enumeration guard
- Metrics after: zero forbidden UI Runtime config dependencies/imports/direct calls and all 31 boundary checks green on the POP-13-integrated head
- Visual result: no visual or serialized UI content changed
- Residual risk: none in Phase 1; all future UI Runtime source remains subject to the assembly-boundary gate
- Next ready task: Phase 1 complete

### 2026-07-10 - APH-212 - Integrated ECS/Burst hot-path architecture gate

- Status: Complete
- Commit or worktree baseline: `207f99142`
- Stable commit/push: `401bd1435`, integration merge `87339b802`, and guard commit `afe9e7d74` pushed to `main`
- Files changed: resource-hauler query scratch, audio singleton mutation paths, Resource Exchange visual-cue Burst declaration, ECS hot-path architecture classifications, and focused tests
- Behavior preserved/changed: resource-hauler assignment ordering, audio music/settings state, and Resource Exchange visual cues are unchanged; the final temporary entity snapshot and recurring direct singleton writes were removed
- Validation: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`; Resource production/hauler `36/36`; audio request/state `5/5`; `Game.Runtime`, `Game.UI.Runtime`, `Game.UI.Shell.Ecs`, `Game.Composition`, and `Game.Tests.Editor` built with 0 errors; no Burst compiler errors; `git diff --check` passed
- Artifacts: `/private/tmp/aph212-resource-hauler-fix.log`, `/private/tmp/aph212-audio-singleton-write-retry.log`, and `/private/tmp/postmerge-ecs-hotpath-v2.log`
- Metrics before: one remaining `ToEntityArray(Allocator.Temp)`, two recurring direct audio singleton writes, and managed POP-13 boundaries not yet classified
- Metrics after: 173 ECS `OnUpdate` implementations, 58 Burst, 45 job-backed, 115 non-Burst, two tracked hot paths, zero unclassified systems, and zero snapshot debt
- Visual result: not applicable; deterministic ECS architecture and behavior validation
- Residual risk: the accepted red steady-state Match GC baseline and frame-time comparison remain `APH-213`
- Next ready task: `APH-213`

### 2026-07-10 - APH-213 - Performance recapture and assistant boundary repair

- Status: Complete
- Commit or worktree baseline: `00eac6566`
- Stable commit/push: `77f7581da` and `6cccb42c1` pushed to `main`
- Files changed: assistant/objective buffer capacities, full-shell assistant regression, current Match performance artifacts, and APH-213 GC report
- Behavior preserved/changed: assistant read models, commands, narration, and control ownership remain on the existing UI shell boundary with unchanged dynamic-buffer APIs; large row payloads now use external native buffer storage so the boundary archetype stays below Unity's chunk limit
- Validation: assistant read models `9/9`; assistant rollout `12/12`; `[ScriptArchitectureBoundaryValidation] result=Passed tests=31`; `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`; six first-party assemblies built with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/aph213-performance-baseline-fixed.log`, `Design/AgentReports/performance_regression_match_baseline.json`, `Design/AgentReports/performance_regression_match_baseline.md`, `/private/tmp/aph213-gc-steady-state.log`, and `Design/AgentReports/2026-07-10_aph-213_match-gc-steady-state_failed.md`
- Metrics before: POP-13 added a 2,304-byte inline message buffer to a 15,296-byte archetype, causing 6,783 runtime exceptions and invalidating the first capture; accepted frame baseline average/p95/p99/max `3.42/5.22/6.87/18.13 ms`; GC `269,482` bytes
- Metrics after: zero archetype-size exceptions; average/p95/p99/max `2.49/3.82/5.83/14.52 ms`; GC `234,324` bytes, a 13.0% improvement that remains above the unchanged 1,024-byte target
- Visual result: Match reached the normal ready state with 733 units, 628 buildings, 98 markers, and 46 visible models; no visual assets changed
- Residual risk: external singleton buffers incur bounded first-growth native allocation and reduced locality; steady-state managed GC remains red and must be addressed by measured Phase 7 owners
- Next ready task: `APH-300`

### 2026-07-10 - APH-300 - Persisted settings startup coverage

- Status: Complete
- Commit or worktree baseline: `171acc7e4`
- Stable commit/push: `25b50b0bf` pushed to `main`
- Files changed: existing audio/settings projection Editor test suite only
- Behavior preserved/changed: production behavior unchanged; the real Menu bootstrap `Awake` path is now locked to load persisted settings and apply them without opening the Settings popup
- Validation: `[AudioSettingsUiProjectionValidation] result=Passed tests=7`, `[SettingsPopupValidation] result=Passed tests=8`, `[AssistantSettingsPersistenceValidation] result=Passed tests=3`, Editor-test build 0 errors, and `git diff --check` passed
- Artifacts: `/private/tmp/aph300-settings-startup.log`, `/private/tmp/aph300-settings-popup.log`, and `/private/tmp/aph300-assistant-settings.log`
- Metrics before: persistence and direct runtime projection were tested separately, but no test crossed the actual Menu startup lifecycle
- Metrics after: one startup integration covers persisted audio, graphics, frame-rate, accessibility, and assistant payloads plus ECS audio/assistant projection and zero popup requests/instances
- Visual result: no visual assets changed; the test explicitly proves the popup hierarchy remains absent
- Residual risk: `Awake` and `OnEnable` can both enter initialization; exactly-once startup application remains owned by `APH-304`
- Next ready task: `APH-301`

### 2026-07-10 - APH-301 through APH-303 - Platform frame-rate policy

- Status: Complete
- Commit or worktree baseline: `082241010`
- Stable commit/push: `74f42e192` pushed to `main`
- Files changed: `SettingsService`, Android visual-quality validation, and audio/settings projection validation
- Behavior preserved/changed: Android defaults to 60 FPS, preserves 30/60, and normalizes direct or persisted 120 requests to 60 before application and event publication; Editor/standalone behavior retains 120 FPS support
- Validation: `[AndroidVisualQualityValidation] result=Passed tests=8`, `[AudioSettingsUiProjectionValidation] result=Passed tests=8`, `[SettingsPopupValidation] result=Passed tests=8`, `[ScriptArchitectureBoundaryValidation] result=Passed tests=31`; UI Runtime, Composition, and Editor-test builds passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/aph301-303-android-frame-rate.log`, `/private/tmp/aph302-desktop-frame-rate.log`, `/private/tmp/aph301-303-settings-popup.log`, and `/private/tmp/aph301-303-assembly-boundary.log`
- Metrics before: Android startup initially set 60, but later Settings application could publish and apply 120; mobile defaults and saved values also retained 120
- Metrics after: Android target set is `{30, 60}` with default 60 and no 120 event payload; non-Android target set remains `{30, 60, 120}`
- Visual result: no visual assets changed
- Residual risk: the settings panel still authors a common option set; exactly-once startup ownership and event routing remain `APH-304` through `APH-306`
- Next ready task: `APH-304`

### 2026-07-10 - APH-304 - Exactly-once startup settings routing

- Status: Complete
- Commit or worktree baseline: `a7d138b29`
- Stable commit/push: `7c13bf7e7` pushed to `main`
- Files changed: Menu composition startup routing, audio and assistant settings lifecycle systems, focused startup test, and ECS architecture classification
- Behavior preserved/changed: ECS projection owners now subscribe without loading independently; composition waits for both owners and applies persisted settings once per ECS world, so repeated Menu lifecycle entries do not duplicate application
- Validation: `[AudioSettingsUiProjectionValidation] result=Passed tests=8`, Menu and Match `[SettingsAudioRuntimeSmoke] result=Passed`, `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`; UI Shell ECS, Composition, and Editor-test builds passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/aph304-settings-focused-retry.log`, `/private/tmp/aph304-menu-settings-smoke.log`, `/private/tmp/aph304-match-settings-smoke.log`, and `/private/tmp/aph304-ecs-hotpath.log`
- Metrics before: three startup load/application paths and duplicate Menu `Awake`/`OnEnable` application potential
- Metrics after: one composition-owned persisted load/application after two required owner systems exist, with repeated lifecycle calls remaining at one runtime event
- Visual result: no visual assets changed; Menu and Match HUD remained operational through their real smoke routes
- Residual risk: runtime change subscriptions still live in the ECS lifecycle systems and visual quality is not yet an event target; `APH-305` owns that routing
- Next ready task: `APH-305`

### 2026-07-10 - APH-305 and APH-306 - Event-driven visual-quality routing

- Status: Complete
- Commit or worktree baseline: `3d2f9612a`
- Stable commit/push: `c92d3095e` pushed to `main`
- Files changed: Match composition settings routing, visual-quality apply API, SettingsService domain-reload cleanup/publication seam, and focused visual-quality validation
- Behavior preserved/changed: settings now reach visual quality through one composition-owned subscription; the latest graphics tier is cached until the managed visual owner is ready, applied explicitly on change, and no longer polled each Match frame; shutdown and subsystem registration remove stale listeners
- Validation: `[AndroidVisualQualityValidation] result=Passed tests=10`, Match `[SettingsAudioRuntimeSmoke] result=Passed` at HUD ready, `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`; UI Runtime, Game Runtime, Composition, and Editor-test builds passed with 0 errors; `git diff --check` passed
- Artifacts: `/private/tmp/aph305-306-visual-quality-routing-final.log`, `/private/tmp/aph305-306-match-settings-smoke.log`, and `/private/tmp/aph305-306-ecs-burst-architecture.log`
- Metrics before: one manual visual-quality call ran every Match frame and visual quality did not receive SettingsService changes through composition
- Metrics after: zero Match-frame visual-quality polling calls, one active composition subscription, and explicit Low/Balanced/High/Ultra mapping with verified shutdown unsubscription
- Visual result: Match HUD remained operational; no visual asset changed
- Residual risk: a tier change can still overwrite Day/Night-owned light, ambient, fog, and volume state; `APH-307` and `APH-308` own that boundary and its regression
- Next ready task: `APH-307`

### 2026-07-10 - APH-307 and APH-308 - Day/Night environment authority

- Status: Complete
- Commit or worktree baseline: `51688e0c3`
- Stable commit/push: `7167baa75` pushed to `main` after rebasing over unrelated campaign-design commit `ab3a968ae`
- Files changed: visual-quality static policy, Day/Night runtime volume ownership and shadow cap, Match composition handoff, and focused environment-authority regression
- Behavior preserved/changed: visual-quality changes no longer write sun, ambient, fog, skybox, volume weight, or volume override values; Day/Night clones the selected static volume profile, applies its current-hour values, and remains the sole final environment writer; tier-specific shadow strength is preserved as a cap consumed by Day/Night
- Validation: `[AndroidVisualQualityValidation] result=Passed tests=11`, Match `[SettingsAudioRuntimeSmoke] result=Passed` at HUD ready, `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`, final Unity compilation had 0 C# errors, and `git diff --check` passed
- Artifacts: `/private/tmp/aph307-308-daynight-shadow-policy.log`, `/private/tmp/aph307-308-match-final.log`, and `/private/tmp/aph307-308-ecs-burst-final.log`
- Metrics before: a settings event could restore baseline environment state and apply tier-owned sun/ambient/fog values until the next Day/Night refresh; Ultra also wrote volume weight directly
- Metrics after: zero visual-quality environment writes, one changed-tier Day/Night rebind/reapply, and deterministic 22:00 environment equality across an Ultra-to-High switch
- Visual result: Match HUD remained operational; no visual asset changed; Day/Night state remains authoritative through runtime quality changes
- Residual risk: complete tier-to-pipeline/render-scale/post-processing/AA/shadow mapping still requires the explicit APH-309 matrix
- Next ready task: `APH-309`

### 2026-07-10 - APH-309 - Runtime quality-tier mapping matrix

- Status: Complete
- Commit or worktree baseline: `548b2edf4`
- Stable commit/push: `dcde564ca` pushed to `main`
- Files changed: focused Android visual-quality Editor validation only
- Behavior preserved/changed: production behavior unchanged; the active project profile and runtime visual-quality system are now locked to explicit Low, Medium/Balanced, High, and Ultra mappings
- Validation: `[AndroidVisualQualityValidation] result=Passed tests=12`, final Unity compilation had 0 C# errors, `QualitySettings.asset` and render assets remained clean, and `git diff --check` passed
- Artifacts: `/private/tmp/aph309-tier-mapping.log`
- Metrics before: High and serialized mobile values had focused coverage, but no single test drove and compared every runtime tier
- Metrics after: four runtime modes verify six policy dimensions each: render pipeline, render scale, volume profile, post-processing, AA, and shadow cap
- Visual result: no visual asset changed; tests use the actual configured render assets and restore runtime state on disposal
- Residual risk: integrated popup, Match, and day/dusk/night visual proof remains `APH-310`
- Next ready task: `APH-310`

### 2026-07-10 - APH-310 - Integrated settings, graphics, Match, and visual proof

- Status: Complete with one recorded red visual-readability finding
- Commit or worktree baseline: `655e74cda`
- Stable commit/push: `fd10d99e6` pushed to `main`
- Files changed: deterministic mobile visual capture workflow, reviewed 1920x1080 capture set, manifest, and visual-review report
- Behavior preserved/changed: production rendering behavior is unchanged; the editor proof workflow now sets fixed 12:00 day, project-correct 21:00 dusk, and 23:00 night states and fails when the Day/Night owner is unavailable
- Validation: `[SettingsPopupValidation] result=Passed tests=8`, `[AndroidVisualQualityValidation] result=Passed tests=12`, Match `[SettingsAudioRuntimeSmoke] result=Passed` at HUD ready, `[MobileVisualQualityPlayModeCapture] result=Passed profile=current`, and `git diff --check` passed
- Artifacts: `/private/tmp/aph310-settings-popup.log`, `/private/tmp/aph310-android-visual.log`, `/private/tmp/aph310-match-smoke.log`, `/private/tmp/aph310-visual-capture-dusk-corrected.log`, `Design/AgentReports/2026-07-10_aph-310_integrated_graphics_visual_review.md`, and `Design/AgentReports/Captures/ArchitecturePerformanceHardening/APH-310/`
- Metrics before: the capture workflow used startup time for day, had no dusk state, and only fixed deepest night
- Metrics after: deterministic 12:00/21:00/23:00 evidence plus normal and max-zoom day views; mean luma day gameplay/day max/dusk/night is `133.61/140.66/108.16/22.34`
- Visual result: day and dusk pass structural review without missing geometry, road/terrain changes, or lighting/fog reset; the 23:00 frame is valid but too dark for reliable battlefield identification
- Residual risk: deepest-night readability requires an explicit art/design decision; APH-311 must record it separately in both Android frame-rate captures
- Next ready task: `APH-311`

### 2026-07-10 - APH-400 - Catalog audio content-residency report

- Status: Complete
- Commit or worktree baseline: `51361fa95`
- Stable commit/push: `6f858f762` pushed to `main`
- Files changed: content-residency generator, focused generator tests, and regenerated JSON/Markdown inventory artifacts
- Behavior preserved/changed: runtime audio behavior, event IDs, catalog references, and importers are unchanged; the report now isolates serialized catalog clips from unused legacy audio and records the required per-clip import and size dimensions
- Validation: `[ContentResidencyInventoryValidation] result=Passed tests=8`, `[ContentResidencyInventory] result=Passed assets=4102 roots=12 catalogAudioClips=226`, 226/226 compressed-size measurements, zero report warnings, Unity compilation with 0 C# errors, and `git diff --check` passed
- Artifacts: `/private/tmp/aph400-content-residency-tests.log`, `/private/tmp/aph400-content-residency-generate.log`, `Design/AgentReports/architecture_performance_content_residency_baseline.json`, and `Design/AgentReports/architecture_performance_content_residency_baseline.md`
- Metrics before: the dependency report counted included audio assets but did not identify the runtime catalog set or expose bus/category, duration, format, load type, and decoded-size evidence together
- Metrics after: 234 catalog clips, 580.358 seconds, 49.41 MiB measured compressed storage-memory size, 97.77 MiB decoded PCM estimate; the regenerated runtime catalog is guarded against the source JSON by the `14/14` audio contract
- Visual result: Not applicable
- Residual risk: static dependency/import evidence does not prove simultaneous runtime residency or first-play latency; APH-401 owns controlled Menu/Match playback captures
- Next ready task: `APH-401` (completed 2026-07-10)

### 2026-07-10 - APH-402 and APH-403 - Music/Ambience importer drift

- Status: Complete; APH-403 accepted as a verified no-op
- Commit or worktree baseline: `a95a33030`
- Stable commit/push: `e2eb61b3f` pushed to `main`
- Files changed: deterministic drift report, stale audio contract source-path guard, and matching audio implementation-spec reference
- Behavior preserved/changed: no runtime audio behavior, event ID, catalog, clip reference, or importer changed; contract ownership paths now match the current `Game.Configs` layout
- Validation: `[AudioConfigContractValidation] result=Passed tests=13`; 262 project WAV importers, 15 Music/Ambience assets, 9 JSON catalog references, 9 serialized catalog references, 0 catalog importer mismatches, and 6 unreferenced legacy clips
- Artifacts: `/private/tmp/aph402-403-audio-contract.log` (stale-path reproduction), `/private/tmp/aph402-403-audio-contract-fixed.log`, and `Design/AgentReports/2026-07-10_aph-402_audio_catalog_drift_audit.md`
- Metrics before: catalog drift and legacy inventory were not separated; the audio contract runner failed on five obsolete source paths
- Metrics after: all 9 cataloged Music/Ambience clips are verified Streaming/not-preloaded/background-loaded, 6 legacy clips are excluded from runtime residency, and the 13-test contract is green
- Visual result: Not applicable
- Residual risk: unused legacy clips can still affect repository/build-size work if later included by a build path, but they do not affect current runtime catalog residency
- Next ready task: `APH-404` follows the accepted `APH-401` measured baseline

### 2026-07-10 - APH-401 - Controlled audio playback residency

- Status: Complete
- Stable commits/pushes: runtime parity and first capture `c2ead0fa1`; evidence hardening `8e1f21c2a`; acceptance reports `0aecb723b`
- Files changed: batchmode capture utility, focused capture-contract tests, source/runtime catalog parity guard, serialized runtime catalog, Menu/Match JSON and Markdown evidence, and refreshed content-residency baseline
- Behavior preserved/changed: runtime playback behavior is unchanged; the serialized runtime catalog now includes all 234 source events, and evidence capture rejects missing events, duplicate runtime views, active-source baselines, unstable residency, incomplete phase sets, and missing provenance
- Validation: audio contracts `14/14`; capture contracts `7/7`; Menu capture passed `3/3` required phases; Match capture passed `5/5`; all requested UI/combat/music/ambience/ARIA events reached `Presented`; second review reported no remaining findings
- Metrics before: 226 serialized runtime clips; Match baseline had one unidentified active source; post-playback samples occurred immediately at `AudioSource.Play`; timestamps used mixed origins; raw profiler provenance was transient and unhashed
- Metrics after: 234 source-parity clips; Menu and Match baselines have zero active sources; streamed music/ambience raise settled loaded counts from 225 to 226/227; reports record one clock origin, exact commit, dirty state, catalog hash, raw-profiler size/hash, build target, and reproducible invocation
- Visual result: Not applicable; controlled audio and memory evidence only
- Residual risk: raw profiler files are intentionally local/transient because the Match capture is hundreds of MiB; the committed report records their hashes and exact regeneration commands. Android first-play/glitch evidence remains owned by `APH-405` and requires a physical device.
- Next ready task: `APH-404`; `APH-405` remains device-gated

### 2026-07-10 - APH-500 - Android BuildReport top-100 tooling

- Status: Active; tooling complete, release APK/AAB evidence pending
- Stable tooling commit/push: `671a009aa`
- Files changed: `AndroidBuildReportGenerator`, focused tests, release Android build integration, and Jenkins artifact archiving
- Behavior preserved/changed: profiler builds are unchanged; release APK/AAB builds now request `DetailedBuildReport` and fail rather than publish incomplete report evidence
- Validation: focused aggregation/evidence contract `6/6`; Unity compile clean; deterministic ordering, normalized paths, top-100 cap, packed-overhead accounting, and report provenance are covered
- Metrics before: Android artifact stages produced APK/AAB files without deterministic included-asset attribution
- Metrics after: tooling can produce separate APK and AAB JSON/Markdown reports with top-100 packed contributions, unattributed bytes, packed-file overhead, summary remainder, artifact bytes, and SHA-256
- Visual result: Not applicable
- Residual risk: no release APK/AAB evidence exists yet; both long-running builds and report review are required before completion
- Next ready task: run the release APK and AAB build commands from commit `671a009aa` or later

### 2026-07-10 - APH-604 - Deterministic rebake and manifest-owned cleanup

- Status: Complete
- Commit or worktree baseline: `9280ead85`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: static-map editor baker, canonical source hash, output ownership utility, focused ownership tests, generated manifest provenance/content hashes, and evidence report
- Behavior preserved/changed: canonical Match and generated scene content are unchanged; identical rebakes skip all scene writes, and stale cleanup can target only validated scene paths captured from the prior manifest
- Validation: Unity ownership/reuse tests `23/23`; real AssetDatabase rollback/GUID restoration `1/1`; migration bake passed; identical rebake reported `reusedScenes=1 scenesWritten=0 staleScenesDeleted=0`; exhaustive APH-605 structural parity passed `2/2` in `658.45 s`; editor assemblies build with zero errors
- Artifacts: `/private/tmp/warline-aph604-ownership-escalated.xml`, `/private/tmp/warline-aph604-rollback-integration-rerun-escalated.xml`, `/private/tmp/warline-aph604-migration-bake-escalated.log`, `/private/tmp/warline-aph604-identical-reuse-escalated.log`, `/private/tmp/warline-aph605-structural-final-escalated.xml`, and `Design/AgentReports/2026-07-10_aph-604_deterministic_rebake_and_cleanup.md`
- Metrics before: identical full bake rewrote 525 scenes and churned 312,777 inserted/deleted YAML lines because generated object file IDs changed
- Metrics after: one integrity/layer migration wrote 525 scenes and content hash `393591d2855b764bce260888e6f5fa20`; the identical follow-up reused 525/525 scenes with zero writes and zero stale deletions
- Visual result: Not applicable; no runtime loading or scene content changed
- Residual risk: abrupt editor termination or power loss cannot invoke in-process rollback; runtime manifest ownership/build inclusion and streaming remain APH-606/607
- Next ready task: `APH-606`

### 2026-07-10 - APH-605 - Full static-map structural parity

- Status: Complete
- Commit or worktree baseline: APH-604 migrated output at content hash `393591d2855b764bce260888e6f5fa20`
- Stable commit/push: pending coordinated stable-slice commit
- Behavior preserved/changed: validation only; transform and bounds comparisons use the accepted D-021 serialization tolerances while identity/state/component contracts remain exact
- Validation: `StaticMapPresentationStructuralValidationTests` passed `2/2` over 17,564 renderers and 525 additive scenes in `658.45 s`
- Artifact: `/private/tmp/warline-aph605-structural-final-escalated.xml`
- Residual risk: runtime chunk loading and Android build inclusion are not part of APH-605 and remain APH-606/607
- Next ready task: `APH-606`

### 2026-07-10 - APH-700 through APH-702 - Dependency report and source growth guards

- Status: Complete
- Commit or worktree baseline: `9280ead85`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: APH-700 generator/scanner/models/tests and deterministic JSON/Markdown reports; APH-701/702 baseline manifest and focused architecture tests
- Behavior preserved/changed: editor/test architecture tooling only; no runtime, scene, prefab, config, or visual behavior changed
- Validation: independent reviews closed all scanner, freshness, exception, ratchet, malformed-input, and Jenkins findings; APH-700 Unity tests passed `7/7`; APH-701/702 Unity tests passed `15/15`; Game Editor and Editor-test builds pass with zero errors
- Artifacts: `Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json`, matching Markdown report, and `Design/Architecture/production_source_growth_baseline.md`
- Metrics before: no complete assembly-edge/type-reference report; helper and oversized-file debt was not frozen globally
- Metrics after: 18 assemblies, 79 first-party edges, 1,132 owned source files, 2,462 visible types, 29,641 resolved cross-domain occurrences, 265 frozen helper paths, 108 reviewed files above 500 lines, and 27 strict no-growth files above 1,000 lines
- Visual result: Not applicable
- Residual risk: APH-700 remains a context-aware lexical report rather than a compiler semantic-symbol graph; Jenkins PowerShell behavior is contract-tested locally but still awaits the next Windows agent execution
- Next ready task: `APH-704` remains a measured prototype and must not enter production without its adoption gates

### 2026-07-10 - APH-703 - Default-world and recurring GC-owner inventory

- Status: Complete
- Commit or worktree baseline: `9280ead85`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: `Design/AgentReports/2026-07-10_aph-703_default-world-and-gc-owner-inventory.md`
- Behavior preserved/changed: analysis only; no runtime, scene, prefab, config, or asset behavior changed
- Validation: independent source recount reproduced 91 occurrences across 44 files, all CE/PE/AD/HSL classification totals, and all 13 APH-007 owners totaling 204,480 bytes / 3,676 samples; no findings
- Artifacts: `Design/AgentReports/2026-07-10_aph-703_default-world-and-gc-owner-inventory.md`
- Metrics before: recurring lookup and allocation ownership was distributed across source and raw profiler metadata
- Metrics after: 9 hidden service-locator sites and 13 recurring allocation owners have explicit remediation ownership for APH-707 through APH-710
- Visual result: Not applicable
- Residual risk: source classification does not remove the debt; APH-710 and domain decomposition tasks must eliminate or explicitly retain each owner before APH-711
- Next ready task: `APH-710` remediation remains gated by the relevant domain slices

### 2026-07-10 - APH-603 - Shared-mesh additive presentation baker

- Status: Complete
- Commit or worktree baseline: `554ea2c5a`
- Files changed: runtime-readable presentation manifest model, editor-only canonical Match baker, 525 generated additive scenes, generated manifest, and focused evidence report
- Behavior preserved/changed: canonical Match authoring and runtime behavior are unchanged; no generated scene is in build settings or runtime loading yet. The baker creates presentation-only clones that retain shared mesh/material references and exact representable world transforms.
- Validation: Unity compilation with 0 C# errors, Android visual/import `12/12`, Game Runtime and Editor .NET builds, 525/525 scene-path presence, 17,564 unique source IDs, and zero forbidden generated components/assets/static-batch flags
- Artifacts: `Assets/Game/GeneratedStaticMapPresentation/`, `/private/tmp/warline-aph603-bake.log`, `/private/tmp/warline-aph603-compile-retry.log`, and `Design/AgentReports/2026-07-10_aph-603_static_map_presentation_bake.md`
- Metrics before: no overlay-safe shared-mesh additive presentation output or ownership manifest
- Metrics after: 17,564 sources, 525 chunks, 63 MB generated output, 14 MB manifest, zero duplicated mesh/material assets, and content hash `05da13c7d8593ee269ad730856904213`
- Visual result: no runtime visual change in this slice; generated scenes are not loaded. Canonical `Match.unity` remained clean.
- Residual risk: stale generated scenes are not removed yet; deterministic rebake and sentinel-safe cleanup are owned by APH-604. Full source/renderer-state equivalence remains APH-605.
- Next ready task: `APH-604`
- Stable commit/push: `9e80c0f1d` committed to `main`; push recorded by the follow-up tracker checkpoint

### 2026-07-10 - APH-606 - Composition-owned manifest and Android scene inclusion

- Status: Complete
- Commit or worktree baseline: `1224f9bf4`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: `MatchSceneView`, canonical Match serialization, Android build scene resolver and tests, deterministic editor scene wiring and tests, generated manifest dependency hash, and refreshed APH-700 reports
- Behavior preserved/changed: non-Android build lists remain unchanged; Android release and profiler builds now package enabled base scenes plus exactly all 525 manifest chunks. Runtime chunk loading remains APH-607.
- Validation: resolver `21/21`, scene wiring `2/2`, APH-700 `7/7`, APH-701/702 `15/15`, all 12 first-party assemblies with zero errors, and two identical post-wiring bakes at content hash `393591d2855b764bce260888e6f5fa20` with `reusedScenes=1`, `scenesWritten=0`, and `staleScenesDeleted=0`
- Independent review: fixed stale canonical-dependency acceptance and missing non-Match base-scene acceptance before final validation; no remaining finding
- Residual risk: packaging does not control residency; bounded preload/load/unload ownership and Match teardown are APH-607
- Next ready task: `APH-607`

### 2026-07-11 - APH-607 - Bounded map presentation streaming and teardown

- Status: Complete
- Commit or worktree baseline: `417333ed3`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: plain managed presentation streamer, detached-operation cleanup owner, manifest index, Unity scene API adapter, Menu composition lifecycle integration, focused behavior/composition tests, source-growth authorization, and refreshed dependency reports
- Behavior preserved/changed: Android Match now preloads the camera footprint plus one chunk ring before gameplay starts, streams with a second-ring unload buffer, and unloads every presentation chunk before the Match scene. Non-Android production remains a no-op. No gameplay `ISystem`, `SystemBase`, `MonoBehaviour`, scene search fallback, or background Unity API access was added.
- Bounds: 64 queued requests maximum, one active scene operation, one operation start per update, and at most 16 counted scene-state checks per update; small manifests perform at most one load and one unload reconciliation pass rather than consuming the full budget repeatedly
- Lifecycle safety: preload failure still permits draining; route reversal waits for drain then rebinds; an in-flight operation cannot be abandoned by unbind; detached loads are unloaded while the menu remains open; cleanup retries once and then fails closed
- Validation: streamer/composition `31/31`, production source-growth `15/15`, APH-700 dependency report `7/7`, final changed-scope .NET assemblies `4/4` with zero errors, and `git diff --check`; the complete 12-assembly matrix also passed earlier in the same slice before the final lifecycle-only helper addition
- Independent review: the same reviewer was reused across follow-ups; all scene-check, drain, route reversal, disabled-platform, active-operation ownership, orphan cleanup, and no-rebind cleanup findings were fixed or withdrawn; no actionable finding remains
- Artifacts: `/private/tmp/warline-aph607-final-r2-focused.xml`, `/private/tmp/warline-aph607-source-growth-final-r5.log`, `/private/tmp/warline-aph607-aph700-final-tests.xml`, and refreshed APH-700 JSON/Markdown reports
- Tooling note: final report generation emitted its success marker and wrote both reports before Unity 6000.5.2f1 crashed during domain-unload shutdown; the subsequent APH-700 test run exited normally and passed `7/7`
- Residual risk: Android frame-time, memory, package, screenshot, and 10-minute visual-soak acceptance remain in the next map-optimization tasks
- Next ready task: enable Unity mobile GPU instancing for repeated map visuals, retain a measured fallback, then compare device performance and screenshots

### 2026-07-11 - APH-608 and partial APH-609 - Android GPU instancing

- Status: GPU instancing configuration complete; full device comparison active
- Commit or worktree baseline: `7986d7c9c`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: Mobile URP asset, Android batching setting, refreshed static-map manifest dependency hash, Android visual-quality validation, comparison report, and this tracker
- Behavior preserved/changed: repeated compatible map renderers now use Unity GPU Resident Drawer `InstancedDrawing`; incompatible renderers stay on Unity's ordinary URP path. GPU occlusion and small-mesh culling remain disabled. No gameplay system or custom render owner was added.
- Validation: Android visual-quality plus build-scene resolver tests pass `36/36`; dependency/source architecture tests pass `22/22`; changed Editor-test assembly builds with zero errors; profiler APK builds with zero shader/compiler errors; runtime emits `GPU Resident Drawer created.`; final screenshot retains terrain, buildings, props, units, HUD textures, command controls, and minimap; no shader/BRG/crash/missing-reference errors
- Same-source device comparison: instanced versus prior settings measured `36.3 vs 32.6 FPS`, `21.69 vs 28.82 ms GPU`, `3.07 vs 5.24 ms render thread`, and `45.0 vs 49.2 SetPass`; draw calls and geometry remained effectively unchanged
- Memory tradeoff: development PSS increased approximately 75 MB and graphics PSS approximately 45 MB; this requires release-device budget evidence before product acceptance
- Build evidence: ignored profiler APK SHA-256 `e230e52a40a45bbbc2afd82a52f83227f5cf519ee562afbd93f1edcadf48e57d`; generated map rebake reused all 525 scenes with zero scene writes/deletes
- Artifact: `Design/AgentReports/2026-07-11_gpu_instancing_android_comparison.md`
- Residual risk: 60 FPS remains red; comparison order was not thermally randomized; cold release startup, package, memory, full visual matrix, and extended soak evidence remain open
- Next ready task: isolate the remaining GPU geometry cost and release-memory overhead before enabling any additional culling or removing the validated fallback path

### 2026-07-10 - APH-501 - Tracked package and memory budget authority

- Status: Active; clean release package budgets accepted, release-device memory limits pending
- Commit or worktree baseline: `1224f9bf4`
- Stable commit/push: pending coordinated stable-slice commit
- Files changed: schema-4 accepted baseline JSON, product-budget validator and focused tests, and performance regression contract
- Behavior preserved/changed: tooling/config only; APK and AAB budgets now ratchet against exact clean APH-500 artifacts. Unmeasured installed size and absolute peak/texture/mesh/audio/graphics-driver memory limits remain fail-closed `null`.
- Validation: `[PerformanceProductBudgetValidation] result=Passed tests=5`, Game Editor and Editor-test assemblies plus the complete 12-assembly matrix build with zero errors, and `git diff --check` passes
- Residual risk: accepted release-device captures are still required before absolute installed-size and memory limits can replace measurement-required states
- Next action: collect the required release artifact/device/scenario/profiler metrics under the serialized Android evidence lane

### 2026-07-13 - APH-501 - Budget authority audit and schema-4 alignment

- Status: stable contract repair; APH-501 remains active for measured release-device limits
- Authority: accepted clean APK `<= 463,359,198` bytes and AAB `<= 426,399,778` bytes remain unchanged and resolve to their immutable release BuildReport evidence
- Fail-closed limits: installed size and absolute peak/texture/mesh/audio/graphics-driver memory stay `null`; no current evidence supports inventing a numeric ceiling
- Contract repair: the validator and focused test now require accepted baseline version 4 and the exact accepted 20 ms Editor p95 ratchet instead of stale schema-3/50 ms assertions
- Validation: product-budget contract `5/5`, Editor-test build with zero errors, scoped whitespace passes, and the non-Unity Android gate audit passes `46/46`
- Evidence: `Design/AgentReports/2026-07-13_aph-501_product_budget_evidence_handoff.md` and `/private/tmp/warline-aph501-budget-schema-r2.log`
- Next action: produce a clean package-compliant release APK, collect accepted APH-804 device evidence, then capture same-artifact category residency before replacing any `null` limit

### 2026-07-10 - APH-311 and APH-601 - Android 96 m combine candidate rejection

- Status: Partial evidence accepted; implementation candidate rejected
- Commit or worktree baseline: `4c05a2da1` plus ignored/uncommitted transient Android output
- Stable evidence commit: `f5375ee37`
- Behavior preserved/changed: canonical `Assets/Game/Scenes/Match.unity` remained untouched; Android build tooling generated an ignored Match copy with 602 combined meshes and replaced covered source renderers only in that copy
- Validation: Unity profiler APK built with 0 compiler errors; install and deterministic Match auto-start passed; post-rejection Android visual/import validation passed `12/12`; Match reached static-map startup in `3.24 s` and gameplay-ready in `6.60 s`; device thermal status remained `0`; a foreground Match screenshot is nonblank and structurally coherent at the tested camera
- Artifacts: `Design/AgentReports/2026-07-10_perf_WarlineCapture_candidate_android_full_summary.md`, `Design/AgentReports/2026-07-10_perf_WarlineCapture_candidate_android_steady_summary.md`, `/private/tmp/warline-aph-candidate-profiler-build.log`, and ignored raw capture `ProfilerCaptures/WarlineCapture_2026-07-10_15-40-candidate.data.raw`
- Metrics: steady avg `22.17 ms / 45.1 FPS`, p95 `26.80 ms`, p99 `31.13 ms`, GPU avg/p95 `20.23/20.87 ms`, CPU active avg `16.77 ms`, SetPass `51`, triangles `1,058,417` avg, vertices `1,995,230` avg, and `7,612,760` GC bytes across 1,700 development-profile frames
- Rejection reason: both candidates fix the 9% load stall and draw-call pressure but miss the 60 FPS GPU budget; the 32 m variant lowers geometry but is insufficient alone. Their generator can also remove overlay-sensitive source filters and therefore does not meet the Phase 6 quality lock
- Next ready action: test finer presentation-culling granularity without committing the transient candidate, then implement the accepted path through APH-603 through APH-609 with source/overlay validation

### 2026-07-09 - Unity validation timeout reliability

- Status: Complete infrastructure repair; no checklist item added
- Commit or worktree baseline: `26c6177e0`
- Stable commit/push: `88404af67` pushed to `main`
- Files changed: `Tools/CI/invoke_unity_macos.sh`
- Behavior preserved/changed: timeout duration is counted by completed one-second wait cycles instead of adjustable wall-clock time; Unity invocation arguments and cleanup behavior are unchanged
- Validation: `bash -n` and `git diff --check` passed; subsequent focused Unity and 240-frame profiler runs completed under the corrected timeout
- Artifacts: APH-104 and APH-210 logs above
- Metrics before: system-time adjustments could falsely report a 900-second timeout within one launch interval
- Metrics after: completed validations exit normally while retaining hard timeout enforcement
- Visual result: Not applicable
- Residual risk: Unity package initialization can still be slow; it no longer causes false elapsed-time calculation
- Next ready task: continue Wave 1

### 2026-07-11 - APH-611 - Android static-map integrity soak

- Status: Complete for map integrity and process survival; release performance remains active under APH-609 and APH-803
- APK: ignored profiler artifact SHA-256 `959da0fbc8c6e82073370fc18c41cd0a21896e20c283e7dc8b72bd8717ebc4ea`
- Device: Xiaomi 24090RA29G, Android 16
- Duration: `762.363` seconds of Match diagnostics; PID `24307` remained resumed and foreground
- Visual result: valid 60-second, 600-second, and post-soak captures preserve the tested terrain, roads, structures, vehicles, props, and HUD without an observed culling hole, floating/buried prop, lighting discontinuity, or terrain-quality regression
- Rejected evidence: the 300-second capture contains Android readback tiles across both HUD and world; three immediate repeats are coherent, so the invalid capture is retained but excluded
- Runtime result: final 235-sample window averaged `45.54 FPS`, `22.04 ms` frame time, and `20.26 ms` across valid GPU samples; logs contain no static-map failure, missing-reference exception, crash, or application ANR
- Thermal boundary: HAL skin reached status `3`; development PSS reached `2,445,302 KB`. This run cannot close release FPS, sustained-performance, or memory budgets.
- Evidence: `Design/AgentReports/2026-07-11_aph-611_android_map_soak.md`
- Next ready task: finish APH-610 mesh Read/Write proof, then collect thermally controlled release-device APH-609/APH-803 evidence

### 2026-07-11 - APH-610 - Static-map ownership and mesh Read/Write closeout

- Status: Complete
- Runtime result: valid Android manifests suppress all `17,564` canonical renderers and bypass legacy runtime static-map combination; stale/missing manifests retain the visible canonical/legacy fallback
- Inventory: `1,186` unique mesh sub-assets across `830` manifest FBX importers; `758` are already unreadable and `72` remain readable
- Read/Write decision: no additional importer is safe to change while runtime roads, decorations, and the accepted legacy fallback use CPU mesh combination
- Behavior preserved/changed: no importer, scene, manifest, road, decoration, collider, or fallback behavior changed in this proof slice
- Independent review: two bounded audits independently identified the same 830/758/72 inventory and the same runtime CPU-consumer boundary
- Evidence: `Design/AgentReports/2026-07-11_aph-610_static_map_mesh_read_write_proof.md`
- Next ready task: thermally controlled release-device APH-609/APH-803 capture and the remaining architecture decomposition slices

### 2026-07-11 - APH-705 and APH-706 - Combat observation assembly split

- Status: Complete
- Baseline: `0137e9cc0`
- Behavior preserved/changed: combat-damage observation ownership moved into `Game.Runtime.Combat`; damage, targeting, queue retention, audio, VFX, UI, and visual behavior are unchanged
- Dependency result: `Game.Runtime -> Game.Runtime.Combat -> Game.Components` and `Game.Tests.Editor -> Game.Runtime.Combat`; no reverse combat-to-runtime edge or first-party cycle
- Integrated repair: shared narrative contracts moved down to the catalog boundary, frozen source-growth debt removed through partial extraction/renaming, and disabled spatial-index structural mutations converted to command-buffer/singleton access
- Validation: combat observation `9/9`, assembly boundary `31/31`, ECS/Burst `10/10`, source growth `15/15`, unit combat `3/3`, building defense `13/13` with zero measured allocation, ground missile `5/5`, air missile pass, narrative menu `5/5`, narrative presentation `6/6`, APH-700 generator pass, zero Unity compiler errors, and `git diff --check` pass
- Evidence: `Design/AgentReports/2026-07-11_aph-706_combat_observation_assembly_split.md`
- Residual risk: larger combat systems remain coupled to movement, building runtime, audio, VFX, and presentation and must not move until those reverse dependencies are removed
- Next ready task: continue one bounded architecture decomposition slice or close thermally controlled Android performance/device gates

### 2026-07-11 - APH-707 - Transport boarding decomposition

- Status: Complete
- Baseline: `471986d86`
- Behavior preserved/changed: the same unmanaged transport command owner, public API, update ordering, queries, and source GUID remain; methods moved verbatim into lifecycle, routing, boarding, approach, airdrop, and disembark partials
- Structural result: original owner reduced from 3,226 to 181 lines; nine new partials are 241-416 lines; deterministic reconstruction matches all 16,571 original implementation tokens
- Integrated repair: source-reading guards cover all partials, partial systems inherit their declared ECS base in the non-ECS inventory, transport recovery reuses the central target/path command application, and DR-001 path setup uses the central path-request owner
- Validation: transport `78/78`, selection `60/60`, scenario `15/15`, extraction `33/33`, DR-001 `2/2`, gateway PlayMode `1/1`, performance/allocation pass, source growth `15/15`, ECS/Burst `10/10`, assembly boundary `31/31`, three project builds with zero errors, and `git diff --check` pass
- Residual evidence: the broader scene transport fixture passes `8/9`; its existing soldier parachute visual offset expectation is `1.0` while runtime is `1.2`, and no visual-height code changed in this slice
- Evidence: `Design/AgentReports/2026-07-11_aph-707_transport_boarding_decomposition.md`
- Next ready task: close an active device/product gate or begin APH-708 only after assigning a disjoint UI gateway ownership boundary

### 2026-07-12 - APH-708 - UI shell gateway decomposition

- Status: Complete
- Baseline: `dc13f0d44`
- Behavior preserved/changed: the same registered facade, contracts, public signatures, explicit interface surface, cache reset, and view call sites remain; implementation ownership moved into private route, action, settings, and read-model adapters
- Structural result: gateway reduced from 2,802 to 252 lines; all adapter files remain below 500 lines and inside `Game.UI.Shell.Ecs`; no serialized class, prefab, contract, or assembly edge changed
- Integrated repair: authoritative empty fuel summaries no longer expose mock header fuel, Entities buffer tests reacquire after structural changes, Hold/Stop HUD tests match the established pressable-rejection design, and a stale narrative `Presenter` type name was removed with GUID preservation
- Validation: shell content `11/11`, assistant commands `6/6`, assistant panel `2/2`, resource header `8/8`, resource routing `5/5`, lifecycle PlayMode `1/1`, narrative presentation `6/6`, source growth `15/15`, boundary `31/31`, broad shell `1/1`, ECS/Burst `10/10`, APH-700 generation pass, three project builds with zero errors, and `git diff --check` pass
- Evidence: `Design/AgentReports/2026-07-12_aph-708_ui_shell_gateway_decomposition.md`
- Residual risk: shared static cache ownership and default-world lookups remain for APH-710/711; this slice does not claim a Match FPS or GC reduction
- Next ready task: close an active Android/product gate or select APH-709 only when profiler/change-frequency evidence proves a bounded selection/HUD target

### 2026-07-12 - APH-709 - Selection camera query decomposition

- Status: Complete
- Baseline: `3b305ffd1`
- Behavior preserved/changed: the public runtime camera helper, request routing, pan/zoom behavior, tactical-follow ownership, views, serialized types, and assembly edges are unchanged; only tactical-follow mode/pose query ownership moved into a world-bound cache
- Measured result: the two recurring 11,960-byte tactical-camera rows from APH-007/213 are absent; a warmed 300-tick focused test allocates zero bytes and the same cache correctly rebinds across world replacement
- Integrated result: current full Match GC is `266,974 / 1,024` bytes and remains red because unrelated scene-root, transport, UI shell, command-query, audio, and pathfinding owners remain; Match performance passes at average/p95/p99/max `2.75/4.31/5.42/15.59 ms` with zero current-thread allocation
- Validation: camera `31/31`, selection `60/60`, selection/move/attack PlayMode `1/1`, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, and Runtime/Editor-test project builds pass with zero compiler errors
- Evidence: `Design/AgentReports/2026-07-12_aph-709_selection_camera_query_decomposition.md`, `/private/tmp/warline-aph709-camera-final.log`, `/private/tmp/warline-aph709-selection.log`, `/private/tmp/warline-aph709-playmode-r2.xml`, `/private/tmp/warline-aph709-growth.log`, `/private/tmp/warline-aph709-boundary.log`, `/private/tmp/warline-aph709-burst.log`, `/private/tmp/warline-aph709-performance.log`, and `/private/tmp/warline-aph709-gc-r2.log`
- Residual risk: the unchanged 1,024-byte global GC gate remains red; APH-710 owns remaining explicit-dependency/allocation remediation and APH-711 owns final integrated recapture
- Next ready task: remove the smallest independently testable APH-710 recurring allocation owner, beginning with the audio gameplay-state query cache

### 2026-07-12 - APH-710 - Audio gameplay-state query cache

- Status: Stable partial slice; APH-710 remains active
- Baseline: `1616ef8c4`
- Behavior preserved/changed: audio event routing, gameplay-only culling, source pooling, settings fades, mixer behavior, and serialized configuration are unchanged; the existing bridge/view lifecycle now owns one world-bound gameplay-state query
- Measured result: the recurring 11,960-byte audio owner is absent, camera owners remain absent, and full Match GC improves from `266,974` to `235,496` bytes while the unchanged `1,024`-byte gate remains red
- Validation: audio bridge `8/8`, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, Runtime and Editor-test builds with zero errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_audio_gameplay_state_query_cache.md`, `/private/tmp/warline-aph710-audio-final-r2.log`, `/private/tmp/warline-aph710-audio-growth-r2.log`, `/private/tmp/warline-aph710-audio-boundary.log`, `/private/tmp/warline-aph710-audio-burst.log`, and `/private/tmp/warline-aph710-audio-gc-r2.log`
- Residual risk: scene-root, transport, UI shell, road/building command-query, pathfinding, selection-panel, and hidden-world owners remain; APH-710 cannot close until each is removed or explicitly reassigned and APH-711 repeats the integrated lane
- Next ready task: design the lifecycle-safe non-allocating Match scene-view cache or remove another independent command-query owner

### 2026-07-12 - APH-710 - Match scene root lookup

- Status: Stable partial slice; APH-710 remains active
- Baseline: `ca663b6ef`
- Behavior preserved/changed: Match scene discovery, static-map presentation, runtime UI binding, unload/reload, and root-only view ownership remain unchanged; the helper reuses a capacity-stable root list instead of allocating two arrays per frame
- Measured result: both 38,272-byte scene-reference rows are absent, prior audio/camera rows remain absent, and full Match GC improves from `235,496` to `167,712` bytes while the unchanged `1,024`-byte gate remains red
- Validation: scene reference `5/5`, Menu-to-Match-to-Menu PlayMode `1/1`, source growth `15/15`, assembly boundary `31/31`, Composition and Editor-test builds with zero errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_match_scene_root_lookup.md`, `/private/tmp/warline-aph710-scene-reference-final.log`, `/private/tmp/warline-aph710-scene-lifecycle.xml`, `/private/tmp/warline-aph710-scene-growth.log`, `/private/tmp/warline-aph710-scene-boundary.log`, and `/private/tmp/warline-aph710-scene-gc-final.log`
- Residual risk: transport helper construction, UI shell attribution, road/building command queries, pathfinding diagnostics, intermittent selection-panel allocations, and hidden world lookups remain
- Next ready task: remove one isolated road/building command-query owner with lifecycle-bound query/entity caching

### 2026-07-12 - APH-710 - Road command entity cache

- Status: Stable partial slice; APH-710 remains active
- Baseline: `93c8df15b`
- Behavior preserved/changed: road command requests, results, processing, queue repair, and lifecycle behavior are unchanged; recurring polling now validates a cached world/entity handle and queries only on cold binding or recovery
- Measured result: the recurring 11,960-byte road command-query row is absent; the warmed full capture reports `187,690 / 1,024` bytes and remains red from unrelated owners
- Validation: road command `11/11`, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, Runtime and Editor-test builds with zero errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_road_command_entity_cache.md`, `/private/tmp/warline-aph710-road-command-final.log`, `/private/tmp/warline-aph710-road-growth.log`, `/private/tmp/warline-aph710-road-boundary.log`, `/private/tmp/warline-aph710-road-burst.log`, and `/private/tmp/warline-aph710-road-gc-r2.log`
- Residual risk: building camp/production/placement command queries, transport helper construction, UI shell attribution, pathfinding diagnostics, selection-panel allocations, and hidden world lookups remain
- Next ready task: cache the building production command entities behind one world-bound owner without changing production semantics

### 2026-07-12 - APH-710 - Building production command entity cache

- Status: Stable partial slice; APH-710 remains active
- Baseline: `012feb64f`
- Behavior preserved/changed: production and camp-item request/result semantics are unchanged; one cache now owns positive and absent queue resolution, world rebinding, destroyed-entity recovery, and buffer repair
- Integrated repair: runtime faction summary publication ensures both boundary buffers before retaining writable handles, preventing structural-change invalidation exposed by the broad production runner
- Measured result: both recurring 11,960-byte building production command-query rows are absent; the warmed full capture improves from `187,690` to `145,792 / 1,024` bytes and remains red from unrelated owners
- Validation: building production `26/26`, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, Runtime and Editor-test builds with zero errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_building_production_command_entity_cache.md`, `/private/tmp/warline-aph710-building-command-cache-negative-focused.log`, `/private/tmp/warline-aph710-building-command-cache-negative-source-growth.log`, `/private/tmp/warline-aph710-building-command-cache-negative-boundary.log`, `/private/tmp/warline-aph710-building-command-cache-negative-ecs-burst.log`, and `/private/tmp/warline-aph710-building-command-cache-match-gc-r2.log`
- Residual risk: building placement, transport helper construction, UI shell attribution, pathfinding, selection-panel allocations, and hidden world lookups remain
- Next ready task: remove the isolated building placement command-query owner with the same absent-entity evidence requirement

### 2026-07-12 - APH-710 - Building placement command entity cache

- Status: Stable partial slice; APH-710 remains active
- Baseline: `f2028be02`
- Behavior preserved/changed: placement request/result processing is unchanged; one positive/negative cache owns idle lookup, queue creation, world rebinding, destroyed-entity recovery, and buffer repair
- Measured result: the recurring 11,960-byte placement lookup row is absent; the candidate capture reports `161,180 / 1,024` bytes, with cross-run pathfinding variance preventing total-byte comparison
- Integration repair: `FirstLaunchNarrativePlayer.cs` shrank from 595 to 487 lines by extracting audio routing and presentation-model construction into same-domain types; no source budget or allowlist changed
- Validation: placement command `16/16`, narrative player `5/5`, narrative presentation `9/9`, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, Composition and Editor-test builds with zero errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_building_placement_command_entity_cache.md`, `/private/tmp/warline-aph710-placement-command-cache-focused.log`, `/private/tmp/warline-narrative-source-growth-repair-player.log`, `/private/tmp/warline-narrative-source-growth-repair-presentation.log`, `/private/tmp/warline-narrative-source-growth-repair-gate.log`, `/private/tmp/warline-placement-narrative-integrated-boundary.log`, `/private/tmp/warline-placement-narrative-integrated-ecs-burst.log`, and `/private/tmp/warline-aph710-placement-command-cache-match-gc.log`
- Residual risk: transport helper construction, UI shell attribution, pathfinding, selection-panel allocations, and hidden world lookups remain
- Next ready task: remove the transport helper-construction owner or reconcile the UI shell attribution with its zero-byte runtime probe

### 2026-07-12 - APH-710 - Transport idle helper allocation

- Status: Stable partial slice; APH-710 remains active
- Baseline: `4d81bd9cc`
- Behavior preserved/changed: pre-resolved transport commands use the same helpers and routing; empty or unrelated command queues return before managed helper construction
- Measured result: the recurring 23,920-byte selection-helper constructor row and 16,744-byte pre-resolved routing row are absent; current Match GC is `122,248 / 1,024` bytes
- Validation: transport `79/79`, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, Runtime and Editor-test builds with zero errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_transport_idle_helper_allocation.md`, `/private/tmp/warline-aph710-transport-idle-allocation-focused.log`, `/private/tmp/warline-aph710-transport-idle-source-growth.log`, `/private/tmp/warline-aph710-transport-idle-boundary.log`, `/private/tmp/warline-aph710-transport-idle-ecs-burst.log`, and `/private/tmp/warline-aph710-transport-idle-match-gc.log`
- Residual risk: UI shell attribution, pathfinding, selection-panel allocations, and hidden world lookups remain
- Next ready task: reconcile the UI shell call-stack row against its zero-byte runtime probe before changing UI code

### 2026-07-12 - APH-710 - Path diagnostics allocation and UI attribution

- Status: Stable partial slice; APH-710 remains active
- Baseline: `17846ff929`
- Behavior preserved/changed: path scheduling and result application are unchanged; disabled move-command diagnostics now skip entity-description and interpolated-string construction at the call site
- Measured result: all path scheduler/result trace rows are absent and current Match GC is `72,944 / 1,024` bytes, down from `122,248`; the recurring UI `Update` row is classified as Unity Editor invocation/call-stack capture overhead after zero-byte runtime probes and three controlled captures
- Validation: pathfinding `3/3` with zero measured allocation, source growth `15/15`, assembly boundary `31/31`, ECS/Burst `10/10`, full Match capture with zero compiler errors, and `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_path_diagnostics_allocation.md`, `/private/tmp/warline-aph710-path-diagnostics-allocation-focused.log`, `/private/tmp/warline-aph710-path-diagnostics-match-gc.log`, `/private/tmp/warline-aph710-path-diagnostics-source-growth.log`, `/private/tmp/warline-aph710-path-diagnostics-boundary.log`, and `/private/tmp/warline-aph710-path-diagnostics-ecs-burst.log`
- Residual risk: one-frame building source-key normalization, intermittent selection read-model refreshes, diagnostics logging, header-model updates, and hidden world lookups remain
- Next ready task: remove allocation from building source-key matching without changing prefab identity semantics

### 2026-07-12 - APH-710 - Building source-key matching integration blocker

- Status: Implementation complete locally; integration blocked by unrelated current-main source-growth failure
- Baseline: `0e1d83735`
- Behavior preserved/changed: spawn-prefab source-key matching keeps exact trim, case-insensitive, and ` (Clone)` normalization semantics while using stack-backed fixed strings for normal-size keys; oversized exceptional names retain the managed fallback
- Measured result: all `BuildingSpawnPrefabSystem.NormalizeSourceKey` and `SourceKeysMatch` allocation rows are absent from the candidate Match capture
- Validation passed: building production `26/26`, assembly boundary `31/31`, ECS/Burst `10/10`, Game.Runtime generated-project build with zero errors, full Match capture with zero compiler errors, and `git diff --check`
- Blocker: `ProductionSourceGrowthArchitectureValidation` fails on current `main` because `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationSystemHelper.cs` is an unreviewed 548-line production file introduced by the separate audio slice. The shared worktree's commander-screen slice also grows `UIShellContentView.cs` from 955 to 985 lines and its generated prefab/scene YAML currently leaves `git diff --check` red. This performance slice owns none of those files.
- Next unblocking action: integrate the audio owner's source-size repair and the commander-screen owner's bounded responsibility extraction/source cleanup, confirm source growth and `git diff --check` pass without weakening budgets, rerun the building focused/integrated gates, then commit and push only the building source-key change plus accepted evidence

### 2026-07-12 - APH-710 - Match Intro explicit world binding

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: Match Intro input-lock and completion reads retain the same ECS state semantics, but the query now receives its world from Match composition lifecycle ownership instead of resolving `World.DefaultGameObjectInjectionWorld` during recurring reads
- Performance result: 300 warmed pairs of input-lock and completion reads allocate zero managed bytes
- Validation: Match Intro `4/4`, assembly boundary `31/31`, asynchronous Match smoke passed with `MatchHudReady`, Composition and Editor-test builds with zero errors, and task-owned `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_match_intro_world_binding.md`, `/private/tmp/warline-aph710-match-intro-world-binding-gc.log`, `/private/tmp/warline-aph710-match-intro-assembly-boundary.log`, and `/private/tmp/warline-aph710-match-intro-smoke-async.log`
- Integration boundary: the global source-growth and whole-worktree whitespace gates remain red only in the separate audio and Commander Profile ownership described above; no exception was added or widened for this slice
- Next ready task: bind `UnitPathfindingPendingStateReader` to the existing building and citizen composition entity-manager lifecycles and prove warmed reads allocate zero bytes

### 2026-07-12 - APH-710 - Pathfinding pending-state explicit binding

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: building and citizen population keep the same pending-path suppression behavior, but their shared reader now receives an explicit entity manager from existing composition boundaries and never falls back to the global world
- Performance result: 512 warmed pending-state reads allocate zero managed bytes
- Validation: reader `5/5`, pathfinding performance `3/3`, building resource production `36/36`, ECS/Burst `10/10`, assembly boundary `31/31`, asynchronous Match smoke passed, Runtime and Editor-test builds with zero errors, and task-owned `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_pathfinding_pending_state_binding.md`, `/private/tmp/warline-aph710-pathfinding-pending-reader-focused.log`, `/private/tmp/warline-aph710-pathfinding-pending-reader-pathfinding.log`, `/private/tmp/warline-aph710-pathfinding-pending-reader-building.log`, `/private/tmp/warline-aph710-pathfinding-pending-reader-ecs-burst.log`, `/private/tmp/warline-aph710-pathfinding-pending-reader-boundary.log`, and `/private/tmp/warline-aph710-pathfinding-pending-reader-smoke.log`
- Integration boundary: global source-growth and whole-worktree whitespace gates remain red only in separately owned audio and Commander Profile work; no architecture exception was added or widened
- Next ready task: remove the next smallest composition-ready hidden world lookup without changing presentation or startup behavior

### 2026-07-12 - APH-710 - Runtime-city readiness world/query cache

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: runtime-city startup retains grid, pending-initial-unit, and base-exclusion semantics; startup composition now supplies one world and readiness reuses three lifecycle-bound queries instead of resolving the global world and creating two config queries per check
- Performance result: 512 warmed pending-config checks allocate zero managed bytes
- Source ceilings: readiness shrank `145/5,542 -> 143/5,499`; runtime-city composition shrank `788/42,890 -> 787/42,890`; feature startup shrank `109/5,479 -> 101/5,421`
- Validation: readiness `6/6`, runtime-city generation `2/2`, initial-spawn cells `2/2`, assembly boundary `31/31`, ECS/Burst `10/10`, asynchronous Match smoke passed, Runtime and Composition builds with zero errors, and task-owned `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_runtime_city_readiness_binding.md` and `/private/tmp/warline-aph710-runtime-city-*-clone.log`
- Integration boundary: isolated-clone source growth reports only the pre-existing 548-line audio owner; the main project remains concurrently occupied by Commander Profile work and its whole-worktree gates are not attributed to this slice
- Next ready task: bind the runtime-city road-build fallback to the same explicit world/readiness lifecycle or continue the next disjoint hidden-world owner

### 2026-07-12 - APH-710 - Runtime-city road fallback binding

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: configured road-runtime cell size remains authoritative; the fallback now reads the already bound runtime-city readiness grid instead of resolving the default world and constructing a transient grid query
- Source ceilings: road bridge shrank `192/7,237 -> 183/6,941`; runtime-city composition now stands at `773/42,446`, below its `788/42,890` ceiling
- Validation: combined readiness/road fallback `8/8`, runtime-city generation `2/2`, initial-spawn cells `2/2`, assembly boundary `31/31`, ECS/Burst `10/10`, asynchronous Match smoke passed, and task-owned `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_runtime_city_readiness_binding.md`, `/private/tmp/warline-aph710-runtime-city-road-fallback-focused-main.log`, `/private/tmp/warline-aph710-runtime-city-road-generation-main.log`, `/private/tmp/warline-aph710-runtime-city-road-initial-spawn-main.log`, `/private/tmp/warline-aph710-runtime-city-road-source-growth-main.log`, `/private/tmp/warline-aph710-runtime-city-road-boundary-main.log`, `/private/tmp/warline-aph710-runtime-city-road-ecs-burst-main.log`, and `/private/tmp/warline-aph710-runtime-city-road-smoke-main.log`
- Integration boundary: main source growth lists only the separately owned audio and Commander UI files; this slice introduces no compiler error, source-growth violation, or whitespace error
- Next ready task: remove the next disjoint hidden-world owner, beginning with selection input state only after confirming its active ownership does not overlap Commander UI work

### 2026-07-12 - APH-710 - Selection building explicit world binding

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: selection building interaction keeps the same focus clearing, transport click, building move order, HUD feedback, and marker behavior; it now consumes the explicit selection camera-system world and fails safely when unbound instead of mutating whichever default world is active
- Architecture result: the reusable interaction owner and selection startup's recurring entity-manager closure no longer resolve `World.DefaultGameObjectInjectionWorld`
- Source ceilings: interaction shrank `187/7,339 -> 185/7,268`; selection startup remains `1,589` lines and shrank `84,092 -> 84,086` bytes
- Validation: selection input `62/62`, deterministic selection/move/attack PlayMode `1/1`, assembly boundary `31/31`, ECS/Burst `10/10`, asynchronous Match smoke passed, Runtime and Editor-test builds with zero errors, and task-owned `git diff --check`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_selection_building_world_binding.md`, `/private/tmp/warline-aph710-selection-building-world-focused.log`, `/private/tmp/warline-aph710-selection-building-world-playmode.xml`, `/private/tmp/warline-aph710-selection-building-world-source-growth.log`, `/private/tmp/warline-aph710-selection-building-world-boundary.log`, `/private/tmp/warline-aph710-selection-building-world-ecs-burst.log`, and `/private/tmp/warline-aph710-selection-building-world-smoke.log`
- Integration boundary: source growth lists only the separately owned audio and Commander UI files; whole-worktree whitespace remains Commander-prefab-owned
- Next ready task: rank another disjoint hidden-world owner while the six-file selection-input-state migration waits for Commander-owned `UIShellCurrentContentLoadTests.cs` to become available

### 2026-07-12 - APH-710 - Gameplay loading-gate explicit world binding

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: initial-spawn readiness and periodic loading diagnostics retain the same config, initialized, and progress query semantics; Match composition now binds their world once and teardown disposes the lifecycle-bound query set
- Architecture result: `GameplayRuntimeUpdateCompositionSystemHelper` contains no default-world lookup; an unrelated default world cannot complete an incomplete bound Match world
- Source ceilings: gameplay runtime update remains `469` lines and shrank `22,223 -> 22,217` bytes; Match bootstrap remains below its line ceiling and at its baseline `55,416` bytes
- Validation: focused world isolation `1/1`, existing gameplay runtime contract `1/1`, assembly boundary `31/31`, ECS/Burst `10/10`, Runtime/Composition/Editor-test builds with zero errors, and asynchronous Match smoke passed at `MatchHudReady`
- Evidence: `Design/AgentReports/2026-07-12_aph-710_gameplay_runtime_world_binding.md`, `/private/tmp/warline-aph710-gameplay-runtime-world-focused-execute.log`, `/private/tmp/warline-aph710-gameplay-runtime-contract.log`, `/private/tmp/warline-aph710-gameplay-runtime-source-growth.log`, `/private/tmp/warline-aph710-gameplay-runtime-boundary.log`, `/private/tmp/warline-aph710-gameplay-runtime-ecs-burst.log`, and `/private/tmp/warline-aph710-gameplay-runtime-smoke.log`
- Integration boundary: source growth lists only the separately owned audio and Commander UI files; the lifecycle PlayMode rerun was deferred when Commander UI acquired the main Unity lease after the passing Match smoke
- Next ready task: bind `RuntimeGameplayStateSystem` to explicit composition state, or resume selection input state after the Commander-owned test file becomes available

### 2026-07-12 - APH-710 - Runtime gameplay-state explicit binding

- Status: Stable partial slice; APH-710 remains active
- Baseline: `782eb2f4f`
- Behavior preserved/changed: all gameplay, camera-input, and initial-focus legacy/ECS mirror semantics remain; state access now uses an explicitly bound unmanaged world/entity handle instead of a static default-world cache
- Composition result: Match, selection, building, road, and runtime-city owners bind their copied facades at existing composition edges and unbind retained copies before disposal commands run
- Teardown correction: the first Match smoke exposed a disposed-world read from the HUD adapter; explicit adapter/domain unbinding removed it, and the final smoke passes with no `ObjectDisposedException`
- Source ceilings: Match bootstrap remains `1,163` lines and shrank `55,416 -> 55,404` bytes; selection startup remains at its `1,589/84,092` ceiling; UI runtime adapters shrink `789/32,622 -> 756/32,384`; runtime city remains below its `788/42,890` ceiling at `776/42,593`
- Validation: runtime state `10/10`, selection input `62/62`, road commands `11/11`, building composition smoke, runtime-city readiness `8/8`, assembly boundary `31/31`, ECS/Burst `10/10`, final asynchronous Match smoke passed at `MatchHudReady`, and Runtime/Composition/Editor-test builds have zero errors
- Evidence: `Design/AgentReports/2026-07-12_aph-710_runtime_gameplay_state_binding.md`, `/private/tmp/warline-aph710-runtime-state-focused-r2.log`, `/private/tmp/warline-aph710-runtime-state-selection.log`, `/private/tmp/warline-aph710-runtime-state-road.log`, `/private/tmp/warline-aph710-runtime-state-building.log`, `/private/tmp/warline-aph710-runtime-state-city.log`, `/private/tmp/warline-aph710-runtime-state-source-growth-r3.log`, `/private/tmp/warline-aph710-runtime-state-boundary-r2.log`, `/private/tmp/warline-aph710-runtime-state-ecs-burst-r2.log`, and `/private/tmp/warline-aph710-runtime-state-smoke-r3.log`
- Integration boundary: source growth lists only the separately owned audio and Commander UI files; task-owned whitespace is clean
- Next ready task: resume the remaining selection input-state hidden lookup after Commander-owned test-file work is available, then reconcile intermittent selection-panel allocation evidence

### 2026-07-12 - APH-803 - Android development runtime recorder

- Status: Stable partial slice; APH-803 remains active
- Behavior: the exact first Match HUD transition arms one flag-gated recorder delegated from the existing diagnostics update owner; normal builds allocate no frame-timing or sustained-sample buffers
- Capture contract: 60 seconds of active focused warmup, 600 seconds of structured per-frame samples, CPU/GPU timing availability, peak Unity/Mono memory, process-relative Match-ready time, and one completion-time JSON write under `Application.persistentDataPath`
- Fail-closed evidence: the Python gate rejects incomplete/timingless recorder output, tampered artifacts, and any nested sustained result that differs from submitted evidence
- Source ceilings: performance diagnostics shrank `909/39,101 -> 906/39,041`; Menu bootstrap remains at `917` lines and shrank `37,942 -> 37,916` bytes; the new plain recorder remains below production review size at `324/12,193`
- Validation: focused recorder `5/5`, existing diagnostics allocation `4/4`, assembly boundary `31/31`, Python evidence contracts `18/18`, Runtime/Composition/Editor-test builds with zero errors, and task-owned whitespace passed
- Integration boundary: source growth reports only the separately owned audio presentation and Commander UI files; an ECS/Burst rerun was serialized behind Commander UI after the relevant assembly and focused gates passed
- Evidence: `Design/AgentReports/2026-07-12_aph-803_android_runtime_recorder.md`, `/private/tmp/warline-aph803-recorder-focused-r2.log`, `/private/tmp/warline-aph803-performance-diagnostics-regression.log`, `/private/tmp/warline-aph803-assembly-boundary.log`, and `/private/tmp/warline-aph803-source-growth.log`
- Next ready task: collect repeated clean reference-device startup/sustained evidence, approve p99/startup p95 limits, and run the final fail-closed gate

### 2026-07-12 - APH-803 - Android reference-device diagnostic run

- Status: Diagnostic device integration complete; acceptance remains active
- Device/build: Xiaomi `24090RA29G` on Android 16; ARM64 IL2CPP development profiler APK from revision `8f75852919014c7dbc60a27646fbc690b281074c` plus a dirty worktree
- Runtime result: Match reached HUD ready in `19,005.75 ms`; the recorder completed a 60.02-second warmup and 600.01-second sustained capture with 27,788 frame samples and no recorder failure
- Metrics: average/p95/p99/max frame time `21.59/32.83/41.61/65.61 ms`; CPU/GPU p95 `31.83/25.59 ms`; peak Unity/Mono memory `1,084.15/60.38 MB`
- Integrity: process survived recorder completion and the final screenshot retained a coherent world, HUD, controls, and minimap
- Rejection: skin temperature reached `51.77 C` at thermal status `3`; peak Unity allocation exceeded the provisional `967.5 MB` limit; the build provenance was dirty; only one startup was captured; p99 and startup limits remain measurement-required
- Evidence: `Design/AgentReports/2026-07-12_aph-803_android_runtime_recorder.md` plus hashed local recorder JSON, final screenshot, thermal dump, and full device log under `/private/tmp/warline-aph803-*`
- Next ready task: cool the device, rebuild from an accepted clean revision, collect five cold and five warm starts, then repeat the 10-minute run and execute the fail-closed evidence gate

### 2026-07-12 - APH-803 - Offline CI evidence preflight

- Status: Stable non-device slice; APH-803 acceptance remains active
- Behavior: Jenkins now runs the APH-803 Python unit suite and generates a revision-bound collection contract immediately after checkout and before Unity resolution
- Fail-closed boundary: missing Python, missing/malformed profile or schema, nonzero test/gate exit, missing JSON, identity mismatch, or missing exact contract marker fails the stage; the contract and log are archived on every result
- Isolation: the wrapper contains no Unity or ADB invocation and does not attempt physical-device acceptance without exclusive USB ownership
- Validation: combined evidence and CI source-contract tests pass `25/25`; direct contract generation, JSON parsing, Python compilation, and scoped whitespace checks pass
- Environment limitation: local PowerShell/Groovy parser execution is unavailable; the source contract covers wrapper/stage wiring, and the first Windows Jenkins execution remains required operational evidence
- Files: `Jenkinsfile.groovy`, `Tools/CI/InvokeAndroidDevelopmentPerformanceContract.ps1`, and `Tools/CI/tests/test_android_development_performance_ci_contract.py`
- Next ready task: complete clean thermally controlled device sampling and approve startup/p99 thresholds without weakening the current frame or memory limits

### 2026-07-12 - APH-704 - Building-defense adoption rejection

- Status: Candidate rejected and removed; shadow index remains disabled
- Behavior proof: building-defense focused validation passed `18/18`, including deterministic ties, stale/overflow fallback, same-frame movement/health, and warmed zero-allocation coverage
- Performance rejection: 32-cell indexed build/query measured `2.161-2.793 ms`; 64-cell measured `1.260-1.571 ms`; 128-cell measured `1.258 ms`. All miss the fixed `<1.025 ms` building-acquisition gate despite relative improvements of `51.83-75.96%`.
- Decision: do not weaken the absolute threshold and do not pay every-frame shared-index construction for one consumer. The production patch, new acquisition file, expanded tests, and auto-creation change were removed.
- Preserved state: the existing APH-704 shadow `ISystem` remains `[DisableAutoCreation]`; no Match runtime behavior, scheduling, source-growth baseline, or package content changed
- Evidence: `/private/tmp/warline-aph704-building-defense-focused-r2.log`, `/private/tmp/warline-aph704-spatial-performance.log`, `/private/tmp/warline-aph704-spatial-performance-r2.log`, and `/private/tmp/warline-aph704-spatial-performance-r3.log`
- Next ready task: revisit only with a materially cheaper build strategy or multiple measured consumers that improve complete frame cost while preserving exact results

### 2026-07-13 - APH-704 - Compact linked-cell benchmark acceptance

- Status: complete for design and benchmark; runtime consumer adoption remains disabled
- Design: one `[DisableAutoCreation]` unmanaged `ISystem` clears 256 fixed bucket heads and inserts at most 2,048 compact 24-byte entries in one query using 128-unit cells; entries retain only entity, cell, source order, and next link
- Correctness: fixed edges, 100 randomized seeds, reversed chains, deterministic distance/source-order ranking, stale/overflow/grid mismatch rejection, direct fallback, capacity rejection, and refresh cadence pass `9/9`
- Performance: the post-rebase 740-unit/32-consumer complete direct/indexed acquisition measures `2.723/0.684 ms`; indexed improves `74.88%`, stays below the fixed `1.025 ms` ceiling, allocates zero warmed managed bytes, and uses `18,784` bytes versus the `256 KiB` ceiling
- Architecture: ECS/Burst `10/10`, naming/non-ECS `9/9`, assembly boundaries `31/31`, Runtime and Editor-test builds with zero errors, and scoped whitespace pass; no production scheduling or consumer behavior changed
- Evidence: `/private/tmp/warline-aph704-linked-equivalence-r2.log`, `/private/tmp/warline-aph704-linked-benchmark.{log,xml}`, `/private/tmp/warline-aph704-linked-ecs-burst.log`, `/private/tmp/warline-aph704-linked-non-ecs.log`, and `/private/tmp/warline-aph704-linked-boundary.log`
- Next action: open a separate adoption slice only for dependency-ready consumers, schedule index refresh before reads, retain all direct fallbacks, and remeasure complete Match frame cost before enabling auto creation

### 2026-07-13 - APH-502 / APH-710 - Complete texture export and source-growth reconciliation

- Status: stable local tooling and architecture slice; APH-502 remains active pending clean same-revision Android evidence, and global source-growth closeout remains active
- Commit or worktree baseline: `0587e3720`
- Stable commit/push: this integration commit, rebased and pushed to `main` after the recorded validation
- Files changed: Android BuildReport generator/tests and classifier contract; Resource Exchange physical-storage facade/query/mutation helpers; material-fabrication system/command helper; source-growth ledger; Match smoke diagnostics
- Behavior preserved/changed: production gameplay scheduling and outcomes are unchanged; BuildReport evidence additionally exports every reported texture path, and the classifier requires exact clean complete evidence before accepting final buckets
- Validation: Resource Exchange behavior `42/42`, source-policy `2/2`, material fabrication `17/17`, Android BuildReport `10/10`, Python classifier `4/4`, Runtime/Editor-test builds with zero errors, and Menu-to-Match smoke passed with HUD ready, intro complete, and input unlocked
- Artifacts: `/private/tmp/warline-resource-storage-focused.xml`, `/private/tmp/warline-source-growth-policy-current.xml`, `/private/tmp/warline-source-growth-integrated-current-r3.log`, `/private/tmp/warline-material-fabrication-focused.log`, `/private/tmp/warline-aph502-build-report-focused.log`, `/private/tmp/warline-resource-storage-match-smoke-diagnostic-r2.log`
- Metrics before: physical-storage facade `449 / 20,152`; material fabrication `552 / 24,838`; Android texture evidence limited by the top-100 asset table
- Metrics after: physical-storage facade/query/mutation `306 / 13,630`, `76 / 3,275`, and `104 / 4,588`; material fabrication/command utility `469 / 21,165` and `133 / 5,318`; complete texture table is deterministic and independent of top-100 ranking
- Visual result: Match route passed runtime smoke; no asset or visual behavior changed
- Residual risk: 28 pre-existing oversized production owners remain in the global source-growth report; APH-502 still needs clean current-revision APK/AAB BuildReport and residency evidence
- Next ready task: continue bounded oversized-owner decomposition while generating accepted APH-502 evidence at the next clean Android release-build gate

### 2026-07-12 - APH-710 - Post-merge source-growth reconciliation

- Status: Stable bounded/decomposition slice; global source-growth remains red only for the separately owned audio presentation file
- Exact bounded changes: tactical-materials authored config `761 / 57,493`, custom-game startup `964 / 44,871`, unit movement `824 / 37,358`, and the tactical-materials mutation utility `97 / 4,226`
- Decomposed owners: initial spawn `1,857 / 1,888` lines, map vehicle placement `575 / 576`, building resource hauling `1,553 / 1,634`, and shell content `951 / 955`
- New boundaries: 99-line faction tactical-materials startup projection, 264-line stateless vehicle route-clearance utility, and 112-line Commander route-lifecycle presentation; no `SystemBase`, ECS declaration, update callback, or player-loop owner was added
- Validation: initial spawn `12/12`, tactical materials `7/7`, map placement `5/5`, building resources `38/38`, shell content `14/14`, non-ECS architecture `9/9`, Unity import/compile with zero compiler errors, and scoped whitespace/naming checks pass
- Global result: source growth advances from eight committed violations to only `AudioPlaybackPresentationSystemHelper.cs`; user direction keeps that file untouched for its current owner
- ECS inventory result: all 197 declarations are registered and no `SystemBase` was added; the refreshed gate reports the next blocker as 26 runtime-loop registrations rather than missing ECS rows
- Evidence: `/private/tmp/warline-source-growth-integrated-extractions.log`, `/private/tmp/warline-initial-units-extraction-focused.log`, `/private/tmp/warline-tactical-materials-extraction-focused.log`, `/private/tmp/warline-route-clearance-map-focused.log`, `/private/tmp/warline-route-clearance-building-focused.log`, `/private/tmp/warline-commander-shell-extraction-focused.log`, `/private/tmp/warline-extractions-non-ecs-architecture.log`, and `/private/tmp/warline-non-ui-system-inventory-reconciled-r2.log`
- Commit state: not committed or pushed because the required global architecture gate is still red and the worktree contains unrelated active map/UI/runtime changes
- Next ready task: separately classify the Scenario Lab and resource-exchange loops while accepting the audio owner's completed repair when available; then rerun both global architecture gates before a scoped integration commit

### 2026-07-12 - APH-804 - Android release evidence contract

- Status: Stable non-device contract slice; runtime and device acceptance remain active
- Architecture: APH-803 and APH-804 use one strict Python validation core with task/build-specific wrappers and profiles; no duplicate recorder or device runner was added
- Release boundary: exact clean ARM64 IL2CPP release identity, tokenized launch arguments, requested/attested 60 FPS, `<33 ms` p95, APK size, sustained duration/sample count, process survival, and thermal validity are blocking
- Separation: `<25 ms` p95 is a non-blocking high-end observation; p99, startup, installed size, and absolute memory remain measurement-required rather than invented limits
- Validation: combined development/release/CI tests pass `41/41`; Python compilation, schema/profile parsing, byte-identical contract generation, and scoped whitespace checks pass
- Evidence: `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`
- Next ready task: implement explicit release recorder mode through the existing diagnostics/settings owners, then add real-artifact CI binding and clean-device collection

### 2026-07-12 - APH-804 - Android release recorder mode

- Status: Stable runtime slice; release APK/CI/device acceptance remains active
- Architecture: one `AndroidPerformanceRecorder` preserves the APH-803 filename/schema and adds APH-804 release output; no `MonoBehaviour`, ECS system, `SystemBase`, or second update owner was added
- Runtime settings: APH-804 token parsing applies an unsaved Mobile/60 override through `SettingsService`; normal and APH-803 settings are unchanged
- Measurements: preallocated frame/CPU/GPU buffers, one opt-in GC counter, forwarded render counters, Unity/Mono memory, one-second Android PSS, start/end battery, and GC collection counts populate the strict release shape
- Fail-closed behavior: capture rejects dirty development/script-debugging/profiler/marker provenance, missing timing/counters, unavailable/increasing battery, or unavailable PSS; profiler enablement is latched during capture
- Validation: recorder/settings/JSON/allocation `16/16`, diagnostics allocation `4/4`, naming/non-ECS architecture `9/9`, assembly boundary `31/31`, Python contracts `41/41`, Python compilation, and scoped whitespace checks pass with zero Unity compiler errors
- Global red gates outside this slice: current source growth reports only the separately owned audio presentation file; the complete ECS inventory is current, while its full gate reports 26 separately introduced runtime loops. Logs: `/private/tmp/warline-source-growth-integrated-extractions.log` and `/private/tmp/warline-non-ui-system-inventory-reconciled-r2.log`
- Evidence: `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`, `/private/tmp/warline-aph804-recorder-focused-final.log`, and `/private/tmp/warline-aph804-diagnostics-allocation.log`
- Next ready task: add real release-artifact orchestration and collect clean thermally controlled reference-device evidence

### 2026-07-12 - APH-804 - Release APK artifact contract binding

- Status: Stable CI/source-contract slice; Windows execution, package reduction, and device acceptance remain active
- Build provenance: Android release reports now require `DetailedBuildReport` and reject development, script-debugging, profiler-connect, and deep-profiling options
- CI binding: the APK build stage is followed by an equally gated APH-804 contract stage before deployment; it verifies exact clean revision, canonical report, release Android IL2CPP ARM64 identity, actual APK SHA-256/size, and the package ceiling
- Fail-closed output: the generated contract must retain `acceptanceReady=false` with release-recorder and validated-device requirements, match the actual revision/hash, emit the exact pass marker, and archive contract/log on every result
- Validation: build-report provenance `8/8`, combined release/development CI and gate tests `50/50`, Python compilation, direct contract generation, and scoped whitespace checks pass
- Environment limit: PowerShell is unavailable locally; the first direct wrapper execution remains on Windows Jenkins. The wrapper invokes neither Unity nor ADB.
- Current artifact blocker: the local dirty/stale APK is `552,481,264` bytes, `89,122,066` bytes above the fixed `463,359,198` limit, and cannot satisfy acceptance
- Files: `Assets/Game/Scripts/Editor/AndroidBuildReportGenerator.cs`, `Assets/Tests/Editor/AndroidBuildReportGeneratorTests.cs`, `Tools/CI/InvokeAndroidReleasePerformanceContract.ps1`, `Tools/CI/tests/test_android_release_performance_ci_contract.py`, and `Jenkinsfile.groovy`
- Next ready task: serialized ADB collection is completed by the following slice; reduce/package a clean release APK and collect thermally controlled device evidence

### 2026-07-12 - APH-804 - Serialized release-device collection

- Status: Stable tooling slice; physical-device acceptance remains active
- Preflight: the collector rejects mismatched serial/profile, non-canonical or dirty BuildReport, stale revision, APK path/hash/size mismatch, and package-budget failure before invoking ADB
- Device contract: one exact pinned target, hardware/OS/display identity, clean install, ARM64 ABI, non-debuggable package, device-side APK hash, and installed byte size are required
- Collection: exactly five cold starts clear package data, five warm starts retain it, and the sustained run enforces an unplugged battery, idle before/during/after thermal state, stable PID/foreground GameActivity, exact five-token Unity launch, one Match-ready marker, one successful recorder marker, and a 780-second upper bound
- Evidence: the external recorder is pulled verbatim; raw log, screenshot, recorder, APK, startup statistics, installed size, thermal snapshots, process survival, and exact revision are hashed/assembled and passed through the existing release gate
- Validation: focused collector `12/12`; combined development/release CI, gate, and collector tests `62/62`; Python compilation, syntax, and scoped whitespace checks pass. The current oversized APK fails before ADB with the exact package-ceiling reason.
- Package audit: the current APK is `84.99 MiB` over budget. Unchanged UI, 4K textures, animation textures, audio, and package manifests are not demonstrated causes; dirty combined static-map output is the leading candidate but requires a clean current-revision build and complete ZIP/included-asset comparison before attribution or deletion.
- Global integration state: source-growth reconciliation now leaves only the separately owned audio presentation violation; the complete ECS inventory proceeds to 26 runtime-loop registrations, so this slice is not committed or pushed from the shared dirty worktree
- Files: `Tools/CI/android_release_device_collection.py`, `Tools/CI/tests/test_android_release_device_collection.py`, `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`, `Design/Architecture/production_source_growth_baseline.md`, and this tracker
- Next ready task: generate a clean current-revision release APK using the shared-mesh map presentation path, then collect the unplugged reference-device run

### 2026-07-13 - APH-803 / APH-804 - Clean integration recovery and validation

- Status: stable implementation and CI slice; both tasks remain active for clean-device acceptance
- Integration: the previously reviewed recorder, settings override, evidence gates, Jenkins preflight/artifact binding, serialized device collector, and release build-option guard are now present together in one clean integration worktree rather than only in the shared dirty workspace
- Architecture: the existing diagnostics and menu composition owners delegate through narrow partial-class files; normal builds keep recorder buffers disabled, no `MonoBehaviour`, `SystemBase`, new ECS system, or second runtime loop was added, and FirstLaunch/audio owners remain untouched
- Source bounds: the existing diagnostics owner is `909 / 39,630` and the menu composition owner is `917 / 37,898`; Android-specific integration is isolated in `32 / 928` and `19 / 517` partial files
- Validation: recorder/settings/evidence `16/16`, build-report provenance `8/8`, diagnostics allocation `5/5`, naming/non-ECS architecture `9/9`, assembly boundaries `31/31`, Python CI/gate/device contracts `62/62`, Python compilation/JSON parsing, scoped naming/whitespace checks, and Runtime/Composition/Editor/Editor-test builds all pass with zero compiler errors
- Global gate boundary: production source growth still fails on four pre-existing resource/tactical `*SystemHelper` files that lack approved ledger entries; none of the APH-803/804 paths appear in that violation, and this slice does not weaken or edit the gate
- Evidence: `/private/tmp/warline-aph804-recorder-focused-persistent.log`, `/private/tmp/warline-aph804-build-report-focused.log`, `/private/tmp/warline-aph804-diagnostics-allocation.log`, `/private/tmp/warline-aph804-non-ecs-architecture.log`, `/private/tmp/warline-aph804-assembly-boundary.log`, `/private/tmp/warline-aph804-source-growth.log`, `Design/AgentReports/2026-07-12_aph-803_android_runtime_recorder.md`, and `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`
- Next ready task: produce a clean current-revision release APK below the accepted package ceiling, then run the unplugged reference-device collection and approve only measured startup/p99/installed/memory limits

### 2026-07-13 - Phase 7 - Campaign shell source-growth repair

- Status: stable bounded UI decomposition; global architecture closeout remains active
- Behavior: Skirmish, Campaign, and Mission Briefing retain the same region clearing, Commander-route exit, prefab installation, and Quick Custom binding order through one stateless `MenuOverlayRoutePresentation` helper
- Source bounds: `UIShellContentView.cs` shrinks from `983 / 42,625` to `946 / 41,020`, below its accepted `955 / 41,029` ceiling; the new helper is `58 / 2,058` and introduces no update loop, ECS system, or forbidden broad type suffix
- Validation: Skirmish `4/4`, Campaign `4/4`, Mission Briefing `4/4`, UI Runtime and Editor-test builds with zero errors, naming/static checks, and scoped whitespace pass
- Global gate boundary: the full source-growth run now stops first on four pre-existing resource/tactical helper paths without exact ledger entries; Campaign shell content is no longer a violation, and this slice does not change those helper or audio/building-defense owners
- Evidence: `/private/tmp/warline-ui-overlay-source-growth.log`, `/private/tmp/warline-ui-overlay-skirmish.log`, `/private/tmp/warline-ui-overlay-campaign.log`, and `/private/tmp/warline-ui-overlay-mission-briefing.log`
- Next ready task: reconcile the four exact helper ledger entries without weakening source ceilings, then review the separately owned audio/building-defense residuals

### 2026-07-13 - APH-710 / Phase 7 - Resource Exchange and runtime-loop reconciliation

- Status: stable non-audio architecture/performance slice; global commit remains blocked by the separately owned audio presentation boundary
- Resource Exchange performance: the popup no longer owns an `Update`. The existing shell presentation loop calls a version-only gateway read; full fixed-string conversion and UI application occur only when the projected version changes.
- ECS projection: an unmanaged 64-bit fingerprint skips unchanged recipe/card/detail/queue reconstruction and updates countdown/progress only when the displayed second or percentage changes. The warmed fingerprint path allocates zero managed bytes over 512 measured calls.
- Decomposition: the read-model `ISystem` is reduced from 724 to 580 lines by moving existing display formatting into an exact-bounded 158-line helper; the 151-line fingerprint helper and text helper add no ECS system or player-loop owner.
- Loop governance: 24 coroutine keys are accepted only for `BattleScenarioLabVisualPlayback`; Scenario Lab scenes are absent from enabled production build scenes. The Phase 7 gate now reports only `AudioPlaybackPresentationRuntimeView.Update`.
- Validation: Resource Exchange projection/allocation `5/5`, Resource Exchange architecture `7/7`, naming/non-ECS architecture `9/9`, assembly boundary `31/31`, and `Game.UI.Shell.Ecs`, `Game.UI.Runtime`, and `Game.Tests.Editor` builds pass with zero compiler errors; scoped whitespace and naming checks pass. Full source-growth and Phase 7 gates each advance to only the untouched audio owner.
- Evidence logs: `/private/tmp/warline-resource-exchange-projection-decomposed-r2.log`, `/private/tmp/warline-resource-exchange-architecture-focused.log`, `/private/tmp/warline-resource-exchange-naming-non-ecs.log`, `/private/tmp/warline-resource-exchange-assembly-boundary.log`, `/private/tmp/warline-source-growth-resource-exchange-reconciled-r2.log`, and `/private/tmp/warline-phase7-runtime-loop-reconciled.log`
- Integration boundary: no commit or push while the two required global gates remain red; `AudioPlaybackPresentationSystemHelper.cs` and `AudioPlaybackPresentationRuntimeView.cs` remain untouched per user direction
- Next unblocking action: review the audio owner's completed work when available, rerun source growth, Phase 7, naming, assembly, and project builds, then commit only the stable owned slice if all required gates are green

### 2026-07-13 - APH-710 - Selection input and panel allocation closeout

- Status: complete; post-remediation integration validation continues under APH-711
- Behavior: Match composition injects the camera-system world into selection input state, UI commands, rectangle-state reads, and focused-unit reads; disposed or unavailable injected worlds fail safely without falling back to the current default world
- Coverage: focused selection validation passes `64/64`, including alternate-world ownership and disposed-world no-fallback cases; selection summary passes `18/18`, including 512 warmed unchanged hidden, focused, and multi-selection panel updates at zero managed bytes; asynchronous Match smoke reaches `MatchHudReady`
- Architecture: broad naming `1/1`, non-ECS naming/conversion `9/9`, assembly boundary `31/31`, and ECS/Burst `10/10` pass; no `SystemBase`, player-loop owner, or forbidden broad name was added
- Source ceilings: input state `138 / 5,303`, input composition `929 / 37,870`, UI command `278 / 10,023`, selection startup `1,589 / 83,940`, UI runtime adapters `757 / 32,478`, and Match bootstrap `1,163 / 55,412`; source growth reports only the untouched audio presentation file
- Compiler health: Runtime, Composition, and Editor-test project builds pass with zero errors
- Profiler reconciliation: marker-enabled attribution is exactly `256` bytes/two samples per panel refresh; suppressing only the panel marker re-parents the same exact per-refresh signature to the outer selection marker while both runtime panel probes remain zero. The production marker is restored and retained.
- Evidence: `Design/AgentReports/2026-07-13_aph-710_selection_input_world_binding.md`, `/private/tmp/warline-selection-input-world-binding-{focused,naming,broad-naming,boundary,ecs-burst,source-growth,match-smoke}.log`, `/private/tmp/warline-selection-panel-zero-allocation-focused.log`, and `/private/tmp/warline-selection-panel-marker-{baseline,suppressed}.{log,md}`
- Commit state: not committed or pushed because the required global architecture gates remain red in the separately owned audio boundary and the shared worktree contains unrelated active changes
- Next ready task: complete the APH-711 integration rerun without touching the separately owned audio files

### 2026-07-13 - Final allocation-remediation integration rerun

- Status: complete; the allocation and Match performance evidence passes and the scoped implementation is integrated in commits `c47c73f62` and `86de3e054`
- Runtime changes: AI build selection directly scans read-only buffers with no temporary native copy or one-item job scheduling. Building runtime registration composes one cached context graph per call and keeps deferred side-effect state live through a delegate instead of recursively rebuilding nested source/context chains.
- Allocation proof: AI/building/GC focused validation passes `44/44`, the final allocation review passes `12/12`, and capture classification/retry coverage passes `41/41`. The unchanged 180-warmup/300-measured-frame steady-state capture passes at `930 / 1,024` player-relevant bytes; every production runtime probe is zero. The capture rejects and retries any sample containing building, production-transport, or drop-visual creation.
- Match proof: the latest canonical fixture passes over 766 frames at `5.23 ms` average, `8.16 ms` p95, `10.33 ms` p99, `25.79 ms` max, and zero measured allocation with 733 units, 628 buildings, 22 markers, and 46 visible models.
- Compile/reload proof: Unity records `906 ms` script compilation and `684 ms` scripting domain reload and completes the final capture with zero C# compiler errors. An isolated `Game.Runtime.csproj` no-incremental rebuild passes with zero errors. A concurrent Rider design-time MSBuild owner makes direct full editor-test project-graph output nondeterministic by deleting generated `Temp/Bin` references; the final focused Unity suites compile and pass inside Unity, so this is recorded as tooling contention rather than a source failure.
- Content/visual sidecars: texture classification, mip-streaming readiness, Android override, animation-texture residency, package usage, and visual-matrix tooling pass `68/68` Python tests. The streaming and visual gates remain fail-closed pending Unity/device artifacts.
- Architecture state: source growth passes `13/15` checks and reports only separately owned Campaign shell content plus two audio helpers. The complete Phase 7 inventory reports only the audio runtime view loop. No FirstLaunch or audio-owned file was changed by this slice.
- Evidence: `/private/tmp/warline-aph711-building-ai-focused-r16.xml`, `/private/tmp/warline-aph711-allocation-review-r22.xml`, `/private/tmp/warline-aph711-performance-r23.log`, `/private/tmp/warline-aph711-gc-classification-focused-r31.log`, `/private/tmp/warline-aph711-gc-steady-state-r32.log`, `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`, and `Design/AgentReports/performance_regression_match_baseline.md`
- Integration boundary: stage only allocation-remediation, deterministic audit, evidence, and tracker-owned files after branch reconciliation; preserve the active generated-map, Campaign UI, audio, and FirstLaunch work
- Commit state: clean integration branch `codex/aph711-allocation-closeout`; implementation commits `c47c73f62` and `86de3e054`
- Next ready task: acquire clean same-revision Android/Unity evidence for the bounded two-texture streaming pilot, then produce the required near/medium/far visual matrix before any importer mutation is accepted

### 2026-07-14 - APH-710 - Resource-hauler storage boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `f6e4a113d`
- Stable commit/push: `c0decb98f` pushed to `main`
- Files changed: `ResourceHaulerUtilitySystemHelper.cs`, new `ResourceHaulerStorageAccessSystemHelper.cs`, exact source authorization, and this tracker
- Behavior preserved/changed: resource-kind conversion, storage snapshots, ECS lookup/commit, metadata synchronization, and managed storage mirroring moved intact into one stateless helper; hauling commands, ordering, transfer semantics, scheduling, and public APIs are unchanged
- Validation: resource-hauler focused validation passed `25/25`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; full source growth no longer reports either owned resource-hauler file; clean-cache Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-resource-hauler-storage-focused.log`, `/private/tmp/warline-resource-hauler-source-policy.xml`, `/private/tmp/warline-resource-hauler-source-growth-full.log`, and `/private/tmp/warline-resource-hauler-match-smoke-r2.log`; the first smoke attempt's generated Burst compiler cache was quarantined after a native compiler crash and the clean-cache retry passed
- Metrics before: existing helper `567 / 21,898`; post-rebase global source-growth inventory `30` unrelated or oversized owners including this resource-hauler owner
- Metrics after: existing helper `490 / 18,528`; extracted helper `88 / 3,680`; post-rebase global inventory `29` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD
- Residual risk: 29 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: continue the next dependency-ready bounded decomposition after stable integration

### 2026-07-14 - APH-710 - Map-vehicle placement clearance boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `105f95fb1`
- Stable commit/push: `434bc7c44` pushed to `main`
- Files changed: `MapVehiclePlacementSpawnPrefabSystemHelper.cs`, new `MapVehiclePlacementClearanceSystemHelper.cs`, exact source authorization, and this tracker
- Behavior preserved/changed: footprint resolution, blocker clearing, cardinal departure-corridor search, and clearance progress accounting moved intact into one stateless helper; spawn state, scheduling, iteration order, source-key normalization, constants, blocker mutations, and test-facing facade signatures are unchanged
- Validation: placement/blocker behavior passed `5/5`; startup completion plus non-ECS architecture/naming passed `10/10`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-map-vehicle-clearance-focused.log`, `/private/tmp/warline-map-vehicle-source-policy.xml`, `/private/tmp/warline-map-vehicle-source-growth-full.log`, `/private/tmp/warline-map-vehicle-startup-architecture.xml`, `/private/tmp/warline-map-vehicle-match-smoke.log`, `/private/tmp/warline-map-vehicle-post-rebase-source-policy.xml`, and `/private/tmp/warline-map-vehicle-post-rebase-match-smoke.log`
- Metrics before: existing helper `687 / 27,430`; post-rebase global source-growth inventory `29` unrelated or oversized owners including this map-vehicle owner
- Metrics after: existing helper `499 / 20,090`; extracted helper `244 / 9,254`; post-rebase global inventory `28` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD
- Residual risk: 28 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: continue the next dependency-ready bounded decomposition after stable integration

### 2026-07-14 - APH-710 - Building runtime path-interaction context boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `d307e8af9`
- Stable commit/push: `cc00c2c47` pushed to `main`
- Files changed: `BuildingRuntimeContextFactoryCompositionSystemHelper.cs`, new `BuildingRuntimePathInteractionContextFactoryCompositionSystemHelper.cs`, exact source authorization, and this tracker
- Behavior preserved/changed: redirect, barrier, resource-hauler bridge, and selected-hauler context construction moved intact into one stateless helper; facade signatures, deferred delegate reads, barrier behavior, resource-hauler ordering, scheduling, and ECS ownership are unchanged
- Validation: context-allocation behavior passed `5/5`; resource-hauler storage/approach behavior passed `2/2`; barrier behavior passed `2/2`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Broader fixture note: `BuildingRuntimeValidationTests` passed `11/12`; the sole failure is an unchanged authored-content expectation that Faction 2 must contain `Tent_Regular`, outside this context-only slice
- Artifacts: `/private/tmp/warline-building-context-path-interaction-focused.xml`, `/private/tmp/warline-building-context-path-barrier-focused.log`, `/private/tmp/warline-building-context-path-naming.log`, `/private/tmp/warline-building-context-path-source-policy.xml`, `/private/tmp/warline-building-context-path-source-growth-full.log`, `/private/tmp/warline-building-context-path-runtime-focused.xml`, and `/private/tmp/warline-building-context-path-match-smoke.log`
- Metrics before: existing helper `557 / 35,856`; post-rebase global source-growth inventory `28` unrelated or oversized owners including this context-factory owner
- Metrics after: existing helper `499 / 32,545`; extracted helper `99 / 5,222`; post-rebase global inventory `27` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: 27 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: integrate the reviewed building-spawn or Build Drawer decomposition after stable commit

### 2026-07-14 - APH-710 - AI Resource Exchange startup projection reconciliation

- Status: stable upstream architecture reconciliation; global architecture closeout remains active
- Commit or worktree baseline: `8ff8eb52f`
- Stable commit/push: `ad8107181` pushed to `main`
- Files changed: `ResourceExchangeStartupProjectionSystemHelper.cs`, new `ResourceExchangeAIStartupProjectionSystemHelper.cs`, exact source authorizations, and this tracker
- Behavior preserved/changed: AI-faction discovery, scenario gating, existing-boundary disable/reset, and per-faction exchange projection moved intact behind the existing public startup facade; the new recovery component's AI startup initialization remains in `AIStartupSystem` under an exact reviewed ceiling
- Validation: Resource Exchange startup projection passed `12/12`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; full source growth no longer reports either Resource Exchange startup helper or `AIStartupSystem`; Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-resource-exchange-ai-startup-focused.xml`, `/private/tmp/warline-resource-exchange-ai-startup-naming.log`, `/private/tmp/warline-post-rebase-source-policy-final.xml`, `/private/tmp/warline-post-rebase-source-growth-final.log`, and `/private/tmp/warline-post-rebase-match-smoke-final.log`
- Metrics before: shared startup helper `463 / 22,950` with an exact-helper authorization failure; AI startup owner `554 / 25,422`; global oversized-source inventory `28` including the AI startup owner
- Metrics after: shared startup helper `294 / 14,551`; extracted AI helper `239 / 11,012`; AI startup owner frozen at `554 / 25,422`; global inventory returns to `27` unrelated owners
- Residual risk: 27 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: rerun exact policy and Menu-to-Match smoke on the reconciled head, then commit and push the stable slices

### 2026-07-14 - APH-710 - Building-definition footprint clone boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `e27bf8636`
- Stable commit/push: `6b1e42749` pushed to `main`
- Files changed: `BuildingRuntimeSpawnCompositionSystemHelper.cs`, new `BuildingDefinitionFootprintCloneSystemHelper.cs`, exact source authorization, and this tracker
- Behavior preserved/changed: null handling and exact `BuildingDefinition` field-copy order moved intact into one stateless helper while the public `CloneDefinitionWithFootprint` facade, one-definition allocation, and footprint override remain unchanged; no ECS operation, scheduling owner, or mutable state was added
- Validation: building cost/config/clone projection passed `3/3`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; pre-rebase and combined post-rebase Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-building-definition-footprint-clone-focused.xml`, `/private/tmp/warline-building-definition-clone-naming.log`, `/private/tmp/warline-building-definition-clone-post-rebase-source-policy-r2.xml`, `/private/tmp/warline-material-fabrication-telemetry-source-growth-full.log`, `/private/tmp/warline-building-definition-clone-match-smoke.log`, and `/private/tmp/warline-building-clone-fabrication-post-rebase-match-smoke.log`
- Metrics before: existing helper `531 / 26,444`; global source-growth inventory `27` including this runtime spawn owner
- Metrics after: existing helper `473 / 22,716`; extracted helper `70 / 4,084`; global inventory `26` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: 26 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: integrate the reviewed Build Drawer projection decomposition after stable commit

### 2026-07-14 - APH-710 - Fabrication telemetry architecture reconciliation

- Status: stable upstream architecture reconciliation; global architecture closeout remains active
- Commit or worktree baseline: `9fd5fa44c`
- Stable commit/push: `7a724318e` pushed to `main`
- Files changed: `MaterialFabricationSystem.cs`, new `MaterialFabricationTelemetrySystemHelper.cs`, exact source authorizations, and this tracker
- Behavior preserved/changed: active/blocked duration accumulation, saturating float counters, and telemetry-version mutation moved intact behind the existing public `MaterialFabricationSystem.AccumulateTelemetry` facade; spend-kind lifetime telemetry, construction-refund symmetry, and nonnegative validation remain in the atomic tactical-material mutation utility under an exact superseding ceiling
- Validation: fabrication behavior passed `19/19`; tactical-material and construction-resource behavior passed `17/17`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; combined Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-material-fabrication-telemetry-focused.xml`, `/private/tmp/warline-tactical-materials-telemetry-focused.xml`, `/private/tmp/warline-material-fabrication-telemetry-naming.log`, `/private/tmp/warline-material-fabrication-telemetry-source-policy.xml`, `/private/tmp/warline-material-fabrication-telemetry-source-growth-full.log`, and `/private/tmp/warline-building-clone-fabrication-post-rebase-match-smoke.log`
- Metrics before: fabrication system `572 / 25,833`; tactical-material utility `159 / 7,392` exceeded its stale exact ceiling; global inventory `27` including the fabrication system
- Metrics after: fabrication system `498 / 22,605`; extracted telemetry helper `96 / 4,027`; tactical-material utility frozen at `159 / 7,392`; global inventory `26` unrelated owners
- Residual risk: 26 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: commit and push the reconciled head, then integrate the reviewed Build Drawer projection decomposition

### 2026-07-14 - APH-710 - Build Drawer projection-formatting boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `2da2ef4d3`
- Stable commit/push: `80e6878f2` pushed to `main`
- Files changed: `UiBuildDrawerReadModelSystem.cs`, new `UiBuildDrawerProjectionSystemHelper.cs`, exact source authorization, and this tracker
- Behavior preserved/changed: localization, cost/time/footprint/requirement formatting, failure and ready copy, placement-status cleanup, and bounded fixed-string conversion moved intact into one stateless helper; catalog queries, static catalog/source ownership, request routing, cancellation ordering, sprite-key projection, and the existing unmanaged `ISystem` schedule remain unchanged
- Validation: owned Build Drawer read-model behavior passed `6/6`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Broader fixture note: the combined Build Drawer run passed `27/31`; four existing prefab/placement assertions remain outside this formatting-only slice, while every directly owned read-model test passed
- Artifacts: `/private/tmp/warline-build-drawer-projection-focused.xml`, `/private/tmp/warline-build-drawer-projection-source-growth-full.log`, `/private/tmp/warline-build-drawer-projection-match-smoke.log`, `/private/tmp/warline-build-drawer-projection-post-rebase-match-smoke.log`, and standalone exact-policy/naming validation output in the active task transcript
- Metrics before: read-model owner `610 / 30,956`; global source-growth inventory `26` including this owner
- Metrics after: read-model owner `436 / 20,844`; extracted helper `188 / 10,563`; global inventory `25` unrelated owners
- Residual risk: 25 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: commit/push this stable slice, then select the next dependency-ready oversized owner

### 2026-07-14 - APH-710 - Resource Exchange read-model projection boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `f27f2d143`
- Stable commit/push: `670f353de4` pushed to `main`
- Files changed: `UiResourceExchangeReadModelSystem.cs`, new `UiResourceExchangeProjectionSystemHelper.cs`, exact source authorization, and this tracker
- Behavior preserved/changed: route availability and confirm validation, amount normalization, output/duration calculation, queue-state mapping, presentation formatting, reason copy, and bounded fixed-string conversion moved intact into one stateless helper; player-faction query selection, physical-resource reads, change-driven fingerprinting, buffer writes, and the existing unmanaged `ISystem` schedule remain unchanged
- Validation: Resource Exchange read-model and architecture behavior passed `12/12`; post-rebase Resource Exchange plus fuel-logistics behavior passed `40/40`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; Runtime and Editor-test builds passed with zero errors; post-rebase Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-resource-exchange-projection-focused.xml`, `/private/tmp/warline-resource-exchange-projection-source-growth-full.log`, `/private/tmp/warline-resource-exchange-projection-match-smoke.log`, `/private/tmp/warline-resource-exchange-projection-post-rebase-focused.xml`, `/private/tmp/warline-resource-exchange-projection-post-rebase-source-growth.log`, `/private/tmp/warline-resource-exchange-projection-post-rebase-match-smoke.log`, and standalone exact-policy/naming validation output in the active task transcript
- Metrics before: read-model owner `775 / 34,599`; global source-growth inventory `25` including this owner
- Metrics after: read-model owner `480 / 21,952`; extracted helper `312 / 13,094`; global inventory `24` unrelated owners
- Residual risk: 24 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: commit/push this stable slice, then integrate the reviewed action-request decomposition

### 2026-07-14 - APH-710 - Fuel-logistics telemetry source authorization

- Status: stable upstream architecture reconciliation; global architecture closeout remains active
- Commit or worktree baseline: `b3c905e93`
- Stable commit/push: `41d821319` pushed to `main`
- Files changed: exact source authorization and this tracker; upstream feature source remains unchanged
- Behavior preserved/changed: no runtime behavior changed; the 71-line stateless telemetry utility keeps saturating route assignment, reassignment, failure, refinery/depot delivery, and version mutation outside ECS update owners under an exact non-growth ceiling
- Validation: Resource Hauler behavior passed `28/28` within the combined post-rebase `40/40` suite; exact source authorization passed `2/2`; naming passed `9/9`; Runtime and Editor-test builds passed with zero errors; full source inventory remains 24; Menu-to-Match reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-resource-exchange-projection-post-rebase-focused.xml`, `/private/tmp/warline-resource-exchange-projection-post-rebase-source-growth.log`, and `/private/tmp/warline-resource-exchange-projection-post-rebase-match-smoke.log`
- Residual risk: the enlarged resource-hauler bridge and initial-spawn owners remain inside the 24-file decomposition debt list; this authorization does not permit their growth
- Next ready task: push the reconciled head, then integrate the reviewed action-request decomposition

### 2026-07-14 - APH-710 - UI action request dispatch boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `2349a9595`
- Stable commit/push: `da7375f4a` pushed to `main`
- Files changed: `UiActionRequestSystem.cs`, new `UiActionRequestDispatchSystemHelper.cs`, new `UiActionResourceExchangeRequestSystemHelper.cs`, exact source authorizations, and this tracker
- Behavior preserved/changed: typed route, popup, build, placement, tactical-command, squad-tray, diagnostics, and drawer-audio dispatch moved intact into one stateless helper; selected Resource Exchange recipe lookup, stepped amount normalization, and confirmed start-request creation moved into a second stateless command helper; entity queries, action ordering, request-buffer ownership, and the existing unmanaged `ISystem` schedule remain unchanged
- Validation: focused UI action, Resource Exchange header, settings popup, and shell audio/popup behavior passed `26/26`; non-ECS architecture/naming passed `9/9`; exact source authorization passed `2/2`; UI Shell ECS and Editor-test builds passed with zero errors; Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-ui-action-focused.xml`, `/private/tmp/warline-ui-action-focused.log`, `/private/tmp/warline-ui-action-import.log`, `/private/tmp/warline-ui-action-match-smoke.log`, and standalone source-growth/naming validation output in the active task transcript
- Metrics before: UI action request owner `885 / 43,874`; global source-growth inventory `24` including this owner
- Metrics after: UI action request owner `300 / 15,134`; action-dispatch helper `487 / 24,721`; Resource Exchange command helper `113 / 4,101`; global inventory `23` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: 23 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: commit/push this tracker evidence, then select the next dependency-ready oversized owner

### 2026-07-14 - APH-710 - Match HUD minimap data-source boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `a7e867d02`
- Stable commits/push: `9fc2c75df` and `fe085f2d2` pushed to `main`
- Files changed: `UiRuntimeAdapters.cs`, new `MatchHudMinimapDataSourceAdapter.cs`, path-sensitive non-ECS architecture coverage, ARIA missing-button log expectation, and this tracker
- Behavior preserved/changed: minimap world bounds, marker collection, visibility, projection, relationship/type mapping, fallback position, and diagnostics moved byte-for-byte into a dedicated composition adapter; the eight remaining UI adapters, runtime queries, update ownership, and public interfaces are unchanged. The ARIA test now explicitly expects the intentional error emitted when its authored Match HUD button is absent; production behavior is unchanged
- Validation: minimap projection, marker, and architecture behavior passed `31/31`; rebased ARIA helper behavior passed `8/8`; exact source authorization passed `2/2`; Composition and Editor-test builds passed with zero errors; final Menu-to-Match smoke on the rebased head reached `MatchHudReady`, completed Match Intro, and unlocked input without a gameplay crash
- Artifacts: `/private/tmp/warline-ui-runtime-adapter-focused.xml`, `/private/tmp/warline-ui-runtime-adapter-post-rebase-assistant.xml`, `/private/tmp/warline-ui-runtime-adapter-post-rebase-source-policy.xml`, `/private/tmp/warline-ui-runtime-adapter-final-source-growth.xml`, and `/private/tmp/warline-ui-runtime-adapter-final-match-smoke.log`
- Metrics before: general UI runtime adapter owner `792 / 33,052`; global source-growth inventory `23` including this owner
- Metrics after: general adapter owner `353 / 14,956`; dedicated minimap adapter `454 / 18,395`; global inventory `22` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD
- Residual risk: 22 unrelated source-growth violations and the separately owned audio runtime loop remain red
- Next ready task: decompose the Build Drawer catalog runtime view into bounded presentation and production-queue responsibilities without adding a runtime loop

### 2026-07-14 - APH-710 - Build Drawer catalog-presentation and production-queue boundaries

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `cf03cad6c`
- Stable commits/push: `d2bdc5b75` and `095db4f6f` pushed to `main`
- Files changed: `BuildDrawerCatalogRuntimeView.cs`, new `BuildDrawerCatalogPresentationSystemHelper.cs`, new `BuildDrawerProductionQueueUiSystemHelper.cs`, localization migration coverage, exact source authorizations, refreshed APH-700 dependency reports, and this tracker
- Behavior preserved/changed: tab and catalog projection, pooled card presentation, selection/detail binding, localized instructions, command feedback, queue snapshot projection, pooled queue rows, cancel/clear ordering, and queue controls moved into two stateless managed helpers. The runtime view retains serialized ownership, lifecycle, public APIs, feedback policy, primary action routing, and its existing 0.2-second `Update`; no new runtime loop, coroutine, `MonoBehaviour`, `SystemBase`, or mutable static state was added
- Validation: combined Build Drawer, dual-cost read-model, and localization behavior passed `35/35`; APH-700 report parity passed `1/1`; `Game.UI.Runtime`, `Game.UI.Shell.Ecs`, `Game.Composition`, `Game.Editor`, `Game.Tests.Editor`, and `Game.Tests.PlayMode` built with zero compiler errors before the documentation-only final rebase, and `Game.Tests.Editor` passed again with zero compiler errors on commit `095db4f6f`; `git diff --check` passed; the code-equivalent Menu-to-Match smoke reached `MatchHudReady`, completed Match Intro, and unlocked input
- Artifacts: `/private/tmp/warline-build-drawer-final-focused.xml`, `/private/tmp/warline-build-drawer-final-aph700-test.xml`, `/private/tmp/warline-build-drawer-final-match-smoke.log`, and `/private/tmp/warline-build-drawer-push-aph700-generate.log`
- Metrics before: Build Drawer catalog runtime view `1,051 / 47,558`; global source-growth inventory `22` including this owner
- Metrics after: runtime view `477 / 21,440`; catalog-presentation helper `463 / 22,451`; production-queue UI helper `399 / 15,735`; global inventory `21` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: the global exact-source test remains blocked by separately owned FirstLaunch growth, 21 unrelated source-growth violations remain, and the separately owned audio runtime loop remains red
- Next ready task: select the next dependency-ready oversized production owner outside FirstLaunch and audio playback ownership

### 2026-07-14 - APH-710 - Building UI query read-model and production-entry boundaries

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `bc6b61630`
- Stable commits/push: `069ade3b9` and `0c3df66d2` pushed to `main`
- Files changed: `BuildingUiQueryUiSystemHelper.cs`, new `BuildingMaterialFabricationReadModelUiSystemHelper.cs`, new `BuildingProductionEntriesUiSystemHelper.cs`, exact source authorizations, refreshed APH-700 dependency reports, and this tracker
- Behavior preserved/changed: per-Match fabrication/storage/faction-material projection state moved into one facade-owned helper; produced-unit read-model projection, pruning, pending progress, and friendly queue enumeration moved into one stateless helper. Existing facade signatures, nested DTO identities, caller contracts, faction-resolution precedence, versioning, invalidation, append/clear behavior, temporary query behavior, and pull scheduling remain unchanged; no runtime loop, coroutine, `MonoBehaviour`, `SystemBase`, mutable static state, or assembly move was added
- Validation: building query, selection summary, and Build Drawer behavior passed `58/58`; APH-700 dependency generation passed and its full repository validation passed `7/7`; Runtime, UI Shell ECS, Composition, Editor-test, and PlayMode-test builds passed with zero compiler errors; `git diff --check` passed; Menu-to-Match reached `MatchHudReady`, completed Match Intro, and unlocked input without a gameplay crash
- Policy note: the owned helper authorization tuples are accepted; the global exact-source gate remains red only for separately owned FirstLaunch and material-scenario helper growth. The broader architecture run produced no finding against either new helper
- Artifacts: `/private/tmp/warline-building-ui-query-focused.xml`, `/private/tmp/warline-building-ui-query-source-policy.xml`, `/private/tmp/warline-building-ui-query-architecture.xml`, `/private/tmp/warline-building-ui-query-aph700-test.xml`, `/private/tmp/warline-building-ui-query-match-smoke.log`, and `/private/tmp/warline-building-ui-query-inventory.xml`
- Metrics before: building UI query owner `868 / 38,898`; global oversized-production inventory `21` including this owner
- Metrics after: facade `411 / 18,997`; material-fabrication read-model helper `230 / 10,323`; production-entry UI helper `351 / 13,949`; measured global inventory remains `21` because the upstream `MaterialsScenarioRecoveryValidationSystemHelper.cs` entered the gate
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: 21 unrelated source-growth violations, the upstream material-scenario helper authorization, separately owned FirstLaunch growth, and the separately owned audio runtime loop remain red
- Next ready task: decompose `ResourceExchangeQueueTickSystem.cs` into one bounded completion helper while preserving its single `ISystem` schedule and zero-allocation hot path

### 2026-07-14 - APH-710 - Resource Exchange queue-completion boundary

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `cadc9cd2f`
- Stable commits/push: `71e0b6a6d` and `3b9cf90a6` pushed to `main`
- Files changed: `ResourceExchangeQueueTickSystem.cs`, new `ResourceExchangeQueueCompletionSystemHelper.cs`, exact source authorization, refreshed APH-700 dependency reports, and this tracker
- Behavior preserved/changed: output-storage validation, physical reservation completion, output grant, completion and blocked results, economy events, delta flyouts, toasts, and ARIA announcements moved into one stateless helper. The original unmanaged `ISystem` retains query ownership, timer/state progression, ordering, summary projection, and all public `TryCompleteQueueItem` overloads as forwarding methods; no new schedule, runtime loop, coroutine, `MonoBehaviour`, `SystemBase`, or mutable static state was added
- Validation: canonical field-fabrication closeout passed `17/17` suites; owned queue behavior passed `6/6`, physical-storage behavior `7/7`, GC allocation `2/2`, and steady-state performance `1/1`; ECS/Burst, non-ECS ownership, and broad naming passed `20/20`; APH-700 tracked-report parity passed `1/1`; Resource Exchange PlayMode flow passed `1/1`; Runtime, UI Shell ECS, Composition, Editor, Editor-test, and PlayMode-test builds passed with zero compiler errors; `git diff --check` passed; Menu-to-Match reached `MatchHudReady`, completed Match Intro, and unlocked input without a gameplay crash
- Fixture note: a broad raw-NUnit feedback run passed `42/53`; its 11 request/toast/flyout/ARIA failures depend on unsupported randomized fixture order. The canonical fixed-order closeout runner passed all 17 suites, including Resource Exchange feedback and queue validation
- Policy note: the owned helper authorization tuple is accepted; the global exact-source gate remains red only for separately owned FirstLaunch and material-scenario helper growth
- Artifacts: `/private/tmp/warline-resource-exchange-queue-focused.xml`, `/private/tmp/warline-resource-exchange-queue-closeout.log`, `/private/tmp/warline-resource-exchange-queue-architecture.xml`, `/private/tmp/warline-resource-exchange-queue-aph700-test.xml`, `/private/tmp/warline-resource-exchange-queue-playmode.xml`, `/private/tmp/warline-resource-exchange-queue-match-smoke.log`, and `/private/tmp/warline-resource-exchange-queue-inventory.xml`
- Metrics before: queue tick owner `756 / 31,054`; global oversized-production inventory `21` including this owner
- Metrics after: queue tick owner `484 / 19,906`; queue-completion helper `415 / 16,853`; global inventory `20` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: 20 unrelated source-growth violations, the upstream material-scenario helper authorization, separately owned FirstLaunch growth, and the separately owned audio runtime loop remain red
- Next ready task: decompose the AI build-planning policy and materials-recovery mutation from `AIBuildPlannerSystem.cs` while preserving its single `ISystem` schedule, mutable request ID, request transaction, and Burst classification

### 2026-07-14 - APH-710 - AI build-planning policy, recovery, and read boundaries

- Status: stable bounded decomposition; global architecture closeout remains active
- Commit or worktree baseline: `d2866e7f8`
- Stable production commits: `421aea93a` and `be6d88418`
- Files changed: `AIBuildPlannerSystem.cs`, new `AIBuildPlanningPolicySystemHelper.cs`, new `AIMaterialsRecoveryNeedMutationSystemHelper.cs`, new `AIBuildPlanningReadSystemHelper.cs`, exact source authorizations, refreshed APH-700 dependency reports, and this tracker
- Behavior preserved/changed: deterministic build selection and placement policy, affordability result mapping, materials-recovery publication/clear semantics, economy snapshot projection, faction/building lookup, AI-control resolution, and spawn-result labeling moved into three stateless helpers. The original unmanaged `ISystem` retains its one schedule, query fields and handle updates, request transaction and settlement, mutable request ID, diagnostics, public forwarding surface, nested decision contracts, and `[BurstCompile]` decision entry point; no new query schedule, runtime loop, coroutine, `MonoBehaviour`, `SystemBase`, or mutable static state was added
- Validation: focused AI build-planning behavior passed `7/7`; canonical field-fabrication closeout passed `17/17` suites; non-ECS architecture/naming passed `9/9`; ECS/Burst architecture passed `10/10`; post-rebase APH-700 dependency generation and tracked-report parity passed `1/1`; Runtime and Editor-test builds passed with zero compiler errors, including the combined post-rebase head; `git diff --check` passed; the warm-state post-rebase Menu-to-Match repeat reached `MatchHudReady`, completed Match Intro, and unlocked input without a gameplay crash
- Validation note: the first post-rebase smoke ran alongside the incoming FirstLaunch asset import and timed out in `EnteringMatchHud` with the Match scene and HUD already loaded; the immediate warm-state repeat on identical code passed. Both logs contain one Unity Editor QuickSearch startup-index exception from `UnityEditor.Search.SearchDatabase`; it is outside production game code and the passing repeat ended in the required input-ready Match state
- Artifacts: `/private/tmp/warline-aibuildplanner-read-focused.log`, `/private/tmp/warline-aibuildplanner-field-fabrication.log`, `/private/tmp/warline-aibuildplanner-nonecs.log`, `/private/tmp/warline-aibuildplanner-burst.log`, `/private/tmp/warline-aibuildplanner-postrebase-aph700-generate.log`, `/private/tmp/warline-aibuildplanner-postrebase-aph700-parity.xml`, `/private/tmp/warline-aibuildplanner-postrebase-match-smoke.log`, `/private/tmp/warline-aibuildplanner-postrebase-match-smoke-r2.log`, `/private/tmp/warline-aibuildplanner-helper-policy.xml`, and `/private/tmp/warline-aibuildplanner-owner-policy.xml`
- Metrics before: AI build planner owner `863 / 38,180`; global oversized-production inventory `20` including this owner
- Metrics after: AI build planner owner `496 / 23,437`; planning-policy helper `257 / 9,792`; recovery-mutation helper `79 / 2,988`; planning-read helper `147 / 5,711`; global inventory `19` unrelated owners
- Visual result: Passed; automated live Menu-to-Match inspection reached the visible, input-ready Match HUD without a gameplay crash
- Residual risk: 19 unrelated source-growth violations, the upstream material-scenario helper authorization, separately owned FirstLaunch growth, and the separately owned audio runtime loop remain red
- Next ready task: select the next dependency-ready oversized production owner outside FirstLaunch and audio playback ownership while the final Android evidence lane proceeds from a clean stable revision

### 2026-07-14 - APH-408 - Android audible-smoke acceptance

- Status: accepted from explicit user approval after the objective Android audio smoke and focused validation passed
- Result: config/importer `14/14`, performance `4/4`, and listener/scene `6/6` passed; playback produced non-zero amplitude, survived 90 seconds, emitted no listener/clip/crash/ANR error, and added zero mixer underruns or delayed writes
- Production code: unchanged by this acceptance update
- Residual risk: remaining audio architecture work is tracked separately and is not closed by audible-smoke acceptance

### 2026-07-14 - Editor FPS rendering-configuration regression closeout

- Status: stable regression repair integrated in `c47c4e47b`; no checklist count changes because the repair restores accepted APH-608 behavior
- Root causes: Standalone Editor quality fallback selected desktop `Ultra` when the authored `Mobile` tier was excluded; BatchRendererGroup shader stripping prevented GPU Resident Drawer activation; URP runtime settings omitted the GPU Resident Drawer resource; idle missile audio still entered its update path without projectiles
- Repair: semantic quality-tier fallback now preserves mobile intent across platform tier lists, BatchRendererGroup variants use `KeepAll`, the URP runtime resource registry retains `GPUResidentDrawerResources`, and `MissileFlightAudioSystem` requires a matching projectile query before updating
- Validation: `AndroidVisualQualityValidationTests.RunFocusedValidation` passes `15/15`; `CombatMotionAudioSystemTests.RunFocusedValidation` passes `4/4`; `Game.Runtime` and `Game.Tests.Editor` build with zero compiler errors; `git diff --check` passes
- Evidence: `/private/tmp/warline-fps-graphics-focused.log` and `/private/tmp/warline-fps-audio-focused.log`
- Residual risk: final Editor FPS comparison requires a full Editor restart after importing `c47c4e47b`, identical Game-view resolution, and idle indexing/security processes; the previously captured compact-minimap refresh remains the next measured CPU target only if the regression persists

### 2026-07-15 - APH-901 - Local integrated closeout candidate

- Status: Accepted after strict fast-forward integration into `origin/main` at `8caf7b00c`; implementation commit `98cb2bb6d4321132574ebc5247aa3f2c93359eac`
- Allocation repair: lifecycle-owned placement delegates and tactical queries, extracted Match HUD resource formatting/read models, cached FPS strings, pre-sized production ID lookup, allocation-free canonical spawnable-key normalization, and world-aware faction-storage and resource-hauler move-order EntityQuery ownership
- Architecture: `UnitMoveOrderRequestSystem` remains an unmanaged `ISystem` and is held at exactly 500 lines; no `SystemBase`, `*Controller`, `*Player`, second player-loop owner, mutable static gameplay registry, or map-content mutation was added
- Validation: source growth `15/15`; integrated architecture closeout `23/23`; move order `18/18`; affected allocation/resource/UI fixtures pass; five critical PlayMode flows pass `5/5`; full static-map structural validation passes `2/2`; all 12 first-party projects build no-incremental with `0` errors; CI Python contracts pass `218/218`
- Performance: 733 units, 628 buildings, 557 frames, `7.20 ms` average, `11.48 ms` p95, `13.16 ms` p99, `34.98 ms` maximum, and zero measured allocation
- GC: authoritative Menu-to-Match capture passes at `292 / 1,024` player-relevant bytes over 300 measured frames after 180 warmup frames; recurring production-ID dictionary initialization, resource-storage query creation, and hauler move-order queue query creation are absent
- Non-owned test debt: three full-fixture assertions outside the changed paths remain reproducible when run individually: two Building gameplay initialization assumptions and one managed production-tick exact-float assumption. Their production owners and tests were not changed by this slice; the tracker-required closeout and affected paths are green
- Preserved exclusions: the combined closeout runner invokes a FirstLaunch validation that regenerates two portrait `.meta` files, the presentation prefab, and `Menu.unity`; those four validation side effects were restored to branch `HEAD` immediately and are absent from the architecture commit. No operation-map asset, scene, generator, manifest, or design document was edited.
- Evidence: `/private/tmp/warline-aph-query-cache-source-growth-final.log`, `/private/tmp/warline-aph-query-cache-build-final.log`, `/private/tmp/warline-aph-unit-move-focused.xml`, `/private/tmp/warline-aph901-closeout-after-gc.log`, `/private/tmp/warline-aph901-performance-after-query-cache.log`, `/private/tmp/warline-aph901-gc-steady-after-query-cache-r2.log`, `/private/tmp/warline-aph901-static-map-structural-final.xml`, `/private/tmp/warline-aph8*-final.xml`, `/private/tmp/warline-aph901-build-*.log`, `/private/tmp/warline-aph901-python-contracts-final.log`, `/private/tmp/warline-aph901-editor-build-post-rebase.log`, `/private/tmp/warline-aph901-source-growth-post-rebase.log`, `Design/AgentReports/performance_regression_match_baseline.md`, and `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`
- Next action: connect the Android reference device and collect the clean same-artifact development, release, thermal, memory, startup, and visual evidence

### 2026-07-15 - APH-803 / APH-804 / APH-809 - Device and visual preflight

- Status: the operation-map manifest blocker is resolved; formal Android development/release evidence and the visual matrix remain open; no architecture-owned map production file changed
- Device: Xiaomi `24090RA29G` (`R4M7PZEQZ58T59ZH`) passed identity, installation, cold-launch, Menu/Campaign/Match routing, 60-second survival, crash/ANR, and thermal-status-`0` smoke checks before disconnecting from ADB prior to formal collection
- Android builds: clean revision `792836a043f8b30bf83f4f036b25f02ffd4d8e4d` produced a release APK of `442,922,672` bytes with SHA-256 `ec23ddb8a2b9d5e93e2c0b84878db144cc2706bc4dca89fed2dabdd87fd8e20f`; clean revision `183c7d2ad9b999df3738852987856502d28184e6` produced a profiler APK of `478,226,023` bytes with SHA-256 `ed0b29b323a9db7db155962090c978fcfeff648005d16ec293e7fd555120588b`
- Collector preflight: the connected device reported `Mediatek MT6878` while the canonical profile records `MediaTek MT6878`; shared development/release identity validation now accepts only casing/whitespace-equivalent SoC text, emits the canonical profile value for downstream exact evidence binding, and preserves exact checks for serial and every other field. The combined development/release collector matrix passes `23/23`.
- Install preflight: Homebrew ADB `37.0.0` and Unity ADB `36.0.0` can return exit code `0` with empty install stdout or the standard two-line `Performing Push Install` / `Success` acknowledgment on the pinned Xiaomi. Install acknowledgment permits only empty output, exact `Success`, or that exact two-line response after a successful process exit; uninstall remains exact-`Success`, and collection cannot proceed until component, ABI, debuggable flag, `run-as`, and device-side SHA-256 all verify. The combined collector matrix passes `24/24`.
- Launcher preflight: Unity ADB `36.0.0` returns the canonical `cmd package resolve-activity --brief` summary row followed by the exact GameActivity component on the pinned Xiaomi. The shared development/release parser now accepts only the component row alone or one strictly shaped summary row followed by that component, rejects missing/malformed/extra rows, and leaves the exact expected-component comparison unchanged. The combined collector matrix passes `25/25`.
- Launch-extra preflight: ADB remote-shell argument reconstruction split the space-delimited Unity launch extra, causing its second `-warline...` token to be parsed as an Activity Manager option. Both collectors now shell-quote the complete token string as one `--es unity` value; direct pinned-device probes retain `am start -W -S`, return exact `Status: ok`, and preserve the component and all required gate tokens. The combined collector matrix remains `25/25`.
- Exact development evidence: clean commit `8568654b249bde2dc12fadb88468d2abcc38adf5` and profiler APK SHA-256 `541b03b1ebc5c165b93e86266b09115e05df7cf7c8c75725a5460c2148ed8869` completed five cold starts, five warm starts, `60.02 s` warmup, `600.00 s` sustained capture, process/crash integrity, and thermal status/cooling value `0`. Average/p95/p99/max frame time is `22.68/32.78/33.10/65.52 ms`; CPU/GPU p95 is `30.15/25.11 ms`; cold/warm startup p95 is `24,758.32/24,861.44 ms`; peak allocated/Mono memory is `1,085.77/65.26 MB`.
- Approved development ceilings: explicit user approval freezes p99 at `<=50 ms` and startup p95 at `<=30,000 ms`, retaining headroom over both the clean run and prior diagnostic evidence without weakening the existing `<33 ms` p95, `967.5 MB` allocated-memory, or zero-thermal limits. The exact run passes frame, startup, process, crash, and thermal requirements but remains red on peak allocated memory by `118.27 MB`.
- Visual preflight: the 16:9 current-profile session emitted all 13 expected artifacts, but final acceptance is rejected because night readability is too dark and the near static-map capture has black/missing surface regions
- Preserved evidence: visual artifacts are staged outside Git at `/private/tmp/warline-aph809-current-16x9-staging`; accepted build logs are `/private/tmp/warline-aph803-profiler-apk-792836a04.log` and `/private/tmp/warline-aph804-release-apk-792836a04-clean-provenance.log`; the canonical release report is `/private/tmp/warline-aph804-release-apk-report-792836a04.json`; the clean manual Match smoke is `/private/tmp/aph803-device-user-entered-match.png` plus `/private/tmp/aph803-match-check.mp4`
- Ownership boundary: no operation-map or First Launch file was edited by this architecture slice; the manifest refresh is consumed as landed external work
- Next action: rebuild the profiler artifact from the SoC-normalization revision, leave the pinned phone foreground/untouched for the automatic APH-803 collector, then collect APH-804 and recapture all four APH-809 visual sessions from one admissible revision

### 2026-07-15 - APH-509 - Explicit package static probes

- Status: stable non-removal slice; APH-509 remains active because isolated Unity/build/device removal proof has not run
- Result: five hard-coded unknowns were replaced by deterministic source, editor-workflow, external-configuration, and built-in YAML/class-signature probes
- Classification: Cloth and Wind are static-only candidates; Umbra has first-party occlusion evidence; Rider and Visual Studio remain fail-closed because their ignored external configuration cannot be proven from tracked files
- Safety: all 15 candidates retain `packageRemovalAuthorized=false` and the complete import/compile/test/build/device blocker set; no package manifest, lockfile, map, First Launch, runtime, scene, or prefab file changed
- Validation: focused inventory `12/12`; report regeneration and `--check` pass with `68` total, `47` manifest, `15` candidates, and `2` blind spots; `git diff --check` passes
- Evidence: `/private/tmp/aph509-focused-tests-r3.log`, `/private/tmp/aph509-write-report-r2.log`, `/private/tmp/aph509-report-check-r2.log`, `Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md`, and `.json`

### 2026-07-15 - APH-501 / APH-510 - BrightCommand Android texture pilot

- Status: stable implementation pilot; package, device-memory, and visual acceptance remain open
- Scope: exactly 60 PNG sprite importers directly under `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites`; no Match HUD, First Launch, source/reference, animation, audio, scene, prefab, or operation-map asset is included
- Policy: explicit Android ASTC 4x4, 2048 maximum size, high-quality compression, quality `100`, and no mip chain for this screen-space UI set; existing sprite borders, pivots, PPU, readability, alpha, wrap/filter modes, GUIDs, and every non-Android platform override remain unchanged
- Reproducibility: `Aph501AndroidMainMenuTexturePolicy` enforces the exact folder on import and exposes one deterministic apply command; unrelated paths fail outside the exact-folder classifier
- Validation: editor and editor-test assemblies compile with zero errors; focused Unity policy coverage passes `12/12`; all 60 importer diffs modify only the Android platform format, compression, quality, and override fields; `git diff --check` passes
- Evidence: `/private/tmp/warline-aph501-main-menu-policy-apply.log` and `/private/tmp/warline-aph501-main-menu-policy-tests-r2.xml`
- Next action: build one clean release APK from the committed pilot revision, compare the canonical BuildReport and artifact bytes, then run Android visual and memory acceptance before closing the pilot
- Measured package result: exact clean commit `17566e35605b3925c96df5d86f173f382251f12a` produced a `505,130,133`-byte APK with SHA-256 `7cf40efabd5e2c31298762745f4b68ad235a1b3f661065845a9c4d9487f26a0a`; this is `6,951,948` bytes smaller than the clean pre-pilot artifact but remains `41,770,935` bytes over the accepted ceiling
- BuildReport delta: the BrightCommand rows present in the top-100 table fall from 41 / `179,595,448` bytes to 31 / `157,452,404` bytes; serialized contribution and compressed APK bytes are intentionally reported separately
- Acceptance: package gate remains red; retain the pilot only as an active candidate until device-memory and visual evidence determine whether the measured residency benefit is worth keeping
- Updated next action: attribute and remove the remaining package overage without changing operation-map or offline FirstLaunch behavior, then rebuild one exact artifact for package, memory, and visual acceptance

### 2026-07-15 - APH-501 / APH-510 - Supporting UI Android texture pilot

- Status: implementation in progress; exact package and visual acceptance pending
- Basis: the exact mipless BrightCommand artifact is `470,365,902` bytes, leaving a `7,006,704`-byte package gap; independent ZIP/BuildReport attribution identifies 62 included Match HUD and four included V15C textures as the smallest non-map group with sufficient modeled margin
- Scope: all 143 sprite sources under `Assets/Game/Art/UI/Generated/MatchHUD` and `Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo`; First Launch, BrightCommand, reference/source art, animation textures, scenes, prefabs, audio, and operation-map content remain excluded
- Policy: mipless Android ASTC 6x6, 2048 maximum size, high-quality compression, and quality `100`; sprite dimensions, borders, pivots, PPU, readability, alpha, GUIDs, and every non-Android platform override remain unchanged
- Validation: exact-root classification and importer policy pass `23/23`; Game.Configs, Game.Editor, and Game.Tests.Editor compile with zero errors; all 143 importer diffs are confined to mip state and Android platform settings; `git diff --check` passes
- Evidence: `/private/tmp/warline-aph501-supporting-ui-policy-apply.log` and `/private/tmp/warline-aph501-ui-policies-tests.xml`
- Required acceptance: deterministic importer tests, clean APK `<=463,359,198`, exact BuildReport comparison, release-device Menu/Match visual matrix, and allocated-memory evidence
- Exact package result: clean commit `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` produces `449,902,496` bytes with SHA-256 `b4ce2922a33208bb348912b08160cf9aa5078144797b81b9b9b8ad31c7268eaf`; package gate passes with `13,456,702` bytes of headroom
- Acceptance state: importer and package gates pass; release-device visual, allocated-memory, installed-size, and thermal evidence remain required before the policy or APH-501/510 can close


### 2026-07-16 - APH-804 - Idempotent release installation preflight

- Status: stable collector repair; release-device acceptance remains active
- Device finding: after the user canceled Android's install confirmation, no package remained; a retry failed before installation because unconditional `adb uninstall` returned `DELETE_FAILED_INTERNAL_ERROR` for the absent package
- Result: the release collector now performs one exact user-0 package query, accepts only zero rows or the one expected package row, skips uninstall when absent, and still requires exact `Success` when the expected package is present
- Safety: ambiguous package output, a failed uninstall of an installed package, artifact/profile/hash/revision mismatch, and every existing install verification remain fail-closed
- Validation: release, development-regression, and CI contract coverage pass `37/37`; Python bytecode compilation passes with an isolated cache; `git diff --check` passes
- Next action: approve the device install prompt on retry, then leave the reference device untouched through the formal APH-804 capture

## Decision Log

Append approved deviations here. Do not silently alter Fixed Architecture Decisions.

| Date | Decision ID | Decision | Reason | Evidence/approval |
|---|---|---|---|---|
| 2026-07-09 | `D-001` through `D-011` | Initial implementation decisions accepted as the audit baseline | Convert the audit into deterministic implementation work | This tracker |
| 2026-07-09 | `D-012` | Reconcile execution to `a6af1335c` while retaining historical audit evidence | The tracker entered `main` before the ARIA and FPS commits were integrated | Commit history and current source inspection |
| 2026-07-09 | `D-013` through `D-016` | Adopt coordinator-owned tracker integration, disjoint worker ownership, an exclusive Unity lease, and Phase 0 parallel intake | Multiple agents can reduce elapsed time only when shared-state and Unity collisions are controlled | User approval and Multi-Agent Operating Model |
| 2026-07-09 | `D-017` | Commit and push each accepted implementation slice only after the stable integration gate passes | Keep `main` usable throughout the program and prevent broken or unrelated work from entering task commits | User direction and Stable Integration and Push Gate |
| 2026-07-09 | `D-018` | Accept `APH-007` as a reproducible baseline while keeping its 1,024-byte GC gate red and unchanged | Baseline capture must freeze measured debt before optimization; requiring a green result would optimize before establishing the baseline | Raw-sample metadata report, 16 focused tests, and coordinator review |
| 2026-07-10 | `D-019` | Reject direct 96 m and 32 m transient Match mesh-combine output; retain the canonical scene and require shared-mesh/overlay-safe presentation chunks plus measured GPU culling work for Phase 6 | Direct combination solved preload but duplicated roughly 9.5-9.8M vertices, increased generated payload, removed possible overlay source filters, and remained GPU-bound at 41-50 FPS | APH-311/APH-601 Android reports, live 32 m diagnostics, and two independent architecture handoffs |
| 2026-07-10 | `D-020` | Freeze 265 helper paths, review-baseline 108 production files above 500 lines, and apply strict ceilings to 27 files above 1,000 lines at baseline `9280ead85` | New helper/large-file debt must fail by exact path while deletion and shrinkage remain encouraged | APH-701/702 baseline manifest and focused `5/5` validation |
| 2026-07-10 | `D-021` | Compare static-map serialized transforms at `0.0005` and derived world bounds at `0.005`, while retaining exact source/asset/layer/lightmap/shadow/probe/component identity | Unity YAML round-trips change floating values below visible precision; bit equality produced 26,010 false positives without indicating visual or structural drift | APH-605 full parity run and retained exact structural contracts |
| 2026-07-10 | `D-022` | Permit bounded APH-607 helper growth in the persistent Menu composition owner | The existing owner must gate Match-ready and Match unload without adding another player-loop owner or `SystemBase` | `source-growth-exception(path=Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=932;maxBytes=38015;task=APH-607)` |
| 2026-07-10 | `D-023` | Permit bounded APH-607 reviewed-file growth in the persistent Menu composition owner | The lifecycle integration remains in the existing reviewed composition boundary while the new streamer types stay below 500 lines | `source-growth-exception(path=Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs;scope=production-over-500-review;maxLines=932;maxBytes=38015;task=APH-607)` |
| 2026-07-12 | `D-024` | Register the renamed FirstLaunch audio presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeAudioPresentationSystemHelper.cs;scope=system-helper;maxLines=76;maxBytes=3040;task=APH-712)` |
| 2026-07-12 | `D-025` | Register the renamed FirstLaunch composition boundary | The naming contract requires a managed composition reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeCompositionSystemHelper.cs;scope=system-helper;maxLines=453;maxBytes=19785;task=APH-712)` |
| 2026-07-12 | `D-026` | Register the renamed FirstLaunch model utility boundary | The naming contract requires a pure utility reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeModelUtilitySystemHelper.cs;scope=system-helper;maxLines=63;maxBytes=2833;task=APH-712)` |
| 2026-07-12 | `D-027` | Register the renamed FirstLaunch review utility boundary | The naming contract requires a managed utility reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeReviewUtilitySystemHelper.cs;scope=system-helper;maxLines=28;maxBytes=653;task=APH-712)` |
| 2026-07-12 | `D-028` | Register the renamed FirstLaunch sequence presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs;scope=system-helper;maxLines=487;maxBytes=20664;task=APH-712)` |
| 2026-07-12 | `D-029` | Register the renamed narrative punctuation utility boundary | The naming contract requires a pure utility reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/NarrativePunctuationUtilitySystemHelper.cs;scope=system-helper;maxLines=23;maxBytes=847;task=APH-712)` |
| 2026-07-12 | `D-030` | Register the renamed dialogue presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/UI/Narrative/NarrativeDialoguePresentationSystemHelper.cs;scope=system-helper;maxLines=140;maxBytes=4693;task=APH-712)` |
| 2026-07-12 | `D-031` | Register the renamed dialogue reveal presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/UI/Narrative/NarrativeDialogueRevealPresentationSystemHelper.cs;scope=system-helper;maxLines=132;maxBytes=4335;task=APH-712)` |
| 2026-07-12 | `D-032` | Register the renamed panel residency presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/UI/Narrative/NarrativePanelAssetResidencyPresentationSystemHelper.cs;scope=system-helper;maxLines=124;maxBytes=4505;task=APH-712)` |
| 2026-07-12 | `D-033` | Register the renamed panel motion presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/UI/Narrative/NarrativePanelMotionPresentationSystemHelper.cs;scope=system-helper;maxLines=135;maxBytes=4542;task=APH-712)` |
| 2026-07-12 | `D-034` | Register the renamed voice playback presentation boundary | The naming contract requires a managed presentation reason suffix on the existing GUID-preserved source | `source-growth-exception(path=Assets/Game/Scripts/UI/Narrative/NarrativeVoicePlaybackPresentationSystemHelper.cs;scope=system-helper;maxLines=59;maxBytes=1633;task=APH-712)` |
| 2026-07-12 | `D-035` | Register the FirstLaunch profile persistence composition boundary | Profile load/save/default projection must leave the transitional sequence/composition owner through one narrow managed persistence edge | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeProfileCompositionSystemHelper.cs;scope=system-helper;maxLines=135;maxBytes=4892;task=APH-712)` |
| 2026-07-12 | `D-036` | Register the FirstLaunch shell ECS composition boundary | Startup disposition, handoff lifecycle, and one-shot route publication must leave the transitional owner through one narrow shell ECS edge | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeShellCompositionSystemHelper.cs;scope=system-helper;maxLines=84;maxBytes=3061;task=APH-712)` |
| 2026-07-12 | `D-037` | Register the FirstLaunch reviewer presentation boundary | Reviewer controls, safe-area preview, navigation, and completion evidence must stay outside production profile and shell routing state | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeReviewPresentationSystemHelper.cs;scope=system-helper;maxLines=161;maxBytes=6297;task=APH-712)` |
| 2026-07-12 | `D-038` | Register the FirstLaunch pure route-decision boundary | Mission handoff, debrief, reviewer transition, and Skip-confirmation policy must leave composition while remaining independent of concrete UI and ECS | `source-growth-exception(path=Assets/Game/Scripts/Narrative/Runtime/FirstLaunchNarrativeRouteUtilitySystemHelper.cs;scope=system-helper;maxLines=96;maxBytes=3640;task=APH-712)` |
| 2026-07-12 | `D-039` | Register the FirstLaunch pure sequence-progression boundary | Graph validation, state/index ownership, authored timing, transition-token validation, continue/skip routing, and typed sequence outputs must run without views, audio, Addressables, ECS, or editor APIs | `source-growth-exception(path=Assets/Game/Scripts/Narrative/Runtime/FirstLaunchNarrativeSequenceUtilitySystemHelper.cs;scope=system-helper;maxLines=321;maxBytes=12366;task=APH-712)` |
| 2026-07-12 | `D-040` | Revise the FirstLaunch managed sequence-presentation ceiling for the typed runtime adapter | The presentation edge replaces embedded progression fields and algorithms with explicit typed-intent construction and output adaptation; this temporary two-line adapter increase remains below 500 lines and is scheduled to shrink further during async residency extraction | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs;scope=system-helper;maxLines=489;maxBytes=22065;task=APH-712)` |
| 2026-07-12 | `D-041` | Revise the FirstLaunch profile boundary ceiling for authored milestone IDs | Save compatibility still records stable state IDs, but the profile boundary now receives commander/guidance milestone IDs from authored route roles instead of owning literals | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeProfileCompositionSystemHelper.cs;scope=system-helper;maxLines=135;maxBytes=5172;task=APH-712)` |
| 2026-07-12 | `D-042` | Revise the FirstLaunch model utility ceiling for config-to-domain projection | Semantic state lookup and typed route-request projection keep config traversal and metadata adaptation out of presentation and composition policy | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeModelUtilitySystemHelper.cs;scope=system-helper;maxLines=71;maxBytes=3132;task=APH-712)` |
| 2026-07-12 | `D-043` | Revise the FirstLaunch sequence-presentation ceiling for authored route metadata | The adapter now emits typed route requests and completion payloads from authored state metadata while remaining below the 500-line production review threshold | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs;scope=system-helper;maxLines=499;maxBytes=22588;task=APH-712)` |
| 2026-07-12 | `D-044` | Register the FirstLaunch panel presentation boundary | Aspect selection, direct fallback, current/next projection, and token-checked panel application must stay outside sequence progression and UI handle ownership | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativePanelPresentationSystemHelper.cs;scope=system-helper;maxLines=130;maxBytes=5310;task=APH-712)` |
| 2026-07-12 | `D-045` | Revise the panel residency ceiling for asynchronous handle lifecycle | Current/next async acquisition, completion/failure callbacks, token propagation, rapid-seek reuse, and pending-handle release replace synchronous `WaitForCompletion()` | `source-growth-exception(path=Assets/Game/Scripts/UI/Narrative/NarrativePanelAssetResidencyPresentationSystemHelper.cs;scope=system-helper;maxLines=170;maxBytes=6456;task=APH-712)` |
| 2026-07-12 | `D-046` | Tighten the FirstLaunch sequence-presentation ceiling after panel extraction | Moving panel loading and projection into a narrow presentation boundary reduces the managed sequence adapter by 58 lines | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs;scope=system-helper;maxLines=441;maxBytes=19797;task=APH-712)` |
| 2026-07-12 | `D-047` | Register the FirstLaunch interactive presentation boundary | Commander/guidance defaults, action context, input normalization, commit debouncing, and accessibility copy must stay outside passive Canvas views | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeInteractivePresentationSystemHelper.cs;scope=system-helper;maxLines=159;maxBytes=6036;task=APH-712)` |
| 2026-07-12 | `D-048` | Tighten the FirstLaunch sequence-presentation byte ceiling after passive-view extraction | Raw dialogue and Skip intents now bind at the managed presentation edge while sequence context and interactive policy no longer pass through `NarrativeSequenceView` | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs;scope=system-helper;maxLines=441;maxBytes=19352;task=APH-712)` |
| 2026-07-12 | `D-049` | Register the tactical-materials mutation utility boundary | Pure saturating grant/spend policy remains outside ECS update owners, adds no player-loop owner, and is frozen at its reviewed 97-line boundary | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs;scope=system-helper;maxLines=97;maxBytes=4226;task=APH-710)` |
| 2026-07-12 | `D-050` | Bound the tactical-materials authored config fields | Four serialized fields extend the existing gameplay config contract; extracting them would fragment authored data without reducing runtime ownership | `source-growth-exception(path=Assets/Game/Scripts/Configs/GameplayConfigModels.cs;scope=production-over-500-review;maxLines=761;maxBytes=57493;task=APH-710)` |
| 2026-07-12 | `D-051` | Bound tactical-materials custom-game startup projection | Four cohesive startup projection lines remain in the existing startup boundary and add no policy, allocation owner, or player-loop owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs;scope=system-helper-growth;maxLines=964;maxBytes=44871;task=APH-710)` |
| 2026-07-12 | `D-052` | Review the tactical-materials custom-game startup file at its exact size | The same four-line projection is accepted without authorizing any further helper or oversized-file growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs;scope=production-over-500-review;maxLines=964;maxBytes=44871;task=APH-710)` |
| 2026-07-12 | `D-053` | Bound the resource-hauler movement state integration | Four coupled movement/path-state lines belong to the existing movement system; a new abstraction would add indirection without separating a responsibility | `source-growth-exception(path=Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs;scope=production-over-500-review;maxLines=824;maxBytes=37358;task=APH-710)` |
| 2026-07-12 | `D-054` | Register the faction tactical-materials startup boundary | Economy discovery, initial player-credit projection, faction material seeding, and fallback player-economy creation leave the oversized spawn system without adding an ECS system or player-loop owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionTacticalMaterialsStartupSystemHelper.cs;scope=system-helper;maxLines=99;maxBytes=4074;task=APH-710)` |
| 2026-07-12 | `D-055` | Register the vehicle route-clearance utility boundary | Footprint/corridor blocker clearance and deterministic building-approach search leave two oversized owners in one stateless non-ECS utility with no new update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/VehicleRouteClearanceUtilitySystemHelper.cs;scope=system-helper;maxLines=264;maxBytes=10265;task=APH-710)` |
| 2026-07-13 | `D-056` | Register the Resource Exchange projection-fingerprint utility boundary | A stateless unmanaged fingerprint lets the existing `ISystem` skip unchanged text/buffer projection and quantizes only displayed countdown seconds and percentage without adding a runtime loop | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeProjectionFingerprintUtilitySystemHelper.cs;scope=system-helper;maxLines=151;maxBytes=5674;task=APH-710)` |
| 2026-07-13 | `D-057` | Register the Resource Exchange text-projection utility boundary | Moving existing managed display formatting out of the read-model system reduces that owner from 724 to 580 lines and keeps formatting separate from ECS projection policy | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeTextProjectionUtilitySystemHelper.cs;scope=system-helper;maxLines=158;maxBytes=6107;task=APH-710)` |
| 2026-07-13 | `D-058` | Register 24 Scenario Lab-only coroutine keys in the Phase 7 loop baseline | `BattleScenarioLabVisualPlayback` is confined to Scenario Lab scenes; enabled production build scenes contain only `Menu` and `Match`, so the reviewed routines cannot execute in shipping flows | `Design/Architecture/phase7_monobehaviour_loop_baseline.md` rows `42-65`; `ProjectSettings/EditorBuildSettings.asset` |
| 2026-07-13 | `D-059` | Accept the compact APH-704 linked-cell index as the disabled benchmark foundation, while deferring production consumer scheduling | The second candidate passes exact equivalence, `<1.025 ms` complete acquisition, `>=10%` improvement, zero warmed allocation, and payload gates; keeping `[DisableAutoCreation]` prevents unmeasured Match work before consumer-level adoption evidence | APH-704 `9/9` equivalence and post-rebase 740-unit/32-consumer benchmark at `0.684 ms`, `74.88%`, `0` bytes GC, `18,784` bytes payload |
| 2026-07-13 | `D-060` | Supersede the stale tactical-materials utility ceiling | Canonical export refunds and construction refunds extend the existing saturating faction-material mutation policy without adding an ECS system or player-loop owner; the superseding ceiling is exact and authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs;scope=system-helper;maxLines=136;maxBytes=6083;task=APH-710)` |
| 2026-07-13 | `D-061` | Register the atomic construction-resource utility boundary | Coupled credit/material evaluation, spend, and rollback must remain atomic outside AI and player construction callers; the stateless boundary adds no scheduling owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionConstructionResourceUtilitySystemHelper.cs;scope=system-helper;maxLines=85;maxBytes=3813;task=APH-710)` |
| 2026-07-13 | `D-062` | Register the canonical Resource Exchange resource adapter | Canonical credit, tactical-material, oil, and fuel queries/mutations remain in one stateless adapter so exchange systems do not recreate wallet/storage policy | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeResourceUtilitySystemHelper.cs;scope=system-helper;maxLines=162;maxBytes=6360;task=APH-710)` |
| 2026-07-13 | `D-063` | Register the reduced physical-storage transaction facade | Queue reservation, completion, and cancellation orchestration remains one public transaction boundary after candidate discovery and reservation mutation are extracted; it adds no ECS system or update loop | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangePhysicalStorageUtilitySystemHelper.cs;scope=system-helper;maxLines=306;maxBytes=13630;task=APH-710)` |
| 2026-07-13 | `D-064` | Register the physical-storage candidate-query boundary | Deterministic storage discovery, capacity projection, and stable runtime-building/entity ordering are a read-only query responsibility separated from transaction mutation | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangePhysicalStorageCandidateQuerySystemHelper.cs;scope=system-helper;maxLines=76;maxBytes=3275;task=APH-710)` |
| 2026-07-13 | `D-065` | Register the physical-storage reservation-mutation boundary | Release, consume, line removal, and partial-reservation rollback share one mutation responsibility and remain outside the reduced transaction facade without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangePhysicalStorageReservationMutationSystemHelper.cs;scope=system-helper;maxLines=104;maxBytes=4588;task=APH-710)` |
| 2026-07-13 | `D-066` | Register the Resource Exchange startup projection boundary | Scenario-gate validation, canonical faction resolution, exchange component/buffer initialization, and recipe projection run once through the existing startup composition path and add no player-loop owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeStartupProjectionSystemHelper.cs;scope=system-helper;maxLines=268;maxBytes=13520;task=APH-710)` |
| 2026-07-13 | `D-067` | Register the material-fabrication command utility boundary | Request validation, bounded queue mutation, correlated result lookup, and version wrapping form one stateless command responsibility extracted from the fabrication `ISystem`; the helper adds no scheduling owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/MaterialFabricationCommandUtilitySystemHelper.cs;scope=system-helper;maxLines=133;maxBytes=5318;task=APH-710)` |
| 2026-07-14 | `D-068` | Register the resource-hauler storage-access boundary | Resource-kind conversion, storage snapshots, ECS lookup/commit, metadata synchronization, and managed storage mirroring form one stateless storage responsibility extracted from the existing hauling utility without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceHaulerStorageAccessSystemHelper.cs;scope=system-helper;maxLines=88;maxBytes=3680;task=APH-710)` |
| 2026-07-13 | `D-069` | Supersede the direct-integration portions of `D-013` and `D-017` with the authoritative agent pull request workflow for new tasks; grandfather tasks already in progress at activation | Separate substantive implementation from independent findings-first acceptance while retaining coordinator tracker control, existing validation gates, historical evidence, and a bounded transition | User-approved bootstrap; `Design/Architecture/agent_pull_request_review_merge_workflow.md` |
| 2026-07-14 | `D-070` | Register the map-vehicle placement clearance boundary | Footprint resolution, blocker clearing, cardinal departure-corridor search, and clearance progress accounting form one stateless responsibility extracted from the placement/spawn facade without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/MapVehiclePlacementClearanceSystemHelper.cs;scope=system-helper;maxLines=244;maxBytes=9254;task=APH-710)` |
| 2026-07-14 | `D-071` | Register the building runtime path-interaction context boundary | Redirect, barrier, resource-hauler bridge, and selected-hauler context wiring form one stateless composition responsibility extracted from the runtime context factory without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimePathInteractionContextFactoryCompositionSystemHelper.cs;scope=system-helper;maxLines=99;maxBytes=5222;task=APH-710)` |
| 2026-07-14 | `D-072` | Register the AI Resource Exchange startup-projection boundary | AI-faction discovery, scenario gating, exchange-boundary disable/reset, and per-faction startup projection form one startup-only responsibility extracted from the shared Resource Exchange projection facade without adding an ECS update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeAIStartupProjectionSystemHelper.cs;scope=system-helper;maxLines=239;maxBytes=11012;task=APH-710)` |
| 2026-07-14 | `D-073` | Supersede the Resource Exchange startup-projection facade ceiling from `D-066` | The existing public facade retains its nested AI result contract and one forwarding method while AI implementation leaves the owner; the exact 294-line ceiling authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeStartupProjectionSystemHelper.cs;scope=system-helper;maxLines=294;maxBytes=14551;task=APH-710)` |
| 2026-07-14 | `D-074` | Bound AI recovery component initialization in the existing AI startup boundary | Creating, backfilling, and initializing the recovery-need component is inseparable from faction build-plan startup; the exact reviewed ceiling adds no update owner and authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/AIStartupSystem.cs;scope=production-over-500-review;maxLines=554;maxBytes=25422;task=APH-710)` |
| 2026-07-14 | `D-075` | Register the building-definition footprint clone boundary | Exact field-preserving definition cloning with one footprint override is a pure construction responsibility extracted from the runtime spawn facade without adding scheduling or mutable state | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingDefinitionFootprintCloneSystemHelper.cs;scope=system-helper;maxLines=70;maxBytes=4084;task=APH-710)` |
| 2026-07-14 | `D-076` | Supersede the tactical-materials mutation utility ceiling from `D-060` | Spend-kind lifetime telemetry, construction-refund symmetry, and nonnegative telemetry validation extend the same atomic saturating mutation policy; the exact reviewed ceiling adds no ECS or update owner and authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs;scope=system-helper;maxLines=159;maxBytes=7392;task=APH-710)` |
| 2026-07-14 | `D-077` | Register the material-fabrication telemetry boundary | Active/blocked duration accumulation, saturating float counters, and telemetry-version mutation form one stateless responsibility extracted from the fabrication `ISystem` without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/MaterialFabricationTelemetrySystemHelper.cs;scope=system-helper;maxLines=96;maxBytes=4027;task=APH-710)` |
| 2026-07-14 | `D-078` | Register the Build Drawer projection-formatting boundary | Localization, display formatting, bounded fixed-string conversion, and presentation-copy trimming form one stateless managed projection responsibility extracted from the unmanaged read-model `ISystem` without changing queries or update ownership | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerProjectionSystemHelper.cs;scope=system-helper;maxLines=188;maxBytes=10563;task=APH-710)` |
| 2026-07-14 | `D-079` | Register the Resource Exchange read-model projection boundary | Route validation, amount normalization, queue-state projection, display formatting, and bounded fixed-string conversion form one stateless responsibility extracted from the existing change-driven unmanaged read-model `ISystem` without changing query or update ownership | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeProjectionSystemHelper.cs;scope=system-helper;maxLines=312;maxBytes=13094;task=APH-710)` |
| 2026-07-14 | `D-080` | Register the faction fuel-logistics telemetry mutation boundary | Saturating tray assignment, reassignment, failure, refinery/depot delivery, and version mutation form one stateless telemetry responsibility added by the fabrication feature without creating an ECS update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionFuelLogisticsTelemetryUtilitySystemHelper.cs;scope=system-helper;maxLines=71;maxBytes=2445;task=APH-710)` |
| 2026-07-14 | `D-081` | Register the UI action-dispatch boundary | Typed route, popup, build, placement, tactical-command, squad-tray, diagnostics, and drawer-audio dispatch form one stateless responsibility extracted from the existing unmanaged request `ISystem` without changing query or update ownership | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestDispatchSystemHelper.cs;scope=system-helper;maxLines=487;maxBytes=24721;task=APH-710)` |
| 2026-07-14 | `D-082` | Register the UI Resource Exchange command boundary | Selected-recipe resolution, stepped amount normalization, and confirmed start-request creation form one stateless command responsibility separated from general UI dispatch without adding an ECS update owner | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiActionResourceExchangeRequestSystemHelper.cs;scope=system-helper;maxLines=113;maxBytes=4101;task=APH-710)` |
| 2026-07-14 | `D-083` | Register the Build Drawer catalog-presentation boundary | Tab/catalog projection, pooled item presentation, selection/detail binding, localized instruction copy, and command-result formatting leave the MonoBehaviour owner without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresentationSystemHelper.cs;scope=system-helper;maxLines=463;maxBytes=22451;task=APH-710)` |
| 2026-07-14 | `D-084` | Register the Build Drawer production-queue UI boundary | Queue snapshot projection, row pooling, cancel/clear ordering, and queue-control binding leave the existing runtime view while its 0.2-second `Update` schedule remains unchanged | `source-growth-exception(path=Assets/Game/Scripts/UI/Screens/BuildDrawerProductionQueueUiSystemHelper.cs;scope=system-helper;maxLines=399;maxBytes=15735;task=APH-710)` |
| 2026-07-14 | `D-085` | Register the building material-fabrication read-model boundary | Per-Match versioned fabrication, storage, and faction-material projection state leaves the building UI query facade without changing pull ownership, faction resolution, or invalidation behavior | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingMaterialFabricationReadModelUiSystemHelper.cs;scope=system-helper;maxLines=230;maxBytes=10323;task=APH-710)` |
| 2026-07-14 | `D-086` | Register the building production-entry UI boundary | Produced-unit and pending-production entry projection, pruning, progress, and friendly queue enumeration leave the building UI query facade without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingProductionEntriesUiSystemHelper.cs;scope=system-helper;maxLines=351;maxBytes=13949;task=APH-710)` |
| 2026-07-14 | `D-087` | Register the Resource Exchange queue-completion boundary | Completion storage validation, physical settlement, output grant, results, economy events, flyouts, toasts, and ARIA projection leave the recurring queue owner while its single `ISystem` schedule and public forwarding API remain unchanged | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeQueueCompletionSystemHelper.cs;scope=system-helper;maxLines=415;maxBytes=16853;task=APH-710)` |
| 2026-07-14 | `D-088` | Register the AI build-planning policy boundary | Deterministic build-id normalization, owned/pending/config lookup, affordability-result mapping, and preferred placement policy leave the single AIBuildPlanner `ISystem` without adding scheduling ownership or mutable state | `source-growth-exception(path=Assets/Game/Scripts/Systems/AIBuildPlanningPolicySystemHelper.cs;scope=system-helper;maxLines=257;maxBytes=9792;task=APH-710)` |
| 2026-07-14 | `D-089` | Register the AI materials-recovery mutation boundary | Publish/clear mutation and first-blocked/version semantics leave the single AIBuildPlanner `ISystem` without adding scheduling ownership or mutable state | `source-growth-exception(path=Assets/Game/Scripts/Systems/AIMaterialsRecoveryNeedMutationSystemHelper.cs;scope=system-helper;maxLines=79;maxBytes=2988;task=APH-710)` |
| 2026-07-14 | `D-090` | Register the AI build-planning read boundary | Economy snapshot projection, faction/building lookup, materials-change comparison, AI-control resolution, and spawn-result labeling leave the single AIBuildPlanner `ISystem` while its query fields, handle updates, transaction, settlement, request ID, and diagnostics ownership remain unchanged; no new schedule or mutable state | `source-growth-exception(path=Assets/Game/Scripts/Systems/AIBuildPlanningReadSystemHelper.cs;scope=system-helper;maxLines=147;maxBytes=5711;task=APH-710)` |
| 2026-07-14 | `D-091` | Register the Materials scenario-recovery startup boundary | Startup/catalog/faction reads and authored-plan validation remain in one startup-only facade while pure recovery policy leaves the oversized owner; no ECS system or recurring update owner is added | `source-growth-exception(path=Assets/Game/Scripts/Systems/MaterialsScenarioRecoveryStartupSystemHelper.cs;scope=system-helper;maxLines=484;maxBytes=21672;task=APH-710)` |
| 2026-07-14 | `D-092` | Register the Materials scenario-recovery policy boundary | Capacity, reserve, seeded-chain, rebuild-chain, exchange-import, and aggregate-path evaluation form one deterministic stateless policy separated from ECS/catalog reads | `source-growth-exception(path=Assets/Game/Scripts/Systems/MaterialsScenarioRecoveryPolicyUtilitySystemHelper.cs;scope=system-helper;maxLines=167;maxBytes=6607;task=APH-710)` |
| 2026-07-14 | `D-093` | Register the FirstLaunch portrait and voice-selection presentation boundary | Selected portrait capture and commander neutral/female/default voice fallback are one managed presentation responsibility extracted from sequence orchestration without changing route or progression policy | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper.cs;scope=system-helper;maxLines=60;maxBytes=2102;task=APH-712)` |
| 2026-07-14 | `D-094` | Supersede the FirstLaunch review-request utility ceiling from `D-027` | Reviewer request, explicit clear, and one-shot consume all mutate the same Editor-only preference key; retaining readable request/delete/save semantics is preferable to byte-driven one-letter compression | `source-growth-exception(path=Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeReviewUtilitySystemHelper.cs;scope=system-helper;maxLines=36;maxBytes=819;task=APH-712)` |
| 2026-07-14 | `D-095` | Register the single managed audio presentation loop | `AudioPlaybackPresentationRuntimeView.Update` is the sole Menu/Match owner that ticks pooled `AudioSource` objects and drains accepted ECS requests; one-owner and warmed zero-allocation tests constrain it, and moving Unity-object pooling into an unmanaged system would violate the managed presentation boundary | User approval of the audio changes; `AudioPlaybackPresentationSceneBindingTests`; `AudioPlaybackPresentationBridgeSystemHelperTests` |
| 2026-07-14 | `D-096` | Require clean Unity build cache for release Android packages | The current oversized APK contains one 69,156,501-byte catalog-unindexed stale DOTS archive left in Gradle staging; `BuildOptions.CleanBuildCache` removes stale build results without deleting source content, while profiler build options remain unchanged | ZIP/catalog/Bee audit; `AndroidBuildReportGeneratorTests` |
| 2026-07-14 | `D-097` | Register the Match gameplay-startup composition boundary | Step sequencing, startup status, failure latching, retry state, shutdown reset, and startup resolver wiring leave the oversized Match bootstrap without adding another update owner | `source-growth-exception(path=Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs;scope=system-helper;maxLines=390;maxBytes=18337;task=APH-710)` |
| 2026-07-14 | `D-098` | Register the camp-item command policy boundary | Stable request identity, affordability-result mapping, camp failure projection, and result-price policy leave the oversized production request owner without adding scheduling or mutable state | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingCampItemCommandPolicySystemHelper.cs;scope=system-helper;maxLines=98;maxBytes=4940;task=APH-710)` |
| 2026-07-14 | `D-099` | Register the fuel-logistics telemetry bridge boundary | Assignment, failure, refinery/depot delivery, cached telemetry lookup, and task-status projection form one stateless bridge extracted from resource hauling without adding an update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionFuelLogisticsTelemetryBridgeCompositionSystemHelper.cs;scope=system-helper;maxLines=172;maxBytes=7473;task=APH-710)` |
| 2026-07-14 | `D-100` | Supersede the tactical-materials startup boundary from `D-054` | The complete current startup contract includes player and AI current/capacity rules, fabrication and fuel-logistics telemetry, event-buffer initialization, and queue markers; keeping that atomic startup projection prevents partial faction state | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionTacticalMaterialsStartupSystemHelper.cs;scope=system-helper;maxLines=130;maxBytes=5849;task=APH-710)` |
| 2026-07-14 | `D-101` | Register the Resource Exchange request-buffer boundary | Request ID allocation, typed request-buffer writes, and correlated result lookup leave the validation system while its public forwarding API and single schedule remain unchanged | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeRequestQueueSystemHelper.cs;scope=system-helper;maxLines=94;maxBytes=3662;task=APH-710)` |
| 2026-07-14 | `D-102` | Register the Resource Exchange rush policy boundary | Rush eligibility, ticket capacity, queue lookup, ticket application, completion forwarding, and accepted/rejected result projection form one stateless policy outside the request-processing owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceExchangeRushPolicySystemHelper.cs;scope=system-helper;maxLines=163;maxBytes=6473;task=APH-710)` |
| 2026-07-14 | `D-103` | Register the resource-hauler AI oil-allocation policy boundary | Per-scan input caching plus deterministic construction and fuel-pressure evaluation leave the oversized hauling bridge without adding scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceHaulerAIOilAllocationPolicySystemHelper.cs;scope=system-helper;maxLines=96;maxBytes=3222;task=APH-710)` |
| 2026-07-14 | `D-104` | Register the resource-hauler automatic-route policy boundary | Route discovery, fabrication demand, destination classification, and deterministic scoring leave the hauling bridge while movement, phases, and reservations remain in its existing owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs;scope=system-helper;maxLines=417;maxBytes=16889;task=APH-710)` |
| 2026-07-14 | `D-105` | Review the authored gameplay configuration contract at its exact size | Serialized gameplay configuration records remain a cohesive authored-data surface; splitting fields across files would fragment asset serialization without isolating runtime policy | `source-growth-exception(path=Assets/Game/Scripts/Configs/GameplayConfigModels.cs;scope=production-over-500-review;maxLines=802;maxBytes=60498;task=APH-710)` |
| 2026-07-14 | `D-106` | Review the building-definition prefab contract at its exact size | Authored building definition data and prefab projection remain one serialized boundary; the exact tuple authorizes no additional growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs;scope=production-over-500-review;maxLines=1002;maxBytes=48335;task=APH-710)` |
| 2026-07-14 | `D-107` | Review building gameplay composition at its exact size | The owner contains composition wiring for one building gameplay boundary and adds no independent schedule; a two-line split would add indirection without separating policy | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs;scope=production-over-500-review;maxLines=773;maxBytes=48379;task=APH-710)` |
| 2026-07-14 | `D-108` | Bound building runtime processing at its exact size | The one-line integration remains in the established processing composition owner and introduces no new loop or responsibility; further growth remains forbidden | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs;scope=production-over-1000-growth;maxLines=1678;maxBytes=74548;task=APH-710)` |
| 2026-07-14 | `D-109` | Supersede the Custom Game startup reviewed ceiling from `D-052` | Current startup projection remains one scenario-initialization responsibility after tactical-material and exchange integration; the exact tuple replaces the stale ceiling only | `source-growth-exception(path=Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs;scope=production-over-500-review;maxLines=968;maxBytes=45255;task=APH-710)` |
| 2026-07-14 | `D-110` | Review performance diagnostics at its exact size | Diagnostics labels and bounded telemetry projection remain in the existing diagnostics-only boundary, outside gameplay mutation and scheduling ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystemHelper.cs;scope=production-over-500-review;maxLines=909;maxBytes=39630;task=APH-710)` |
| 2026-07-14 | `D-111` | Bound selection gameplay startup at its exact size | Selection startup, command bridge setup, and teardown form one lifecycle boundary; the reviewed tuple adds no player-loop owner and authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs;scope=production-over-1000-growth;maxLines=1673;maxBytes=88186;task=APH-710)` |
| 2026-07-14 | `D-112` | Bound selection HUD feedback at its exact size | Command-specific status, warning, and accessibility feedback remain one UI feedback responsibility; the exact tuple preserves complete command coverage without authorizing growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs;scope=production-over-1000-growth;maxLines=2175;maxBytes=95202;task=APH-710)` |
| 2026-07-14 | `D-113` | Review the Match HUD selection panel view at its exact size | Serialized references and passive binding methods remain one panel view contract; the view owns no gameplay policy or recurring schedule | `source-growth-exception(path=Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs;scope=production-over-500-review;maxLines=752;maxBytes=31771;task=APH-710)` |
| 2026-07-14 | `D-114` | Bound UI shell ECS component contracts at their exact size | Typed shell components remain a data-only contract surface; splitting records by byte count would fragment shared ECS schema without reducing runtime ownership | `source-growth-exception(path=Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs;scope=production-over-1000-growth;maxLines=1925;maxBytes=63944;task=APH-710)` |
| 2026-07-15 | `D-115` | Retire the Main Menu growth exception | One-time HUD reference discovery moved to an exact-bounded setup helper; `MainMenuPlayUI` is now below its original `921`-line / `36,715`-byte review baseline, so the prior growth exception is removed and future growth remains fail-closed | Focused production source-growth gate |
| 2026-07-14 | `D-116` | Review the Resource Exchange read model at its exact size | Change-driven ECS query ownership remains in the single unmanaged read-model system while formatting and fingerprint policy are already extracted | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs;scope=production-over-500-review;maxLines=513;maxBytes=24004;task=APH-710)` |
| 2026-07-14 | `D-117` | Review UI shell content routing at its exact size | Screen content references and route visibility remain one passive shell view boundary; the exact tuple covers current Campaign integration only | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/UIShellContentView.cs;scope=production-over-500-review;maxLines=951;maxBytes=41317;task=APH-710)` |
| 2026-07-14 | `D-118` | Bound building-definition helper growth at its exact size | The same serialized definition boundary reviewed by `D-106` remains in the established helper; separate helper growth authorization preserves both guard dimensions | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs;scope=system-helper-growth;maxLines=1002;maxBytes=48335;task=APH-710)` |
| 2026-07-14 | `D-119` | Bound building gameplay composition helper growth | The same composition boundary reviewed by `D-107` retains its existing helper identity and adds no scheduling owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=773;maxBytes=48379;task=APH-710)` |
| 2026-07-14 | `D-120` | Bound building runtime processing helper growth | The same exact owner reviewed by `D-108` remains the established composition helper; no further helper growth is authorized | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=1678;maxBytes=74548;task=APH-710)` |
| 2026-07-14 | `D-121` | Supersede the Custom Game startup helper ceiling from `D-051` | The current scenario-startup projection remains in the established helper; the exact tuple replaces the stale helper ceiling and authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs;scope=system-helper-growth;maxLines=968;maxBytes=45255;task=APH-710)` |
| 2026-07-14 | `D-122` | Bound performance diagnostics helper growth | The reviewed diagnostics-only boundary keeps its existing helper identity, no mutable gameplay authority, and no additional update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystemHelper.cs;scope=system-helper-growth;maxLines=909;maxBytes=39630;task=APH-710)` |
| 2026-07-14 | `D-123` | Bound selection gameplay startup helper growth | The lifecycle owner reviewed by `D-111` remains one established helper with exact measured limits and no new schedule | `source-growth-exception(path=Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs;scope=system-helper-growth;maxLines=1673;maxBytes=88186;task=APH-710)` |
| 2026-07-14 | `D-124` | Bound selection HUD feedback helper growth | The UI feedback owner reviewed by `D-112` remains one established helper; exact limits preserve complete command feedback without allowing further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs;scope=system-helper-growth;maxLines=2175;maxBytes=95202;task=APH-710)` |
| 2026-07-14 | `D-125` | Bound building-definition authoring metadata projection | Fabrication and materials-cost fields extend one field-for-field runtime metadata projection; splitting them risks partially initialized definitions | `source-growth-exception(path=Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs;scope=system-helper-growth;maxLines=89;maxBytes=5056;task=APH-710)` |
| 2026-07-14 | `D-126` | Bound Match-start terminal failure projection | Terminal startup failure, bounded result text, and request correlation remain one state-machine transition responsibility | `source-growth-exception(path=Assets/Game/Scripts/Composition/MatchStartSceneSystemHelper.cs;scope=system-helper-growth;maxLines=298;maxBytes=11869;task=APH-710)` |
| 2026-07-14 | `D-127` | Bound UI catalog authored-cost projection | One materials-cost field extends the existing immutable authoring-to-catalog projection without adding policy | `source-growth-exception(path=Assets/Game/Scripts/Composition/UiCatalogAuthoringMetadataUiSystemHelper.cs;scope=system-helper-growth;maxLines=69;maxBytes=2895;task=APH-710)` |
| 2026-07-14 | `D-128` | Bound building gameplay startup resource wiring | Seed-before-configure ordering plus the extracted AI-oil input wiring remain one startup dependency-composition responsibility and add no update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingGameplayStartupCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=37;maxBytes=1806;task=APH-710)` |
| 2026-07-14 | `D-129` | Bound managed gameplay startup selection wiring | Two selection startup dependencies remain pure composition wiring in the existing managed startup boundary | `source-growth-exception(path=Assets/Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs;scope=system-helper-growth;maxLines=353;maxBytes=19452;task=APH-710)` |
| 2026-07-14 | `D-130` | Bound placement command construction-resource forwarding | Construction-result forwarding remains cohesive command plumbing in the existing placement command boundary | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingPlacementCommandCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=183;maxBytes=12274;task=APH-806)` |
| 2026-07-14 | `D-131` | Bound placement context construction-resource wiring | Construction transaction delegates remain one immutable context-wiring responsibility | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingPlacementContextCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=279;maxBytes=16498;task=APH-806)` |
| 2026-07-14 | `D-132` | Bound placement session transaction forwarding | Placement-session transaction identity and result forwarding remain cohesive session plumbing | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingPlacementSessionCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=142;maxBytes=6496;task=APH-806)` |
| 2026-07-14 | `D-133` | Bound placement visual composition result wiring | Placement visual composition retains one presentation-context responsibility; the byte-only delta adds no policy or loop | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingPlacementVisualCompositionPresentationSystemHelper.cs;scope=system-helper-growth;maxLines=248;maxBytes=14207;task=APH-806)` |
| 2026-07-14 | `D-134` | Bound placement visual-update construction result wiring | Five lines forward typed construction outcomes through the existing visual-update boundary without adding ownership | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=284;maxBytes=16135;task=APH-806)` |
| 2026-07-14 | `D-135` | Bound production composition affordability wiring | Affordability and AI-oil delegates extend the established production composition path without adding policy | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingProductionCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=103;maxBytes=6999;task=APH-710)` |
| 2026-07-14 | `D-136` | Bound production context affordability wiring | Construction-resource and AI-oil dependencies remain immutable production-context wiring | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingProductionContextCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=374;maxBytes=24312;task=APH-710)` |
| 2026-07-14 | `D-137` | Bound runtime context resource wiring | The one-line resource dependency remains in the established runtime-context factory boundary | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeContextCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=266;maxBytes=17375;task=APH-710)` |
| 2026-07-14 | `D-138` | Bound runtime creation fabrication projection | Fabrication metadata remains part of atomic runtime-building creation and registration | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeCreationCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=243;maxBytes=12934;task=APH-710)` |
| 2026-07-14 | `D-139` | Bound runtime entity fabrication components | Fabrication/storage component projection remains one entity-construction responsibility | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=340;maxBytes=16636;task=APH-710)` |
| 2026-07-14 | `D-140` | Bound runtime ownership resource propagation | Resource ownership propagation remains cohesive with runtime building ownership changes | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=130;maxBytes=5897;task=APH-710)` |
| 2026-07-14 | `D-141` | Bound building UI typed construction failures | Typed construction failures extend the established command result contract without adding UI policy | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingUiCommandSystemHelper.cs;scope=system-helper-growth;maxLines=240;maxBytes=10267;task=APH-710)` |
| 2026-07-14 | `D-142` | Bound building UI canonical resource context | Canonical faction-resource resolution remains context wiring for the existing UI command boundary | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingUiContextCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=303;maxBytes=16560;task=APH-710)` |
| 2026-07-14 | `D-143` | Bound Build Drawer materials-cost catalog projection | One authored materials-cost field extends the existing catalog query projection without policy or scheduling | `source-growth-exception(path=Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogQueryUiSystemHelper.cs;scope=system-helper-growth;maxLines=474;maxBytes=18013;task=APH-710)` |
| 2026-07-14 | `D-144` | Register the building storage reservation boundary | Source and destination reservation, release, consumption, delivery, and version mutation form one stateless storage transaction policy extracted from transfer execution | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingResourceStorageReservationSystemHelper.cs;scope=system-helper;maxLines=187;maxBytes=6749;task=APH-710)` |
| 2026-07-14 | `D-145` | Register the controlled-faction resource boundary | Controlled-faction discovery, startup seeding, canonical credits/materials mutation, lookup, and citizen context form one resource authority with no recurring update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/RuntimeFactionResourceSystemHelper.cs;scope=system-helper;maxLines=274;maxBytes=11062;task=APH-710)` |
| 2026-07-14 | `D-146` | Register the construction resource transaction boundary | Construction transaction identity, reserve, finalize, rollback, and duplicate-settlement protection form one bounded transaction owner over the canonical faction-resource authority | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingConstructionResourceTransactionSystemHelper.cs;scope=system-helper;maxLines=87;maxBytes=3265;task=APH-710)` |
| 2026-07-15 | `D-147` | Register the faction AI oil allocation input boundary | Faction plan, economy, control, fuel snapshot, and build-decision inputs form one query-backed allocation input boundary with no independent scheduling owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingFactionAIOilAllocationInputSystemHelper.cs;scope=system-helper;maxLines=185;maxBytes=8158;task=APH-710)` |
| 2026-07-15 | `D-148` | Bound building gameplay source composition wiring | The extracted AI-oil input helper remains owned by the existing query-source composition boundary; the forwarding lines add no policy or recurring update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=164;maxBytes=14396;task=APH-710)` |
| 2026-07-15 | `D-149` | Register the Match HUD header reference cache boundary | One-time direct-child resource-slot and threat-panel discovery leaves the oversized menu owner, removes hierarchy lookup APIs, and adds no recurring update owner | `source-growth-exception(path=Assets/Game/Scripts/UI/Screens/MatchHudHeaderReferenceUiSystemHelper.cs;scope=system-helper;maxLines=148;maxBytes=5702;task=APH-710)` |
| 2026-07-15 | `D-150` | Register the assistant HUD reference boundary | Setup-only objective, button, and text-reference discovery leaves the established assistant helper below its ratcheted ceiling; the extracted helper adds no scheduling owner or mutable gameplay state | `source-growth-exception(path=Assets/Game/Scripts/UI/Screens/MatchHudAssistantReferenceUiSystemHelper.cs;scope=system-helper;maxLines=89;maxBytes=3232;task=APH-710)` |
| 2026-07-15 | `D-151` | Supersede the UI shell content-view ceiling from `D-117` | Match HUD installation now clears the completed loading layer before revealing gameplay; the single lifecycle line prevents an obsolete overlay from remaining active and the exact ceiling authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/UI/Shell/UIShellContentView.cs;scope=production-over-500-review;maxLines=952;maxBytes=41372;task=APH-710)` |
| 2026-07-15 | `D-152` | Supersede the gameplay source-composition ceiling from `D-148` | The retained placement-footprint delegate is now lifecycle-owned alongside the existing source adapters, eliminating recurring delegate allocation without adding policy or scheduling ownership; the exact ceiling authorizes no further growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=167;maxBytes=14595;task=APH-710)` |
| 2026-07-15 | `D-153` | Bound runtime placement-query delegate ownership | The runtime query composition keeps one lifecycle-owned placement-footprint delegate instead of constructing it during recurring context composition; this removes steady-state allocation and adds no query or update owner | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingRuntimeQueryCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=141;maxBytes=5433;task=APH-710)` |
| 2026-07-15 | `D-154` | Bound resource-hauler queue-cache growth | The existing hauler composition owns one world-aware move-order query cache so recurring route issuance does not construct three EntityQuery instances per order; the exact strict ceiling authorizes no other growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs;scope=production-over-1000-growth;maxLines=1574;maxBytes=73319;task=APH-710)` |
| 2026-07-15 | `D-155` | Review resource-hauler queue-cache ownership | Cached move-order query ownership remains in the existing hauler lifecycle boundary and delegates queue processing to the existing ISystem request contract | `source-growth-exception(path=Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=1574;maxBytes=73319;task=APH-710)` |
| 2026-07-15 | `D-156` | Review faction resource storage-query caching | The existing resource composition owns one world-aware storage query rather than constructing it on every production tick; the exact review ceiling authorizes no other growth | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs;scope=production-over-500-review;maxLines=865;maxBytes=34228;task=APH-710)` |
| 2026-07-15 | `D-157` | Bound faction resource query-cache ownership | Resource storage query lifetime remains inside the existing faction resource composition boundary with no additional update owner or SystemBase | `source-growth-exception(path=Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs;scope=system-helper-growth;maxLines=865;maxBytes=34228;task=APH-710)` |
| 2026-07-15 | `D-158` | Approve APH-803 p99 and startup p95 ceilings | Clean exact-revision evidence measured `33.10 ms` p99 and `24,861.44 ms` worst startup p95; the explicitly approved `50 ms` and `30,000 ms` tracked limits retain bounded variance headroom while leaving p95, memory, and thermal requirements unchanged | `Tools/CI/android_reference_device_profile.json`; `/private/tmp/warline-main-aph803-8568654b2-r1/aph803_android_development_evidence.json` |
| 2026-07-15 | `D-159` | Run a scoped BrightCommand Android ASTC 4x4 pilot | The clean APK is `48,722,883` bytes over budget and BrightCommand sprites are a large non-map, currently uncompressed included group; ASTC 4x4 preserves dimensions and UI metadata while reducing package and GPU texture payload, but acceptance remains conditional on exact clean-build, device-memory, and visual evidence | `Aph501AndroidMainMenuTexturePolicy`; `/private/tmp/warline-aph501-main-menu-policy-tests-r2.xml` (`12/12`) |
| 2026-07-15 | `D-160` | Remove mip chains from the scoped BrightCommand pilot | The first ASTC build reduced serialized texture contribution more than compressed APK size while all 60 assets remain screen-space UI protected from streaming; removing unused mip levels lowers package and residency without changing world-texture policy, but still requires exact visual proof | `Aph501AndroidMainMenuTexturePolicy`; focused importer tests and next clean BuildReport |
| 2026-07-15 | `D-161` | Run a scoped Match HUD and V15C Android ASTC 6x6 fallback | The exact mipless BrightCommand artifact remains `7,006,704` bytes over budget; 66 currently included supporting UI textures provide sufficient modeled package margin, while a path-scoped policy leaves offline First Launch and operation-map content unchanged | `Aph501AndroidSupportingUiTexturePolicy`; clean APK/visual/device acceptance required |
| 2026-07-15 | `D-162` | Defer scoped UI CPU-copy removal until measured need | Static code and importer evidence supports disabling readability on 72 governed UI textures with a likely `6.31 MiB` current-build upper bound, but the package-compliant APK is already ready for APH-804 and modeled residency is not accepted device evidence | `Design/AgentReports/2026-07-15_aph-501_ui_texture_cpu_readability_audit.md`; APH-804 release-device capture |
| 2026-07-16 | `D-163` | Register the operation-map runtime bootstrap boundary | One load-strategy-neutral composition boundary resolves a small validated catalog once per publication, publishes immutable operation-map metadata into the existing ECS lifecycle contract, owns only its blob lifetime, and has no recurring update loop, asset search, or scene-loading policy | `source-growth-exception(path=Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs;scope=system-helper;maxLines=295;maxBytes=11398;task=APH-710)` |
| 2026-07-16 | `D-164` | Bind the existing RTS camera bridge to active operation-map bounds | The established camera request bridge remains the shell-owned Unity camera mutation boundary; it now prefers one immutable active-map camera extent and retains `GridConfig` only as compatibility fallback, without adding a system, update loop, managed cache, or loading policy | `source-growth-exception(path=Assets/Game/Scripts/Systems/RtsCameraRequestSystem.cs;scope=production-over-500-review;maxLines=578;maxBytes=26940;task=APH-710)` |
| 2026-07-16 | `D-165` | Measure permission-safe installed release artifacts | Xiaomi shell access can hash and size `base.apk` and size extracted native libraries but cannot traverse protected `oat`; APH-804 therefore records the exact accessible artifact sum, keeps the full installed-footprint budget measurement-required, and never treats permission denial as zero bytes | `android_release_device_collection.py`; pinned-device `du -sb` evidence; collector regressions `28/28` |
