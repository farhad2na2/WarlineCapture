# AM-WP-005 - Loading Progress Projection Cache

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts loading progress/status semantic versions, and `AM-028` accepts World binding, invalidation, and projection/apply order.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-002`.

## 1. Current Authority And Defect

- `UiShellFlowSystem` is the ECS loading-state authority. It consumes the newest request and writes progress, fixed-string status, and completion state.
- `UIShellLoadingProgressView.Update` reads the gateway every frame while loading and every `0.25 s` after completion.
- `TryReadLoadingProgress` converts `FixedString64Bytes Status` into a new managed string before the view compares it with the prior status.
- The view retains percent/status presentation state, but it rewrites fill width on every successful read even when progress is unchanged.
- `UIGameUiSmokeDriverView` is a development driver that can publish progress every frame. It is not production loading authority and must not define the cache contract.

The defect is not that changing progress is frequent. Smooth numeric progress is legitimate. The defect is coupling that numeric lane to repeated status-string conversion, model reconstruction, unchanged fill application, and post-completion polling.

## 2. Accepted Future Ownership

- `UiShellFlowSystem` remains the sole loading-state writer and stays unmanaged `ISystem`.
- The loading component exposes independent visible-semantic identities for numeric progress, status text, and completion/route state, or one composite version plus an independently cacheable status identity.
- A World-bound `UiShellLoadingManagedProjectionCache` retains the managed status string and last accepted identities.
- Numeric progress may project whenever its semantic value changes without rebuilding status text. Equal progress requests do not advance identity.
- `UIShellLoadingProgressView` becomes retained apply. It mutates fill/percent only when their values change and status only when the status identity changes.
- Completion stops recurring reads. World/boundary replacement, route transition, a new loading sequence, or explicit invalidation reactivates projection through the accepted presentation owner.
- Missing/destroyed source fails closed without structural mutation or stale completed state.

No new `SystemBase`, MonoBehaviour `Update`, coroutine poller, static service locator, or parallel loading authority is allowed.

## 3. Version And Identity Contract

`AM-027` and `AM-028` must name the writers and rollover contract before dispatch. Minimum identity:

- bound World and shell boundary identity;
- loading-sequence identity, distinct across app startup and Match transitions;
- progress version and normalized progress value;
- status version and fixed-string status identity;
- completion version and completion state;
- active route/transition sequence identity;
- localization, settings/accessibility, and resolution/layout generations;
- explicit invalidation generation and reason.

Rules:

- Equal normalized progress, status, and completion writes do not advance any version.
- A progress-only change reuses the exact managed status string reference and does not run fixed-string conversion.
- A status-only change converts once and does not recreate unrelated presentation state.
- Completion applies once and disables recurring reads until a new loading sequence or lifecycle invalidation.
- Percent text changes only when rounded percent changes. Fill width changes only when accepted progress changes.
- Version rollover uses equality-based invalidation under the accepted `AM-028` contract.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellLoadingProgressView.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellLoadingManagedProjectionCache.cs` and its `.meta` if required
- `Assets/Game/Scripts/Composition/MenuBootstrapLoadingUtilities.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs`

Test files allowed:

- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
- `Assets/Tests/Editor/UiShellLoadingProjectionPerformanceValidation.cs` and its `.meta` if required
- `Assets/Tests/Editor/UiShellLoadingManagedProjectionCacheTests.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_005_loading_projection_cache_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_005_loading_projection_cache_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only fixtures:

- Splash/loading prefabs and scenes;
- `UIGameUiSmokeDriverView` as a development-driver characterization source;
- FirstLaunch and Match-intro flows.

Hard exclusions:

- operation-map/static-map production, scenes, trackers, and evidence;
- FirstLaunch production/tests, audio, UI visual-lock art, prefabs, packages, and `ProjectSettings`;
- loading duration, fake two-second loading design, route behavior, intro behavior, status copy, art, layout, or transition timing;
- any file outside this allowlist without a reviewed package amendment.

## 5. Characterization And Failure Cases

Required before production edits:

1. startup loading, Menu-ready completion, Menu-to-Match loading, Match-ready completion, and return to Menu;
2. unchanged progress/status, progress-only, status-only, completion-only, and all-fields change;
3. repeated equal requests and multiple queued requests where the newest wins;
4. progress clamping below zero/above one and percent rounding boundaries;
5. empty status fallback and maximum fixed-string status;
6. interrupted loading, route replacement, new sequence after completion, and rapid Menu/Match transitions;
7. World replacement/destruction, missing boundary/component, scene unload, subsystem registration, view disable/enable, and view rebind;
8. localization/settings/resolution invalidation;
9. development smoke-driver behavior without treating it as production authority.

Record request consumption, component writes, version changes, fixed-string conversions, managed-string identities, model reads, fill/percent/status applies, and allocations.

## 6. Baseline And Acceptance Gates

Measure the real ECS gateway and bound view for:

- `180` warmup plus `300` unchanged loading frames;
- `180` warmup plus `300` unchanged completed frames;
- `300` changing-progress frames with stable status;
- separate status changes, completion, sequence restart, lifecycle invalidation, and route transitions.

Report production-owned managed bytes; status conversions; managed string reference changes; component writes; model reads; fill, percent, and status applies; and average/P95/P99/maximum projection/apply time.

Acceptance:

- exactly zero recurring production-owned managed bytes in unchanged loading, unchanged completed, and changing-progress/stable-status windows;
- zero status conversions and stable managed status reference during progress-only changes;
- zero recurring reads after completion until an accepted invalidation;
- equal requests produce zero component write/version advance;
- one conversion/apply per status change and one completion apply;
- no stale state across World, boundary, route, or loading-sequence replacement;
- focused behavior/performance tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

The global player-relevant allocation allowance cannot be used to excuse allocations in this changed owner. Instrumentation is reported separately and requires raw attribution.

## 7. Maximum Slices And Rollback

At most two independently stable commits:

1. characterization plus accepted component versions and equal-write suppression;
2. World-bound managed cache, retained apply, completion invalidation, and recurring-poll removal.

Rollback if progress becomes visually discontinuous, percent/status/completion becomes stale, loading/route behavior changes, completion can no longer restart, changing progress allocates, or the slice introduces `SystemBase`, polling, default-World discovery, mutable static state, read-side mutation, or protected ownership overlap.
