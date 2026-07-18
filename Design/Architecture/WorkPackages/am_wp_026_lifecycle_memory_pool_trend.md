# AM-WP-026 - Lifecycle Memory And Pool Trend

Status: active and dispatchable. `AM-021` through `AM-023` are accepted and the lifecycle snapshot schema is stable. This is an early-development Editor/PlayMode package; it contains no Android, thermal, cold/warm launch, sustained gameplay, graphics-memory, or release certification work.

Umbrella task: `AM-024`

Evidence sources: accepted AM-021 ownership authority, `AM-WP-025` lifecycle snapshots, Unity `Profiler` memory counters, and existing pool diagnostics.

## 1. Current Coverage And Risk

- Existing tests cover selected cache allocations, building pool disposal, Resource Exchange actor pools, and audio pool counts. Audio remains read-only evidence under separate ownership.
- `architecture_lifecycle_inventory.py` inventories persistent native containers, pools, roots, subscriptions, caches, and World owners statically.
- Editor capture tools expose managed/total allocated/reserved memory and selected pool/transition diagnostics, but no unified per-cycle lifecycle snapshot or retained-trend calculation exists.
- Current-thread GC tests measure transient managed allocation, not retained memory. Profiler totals include allocator reservation noise and do not identify a leaking owner.

Risks are declaring stability from noisy totals, missing owner/count growth, pooling that grows after warmup, stale Match objects retained in Menu, silent missing counters, and expanding an early architecture test into release memory certification.

## 2. Accepted Measurement Ownership

- One PlayMode fixture reuses the accepted production transition/snapshot schema, runs one warm-up cycle and five measured cycles, and captures settled Match and settled Menu snapshots. The former three-warm-up/twelve-measured extended trend is deferred until a maturity or release gate justifies its cost.
- Structural counts are authoritative: World/entity/root/native-owner/query/cache/subscription/presentation/pool active-created-inactive counts must plateau after warmup.
- Unity memory counters are supporting trend signals, not early-development leak verdicts. Total allocated, reserved, Mono used, and Mono heap are sampled at settled checkpoints after the same bounded stabilization procedure; Editor scene/asset/test-runner retention is reported separately from authoritative structural ownership.
- Transition allocations are reported separately from settled retained measurements.
- Missing required counters fail closed; they are never recorded as zero. Protected-domain counts are consumed only through existing or owner-approved read-only APIs.
- A deterministic test-only trend implementation computes medians and Theil-Sen slopes without runtime dependencies or mutable global state.

No `SystemBase`, production memory poller, reflection-based production registry, forced-GC manipulation that invalidates measurements, device/thermal/release work, or protected-owner edit is allowed.

## 3. Snapshot And Trend Contract

Every settled snapshot records phase/cycle, World count/identity, total entities, lifecycle/Match/presentation roots, governed native owner/count identity, query/cache/subscription/static-cache counts, each available pool's active/created/inactive/capacity values, active actors/audio sources, total allocated/reserved memory, Mono used/heap, and source/environment identity.

Rules:

- Exactly one valid default World and one lifecycle root exist throughout normal cycles.
- Menu checkpoints contain zero Match-only roots/entities/actors. The single enabled listener and owner-approved audio pool/active-source counts must equal their post-warmup Menu baseline; persistent shell audio is not classified as Match retention.
- Native-owner, subscription, static/query-cache, and pooled-capacity counts never exceed warm baseline after warmup.
- Created pool counts may grow only during warmup; measured-cycle growth is zero.
- Count divergence fails immediately regardless of memory-total noise.
- Memory medians and slopes use the five measured cycles only; Match and Menu phases are evaluated separately.
- Missing/invalid samples invalidate the run rather than weakening the denominator.

## 4. Exact File Allowlist

Allowed test/tool files:

- the test-only lifecycle snapshot helper accepted under `AM-WP-025`
- `Assets/Tests/PlayMode/ArchitectureLifecycleMemoryPoolTrendPlayModeTests.cs` and its `.meta`
- one narrowly named Editor-only snapshot model/collector if not already accepted
- focused tests for snapshot normalization, median, Theil-Sen slope, missing-counter failure, and threshold evaluation
- minimal fixture extraction from `Aph805MenuMatchMenuLifecyclePlayModeTests.cs` only if still required
- CI invocation/test-list updates limited to this package
- `Design/AgentReports/ArchitectureMaturity/am024_lifecycle_memory_pool_trend_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am024_acceptance_record.json`
- bounded task-owned logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- AM-024 validation/evidence/Progress Snapshot records

Read-only dependencies: `architecture_lifecycle_inventory.py`, existing building/Resource Exchange/audio pool diagnostics, Profiler capture patterns, accepted AM-021 through AM-023 evidence, scenes, prefabs, operation-map/static-map, and audio implementation.

Production files are not generally allowlisted. A missing counter requires a reviewed amendment and exact owner handoff.

Hard exclusions: operation-map/static-map implementation/tests, FirstLaunch, audio implementation, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, graphics-memory requirements, Android/device/thermal/cold-warm/sustained/release certification, and the unrelated Arabic font asset.

## 5. Execution Matrix

1. clean process reaches stable Menu and validates required counters;
2. run one unmeasured Menu-to-Match-to-Menu warm-up cycle;
3. freeze warm baseline owner/root/subscription/cache/pool capacities;
4. run five measured cycles;
5. capture one settled Match and one settled Menu snapshot per cycle;
6. assert structural plateaus immediately at each checkpoint;
7. calculate first-two versus final-two medians per phase;
8. calculate Theil-Sen measured-cycle slopes per phase;
9. retain compact baseline/final summaries and full data in bounded evidence files;
10. restore all test state on success or failure.

The later Release Certification Lane owns device/thermal/sustained memory validation and may define different thresholds from separate evidence.

## 6. Validation And Acceptance Gates

Acceptance requires zero measured-cycle growth in governed native-owner, subscription, static/query-cache, presentation-root, and pool-created/capacity counts; zero Match-only retained owners/actors at Menu; stable post-warmup audio active-source counts per phase; no duplicate owner, invalid World access, or disposal exception; and no missing required counter.

Per phase, the investigation ceilings are: final-two median minus first-two median at most `+1 MiB` Mono used, `+4 MiB` total allocated, and `+8 MiB` total reserved; Theil-Sen slope at most `64 KiB/cycle` Mono used and `256 KiB/cycle` total allocated. Crossing a ceiling must be recorded with the exact value and a release-lane follow-up, but does not override a green structural ownership/pool plateau or fail this early-development package because forced-GC manipulation is prohibited and Editor scene/asset/test-runner retention is not a retained-owner measurement. Mono heap and reserved memory remain separately reported.

Focused trend/counter tests, zero compiler errors, five measured cycles, integrated architecture checks, and `git diff --check` must pass. Evidence binds exact commit/tree, environment, accepted ownership/snapshot schema hashes, raw bounded snapshots, thresholds, trend results, and focused review.

No AM-024 checklist credit is awarded for transient-GC-only evidence, missing required owner/pool counters, fewer than five measured cycles, combined Menu/Match statistics, unexplained structural count growth hidden by memory tolerance, or unreported memory-ceiling crossings.

## 7. Maximum Slices And Rollback

At most three independently stable commits:

1. snapshot/counter completeness plus deterministic trend evaluator tests;
2. 1-warmup/5-measured production transition fixture and CI entrypoint;
3. full run, integrated validation, evidence, review closure, and AM-024 acceptance.

Rollback if measurements rely on unstable phase timing, force GC in a way that hides retention, accept missing counters, allow post-warmup pool growth, combine phases, add production polling, modify protected files, activate release certification, or weaken ownership/allocation contracts.
