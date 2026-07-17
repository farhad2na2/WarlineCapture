# AM-WP-006 - Shell Layout Invalidation

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted and `AM-028` accepts shell lifecycle, layout identity, invalidation ownership, and view apply order.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-021`.

## 1. Current Ownership And Risk

- `UISafeAreaView.Update` reads `Screen.safeArea`, width, and height every frame. It correctly returns before mutation when those values are unchanged.
- `UIAspectVariantView.Update` reads its `RectTransform` size every frame. It correctly returns before variant activation when size is unchanged.
- No focused tests measure unchanged allocation/CPU cost, safe-area transitions, threshold behavior, lifecycle rebinding, or the number of active polling instances.
- The views are independent platform/layout boundaries with no shared invalidation generation. Multiple installed instances can therefore duplicate screen/rect checks.

This inventory does not assert that the current early-return paths allocate. The package first measures them. Refactoring is accepted only if it reduces recurring work or closes lifecycle/test gaps without weakening safe-area correctness or changing visual targets.

## 2. Accepted Future Ownership

- Rect/aspect changes use `OnRectTransformDimensionsChange`, enable/rebind callbacks, or another proven Unity layout callback before any recurring polling is considered.
- Safe-area changes have one explicit shell/platform invalidation owner. If supported target platforms expose no reliable callback, one measured `UIShellLayoutInvalidationView.Update` may poll the fixed-size screen signature while the shell is active.
- The polling exception, if retained, is a documented Unity managed boundary with one owner, one instance, zero managed allocation, bounded work, and a removal/review condition. It is not ECS behavior and must not be implemented as `SystemBase`.
- `UISafeAreaView` and `UIAspectVariantView` remain apply-only targets. They receive an accepted layout identity and do not each own recurring screen polling.
- Layout identity changes only for screen size, safe area, target rect size, orientation, display, render scale, or configured threshold changes that can alter visible layout.
- Visual roots, anchors, threshold (`2.05` unless separately approved), art, and serialized references remain unchanged.

## 3. Layout Identity And Invalidation Contract

Minimum immutable signature:

- display identity and orientation;
- screen pixel width/height;
- safe-area position/size;
- target `RectTransform` width/height;
- canvas/render scale identity where it affects target size;
- standard/wide threshold configuration;
- shell/view lifecycle generation;
- resolution/settings/accessibility generation where layout can change;
- explicit invalidation generation and reason.

Rules:

- Equal signatures cause zero anchor, offset, active-state, layout, or Canvas mutation.
- Safe-area apply occurs once per changed signature and clamps denominators to at least one.
- Aspect variant changes only when crossing the configured threshold; a size change within the same variant does not toggle roots.
- Enable/rebind applies current state once even if the prior instance held an equal signature.
- Disable/destroy/unload unsubscribes callbacks and cannot retain stale Unity object references.
- Multi-display or unsupported safe-area states fail to the current full-screen behavior without invalid anchors.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Shell/UISafeAreaView.cs`
- `Assets/Game/Scripts/UI/Shell/UIAspectVariantView.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellLayoutInvalidationView.cs` and its `.meta` if one shared platform poll owner is proven necessary
- the exact existing shell composition file that installs/binds these views; amend this allowlist after `AM-028` names that owner

Test files allowed:

- `Assets/Tests/Editor/UIShellLayoutInvalidationTests.cs` and its `.meta`
- `Assets/Tests/Editor/UIShellLayoutInvalidationPerformanceValidation.cs` and its `.meta`
- an existing shell-content fixture only after its exact path is added by a package amendment

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_006_shell_layout_invalidation_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_006_shell_layout_invalidation_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only fixtures:

- all UI prefabs, scenes, visual-lock assets, screenshots, and serialized layout references;
- FirstLaunch safe-area preview and narrative UI, which are separately owned and not this runtime shell boundary.

Hard exclusions:

- operation-map/static-map production, scenes, trackers, and evidence;
- FirstLaunch, audio, visual-lock art, prefabs, scenes, packages, and `ProjectSettings`;
- layout redesign, anchor/offset target changes, threshold changes, new aspect variants, device-specific art, or safe-area UX changes;
- any production file outside this allowlist without a reviewed package amendment.

## 5. Characterization Matrix

Required before edits:

1. unchanged standard `16:9`, tall mobile, `20:9`, and `21:9` states;
2. standard-to-wide and wide-to-standard threshold crossing, plus size changes that remain within one variant;
3. full-screen safe area, top/bottom inset, left/right inset, asymmetric inset, and zero/invalid dimensions;
4. orientation change, resolution change, render-scale change, and display change where supported;
5. enable/disable, parent change, rebind, scene unload/reload, and destroyed target;
6. multiple installed views, proving the accepted single-owner rule;
7. platform callback capability audit for supported Android and Editor environments;
8. current anchors, offsets, active roots, and standard/wide-only object states as parity evidence.

Use injected pure signature/apply functions in tests; do not attempt to mutate global `Screen` state through brittle reflection. A focused integration fixture may drive actual `RectTransform` changes.

## 6. Baseline And Acceptance Gates

Baseline and post-change measurements:

- `180` warmup plus `300` unchanged frames for standard and wide layouts;
- `300` alternating rect-size frames within one variant;
- `300` threshold-crossing transitions;
- separate safe-area/orientation/resolution/lifecycle transition cases;
- active owner count, signature reads, callback count, poll count, anchor applies, variant toggles, layout rebuilds, and production-owned managed bytes;
- average, P95, P99, and maximum owner/apply time.

Acceptance:

- exactly zero recurring production-owned managed bytes after warmup;
- one active platform poll owner at most, and only when the capability audit proves it necessary;
- zero per-view recurring polling for aspect variants;
- zero anchor or root mutation for equal signatures;
- one safe-area apply per changed safe-area signature;
- one root toggle only when crossing the aspect threshold;
- no stale subscription or Unity object reference after lifecycle transitions;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, callback capability result, and focused review.

## 7. Maximum Slice And Rollback

Maximum slice: one shell layout invalidation owner plus the two existing apply views and focused tests/evidence. Do not combine content projection, popup, navigation, visual-lock, or layout-redesign work.

Rollback if any supported aspect/safe-area state is incorrect, if orientation or dynamic inset changes become stale, if multiple poll owners remain, if recurring allocation increases, or if the slice introduces `SystemBase`, broad static state, service location, ECS layout authority, protected asset edits, or non-allowlisted ownership overlap.
