# AM-WP-004 - Build Placement Confirmation Projection

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts the placement-visible semantic version, and `AM-028` accepts World binding, invalidation, and projection/apply order.

Umbrella task: `AM-030`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-014`.

## 1. Current Authority And Defect

The active behavior is owned by `BuildPlacementConfirmationBarView`:

- `UIShellContentView` binds `IBuildingUiCommand` directly into the view.
- The view polls every `0.1 s`, reads pending/confirm/status/cost/duration fields, splits and formats strings, resolves localization, and reapplies text/button state even when placement presentation is unchanged.
- Cancel, rotate, and confirm are event-driven and force an immediate refresh. Their command routing, feedback, and Build command-mode behavior are the parity baseline.

A second dormant/incomplete projection path exists:

- `UiBuildPlacementReadModelSystem` runs in `PresentationSystemGroup` and can rebuild the entire fixed-string component every frame.
- Its mutable static `UiBuildPlacementReadModelSource` has no production `Configure` or `Clear` caller, so the system is not a valid canonical source today.
- `TryReadBuildPlacementConfirmationBar` performs read-side structural initialization, converts every fixed string, and reconstructs a managed model. No active production apply caller was found.

The package must not activate the dormant path unchanged. It preserves the active view’s visible behavior while removing unchanged polling, mutable static source ownership, read-side mutation, and duplicate projection authority.

## 2. Accepted Future Ownership

- Building placement gameplay remains authoritative in the existing placement lifecycle/command owners.
- One source owner publishes a placement-visible snapshot and semantic version only when visible state changes.
- One World-bound managed `BuildPlacementConfirmationManagedProjectionCache` converts a changed snapshot into the retained managed model before Canvas apply.
- `BuildPlacementConfirmationBarView` becomes input forwarding plus retained visual apply. It has no recurring `Update()` and does not format or query gameplay state.
- Cancel, rotate, confirm, feedback, audio, command-mode clearing, pointer blocking, and immediate post-command visibility remain behaviorally identical.
- The incomplete ECS projector/gateway path is removed or delegated to the canonical source/cache after parity. Exactly one projection authority remains.
- Missing source/boundary fails closed without creating components, entities, buffers, queries, or views during reads.

No new polling loop or `SystemBase` is allowed. If a new ECS writer is required, it uses unmanaged `ISystem` and changes only on accepted source invalidation; the managed cache is a plain main-thread presentation object.

## 3. Version And Cache Identity

`AM-027` and `AM-028` must name the writer, lifecycle owner, and rollover contract before dispatch. Minimum identity:

- bound World and Match/UI boundary identity;
- active placement session/definition identity;
- placement-visible version covering visibility, title source, status/reason, validity, cost, duration, footprint orientation, and action availability;
- localization, settings/accessibility, resolution/layout, and icon/sprite generations;
- explicit invalidation generation and reason.

Pointer movement advances the visible version only when the displayed validity/status changes. Repeated movement across cells with equal visible status must not rebuild the bar. Rotation or command completion rebuilds once. A hidden unchanged bar performs no formatting, conversion, model construction, or apply.

Every recurring/event read checks identity before status splitting, uppercasing, number/time formatting, localization, fixed-string conversion, managed-model construction, or Canvas mutation.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Contracts/UiRuntimeContracts.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildPlacementReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Presentation.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/BuildPlacementConfirmationManagedProjectionCache.cs` and its `.meta` if required
- the existing building placement lifecycle/query/command file selected by `AM-027` as sole semantic-version writer; amend this allowlist with its exact path before implementation

Test files allowed:

- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
- `Assets/Tests/Editor/BuildingPlacementRuntimeTickSystemTests.cs`
- `Assets/Tests/Editor/BuildingPlacementValidationSystemTests.cs`
- `Assets/Tests/Editor/BuildingPlacementCommitCompositionSystemHelperTests.cs`
- `Assets/Tests/Editor/BuildPlacementConfirmationProjectionPerformanceValidation.cs` and its `.meta` if required
- `Assets/Tests/Editor/BuildPlacementConfirmationManagedProjectionCacheTests.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_004_build_placement_projection_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_004_build_placement_projection_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-030` tracker record and progress snapshot

Hard exclusions:

- operation-map/static-map files, scenes, trackers, and evidence;
- FirstLaunch, audio production, visual-lock art, prefabs, configs, packages, and `ProjectSettings`;
- placement validity, footprint, economy, production timing, camera, input, balance, layout, art, or localization-content changes;
- Build Drawer projection work from `AM-WP-002` except through an already accepted shared boundary;
- any file outside this allowlist without a reviewed package amendment.

## 5. Characterization And Failure Cases

Required cases before production edits:

1. hidden/no placement and placement start;
2. valid and invalid ground, blocked footprint, insufficient resource, and changed reason text;
3. pointer movement with equal visible status versus status transition;
4. rotation success/failure and orientation change;
5. confirm success/rejection and cancel;
6. zero/nonzero cost and duration formatting boundaries, including rounding at `59/60` seconds;
7. localization/settings/resolution invalidation;
8. destroyed World, missing boundary/source, scene unload, Match exit/re-entry, and view rebind;
9. rapid start/cancel/start and rotate/confirm sequences;
10. action feedback, sticky Build mode, pointer blocking, button interactability, and audio route parity.

Characterization records source reads, status splits, localization/format calls, model rebuilds, view applies, button-state mutations, and allocations. It must prove the ECS projector and gateway are dormant before removal; absence is evidence, not an assumption.

## 6. Baseline And Acceptance Gates

Measure the real active bar for `180` warmup plus `300` unchanged measured frames in hidden, valid-open, and invalid-open states. Report separately:

- production-owned managed bytes;
- source reads, string/localization/format conversions, projection rebuilds, and Canvas applies;
- average, P95, P99, and maximum projection/apply time;
- transition allocations for each characterization case;
- instrumentation-only allocation as a separate control.

Accepted unchanged-state target:

- exactly zero recurring production-owned managed bytes;
- zero status split, localization, number/time formatting, fixed-string conversion, model construction, or Canvas apply;
- one rebuild/apply per visible semantic change;
- no recurring `Update()` on the placement bar and no every-frame ECS rewrite;
- no default-World lookup, mutable static source, or read-side structural mutation.

Required validation:

1. all characterization behavior and feedback routes remain unchanged;
2. real-path `180 + 300` gates pass in all three states;
3. World/boundary/source lifecycle cases fail closed without stale presentation;
4. only one projection authority remains;
5. production and tests compile with zero errors;
6. focused placement/UI tests, integrated architecture checks, and `git diff --check` pass;
7. evidence binds baseline and implementation commit/tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most two independently stable commits:

1. characterization plus accepted semantic source/version, without authority switch;
2. managed cache/retained apply plus duplicate retirement.

Rollback if visible status, actions, feedback, button state, pointer blocking, or command mode changes; if unchanged allocation/conversion/apply remains; if stale state survives lifecycle invalidation; or if the slice introduces a polling loop, `SystemBase`, service locator, mutable static cache, default-World dependency, or non-allowlisted ownership overlap.
