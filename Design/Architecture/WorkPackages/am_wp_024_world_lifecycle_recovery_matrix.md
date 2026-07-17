# AM-WP-024 - World Lifecycle Recovery Matrix

Status: draft, dependency-blocked, and not dispatchable. Do not implement until `AM-021` is accepted with zero open ownership gaps and the exact persistent-resource authority is regenerated. After that gate, `AM-022` may dispatch this test-only package before the separate `AM-023` 100-cycle stress package.

Umbrella task: `AM-022`

Evidence sources: `Design/AgentReports/ArchitectureMaturity/am021_persistent_resource_ownership.json`, existing focused lifecycle/cache tests, and `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs` as read-only transition evidence.

## 1. Current Coverage And Risk

- `WorldScopedComponentQueryCacheTests` covers World rebind, positive/negative lookup, invalidation, missing/destroyed/replaced singleton recovery, duplicate cardinality failure, and idempotent disposal.
- `WorldScopedComponentQueryCachePerformanceValidation` proves governed cache reuse/rebind and selected warm paths allocate zero recurring managed bytes.
- `PersistentResourceOwnershipLifecycleTests` covers selected subsystem reset, UI gateway World replacement, tactical/building query-owner disposal, presentation-root destruction, static VFX release, and removed Scenario Lab mirrors.
- `ThreatWarningValidationTests` covers Threat and Match Intro World isolation, missing/duplicate authority, explicit binding, and recovery.
- Building Placement and Road tests cover command-entity World rebind, destruction/replacement, buffer repair, and transaction identity in those two domains.
- Existing shell/resource tests cover selected root replacement and listener transfer; the APH-805 PlayMode test covers one Menu-to-Match-to-Menu cycle.

Missing are one registry-bound matrix across every governed cache/boundary/command owner, subsystem/domain reset coverage for all mutable static owners, duplicate-system prevention after World recreation, generalized scene/root/subscription cleanup, replacement coverage beyond two command queues, interrupted transition recovery, and one integrated recovery sequence. Without those proofs, isolated green tests can still miss stale cross-owner state.

## 2. Accepted Test Ownership

- `AM-021` first publishes a zero-gap persistent-resource matrix. `AM-022` derives its governed owner/case inventory from that accepted authority rather than maintaining an independent mutable registry.
- Focused Editor tests own deterministic World, singleton, command-entity, reset, and owner-disposal cases. A bounded PlayMode integration owns scene/root/listener recovery, but not 100-cycle stress.
- The default World may be changed only inside a scoped fixture that captures and restores the previous value in `finally`/teardown.
- Static reset tests invoke the production `SubsystemRegistration` boundary and prove idempotent unsubscription/reinitialization. CI runs both ordinary domain reload and domain-reload-disabled subsystem-reset configurations without committing `ProjectSettings` changes.
- Duplicate-system tests assert exact system handles/types per World, not merely singleton component counts.
- Command replacement preserves monotonic request/transaction identity and repairs required buffers through the owning domain; UI/tests do not fabricate business state.
- Failures in protected or production owners produce a separately reviewed implementation package and owner handoff. This test package does not opportunistically patch production code.

No generic lifecycle controller, mutable static test registry, production service locator, `SystemBase`, default-World leakage, release-device/thermal work, or modification of protected paths is allowed.

## 3. Identity And Recovery Contract

Every matrix row records:

- owner ID and exact source/member from the accepted AM-021 authority;
- World sequence/name, system handle/type, boundary entity identity/version, and command entity/request sequence where applicable;
- cache/query bind generation, positive/negative lookup generation, invalidation reason, and disposal generation;
- scene/root/binding generation and exact subscription publisher/callback identity;
- subsystem/domain reset generation and static initialized/subscriber state;
- expected precondition, mutation, fail-closed state, recovery trigger, recovered identity, and final disposal assertion.

Destroyed World/query/entity/root identities are never read after replacement. Missing authority fails closed without mutation; appearance plus explicit invalidation/rebind recovers once. Duplicate authority fails closed without choosing a winner. Replacement command entities preserve monotonic identity and required buffers. Reset/dispose/unbind is idempotent; reinitialize/rebind creates exactly one owner/listener/system. Scene/root replacement delivers no callback to destroyed views. After recovery warmup, governed unchanged cache access performs zero recurring production-owned managed allocation.

## 4. Exact File Allowlist

Existing test files allowed:

- `Assets/Tests/Editor/PersistentResourceOwnershipLifecycleTests.cs`
- `Assets/Tests/Editor/WorldScopedComponentQueryCacheTests.cs`
- `Assets/Tests/Editor/WorldScopedComponentQueryCachePerformanceValidation.cs`
- `Assets/Tests/Editor/ThreatWarningValidationTests.cs`
- `Assets/Tests/Editor/BuildingPlacementValidationSystemTests.cs` only for existing command-cache fixture extraction or assertions
- `Assets/Tests/Editor/RoadBuildCommandCompositionSystemHelperTests.cs` only for existing command-cache fixture extraction or assertions
- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs` and `Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs` only for existing root/listener fixture extraction or assertions
- `Assets/Tests/Editor/MatchSceneReferenceSceneSystemHelperTests.cs` only for existing scene-reference fixture extraction or assertions

New test/evidence files allowed:

- `Assets/Tests/Editor/WorldLifecycleRecoveryMatrixTests.cs` and its `.meta`
- `Assets/Tests/PlayMode/WorldSceneLifecycleRecoveryPlayModeTests.cs` and its `.meta`
- one narrowly scoped Editor validation entrypoint if required by the existing CI pattern
- `Design/AgentReports/ArchitectureMaturity/am022_world_lifecycle_recovery_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am022_acceptance_record.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- `AM-022`, validation, evidence, and Progress Snapshot records in this tracker

Read-only evidence: `Aph805MenuMatchMenuLifecyclePlayModeTests.cs`, accepted AM-021 authority/evidence, audio lifecycle tests, operation-map lifecycle tests, FirstLaunch tests, scenes, prefabs, and configuration.

Production files are not allowlisted. Any required production fix needs a reviewed package amendment, exact active-owner handoff, and its own stable slice before the matrix is rerun.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, release-device/thermal certification, and the unrelated Arabic font asset.

## 5. Bounded Recovery Matrix

Exactly these ten case families belong to `AM-022`:

1. destroy a bound World, create a replacement, and prove every governed cache/gateway binds only to the replacement;
2. missing singleton fails closed, then explicit creation/invalidation recovers exactly once;
3. resolved singleton is destroyed or loses its component, then replacement resolves without stale identity;
4. duplicate singleton/boundary fails closed without structural mutation or winner selection;
5. each registered command queue is destroyed/replaced, required buffers are repaired, and request/transaction identity remains monotonic;
6. subsystem/domain reset runs twice, clears every registered mutable static owner/subscription, and reinitializes exactly once in domain-reload-enabled and disabled CI configurations;
7. scene unload/reload clears governed presentation roots, subscriptions, queries, command references, and Match dependencies;
8. World/system recreation leaves exactly one required system handle/type and one lifecycle root per accepted composition contract;
9. integrated sequence combines World replacement, missing authority, replacement command entity, root replacement, and recovery without exception/stale state;
10. after recovery warmup, governed unchanged cache access and listener/root idle state meet zero recurring allocation/work gates.

The second complete Menu-to-Match-to-Menu cycle, repeated transition stress, retained-memory trend, and 100-cycle requirement belong to `AM-023`/`AM-024`, not this package.

## 6. Validation And Acceptance Gates

Acceptance requires:

- the accepted AM-021 authority has zero ownership gaps and every matrix owner ID/source hash resolves;
- all ten case families execute against registered owners with explicit pass/fail/skip rationale; protected-owner rows remain visible as separately accepted evidence, not silently omitted;
- World/default-World state, scenes, roots, systems, subscriptions, and test-created objects are restored after every test and injected failure;
- missing/duplicate/destroyed/replaced authority behavior matches the standard World-bound cache contract and performs no read-side structural mutation;
- every registered command owner has replacement/recovery evidence and monotonic identity where required;
- subsystem reset leaves zero old subscribers and exactly one reinitialized subscriber/owner;
- scene/root recovery leaves no stale callback, Match dependency, query, entity, or view reference;
- duplicate-system checks prove exact handles/types per World;
- focused Editor/PlayMode tests, zero compiler errors, integrated architecture checks, Python/evidence checks, and `git diff --check` pass;
- unchanged recovered cache paths meet `180` warmup plus `300` measured frames/calls with zero recurring production-owned managed bytes;
- evidence binds baseline/implementation commit and tree, AM-021 authority hash, test/source hashes, environment, compressed logs, metrics, and focused review.

No AM-022 checklist credit is awarded for a partial matrix or while AM-021 retains an open gap.

## 7. Maximum Slices And Rollback

At most three independently stable commits after AM-021 acceptance:

1. registry-derived Editor recovery matrix for World/singleton/cache/command/reset/system cases;
2. bounded scene/root/subscription PlayMode recovery integration;
3. zero-allocation, integrated validation, evidence, review closure, and AM-022 acceptance.

Rollback a slice if it leaks default World/test state, hides a registered owner, mutates production authority from a read/assertion, duplicates systems/listeners, depends on mutable static test registration, absorbs AM-023 stress scope, changes protected files, or passes by weakening an existing architecture/performance contract.
