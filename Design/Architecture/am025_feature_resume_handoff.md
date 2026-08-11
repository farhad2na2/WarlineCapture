# AM-025 Feature-Resume Handoff

Date: 2026-08-11
Status: Accepted architecture handoff. AM-027 is next and has not started.
Parent acceptance: `AM-025`
Closeout tracker: `am025_feature_readiness_architecture_closeout_tracker.md`
Exit evidence: `../AgentReports/ArchitectureMaturity/am025_phase2_exit_evidence.json`
Acceptance record: `../AgentReports/ArchitectureMaturity/am025_acceptance_record.json`

## 1. Resume Decision

The bounded architecture feature-readiness gate is complete. New feature planning may resume after this handoff is accepted, but this document does not itself start a feature or AM-027 implementation.

The next maturity task is `AM-027`: add or correct semantic ECS source versions before UI domains rely on change-driven projection caches. `AM-027` is required before new UI work that would otherwise add another polling, conversion, or projection owner. A non-UI feature may proceed only through its normal tracker authority and must consume the collision rule below when it touches an open maturity domain.

Accepted Phase 2 authority remains:

- `640` persistent resources: `567` explicit owners, `73` protected owners, `0` gaps;
- `425` current review rows: `420` resolved, `5` protected, `0` genuine debt, `0` unclassified;
- source growth `17 / 17`, architecture closeout `23` suites, and final compiler errors `0`;
- one accepted World/lifecycle owner, bounded recovery and transition behavior, bounded pool/native ownership, and zero recurring allocation in the governed recovered paths.

## 2. Feature-Collision Routing

| Feature change touches | Required maturity consumer | Rule before acceptance |
|---|---|---|
| Canvas/UI visible semantic data, managed strings/models, popup or HUD projection, or polling | Phase 3 `AM-027` through `AM-035` | Start with semantic source versions (`AM-027`), then use one World/boundary/version projection cache. Do not add a second writer, per-frame `FixedString.ToString()`, or view-local polling owner. |
| Runtime `Instantiate`, `Destroy`, `AddComponent`, material clone, presentation list growth, or pool expansion after warmup | Phase 4 `AM-036` through `AM-044` | Inventory the creation path, bind a data-backed capacity and exhaustion policy, prewarm where required, and prove stable post-warmup ownership/allocation. Gameplay authority may not depend on presentation capacity. |
| Combat, AI, pathfinding, economy, construction, transport, aircraft, commands, interruption, or failure recovery | Phase 5 `AM-045` through `AM-052` | Add the relevant deterministic seed/input/state-hash or bounded failure-path evidence. Presentation variance and diagnostics must not alter authoritative simulation state. |
| Production diagnostics, formatting, telemetry, profiler markers, capture suppression, or retained diagnostic buffers | Phase 7 `AM-063` through `AM-069` | Gate work before formatting, use bounded/preallocated storage, preserve subsystem reset, distinguish instrumentation cost, and require zero recurring managed allocation in normal production telemetry. |
| New dependency direction, runtime loop, global World lookup, mutable static, lifecycle resource, source exception, performance gate, or evidence schema | Phase 8 `AM-070` through `AM-078` | Extend the existing fail-closed enforcement in the same change. Record owner, rationale, measured effect, approval, review/removal condition, and focused evidence; never hide the row through an allowlist-only change. |

When one feature touches several rows, it consumes all relevant rows in one ordered plan. The feature tracker remains the behavior authority; the maturity row remains the architecture/evidence authority. Neither may create a second state, lifecycle, update, or publication owner.

## 3. Preserved Phase 2 Invariants

Every resumed change must preserve:

1. one explicit World/state owner and one transition owner;
2. one creator/resizer/disposer for persistent native storage and pools;
3. deterministic replacement-World behavior and destroyed/missing-singleton recovery;
4. no recurring discovery, service locator, unregistered polling loop, or process-wide gameplay state;
5. exact source-growth authority with no speculative headroom;
6. checked Unity compilation, applicable focused behavior/equivalence tests, deterministic evidence, protected-path audit, and `git diff --check`;
7. clean, pushed commits with no generated/recovery/log residue.

If a feature needs to change a protected owner, asset, threshold, device contract, or another domain's accepted authority, stop and obtain that exact handoff rather than broadening this document.

## 4. Deferred Release Lanes

Phase 6 (`AM-053` through `AM-062`) remains inactive until the pre-release performance certification backlog activates. Phase 9 (`AM-079` through `AM-086`) remains inactive until the Core Architecture Lane and Phase 6 are complete and the pre-release backlog is accepted.

This handoff does not request an Android reinstall, device run, thermal route, cold/warm launch series, sustained soak, graphics-memory certification, APK build, or release-candidate evidence. Existing accepted map/device evidence remains historical scenario evidence only and cannot substitute for a later activated release gate.

## 5. Residual Risks And Follow-Up

- Raw Unity Editor memory totals remain investigation-only; only structural-owner and governed-pool plateau evidence was accepted.
- The extended 100-cycle transition run remains deferred under its accepted contract.
- Protected audio and FirstLaunch owners remain outside AM-025 and require their own handoffs if a feature touches them.
- The five protected Phase 2 rows remain classified, owned, and non-credit; they were not silently resolved or transferred.
- Architecture maturity is ongoing governance, not permission to skip focused review when features collide with open rows.

## 6. Resume Sequence

1. Start `AM-027` from its existing Phase 3 authority; do not recreate or fork the accepted AM-025 evidence.
2. For the next user feature, declare its behavior scope and run the collision matrix above before editing.
3. Pair only the rows actually touched by that feature; leave unrelated maturity rows for their dependency order.
4. Commit and push each validated bounded slice cleanly.
5. Keep Phase 6 and Phase 9 deferred until their activation contracts pass.

Handoff result: feature planning is unblocked, `AM-027` is next/not-started, Phase 2 remains accepted, and no feature implementation was performed by this handoff.
