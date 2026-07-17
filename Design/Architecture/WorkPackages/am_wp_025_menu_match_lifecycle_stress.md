# AM-WP-025 - Menu Match Lifecycle Stress

Status: draft, dependency-blocked, and not dispatchable. Do not implement until `AM-021` and `AM-022` are accepted. `AM-023` may then dispatch this early-development Editor/PlayMode package; it contains no release-device, thermal, graphics-quality, FPS, or sustained certification work.

Umbrella task: `AM-023`

Evidence source: `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs` and the accepted `AM-WP-024` recovery matrix.

## 1. Current Coverage And Risk

- The APH-805 PlayMode test executes one production Menu-to-Match-to-Menu transition, preserves the default World, proves one lifecycle root, binds the HUD, and verifies selected Match teardown.
- Focused tests prove selected cache/root/listener/audio behaviors, but no test repeats the complete production transition enough times to expose accumulation.
- There is no dedicated lifecycle-stress CI entrypoint or unified snapshot of entities, critical systems, command queues, roots, subscriptions, callbacks, VFX owners, audio listener state, and transition phase.
- Unbounded per-cycle logs would recreate the desktop output failure risk and obscure the first divergence.

Risks are duplicate systems/roots/listeners, stale Match dependencies, command queues surviving Menu return, destroyed references, hidden entity growth, non-deterministic late-cycle failure, and a stress test that becomes a release-performance test instead of an architecture gate.

## 2. Accepted Stress Ownership

- One PlayMode fixture loads Menu once, preserves the same valid default World, performs two warm-up cycles, then exactly 100 measured production route cycles through `UiShellRuntimeGateway`.
- Every cycle captures cheap invariant counts at stable Match and stable Menu checkpoints. A full diagnostic snapshot is captured every tenth cycle and immediately on failure.
- The fixture retains only baseline, previous, and failing snapshots. Logs are compact summaries; full structured evidence is written to bounded files.
- Counters are read-only and allocation-free. They are updated through existing bind/create/release ownership or test-only queries, not production polling or reflection infrastructure.
- Transition duration and the existing 180-second hard liveness ceiling are recorded for diagnosis, not treated as a frame-time/performance budget.
- Native/pool retained-memory plateau and slope belong to `AM-024`/`AM-WP-026`; this package asserts stable counts only.

No `SystemBase`, production stress controller, mutable static counter registry, release/device/thermal test, scene/prefab edit, or weakening of liveness/ownership contracts is allowed.

## 3. Snapshot And Invariant Contract

Each checkpoint records cycle, phase, elapsed transition time, loaded scenes, default World sequence/name, total entities, lifecycle/shell/Match roots, registered critical system handles/types, command queue/singleton counts, HUD/runtime dependency state, presentation roots, subscriptions/callbacks, VFX owner/active counts, enabled audio listeners, and available owner-approved pool counts.

Rules:

- Stable Menu and Match baselines are captured after warmup; measured counts must equal their corresponding baseline unless a named one-time cache is approved before execution.
- Menu has zero Match-only roots, HUD dependencies, Match command owners, active actors, and stale destroyed references.
- Match has exactly one Match view/HUD/runtime-root set and one registered critical system instance per World.
- Exactly one scene-lifecycle root, shell authority, and enabled audio listener exist where the accepted composition requires them.
- Subscription/callback counts equal baseline after every bind/unbind cycle.
- Any unexpected count increase, duplicate, invalid World access, disposal exception, timeout, or stale reference fails immediately and preserves the first divergent snapshot.

## 4. Exact File Allowlist

Allowed test/tool files:

- `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs` only if extracting reusable test fixtures
- `Assets/Tests/PlayMode/ArchitectureMenuMatchLifecycleStressPlayModeTests.cs` and its `.meta`
- one narrowly named test-only lifecycle snapshot helper and focused counter-correctness tests
- narrow read-only diagnostic accessors in architecture-owned lifecycle owners only after exact amendment and owner review
- a dedicated CI invocation/test-list entry using `Tools/CI/invoke_unity_macos.sh`
- `Design/AgentReports/ArchitectureMaturity/am023_menu_match_lifecycle_stress_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am023_acceptance_record.json`
- bounded task-owned logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- AM-023 validation/evidence/Progress Snapshot records

Read-only dependencies: operation-map/static-map and audio counters/tests through existing owner-approved APIs, accepted AM-021/AM-022 evidence, scenes, prefabs, and shell/runtime production code.

Production files are not generally allowlisted. Any missing counter or lifecycle fix needs a reviewed amendment and exact owner handoff.

Hard exclusions: operation-map/static-map, FirstLaunch, audio implementation, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, release-device/thermal/cold-warm/sustained certification, and the unrelated Arabic font asset.

## 5. Execution Matrix

1. clean test process loads Menu and reaches stable shell/World state;
2. two unmeasured production Menu-to-Match-to-Menu warm-up cycles establish caches and pools;
3. capture stable Menu and Match baseline snapshots;
4. run 100 measured cycles in the same process and default World;
5. at each Match checkpoint assert scenes, one Match root/HUD/dependency set, systems, command owners, listeners, and callbacks;
6. at each Menu checkpoint assert Match unload/cleanup, one lifecycle/shell root, zero Match dependencies/owners, and baseline counts;
7. every tenth cycle capture the full bounded snapshot; other cycles retain only cheap invariant totals;
8. on first failure capture full diagnostics and stop without flooding stdout;
9. teardown restores scenes, World, listeners, roots, and all test-created state even after failure.

Interrupted/cancelled transition characterization is accepted under AM-022; retained-memory trend is accepted under AM-024.

## 6. Validation And Acceptance Gates

Acceptance requires exactly 100 measured cycles, one valid unchanged default World, one lifecycle root and shell authority, correct stable Menu/Match scene and root sets, one critical system instance per World, no duplicate/stale command queues or destroyed references, one enabled audio listener, baseline-stable entity/subscription/callback/root/owner counts, and zero unexplained growth.

Each transition must complete within the existing 180-second liveness ceiling; timing is evidence only. Every permitted one-time cache is named before execution. Focused counter tests, zero compiler errors, the accepted AM-022 matrix, the full PlayMode stress run, integrated architecture checks, and `git diff --check` must pass. Evidence binds exact commit/tree, environment, source/test hashes, baseline/final/failure snapshots, compact logs, and focused review.

No AM-023 checklist credit is awarded for fewer cycles, simulated route state without production transitions, omitted counters, or a run that silently tolerates growth.

## 7. Maximum Slices And Rollback

At most three independently stable commits:

1. reusable fixture and read-only snapshot/counter correctness;
2. bounded 2-warmup/100-measured production transition runner and CI entrypoint;
3. full run, integrated validation, evidence, review closure, and AM-023 acceptance.

Rollback if the test leaks state, changes production behavior to make assertions pass, adds polling/reflection infrastructure, floods logs, masks a divergent cycle, modifies protected files, activates release certification, or weakens existing ownership/liveness gates.
