# Architecture and Performance Hardening Implementation Tracker

## Purpose

Turn the 2026-07-09 architecture and performance audit into an executable, multi-agent remediation program. This tracker is the source of truth for this program. An agent should be able to select one ready checklist item, implement it, validate it, and record evidence here without repeating the audit.

This tracker follows the completed historical program in `Design/Architecture/architecture_performance_audit_followup_tracker.md`. Do not reopen or rewrite that completed tracker. Carry forward its accepted baselines and lessons through this document.

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
| Current program status | Active - Phases 0 through 2 complete; Phase 3 settings ownership active |

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
- The coordinator updates `Progress Snapshot`, task checkboxes, and `Implementation Log` in the same integration change. Parallel workers return handoff evidence and do not edit these shared tracker sections.
- A completed implementation slice must be committed and pushed by the coordinator after the Stable Integration and Push Gate passes. Do not mark a task complete while its accepted changes exist only in a local worktree.
- Never raise a budget, allowlist a violation, or weaken a test merely to obtain a pass. A budget or allowlist change requires a before/after report and explicit approval in the Decision Log.
- Run `git diff --check` after every implementation slice.
- Preserve Unity `.meta` files and serialized scene/prefab references.
- Do not combine gameplay balance changes, visual redesign, random-map generation, or unrelated UI work with this program.

## Multi-Agent Operating Model

The program may use multiple coding agents, but the coordinator owns the critical path and integration state. Parallelism is allowed only for tasks with satisfied dependencies and non-overlapping file ownership.

### Roles and Initial Allocation

| Role | Initial assignment | Ownership |
|---|---|---|
| Coordinator | Program control and integration | tracker state, dependency checks, work allocation, conflict review, final validation, commits, and pushes |
| Build agent | `APH-006` | sequential first-party build matrix and categorized warning report; no Unity lease required |
| Performance agent | `APH-007` | current-head Match performance and steady-state GC captures; exclusive Unity lease required |
| Residency agent | `APH-008` | machine-readable content-residency inventory and Markdown summary; Unity importer inspection requires the Unity lease |
| Budget analysis agent | prepare `APH-009` | draft measured budget recommendations only after `APH-007` and `APH-008` evidence exists; coordinator approves and integrates |

### Coordination Rules

1. The coordinator is the only agent that claims tasks in this tracker, edits shared progress/log sections, integrates commits, rebases, or pushes.
2. Workers receive a bounded task, an explicit file allowlist, required validations, and a prohibition on touching unrelated dirty work.
3. Workers must not edit the same production file, test file, generated artifact, scene, prefab, or importer concurrently. If overlap becomes necessary, stop one worker and serialize the work.
4. Workers operating in the canonical shared worktree do not commit, rebase, or push. They return a handoff; the coordinator reviews and integrates it. A worker may commit only when the coordinator explicitly assigns an isolated worktree and commit ownership.
5. Only one Unity Editor or Unity batchmode process may use a project workspace at a time. The coordinator grants an exclusive Unity lease. Other agents may continue non-Unity builds, source analysis, or report work while that lease is active.
6. An isolated Unity workspace may run concurrently only when it already exists, points at the same execution baseline, has no shared `Library` directory, and the coordinator records its path and result. Never create an ad hoc clone merely to bypass a lock or licensing failure.
7. Use `Tools/CI/invoke_unity_macos.sh` in windowed batchmode without `-nographics`. If licensing fails in the sandbox, rerun once with the documented escalated/out-of-sandbox workaround before reporting a blocker.
8. The coordinator runs cross-slice validation after integration. A worker's focused pass is necessary evidence, not permission to skip the integrated build and architecture gates.
9. When the integrated slice is stable and has no compiler errors, the coordinator stages only task-owned files, commits one focused slice, reconciles with the latest remote branch without overwriting other work, and pushes. Never commit or push a known-broken slice or unrelated dirty work.

### Worker Handoff Contract

Every worker handoff must contain: task ID, baseline commit, files changed, files intentionally not touched, behavior statement, exact commands and pass markers, artifact/log paths, metrics before/after, visual result when applicable, residual risks, recommended next task, and a proposed focused commit message. The coordinator rejects incomplete handoffs and leaves the task pending or blocked.

## Agent Start Procedure

1. Read this document completely.
2. Read only the source files and focused tests named by the selected task.
3. Run `git status --short`, record the baseline commit, and preserve unrelated work.
4. Confirm every dependency in the Phase Dependency table is complete and obtain the task/file claim from the coordinator.
5. The coordinator changes the selected task to `[~]` and updates `Current task` in the Progress Snapshot before implementation begins.
6. Implement the narrowest behavior-preserving slice within the assigned file allowlist.
7. Run the task-specific commands and relevant rows of the Common Validation Matrix.
8. Inspect Unity console output and, for visual work, capture the required views while holding the Unity lease.
9. Return the Worker Handoff Contract evidence to the coordinator. Do not edit shared tracker state from a parallel worker.
10. The coordinator reviews the diff, runs integrated validation, writes the Implementation Log entry, commits and pushes the stable slice, and marks the task `[x]` only after all acceptance criteria and the Stable Integration and Push Gate pass.

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

| Gate | Current failure | Required end state |
|---|---|---|
| `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` | `Game.UI.Runtime` references `Game.Configs` | pass all 31 checks without weakening the contract |
| `EcsBurstHotPathArchitectureTests.RunFocusedValidation` | current head still contains resource-exchange and building-defense query snapshots; exact current-head guard count must be reproduced | zero hot-path `ToEntityArray` / `ToComponentDataArray` debt |
| `MatchGcAllocationCallstackCapture.RunSteadyState` | current baseline records `269,482 / 1,024` player-relevant bytes over 300 measured frames | pass the unchanged 1,024-byte budget after 180 warmup and 300 measured frames |

Current violating files:

- `Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef`: direct `Game.Configs` reference.
- `Assets/Game/Scripts/UI/Screens/BattleHudRuntimeFeedbackUiSystemHelper.cs`: direct `GameText` access.
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`: direct `GameText` access.
- `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs`: direct `GameText` access.
- `Assets/Game/Scripts/UI/Screens/MatchHudCurrentOrderBannerUiSystemHelper.cs`: direct `GameText` access.
- `Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs`: direct `GameText` access.
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs`: direct `GameText` access.
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`: direct `GameText` access.
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`: `exchangeQuery.ToEntityArray(Allocator.Temp)`.
- `Assets/Game/Scripts/Systems/BuildingDefenseAttackSystem.cs`: explicit dependency completion plus one `ToEntityArray` and three `ToComponentDataArray` snapshots on each throttled target-acquisition pass.

Current measured steady-state GC owners include per-frame scene-root scanning, `SelectionStateCompositionSystemHelper` construction, transport request processing, UI shell presentation, tactical-camera state queries, building command-entity queries, audio gameplay-state lookup, road command lookup, and pathfinding schedule/apply allocations. The authoritative owner table and call stacks are in `Design/AgentReports/2026-07-09_aph-007_match-gc-steady-state_raw-metadata-final_failed.md`.

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
| Checklist complete | `43 / 106` |
| Checklist percent complete | `40.6%` |
| Checklist in progress | `1 / 106`: `APH-310` |
| Complete plus active coverage | `44 / 106` (`41.5%`); this is visibility only, not accepted completion |
| Current phase | Phase 3 - settings, frame-rate, and environment ownership |
| Current task | `APH-310` run integrated settings, graphics, Match, and day/dusk/night visual proof gates |
| Red architecture gates | `0`: UI boundary `31/31` and ECS/Burst hot-path `10/10` pass on the integrated head |
| Red performance gates | `1`: steady-state Match GC `234,324 / 1,024` bytes; improved 13.0% from the APH-007 baseline without weakening the budget |
| Last verified commit | `dcde564ca`; runtime tier mapping matrix passes `12/12` |
| Last update | 2026-07-10 - APH-309 complete; integrated graphics proof active |

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
- [~] `APH-310` Run `SettingsPopupValidationTests.RunFocusedValidation`, `AndroidVisualQualityValidationTests.RunFocusedValidation`, Match smoke, and visual captures at day, dusk, and night.
- [ ] `APH-311` On Android, run a 10-minute 30 FPS tier capture and a 60 FPS tier capture. Record frame, GPU, memory, thermal, and visual results separately.

## Phase 4 - Reduce Audio Residency

Goal: reduce persistent audio memory without clipping, first-play stalls, or broken event routing.

Known baseline:

- Menu persistently references `AudioEventCatalogConfig.asset`.
- The catalog contains 226 clip references.
- There are 262 project WAV importers: 253 DecompressOnLoad/preloaded and 9 Streaming/not preloaded.
- Existing profile policy already specifies Streaming for cataloged Music and Ambience.
- Existing profile policy specifies DecompressOnLoad/preload for Voice. Voice is the first measured optimization candidate.

- [ ] `APH-400` Extend the content-residency report to list only catalog-referenced audio clips by bus/category, duration, channels, frequency, import load type, compressed size, and estimated decoded size.
- [ ] `APH-401` Capture Menu and Match audio memory before playback and after representative UI, combat, music, ambience, and ARIA voice playback.
- [ ] `APH-402` Identify catalog drift: cataloged Music/Ambience clips that do not match the existing Streaming profile and unused legacy clips that do not affect runtime residency.
- [ ] `APH-403` Correct cataloged Music/Ambience drift without changing event IDs or playback behavior.
- [ ] `APH-404` Change the Voice import profile pilot to `CompressedInMemory`, `preloadAudioData=false`, and `loadInBackground=true` for a representative ARIA subset.
- [ ] `APH-405` Measure first-play latency, repeated-play latency, decoded memory, compressed memory, and audio glitches on Android for the pilot.
- [ ] `APH-406` If the pilot passes, apply the Voice policy through the existing JSON-driven audio importer workflow and update contract tests. If it fails, record the failing devices/clips and use a bounded prewarm set instead of reverting to global preload.
- [ ] `APH-407` Evaluate splitting the persistent catalog into Core/Menu, Match, and Voice catalogs. Open a separate implementation slice only if importer changes do not meet the memory target.
- [ ] `APH-408` Run `AudioConfigContractTests.RunFocusedValidation`, `AudioPerformanceValidationTests.RunFocusedValidation`, audio scene binding tests, and an Android audible smoke pass.

## Phase 5 - Texture, Animation, Build, and Memory Policy

Goal: make residency and package size intentional while preserving visible quality.

- [ ] `APH-500` Produce a BuildReport-based top-100 included-asset table for Android APK and AAB builds.
- [ ] `APH-501` Add tracked budgets for APK/AAB size, installed size, peak allocated memory, texture memory, mesh memory, audio memory, and graphics-driver memory.
- [ ] `APH-502` Classify texture importers into UI, world albedo, world normal/mask, VFX, impostor/atlas, generated source/reference, and excluded/unreferenced groups.
- [ ] `APH-503` Add an editor guard preventing mip streaming on UI, font, animation-data, sprite-atlas, and generated reference/source textures.
- [ ] `APH-504` Enable mip streaming for a representative world-texture subset with mipmaps; set a mobile memory budget and preserve full-resolution nearby textures.
- [ ] `APH-505` Capture identical near, medium, and far camera screenshots before and after streaming. Reject blur, late pop, terrain seams, or missing vegetation detail.
- [ ] `APH-506` Measure memory and I/O during a 10-minute camera-pan/zoom session. Expand streaming only after the pilot passes.
- [ ] `APH-507` Audit Android texture overrides for ASTC size/quality and remove oversized 4K/8K limits only where the BuildReport proves inclusion and visual evidence proves no loss.
- [ ] `APH-508` Audit the six generated animation textures for actual runtime residency, duplication, clip coverage, precision requirements, and unload behavior. Do not change format based only on source file size.
- [ ] `APH-509` Remove unused packages only after a package-usage report proves no source, serialized asset, build script, or editor workflow dependency.
- [ ] `APH-510` Rebuild Android, compare BuildReport/memory/frame metrics, and record per-category deltas.

## Phase 6 - Editor-Baked Static Map Chunks

Goal: keep the exact authored Match appearance while replacing runtime hierarchy scanning and mesh combination with validated editor-baked output.

Quality lock:

- The canonical authored `Assets/Game/Scenes/Match.unity` remains the source.
- Do not move, rotate, scale, delete, procedurally regroup, or regenerate authored buildings, interiors, roads, props, vegetation, or terrain.
- Generated meshes must preserve world transforms, materials, lightmap indices/scale offsets, layers, shadows, probes, bounds, and culling behavior.
- Every visual acceptance requires top-down, oblique, low-ground, and gameplay-camera captures. Use Unity MCP for hierarchy inspection/screenshots when available.

- [ ] `APH-600` Add startup profiler markers and structured metrics around `StaticMapChunkBatchingPresentationSystemHelper.Initialize`.
- [ ] `APH-601` Capture current startup duration, renderer scan count, eligible/skipped counts, generated vertices, CPU mesh memory, GPU mesh memory, and peak startup allocation.
- [ ] `APH-602` Extract the current `BatchKey`, safety filters, chunk size, and mesh limits into editor-usable deterministic code without changing runtime behavior.
- [ ] `APH-603` Build an editor baker that writes generated chunk meshes/prefabs under `Assets/Game/GeneratedCombinedMeshes/StaticMapChunks/` with a manifest containing source object IDs and hashes.
- [ ] `APH-604` Add deterministic rebake and stale-output cleanup tests. Never delete output not listed in the manifest.
- [ ] `APH-605` Add structural validation for source count, combined bounds, material/submesh mapping, lightmap data, layers, shadows, probes, and unchanged source transforms.
- [ ] `APH-606` Add a generated-root reference to `MatchSceneView` or an equivalent serialized composition-owned config. Do not add `GameObject.Find` fallback for the new path.
- [ ] `APH-607` Load the baked chunks at runtime and disable only source renderers represented by a validated manifest entry.
- [ ] `APH-608` Compare runtime-batched and editor-baked screenshots plus draw calls, triangles, memory, and startup time. Require pixel/visual review, not metrics alone.
- [ ] `APH-609` After acceptance, disable runtime combination when a valid baked manifest is present; keep a development-only fallback for stale/missing bakes.
- [ ] `APH-610` Disable Read/Write on source meshes that no accepted runtime path needs, then rerun placement, road, city, map surface, lighting, and Match lifecycle tests.
- [ ] `APH-611` Run a full Android 10-minute soak and reject any missing interiors, floating/buried props, road changes, lighting changes, culling holes, or terrain-quality regression.

## Phase 7 - Incremental Architecture Decomposition

Goal: reduce change risk in measured/high-change domains without a broad rewrite.

Priority order:

1. Combat/building defense, because Phase 2 exposes current targeting costs.
2. Resource exchange/economy, because current work recently added hot-path and cross-layer behavior.
3. Transport boarding, because `TransportBoardingCommandSystem` remains 3,226 lines.
4. UI shell, because `UiShellEcsGateway` remains 2,577 lines.
5. Selection/HUD, because several files exceed 1,500-2,000 lines.

The `APH-007` raw-metadata GC report is mandatory measured input for this phase. Its recurring scene-root, helper-construction, transport, UI shell, selection, command-query, audio, road, and pathfinding allocation owners must be removed or explicitly reassigned to another tracker task before `APH-711` can pass.

- [ ] `APH-700` Add a generated domain dependency report listing every first-party assembly edge and top cross-domain type references.
- [ ] `APH-701` Freeze new `*SystemHelper` and `*CompositionSystemHelper` files unless the architecture guard contains an approved task ID from this tracker.
- [ ] `APH-702` Add a size guard for new or growing production files: review required above 500 lines; no existing file above 1,000 lines may grow without an explicit tracker exception.
- [ ] `APH-703` Inventory `World.DefaultGameObjectInjectionWorld` runtime call sites and the recurring `APH-007` allocation owners; classify composition edge, authoring/debug, presentation edge, hidden service-locator debt, or unrelated allocation debt.
- [ ] `APH-704` Design and benchmark a shared read-only unit spatial index for building defense, AI targeting, threat detection, and selection candidates. Adopt it only when it beats the Phase 2 direct-query baseline and preserves results.
- [ ] `APH-705` Extract combat contracts/data required for a future `Game.Runtime.Combat` assembly without introducing a dependency cycle.
- [ ] `APH-706` Split one cohesive combat slice and pass the full assembly boundary/build/test matrix before opening another domain split.
- [ ] `APH-707` Decompose `TransportBoardingCommandSystem` by its existing planning/routing/application seams while preserving public behavior and scenario tests.
- [ ] `APH-708` Decompose `UiShellEcsGateway` into route, read-model, action, and settings adapters behind existing contracts; views remain passive.
- [ ] `APH-709` Decompose selection/HUD files only where profiler or change-frequency evidence identifies risk; do not split for line count alone.
- [ ] `APH-710` Remove classified hidden world lookups and recurring per-frame allocation owners as each domain receives explicit World/EntityManager/query dependencies or cached non-allocating state.
- [ ] `APH-711` Re-run compile time, domain reload time, architecture gates, focused tests, Match performance, and the unchanged steady-state GC capture after every physical asmdef split and final allocation-remediation slice.

## Phase 8 - CI, PlayMode, Visual, and Device Gates

Goal: prevent recurrence and cover behavior that source-scanning tests cannot prove.

- [ ] `APH-800` Add both architecture execute-method validations to CI and fail the build on nonzero exit or missing pass marker.
- [ ] `APH-801` Add the editor Match performance baseline and steady-state GC capture to a scheduled or pre-merge performance lane; fail when the unchanged 1,024-byte GC budget is exceeded.
- [ ] `APH-802` Ratchet the editor p95 budget from the current lenient `50 ms` only after at least five stable captures establish variance.
- [ ] `APH-803` Add an Android development-build gate for p95/p99 frame, peak memory, startup time, and thermal status on the reference device profile.
- [ ] `APH-804` Add Android release-build acceptance for the documented 30 FPS tier; keep 60 FPS as a separate high-end target.
- [ ] `APH-805` Add a Menu -> Match -> Menu lifecycle PlayMode test covering serialized references, world creation, UI binding, and cleanup.
- [ ] `APH-806` Add deterministic PlayMode flows for selection/move/attack and building placement/production.
- [ ] `APH-807` Add deterministic PlayMode flows for boarding/disembark and resource exchange.
- [ ] `APH-808` Add a scene/prefab missing-reference gate for the two enabled build scenes and runtime UI content prefabs.
- [ ] `APH-809` Add required visual captures for graphics tiers, Day/Night, static map chunks, and mip streaming. A visual task cannot pass from logs alone.
- [ ] `APH-810` When Unity MCP is connected, require agents to inspect affected scene/prefab hierarchy, console, Play Mode state, and screenshots through MCP. When unavailable, record the fallback runner and screenshots used.
- [ ] `APH-811` Add a tracked performance dashboard generated from JSON artifacts rather than manually copied metrics.

## Phase 9 - Final Closeout

Goal: prove the complete program improved production readiness without gameplay or visual regression.

- [ ] `APH-900` Run the complete first-party build matrix with 0 errors.
- [ ] `APH-901` Run architecture, ECS/Burst, focused EditMode, critical PlayMode, Match smoke, performance, GC, audio, graphics, and static-map validation gates.
- [ ] `APH-902` Produce final same-device Android development and release reports, including 10-minute thermal sessions.
- [ ] `APH-903` Compare final architecture/performance metrics against this baseline and write `Design/AgentReports/architecture_performance_hardening_final_report.md`.
- [ ] `APH-904` Re-rate architecture and performance, list residual risks, update the root architecture index, and mark this tracker complete only when no required task remains.

## Stable Integration and Push Gate

Run this gate after each implementation slice and before changing its task status to `[x]`:

1. Inspect `git status --short` and the complete diff. Identify task-owned files and preserve all unrelated work.
2. Run `git diff --check`.
3. Build the owning assembly and every directly affected first-party dependent assembly with `0` errors. For shared runtime or composition changes, include `Game.Runtime`, `Game.Editor`, and the relevant test assembly at minimum.
4. Run every focused test and architecture gate required by the task. Unity-required slices must reach their execute method and required pass marker; a licensing failure is handled through the documented wrapper/escalation retry first.
5. Inspect the relevant Unity log or live Editor console for `error CS`, compiler failure, unhandled exception, test failure, or asset import failure attributable to the slice. Existing unrelated warnings must be categorized in the handoff.
6. Stage only task-owned implementation files, tests, generated evidence, and the coordinated tracker update. Review `git diff --cached --check` and `git diff --cached --stat` before committing.
7. Create one focused commit for the stable slice. Do not use the commit as a checkpoint for broken compilation or incomplete acceptance criteria.
8. Fetch the latest remote branch before pushing. If it advanced, inspect overlap and rebase or integrate without overwriting other work. Rerun affected validation after any conflict resolution or content-changing integration.
9. Push the current branch and confirm local/remote divergence is zero. Record the final commit hash and branch in the Implementation Log.

If any required check fails, do not commit or push the slice. Keep the task `[~]`, or mark it `[!]` only with the exact blocker and next unblocking action. A documentation-only coordination change may be committed separately when `git diff --check` passes and it does not claim implementation acceptance.

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
- Stable commit/push: `<final hash>` pushed to `<branch>`, or `not pushed` with reason
- Files changed: `path`, `path`
- Behavior preserved/changed: exact statement
- Validation: command plus required pass marker; include Stable Integration and Push Gate result
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

## Decision Log

Append approved deviations here. Do not silently alter Fixed Architecture Decisions.

| Date | Decision ID | Decision | Reason | Evidence/approval |
|---|---|---|---|---|
| 2026-07-09 | `D-001` through `D-011` | Initial implementation decisions accepted as the audit baseline | Convert the audit into deterministic implementation work | This tracker |
| 2026-07-09 | `D-012` | Reconcile execution to `a6af1335c` while retaining historical audit evidence | The tracker entered `main` before the ARIA and FPS commits were integrated | Commit history and current source inspection |
| 2026-07-09 | `D-013` through `D-016` | Adopt coordinator-owned tracker integration, disjoint worker ownership, an exclusive Unity lease, and Phase 0 parallel intake | Multiple agents can reduce elapsed time only when shared-state and Unity collisions are controlled | User approval and Multi-Agent Operating Model |
| 2026-07-09 | `D-017` | Commit and push each accepted implementation slice only after the stable integration gate passes | Keep `main` usable throughout the program and prevent broken or unrelated work from entering task commits | User direction and Stable Integration and Push Gate |
| 2026-07-09 | `D-018` | Accept `APH-007` as a reproducible baseline while keeping its 1,024-byte GC gate red and unchanged | Baseline capture must freeze measured debt before optimization; requiring a green result would optimize before establishing the baseline | Raw-sample metadata report, 16 focused tests, and coordinator review |
