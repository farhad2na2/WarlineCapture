# Post-Hardening Architecture Maturity Tracker

## Purpose

Move WarlineCapture from the `8.8 / 10` early-development architecture closeout to a practical, evidence-backed `9.5+ / 10` production architecture without making release-cycle device qualification a prerequisite for current code-quality work.

This is a post-hardening program, not a replacement for `architecture_performance_hardening_implementation_tracker.md`. The prerequisite tracker closed its early-development scope with `107 / 107` explicit dispositions, including 15 unpassed release-only certifications transferred to `pre_release_performance_certification_backlog.md`.

The program has two activation lanes. The **Core Architecture Lane** covers Phases 0-5, 7, and 8 and may begin now from the accepted early-development closeout. The **Release Certification Lane** covers Phases 6 and 9 and remains inactive until the pre-release backlog activation criteria are met. Core architecture tasks must preserve fail-closed release contracts but do not wait for long thermal, repeated cold/warm, or sustained release evidence.

When this program activates, every task must follow `Design/Architecture/agent_pull_request_review_merge_workflow.md` for shared-object worktrees, `codex/<task-id>-<slug>` branches, implementation ownership, independent findings-first review, risk-based integration, tracker administration, PR merge, and cleanup. This tracker remains authoritative for maturity scope, dependencies, acceptance, and evidence; the PR workflow is authoritative for git integration and role separation.

A literal `10 / 10` is treated as a sustained operating standard, not a one-time checkbox. The program can establish `9.5+` quality and the controls needed to retain it; production history must supply the final evidence.

## Authority And Entry Contract

| Field | Value |
|---|---|
| Document date | 2026-07-16 |
| Prerequisite tracker | `Design/Architecture/architecture_performance_hardening_implementation_tracker.md` |
| Git integration authority | `Design/Architecture/agent_pull_request_review_merge_workflow.md` |
| Core Architecture Lane prerequisite | Early-development tracker complete; compiler, architecture, critical behavior, Editor performance, and GC closeout gates green |
| Release Certification Lane prerequisite | Core Architecture Lane complete plus `pre_release_performance_certification_backlog.md` activated; Phase 9 additionally requires that backlog accepted |
| Assumed entry rating | approximately `8.8 / 10` |
| Practical target | evidence-backed `9.5+ / 10` |
| Aspirational target | sustained `10 / 10` operating standard |
| Program status | Ready to start Phase 0 in the Core Architecture Lane; Release Certification Lane deferred |

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
- Implementation agents push their task branch and open a PR but never merge or self-accept tracker state. The independent review/merge coordinator owns findings, final integration gates, administrative tracker/evidence commits, merge, and branch/worktree cleanup.
- Direct pushes may still be technically possible, but this planned program uses PRs. Do not claim branch protection/rulesets are active or require GitHub approval counts while all agents share one GitHub identity.
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
| Checklist complete | `3 / 86` (`3.5%`) |
| Core Architecture Lane | `3 / 68` (`4.4%`); active |
| Release Certification Lane | `0 / 18` (`0.0%`); deferred |
| Program state | Core Architecture Lane active; release work inactive |
| Current phase | Phase 0 - Entry Baseline And Scorecard |
| Current task | `AM-004` ready, not yet claimed |
| Core entry baseline | Prerequisite accepted by `AM-001`; environment identity accepted by `AM-002`; canonical scenarios accepted by `AM-003` at `a5bf7b72cdfb9457c6af1e98ee2bcaae983f9ff6`; refreshed gate evidence remains Phase 0 work |
| Release entry review | Deferred until `pre_release_performance_certification_backlog.md` activates |
| Architecture rating | Not re-audited for this program |
| Performance evidence | Must be recaptured at the exact entry commit |
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
- [ ] `AM-004` Freeze the core architecture scorecard, metric definitions, performance budgets, GC classification policy, memory-growth threshold, and evidence freshness rules. Keep release-only device-tier fields explicitly `measurement-required` until `AM-053`.
- [ ] `AM-005` Regenerate the architecture dashboard, produce the validator registry, and reject every required input reported as stale, unknown, malformed, duplicate-owned, or tied to a different commit.
- [ ] `AM-006` Produce a current source-size, dependency, assembly-cycle, runtime-loop, static-state, managed-helper, and active-work ownership inventory.
- [ ] `AM-007` Produce a lifecycle inventory for Worlds, persistent native containers, query caches, presentation pools, scene roots, event subscriptions, and static caches.
- [ ] `AM-008` Publish the entry baseline report with ratings by area, accepted evidence links, known residual risks, and the exact deltas this program owns.

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
- Lifecycle evidence runs at least 100 Menu -> Match -> Menu cycles. Phase 0 freezes the warmup-cycle count, sampled entity/native/pool/subscription fields, plateau tolerance, and permitted one-time caches before `AM-023` starts; no threshold may be invented after a failure.
- Memory evidence is not accepted from a single before/after sample. The scorecard defines sampling cadence, warmup, plateau window, slope limit, return-to-baseline tolerance, and metric source for managed, native, graphics, and pool memory.
- Evidence freshness is content-based: exact commit and environment are mandatory, and any change to a governed source/config/scenario/tool hash invalidates that row. Unrelated documentation changes do not require an expensive recapture when all governed hashes remain identical.
- Every measured artifact records instrumentation overhead or a paired instrumentation-off control when the recorder can materially affect the result.

## Phase 1 - Responsibility And Decomposition Hardening

Reduce change risk in oversized systems without replacing the architecture or changing gameplay authority.

`AM-009` scores candidates from `0-4` for responsibility count, dependency fan-in/fan-out, mutable/lifecycle state, measured runtime cost, and recent change frequency. Lines/bytes are recorded but used only as a tie-breaker. The first wave selects at most three non-overlapping owners and publishes the exact reason each was selected or rejected; no extraction begins from a raw largest-file list.

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
- Branch, isolated shared-object worktree, PR URL, baseline commit, tested head, and dirty-worktree exclusions.
- Exact allowed files, generated/config/scene authority, files intentionally not touched, active owners checked, and overlap resolution.
- Ownership change and behavior-preservation statement.
- Current/future owner, assembly boundary, update order, lifecycle, Burst/thread constraints, and explicit `ISystem`/`SystemBase` decision.
- Before/after source responsibility, dependency, performance, allocation, and memory metrics as applicable.
- Exact scorecard scenario/budget IDs, commands, pass markers, logs, profiler captures, screenshots, and device artifacts.
- Residual risks and unexercised paths.
- Rollback condition, recommended next task, and focused commit message.

The implementation agent owns substantive commits, pushes, PR creation, and review revisions but never merges. The independent coordinator reports findings first, returns substantive fixes to the implementer, runs integrated validation on the final head, and may then add only administrative snapshot, checklist, Decision Log, Implementation Log, or evidence commits. Proposed completion becomes authoritative only when the coordinator merges the PR to `main` and records cleanup.

## Decision Log

| Date | Decision | Reason | Evidence |
|---|---|---|---|
| 2026-07-13 | Treat complete accepted hardening and release certification as the production-maturity/release-lane entry condition, not the Core Architecture Lane entry condition | Completing known hardening debt produces approximately `8.8 / 10`; production maturity still requires complete UI/device coverage and sustained evidence, while code-quality work can proceed earlier | Architecture rating discussion, existing hardening tracker, pre-release backlog, and the 2026-07-16 lane split |
| 2026-07-13 | Target practical `9.5+`, with `10 / 10` as a sustained standard | No finite checklist proves permanent architectural perfection; permanent regression controls and multiple green releases provide stronger evidence | This tracker, Phases 8 and 9 |
| 2026-07-16 | Activate the Core Architecture Lane independently from release certification | Current development benefits from ownership, lifecycle, UI allocation, pool, determinism, diagnostics, and CI hardening; repeated long release qualification remains intentionally deferred until content stabilizes | User approval; completed early-development hardening tracker; pre-release backlog |
| 2026-07-16 | Make the pre-release backlog the sole release-certification obligation source | Phase 6 and Phase 9 must sequence existing backlog contracts rather than creating duplicate collectors, schemas, or evidence definitions | `pre_release_performance_certification_backlog.md` |

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
