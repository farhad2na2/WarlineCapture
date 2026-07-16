# Post-Hardening Architecture Maturity Entry Baseline

- Artifact: `AM-008`
- Acceptance baseline: `9a0aa14252e6559680328e520d26c16bfc7b444e`
- Acceptance tree: `0f3bf4a00a69c417f5a92f3811d88250a7c8d5ef`
- Branch: `main`
- Environment identity SHA-256: `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`
- Required dashboard gate: `accepted`
- Release Certification Lane: `deferred`

This report closes the entry-baseline phase. It does not change production behavior and does not claim release readiness. Earlier Phase 0 artifacts remain accepted through their immutable content hashes and recorded source baselines; this report evaluates them together against the exact acceptance baseline above.

## Entry Rating

| Area | Weight | Entry | Target | Evidence basis | Program-owned delta |
|---|---:|---:|---:|---|---|
| Diagnostics and continuous governance | 10% | 8.6 | 9.5 | 24 canonical validators, 35 unique responsibilities, fail-closed current dashboard | Complete production-safe diagnostics and permanent CI enforcement in Phases 7-8 |
| ECS runtime ownership | 20% | 9.0 | 9.6 | Current owner/runtime-loop inventory; 21 asmdefs with zero dependency cycles | Remove implicit World authority and resolve lifecycle ownership without weakening unmanaged `ISystem` preference |
| Lifecycle and resource safety | 15% | 8.0 | 9.6 | Current World, native, query, pool, root, subscription, and static-cache inventory | Assign creator, capacity policy, disposer, invalidation, and lifecycle test to unresolved rows |
| Maintainability and testability | 15% | 8.2 | 9.5 | 95 production files over 500 lines, 23 over 1,000, and 37 large managed helpers | Characterize and decompose ranked responsibilities through bounded work packages |
| Modularity and dependency boundaries | 15% | 8.7 | 9.4 | 21 first-party assemblies, 92 edges, 102 external references, zero unowned scoped sources | Enforce dependency direction, remove domain leakage, and prevent new cycles continuously |
| Performance and GC discipline | 15% | 8.3 | 9.7 | Current bounded Match fixture passes 4.11 ms P95 and zero current-thread allocation | Prove owner-focused, UI-transition, unchanged-UI, pool, memory, and scenario budgets after each phase |
| UI presentation architecture | 10% | 8.2 | 9.4 | Current runtime-loop/owner inventory and canonical UI surface matrix | Convert remaining polling/rebuild paths to versioned, allocation-free projection and bounded pools |

Weighted entry rating: **8.5 / 10**. This is a planning baseline, not a `9.5` maturity claim. The rating can rise only through accepted task evidence; it cannot rise because documentation became more detailed.

## Current Required Gates

| Gate | Result | Evidence |
|---|---|---|
| Validator registry ownership | Accepted | `validator_registry.json`; 24 unique validator owners and no duplicate responsibility owner |
| Assembly/dependency evidence | Current | 21 assemblies, 92 first-party edges, 102 external references, 1,436 scoped source files, zero unowned scoped sources |
| Editor Match frame budget | Current and passed | 1,426 frames; 2.808 ms average; 4.112 ms P95 against 20 ms; 0 current-thread allocated bytes |
| Required dashboard inputs | Accepted | 2 required inputs current; 0 required rejected; 0 registry errors |
| Release-only advisory inputs | Deferred | Five stale advisory inputs remain visible and non-blocking until Release Certification Lane activation |

## Accepted Evidence Manifest

| Artifact | Source baseline | Current content SHA-256 |
|---|---|---|
| `entry_prerequisite_review.json` | `1a0f1eceb2d6c04359e2f868ddd9f99d26a0e713` | `83736b24e97ff9edd78a27c894ceee4557a76af415a2c32c3ffedca6059d7019` |
| `entry_environment.json` | `68c785502151a54b6f6e4bb115789c8957962403` | `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e` |
| `canonical_scenarios.json` | `a5bf7b72cdfb9457c6af1e98ee2bcaae983f9ff6` | `cf7e02297df96e98eb950eb3ba2f43ed16f30c2501b002d30ce58d9081e3dbf6` |
| `entry_scorecard.json` | `76f80c7a23b06ba6719593cb5f2815e476db7987` | `367af663e03f86f1adcd6cdcf168f5631f61787cc6cd23bcb5f6d39360274d21` |
| `exception_registry.json` | `76f80c7a23b06ba6719593cb5f2815e476db7987` | `1358714270df08b6ca3232a025e120546c6ffc6a4823b0ce014a0ead4944af06` |
| `validator_registry.json` | `9a0aa14252e6559680328e520d26c16bfc7b444e` | `ef862b538596b3bc0a06c96f1e27f67717387e7d8957418bbf08aac42c9221ae` |
| `ownership_inventory.json` | `202b53025d793d6bfe3f0379782b7e185e92be07` | `620f2d2556cb9e1ccb9b74ac2dfdb359f27d205051a763bde0a9b3a4a4f66e13` |
| `lifecycle_inventory.json` | `7f41260803d85f46f6bb11a0078b395bc30d1ee4` | `966243512afcd4a48173a30ca3cec60a342685a7a00cff5fe68c4c004a4a86d8` |
| `architecture_performance_dashboard.json` | `9a0aa14252e6559680328e520d26c16bfc7b444e` | `e7afc1c7cd414372b6cb4407220f268ba58d7a123193f91f703d0efa403e947a` |
| `2026-07-10_aph-700_first_party_assembly_dependencies.json` | `9a0aa14252e6559680328e520d26c16bfc7b444e` | `8f68ab2bf3756cddb8f17d08aee1c562668752beee8e2a4521f98df30d653fce` |
| `performance_regression_match_baseline.json` | `9a0aa14252e6559680328e520d26c16bfc7b444e` | `4ccb462315740f5a5990f398c0ecc4c8e73a3de0b17bbf46777c1aac5a9446cd` |

All paths in this table are under `Design/AgentReports`, with the first eight under `Design/AgentReports/ArchitectureMaturity`. Matching Markdown renderings are retained where the producing task requires them.

## Residual Risks

1. **Responsibility concentration:** 338 managed `*SystemHelper.cs` files remain, including 37 over 500 lines. Phase 1 must rank by responsibility, coupling, state, and update cost before extracting anything.
2. **World and query lifecycle:** 77 World owners expose 87 default-world accesses. Of 505 query/lookup rows, 157 non-system rows still require explicit owner classification.
3. **Native lifecycle:** 129 native-container fields include 57 observed persistent allocations; 63 have no dispose directly observed in a teardown method. Lexical absence is a review requirement, not proof of a leak.
4. **Presentation lifecycle:** 12 of 15 presentation-pool candidates have no directly observed teardown clear/dispose. Exhaustion, capacity, and gameplay-safe degradation are not yet proven.
5. **Subscriptions and static caches:** 57 of 79 named subscriptions have no matching unsubscribe in the owner type; 47 static-cache candidates have no directly observed teardown reset. Helper-call chains and framework ownership require case-by-case proof.
6. **Performance coverage:** the required bounded Editor Match frame gate is current and green, but owner-focused GC, all popup transitions, maximum combat/creation, retained-memory plateau, and repeated lifecycle scenarios remain later Core tasks.
7. **Protected active work:** operation-map and audio paths remain separately owned. FirstLaunch and UI visual-lock paths remain protected until their owners hand them off; maturity work must not absorb those changes implicitly.
8. **Release evidence:** Android tier, thermal, battery, package, cold/warm, soak, and three-release-candidate evidence remains intentionally deferred and cannot be inferred from Editor results.

## Owned Work Deltas

| Delta | Tasks | Acceptance direction |
|---|---|---|
| Responsibility decomposition | `AM-009` through `AM-017` | Characterize first, extract one bounded capability, preserve behavior/order, and recapture focused performance |
| World and lifecycle ownership | `AM-018` through `AM-025` | Explicit World-bound cache contracts, deterministic invalidation/disposal, repeated transition proof, stable allocation counts |
| Allocation-free UI projection | `AM-026` through `AM-035` | Version-driven rebuilds, centralized refresh, warmed open-surface matrix with zero recurring production GC |
| Presentation pools and creation | `AM-036` through `AM-044` | Data-backed capacities, prewarm, deterministic exhaustion policy, maximum creation/combat evidence |
| Determinism and failure paths | `AM-045` through `AM-052` | Seeded input streams, state hashes, record/replay, frame-rate equivalence, bounded failure behavior |
| Production-safe diagnostics | `AM-063` through `AM-069` | Preformatted/bounded telemetry, release gating, deliberate markers, zero recurring diagnostic GC |
| Continuous enforcement | `AM-070` through `AM-078` | Fail closed on new ownership/dependency/lifecycle/evidence regressions and govern exceptions permanently |

Release Certification tasks `AM-053` through `AM-062` and sustained-release tasks `AM-079` through `AM-086` remain outside the active Core execution lane until their documented activation gates are met.

## Phase 0 Decision

Phase 0 is accepted for the Core Architecture Lane. Required architecture and bounded Editor Match evidence is current, clean, exact-identity, and dashboard-accepted. The next dependency-ready task is `AM-009`. No release claim, Android qualification claim, broad memory-plateau claim, or `9.5` maturity claim is made.
