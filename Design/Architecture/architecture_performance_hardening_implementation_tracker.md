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
| Current program status | Active - execution baseline reconciled; Phase 0 parallel intake ready |

### Protected Integrated Work

The following work belonged to the user when this tracker was created and is now integrated in commits `76d098a9a` and `845a56eb8`. Agents must preserve it and must not revert, overwrite, reformat, or include it in unrelated commits:

- `Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.cs`
- `Assets/Tests/Editor/MatchHudAssistantUiSystemHelperTests.cs`
- `Design/VisualLockLayered/POP-13_ARIACommandAssistant/`

If a tracker task genuinely needs one of these files, read and integrate the existing changes. Record the overlap in the task handoff before editing.

### Baseline Reconciliation

The tracker was authored at `b66453e97`, then entered `main` through `68b2da500`. Before implementation began, `main` also received the protected ARIA work and the match CPU performance repair in `a6af1335c`. The current execution baseline is therefore `a6af1335c`, not the original audit commit.

The FPS repair changed `BuildingDefenseAttackSystem` and related presentation, audio, warning, settings, diagnostics, and validation code. It reduced the focused 4,800 x 2,160 Editor benchmark with 733 units to an average `8.893 ms`, median `8.242 ms`, and p95 `12.516 ms`. This is accepted current-head evidence, but it does not complete Phase 2B: the system still creates one entity snapshot and three component snapshots during its throttled 0.12-second target-acquisition pass. `APH-205` through `APH-213` remain pending until their tracker-specific acceptance criteria pass.

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
| Checklist complete | `8 / 106` |
| Checklist percent complete | `7.5%` |
| Checklist in progress | `1 / 106`: `APH-007` |
| Complete plus active coverage | `9 / 106` (`8.5%`); this is visibility only, not accepted completion |
| Current phase | Phase 0 - Baseline and safety freeze |
| Current task | `APH-007` GC attribution repair validation and fresh 300-frame capture; Unity lease held by Performance agent |
| Red architecture gates | `2` at the audit baseline; current-head reproduction remains required |
| Last verified commit | `929ab7bb3` for `APH-008`; full 12-assembly `APH-006` matrix passed at `7084805d7` |
| Last update | 2026-07-09 - `APH-006` and `APH-008` accepted; `APH-007` remains active |

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
- [~] `APH-007` Capture a fresh pre-change Match performance baseline on the current commit.
  - Initial owner: Performance agent. Requires the exclusive Unity lease.
  - Run the editor regression baseline and steady-state GC capture.
  - Preserve the generated JSON/Markdown artifacts before optimization.
  - Acceptance: capture includes at least 700 units, 600 runtime buildings, 180 warmup frames, and 300 measured frames.
- [x] `APH-008` Add a machine-readable content-residency inventory report.
  - Initial owner: Residency agent. File/dependency inventory may run in parallel; serialized importer inspection requires the exclusive Unity lease.
  - Required fields: build-included asset path, type, source size, imported size where available, dependency root, audio load type, texture dimensions/format/mipmap/streaming state, mesh read/write state, and animation texture payload.
  - Output: `Design/AgentReports/architecture_performance_content_residency_baseline.json` plus Markdown summary.
  - Result: 4,099 dependency-root-reachable assets across 12 roots; 2,680 imported-size measurements; 226 audio clips; 637 textures; 1,817 meshes; and 3 reachable animation textures with 50,331,648 payload bytes.
  - Validation: generator and 4 focused tests passed; `Game.Editor` and `Game.Tests.Editor` built with 0 errors; commit `929ab7bb3` pushed to `main`.
- [ ] `APH-009` Freeze initial product budgets in a tracked config.
  - Initial owner: Budget analysis agent for the draft; coordinator owns approval and integration. Depends on accepted `APH-007` and `APH-008` evidence.
  - Keep existing Android p95 budgets: `<33 ms` baseline/recommended and `<25 ms` high-end.
  - Add same-device peak allocated-memory, APK/AAB, installed-size, startup-time, and visual-quality evidence requirements.
  - Initial memory improvement target: at least 10 percent below the validated same-device baseline; do not assert an absolute limit until `APH-008` identifies residency owners.

## Phase 1 - Restore the UI Assembly Boundary

Goal: remove `Game.Configs` from `Game.UI.Runtime` while preserving every configured string and fallback.

Implementation contract:

- Add `IGameTextResolver` to `Game.UI.Contracts` with non-Unity-facing `Get`, `TryGet`, and formatting behavior needed by current UI callers.
- Add an immutable fallback resolver in `Game.UI.Contracts` for unbound prefab previews and focused tests.
- Add the concrete adapter in `Game.Composition`; it may call `Game.Configs.GameText` because composition already owns cross-assembly wiring.
- Inject the resolver through existing UI bootstrap/runtime adapter binding. Do not create a static mutable UI registry.
- Migrate only files compiled by `Game.UI.Runtime` in this phase. `Game.UI.Shell.Ecs` and gameplay-system text ownership are separate later decisions.

- [ ] `APH-100` Add focused tests for resolver fallback, configured text, formatting success, invalid-format fallback, and missing-key fallback before production migration.
- [ ] `APH-101` Add `IGameTextResolver` and immutable fallback implementation under `Assets/Game/Scripts/UI/Contracts`.
- [ ] `APH-102` Add the composition-owned `GameTextResolverAdapter` wrapping the existing config-owned text source.
- [ ] `APH-103` Inject the resolver through Menu and Match UI runtime adapters without hierarchy search or static registration.
- [ ] `APH-104` Migrate the seven known `Game.UI.Runtime` text consumers listed in Current Red Gates.
  - Preserve every key and fallback string from commit `caaafe339`.
  - Preserve formatting culture and error fallback behavior.
- [ ] `APH-105` Remove `using Game.Configs` from files compiled by `Game.UI.Runtime`.
- [ ] `APH-106` Remove `Game.Configs` from `Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef`.
- [ ] `APH-107` Add a guard that enumerates UI runtime source files and fails on future `Game.Configs` imports or direct `GameText` calls.
- [ ] `APH-108` Run UI text focused tests, `Game.UI.Runtime`, `Game.Composition`, and `Game.Tests.Editor` builds, then run the 31-check assembly-boundary validation.
  - Acceptance: `[ScriptArchitectureBoundaryValidation] result=Passed tests=31`.

## Phase 2 - Restore the ECS Hot-Path Gate

Goal: remove all remaining synchronous hot-path query snapshots without changing gameplay behavior or losing the measured FPS repair.

### Phase 2A - Resource Exchange

Implementation contract:

- Replace `exchangeQuery.ToEntityArray(Allocator.Temp)` with direct `SystemAPI.Query` iteration over the required component and buffer set.
- Preserve support for multiple exchange entities and deterministic per-entity processing.
- Preserve request order, result order, wallet mutation, queue mutation, economy events, and summary publication.

- [ ] `APH-200` Add a regression with two exchange entities proving both queues process in one update.
- [ ] `APH-201` Add or extend zero-allocation coverage for a warmed resource-exchange update.
- [ ] `APH-202` Replace the temporary entity array with direct ECS query iteration.
- [ ] `APH-203` Confirm no new `EntityManager.CreateEntityQuery`, structural sync point, managed allocation, or singleton assumption was introduced.
- [ ] `APH-204` Run `ResourceExchangeRequestValidationSystemTests.RunFocusedValidation` and `ResourceExchangeGcAllocationValidationTests.RunFocusedValidation`.

### Phase 2B - Building Defense

Current-head overlap:

- Commit `a6af1335c` changed this system to acquire targets every 0.12 seconds, copy query data into contiguous arrays, and preserve per-frame firing cooldown updates. It also added focused performance diagnostics and supporting tests.
- That repair is the performance floor for this phase. Preserve or improve its focused benchmark and do not restore per-frame target acquisition or random per-candidate `EntityManager` reads.
- The current implementation still uses one entity snapshot and three component snapshots during target acquisition, so it does not satisfy `APH-208` or the zero-snapshot architecture gate.

Immediate implementation contract:

- Add a persistent native target scratch collection owned and disposed by `BuildingDefenseAttackSystem`.
- Rebuild it through direct ECS iteration without per-frame allocator use.
- Preserve the current target eligibility, nearest-target ordering, faction filtering, air-target exclusion, four-slot behavior, cooldowns, tracer cadence, damage, audio, VFX, and recent-attacker/health-bar effects.
- Remove explicit `state.Dependency.Complete()` only when lookups/query dependencies make direct access safe. Do not hide the sync in another helper.
- This immediate phase does not introduce the shared spatial index; that is `APH-704`.

- [ ] `APH-205` Extend defense tests for neutral targets, aircraft exclusion, nearest-hostile ordering, four concurrent slots, destroyed targets, and target removal between updates.
- [ ] `APH-206` Add a warmed allocation test covering at least 32 towers and 740 candidate units.
- [ ] `APH-207` Add persistent target scratch storage with explicit `OnCreate`/`OnDestroy` lifetime.
- [ ] `APH-208` Replace `_targetQuery.ToEntityArray(Allocator.Temp)` and remove the per-frame snapshot.
- [ ] `APH-209` Audit the explicit dependency completion and direct `EntityManager` calls; remove or document each unavoidable synchronization point with profiler evidence.
- [ ] `APH-210` Add profiler markers for target collection, target selection, and effect application so the later spatial-index phase has comparable metrics.
- [ ] `APH-211` Run defense, automatic-faction-targeting, combat, audio, and VFX focused validations.
- [ ] `APH-212` Run `EcsBurstHotPathArchitectureTests.RunFocusedValidation`.
  - Acceptance: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10` and zero snapshot debt.
- [ ] `APH-213` Re-run editor performance and steady-state GC baselines; reject the slice if frame p95 or player-relevant GC regresses beyond noise.

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

- [ ] `APH-300` Add tests proving persisted settings are applied during app startup without opening the settings popup.
- [ ] `APH-301` Add Android tests proving 120 FPS is never applied on Android and 30/60 remain valid.
- [ ] `APH-302` Add an editor/standalone test preserving 120 FPS where supported.
- [ ] `APH-303` Change mobile defaults from 120 FPS to 60 FPS and clamp saved legacy 120 FPS values on Android.
- [ ] `APH-304` Apply `SettingsService.Load()` through the composition startup path exactly once after required runtime owners exist.
- [ ] `APH-305` Route settings-change events through composition to `VisualQualitySettingsSystem`; ensure subscriptions are removed on shutdown/domain reload.
- [ ] `APH-306` Give `VisualQualitySettingsSystem` an explicit apply-on-change API and remove its manual per-frame `Update` call from Match bootstrap.
- [ ] `APH-307` Split static tier application from dynamic environment application. After initialization, visual-quality code must not overwrite Day/Night sun, ambient, fog, skybox, or volume values every frame.
- [ ] `APH-308` Add an ownership regression that advances Day/Night, runs visual-quality handling, and proves the Day/Night values remain authoritative.
- [ ] `APH-309` Verify Low, Balanced/High, and Ultra mappings, including render pipeline, render scale, post-processing, AA, and shadow strength.
- [ ] `APH-310` Run `SettingsPopupValidationTests.RunFocusedValidation`, `AndroidVisualQualityValidationTests.RunFocusedValidation`, Match smoke, and visual captures at day, dusk, and night.
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

- [ ] `APH-700` Add a generated domain dependency report listing every first-party assembly edge and top cross-domain type references.
- [ ] `APH-701` Freeze new `*SystemHelper` and `*CompositionSystemHelper` files unless the architecture guard contains an approved task ID from this tracker.
- [ ] `APH-702` Add a size guard for new or growing production files: review required above 500 lines; no existing file above 1,000 lines may grow without an explicit tracker exception.
- [ ] `APH-703` Inventory `World.DefaultGameObjectInjectionWorld` runtime call sites and classify composition edge, authoring/debug, presentation edge, or hidden service-locator debt.
- [ ] `APH-704` Design and benchmark a shared read-only unit spatial index for building defense, AI targeting, threat detection, and selection candidates. Adopt it only when it beats the Phase 2 direct-query baseline and preserves results.
- [ ] `APH-705` Extract combat contracts/data required for a future `Game.Runtime.Combat` assembly without introducing a dependency cycle.
- [ ] `APH-706` Split one cohesive combat slice and pass the full assembly boundary/build/test matrix before opening another domain split.
- [ ] `APH-707` Decompose `TransportBoardingCommandSystem` by its existing planning/routing/application seams while preserving public behavior and scenario tests.
- [ ] `APH-708` Decompose `UiShellEcsGateway` into route, read-model, action, and settings adapters behind existing contracts; views remain passive.
- [ ] `APH-709` Decompose selection/HUD files only where profiler or change-frequency evidence identifies risk; do not split for line count alone.
- [ ] `APH-710` Remove classified hidden world lookups as each domain receives explicit World/EntityManager/query dependencies.
- [ ] `APH-711` Re-run compile time, domain reload time, architecture gates, focused tests, and Match performance after every physical asmdef split.

## Phase 8 - CI, PlayMode, Visual, and Device Gates

Goal: prevent recurrence and cover behavior that source-scanning tests cannot prove.

- [ ] `APH-800` Add both architecture execute-method validations to CI and fail the build on nonzero exit or missing pass marker.
- [ ] `APH-801` Add the editor Match performance baseline and steady-state GC capture to a scheduled or pre-merge performance lane.
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

## Decision Log

Append approved deviations here. Do not silently alter Fixed Architecture Decisions.

| Date | Decision ID | Decision | Reason | Evidence/approval |
|---|---|---|---|---|
| 2026-07-09 | `D-001` through `D-011` | Initial implementation decisions accepted as the audit baseline | Convert the audit into deterministic implementation work | This tracker |
| 2026-07-09 | `D-012` | Reconcile execution to `a6af1335c` while retaining historical audit evidence | The tracker entered `main` before the ARIA and FPS commits were integrated | Commit history and current source inspection |
| 2026-07-09 | `D-013` through `D-016` | Adopt coordinator-owned tracker integration, disjoint worker ownership, an exclusive Unity lease, and Phase 0 parallel intake | Multiple agents can reduce elapsed time only when shared-state and Unity collisions are controlled | User approval and Multi-Agent Operating Model |
| 2026-07-09 | `D-017` | Commit and push each accepted implementation slice only after the stable integration gate passes | Keep `main` usable throughout the program and prevent broken or unrelated work from entering task commits | User direction and Stable Integration and Push Gate |
