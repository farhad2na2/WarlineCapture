# AM-WP-015 - Shell Presentation Command Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-028` accepts World/boundary invalidation, `AM-WP-001` removes Resource Exchange projection work from the shell loop, and `AM-034` dispatches this package.

Umbrella task: `AM-034`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-001`.

## 1. Current Ownership And Risk

- `UIShellEcsPresentationSystem` is a MonoBehaviour despite the architecture rule reserving bare `*System` names for ECS systems.
- Its `Update()` first reads the full shell state only to prove a boundary exists, flushes a pending completion, refreshes Resource Exchange, and then consumes presentation commands.
- The Resource Exchange refresh is separate projection ownership already assigned to `AM-WP-001` and must leave this loop before this package dispatches.
- Command consumption copies the full ECS buffer into a reusable managed list and clears the ECS buffer. List capacity has no documented production ceiling or exhaustion behavior.
- Each accepted sequence creates a capturing completion delegate. `UIShellView.ExecuteCommandSequence()` also allocates a new step list, closure, and array per transition.
- Local execution/completion state has no explicit disable, destroy, World replacement, boundary replacement, or subsystem-registration contract. A completion can retry indefinitely while its original World/boundary is gone.
- Editor-only direct-allocation probes cover steady updates, but no complete production command burst, interruption, lifecycle, or retained-capacity matrix exists.

Risks are unnecessary per-frame reads, mixed projection ownership, transition allocations, unbounded retained capacity, stale completions across lifecycle replacement, and naming-contract drift. The shell bridge is a legitimate managed Unity animation boundary, but its loop must be singular, narrow, and measured.

## 2. Accepted Future Ownership

- Rename the MonoBehaviour and file to `UIShellPresentationCommandView`, preserving the existing `.meta` GUID so serialized references remain intact. No bare non-ECS `*System` alias remains.
- One shell-scoped `Update()` remains as the documented managed ECS-to-Unity animation bridge. It performs only completion flush and lightweight command-availability/identity checks while bound.
- Boundary and World identity are cached through the accepted `AM-028` gateway lifecycle contract; the loop does not read a complete shell model merely to prove availability.
- Resource Exchange and every other domain projection refresh outside command sequencing are removed from this owner.
- Command scratch capacity is pre-sized to the characterized maximum sequence, bounded by an explicit ceiling, and has deterministic overflow behavior that preserves transition authority without silent command loss.
- Sequence completion uses a retained instance callback and stored final-command identity rather than a capturing delegate.
- `UIShellView` retains reusable motion-step scratch storage and passes a bounded count without allocating a list/array/closure for each ordinary transition after warm-up.
- Disable/destroy, shell/root replacement, World/boundary replacement, interrupted animation, and subsystem registration explicitly cancel or reconcile local execution and pending completion state. A completion is accepted only for its original complete identity.

No `SystemBase`, second polling loop, mutable static transition authority, default-World lookup, broad manager/controller/provider/service type, command reordering, or route/animation behavior change is allowed.

## 3. Identity And Invalidation Contract

Minimum identity:

- World sequence and shell boundary entity;
- command-buffer change/availability generation;
- transition sequence ID;
- final command Kind, Region, Route, TargetMode, and PopupKind;
- shell view/root and binding generations;
- active Unity motion transition ID;
- pending-completion state and retry generation;
- explicit invalidation generation and reason.

Rules:

- Unchanged bound state performs no complete shell-model read, command copy, projection refresh, managed allocation, or Canvas mutation.
- Commands are consumed once, in order, and only when a valid shell view can execute them.
- A sequence is never silently truncated. Overflow fails visibly and deterministically, retains authority for recovery, and records the configured ceiling.
- Completion callback identity includes World, boundary, and transition sequence; stale callbacks cannot complete a replacement transition.
- A pending completion retries only while its originating boundary remains valid and has a bounded failure/recovery policy.
- Disable/destroy during animation cannot leave `isExecuting` permanently true or duplicate completion after rebind.
- Empty command buffers produce zero managed list mutations.
- Resource Exchange refresh ownership is absent from this loop after `AM-WP-001` acceptance.
- Version rollover uses equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs` and its `.meta`, renamed in place to `UIShellPresentationCommandView.cs` and `.meta` with GUID preserved
- `Assets/Game/Scripts/UI/Shell/UIShellView.cs` only for reusable command-sequence motion scratch and non-capturing completion
- `Assets/Game/Scripts/Composition/MenuBootstrapView.cs` only for the renamed serialized component type/reference
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for command-availability/lifecycle identity
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for command-availability/lifecycle identity
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs` only for command generation/ceiling identity
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Routes.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Contracts.cs` only for the command-availability/lifecycle contract
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs` only to publish the accepted command generation/sequence identity
- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs` only for the renamed runtime probe owner/path

Test files allowed:

- `Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs`
- `Assets/Tests/Editor/MatchGcAllocationCallstackCaptureTests.cs`
- `Assets/Tests/Editor/ResourceExchangeArchitectureGuardrailTests.cs` only to remove the old mixed refresh-owner assertion
- existing shell route/transition tests only when command lifecycle assertions are directly added
- `Assets/Tests/Editor/UIShellPresentationCommandLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/UIShellPresentationCommandPerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_015_shell_presentation_command_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_015_shell_presentation_command_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-034` tracker record and progress snapshot

Read-only dependencies:

- shell prefab/scene serialized references, UI Toolkit shell counterpart, Resource Exchange production after `AM-WP-001`, UI motion visuals/timings, and route/popup content.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, gameplay, Resource Exchange projection logic, scenes, prefabs, UI Toolkit production, visual-lock art, packages, and `ProjectSettings`;
- command meaning/order, route graph, transition timing/easing, visual state, popup behavior, content, or UX changes;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

This package is serialized with every package claiming shared shell gateway/contracts or `UIShellView`.

## 5. Characterization Matrix

Required before edits:

1. no boundary, valid boundary/no commands, one command, maximum ordinary sequence, and above-ceiling command burst;
2. Menu, popup, loading, Match, and every route transition sequence;
3. synchronous completion, animated completion, delayed completion, rejected completion, and gateway-unavailable retry;
4. disable/destroy/re-enable during execution, shell-view replacement, root replacement, World/boundary replacement, and subsystem registration;
5. stale callback after replacement, duplicate callback, sequence rollover, and interrupted animation;
6. Resource Exchange open/closed proving its refresh no longer belongs to this loop;
7. Canvas and UI Toolkit runtime modes proving the renamed component is enabled/disabled exactly as before;
8. serialized script GUID and component resolution after source/type rename.

Record update ticks, boundary/availability reads, command generations/counts/copies, list capacity, motion-step capacity, managed allocations, execution/completion identities, retries, stale rejections, Canvas mutations, and transition duration.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged frames with Menu, popup, loading, and Match shell states;
- `100` route/popup/Match transition cycles and `100` World/root replacement cycles;
- every ordinary sequence, maximum sequence, above-ceiling burst, and interruption path independently.

Acceptance:

- the sole managed shell command loop has a documented necessity, owner, frequency, and removal/review condition;
- unchanged state causes zero production-owned managed bytes, complete shell-model reads, command copies, domain projection refreshes, or Canvas mutation;
- ordinary warmed transitions allocate zero command-list, motion-step-list/array, or completion-closure bytes;
- command order and behavior are unchanged, every accepted sequence completes once, and no stale sequence completes a replacement boundary;
- scratch capacity remains within the accepted ceiling; overflow behavior is deterministic, visible, recoverable, and never silently drops authority;
- disable/destroy, World/boundary/root replacement, and subsystem registration leave no stuck execution, retained callback, or duplicate completion;
- `UIShellPresentationCommandView` replaces the non-ECS bare `*System` name with preserved script GUID and unchanged Canvas/UI Toolkit mode behavior;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits:

1. characterization, rename/GUID preservation, boundary identity, and removal of mixed Resource Exchange refresh ownership;
2. bounded command/motion scratch plus non-capturing completion;
3. interruption/replacement recovery and complete steady/transition allocation acceptance.

Rollback if serialized component resolution, Canvas/UI Toolkit mode, command order/meaning, route/popup behavior, transition visuals/timing, or completion behavior changes; if command authority is dropped; if lifecycle state sticks or duplicates; or if the slice introduces `SystemBase`, another poller, mutable global authority, protected-file edits, or non-allowlisted overlap.
