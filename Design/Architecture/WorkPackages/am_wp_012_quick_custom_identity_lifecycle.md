# AM-WP-012 - Quick Custom Identity And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts projection identity, `AM-028` accepts lifecycle/invalidation ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-023`.

## 1. Current Ownership And Risk

- `QuickCustomScreenView` owns serialized controls, a managed config copy, direct UI listeners, label arrays, config extraction, and view application.
- `QuickCustomScreenFlowUiSystemHelper` coordinates initialize, reset, apply, and launch through `IQuickCustomGameConfigStore` and `IMatchLaunchCommand`.
- The path is event-driven and has no view-local polling loop.
- `Awake` binds defaults before runtime dependencies are supplied, then `BindRuntimeDependencies` binds the runtime config again.
- Full `Bind` reapplies every control and reconstructs enemy-stepper labels even when the normalized config is unchanged.
- `OnDestroy` removes only Launch, Reset, and Randomize listeners. Enemy-stepper and segmented-control listeners are anonymous lambdas and have no symmetric release path.
- `UiQuickCustomGameConfig` has no accepted normalized fingerprint/generation, and the config-store contract has no change identity.

Risks are duplicate or stale listeners after repeated view lifecycle, unnecessary full rebuilds on equal route binds, inconsistent normalization between controls and store, and launch/apply duplication. This package must preserve the current event-driven architecture and must not move configuration authority into ECS or a mutable static service.

## 2. Accepted Future Ownership

- `IQuickCustomGameConfigStore` remains the authoritative managed setup contract. Gameplay consumes only the config accepted by that store and the existing launch command.
- One plain `UiQuickCustomGameConfigFingerprintUtilitySystemHelper` normalizes and fingerprints the complete config. It owns no state and performs no Unity-object access.
- `QuickCustomScreenView` retains the local editable draft and serialized presentation boundary. It stores the last normalized applied identity and skips equal full binds.
- One explicit listener-binding table owns every direct button, enemy stepper, segmented control, slider, toggle, dropdown, and input listener. Bind and release are symmetric and idempotent; anonymous delegates without retained release identity are prohibited.
- `QuickCustomScreenFlowUiSystemHelper` performs at most one normalized store apply per explicit Apply, Randomize, Reset, or Launch action. Launch observes the accepted config and calls `IMatchLaunchCommand` exactly once.
- Route re-entry, runtime-dependency replacement, disable/enable, destroy, and subsystem lifecycle cannot retain callbacks to stale view or store instances.

No polling, `SystemBase`, gameplay authority, default-World lookup, broad manager/controller/provider/service type, or visual hierarchy change is allowed.

## 3. Identity And Invalidation Contract

Minimum identity:

- every `UiQuickCustomGameConfig` field after normalization;
- config schema/defaults generation;
- config-store accepted generation when the implementation can expose it without breaking external owners;
- view instance and binding generation;
- localization/label-catalog generation;
- route/root lifecycle generation;
- explicit invalidation generation and reason.

Rules:

- Normalization clamps enemy count, enum ranges, income multiplier, and map seed before equality comparison or store publication.
- An equal normalized bind performs zero control rebuilds and allocates zero managed bytes.
- Initial construction does not publish defaults or simulate user input.
- Supplying runtime dependencies causes one bind only when the accepted config differs from the currently applied identity.
- Reset binds normalized store defaults and publishes once only if the accepted config changes.
- Randomize changes only the seed, updates the visible seed/map identity, and publishes once.
- Apply reads controls once and publishes at most once; equal normalized configs are suppressed at the store boundary or characterized if that boundary cannot yet change.
- Launch applies at most once, then launches exactly once with the same accepted config.
- Event-driven user edits update only affected controls/draft state; they do not force an unrelated full bind.
- Version rollover is handled by equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.cs`
- `Assets/Game/Scripts/UI/Screens/QuickCustomScreenFlowUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/Contracts/UiRuntimeContracts.cs` only for a backward-compatible config identity/store contract amendment
- `Assets/Game/Scripts/UI/Screens/UiQuickCustomGameConfigFingerprintUtilitySystemHelper.cs` and its `.meta` if required
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for Quick Custom bind/unbind lifecycle integration

Test files allowed:

- `Assets/Tests/Editor/SkirmishSetupScreenTests.cs`
- `Assets/Tests/Editor/QuickCustomIdentityLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/QuickCustomOpenSurfacePerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_012_quick_custom_identity_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_012_quick_custom_identity_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only dependencies:

- Quick Custom prefabs/scenes, balance probes, gameplay launch/scene loading, visual-lock assets, and localization catalogs.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, gameplay simulation, scenes, prefabs, visual-lock art, packages, and `ProjectSettings`;
- defaults, enum meanings, control labels/options, ranges, balance, launch destination, map selection, visual state, or UX flow changes;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

## 5. Characterization Matrix

Required before edits:

1. initial Awake before dependencies, dependency bind, equal rebind, changed rebind, and route re-entry;
2. every config control independently, all controls together, missing controls, and alternate segmented/dropdown or slider/segmented bindings;
3. Apply, equal Apply, Reset, Randomize, Launch, and rapid repeated actions;
4. disable/enable, destroy/recreate, dependency replacement, and `100` bind/unbind cycles;
5. invalid enum values, enemy counts, income multipliers, blank/invalid/overflow seeds, and missing store/launch command;
6. listener counts and one callback per action after every lifecycle transition;
7. existing locked-control behavior, labels, map-name resolution, and launch route.

Record normalized identities, store reads/applies, control binds, changed controls, listener adds/removes/invocations, launch calls, allocations, and apply time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged open frames on the fully bound production route;
- `100` route enter/exit, enable/disable, dependency-rebind, and destroy/recreate cycles;
- equal and changed bind/apply, each control action, Reset, Randomize, and Launch independently.

Acceptance:

- exactly zero recurring production-owned managed bytes while the route is open and unchanged;
- no polling owner is introduced;
- equal normalized bind causes zero control rebuild and zero store publication;
- exactly one live callback per control after every lifecycle cycle, with zero stale callbacks after release;
- Apply, Reset, and Randomize publish at most once per action; Launch publishes at most once and launches exactly once;
- initial construction and programmatic bind emit no user-change event;
- current defaults, labels, ranges, locked states, map names, visual output, and launch behavior remain unchanged;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most two independently stable commits:

1. normalized identity, characterization, and equal-bind suppression;
2. symmetric listener ownership and complete lifecycle/open-surface acceptance.

Rollback if any config field, default, label, range, map name, locked state, or launch behavior changes; if an action publishes or launches more than once; if a callback is lost or retained after release; or if the slice introduces polling, `SystemBase`, mutable global authority, protected-file edits, or non-allowlisted overlap.
