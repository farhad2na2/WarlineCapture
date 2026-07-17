# AM-WP-014 - Commander Profile Projection And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves the authoritative Commander profile/progression source, `AM-027` accepts profile projection identity, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-004`.

## 1. Current Ownership And Risk

- `UiShellStateSystem` creates `UiShellCommanderProfileComponent` with placeholder defaults, and `MenuBootstrapCompositionSystemHelper` rewrites equivalent defaults during Menu bootstrap.
- `CommanderProfileRouteLifecyclePresentation` installs the Commander route and performs one gateway read when the route opens.
- `UiShellEcsGateway.TryReadCommanderProfile()` converts Name, Subtitle, and PortraitClass fixed strings on each route read. `CommanderProfileContentView.Bind()` applies Name and Subtitle after additional trim/uppercase conversions; PortraitClass is currently not applied there.
- No semantic profile version, authoritative account/progression source, or World/boundary-aware managed cache is proven.
- Gateway reads can call default-state initialization and structurally add a missing profile component.
- Profile changes while the route remains open are not presented. Empty fields do not explicitly clear labels, so a reused view can retain stale profile text.
- Commander background-scrim references have no explicit subsystem-registration reset. Main Menu exit removes the scrim, but every direct Commander-to-other-route exit is not characterized.

Risks are presenting bootstrap placeholders as player progression, repeated managed conversions, stale identity after profile/World replacement, write-on-read ECS behavior, and retained static visual references. This package fails closed until the profile source owner is explicit.

## 2. Accepted Future Ownership

- `AM-026` names one authoritative Commander profile/progression source and proves the source-to-shell synchronization boundary before implementation begins.
- One ECS profile projection contains only display-ready semantic data and advances a version only when displayed Name, Subtitle, or accepted PortraitClass meaning changes.
- One World/boundary/version-aware managed cache converts fixed strings once per accepted profile version and invalidates on profile, World, boundary, localization, and binding changes.
- `CommanderProfileContentView` remains an apply-only serialized presentation boundary. Equal identities produce no formatting or TMP writes; empty values explicitly clear affected fields.
- While the route is open, one centralized version-gated shell apply path presents accepted profile changes exactly once without view-local polling.
- Profile/default component creation occurs in explicit bootstrap ownership. Read APIs are pure and never add components.
- Commander route enter/exit owns background scrim creation and release for every outgoing route. Subsystem registration clears projection caches and static scrim references.
- Duplicated placeholder defaults collapse to one canonical owner without changing current visible values until the authoritative source replaces them.

No new `SystemBase`, view-local polling, mutable static profile authority, default-World lookup, broad manager/controller/provider/service type, or account/progression behavior change is allowed.

## 3. Version And Invalidation Contract

Minimum identity:

- displayed Name, Subtitle, and accepted PortraitClass semantic value;
- profile/progression source generation;
- active account/profile identity;
- ECS profile semantic version;
- World sequence, boundary entity, route/root, and binding generations;
- localization/formatting generation;
- background-scrim lifecycle generation;
- explicit invalidation generation and reason.

Rules:

- Equal displayed profile values do not advance the display version, convert strings, format text, or write TMP.
- Source-generation-only changes may update provenance but reuse cached managed strings.
- Route re-entry with the same complete identity performs zero profile rebuild.
- A changed profile while open applies affected fields exactly once.
- Empty accepted fields clear their targets and cannot retain another profile's values.
- Account/profile, World, boundary, or route-root replacement invalidates cached managed data before the next apply.
- PortraitClass remains dormant unless `AM-026` proves its consumer and visual contract; this package does not invent portrait selection.
- Read APIs never create or mutate ECS state.
- Every Commander exit route releases the scrim once; release is idempotent.
- Version rollover uses equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the versioned profile read contract
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for profile cache/read lifecycle
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Contracts.cs` only for the versioned profile read contract
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs` only for profile cache lifecycle
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` only to consolidate duplicate profile defaults
- `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs` only to consolidate duplicate profile defaults
- `Assets/Game/Scripts/UI/Shell/CommanderProfileRouteLifecyclePresentation.cs`
- `Assets/Game/Scripts/UI/Screens/CommanderProfileContentView.cs`
- `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs` only for complete Commander exit cleanup
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for centralized version-gated profile apply/lifecycle binding
- `Assets/Game/Scripts/UI/Screens/CommanderProfileProjectionIdentity.cs` and its `.meta` if required

Test files allowed:

- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
- `Assets/Tests/Editor/CommanderProfileProjectionLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/CommanderProfileOpenSurfacePerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_014_commander_profile_projection_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_014_commander_profile_projection_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only dependencies:

- authoritative account/profile/progression production, Commander profile prefabs/scenes, portrait assets, localization catalogs, and UI visual-lock assets.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, gameplay, account/profile/progression production, scenes, prefabs, portraits, visual-lock art, packages, and `ProjectSettings`;
- profile values, ranks/titles, progression, persistence, portrait selection, labels, formatting target, layout, actions, or UX changes;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

This package must not overlap implementation of `AM-WP-010`, `AM-WP-011`, or `AM-WP-013` while shared shell gateway/contracts are claimed; those edits are serialized.

## 5. Characterization Matrix

Required before edits:

1. prove the authoritative profile source, bootstrap/default ownership, and current visible placeholder behavior;
2. initial route install, equal route re-entry, changed profile while open, and route close/reopen;
3. Name, Subtitle, and PortraitClass independently and together;
4. empty, whitespace, malformed/overlong, localized, and equal-after-normalization values;
5. account/profile replacement, boundary replacement, World replacement, root replacement, and subsystem registration;
6. Main Menu, Armory, Settings, and every other direct exit from Commander Profile;
7. gateway read with present and missing state, proving no structural mutation;
8. static scrim creation/reuse/release and destroyed-reference recovery.

Record source/component writes, semantic versions, gateway reads, fixed-string conversions, normalization/formatting calls, model/string construction, TMP writes, scrim roots/references, allocations, and apply time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged frames on the fully bound Commander Profile route;
- `100` route enter/exit/re-entry, profile/account replacement, and World/root replacement cycles;
- equal/changed/empty values and every exit route independently.

Acceptance:

- one authoritative source, one canonical default owner, and one source-to-view path are proven;
- semantic version advances only for displayed profile changes;
- exactly zero recurring production-owned managed bytes, fixed-string conversions, formatting, TMP writes, or query work while unchanged;
- equal route reinstallation performs zero rebuild; one real change applies affected fields once;
- empty values clear targets and no stale profile survives account, World, boundary, route, or root replacement;
- gateway reads perform zero ECS structural changes;
- every Commander exit releases the scrim exactly once, and subsystem registration clears static/profile cache state;
- current visible values, route layout, shared header, actions, responsiveness, and visual output remain unchanged;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits after source ownership is accepted:

1. authoritative-source/default characterization, profile version, and pure gateway reads;
2. managed projection cache plus live version-gated apply and explicit empty clearing;
3. scrim/static lifecycle hardening and complete open-surface acceptance.

Rollback if placeholder data becomes progression authority; profile values, formatting, layout, actions, portrait behavior, or route behavior changes; stale text/scrim survives lifecycle replacement; or the slice introduces polling, `SystemBase`, mutable global authority, protected-file edits, or non-allowlisted overlap.
