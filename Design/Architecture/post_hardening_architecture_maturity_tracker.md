# Post-Hardening Architecture Maturity Tracker

## Purpose

Move WarlineCapture from the `8.8 / 10` early-development architecture closeout to a practical, evidence-backed `9.5+ / 10` production architecture without making release-cycle device qualification a prerequisite for current code-quality work.

This is a post-hardening program, not a replacement for `architecture_performance_hardening_implementation_tracker.md`. The prerequisite tracker closed its early-development scope with `107 / 107` explicit dispositions, including 15 unpassed release-only certifications transferred to `pre_release_performance_certification_backlog.md`.

The program has two activation lanes. The **Core Architecture Lane** covers Phases 0-5, 7, and 8 and may begin now from the accepted early-development closeout. The **Release Certification Lane** covers Phases 6 and 9 and remains inactive until the pre-release backlog activation criteria are met. Core architecture tasks must preserve fail-closed release contracts but do not wait for long thermal, repeated cold/warm, or sustained release evidence.

The user replaced the planned pull-request workflow for this program with direct commits to a clean `main` checkout. Remaining tasks must not create branches, worktrees, or pull requests. Bounded ownership, focused review, zero-error validation, task-owned staging, stable commits, fetch/rebase discipline, and push verification remain mandatory.

A literal `10 / 10` is treated as a sustained operating standard, not a one-time checkbox. The program can establish `9.5+` quality and the controls needed to retain it; production history must supply the final evidence.

## Authority And Entry Contract

| Field | Value |
|---|---|
| Document date | 2026-07-16 |
| Prerequisite tracker | `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` |
| Git integration authority | This tracker's direct-`main` Status Rules and Decision Log exception |
| Core Architecture Lane prerequisite | Early-development tracker complete; compiler, architecture, critical behavior, Editor performance, and GC closeout gates green |
| Release Certification Lane prerequisite | Core Architecture Lane complete plus `pre_release_performance_certification_backlog.md` activated; Phase 9 additionally requires that backlog accepted |
| Accepted entry rating | evidence-backed `8.5 / 10` from `AM-008` |
| Practical target | evidence-backed `9.5+ / 10` |
| Aspirational target | sustained `10 / 10` operating standard |
| Program status | Phase 0 accepted; Phase 1 ready in the Core Architecture Lane; Release Certification Lane deferred |

The core entry review verifies the accepted architecture/code-quality evidence and records release-only gaps as `measurement-required`; it does not convert them into passes. The release-lane entry review must reject missing, malformed, stale, commit-unknown, thermally invalid, visually incomplete, or deferred evidence and must never pass by weakening a budget or allowlist.

## Rating Model

The final rating is evidence-backed and cannot be derived from checklist percentage or file-count reduction. Phase 0 publishes `Design/AgentReports/ArchitectureMaturity/entry_scorecard.json` and its Markdown rendering using the weights and rubric below.

| Core area | Closeout evidence | Weight | Target |
|---|---:|---:|---:|
| ECS and runtime ownership | `9.2` | `20%` | `9.6+` |
| Modularity and dependency boundaries | `8.5` | `15%` | `9.4+` |
| UI and presentation architecture | provisional `8.2` | `10%` | `9.4+` |
| Lifecycle and resource safety | `8.6` | `15%` | `9.6+` |
| Maintainability and testability | `8.7` | `15%` | `9.5+` |
| Performance and GC discipline | `8.2` | `15%` | `9.7+` |
| Diagnostics and continuous governance | measurement-required | `10%` | `9.5+` |

The Core Architecture score is the weighted mean rounded to one decimal after every category has current evidence. A `9.5+` claim additionally requires every core category to be at least `9.0`; a strong category cannot hide a weak ownership or lifecycle category. The historical `8.8` remains the closeout assessment until Phase 0 recomputes this model.

| Release-only area | Current evidence | Activation target |
|---|---:|---:|
| Android production evidence | `7.0` | `9.6+` |
| Sustained release governance | unproven | `9.5+` |

Release-only categories are reported separately and never averaged into a premature production rating. A production-maturity claim requires the Core Architecture score and both release-only areas to reach at least `9.5`, with the pre-release backlog accepted.

### Score Anchors

| Score | Evidence meaning |
|---:|---|
| `<5` | unsafe or undefined ownership; recurring regressions are expected |
| `5-6` | documented intent with substantial legacy/manual enforcement |
| `7` | correct in major flows with incomplete automation or lifecycle proof |
| `8` | broadly automated, measured, and behavior-covered with known bounded gaps |
| `9` | fail-closed controls, explicit ownership, current measurements, and no material uncovered path in the category |
| `9.5` | independent review plus broad failure/lifecycle/performance evidence and no high-severity residual risk |
| `10` | the `9.5` standard sustained across multiple production releases without recurring architectural regression |

## Status Rules

- `[ ]` pending
- `[~]` in progress; only one task may be claimed by one agent at a time
- `[x]` complete with required evidence recorded in this document
- `[>]` release-deferred; inactive and not complete until its lane activation gate passes
- `[!]` blocked with the exact blocker, owner, and next unblocking action recorded
- A task is not complete because code compiles or a file became smaller.
- Every implementation task requires behavior-preservation evidence, focused tests, architecture checks, and relevant performance/GC checks.
- Never raise a budget, add an allowlist exception, or discard an allocation sample merely to make a gate green.
- Missing, malformed, unresolved, or commit-unknown evidence fails closed.
- Preserve Unity `.meta` files, serialized references, ECS update order, and existing user-owned work.
- Do not combine balance changes, art changes, or unrelated gameplay features with architecture slices.
- New findings do not silently increase the active checklist. Record them in the Decision Log and obtain explicit scope approval.
- This program now works directly on a clean `main` checkout, as explicitly requested by the user. Do not create task branches, worktrees, or pull requests for remaining rows.
- Each bounded slice still requires focused review, final integration gates, zero compiler errors where compilation is relevant, task-owned staging, and a stable commit before pushing `main`. Do not claim branch protection, review approvals, or PR evidence that does not exist.
- Preserve Jenkins and existing CI/performance contracts; do not introduce GitHub Actions as a substitute.

## Global Architecture Guardrails

- `gameplay_solid_ecs_contract.md`, `file_naming_architecture_contract.md`, and `performance_regression_contract.md` are mandatory inherited contracts; this tracker may tighten but not weaken them.
- Keep simulation state and gameplay authority in ECS systems and components.
- Keep MonoBehaviours limited to serialized binding, platform integration, camera ownership, input/event forwarding, and presentation application.
- Do not introduce parallel gameplay truth in managed views, static services, or presentation pools.
- Do not introduce new service locators, implicit `World.DefaultGameObjectInjectionWorld` dependencies, or mutable static state that survives a World lifecycle.
- One state transition has one owner. A decomposition may delegate work but must not duplicate authority.
- Use unmanaged `ISystem` by default for new ECS behavior. `SystemBase` is permitted only when a concrete managed Unity API/object boundary prevents `ISystem`; the task must record the reason, owner, update frequency, allocation evidence, and removal/review condition in the Decision Log.
- Bare `*System` names are reserved for ECS systems. Non-ECS helpers must use an approved reason suffix. Do not introduce broad `*Controller`, `*Manager`, `*Provider`, `*Player`, `*Service`, `*Facade`, `*Installer`, or `*Orchestrator` ownership types.
- Managed ECS gateways may project data, but managed string/object construction must occur only when source versions change.
- No new production `Update`, `LateUpdate`, or polling loop without an explicit owner, measured need, and architecture registration.
- Persistent native memory must have an explicit creator, capacity policy, disposer, and lifecycle test.
- Pool growth and exhaustion behavior must be bounded and testable.
- Maintain or improve the accepted frame-time, GC, memory, package-size, and visual-quality budgets.
- Extend the existing validator, cache, recorder, or lifecycle owner whenever its responsibility matches. Do not create a parallel gate or second runtime authority merely to satisfy a tracker row.

### Active-Work Ownership Safety

- Phase 0 publishes an active-owner registry before implementation dispatch. Every work package lists exact allowed and excluded paths and checks current trackers/worktrees before editing.
- Operation-map, FirstLaunch, audio, UI visual-lock, and other independently owned feature files remain excluded unless their owner explicitly hands off the exact path and the maturity task genuinely requires it.
- A source file cannot be edited concurrently by two agents. Cross-domain work is split at an existing contract boundary or serialized under one owner.
- Generated files, scenes, prefabs, importer metadata, and configuration assets require their own explicit allowlist; a production-code claim does not implicitly authorize them.

## Progress Snapshot

| Field | Status |
|---|---|
| Checklist complete | `25 / 86` (`29.1%`) |
| Core Architecture Lane | `25 / 68` (`36.8%`); active |
| Release Certification Lane | `0 / 18` (`0.0%`); deferred |
| Program state | Core Architecture Lane active; Phase 1 accepted; Phase 2 ownership, lifecycle recovery, bounded transition stress, and structural/pool trend accepted; AM-025 Phase 2 exit is dependency-ready; audit-only AM-026 accepted in parallel; release work inactive |
| Current phase | Phase 2 - World Lifecycle And Dependency Hardening |
| Current task | `AM-025` remains blocked from acceptance but active for remediation. Commander profile backgrounds now belong to each shell instead of global scene-object fields. The safer GameText reset remains tracked static-registry debt. The `575` historical rows contain `480` non-debt and `95` genuine-debt rows grouped into `40` unique file/rule items. Separately owned FirstLaunch and operation-map helper paths still block the canonical source-growth gate. |
| Parallel preparatory work | Audit-only `AM-026` is accepted with 29 surfaces and `AM-WP-001` through `AM-WP-023`; production tasks `AM-027` through `AM-035` remain gated. `AM-WP-007` is additionally blocked on mission/objective owner handoff and `AM-WP-009` preserves audio/FirstLaunch consumers as read-only dependencies. `AM-WP-024` through `AM-WP-028` define accepted lifecycle work plus active AM-025 exit/debt reconciliation; `AM-WP-026` is accepted |
| Blockers | AM-025 source-growth is externally blocked by four separately owned FirstLaunch helpers and four operation-map-owned helpers. Three operation-map helpers are new since the five-path closure policy was published. Row-bound evidence still exposes `95` genuine-debt rows in `40` unique file/rule items that must be remediated to zero. Release-only certification remains intentionally deferred. |
| Latest validation | Commander background shell ownership passes its focused Unity route check and Unity compiles with zero errors. Phase 2 evidence passes `19 / 19`, ownership-delta regeneration is byte-stable, and `git diff --check` passes. The AM-021 ownership snapshot remains refreshed at implementation commit `2107ff75b` with the same `575 / 553 / 22 / 0` resource, explicit-owner, protected-owner, and gap result. |
| Latest evidence | Schema-2 `am025_phase2_ownership_delta.json/.md` records one decision for each of `575` historical intake rows: `472` resolved, `8` protected-deferred, and `95` genuine debt grouped into `40` unique file/rule items. Removing three global scene-object fields makes commander backgrounds shell-owned and route-bounded. `am025_phase2_ownership_delta_intake.json` preserves the immutable v1 intake, and policy schema 2 lists all five exact source-growth blockers |
| Core entry baseline | Phase 0 accepted by `AM-001` through `AM-008`; exact-identity assembly and bounded Editor Match evidence are current at `9a0aa14252e6559680328e520d26c16bfc7b444e`; the dashboard required gate is accepted; the entry rating and owned deltas are published in `entry_baseline_report.md` |
| Release entry review | Deferred until `pre_release_performance_certification_backlog.md` activates |
| Architecture rating | Evidence-backed Phase 0 entry rating `8.5 / 10`; planning baseline only, not a `9.5` claim |
| Performance evidence | Phase 1 exit canonical median accepted at capture commit `cd6e764bd878c6d7cedcbaa3c5034f0f105825b6`: 1,442 frames, 2.777 ms average, 4.121 ms P95 against both 5.14 ms relative and 20 ms absolute ceilings, 0 current-thread allocated bytes; canonical player-relevant GC `262 / 1,024` bytes; all focused owner gates passed |
| Android evidence | Deferred; retain historical diagnostics and recapture only when the release lane activates |
| Sustained release evidence | Deferred; `0 / 3` qualifying release candidates |

## Phase Dependencies

| Phase | Depends on | May overlap with |
|---|---|---|
| Phase 0 - Entry Baseline And Scorecard | accepted early-development architecture closeout | none |
| Phase 1 - Responsibility And Decomposition Hardening | Phase 0 | Phase 2 after inventories are complete |
| Phase 2 - World Lifecycle And Dependency Hardening | Phase 0 | Phase 1 |
| Phase 3 - Allocation-Free UI Projection | Phases 0 and 2 cache/lifecycle contract | Phase 4 |
| Phase 4 - Presentation Pool And Creation Hardening | Phases 0 and 2 | Phase 3 |
| Phase 5 - Determinism And Failure-Path Proof | Phases 1 and 2 | late Phase 3 or 4 |
| Phase 6 - Android Production Proof | Phases 3, 4, and 5 plus pre-release backlog activation | none; device access is serialized |
| Phase 7 - Production-Safe Diagnostics | Phase 0 | Phases 1 through 6 with file claims |
| Phase 8 - Continuous Architecture Enforcement | starts after Phase 0; core lane closes after Phases 1-5 and 7 | all active core phases |
| Phase 9 - Sustained Release Evidence | Core Architecture Lane complete, Phase 6 complete, and pre-release backlog accepted | none |

## Phase 0 - Entry Baseline And Scorecard

No production behavior changes. Establish a reproducible baseline and prevent an architecture score from substituting for evidence.

- [x] `AM-001` Verify the prerequisite tracker is closed and its compiler, architecture, critical behavior, Editor performance, and GC gates are green. Record every release-only gap as deferred/measurement-required without blocking the Core Architecture Lane.
- [x] `AM-002` Record the exact entry commit, branch, Unity editor version, package lock hash, build target, scripting backend, and active quality configuration.
- [x] `AM-003` Define canonical scenarios for idle Match, maximum combat, construction, transport, aircraft, projectiles, every major popup, Menu-to-Match transitions, and long-duration soak.
- [x] `AM-004` Freeze the core architecture scorecard, metric definitions, performance budgets, GC classification policy, memory-growth threshold, and evidence freshness rules. Keep release-only device-tier fields explicitly `measurement-required` until `AM-053`.
- [x] `AM-005` Regenerate the architecture dashboard, produce the validator registry, and reject every required input reported as stale, unknown, malformed, duplicate-owned, or tied to a different commit.
- [x] `AM-006` Produce a current source-size, dependency, assembly-cycle, runtime-loop, static-state, managed-helper, and active-work ownership inventory.
- [x] `AM-007` Produce a lifecycle inventory for Worlds, persistent native containers, query caches, presentation pools, scene roots, event subscriptions, and static caches.
- [x] `AM-008` Publish the entry baseline report with ratings by area, accepted evidence links, known residual risks, and the exact deltas this program owns.

Phase 0 must produce these commit-bound artifacts before any implementation extraction begins:

- `Design/AgentReports/ArchitectureMaturity/entry_scorecard.json` and `.md`.
- `canonical_scenarios.json` with scenario IDs, fixtures, seeds, warmup/measurement windows, and required UI/world states.
- `ownership_inventory.json` and `.md` with selected owners, responsibilities, assemblies, dependencies, current tests, and active-lane exclusions.
- `lifecycle_inventory.json` and `.md` covering Worlds, native containers, queries, pools, subscriptions, scene roots, and static caches.
- `validator_registry.json` and `.md` mapping every existing architecture/performance gate to its owning tool and preventing duplicate validators.
- `exception_registry.json` with owner, rationale, measured effect, approval, expiry, and removal task for every temporary exception.

| Phase 0 task | Required primary output |
|---|---|
| `AM-001` | `entry_prerequisite_review.json` and `.md`, including accepted core gates and explicitly deferred release gaps |
| `AM-002` | `entry_environment.json`, including commit, Unity/package/config hashes, target, backend, and quality identity |
| `AM-003` | `canonical_scenarios.json` and rendered scenario catalog |
| `AM-004` | `entry_scorecard.json`, score rubric application, budget IDs, freshness rules, and `exception_registry.json` |
| `AM-005` | refreshed architecture dashboard plus `validator_registry.json` and `.md` |
| `AM-006` | `ownership_inventory.json` and `.md`, including active-owner/path exclusions |
| `AM-007` | `lifecycle_inventory.json` and `.md` |
| `AM-008` | `entry_baseline_report.md` tying all Phase 0 artifacts to one exact commit |

All Phase 0 JSON must parse, use an explicit schema version, sort path/id collections deterministically, and regenerate byte-identically from unchanged inputs. Phase 0 changes no production behavior.

### Phase 0 Exit Gate

- Every required baseline artifact identifies the exact commit and environment.
- The dashboard contains no required stale or unknown evidence.
- Each later phase has a bounded owner list and measurable acceptance criteria.

### Bounded Work Package Gate

Umbrella rows such as `AM-012`, `AM-015`, `AM-029` through `AM-033`, and `AM-038` through `AM-042` cannot be dispatched directly. Phase 0 or the owning phase first creates a numbered `AM-WP-###` package containing:

1. One concrete responsibility and its current/future owner.
2. Exact production, test, config, scene/prefab, and generated-file allowlists plus explicit exclusions.
3. Owning and dependent assemblies, update order, data authority, lifecycle, and thread/Burst constraints.
4. Existing characterization tests and the missing behavior/failure cases that must be added before extraction.
5. Baseline source responsibility, dependency, frame, GC, memory, and creation/pool metrics relevant to the change.
6. Exact acceptance commands, pass markers, budgets, visual checks, and rollback condition.
7. A maximum slice boundary: one responsibility extraction or one UI/pool domain, not a broad multi-domain rewrite.

The coordinator may run several disjoint packages in parallel, but an umbrella row becomes complete only after every package named by its Phase 0 owner list is accepted. New findings become separately approved packages; they do not silently expand an active package.

### Core Evidence Contract

- `performance_regression_accepted_baseline.json` remains the budget authority. A maturity task may ratchet a limit only from accepted evidence; loosening requires explicit user approval and a documented product tradeoff.
- Canonical Match GC uses the existing 180-frame warmup and 300-frame measurement window. The global player-relevant budget remains at most `1,024` bytes, while a changed owner must contribute exactly zero recurring bytes in its focused unchanged-state scenario.
- Focused UI unchanged-state allocation uses at least 180 warmup and 300 measured frames with the surface open and fully bound. Opening/closing transition allocation is reported separately and must stay within a Phase 0 scorecard limit.
- Frame evidence records average, p95, p99, maximum, sample count, warmup, fixture counts, quality configuration, resolution, and instrumentation state. Existing scenario-specific p95/p99 limits remain authoritative.
- Core Architecture lifecycle evidence runs one warm-up plus at least 10 measured Menu -> Match -> Menu cycles. The former 100-cycle extended stress policy is deferred until a maturity or release-certification gate activates it; no threshold may be invented after a failure.
- Memory evidence is not accepted from a single before/after sample. The scorecard defines sampling cadence, warmup, plateau window, slope limit, return-to-baseline tolerance, and metric source for managed, native, graphics, and pool memory.
- Evidence freshness is content-based: exact commit and environment are mandatory, and any change to a governed source/config/scenario/tool hash invalidates that row. Unrelated documentation changes do not require an expensive recapture when all governed hashes remain identical.
- Every measured artifact records instrumentation overhead or a paired instrumentation-off control when the recorder can materially affect the result.

## Phase 1 - Responsibility And Decomposition Hardening

Reduce change risk in oversized systems without replacing the architecture or changing gameplay authority.

`AM-009` scores candidates from `0-4` for responsibility count, dependency fan-in/fan-out, mutable/lifecycle state, measured runtime cost, and recent change frequency. Lines/bytes are recorded but used only as a tie-breaker. The first wave selects at most three non-overlapping owners and publishes the exact reason each was selected or rejected; no extraction begins from a raw largest-file list.

- [x] `AM-009` Rank the largest and most coupled production files by lines, dependencies, state ownership, update-time cost, and change frequency; do not rank by lines alone.
- [x] `AM-010` Write a responsibility map for each selected owner, including inputs, outputs, state authority, update order, side effects, tests, and allowed dependencies.
- [x] `AM-011` Add missing characterization tests before extracting behavior from any selected owner.
- [x] `AM-012` Extract one capability or ECS phase at a time behind existing contracts, preserving system order and avoiding a new coordinator-shaped helper.
- [x] `AM-013` Consolidate duplicated query-cache, command-queue, fixed-capacity scratch, and projection-cache mechanics only where one narrow shared contract removes real duplication.
- [x] `AM-014` Add update-order and behavior-equivalence tests for every decomposition that crosses a system or assembly boundary.
- [x] `AM-015` Complete measured decomposition of the highest-risk remaining UI/presentation helper after its characterization coverage is green.
- [x] `AM-016` Update source-growth and responsibility guardrails so extracted files cannot regrow into equivalent god owners elsewhere.
- [x] `AM-017` Recapture focused and canonical Match performance/GC evidence after all Phase 1 integrations and reject any behavior or frame-time regression.

### Phase 1 Exit Gate

- Selected oversized owners have fewer responsibilities, not merely more files.
- No new cyclic dependency, generic service locator, duplicate state authority, or oversized replacement helper exists.
- Canonical behavior, frame time, and managed-allocation gates remain green.

## Phase 2 - World Lifecycle And Dependency Hardening

Make runtime dependencies explicit and prove that caches and native resources cannot outlive their owning World.

- [x] `AM-018` Inventory production uses of global World lookup, mutable static caches, static event subscriptions, hidden singletons, and runtime object discovery.
- [x] `AM-019` Define one standard World-bound query/entity cache contract covering positive lookup, negative lookup, invalidation, rebind, disposal, and destroyed-entity recovery.
- [x] `AM-020` Move mutable runtime state that crosses World lifecycles into explicit World-owned systems, components, or lifecycle containers where practical.
- [x] `AM-021` Give every persistent native container, query, event subscription, and presentation root an explicit creation and disposal owner.
- [x] `AM-022` Add tests for World destruction/recreation, domain reload, scene unload/reload, missing singleton recovery, and replaced command entities.
- [x] `AM-023` Run one warm-up plus 10 measured automated Menu-to-Match-to-Menu cycles without duplicate systems, stale entities, retained subscriptions, or presentation-root accumulation.
- [x] `AM-024` Add native allocation and pool-count snapshots around lifecycle stress tests, prove no upward structural retained-owner/pool trend after warmup, and report supporting Editor memory trends without treating unforced-GC totals as leak verdicts. Accepted package: `Design/Architecture/WorkPackages/am_wp_026_lifecycle_memory_pool_trend.md`.
- [ ] `AM-025` Run the full architecture, lifecycle, compiler, and focused allocation suites and publish the Phase 2 ownership delta. Active packages: `Design/Architecture/WorkPackages/am_wp_027_phase2_exit_acceptance.md` and `Design/Architecture/WorkPackages/am_wp_028_phase2_debt_reconciliation.md`; AM-021 through AM-024 are accepted, while row-bound debt reconciliation and five owner-side source-growth blockers remain.

### Phase 2 Exit Gate

- Runtime systems do not depend on unregistered global lookup or World-surviving mutable state.
- Every persistent resource has one tested lifecycle owner.
- Repeated transitions produce stable entity, native-memory, subscription, and pool counts.

## Phase 3 - Allocation-Free UI Projection

Make every Canvas UI surface change-driven and allocation-free during unchanged steady state, including when popups are open.

The inventory task may be accepted independently because it is audit-only and changes no runtime ownership. All Phase 3 production remediation remains gated until the Phase 2 dependency gate is accepted:

- Surface inventory: `Design/Architecture/ui_projection_allocation_inventory.md`
- First bounded implementation package: `Design/Architecture/WorkPackages/am_wp_001_resource_exchange_projection_cache.md`

- [x] `AM-026` Inventory every Menu, Match HUD, drawer, popup, tooltip, selection panel, and ARIA view by polling owner, source versions, managed conversions, rebuild behavior, and allocation coverage.
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

### Determinism Contract

- A replay header records schema version, exact source/config/content hashes, scenario ID, deterministic seed set, fixed-step configuration, quality-independent simulation settings, and ordered command/input stream.
- Hash authoritative gameplay state by stable gameplay/config identifiers, never transient Entity indices, chunk order, managed object identity, presentation state, profiler counters, timestamps, or rendering order.
- Integer, enum, resource, ownership, queue, occupancy, and deterministic RNG state is exact. Floating authoritative fields use field-specific normalization/quantization frozen in `canonical_scenarios.json` before the first comparison; tolerances cannot be loosened after divergence.
- Gameplay randomness uses explicit deterministic state owned by the relevant ECS domain. Direct `UnityEngine.Random`, wall-clock time, frame count, or presentation callbacks cannot influence authoritative outcomes.
- Same-platform/same-build replay requires exact normalized hashes at every checkpoint. Cross-device comparison uses the same normalized schema and reports the first divergent tick, entity stable ID, component/field, expected value, and actual value.
- Presentation-only variance is excluded from the simulation hash but must still pass behavior/visual checks when a task touches presentation.
- Replay files are bounded, versioned, and rejected on unknown schema/config/content hashes. A compatibility migration must be explicit; silent best-effort replay is prohibited.

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

Replace editor confidence and stale device captures with current, commit-bound production evidence. This phase is inactive until `pre_release_performance_certification_backlog.md` activates. That backlog owns the certification obligations; this phase sequences and accepts them without creating duplicate recorders, collectors, schemas, or visual matrices.

- [>] `AM-053` Activate the pre-release backlog and freeze supported low, recommended, and high-end Android device tiers with OS, chipset, memory, resolution, refresh rate, and thermal environment.
- [>] `AM-054` Reuse and extend the existing build-report/artifact contract so commit, build configuration, content version, scripting backend, quality tier, package hash, and clean provenance are embedded in every certification artifact.
- [>] `AM-055` Map canonical scenario IDs to the existing development/release collectors for idle Match, maximum combat, construction bursts, transport/aircraft, every major popup, transitions, and soak; add no parallel device runner.
- [>] `AM-056` Capture CPU, GPU, frame pacing, GC, native memory, draw/batch, thermal, battery, package, and visual evidence on the low tier.
- [>] `AM-057` Capture the same evidence on the recommended tier.
- [>] `AM-058` Capture the same evidence on the high-end tier.
- [>] `AM-059` Run at least one 30-minute foreground Match soak per tier after the scorecard-defined warmup.
- [>] `AM-060` Run the scorecard-defined cold-start and Menu-to-Match transition cycles per tier and inspect retained memory, loading spikes, lifecycle counts, foreground continuity, and crash/ANR logs.
- [>] `AM-061` Reconcile device memory, package size, installed size, audio residency, texture residency, mesh residency, graphics-driver memory, and native pool budgets against the frozen scorecard.
- [>] `AM-062` Publish one cross-device report, close the corresponding pre-release backlog rows, and fail the phase on unexplained recurring allocation, upward retained-memory trend, thermal instability, visual failure, stale provenance, or weakened frame/package/memory budgets.

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
- [ ] `AM-075` Run focused architecture/compiler/GC gates per PR, canonical Editor lanes on the scorecard-defined scheduled cadence, and Android device lanes only after release-lane activation or an explicitly approved regression investigation.
- [ ] `AM-076` Fail closed on missing, malformed, stale, commit-unknown, or scenario-incomplete evidence.
- [ ] `AM-077` Require every architecture exception to record owner, rationale, measured effect, approval, expiry, and removal task.
- [ ] `AM-078` Add architecture-impact and evidence fields to implementation handoffs and release-candidate review.

### Phase 8 Exit Gate

- Known architectural regressions cannot merge silently.
- CI reports the responsible file, owner contract, violated budget, and required evidence.
- Temporary exceptions expire and cannot become permanent undocumented debt.

## Phase 9 - Sustained Release Evidence

Demonstrate that the architecture remains healthy through real integration cycles rather than one controlled benchmark. This phase remains release-deferred until the Core Architecture Lane and Phase 6 are complete and the pre-release certification backlog is accepted.

- [>] `AM-079` Produce three consecutive release candidates from distinct integration points with every required architecture, compiler, test, GC, memory, visual, and device gate green.
- [>] `AM-080` Keep the architecture dashboard fully fresh and commit-bound for all three candidates.
- [>] `AM-081` Demonstrate zero recurring production-owned managed allocation in all covered warmed scenarios across all three candidates.
- [>] `AM-082` Demonstrate stable native and managed memory plateaus in soak and repeated-transition tests across all three candidates.
- [>] `AM-083` Close or renew every temporary exception through explicit review; no expired, unowned, or unexplained exception may remain.
- [>] `AM-084` Pass the complete supported Android device matrix without weakening frame-time, quality, package, or memory budgets.
- [>] `AM-085` Perform an independent final architecture re-audit, record category ratings and residual risks, and compare them with the Phase 0 scorecard.
- [>] `AM-086` Publish the final maturity report and convert its monitoring gates into permanent project governance rather than declaring architecture work permanently finished.

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
| Naming/system ownership | bare `*System`, prohibited broad-name, non-ECS helper, `ISystem` inventory, and approved `SystemBase` exception gates pass |
| Managed allocation | warmed focused measurement is exactly zero production-owned bytes unless the task explicitly owns a one-time transition |
| Canonical GC | unchanged global Match budget passes; unresolved samples fail closed |
| Performance | focused and canonical p95/p99 remain within approved budgets |
| Native memory | persistent allocations and pools return to stable lifecycle counts |
| Visual behavior | fixed-view captures or PlayMode checks prove unchanged presentation where applicable |
| Evidence identity | source/config/scenario/tool hashes and exact commit/environment satisfy the Core Evidence Contract |
| Device behavior | required only after release-lane activation or for a specifically approved platform-runtime regression; Android evidence is tied to the exact commit and configuration |

## Work Package And Handoff Contract

Each implementation package must record:

- Role/context, task ID, `AM-WP-###` ID, lane, prerequisite state, and workflow path.
- Direct-to-`main` workflow, baseline commit, tested head, pushed commit, and dirty-worktree exclusions.
- Exact allowed files, generated/config/scene authority, files intentionally not touched, active owners checked, and overlap resolution.
- Ownership change and behavior-preservation statement.
- Current/future owner, assembly boundary, update order, lifecycle, Burst/thread constraints, and explicit `ISystem`/`SystemBase` decision.
- Before/after source responsibility, dependency, performance, allocation, and memory metrics as applicable.
- Exact scorecard scenario/budget IDs, commands, pass markers, logs, profiler captures, screenshots, and device artifacts.
- Residual risks and unexercised paths.
- Rollback condition, recommended next task, and focused commit message.

The implementing agent works directly on `main`, owns the bounded substantive and administrative commits, runs focused review and integrated validation before each push, and stages only task-owned files. A row becomes authoritative when its stable commits are pushed to `origin/main` and its tracker evidence is recorded; known-broken work is never pushed.

## Decision Log

| Date | Decision | Reason | Evidence |
|---|---|---|---|
| 2026-07-13 | Treat complete accepted hardening and release certification as the production-maturity/release-lane entry condition, not the Core Architecture Lane entry condition | Completing known hardening debt produces approximately `8.8 / 10`; production maturity still requires complete UI/device coverage and sustained evidence, while code-quality work can proceed earlier | Architecture rating discussion, existing hardening tracker, pre-release backlog, and the 2026-07-16 lane split |
| 2026-07-13 | Target practical `9.5+`, with `10 / 10` as a sustained standard | No finite checklist proves permanent architectural perfection; permanent regression controls and multiple green releases provide stronger evidence | This tracker, Phases 8 and 9 |
| 2026-07-16 | Activate the Core Architecture Lane independently from release certification | Current development benefits from ownership, lifecycle, UI allocation, pool, determinism, diagnostics, and CI hardening; repeated long release qualification remains intentionally deferred until content stabilizes | User approval; completed early-development hardening tracker; pre-release backlog |
| 2026-07-16 | Make the pre-release backlog the sole release-certification obligation source | Phase 6 and Phase 9 must sequence existing backlog contracts rather than creating duplicate collectors, schemas, or evidence definitions | `pre_release_performance_certification_backlog.md` |
| 2026-07-16 | Use direct commits to `main` for the remaining maturity program | The user explicitly rejected PRs and task branches because their coordination consumes excessive tokens; bounded review, validation, owned-file staging, stable commits, and push discipline remain mandatory | User instruction; AM-005 direct integration |

## Implementation Log

### 2026-07-16 - AM-001 - Entry prerequisite review

- Workflow path: pull request.
- Implementation branch and PR: `codex/am-001-prerequisite-review`; [PR #14](https://github.com/farhad2na2/WarlineCapture/pull/14).
- Baseline: `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713`; reviewed implementation head `8d9e5388c792fb2de407e5808bfb6fcbc5dc530c`.
- Result: Core Architecture Lane entry accepted from the bounded prerequisite closeout record; Release Certification Lane remains deferred and no release-certification claim is made.
- Evidence: `Design/AgentReports/ArchitectureMaturity/entry_prerequisite_review.json` and matching Markdown rendering.
- Validation: JSON parse and invariants passed; deterministic ID/path ordering passed; every cited repository path exists; Markdown/JSON gate, deferral, and risk IDs match; `git diff --check` passed; substantive scope is exactly the two AM-001 artifacts.
- Independent review: no material finding in the two-file substantive diff. The artifacts distinguish the review baseline from historical measurement revisions and preserve all 15 release-only obligations as unaccepted.
- Residual risks: direct Editor and GC artifacts declare no measurement commit; the GC filename date differs from its internal capture date; the architecture dashboard has zero healthy/current inputs and an older Editor snapshot. Later Phase 0 tasks must refresh exact-commit environment, dashboard, performance, and GC evidence before Phase 0 exit.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, packages, and `ProjectSettings`.
- Next task: `AM-002` records the exact entry environment identity.

### 2026-07-16 - AM-002 - Exact entry environment identity

- Workflow path: pull request.
- Implementation branch and PR: `codex/am-002-entry-environment`; [PR #15](https://github.com/farhad2na2/WarlineCapture/pull/15).
- Baseline: `68c785502151a54b6f6e4bb115789c8957962403`, tree `a22c7a140b8326dd6a9bc2b00833a3b1764762af`; reviewed implementation head `5d3bce4d9a860c8f361c272f623847a8038c2b47`.
- Result: the entry environment records Unity `6000.5.2f1 (eb73d3b415a1)`, the package-lock SHA-256, Android as the persisted active Editor target, IL2CPP as its configured backend, and Mobile/index `1` as the serialized current quality configuration.
- Evidence: `Design/AgentReports/ArchitectureMaturity/entry_environment.json`.
- Validation: canonical JSON serialization, schema and identity invariants, deterministic array ordering, commit/tree equality, tracked authority hashes, machine-local source/tool/conversion hashes, repeated `binary2text` conversion, cited selectors, single-file substantive scope, and `git diff --check` passed.
- Independent review: one P1 finding rejected null active target/backend/quality values. The implementation agent resolved it with content-addressed `Library/EditorUserBuildSettings.asset` evidence and explicit tracked-setting derivations; complete rereview found no remaining material issue.
- Residual risk: Android is a persisted machine-local Editor setting rather than a live Editor API observation. No Player runtime was launched, so runtime quality remains explicitly not observed and is not required for this documentation-only identity slice.
- Exclusions preserved: the concurrent operation-map commit was rebased intact; operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-002.
- Next task: `AM-003` defines the canonical scenario catalog.

### 2026-07-16 - AM-003 - Canonical scenario catalog

- Workflow path: pull request.
- Implementation branch and PR: `codex/am-003-canonical-scenarios`; [PR #16](https://github.com/farhad2na2/WarlineCapture/pull/16).
- Baseline: `a5bf7b72cdfb9457c6af1e98ee2bcaae983f9ff6`, tree `8b2f87e5d9006fd9cea8a97c2153abfe2d265d2a`; reviewed implementation head `f00e557eee8db6e12f5de4cf0faa2eed9c30cd8d`.
- Result: ten deterministic canonical scenarios cover idle Match, maximum combat, construction, transport, aircraft, projectiles, twelve implemented Match UI surfaces, both returning-user Menu/Match transitions, and a definition-only long-soak procedure. Result and Support selector routes remain explicit non-executable implementation gaps.
- Evidence: `Design/AgentReports/ArchitectureMaturity/canonical_scenarios.json` and matching rendered Markdown catalog.
- Validation: canonical JSON serialization, schema and baseline invariants, unique/sorted scenario and coverage IDs, deterministic seeds and windows, null AM-004 thresholds, required retry/skip policy, cited path existence, JSON/Markdown ID parity, two-file substantive scope, and `git diff --check` passed.
- Independent review: two findings rejected an accidentally active 30-minute Core soak and missing Support-selector coverage. Implementation commit `b61848e3f` made the soak definition-only/deferred and added the evidence-backed Support gap; complete latest-main rereview found no remaining material issue.
- Residual risks: the catalog exposes fixture limitations for exact maximum-combat saturation, construction burst scale, transport runway behavior, full Match aircraft lifecycle, and historical soak freshness. AM-003 defines procedures and does not execute Unity, Player, Android, thermal, or sustained certification.
- Exclusions preserved: current operation-map compatibility metadata and canonical Match authorities were retained while separately owned operation-map R&D, FirstLaunch, audio, UI visual-lock, and production assets remained unmodified.
- Next task: `AM-004` freezes scorecard metrics, budgets, freshness rules, and the exception registry.

### 2026-07-16 - AM-004 - Core scorecard and evidence policy

- Workflow path: pull request.
- Implementation branch and PR: `codex/am-004-scorecard`; [PR #17](https://github.com/farhad2na2/WarlineCapture/pull/17).
- Baseline: `76f80c7a23b06ba6719593cb5f2815e476db7987`, tree `af237ee62e29e8d5191d4e3051451b6b47ab1712`; reviewed artifact head `87a4a1ddbd60d28aefc6b61614e4f5725db83d2f`.
- Result: the seven-category Core score model, accepted Editor/GC/UI budgets, all 30 canonical scenario dispositions, GC classification, 10-warmup plus 100-measured lifecycle policy, memory plateau/slope/return tolerances, content-based freshness, and deferred release fields are frozen without claiming current measurements or a recomputed rating.
- Evidence: `Design/AgentReports/ArchitectureMaturity/entry_scorecard.json`, matching Markdown rendering, and `Design/AgentReports/ArchitectureMaturity/exception_registry.json`.
- Validation: canonical JSON serialization, exact parent commit/tree, category weights totaling 100, 30 unique AM-003 budget IDs with JSON/Markdown parity, frozen lifecycle/memory invariants, governed content hashes, source-ratchet hash/count reconciliation, exact three-file substantive scope, and `git diff --check` passed.
- Independent review: the first complete review found no material scorecard issue. A later P2 rereview rejected an ambiguous selected-section hash ordering rule; the final artifact specifies source-document order, exact boundaries, UTF-8/LF normalization, per-line trailing LF, and no inserted separator. Final rereview recomputed `836d070c6dd0a97de857357cc2fb54301fd1df233588c5845e0d952d92b9b8ff` and found no remaining material issue.
- Exception result: zero qualified active temporary exceptions and zero performance/GC waivers. The registry separately audits 139 exact-ceiling source-growth ratchets, 25 managed-boundary inventory entries, and the separately owned operation-map R&D authorization without promoting them into Core passes.
- Residual risks: current architecture ratings and exact-entry performance measurements remain unclaimed; AM-005 must implement the frozen content/environment freshness rules and reject the currently stale or unknown dashboard inputs. Graphics/device residency and sustained evidence remain inactive until `AM-053`.
- Exclusions preserved: two concurrent operation-map commits were rebased intact; operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-004.
- Next task: `AM-005` regenerates the architecture dashboard and validator registry with fail-closed freshness enforcement.

### 2026-07-16 - AM-005 - Validator registry and fail-closed dashboard

- Workflow path: direct commit to `main`, per the user's replacement of the planned PR workflow.
- Baseline: `acf21c7c900a55aa19fdb8a19a5693be38d2c036`; implementation and evidence baseline: `5c7ed147c4f49c1a5bc6ea0b697922cd74845277`, tree `5170ec5b70e1ecaa53fe955404660b0e976747bb`.
- Result: 24 canonical validators own 35 unique responsibilities; four composite/wrapper entrypoints are explicitly non-canonical aliases; seven dashboard evidence inputs have one owner each and are classified as two required Core rows plus five advisory/deferred rows.
- Evidence: `Design/AgentReports/ArchitectureMaturity/validator_registry.json` and matching Markdown rendering; refreshed `Design/AgentReports/architecture_performance_dashboard.json` and Markdown rendering.
- Fail-closed behavior: required missing, malformed, schema-incomplete, revision-unknown, commit-mismatched, dirty, environment-unknown, environment-mismatched, duplicate-ID, duplicate-path-owner, duplicate-responsibility-owner, and malformed-registry states reject the gate. `--revision` is mandatory and `--check` returns exit `2` while rejected.
- Current dashboard disposition: registry current with zero registry errors; both required inputs rejected as `unknown` because the tracked APH-700 assembly report and Editor Match performance report declare neither an exact revision nor the AM-002 environment hash. All five historical audio/build/content rows are advisory/deferred and stale; they remain visible without blocking Core work.
- Validation: 14 focused Python tests passed; production registry reports 24 unique validator IDs and 35 unique responsibilities; JSON parse/invariants passed; byte-identical regeneration passed; expected fail-closed production check returned `2`; `git diff --check` passed; no Unity or production compilation was required because AM-005 changes only Python CI tooling and generated documentation/evidence.
- Review: bounded self-review found and resolved two pre-push gaps: provenance fields leaking into metrics and a syntactically valid but schema-empty artifact being accepted. A final identity review made `--revision` mandatory so no run can silently bind evidence to an unintended `HEAD`.
- Residual risks: Phase 0 exit remains blocked on exact-identity regeneration of the two required evidence rows and on later ownership/lifecycle inventories. No architecture rating, performance pass, GC pass, Android pass, or release-certification claim is made by AM-005.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, packages, and `ProjectSettings` were not edited.
- Next task: `AM-006` produces the source-size, dependency, assembly-cycle, runtime-loop, static-state, managed-helper, and active-work ownership inventory.

### 2026-07-16 - AM-006 - Current architecture ownership inventory

- Workflow path: direct commits to `main`.
- Baseline: `cc6748b86601e0e6461e754aa00ac35258a4636b`; implementation and inventory baseline: `202b53025d793d6bfe3f0379782b7e185e92be07`, tree `181bc23148883cf3b67f86f5a9b52324dc150a97`.
- Result: the live production tree contains 992 non-Editor C# files, 95 files over 500 lines, 23 over 1,000 lines, 338 managed `*SystemHelper.cs` files, 37 helpers over 500 lines, 63 MonoBehaviour update/coroutine loop rows, and 166 lexical mutable-static candidates requiring later owner classification.
- Dependency result: 21 first-party asmdefs declare 92 first-party edges and 100 external references with zero strongly connected dependency cycles.
- Ownership result: five bounded owner domains map to existing AM-005 validator owners. Operation-map and audio are active protected lanes; completed FirstLaunch and held UI visual-lock paths remain protected from maturity edits without exact owner handoff.
- Evidence: `Design/AgentReports/ArchitectureMaturity/ownership_inventory.json` and matching Markdown rendering, generated by `Tools/CI/architecture_ownership_inventory.py`.
- Validation: five focused Python tests passed; live source/helper counts matched direct filesystem counts; all 63 loop rows matched the independent existing Phase 7 loop generator exactly; asmdef counts and edges reconciled with the APH-700 report while cycles were recomputed from current asmdefs; validator owner IDs resolved through the AM-005 registry; authority hashes and ID/path ordering passed; byte-identical regeneration and `git diff --check` passed.
- Review: mutable-static rows are explicitly candidates, not automatic violations; expression-bodied static properties, comments, strings, Editor source, readonly fields, and methods are excluded. Current scans are derived from source and asmdef files rather than promoted from stale historical report commit claims.
- Residual risks: candidate rows still require responsibility classification in later bounded work packages; the inventory measures current structure but does not claim performance, GC, memory plateau, lifecycle cleanup, or release readiness. Phase 0 still needs the lifecycle inventory and entry baseline report.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, production code, scenes, prefabs, packages, and `ProjectSettings` were read for ownership/hash evidence but not edited.
- Next task: `AM-007` produces the World, native-container, query-cache, presentation-pool, scene-root, subscription, and static-cache lifecycle inventory.

### 2026-07-16 - AM-007 - Lifecycle ownership inventory

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Baseline: `d409eca06225a0f21b9ac8b2f324c9ce20864876`; accepted implementation and inventory baseline: `7f41260803d85f46f6bb11a0078b395bc30d1ee4`, tree `b90137d8c6dfd4b6bf7b5822955d2bf577671dd8`.
- Result: the live non-Editor production tree contains 77 World owners with 87 default-world accesses, 129 native-container ownership candidates, 57 field-specific `Allocator.Persistent` observations, 505 query/lookup cache candidates, 15 presentation-pool candidates, 11 scene-root candidates, 79 named event-subscription candidates, and 47 static-cache candidates.
- Lifecycle attention: 63 native fields have no directly observed teardown dispose, 157 non-system query/lookup fields require owner classification, 12 presentation pools have no directly observed teardown clear/dispose, 57 named subscriptions have no matching unsubscribe in the owner type, 71 have no unsubscribe directly in a teardown method, and 47 static caches have no directly observed teardown reset. These are classification rows, not automatic leak findings.
- Evidence: `Design/AgentReports/ArchitectureMaturity/lifecycle_inventory.json` and matching Markdown rendering, generated by `Tools/CI/architecture_lifecycle_inventory.py`.
- Validation: five focused Python tests passed; generic fields, nested owner attribution, member-depth filtering, direct teardown observation, exact subscription pairing, expression-bodied lifecycle methods, comments, strings, method locals, Editor source, exact Git identity, authority presence, active-owner ordering, and byte-identical regeneration were covered; production JSON/Markdown hashes matched an independent regeneration; `git diff --check` passed.
- Review: broad lexical matches are constrained by owner/type/path signals and preserve source lines. Missing cleanup remains a review requirement because helper-call chains and framework-owned ECS lifetimes cannot be proven by lexical scanning alone. System-owned query fields are classified separately from explicit-dispose and unresolved owner rows.
- Residual risks: AM-007 identifies ownership debt but does not change runtime behavior, prove memory plateau, or claim performance. Candidate resolution belongs to bounded later work packages using the required creator, capacity policy, disposer, and lifecycle-test fields.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, production C# behavior, scenes, prefabs, packages, and `ProjectSettings` were read for lifecycle evidence but not edited.
- Next task: `AM-008` publishes the exact-identity Phase 0 entry baseline report and owned delta list.

### 2026-07-16 - AM-008 - Phase 0 entry baseline report

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Entry baseline: `9a0aa14252e6559680328e520d26c16bfc7b444e`, tree `0f3bf4a00a69c417f5a92f3811d88250a7c8d5ef`; environment identity SHA-256 `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`.
- Result: Phase 0 is accepted for the Core Architecture Lane with an evidence-backed `8.5 / 10` planning baseline. Ratings, targets, evidence links, residual risks, and exact later-phase deltas are published in `Design/AgentReports/ArchitectureMaturity/entry_baseline_report.md`.
- Required evidence: the current assembly report records 21 assemblies, 92 first-party edges, 102 external references, 1,436 scoped source files, and zero unowned scoped source files. The bounded Editor Match capture records 1,426 frames, 2.808 ms average, 4.112 ms P95, zero current-thread allocated bytes, 733 units, 628 buildings, and 46 visible-model estimate.
- Dashboard: revision `9a0aa14252e6559680328e520d26c16bfc7b444e`; required gate `accepted`; two required inputs current; zero required rejected; zero unknown/missing/malformed inputs; zero registry errors. Five historical advisory inputs remain stale and release-deferred without blocking Core work.
- Validation: real Unity compile-only run completed with zero compiler errors; the APH-700 Unity execute method regenerated the report successfully; the Match performance execute method passed its accepted 20 ms/zero-allocation/full-fixture contract; 14 dashboard Python tests passed; all 11 baseline-manifest content hashes reconciled; JSON identities, environment hashes, clean flags, dashboard invariants, checklist math, and `git diff --check` passed.
- Test infrastructure note: a focused Unity Test Runner attempt compiled successfully but stalled in the package prebuild hook after UPM disconnected, before executing test cases. It was stopped; the production generator and capture execute methods then supplied direct functional evidence. No test failure was reported or hidden.
- Residual risks: category-specific ownership/lifecycle/UI/pool/determinism/diagnostics/enforcement gaps remain explicitly assigned to `AM-009` through `AM-052` and `AM-063` through `AM-078`. Release-only Android and sustained-candidate rows remain deferred.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, gameplay/runtime behavior, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-008.
- Next task: `AM-009` ranks the largest and most coupled production owners using size, dependency, state, update-cost, and change-frequency evidence rather than line count alone.

### 2026-07-16 - AM-009 - Production owner risk ranking

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Measurement baseline: `91aaf6db3f44dc6f6ed44ecdaa8b7a702f0e29e9`, tree `65cfc22224729c10c3a149af4869ba79075dae6c`; current focused timing evidence committed at `6e16f8001b6aecc58edaed9db28b26693cf96ac1` after rebasing the disjoint operation-map lane intact.
- Ranking baseline: generator/test commit `1b9e866acbf560650b2a2869582913ef31fa4901`, tree `6a6954ebf7fc213878caaed5138e13a483e095e5`; generated ranking committed at `eeec1abb5d8b66bf7d1eea094b0899327c8dd25e`.
- Result: all 992 non-Editor production C# owners have a deterministic screening rank and an exact selection/rejection reason. The report retains 49 protected owners as visible and ineligible. Missing responsibility or runtime evidence remains `null`, never an invented zero.
- First wave: `GroundMissileLauncherSystems.cs` and `UnitTransportBoardingSystem.cs` are selected with disjoint one-file initial allowlists. The post-fix `PathfindBatchJob.cs` row remains ranked but selection-ineligible until current characterization exists. No third owner is forced into the wave.
- Current timing: ground-missile simulation passed at 0.924 ms average / 0.963 ms P95; transport boarding update passed at 0.253 ms average / 0.271 ms P95. Both focused fixtures measured 64 scenarios after 16 warmups with zero current-thread allocated bytes and zero compiler errors.
- Evidence: `Design/AgentReports/ArchitectureMaturity/owner_runtime_measurements.json`, matching Markdown summary, `Design/AgentReports/ArchitectureMaturity/owner_risk_ranking.json`, and matching generated Markdown ranking.
- Validation: 12 focused ranking tests and 36 integrated ownership/lifecycle/ranking/dashboard Python tests passed. Exact commit/tree and generator/test/authority hashes, production/harness/environment hashes, tracked and untracked governed-path rejection, field/property state attribution, local exclusion, conservative unique type-use coupling, recursive protected globs, nonempty/disjoint allowlists, all-row decisions, byte-identical regeneration, and `git diff --check` passed.
- Independent review: initial reviews rejected ambiguous type coupling, local/state undercounting, stale or unattributed runtime scores, truncated decisions, incomplete Git binding, recursive-glob gaps, and heuristic-only overlap. Every finding was resolved; final rereview returned `PASS` with no blocker.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, gameplay behavior, scenes, prefabs, packages, and `ProjectSettings` were read only for hashes/ownership evidence and were not edited by AM-009.
- Next task: `AM-010` writes responsibility maps for the two selected owners, including inputs, outputs, state authority, update order, side effects, tests, and allowed dependencies.

### 2026-07-16 - AM-010 - Selected owner responsibility maps

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Source baseline: `ab955a7721fd0616b627daab508632d023e047d8`, tree `a40d19b327a33bc2aca8e126dcdc61616ccee458`; accepted responsibility-map implementation commit `c1e30c39452906f25ceb1c53a8376689e0e9fb8b`.
- Result: exact responsibility maps now cover both AM-009 selections and their declared ECS types, inputs, outputs, state authority, update order, side effects, failure behavior, tests, allowed/forbidden dependencies, and bounded extraction candidates. Owner paths, modification scopes, and initial allowlists are paired exactly to their AM-009 ranking rows.
- Ground-missile findings: the owner contains six unmanaged systems and two Burst jobs. `UnitAttackSystem` arms canonical launcher state; fire advances launcher phases; visual systems project state; projectile flight emits one-shot impact requests; impact owns request consumption and damage. AM-011 must characterize the currently unhandled `Recovering` phase and launcher in-flight cleanup when a projectile disappears before impact.
- Ground-missile candidate: replace only the direct `MissileTrailVfxView` call with a neutral ECS request plus a documented managed presentation consumer. Producer order, consumer order, transient-request cleanup, pooled trail lifecycle, and the two-file future expanded allowlist are explicit; no implementation is authorized by AM-010.
- Transport findings: boarding owns validation, landed/reach decisions, capacity revalidation, cancellation, movement retry, and handoff while `UnitTransportPassengerStateSystem` retains onboard-state authority. The active `UnityEngine.Time.frameCount` diagnostics throttle is explicit. Multiple-grid and duplicate-passenger-buffer invariants require AM-011 characterization.
- Transport candidate: extract only duplicated passenger-kind, capacity, and occupancy rules into a stateless Burst-compatible utility. Query ownership, update order, stale-decision checks, diagnostics, ECB playback, and passenger mutation remain in their current owners.
- Evidence: `Design/AgentReports/ArchitectureMaturity/selected_owner_responsibility_maps.json` and deterministic matching Markdown, generated by `Tools/CI/architecture_owner_responsibility_maps.py` and enforced by `Tools/CI/tests/test_architecture_owner_responsibility_maps.py`.
- Validation: five focused AM-010 tests and 41 integrated ownership/lifecycle/ranking/dashboard/responsibility-map Python tests passed. Exact AM-009 row pairing, all cited source/test hashes, baseline diffs, line bounds, renderer/validator hashes, deterministic byte-for-byte Markdown regeneration, nonempty future expanded allowlists, order-independent overlap rejection, JSON parsing, Python compilation, and `git diff --check` passed. No production C# changed, so Unity compilation was not required for this documentation/CI slice.
- Independent review: initial reviews rejected missing cross-file freshness, an incomplete trail consumer/cleanup boundary, unsupported authority language, an omitted managed dependency, inaccurate evidence ranges, overbroad stale-decision wording, pairing/overlap validator loopholes, and independent Markdown drift. All findings were resolved; final rereview returned `PASS`.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, production behavior, scenes, prefabs, packages, and `ProjectSettings` were read only where cited and were not edited by AM-010.
- Next task: `AM-011` adds missing characterization tests for the recorded ground-missile phase/cleanup/trail boundary and transport capacity/occupancy/configuration invariants before any AM-012 production extraction.

### 2026-07-16 - AM-011 - Selected owner characterization coverage

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted implementation and evidence commit: `d2690f6cc14334c9179fa0550db8a16d753ff22a`, tree `562145cea01d6a75146fa616b2af00aa061e4139`.
- Result: nine focused characterizations now freeze the selected ground-missile and transport-boarding behaviors before production extraction. No production C# source changed.
- Ground-missile coverage: recovering-phase countdown without transition or cleanup and missing-projectile in-flight launcher cleanup are recorded as known limitations; trail reuse, missing-frame tolerance, release, and return-to-pool behavior are preserved contracts.
- Transport coverage: duplicate live passenger entries consuming capacity and multiple-grid singleton rejection are recorded as known limitations. Missing passenger entries, vehicle cargo-capacity requirements, cargo soldier-capacity override, and boarding-target passenger-kind fallback are preserved contracts.
- Unity validation: ground characterization `3 / 3`; transport characterization `6 / 6`; complete transport regression `88 / 88`; ground attack regression `5 / 5`; ground visual regression `1 / 1`; zero compiler errors in all accepted runs.
- Performance evidence: both selected production owners were measured for 16 warmup and 64 measured scenarios with zero current-thread allocation. Ground missile averaged `1.314 ms` total and `1.195 ms` simulation with `1.376 ms` total P95; transport boarding averaged `2.259 ms` total and `0.386 ms` boarding update with `2.541 ms` total P95.
- Evidence: `Design/AgentReports/ArchitectureMaturity/selected_owner_characterization_evidence.json`, the two tracked `am011_*_performance.json` reports, and their fail-closed Python validator. AM-010 responsibility-map freshness continues to protect all production sources while permitting later characterization tasks to evolve cited test files.
- Validation: 11 focused AM-010/AM-011 evidence tests and 47 integrated ownership/lifecycle/ranking/dashboard/responsibility/characterization tests passed; Python compilation, JSON parsing, exact hashes, deterministic responsibility-map regeneration, `git diff --check`, direct Unity execution, and post-rebase identity checks passed.
- Independent review: the first review rejected a multiple-grid test that bypassed the production system, an incorrect full-runner count, and an unasserted stale-entry claim. The final implementation invokes the unmanaged system directly, enforces the exact `88` count in code and evidence, and asserts retained null/destroyed entries; final rereview returned `PASS`.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, gameplay production behavior, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-011.
- Next task: `AM-012` extracts one bounded capability at a time behind existing contracts, starting with the stateless transport passenger-kind/capacity/occupancy rules unless fresh evidence invalidates that lower-risk extraction candidate.

### 2026-07-16 - AM-012 - Transport boarding capacity rules extraction

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted implementation and evidence commit: `1b549d0811cd081a0a586bc3cf9b33344d1b921d`, tree `bca11356c657e877cdd5e19f7eff54d57607d1b6`; source baseline after the disjoint operation-map integration: `50e574c87f09730148982d5dacb9269005e7d626`, tree `48c755aec9847fe762a25975c64fc69a5b3c2c48`.
- Result: duplicated passenger-kind normalization, capacity resolution, and occupancy classification moved from the selected boarding owner into `UnitTransportBoardingCapacityRules`. The selected owner dropped the duplicate rule bodies while retaining all ECS and gameplay authority.
- Boundary: the rules type is stateless, accepts explicit unmanaged values, and owns no World, query, lookup, buffer iteration, container, request, diagnostics, scheduling, command playback, mutation, or cleanup. `UnitTransportBoardingSystem` remains an unmanaged `ISystem`; its Burst job and main-thread stale-decision revalidation call the same rules at their original update positions.
- Behavior proof: six direct rules tests, six AM-011 transport characterizations, the complete `88 / 88` transport regression batch, and the broad architecture/naming contract passed with zero compiler errors. Known duplicate-entry and multiple-grid limitations remain characterized and unchanged.
- Performance: fresh focused transport evidence used 16 warmup and 64 measured scenarios, boarded and disembarked 512 passengers, averaged `1.400 ms` total with `1.504 ms` P95, and allocated zero current-thread bytes. This is below the AM-011 `2.259 ms` average / `2.541 ms` P95 baseline and its accepted 25% non-regression margin.
- Evidence: `Design/AgentReports/ArchitectureMaturity/transport_capacity_rules_extraction_evidence.json`, `am012_transport_boarding_performance.json`, direct rule tests, and the fail-closed Python validator bind exact production/test hashes, exact two-file production scope, historical AM-011 identity, current responsibility-map authorization, Unity pass counts, naming constraints, and performance comparison.
- Validation: 18 focused responsibility/characterization/extraction checks and 54 integrated ownership/lifecycle/ranking/dashboard/responsibility/characterization/extraction checks passed; Python compilation, JSON parsing, deterministic Markdown regeneration, exact diff scope, post-rebase Unity compilation, and `git diff --check` passed.
- Independent review: the initial review found a production-scope proof gap and one stale landed-state reference. Exact pre/post-acceptance production-diff enforcement and the corrected reference resolved both; final rereview returned `PASS`.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-012. The disjoint operation-map commit was rebased intact and became the extraction baseline.
- Next task: `AM-013` audits duplicated query-cache, command-queue, fixed-capacity scratch, and projection-cache mechanics and consolidates only a narrow contract with measured duplication and no new broad owner.

### 2026-07-16 - AM-013 - World-scoped component query-cache consolidation

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted implementation commit: `664ae7fa4544699faad7da01b11db60434e39088`, tree `3d9f539d83f6771103d0470b2024fd3c4757b499`; source baseline `64b7ff7d5b8c06dbd51439d7e1f12bb48762dd4a`, tree `fc44b1e1129ea60aecbd9bdf95f9f5326d4ecf7b`.
- Result: the two identical one-component, World-bound query wrappers were replaced by `WorldScopedComponentQueryCache<T>`. The faction-resource consumer preserves read/write storage access, while the hauler bridge preserves read-only move-order access. Both duplicate wrapper types and their metadata were removed.
- Retained boundaries: domain command queues remain separate because payload, ownership, consumption, failure, and overflow contracts differ. Fixed-capacity scratch remains tied to its owning query/budget invariants. Projection caches remain separate because their presentation lifecycles and invalidation keys differ. No generic cache service, static World/query, managed ECS system, coordinator-shaped helper, or additional state authority was introduced.
- Behavior proof: the shared cache directly verifies read-only matching, read/write matching, same-World reuse, and different-World rebinding. Existing faction-resource and building-resource suites preserve the affected consumers' behavior.
- Unity validation: shared cache `3 / 3`; faction resource `13 / 13`; building resource production `52 / 52`; broad architecture contract `1 / 1`; zero compiler errors in every accepted run.
- Evidence: `Design/AgentReports/ArchitectureMaturity/query_cache_consolidation_evidence.json` and `Tools/CI/tests/test_architecture_query_cache_consolidation_evidence.py` bind exact source/test hashes, exact production scope, deleted wrappers, consumer access modes, retained-separate decisions, and Unity pass counts.
- Validation: five focused AM-013 evidence tests and 59 integrated ownership/lifecycle/ranking/dashboard/responsibility/characterization/extraction/consolidation tests passed; Python compilation and `git diff --check` passed. A new performance capture was not required because query creation, World rebinding, and reuse behavior are unchanged; AM-017 owns the post-Phase-1 canonical recapture.
- Focused review: direct comparison against both deleted wrappers confirmed semantic equivalence, exact access modes, and no broad/static owner. Delegated reviewers produced no actionable handoff, so acceptance used the bounded local review plus the fail-closed evidence suite.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-013.
- Next task: `AM-014` adds update-order and behavior-equivalence tests for every Phase 1 decomposition that crossed a system or assembly boundary. The AM-012 capacity-rules extraction remains within the existing system/assembly; AM-013 changed a helper boundary only, so the audit must explicitly record whether any additional test is required rather than inventing a cross-system claim.

### 2026-07-16 - AM-014 - Phase 1 boundary-equivalence audit

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted audit commit: `c2b30a3fd892d42f7f7cc880dfcf47ccb68deadb`, tree `28b28b229b1ba8b08a2c4839815cc283ef9e3d10`; source baseline `fba2b7c8026419ee3d5fb2c7c71b080f4f4c1580`, tree `0ec20db95157c38039165e4152637d8030db048a`.
- Result: the complete accepted Phase 1 decomposition set contains AM-012 and AM-013 only. Both remain in `Game.Runtime`; neither creates, removes, or transfers authority to another ECS system. `UnitTransportBoardingSystem` remains the sole affected system and retains the same `SimulationSystemGroup` / `UpdateAfter(UnitGridMovementSystem)` contract.
- Test decision: no additional cross-boundary test was added because no accepted decomposition crosses a system or assembly boundary. AM-012 retains its direct capacity-rules, six characterization, and `88 / 88` full-regression coverage. AM-013 retains its shared-cache, faction-resource, and building-resource suites. Creating an update-order test for a nonexistent boundary would weaken rather than clarify the contract.
- Fail-closed evidence: `Design/AgentReports/ArchitectureMaturity/boundary_equivalence_audit_evidence.json` and `Tools/CI/tests/test_architecture_boundary_equivalence_evidence.py` bind the exact AM-012/AM-013 evidence hashes and accepted identities, resolve the owning asmdef at each pre/post commit, detect unmanaged `ISystem` and managed `SystemBase` declarations, compare the retained system's update-order attributes, require nonempty tests for any declared crossing, and reject production-source changes in the AM-014 slice.
- Validation: five focused AM-014 checks and 64 integrated ownership/lifecycle/ranking/dashboard/responsibility/characterization/extraction/consolidation/boundary checks passed; Python compilation and `git diff --check` passed. Unity was not rerun because AM-014 changes only evidence and CI, and it validates content-bound green Unity evidence from AM-012 and AM-013.
- Focused review: local review strengthened system discovery to cover both unmanaged and managed ECS systems. The delegated reviewer remained running without a handoff and was closed; no acceptance claim depends on that missing review.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, production C#, tests, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-014.
- Next task: `AM-015` selects the highest-risk remaining UI/presentation helper from current ownership evidence, confirms characterization coverage, and performs one measured decomposition without creating another broad owner.

### 2026-07-16 - AM-015 - Resource Exchange shell lifecycle decomposition

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Characterization commit: `b1c463f66d51f7a7d8e1fec5100af42c87fff9da`. Accepted implementation commit: `09fb62a4ae7303894cace678442ae311988bb956`, tree `f951386a2fd70f55da53969bac3137b768a4d2b2`; post-operation-map source baseline `e18d3d650374a04b59a34f5a01c8bbafd6a15a4f`, tree `e9d5308e3eb9046c7e9062d5c3a2692e757fb621`.
- Selection: `UIShellContentView` is the current highest-ranked eligible recurring UI/presentation owner: screening rank `5`, score `12`, 952 lines, 52 state slots, fan-out 38, and 22 recent commits. Contracts-only rows own no recurring presentation behavior; the largest eligible `PresentationSystemHelper` ranked lower and lacked safe characterization coverage.
- Result: `ResourceExchangeShellBinding` now owns only the popup instance, view, close button/listener, and visual install/close/region-reset lifecycle. The shell retains route interpretation, typed action enqueue, current `MainMenuPlayUI` dependency, popup-region mutation, content versioning, and public compatibility methods. No `ISystem`, `SystemBase`, generic popup coordinator, service locator, or gameplay authority was introduced.
- Measured decomposition: the selected owner dropped from 952 lines / 41,372 bytes to 898 lines / 38,807 bytes and moved four mutable state slots. The new feature-specific binding is bounded at 94 lines / 3,160 bytes. AM-017 retains ownership of the post-Phase-1 focused and canonical frame-time/GC recapture.
- Characterization: Resource Exchange coverage expanded from 7 to 10 cases, adding direct close idempotence, popup-layer cleanup removing stale listeners, and runtime UI dependency transfer while open. Existing shell `14 / 14` and Settings `8 / 8` regressions remain green.
- Validation: Resource Exchange `10 / 10`, shell `14 / 14`, Settings `8 / 8`, and broad architecture/naming `1 / 1` passed with zero compiler errors. Five focused and 69 integrated evidence tests, Python compilation, and `git diff --check` passed.
- Review: initial review found stale popup ownership when `MainMenuPlayUI` changed, optional commit binding, and weak validation-source proof. The binding now transfers ownership explicitly, a direct test freezes that behavior, acceptance requires an immutable commit/tree, and exact runner source hashes/counts are enforced. Independent rereview returned `PASS`.
- Exclusions preserved: operation-map commits were rebased intact. Operation-map, FirstLaunch, audio, UI visual-lock assets, scenes, prefabs, packages, and `ProjectSettings` were not edited or staged by AM-015.
- Next task: `AM-016` updates source-growth and responsibility guardrails so the extracted lifecycle cannot regrow inside `UIShellContentView` or reappear as an oversized replacement owner.

### 2026-07-16 - AM-016 - Source responsibility and replacement-owner guardrails

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted implementation commit: `3950afd2288e8c7d326a33dca60124b02292bbda`, tree `16e471ecbef3b764d5c661a5cb4b5575ef1d8258`; integrated source baseline `7be5f22b365f282abdc247456ff2a5de533db279`, tree `8c66e40d929bac437d231627b5fac01bbbbf4bad`.
- Result: the canonical `architecture-source-growth` validator now freezes the decomposed Resource Exchange shell boundary rather than creating a parallel validator. `ResourceExchangeShellBinding` is fixed at 94 lines / 3,160 bytes / four state slots and `UIShellContentView` at 898 lines / 38,807 bytes / 49 state slots, with exact source hashes and required/forbidden responsibility symbols.
- Replacement-owner policy: changed or new production candidates under `Assets/Game/Scripts` cannot acquire an equivalent domain signature or grow a generic managed lifecycle owner beyond the accepted baseline. The guard compares distinct lifecycle symbols, total lifecycle occurrences, Resource Exchange occurrences, lines, and bytes; its generic threshold remains three and its replacement-owner allowlist is empty.
- Growth authority: four AM-013 authorizations remain bound to accepted commit `664ae7fa4544699faad7da01b11db60434e39088`. The historical 952-line shell authorization remains recorded but cannot override the stricter AM-016 ratchet.
- Integration authority: the accepted FirstLaunch sources were not edited. Their stale helper ceilings are superseded append-only by `D-202`, `D-203`, and `D-204`, each bound to accepted FirstLaunch commit `7be5f22b365f282abdc247456ff2a5de533db279`. Historical decisions `D-036`, `D-044`, and `D-048` remain unchanged. The user-requested editor commands moved from `Warline Capture > Performance` to `Game > Performance`; those two Editor-only paths are declared explicitly and change no runtime behavior.
- Evidence: `Design/AgentReports/ArchitectureMaturity/source_responsibility_guardrail_evidence.json`, `Design/Architecture/post_hardening_source_responsibility_guardrails.json`, and the hash-bound `am016_source_growth_validation.log` enforce exact scope, commit/tree ancestry, guarded-source identity, validator identity, accepted log identity, and no runtime production-source changes.
- Validation: canonical source-growth `17 / 17`, broad architecture/naming `1 / 1`, zero compiler errors, Python `5 / 5` focused and `74 / 74` integrated, and `git diff --check` passed after the latest FirstLaunch and operation-map integrations.
- Review: independent review found and drove fixes for weak generic-owner detection, shell-state substitution, missing immutable ancestry/log binding, same-symbol-count lifecycle growth, and retroactive FirstLaunch decision edits. The final immutable rereview of `3950afd2288e8c7d326a33dca60124b02292bbda` returned `PASS`.
- Exclusions preserved: operation-map, FirstLaunch source, audio, UI visual-lock, scenes, prefabs, packages, and `ProjectSettings` were not edited by AM-016. No runtime `ISystem`, `SystemBase`, scheduling, gameplay, or presentation behavior changed.
- Next task: `AM-017` recaptures focused and canonical Match performance/GC evidence after all Phase 1 integrations and rejects any behavior, frame-time, or allocation regression.

### 2026-07-16 - AM-017 - Capture policy frozen before measurement

- State: implementation and acceptance capture in progress; `AM-017` remains incomplete.
- Policy: `Design/AgentReports/ArchitectureMaturity/phase1_exit_capture_policy.json` freezes identity, environment, quality, resolution, instrumentation, measurement windows, exact GC ceilings, and the existing 25% non-regression precedent before any acceptance capture is evaluated.
- Frame gates: canonical Match P95 must pass both the accepted `20 ms` product ceiling and the entry-baseline relative ceiling of `5.14 ms`. Ground-missile and transport focused comparisons use the median of three complete measured batches and retain their exact baseline-plus-25% ceilings.
- GC gates: canonical Match remains `180` warmup plus `300` measured frames at no more than `1,024` player-relevant bytes. Every changed owner remains exactly zero recurring bytes; the fully bound Resource Exchange shell uses `180 + 300` unchanged-state frames and 100 measured open/close transitions after one warmup transition.
- Fail-closed rule: missing identity or capture fields, dirty state, behavior failure, source/runner drift, allocation failure, or either frame ceiling failure rejects the evidence. No threshold may be loosened after capture.

### 2026-07-16 - AM-017 - Phase 1 exit performance and GC accepted

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted evidence commit: integrated `a8c42b409da2174ec634b6751696e9c703075882`, tree `e1bcdd83d5fb086e4fefa9273a5a6e42790040fe`; original pre-rebase evidence commit `03b78663fd0237e721acfe7eb8a38d4ad8191161`. Focused capture remains exactly `9d4feba92e19945d96410b25250be09ffefd041b` and maps to integrated equivalent `e769858734ec957954c77fc39507dca23d491a86`; canonical capture remains exactly `cd6e764bd878c6d7cedcbaa3c5034f0f105825b6` and maps to integrated equivalent `1a57ef2d16d3076a7d45023941ba282ee9a08145`.
- Canonical frame result: the median of three complete runs selected run 3 at 1,442 frames, `2.777 ms` average, `4.121 ms` P95, and zero current-thread allocated bytes. It passed both the frozen `5.14 ms` relative ceiling and the `20 ms` product ceiling. The other complete runs were `4.826 / 7.704 ms` and `2.716 / 4.122 ms` average/P95.
- Canonical GC result: 180 warmup plus 300 measured frames produced `262 / 1,024` player-relevant bytes. All `646` samples and `34,666` bytes reconcile exactly across relevant/excluded and resolved/unresolved classifications; the separate attribution suite passed `41 / 41`.
- Focused performance result: ground missile selected the median `1.094 / 1.163 ms` average/P95 run against `1.6425 / 1.72 ms`; transport selected `1.475 / 1.551 ms` against `1.75 / 1.88 ms`; Resource Exchange backend, GC, unchanged shell, 100 open/close transitions, and four query-cache phases passed their frozen frame-time and zero-allocation gates.
- Behavior result: transport capacity, transport behavior, query-cache behavior, faction resources, building resources, Resource Exchange routing, UI shell, settings, source growth, and broad architecture passed `212 / 212`. Together with GC classification, the acceptance bundle records `253 / 253` Unity checks and zero compiler errors.
- Evidence integrity: `am017_focused_capture_manifest.json` binds all 18 focused logs to one exact commit/tree/environment/context, exact runner bytes at that commit, report hashes, compressed log hashes, raw log hashes, and success markers. The final binder governs 19 runner/source files, recomputes frame and transport medians, parses GC arithmetic from the hashed report, and rejects any path outside the AM-017 ownership set or inside protected operation-map, FirstLaunch, audio, or visual-lock domains.
- Rebase and worktree isolation: `am017_acceptance_record.json` preserves both original capture identities and their rebased equivalents, proves governed bytes are unchanged, and freezes the integrated baseline-to-evidence write set. Later unrelated operation-map or feature edits in the live worktree cannot invalidate, expand, or be staged with the accepted AM-017 scope.
- Validation: Python policy `4 / 4`, evidence `9 / 9`, combined focused `13 / 13`, and integrated `87 / 87` architecture tests passed; `git diff --check` passed. Independent review first rejected weak log provenance, three omitted runners, and evidence-only GC arithmetic; all findings were corrected and the final rereview returned `PASS` with no material findings.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, and `ProjectSettings` were not edited. No runtime gameplay, ECS scheduling, presentation behavior, or release-only device certification was changed or claimed.
- Phase result: Phase 1 exit gates are accepted. Selected owners remain decomposed without replacement-owner growth, and canonical behavior, frame-time, and managed-allocation gates are green.
- Next task: `AM-018` inventories production uses of global World lookup, mutable static caches, static event subscriptions, hidden singletons, and runtime object discovery.

### 2026-07-17 - AM-018 - Dependency and lifecycle hazard inventory accepted

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted evidence commit: post-operation-map refresh `02e8fd5b7f670e0cfb076f2d6198f89e23fa769a`, tree `81b2f731316d64a746f1df48a110d46b708ca002`; merged source baseline `d2990d16ce336085d2d83aa4cf4a7f196037070f`, tree `4e96416bc12ef979cce3d4f1529044c4e199a638`. The initial pre-merge evidence commit remains `e4e6be3a7ecee81a9350cca7242e5bcb7b729f5c`.
- Result: deterministic AM-018 evidence scans 1,002 non-Editor production C# files and records 352 findings: 87 global World lookups, 9 hidden singleton/global-access patterns, 241 static cache/state/reference candidates, 7 hierarchy/scene-root discovery rows, and 8 static event subscriptions.
- World classification: `32` composition edges, `25` presentation edges, `21` authoring/debug reads, and `9` hidden service-locator dependencies. HSL rows route through `AM-019`, `AM-020`, and `AM-022`; boundary classifications remain visible and are not permanent exemptions.
- Static-state classification: `64` cache lifecycle rows (`CLR`), `108` assignable mutable lifecycle rows (`MSL`), and `69` readonly-reference rows requiring immutable-table or lifecycle proof (`IRC`). Singleton rows distinguish six hidden/global mutable access points from three immutable fallback/null-object candidates.
- Discovery/subscription classification: five runtime discovery rows route to `AM-020`/`AM-022`; two Baker-only hierarchy lookups are authoring/debug with no runtime-remediation route. Static subscriptions distinguish six indirect pairs, one direct teardown owner, and one unpaired process-lifetime subscription.
- Evidence integrity: the source manifest hash-binds every scanned file; normative, executable, lifecycle, ownership, scorecard, exception, validator, and APH-703 taxonomy authorities are bound; tool/test hashes are embedded; protected exclusions are re-derived from the archived ownership authority rather than trusted from artifact data. `am018_acceptance_record.json` binds the exact five-file evidence commit/tree, hashes, scope, totals, and acceptance test.
- Validation: focused Python `11 / 11`, integrated architecture CI `102 / 102`, Python syntax, deterministic regeneration, exact baseline ancestry/tree, source/authority/tool hashes, protected-path provenance, routing, summary arithmetic, Markdown projection, and `git diff --check` passed. No AM-018 production C# changed, so Unity compilation was not required.
- Independent review: the first pass rejected static-state undercounting, missing scene-root enumeration, incomplete tool/exclusion provenance, Baker misrouting, and missing AM-020 routing for HSL rows. All five findings were fixed; final rereview returned `PASS` with no blocker.
- Exclusions preserved: operation-map, FirstLaunch, audio, UI visual-lock, gameplay/runtime behavior, scenes, prefabs, packages, and `ProjectSettings` were read only. The two live map material edits remained unstaged and are outside AM-018 scope.
- Next task: `AM-019` defines one standard World-bound query/entity cache contract covering positive and negative lookup, invalidation, rebind, disposal, and destroyed-entity recovery.

### 2026-07-17 - AM-019 - World-scoped query and entity cache contract accepted

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted evidence commit: `ea39268476bb4742edb131018c8c073efa4d34cf`, tree `b6f4d951945f47caab49bf91af4d527e13bce000`; implementation commit `e733d7021eef7fb5b80fe3bd0774e5bcc5aed224`, tree `2e7049984ee379e41ffb59fe538bd1ffa1f7a797`; source baseline `57faa4226dcc9ef0dea9d915849509a85e7963e7`, tree `1130cd87a590e9753f840d10015b55d9e2f79627`.
- Contract result: `WorldScopedComponentQueryCache<T>` now owns explicit World binding, positive and negative entity resolution, explicit invalidation, automatic rebind, idempotent terminal disposal, component-order-version structural invalidation, and destroyed or component-removed entity recovery without default-World lookup, static mutable state, or a new ECS update owner.
- Fail-closed boundaries: positive cardinality changes re-resolve and retain `GetSingletonEntity` duplicate failure; enableable component singleton resolution is rejected with `NotSupportedException`; returned queries are borrowed cache-owned handles that callers cannot dispose or retain across invalidation, rebind, or disposal; dependent jobs must complete before those lifecycle operations.
- Performance result: two governed production consumers and positive/negative singleton paths passed six `180`-warmup plus `300`-measurement phases with zero recurring managed allocation. The exact implementation capture recorded query P95 at `100 ns` or better and positive singleton P95 at `400 ns` on the validation host; these are evidence observations, not product frame ceilings.
- Lifecycle result: `11 / 11` behavior cases cover read-only/read-write shape, World rebind, positive reuse, negative persistence plus invalidation, duplicate fail-closed behavior, entity destruction, component loss, enableable rejection, live-World disposal, and dead-World disposal.
- Evidence integrity: `am019_world_scoped_cache_contract_evidence.json` binds the seven-file implementation diff, exact baseline/commit/tree ancestry, file hashes, three compressed Unity logs, result markers, zero-allocation lines, validation counts, exclusions, and independent-review closure. `am019_acceptance_record.json` binds the five-file evidence commit and acceptance authority.
- Validation: focused Python `24 / 24`, acceptance `4 / 4`, integrated architecture CI `116 / 116`, Unity lifecycle `11 / 11`, cache performance `2 / 2`, broad architecture `1 / 1`, zero compiler errors, and `git diff --check` passed.
- Independent review: the first pass rejected stale positive cardinality, enableable ambiguity, missing component-loss/dead-World tests, undefined borrowed-query ownership, and missing historical-evidence ancestry. All findings were fixed; final rereview returned `PASS` with no remaining architecture finding.
- Exclusions preserved: operation-map and runtime-parity integrations were merged without modification. Live map materials, Build Drawer prefab/editor files, FirstLaunch, audio, UI visual-lock, scenes, packages, and `ProjectSettings` remained outside AM-019 scope and unstaged.
- Next task: `AM-020` moves mutable runtime state that crosses World lifecycles into explicit World-owned systems, components, or lifecycle containers where practical.

Accepted AM-019 progress snapshot:

| Field | Accepted value |
|---|---|
| Checklist complete | `19 / 86` (`22.1%`) |
| Core Architecture Lane | `19 / 68` (`27.9%`); active |
| Current task | `AM-020` ready, not yet claimed |

### 2026-07-17 - AM-020 - World-owned runtime-state migration accepted

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Accepted implementation: initial World-owned state migration `f993c3084`, final responsibility/source-growth follow-up `eab79c2f0`, final tree `87845d4fa57f3b44623f1ee1d4d96f338461e91c`; evidence commit `0c7d9e085`, tree `6890951fbbcf62757a399d13acc5b267f4f43481`.
- Ownership result: the threat-warning mailbox is an ECS component in the active World; the unmanaged detection `ISystem` owns writes; `ThreatWarningPresentationState` owns the presentation query cache and disposal; Match Intro and Match bootstrap use explicit supplied World bindings without recurring default-World discovery.
- Responsibility result: threat-warning audio resolution moved out of detection into a bounded event utility, and threat presentation/cache ownership moved out of the runtime update coordinator. Governed oversized files remain within their exact source-growth ceilings without a replacement oversized helper.
- Lifecycle result: isolated Worlds, World recreation, idempotent reset/disposal, missing or duplicate singleton failure, explicit Match Intro binding, missing-boundary recovery, rebind, and duplicate-boundary failure are covered by `18 / 18` focused cases.
- Performance result: three governed cache consumers, positive/negative singleton lookup, threat state, and Match Intro state passed ten `180`-warmup plus `300`-measurement phases with exactly zero recurring managed bytes.
- Validation: World lifecycle behavior `18 / 18`, cache performance `4 / 4`, alert/audio behavior `9 / 9`, source-growth `17 / 17`, broad architecture `1 / 1`, integrated architecture `125 / 125`, current-source Unity compilation with zero compiler errors, and `git diff --check` passed.
- Independent review: the final extraction review found one stale reflective allocation fixture. The fixture and Python guard were migrated to `ThreatWarningPresentationState._queryCache`; rereview returned `PASS` with no remaining blocker.
- Evidence integrity: `am020_world_owned_runtime_state_evidence.json` hash-binds the implementation history, final files, six compressed validation logs, ownership contract, and review closure. `am020_acceptance_record.json` binds the eight-file evidence commit and routes the next dependency-ready work to `AM-021`.
- Exclusions preserved: operation-map, static-map, FirstLaunch, unrelated audio, UI visual-lock, scenes, prefabs, packages, and `ProjectSettings` remained outside AM-020 staging. Concurrent FirstLaunch art was explicitly left unstaged.
- Next task: `AM-021` assigns explicit creation and disposal ownership to every persistent native container, query, event subscription, and presentation root.

Accepted AM-020 progress snapshot:

| Field | Accepted value |
|---|---|
| Checklist complete | `20 / 86` (`23.3%`) |
| Core Architecture Lane | `20 / 68` (`29.4%`); active |
| Current task | `AM-021` ready |

### 2026-07-17 - AM-021 - Persistent-resource ownership matrix and UI lifecycle slice

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Implementation identity: rebased commit `7b1f4835b7db2f5a7ed5d094f235e26472f5a9c5`, tree `c10cbfc5e10f2d98a25e1ccde23ca9880643598d`. AM-021 remains in progress and receives no checklist credit from this partial slice.
- Inventory result: the deterministic ownership matrix records `580` persistent resources: `76` native containers, `454` query handles, `24` event subscriptions, and `26` presentation roots. It classifies `537` resources with explicit owners, `21` open ownership gaps, and `22` separately owned or protected rows that remain visible but unchanged.
- UI lifecycle result: `RuntimeLogBuffer` now has a subsystem-registration reset that unsubscribes and clears process-lifetime state; menu bootstrap shutdown unbinds full-map publisher events; and the UI shell gateway centrally releases all eight static World-bound query handles on subsystem registration and World replacement.
- Validation: focused lifecycle `2 / 2`, source-growth `17 / 17`, ownership generator `10 / 10`, and integrated architecture CI `139 / 139` passed; Unity reported zero compiler errors. Deterministic pinned-baseline regeneration/check and `git diff --check` also passed.
- Remaining ownership gaps: five split-owner grid containers, five Scenario Lab containers, two building-resource query caches, three tactical-follow query-cache owner fields, and six presentation roots. The matrix records each exact path, owner, and missing disposal boundary.
- Exclusions preserved: operation-map, active map-generation work, FirstLaunch, audio-owned behavior, UI visual-lock assets, scenes, prefabs, packages, and `ProjectSettings` were not edited by this slice. The map-owned grid rows remain serialized behind the active map lane.
- Next action: resolve the non-map query-cache and presentation-root gaps, then coordinate the five grid-container ownership changes only after the map lane releases those paths.

### 2026-07-17 - AM-021 - Persistent query-cache lifecycle slice

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Implementation identity: rebased commit `e1c66f1b069e3793f7042cd4af66603e5afd3fe9`, tree `54b4fe9e9d5b78cb525c39477821190a66507600`. AM-021 remains in progress and receives no checklist credit from this partial slice.
- Ownership result: explicit ownership increased from `537` to `542` and open gaps fell from `21` to `16`. Building gameplay shutdown now owns the faction-storage and hauler move-order caches; selection gameplay shutdown owns all three tactical-follow state caches plus the tactical mode's five direct singleton queries.
- Lifecycle result: every affected cache releases live queries before World replacement, supports idempotent terminal disposal, and rejects post-disposal reuse. Building shutdown uses nested `finally` blocks so both cache owners are released even if another presentation cleanup fails.
- Growth result: lifecycle behavior moved into bounded feature-specific partial files. All pre-existing governed owners remain at or below their accepted line and byte ceilings; no growth exception, broad coordinator, static registry, `SystemBase`, or new ECS update owner was introduced.
- Validation: focused ownership lifecycle `5 / 5`, tactical-follow World-rebind `1 / 1`, source-growth `17 / 17`, ownership generator `10 / 10`, and integrated architecture CI `139 / 139` passed. Unity and generated-project compilation reported zero errors; deterministic pinned-baseline regeneration/check and `git diff --check` passed.
- Integrated-baseline note: the broad architecture contract passed `52 / 59`; its seven failures are confined to separately owned operation-map, FirstLaunch/UI, and package-bound paths. Those paths were preserved and not staged with AM-021.
- Exclusions preserved: operation-map, map generation, Scenario Lab, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, and `ProjectSettings` were not edited by this slice.
- Next action: resolve the six non-map presentation-root gaps while the five grid-container and five Scenario Lab rows remain serialized with map ownership.

### 2026-07-17 - AM-021 - Presentation-root lifecycle slice

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Implementation identity: rebased commit `008e42d8489a68c523dd7c6729812bb02414e9ed`, tree `ea6e2a1a64a94869f9cfb4ddd73bab47078c335c`. AM-021 remains in progress and receives no checklist credit from this partial slice.
- Ownership result: explicit ownership increased from `542` to `548`, open gaps fell from `16` to `10`, and all `26` presentation-root rows now have explicit lifecycle owners. The remaining gaps are five grid-component native containers and their five Scenario Lab mirrors.
- Lifecycle result: subsystem registration and idempotent `ReleaseAll` own both process-scoped combat VFX roots and generated trail materials; runtime-city disposal destroys both visual roots; building gameplay shutdown destroys the placement and transport roots and clears transport pools, lookup caches, pooled state, scratch storage, and the generated rope material.
- Growth result: lifecycle behavior moved into bounded feature-specific partial files. The four governed owners remain at or below their accepted ceilings; the runtime-city visual helper remains inside its exact existing `166`-line, `6,374`-byte authorization without expanding that authorization.
- Validation: focused ownership lifecycle `7 / 7`, source-growth `17 / 17`, ownership generator `10 / 10`, and integrated architecture CI `139 / 139` passed. Unity reported zero compiler errors; deterministic pinned-baseline regeneration/check and `git diff --check` passed.
- Integrated-baseline note: the broad architecture contract remains `52 / 59`; its seven failures are confined to separately owned operation-map, FirstLaunch/UI, and package-bound paths. Those paths were preserved and not staged with AM-021.
- Exclusions preserved: active `*OperationMap*` source, operation-map scenes and documents, Scenario Lab, FirstLaunch, audio, UI visual-lock, prefabs, packages, and `ProjectSettings` were not edited by this slice.
- Next action: close the five Scenario Lab native-container mirrors while the five split-owner grid containers remain serialized with the active map lane.

### 2026-07-17 - AM-021 - Scenario Lab grid ownership slice

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Implementation identity: rebased commit `981dcc2dcbe2163aebd5001ec7e1c3912f189de7`, tree `930a9492b9250fdc88337d892bf6bb65ade1b7ee`. AM-021 remains in progress and receives no checklist credit from this partial slice.
- Ownership result: five redundant persistent native-container mirrors were removed from `BattleScenarioLabVisualPlayback`. The deterministic matrix now covers `575` resources with `548` explicit owners, `5` open gaps, and `22` protected-owner rows.
- Lifecycle result: Scenario Lab now allocates its blocker, occupancy, faction-pass, and path-pool containers directly into the runtime grid entity. The MonoBehaviour no longer retains aliasing handles, eliminating ambiguous World-versus-view shutdown ownership and double-disposal risk.
- Validation: focused ownership lifecycle `8 / 8` and ownership generator `10 / 10` passed. Final rebased Unity compilation reported zero compiler errors; deterministic pinned-baseline regeneration/check and `git diff --check` passed. Integrated architecture CI passes `138 / 139`; its sole failure is the separately owned `FirstLaunchLanguageChoiceView` guardrail evidence.
- External guardrail blocker: the final source-growth run reaches the separately owned FirstLaunch additions and rejects four new `*SystemHelper` paths without approved entries. This slice did not edit those paths; its Scenario Lab production file shrank by `39` lines and grew by only `12` replacement lines, so no task-owned source-growth regression was introduced.
- Deferred-release observation: broad Python discovery passed `368 / 373`; five APH-506 release-device mock tests fail because their mock properties omit `ro.serialno`. They are outside AM-021 and do not activate or gate the deferred Release Certification Lane.
- Exclusions preserved: grid components and map systems, active `*OperationMap*` source, operation-map scenes and documents, FirstLaunch, audio, UI visual-lock, prefabs, packages, and `ProjectSettings` were not edited by this slice.
- Next action: wait for an explicit map-owner handoff before resolving the five split-owner grid-container rows; continue only dependency-safe preparatory Core work meanwhile.

### 2026-07-17 - AM-021 - Ownership evidence freshness check

- Workflow path: direct commits to `main`; no branch, worktree, or PR workflow.
- Evidence result: the deterministic matrix was regenerated after separately owned FirstLaunch source moved protected rows. It still covers `575` resources with `548` explicit owners, `5` open map-owned gaps, and `22` protected-owner rows.
- Scope result: no production source changed. Operation-map, map generation, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, and unrelated local work were preserved.
- Validation: deterministic ownership generation/check and `git diff --check` passed. Compiler validation was not required because this slice changes generated Markdown/JSON evidence and tracker text only.
- Blocker: all dependency-safe Phase 2 and UI preparatory packages are complete. AM-021 cannot close until the map owner hands off or resolves the five split-owner grid containers.

### 2026-07-17 - AM-026 - UI projection allocation inventory accepted in parallel

- Dependency decision: AM-026 is audit-only, changes no runtime ownership, and may earn completion credit while AM-021 is blocked. AM-027 through AM-035 remain gated until the Phase 2 exit contract is accepted.
- Inventory result: 29 unique surfaces cover Menu, Match HUD, drawers, popups, tooltips, selection, ARIA, routes, and command-wheel presentation. Every row records polling/apply ownership, source/version identity, managed rebuild behavior, allocation coverage, and remediation routing.
- Reconciliation result: all declared shell routes and popup kinds are classified; `AM-WP-001` through `AM-WP-023` exist exactly once and use the seven-section bounded-package contract.
- Validation: focused inventory contract `7 / 7` and `git diff --check` passed. Integrated architecture passed `145 / 146`; its sole failure is the separately owned `FirstLaunchLanguageChoiceView` source-responsibility guardrail. Compiler validation was not required because no production C# or Unity asset changed.
- Evidence: `am026_ui_projection_inventory_acceptance.json` binds the inventory, CI test, package-set hash, source baseline, and implementation commit `0664ecb12`.
- Scope preserved: operation-map, map generation, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, and unrelated local work were not edited.

### 2026-07-17 - AM-021 - Persistent-resource ownership accepted

- Handoff: Gameplay-Clone commit `b2392edc1` consolidated the five runtime-grid containers under one creation, resize, and disposal owner. Architecture review corrected the non-ECS helper to the contract-compliant `RuntimeGridPersistentStorageUtilitySystemHelper` name in `18423c3b3` while preserving both `.meta` GUIDs.
- Ownership result: the canonical deterministic matrix covers `575` resources with `553` explicit owners, `0` ownership gaps, and `22` separately owned/protected rows.
- Lifecycle result: disposal clears component aliases, repeated teardown is idempotent, same-size ensure preserves state, resize replaces grid-sized storage, and resized storage clears pooled path cells.
- Validation: runtime-grid storage `4 / 4`, persistent lifecycle `8 / 8`, and ownership generator `11 / 11` passed; Unity reported zero compiler errors. Integrated architecture passed `146 / 147`; its sole failure remains the separately owned `FirstLaunchLanguageChoiceView` guardrail. `git diff --check` passed.
- Evidence: `am021_persistent_resource_ownership_acceptance.json`, canonical JSON/Markdown ownership reports, and the Gameplay-Clone handoff report bind the accepted implementation and validation identity.
- Next action: dispatch `AM-022` using `AM-WP-024`; do not start transition stress work until the recovery matrix passes.

### 2026-07-17 - AM-022 - Editor lifecycle recovery matrix slice

- Scope: added only the allowlisted `WorldLifecycleRecoveryMatrixTests` fixture and its `.meta`; no production or protected-domain source changed.
- Authority result: the fixture binds the accepted AM-021 matrix, requires `575` resources, `553` explicit owners, `0` gaps, the contract-compliant runtime-grid owner, and resolvable production source paths.
- Recovery result: seven tests cover World/cache/gateway replacement, missing/destroyed/component-removed/duplicate singletons, Road and Building Placement command replacement, buffer repair, monotonic placement transaction identity, subsystem reset, exact system-handle reuse, and an integrated Editor recovery sequence.
- Validation: Unity 6000.5.2f1 passed `7 / 7` with zero compiler errors; `git diff --check` passed. Compressed log and NUnit XML are hash-bound in `am022_world_lifecycle_recovery_evidence.json`.
- Remaining: bounded scene/root/subscription PlayMode recovery, then `180 + 300` recovered-path zero-allocation proof and final integrated acceptance. AM-023 remains gated.

### 2026-07-17 - AM-022 - PlayMode scene/root/subscription recovery slice

- Scope: added only the allowlisted `WorldSceneLifecycleRecoveryPlayModeTests` fixture and its `.meta`; production scenes and protected source remain read-only.
- Production transition result: the existing APH-805 Menu -> Match -> Menu lifecycle behavior runs inside the AM-022 fixture and proves one lifecycle root, operation-map teardown, destroyed Match view, cleared Match dependencies, and removed Match HUD content.
- Scene/root result: unloading a temporary runtime scene destroys its scene-owned missile presentation root; repeated release clears static ownership; a replacement scene creates exactly one replacement root.
- Subscription result: repeated RuntimeLogBuffer subsystem reset clears initialization and retained entries; repeated initialization rebinds one process owner without stale retained state.
- Validation: Unity 6000.5.2f1 PlayMode passed `2 / 2` in `14.1283987` seconds with zero compiler errors; `git diff --check` passed. Compressed log and NUnit XML are hash-bound in the AM-022 evidence.
- Remaining: `180 + 300` recovered-path zero-allocation proof, integrated architecture validation, and final AM-022 acceptance. AM-023 remains gated.

### 2026-07-17 - AM-022 - Lifecycle recovery accepted

- Implementation identity: `3ffb08ab786b794a11d9e7bf7f7f392e7f495e07`, tree `b24591cc3f1e3597e18372917e98ff88e428724e`.
- Recovery result: all ten AM-WP-024 case families pass across the `8 / 8` Editor matrix and `2 / 2` PlayMode matrix, including the production Menu -> Match -> Menu lifecycle transition.
- Allocation result: after `180` warmup operations, `300` recovered-World cache/runtime-state reads allocate exactly `0` recurring production-owned managed bytes.
- Validation: Unity 6000.5.2f1 reports zero compiler errors; focused Editor and PlayMode suites pass; integrated architecture passes `146 / 147` with the sole failure in separately owned `FirstLaunchLanguageChoiceView`; `git diff --check` passes.
- Follow-up resolution: the lifecycle-owner guardrail amendment at `cff736f1a1faaff8f4e0e9b5d49fa5c53c1363d2`, accepted by `783b8fc93`, removes the false positive without allowlisting the view; canonical integrated architecture now passes `147 / 147`.
- Evidence: `am022_acceptance_record.json` and `am022_world_lifecycle_recovery_evidence.json` bind implementation identity, test sources, all compressed logs/results, AM-021 authority, measured windows, and external-failure ownership.
- Scope preserved: no production, operation-map, FirstLaunch, audio, UI visual-lock, scene, prefab, package, `ProjectSettings`, or release-certification source changed.
- Next action: dispatch AM-023 from `AM-WP-025`; AM-024 remains gated until the accepted 10-cycle transition stress passes.

### 2026-07-18 - AM-023 - Bounded Menu/Match lifecycle stress accepted

- Implementation identity: `1a2e4a9921ab0867c00f20e74f2397fc5507c922`, tree `192b78fde448f6eda9d84902f68ae4ca730545a3`.
- Scope result: one warm-up plus 10 measured production Menu -> Match -> Menu cycles completed in one unchanged default World. The former 100-cycle extended stress policy is deferred until a maturity or release-certification gate justifies it.
- Stability result: Menu remained at `494` entities, one scene/root, one lifecycle root, zero Match/map/HUD roots, and one listener; Match remained at `2027` entities, two scenes, 17 roots, one lifecycle/map/Match/HUD root, and one listener at baseline, cycle 5, and cycle 10.
- System result: `UiShellFlowSystem` and `UiActionRequestSystem` retained the same non-null unmanaged system handles; managed system count remained `69` at every checkpoint.
- Defect closed: repeated transition exposed a destroyed Unity-backed `IMatchHudSelectionPanelView` retained during the teardown/rebind gap. `SelectionHudFeedbackUiSystemHelper` now normalizes that stale interface to null and clears its cache before the replacement HUD binds.
- Validation: Unity 6000.5.2f1 PlayMode passed `1 / 1` in `188.705594` seconds with zero compiler errors; focused static contract passed `5 / 5`; canonical architecture passed `152 / 152`; `git diff --check` passed.
- Evidence: `am023_menu_match_lifecycle_stress_evidence.json`, `am023_acceptance_record.json`, and compressed PlayMode/static/integrated logs bind the exact implementation, snapshots, source hashes, and validation results.
- Scope preserved: no operation-map, FirstLaunch, audio, UI visual-lock, scene, prefab, package, `ProjectSettings`, or release-certification file changed.
- Next action: dispatch AM-024 from `AM-WP-026` for retained native allocation and approved pool-count trend evidence.

### 2026-07-18 - AM-024 - Lifecycle memory and pool trend accepted

- Implementation identity: `233e575b73bb09f8adc48105382ef72d9aa574a9`, tree `b52a34383aba2224fb3b2c466af71afed11de233`.
- Bounded result: one warm-up plus five measured production Menu -> Match -> Menu cycles completed in `82.170742` seconds; the former three-warm-up/twelve-measured trend remains deferred.
- Structural result: Menu stayed at `494` entities with zero Match/map/HUD/path-pool owners; Match stayed at `2027` entities with one map/HUD/path-pool owner and path-pool capacity `1024`. Audio stayed at pool size `8` with one active persistent shell source in both phases. Combat VFX active/created counts stayed `0/0`.
- System result: World sequence, managed system count `69`, `UiShellFlowSystem`, and `UiActionRequestSystem` handles remained stable at every checkpoint.
- Memory result: every Editor investigation ceiling was exceeded and is recorded exactly in evidence. These totals include scene/asset/test-runner retention without forced collection; they are supporting signals and remain a release-lane investigation, not a contradiction of the zero-growth structural owner/pool evidence.
- Validation: Unity 6000.5.2f1 PlayMode passed `8 / 8`; focused static contract passed `5 / 5`; integrated architecture passed `157 / 157`; post-rebase compilation reported zero compiler errors; `git diff --check` passed.
- Scope preserved: no production, operation-map, FirstLaunch, audio, UI visual-lock, scene, prefab, package, `ProjectSettings`, or release-certification source changed.
- Next action: dispatch AM-025 from `AM-WP-027` for Phase 2 exit acceptance and ownership delta publication.

### 2026-07-18 - AM-025 - Resource integration architecture audit

- Gameplay result: AI and player construction now have one clear in-match cost authority: tactical materials. Legacy credit fields remain migration data and are not deducted during construction.
- Lifecycle result: Resource Exchange feedback retains only the newest 32 messages, preventing unbounded memory growth during long matches.
- Architecture result: all four resource-owned helpers remain inside their existing approved line/byte limits. No exception, larger ceiling, new update owner, `SystemBase`, or parallel resource authority was introduced.
- Validation: Resource Exchange feedback `7 / 7`, AI construction `7 / 7`, building production `31 / 31`, Build Drawer projection `6 / 6`, combined architecture `176 / 176`, Phase 2 evidence `19 / 19`, deterministic regeneration, and `git diff --check` pass with zero compiler errors.
- External blocker: canonical Unity source-growth now reaches four FirstLaunch and four operation-map helpers. Those separately owned files remain untouched and must be resolved by their owners before AM-025 can close.
