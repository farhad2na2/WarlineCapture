# Post-Hardening Architecture Maturity Tracker

## Purpose

Move WarlineCapture from the approximately `8.8 / 10` architecture expected after the current Architecture and Performance Hardening tracker reaches `107 / 107` to a practical, evidence-backed `9.5+ / 10` production architecture.

This is a post-hardening program, not a replacement for `architecture_performance_hardening_implementation_tracker.md`. The prerequisite tracker closed its early-development scope with `107 / 107` explicit dispositions, including 15 unpassed release-only certifications transferred to `pre_release_performance_certification_backlog.md`. This maturity program must not begin until that backlog is activated and accepted, in addition to every compiler, architecture, performance, GC, memory, visual, and device gate required by the original entry contract being genuinely green.

When this program activates, every task must follow `Design/Architecture/agent_pull_request_review_merge_workflow.md` for shared-object worktrees, `codex/<task-id>-<slug>` branches, implementation ownership, independent findings-first review, risk-based integration, tracker administration, PR merge, and cleanup. This tracker remains authoritative for maturity scope, dependencies, acceptance, and evidence; the PR workflow is authoritative for git integration and role separation.

A literal `10 / 10` is treated as a sustained operating standard, not a one-time checkbox. The program can establish `9.5+` quality and the controls needed to retain it; production history must supply the final evidence.

## Authority And Entry Contract

| Field | Value |
|---|---|
| Document date | 2026-07-13 |
| Prerequisite tracker | `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` |
| Git integration authority | `Design/Architecture/agent_pull_request_review_merge_workflow.md` |
| Required prerequisite state | Early-development tracker complete plus `pre_release_performance_certification_backlog.md` accepted, with no waived or stale required gate |
| Assumed entry rating | approximately `8.8 / 10` |
| Practical target | evidence-backed `9.5+ / 10` |
| Aspirational target | sustained `10 / 10` operating standard |
| Program status | Planned; inactive because release certification is explicitly deferred |

The entry review must reject a nominal `107 / 107` disposition when any required evidence is deferred, missing, malformed, stale, tied to an unknown commit, or passed by weakening a budget or allowlist. The current `92` accepted plus `15` deferred hardening closeout is intentionally not sufficient to activate this program.

## Rating Model

The final rating is an engineering assessment supported by the gates below. It is not calculated by averaging arbitrary percentages or by reducing file sizes without improving ownership.

| Area | Entry expectation | Target expectation |
|---|---:|---:|
| ECS and runtime ownership | `9.2` | `9.6+` |
| Performance and GC discipline | `9.3` | `9.7+` |
| Modularity and dependency boundaries | `8.5` | `9.4+` |
| UI and presentation architecture | `8.2` | `9.4+` |
| Lifecycle and resource safety | `8.6` | `9.6+` |
| Maintainability and testability | `8.7` | `9.5+` |
| Android production evidence | `9.0` | `9.6+` |
| Sustained release governance | unproven | `9.5+` |

## Status Rules

- `[ ]` pending
- `[~]` in progress; only one task may be claimed by one agent at a time
- `[x]` complete with required evidence recorded in this document
- `[!]` blocked with the exact blocker, owner, and next unblocking action recorded
- A task is not complete because code compiles or a file became smaller.
- Every implementation task requires behavior-preservation evidence, focused tests, architecture checks, and relevant performance/GC checks.
- Never raise a budget, add an allowlist exception, or discard an allocation sample merely to make a gate green.
- Missing, malformed, unresolved, or commit-unknown evidence fails closed.
- Preserve Unity `.meta` files, serialized references, ECS update order, and existing user-owned work.
- Do not combine balance changes, art changes, or unrelated gameplay features with architecture slices.
- New findings do not silently increase the active checklist. Record them in the Decision Log and obtain explicit scope approval.
- Implementation agents push their task branch and open a PR but never merge or self-accept tracker state. The independent review/merge coordinator owns findings, final integration gates, administrative tracker/evidence commits, merge, and branch/worktree cleanup.
- Direct pushes may still be technically possible, but this planned program uses PRs. Do not claim branch protection/rulesets are active or require GitHub approval counts while all agents share one GitHub identity.
- Preserve Jenkins and existing CI/performance contracts; do not introduce GitHub Actions as a substitute.

## Global Architecture Guardrails

- Keep simulation state and gameplay authority in ECS systems and components.
- Keep MonoBehaviours limited to serialized binding, platform integration, camera ownership, input/event forwarding, and presentation application.
- Do not introduce parallel gameplay truth in managed views, static services, or presentation pools.
- Do not introduce new service locators, implicit `World.DefaultGameObjectInjectionWorld` dependencies, or mutable static state that survives a World lifecycle.
- One state transition has one owner. A decomposition may delegate work but must not duplicate authority.
- Prefer Burst-capable `ISystem` and native data for runtime hot paths where the ownership boundary permits it.
- Managed ECS gateways may project data, but managed string/object construction must occur only when source versions change.
- No new production `Update`, `LateUpdate`, or polling loop without an explicit owner, measured need, and architecture registration.
- Persistent native memory must have an explicit creator, capacity policy, disposer, and lifecycle test.
- Pool growth and exhaustion behavior must be bounded and testable.
- Maintain or improve the accepted frame-time, GC, memory, package-size, and visual-quality budgets.

## Progress Snapshot

| Field | Status |
|---|---|
| Checklist complete | `0 / 86` |
| Program state | Planned and gated by the prerequisite tracker |
| Current phase | None |
| Current task | None |
| Entry review | Pending |
| Architecture rating | Not re-audited for this program |
| Performance evidence | Must be recaptured at the exact entry commit |
| Android evidence | Must be recaptured at the exact entry commit |
| Sustained release evidence | `0 / 3` qualifying release candidates |

## Phase Dependencies

| Phase | Depends on | May overlap with |
|---|---|---|
| Phase 0 - Entry Baseline And Scorecard | prerequisite tracker closed and pre-release certification backlog accepted | none |
| Phase 1 - Responsibility And Decomposition Hardening | Phase 0 | Phase 2 after inventories are complete |
| Phase 2 - World Lifecycle And Dependency Hardening | Phase 0 | Phase 1 |
| Phase 3 - Allocation-Free UI Projection | Phases 0 and 2 cache/lifecycle contract | Phase 4 |
| Phase 4 - Presentation Pool And Creation Hardening | Phases 0 and 2 | Phase 3 |
| Phase 5 - Determinism And Failure-Path Proof | Phases 1 and 2 | late Phase 3 or 4 |
| Phase 6 - Android Production Proof | Phases 3, 4, and 5 | Phase 7 |
| Phase 7 - Production-Safe Diagnostics | Phase 0 | Phases 1 through 6 with file claims |
| Phase 8 - Continuous Architecture Enforcement | starts after Phase 0; closes after Phases 1 through 7 | all phases |
| Phase 9 - Sustained Release Evidence | Phases 0 through 8 | none |

## Phase 0 - Entry Baseline And Scorecard

No production behavior changes. Establish a reproducible baseline and prevent an architecture score from substituting for evidence.

- [ ] `AM-001` Verify the prerequisite tracker is closed, the pre-release certification backlog is accepted, and every required gate is green without a waiver that weakens its original acceptance criteria.
- [ ] `AM-002` Record the exact entry commit, branch, Unity editor version, package lock hash, build target, scripting backend, and active quality configuration.
- [ ] `AM-003` Define canonical scenarios for idle Match, maximum combat, construction, transport, aircraft, projectiles, every major popup, Menu-to-Match transitions, and long-duration soak.
- [ ] `AM-004` Freeze the architecture scorecard, metric definitions, device tiers, performance budgets, GC classification policy, memory-growth threshold, and evidence freshness rules.
- [ ] `AM-005` Regenerate the architecture dashboard and reject every required input reported as stale, unknown, malformed, or tied to a different commit.
- [ ] `AM-006` Produce a current source-size, dependency, assembly-cycle, runtime-loop, static-state, and managed-helper inventory.
- [ ] `AM-007` Produce a lifecycle inventory for Worlds, persistent native containers, query caches, presentation pools, scene roots, event subscriptions, and static caches.
- [ ] `AM-008` Publish the entry baseline report with ratings by area, accepted evidence links, known residual risks, and the exact deltas this program owns.

### Phase 0 Exit Gate

- Every required baseline artifact identifies the exact commit and environment.
- The dashboard contains no required stale or unknown evidence.
- Each later phase has a bounded owner list and measurable acceptance criteria.

## Phase 1 - Responsibility And Decomposition Hardening

Reduce change risk in oversized systems without replacing the architecture or changing gameplay authority.

- [ ] `AM-009` Rank the largest and most coupled production files by lines, dependencies, state ownership, update-time cost, and change frequency; do not rank by lines alone.
- [ ] `AM-010` Write a responsibility map for each selected owner, including inputs, outputs, state authority, update order, side effects, tests, and allowed dependencies.
- [ ] `AM-011` Add missing characterization tests before extracting behavior from any selected owner.
- [ ] `AM-012` Extract one capability or ECS phase at a time behind existing contracts, preserving system order and avoiding a new coordinator-shaped helper.
- [ ] `AM-013` Consolidate duplicated query-cache, command-queue, fixed-capacity scratch, and projection-cache mechanics only where one narrow shared contract removes real duplication.
- [ ] `AM-014` Add update-order and behavior-equivalence tests for every decomposition that crosses a system or assembly boundary.
- [ ] `AM-015` Complete measured decomposition of the highest-risk remaining UI/presentation helper after its characterization coverage is green.
- [ ] `AM-016` Update source-growth and responsibility guardrails so extracted files cannot regrow into equivalent god owners elsewhere.
- [ ] `AM-017` Recapture focused and canonical Match performance/GC evidence after all Phase 1 integrations and reject any behavior or frame-time regression.

### Phase 1 Exit Gate

- Selected oversized owners have fewer responsibilities, not merely more files.
- No new cyclic dependency, generic service locator, duplicate state authority, or oversized replacement helper exists.
- Canonical behavior, frame time, and managed-allocation gates remain green.

## Phase 2 - World Lifecycle And Dependency Hardening

Make runtime dependencies explicit and prove that caches and native resources cannot outlive their owning World.

- [ ] `AM-018` Inventory production uses of global World lookup, mutable static caches, static event subscriptions, hidden singletons, and runtime object discovery.
- [ ] `AM-019` Define one standard World-bound query/entity cache contract covering positive lookup, negative lookup, invalidation, rebind, disposal, and destroyed-entity recovery.
- [ ] `AM-020` Move mutable runtime state that crosses World lifecycles into explicit World-owned systems, components, or lifecycle containers where practical.
- [ ] `AM-021` Give every persistent native container, query, event subscription, and presentation root an explicit creation and disposal owner.
- [ ] `AM-022` Add tests for World destruction/recreation, domain reload, scene unload/reload, missing singleton recovery, and replaced command entities.
- [ ] `AM-023` Run at least 100 automated Menu-to-Match-to-Menu cycles without duplicate systems, stale entities, retained subscriptions, or presentation-root accumulation.
- [ ] `AM-024` Add native allocation and pool-count snapshots around lifecycle stress tests and prove no upward retained-memory trend after warmup.
- [ ] `AM-025` Run the full architecture, lifecycle, compiler, and focused allocation suites and publish the Phase 2 ownership delta.

### Phase 2 Exit Gate

- Runtime systems do not depend on unregistered global lookup or World-surviving mutable state.
- Every persistent resource has one tested lifecycle owner.
- Repeated transitions produce stable entity, native-memory, subscription, and pool counts.

## Phase 3 - Allocation-Free UI Projection

Make every Canvas UI surface change-driven and allocation-free during unchanged steady state, including when popups are open.

- [ ] `AM-026` Inventory every Menu, Match HUD, drawer, popup, tooltip, selection panel, and ARIA view by polling owner, source versions, managed conversions, rebuild behavior, and allocation coverage.
- [ ] `AM-027` Add or correct ECS source versions so each UI domain changes version only when its visible semantic data changes.
- [ ] `AM-028` Standardize managed projection caches around World, boundary entity, source versions, item counts, settings versions, and explicit invalidation.
- [ ] `AM-029` Convert Resource Exchange projection to cache managed strings/models and stop rebuilding unchanged data from `FixedString.ToString()` calls.
- [ ] `AM-030` Apply the same change-driven contract to Build, building production, placement, and construction panels.
- [ ] `AM-031` Prove ARIA objectives, threats, recommendations, narration, settings, and target-lock projection only rebuild from relevant version changes.
- [ ] `AM-032` Apply the contract to single selection, multi-selection, squad tray, passenger drawer, minimap, and command-feedback panels.
- [ ] `AM-033` Apply the contract to settings, Menu, Campaign, mission briefing, pause, and end-of-match surfaces.
- [ ] `AM-034` Remove view-local polling loops where the existing shell can own one centralized refresh; register and justify any loop that must remain.
- [ ] `AM-035` Add an automated open-surface matrix that warms and measures every major UI surface, requiring zero recurring production-owned managed allocation when data is unchanged.

### Phase 3 Exit Gate

- No unchanged UI surface converts ECS fixed strings to new managed strings or reconstructs managed models every frame.
- Every major popup and HUD state has an open-state allocation test.
- UI updates remain correct after World recreation, resolution changes, localization refresh, and source-version rollover.

## Phase 4 - Presentation Pool And Creation Hardening

Remove recurring runtime creation from combat and presentation while defining bounded behavior under maximum load.

- [ ] `AM-036` Inventory every production `Instantiate`, `Destroy`, `AddComponent`, material clone, audio-source creation, list growth, and dynamic pool expansion reachable after Match warmup.
- [ ] `AM-037` Define data-backed capacities and exhaustion behavior for each pool; choose reuse, priority eviction, culling, or bounded expansion explicitly.
- [ ] `AM-038` Prewarm and validate audio source, loop, voice, and one-shot capacity for the maximum supported mix without introducing persistent irrelevant playback.
- [ ] `AM-039` Prewarm and validate projectile, missile, muzzle, trail, impact, explosion, and damage-feedback presentation.
- [ ] `AM-040` Prewarm and validate aircraft, helicopter, ground-vehicle, engine-loop, takeoff, landing, and destruction presentation.
- [ ] `AM-041` Prewarm and validate building visuals, construction, production transport, drop visuals, boarding, disembark, and airdrop presentation.
- [ ] `AM-042` Prewarm and validate UI rows, selection markers, world markers, health bars, minimap markers, command traces, and tactical overlays.
- [ ] `AM-043` Add deterministic pool-exhaustion tests that prove gameplay authority remains correct and presentation degradation follows the configured priority policy.
- [ ] `AM-044` Capture maximum-combat and maximum-creation scenarios and require zero recurring production-owned GC after pools are warm, with stable pool and native-memory counts.

### Phase 4 Exit Gate

- No unbounded pool or collection growth occurs in warmed gameplay.
- Runtime creation is either eliminated from steady state or isolated as an explicit, measured one-time transition.
- Pool exhaustion cannot alter simulation truth or create an alert/audio loop.

## Phase 5 - Determinism And Failure-Path Proof

Prove that the architecture remains correct under different frame timing, invalid state, and recovery paths.

- [ ] `AM-045` Define deterministic scenario seeds and canonical input streams for combat, AI, pathfinding, economy, construction, transport, and aircraft.
- [ ] `AM-046` Add stable simulation-state hashes at agreed checkpoints without including presentation-only or nondeterministic diagnostic state.
- [ ] `AM-047` Add record/replay validation for commands and relevant scenario inputs.
- [ ] `AM-048` Compare simulation results across supported frame rates, fixed-step catch-up patterns, and device tiers.
- [ ] `AM-049` Validate interrupted Menu/Match transitions, pause/resume, application focus loss, and scene reload against the same ownership invariants.
- [ ] `AM-050` Validate commands targeting destroyed entities, missing configs, exhausted queues, invalid factions, absent singletons, and disposed Worlds.
- [ ] `AM-051` Add invariants for unique command ownership, bounded queues, entity existence, faction authority, resource conservation, and transport occupancy.
- [ ] `AM-052` Publish a deterministic and failure-path suite that fails on divergent state, leaked ownership, unbounded retries, or uncaught lifecycle faults.

### Phase 5 Exit Gate

- Equivalent inputs produce equivalent authoritative simulation outcomes.
- Invalid or interrupted paths fail boundedly and recover without duplicate authority.
- Presentation variance does not change simulation state.

## Phase 6 - Android Production Proof

Replace editor confidence and stale device captures with current, commit-bound production evidence.

- [ ] `AM-053` Freeze supported low, recommended, and high-end Android device tiers and record OS, chipset, memory, resolution, refresh rate, and thermal environment.
- [ ] `AM-054` Embed commit, build configuration, content version, scripting backend, and quality tier into every build and profiler artifact.
- [ ] `AM-055` Automate device scenarios for idle Match, maximum combat, construction bursts, transport/aircraft, every major popup, transitions, and soak.
- [ ] `AM-056` Capture CPU, GPU, frame pacing, GC, native memory, draw/batch, thermal, and battery evidence on the low tier.
- [ ] `AM-057` Capture the same evidence on the recommended tier.
- [ ] `AM-058` Capture the same evidence on the high-end tier.
- [ ] `AM-059` Run at least one 30-minute foreground Match soak per tier after warmup.
- [ ] `AM-060` Run repeated cold-start and Menu-to-Match transition cycles per tier and inspect retained memory, loading spikes, and lifecycle counts.
- [ ] `AM-061` Reconcile device memory, package size, installed size, audio residency, texture residency, and native pool budgets against the frozen scorecard.
- [ ] `AM-062` Publish a cross-device report and fail the phase on any unexplained recurring allocation, upward retained-memory trend, thermal instability, or weakened existing frame budget.

### Phase 6 Exit Gate

- Every supported tier passes existing approved frame-time and quality budgets.
- Production-owned recurring GC is zero in covered warmed scenarios.
- Memory reaches a stable post-warmup plateau and returns to the expected baseline after transitions.
- Every artifact is reproducible from the recorded commit and build configuration.

## Phase 7 - Production-Safe Diagnostics

Retain useful observability without letting diagnostics distort performance or create false gameplay behavior.

- [ ] `AM-063` Inventory production diagnostics by call frequency, allocation behavior, thread, build availability, retention, and user-visible side effect.
- [ ] `AM-064` Gate interpolation, stack capture, payload construction, and managed formatting before work begins.
- [ ] `AM-065` Route high-frequency structured diagnostics through fixed-capacity native or preallocated ring buffers with explicit overflow behavior.
- [ ] `AM-066` Compile verbose editor/development diagnostics out of release builds while preserving actionable warnings and fatal evidence.
- [ ] `AM-067` Standardize profiler markers and allocation probes so capture instrumentation can be enabled deliberately and identified separately from production ownership.
- [ ] `AM-068` Prove normal production telemetry adds zero recurring managed allocation and remains within its CPU budget.
- [ ] `AM-069` Validate failure diagnostics under missing configuration, pool exhaustion, invalid commands, and device resource pressure.

### Phase 7 Exit Gate

- Disabled diagnostics perform no formatting or payload construction.
- Enabled production telemetry is bounded and allocation-free in steady state.
- Profiling evidence can distinguish instrumentation cost from production code without broad exclusions.

## Phase 8 - Continuous Architecture Enforcement

Move architecture quality from periodic audit work into automated change control.

- [ ] `AM-070` Fail CI on new assembly cycles, forbidden dependency directions, or domain leakage.
- [ ] `AM-071` Fail CI on unregistered runtime loops, implicit global World lookup, unapproved mutable static runtime state, or new service locators.
- [ ] `AM-072` Add lifecycle validation for persistent native resources, query caches, event subscriptions, and presentation roots.
- [ ] `AM-073` Maintain source-growth and responsibility contracts with measured ceilings and reviewed exceptions rather than raw line-count suppression.
- [ ] `AM-074` Run focused zero-allocation and frame-time validations for changed domains, including open-popup UI tests.
- [ ] `AM-075` Run canonical Editor and Android performance/device lanes on the agreed pre-merge, scheduled, and release cadence.
- [ ] `AM-076` Fail closed on missing, malformed, stale, commit-unknown, or scenario-incomplete evidence.
- [ ] `AM-077` Require every architecture exception to record owner, rationale, measured effect, approval, expiry, and removal task.
- [ ] `AM-078` Add architecture-impact and evidence fields to implementation handoffs and release-candidate review.

### Phase 8 Exit Gate

- Known architectural regressions cannot merge silently.
- CI reports the responsible file, owner contract, violated budget, and required evidence.
- Temporary exceptions expire and cannot become permanent undocumented debt.

## Phase 9 - Sustained Release Evidence

Demonstrate that the architecture remains healthy through real integration cycles rather than one controlled benchmark.

- [ ] `AM-079` Produce three consecutive release candidates from distinct integration points with every required architecture, compiler, test, GC, memory, visual, and device gate green.
- [ ] `AM-080` Keep the architecture dashboard fully fresh and commit-bound for all three candidates.
- [ ] `AM-081` Demonstrate zero recurring production-owned managed allocation in all covered warmed scenarios across all three candidates.
- [ ] `AM-082` Demonstrate stable native and managed memory plateaus in soak and repeated-transition tests across all three candidates.
- [ ] `AM-083` Close or renew every temporary exception through explicit review; no expired, unowned, or unexplained exception may remain.
- [ ] `AM-084` Pass the complete supported Android device matrix without weakening frame-time, quality, package, or memory budgets.
- [ ] `AM-085` Perform an independent final architecture re-audit, record category ratings and residual risks, and compare them with the Phase 0 scorecard.
- [ ] `AM-086` Publish the final maturity report and convert its monitoring gates into permanent project governance rather than declaring architecture work permanently finished.

### Phase 9 Exit Gate

- Three consecutive release candidates satisfy the complete scorecard.
- The independent re-audit supports at least a practical `9.5 / 10` rating.
- The repository contains permanent controls capable of preserving that rating.
- A `10 / 10` claim remains conditional on continued production history with no recurring architectural regression.

## Common Validation Matrix

Every implementation slice runs the rows relevant to its ownership. Every phase close runs all applicable rows.

| Validation | Required evidence |
|---|---|
| Diff hygiene | `git diff --check` passes |
| Compiler | affected runtime, editor, and test assemblies compile with zero errors |
| Focused behavior | tests for the touched capability pass |
| Architecture | assembly, dependency, runtime-loop, source-growth, ECS/Burst, and lifecycle gates pass |
| Managed allocation | warmed focused measurement is exactly zero production-owned bytes unless the task explicitly owns a one-time transition |
| Canonical GC | unchanged global Match budget passes; unresolved samples fail closed |
| Performance | focused and canonical p95/p99 remain within approved budgets |
| Native memory | persistent allocations and pools return to stable lifecycle counts |
| Visual behavior | fixed-view captures or PlayMode checks prove unchanged presentation where applicable |
| Device behavior | Android evidence is tied to the exact commit and configuration |

## Work Package And Handoff Contract

Each implementation package must record:

- Role/context, task ID, prerequisite state, and workflow path.
- Branch, isolated shared-object worktree, PR URL, baseline commit, tested head, and dirty-worktree exclusions.
- Exact files changed and files intentionally not touched.
- Ownership change and behavior-preservation statement.
- Before/after source responsibility, dependency, performance, allocation, and memory metrics as applicable.
- Exact commands, pass markers, logs, profiler captures, screenshots, and device artifacts.
- Residual risks and unexercised paths.
- Recommended next task and focused commit message.

The implementation agent owns substantive commits, pushes, PR creation, and review revisions but never merges. The independent coordinator reports findings first, returns substantive fixes to the implementer, runs integrated validation on the final head, and may then add only administrative snapshot, checklist, Decision Log, Implementation Log, or evidence commits. Proposed completion becomes authoritative only when the coordinator merges the PR to `main` and records cleanup.

## Decision Log

| Date | Decision | Reason | Evidence |
|---|---|---|---|
| 2026-07-13 | Treat complete accepted hardening and release certification as the entry condition, not the final architecture target | Completing known hardening debt should produce approximately `8.8 / 10`; production maturity also requires simpler ownership, lifecycle proof, complete UI/device coverage, and sustained evidence | Architecture rating discussion, existing hardening tracker, and pre-release certification backlog |
| 2026-07-13 | Target practical `9.5+`, with `10 / 10` as a sustained standard | No finite checklist proves permanent architectural perfection; permanent regression controls and multiple green releases provide stronger evidence | This tracker, Phases 8 and 9 |

## Implementation Log

No implementation work has started. The program remains inactive until the prerequisite entry contract passes.
