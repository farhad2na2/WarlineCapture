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
| `D-019` | Reject direct 96 m and 32 m transient Match mesh-combine output. Phase 6 uses shared-mesh, overlay-safe presentation chunks and measured GPU culling instead. |
| `D-020` | APH-701/702 freeze the verified `9280ead85` baseline of 265 helper paths, 108 production files above 500 lines, and 27 production files above 1,000 lines. Shrinkage passes; additions or growth require an exact tracker task and Decision Log exception. |
| `D-021` | Static-map structural parity keeps object/asset/state identity exact while allowing only Unity serialization precision of `0.0005` for transforms and `0.005` for derived world bounds. |

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
| Checklist complete | `80 / 106` |
| Checklist percent complete | `75.5%` |
| Checklist in progress | `11 / 106`: `APH-311`, `APH-408`, `APH-501`, `APH-502`, `APH-507`, `APH-508`, `APH-509`, `APH-601`, `APH-609`, `APH-704`, `APH-803` |
| Complete plus active coverage | `91 / 106` (`85.8%`); this is visibility only, not accepted completion |
| Current phase | Phase 3 Android device evidence, Phase 5 product budgets, Phase 6 map closeout, and bounded Phase 8 automation |
| Current task | Close remaining FPS, memory, startup, package, architecture, and device gates; select the next bounded decomposition only when dependency-ready |
| Red architecture gates | `0`: UI boundary `31/31` and ECS/Burst hot-path `10/10` pass on the integrated head |
| Red performance gates | `1`: steady-state Match GC `234,324 / 1,024` bytes; improved 13.0% from the APH-007 baseline without weakening the budget |
| Red visual gates | `1`: 23:00 Match capture is nonblank but battlefield readability remains too dark for final visual acceptance |
| Last verified commit | `5680b8ffe`; combat observation assembly split and integrated architecture evidence are committed on `main` |
| Last update | 2026-07-11 - transport boarding is decomposed into sub-500-line partials with exact method-token parity and green behavior, scenario, performance, source-growth, Burst, and boundary gates |

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
- [~] `APH-408` Run `AudioConfigContractTests.RunFocusedValidation`, `AudioPerformanceValidationTests.RunFocusedValidation`, audio scene binding tests, and an Android audible smoke pass.
  - Active result: config/importer `14/14`, performance `4/4`, and scene/listener binding `6/6` pass. A fresh full-policy APK reached Match, remained alive for 90 seconds, produced non-zero AudioTrack output amplitude, emitted no listener/clip/crash/ANR error, and added zero primary mixer underruns or delayed writes.
  - Remaining: record a human speaker/headphone listening confirmation for clarity, clipping, and perceived glitches. Objective evidence: `Design/AgentReports/2026-07-11_aph-408_full_voice_android_smoke.md`.

## Phase 5 - Texture, Animation, Build, and Memory Policy

Goal: make residency and package size intentional while preserving visible quality.

- [x] `APH-500` Produce a BuildReport-based top-100 included-asset table for Android APK and AAB builds.
  - Tooling: release APK/AAB builds now request `DetailedBuildReport`, deterministically aggregate `BuildReport.packedAssets`, preserve attributed/unattributed/overhead accounting, and write top-100 JSON/Markdown evidence with commit, dirty-state, architecture, artifact size, and SHA-256. Jenkins archives each report with its matching artifact.
  - Validation: focused aggregation/evidence contract `6/6`, Unity compile clean, `git diff --check` clean; tooling commit `671a009aa` pushed to `main`.
  - Evidence: clean ARM64 IL2CPP release APK and AAB reports were generated and reviewed. APK artifact `463,359,198` bytes with SHA-256 `cb18f212...824f20`; AAB artifact `426,399,778` bytes with SHA-256 `c03558f2...7ac29`; evidence commits `a527e151e` and `ddfca3b27` are pushed to `main`.
- [~] `APH-501` Add tracked budgets for APK/AAB size, installed size, peak allocated memory, texture memory, mesh memory, audio memory, and graphics-driver memory.
  - Active result: schema 3 pins clean ARM64 IL2CPP release budgets at APK `<= 463,359,198` bytes and AAB `<= 426,399,778` bytes, including exact artifact commit, SHA-256, immutable evidence commit, and report path. Installed size and absolute peak/texture/mesh/audio/graphics-driver memory limits remain fail-closed `null` until accepted release-device captures provide the required measurements.
- [~] `APH-502` Classify texture importers into UI, world albedo, world normal/mask, VFX, impostor/atlas, generated source/reference, and excluded/unreferenced groups.
  - Active result: deterministic tooling provisionally classifies all 3,464 tracked importers by semantic path/YAML evidence and exposes 105 overlaps. Final inclusion/exclusion buckets remain explicitly unaccepted until a clean same-revision Unity residency inventory and complete BuildReport texture-path export are available.
- [x] `APH-503` Add an editor guard preventing mip streaming on UI, font, animation-data, sprite-atlas, and generated reference/source textures.
  - Result: an editor `AssetPostprocessor` disables mip streaming only for protected UI/sprite, font, animation-data, sprite-atlas, and generated reference/source classes; malformed paths fail closed, the measured runtime impostor path remains explicitly allowed, and no recursive reimport API or unrelated importer mutation was added.
  - Validation: focused policy tests pass `18/18`, editor/test assemblies compile with zero errors, and the final Android profiler build completed after the one-time importer refresh.
- [ ] `APH-504` Enable mip streaming for a representative world-texture subset with mipmaps; set a mobile memory budget and preserve full-resolution nearby textures.
- [ ] `APH-505` Capture identical near, medium, and far camera screenshots before and after streaming. Reject blur, late pop, terrain seams, or missing vegetation detail.
- [ ] `APH-506` Measure memory and I/O during a 10-minute camera-pan/zoom session. Expand streaming only after the pilot passes.
- [~] `APH-507` Audit Android texture overrides for ASTC size/quality and remove oversized 4K/8K limits only where the BuildReport proves inclusion and visual evidence proves no loss.
  - Active result: historical clean-build evidence identifies 13 included Polygon Military 4K textures totaling `135.34 MiB` packed and no confirmed 8K texture. Explicit override/max-size settings and current-revision inclusion remain unproven, so no importer was changed. Evidence: `Design/AgentReports/2026-07-11_aph-507_android_texture_override_audit.md`.
  - Remaining: export complete current-revision Android importer settings and run the bounded single-texture 4K-to-2K device/visual pilot before accepting any override.
- [~] `APH-508` Audit the six generated animation textures for actual runtime residency, duplication, clip coverage, precision requirements, and unload behavior. Do not change format based only on source file size.
  - Active result: deterministic audit evidence covers six textures totaling `100,663,296` payload bytes, finds no exact pixel duplicates, identifies only the three `CharactersBaked` textures in Android build evidence at approximately `48 MiB` packed, and records 430 animation entries across 33 character animators (`67.95%` current coverage). Linear point-filtered `RGBAHalf` remains unchanged because lower precision is not proven safe.
  - Remaining: measure device residency, CPU-copy retention, and post-unload memory, then establish or reject an explicit production release path. Evidence: `Design/AgentReports/2026-07-11_aph-508_animation_texture_audit.md`.
- [~] `APH-509` Remove unused packages only after a package-usage report proves no source, serialized asset, build script, or editor workflow dependency.
  - Active result: a deterministic read-only inventory classifies 46 manifest packages, one embedded depth-zero manifest discrepancy, 20 ordinary lock-only transitives, 14 static candidate-unused packages, and five unresolved static blind spots across source, serialized assets, build scripts, and editor workflows. No package removal is accepted until isolated import/compile/test/build/device validation proves it safe.
  - Validation: six dedicated inventory tests plus syntax and deterministic-report checks pass; evidence commit `e9dcf183e` is pushed to `main`.
- [ ] `APH-510` Rebuild Android, compare BuildReport/memory/frame metrics, and record per-category deltas.

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
  - Remaining: record exact CPU/GPU mesh memory and peak startup allocation for the accepted implementation. Preserve overlay-sensitive source geometry and require foreground load, settled memory, visual, and 60 FPS evidence before closing APH-601.
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
  - Remaining: cold-device repeat, release package/startup/memory evidence, canonical/runtime-batched comparison normalization, and full visual matrix before final acceptance.
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
- [~] `APH-704` Design and benchmark a shared read-only unit spatial index for building defense, AI targeting, threat detection, and selection candidates. Adopt it only when it beats the Phase 2 direct-query baseline and preserves results.
  - Active result: an unmanaged `ISystem` shadow foundation preserves global ECS query order and domain-owned scoring, stays `[DisableAutoCreation]`, and adds no production Match work before adoption. Fixed-edge plus 100-seed equivalence coverage and alternating 16/32/64-cell benchmarks pass `8/8` with zero managed allocation. The final run measured direct/indexed build-plus-query totals of `492.107/509.287 ms` at 16 cells, `455.846/177.372 ms` at 32 cells, and `431.473/98.957 ms` at 64 cells; payloads were `243,968`, `96,512`, and `59,648` bytes respectively.
  - Evidence: `Design/AgentReports/2026-07-11_aph-704_shared_unit_spatial_index_plan.md` and `/private/tmp/warline-aph704-shadow-tests-r3.xml`. Remaining work is one-consumer-at-a-time production migration, repeated paired evidence, no-consumer-regression proof, and the frozen-fixture Match p95 gate; no production consumer has changed.
- [x] `APH-705` Extract combat contracts/data required for a future `Game.Runtime.Combat` assembly without introducing a dependency cycle.
  - Result: shared observation ECS data remains in `Game.Components`; the implementation seam is isolated behind the cycle-free `Game.Runtime -> Game.Runtime.Combat -> Game.Components` direction, and deferred combat dependencies are documented. Evidence: `Design/AgentReports/2026-07-11_aph-705_combat_contract_extraction_plan.md`.
- [x] `APH-706` Split one cohesive combat slice and pass the full assembly boundary/build/test matrix before opening another domain split.
  - Result: `CombatDamageObservationBootstrapSystem` and `CombatDamageObservationUtility` now live in the dedicated combat assembly with their source GUID preserved. The regenerated dependency report contains the intended runtime/test edges and no reverse combat-to-runtime edge. Assembly boundary `31/31`, ECS/Burst `10/10`, source growth `15/15`, combat observation `9/9`, unit combat `3/3`, building defense `13/13`, ground missile `5/5`, air missile, and narrative regression validations pass with zero compiler errors. Evidence: `Design/AgentReports/2026-07-11_aph-706_combat_observation_assembly_split.md`.
- [x] `APH-707` Decompose `TransportBoardingCommandSystem` by its existing planning/routing/application seams while preserving public behavior and scenario tests.
  - Result: the original 3,226-line unmanaged owner is now a 181-line shell plus nine cohesive partials between 241 and 416 lines. Ordered partial reconstruction exactly matches all 16,571 original implementation tokens. Transport `78/78`, selection `60/60`, scenario `15/15`, extraction `33/33`, DR-001 `2/2`, gateway PlayMode `1/1`, performance/allocation, source growth `15/15`, ECS/Burst `10/10`, and assembly boundary `31/31` validations pass with zero compiler errors. Evidence: `Design/AgentReports/2026-07-11_aph-707_transport_boarding_decomposition.md`.
- [ ] `APH-708` Decompose `UiShellEcsGateway` into route, read-model, action, and settings adapters behind existing contracts; views remain passive.
- [ ] `APH-709` Decompose selection/HUD files only where profiler or change-frequency evidence identifies risk; do not split for line count alone.
- [ ] `APH-710` Remove classified hidden world lookups and recurring per-frame allocation owners as each domain receives explicit World/EntityManager/query dependencies or cached non-allocating state.
- [ ] `APH-711` Re-run compile time, domain reload time, architecture gates, focused tests, Match performance, and the unchanged steady-state GC capture after every physical asmdef split and final allocation-remediation slice.

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
  - Active result: the fail-closed non-Unity evidence layer now enforces the reference device/APK/revision/build profile, exactly five cold and five warm starts, a 60-second warmup, 600-second sustained run with at least 9,000 structured samples, recomputed percentiles, peak memory, zero thermal/cooling status, hashed recorder/log/screenshot evidence, PNG dimensions, process survival, and raw crash scanning. It intentionally reports `acceptanceReady=false` while p99 and startup limits remain unset; focused contracts pass `17/17`.
  - Evidence: `Design/AgentReports/2026-07-11_aph-803_android_development_gate_plan.md` and `Tools/CI/android_development_performance_gate.py`. Remaining work is the flag-gated runtime recorder, Match-ready timestamp, accepted p99/startup limits, and live reference-device proof.
- [ ] `APH-804` Add Android release-build acceptance for the documented 30 FPS tier; keep 60 FPS as a separate high-end target.
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
- [x] `APH-810` When Unity MCP is connected, require agents to inspect affected scene/prefab hierarchy, console, Play Mode state, and screenshots through MCP. When unavailable, record the fallback runner and screenshots used.
  - Result: fail-closed JSON schema, validator, and report template require exactly one evidence path. MCP evidence must name hierarchy, console, Play Mode, and screenshot tools; fallback evidence must record the failed probe/reason, exact runner command, log path, pass marker, and screenshots, with optional on-disk artifact/marker verification.
  - Validation: focused contract tests pass `12/12`; Python compilation, JSON schema parsing, CLI smoke, and whitespace checks pass.
- [x] `APH-811` Add a tracked performance dashboard generated from JSON artifacts rather than manually copied metrics.
  - Result: deterministic Python tooling derives a JSON/Markdown dashboard from seven tracked architecture, build, content-residency, runtime-performance, and audio JSON artifacts. Missing, invalid, stale, current, and revision-unknown inputs remain explicit rather than silently accepted.
  - Validation: dedicated dashboard tests pass `6/6`, Python/JSON checks pass, every configured input is tracked, and consecutive generation hashes are byte-identical.

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
- Files changed: schema-3 accepted baseline JSON, product-budget validator and focused tests, and performance regression contract
- Behavior preserved/changed: tooling/config only; APK and AAB budgets now ratchet against exact clean APH-500 artifacts. Unmeasured installed size and absolute peak/texture/mesh/audio/graphics-driver memory limits remain fail-closed `null`.
- Validation: `[PerformanceProductBudgetValidation] result=Passed tests=5`, Game Editor and Editor-test assemblies plus the complete 12-assembly matrix build with zero errors, and `git diff --check` passes
- Residual risk: accepted release-device captures are still required before absolute installed-size and memory limits can replace measurement-required states
- Next action: collect the required release artifact/device/scenario/profiler metrics under the serialized Android evidence lane

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
