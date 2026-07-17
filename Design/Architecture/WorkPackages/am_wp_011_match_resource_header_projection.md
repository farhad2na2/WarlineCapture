# AM-WP-011 - Match Resource Header Projection

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts resource projection identity, `AM-028` accepts lifecycle/invalidation ownership, and `AM-032` dispatches this package.

Umbrella task: `AM-032`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-011`.

## 1. Current Ownership And Risk

- `MainMenuPlayUI.Update()` calls `MatchHudResourceHeaderPresentation.RefreshIfDue()` every frame; the presentation reads sources every `0.2 s` while bound.
- The active presentation binds and applies Oil/Fuel. Credits/Materials are projected by unmanaged `UiMatchHudResourceReadModelSystem`, but no proven production presentation applies those projected fields to their header slots.
- Oil/Fuel source precedence is usable-fuel summary, live usable storage, legacy faction summary, then text fallback. The live-storage fallback allocates two `Allocator.Temp` arrays and scans storage on every due poll.
- `UiMatchHudHeaderComponent.ResourceVersion` covers Credits/Materials. Tactical materials and usable-fuel summaries expose versions, but `FactionEconomy` and legacy/live-storage fallbacks do not provide one complete displayed identity.
- `UiMatchHudResourceValuesModel` drops the usable-fuel source version, so numeric presentation relies on value comparison rather than source identity.
- Credits/Materials and Oil/Fuel use separate projection and formatting paths. Resource-slot click routing for Credits, Oil, Fuel, and Materials is active and must remain unchanged.

Risks are recurring query work, temporary native allocation, partially dormant header fields, stale values across source-precedence or World changes, and duplicated formatting/identity rules. This package must preserve the existing resource economy and fallback precedence.

## 2. Accepted Future Ownership

- `UiMatchHudResourceReadModelSystem` remains an unmanaged `ISystem` and becomes the single canonical ECS projection owner for all displayed Match header resources.
- The projection publishes a complete semantic identity for Credits, Oil, Fuel, Materials current/capacity, Oil visibility, selected source kind, and relevant source generations.
- A managed presentation boundary converts/formats only after that identity changes, applies only changed slots, and performs no recurring query scan while unchanged.
- Live-storage aggregation moves to a versioned ECS summary owner or a reusable query iteration that does not allocate temporary arrays. The managed gateway does not aggregate storage on a timer.
- Bind performs one immediate apply; clear, rebind, World replacement, boundary replacement, and source-precedence transition invalidate exactly once.
- Text fallback remains fail-closed for incomplete migration but carries an explicit identity and removal/review condition.
- Resource Exchange click routing remains a separate input concern and is not coupled to projection refresh.

No new `SystemBase`, view-local polling, mutable static resource authority, default-World lookup, broad manager/controller/provider/service type, or economy/balance change is allowed.

## 3. Version And Invalidation Contract

Minimum identity:

- Credits value and source generation;
- Oil/Fuel values, Oil visibility, usable-fuel summary generation, and selected source kind;
- Materials current/capacity and tactical-materials generation;
- player faction identity;
- shell boundary, World, scene/root, and binding generations;
- formatting/localization generation;
- explicit invalidation generation and reason.

Rules:

- Semantic identity changes only when displayed value, visibility, source precedence, player faction, or binding/lifecycle identity changes.
- Source version changes with equal displayed values may advance source identity but must reuse cached strings and perform zero TMP writes.
- One source transition performs one projection and one affected-slot apply.
- Bind applies immediately without waiting for an interval.
- Unchanged open state performs no source scan, native allocation, string conversion, TMP write, or GameObject visibility write.
- Source precedence remains usable summary, live usable storage, legacy summary, then text fallback.
- Missing player/source data clears or retains values only according to characterized current behavior; stale values across World/boundary replacement are prohibited.
- Version rollover uses equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Screens/MatchHudResourceHeaderPresentation.cs`
- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs` only to remove interval polling and bind the versioned apply path
- `Assets/Game/Scripts/UI/Screens/MatchHudHeaderReferenceUiSystemHelper.cs` only for all-slot references/binding
- `Assets/Game/Scripts/UI/Contracts/UiMatchHudResourceValuesModel.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the complete Match resource-header read contract
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.ResourceValues.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs` only when the complete header identity requires its existing contract
- `Assets/Game/Scripts/UI/Shell/Ecs/UiMatchHudResourceReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`

No additional production file is implied for live-storage aggregation. If the existing unmanaged read-model system cannot own the projection without changing gameplay-source ownership, dispatch requires a reviewed allowlist amendment and the exact source owner's handoff.

Test files allowed:

- `Assets/Tests/Editor/UiMatchHudResourceReadModelSystemTests.cs`
- `Assets/Tests/Editor/UiShellEcsGatewayResourceHeaderTests.cs`
- `Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs` as read-only routing coverage unless a binding seam changes
- `Assets/Tests/Editor/MatchHudResourceHeaderPresentationTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/MatchHudResourceHeaderPerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_011_match_resource_header_projection_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_011_match_resource_header_projection_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-032` tracker record and progress snapshot

Read-only dependencies:

- economy/resource simulation, Resource Exchange input/visual behavior, Match HUD prefabs/scenes, visual-lock assets, and localization catalogs.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, resource balance/economy, scenes, prefabs, visual-lock art, packages, and `ProjectSettings`;
- resource meanings, source precedence, values, rates, visibility policy, labels, formatting target, click actions, layout, or UX changes;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

This package must not overlap implementation of `AM-WP-010` or `AM-WP-013` while the shared gateway contract is claimed; those edits are serialized.

## 5. Characterization Matrix

Required before edits:

1. Credits, Oil, Fuel, and Materials independently and together;
2. usable summary, live usable storage, legacy summary, text fallback, missing source, and every precedence transition;
3. equal values with changed source versions, changed values, Oil visibility changes, and player-faction changes;
4. bind, clear, rebind, boundary replacement, World replacement, scene/root replacement, and subsystem registration;
5. zero, capacity boundaries, large compact values, negative inputs, and materials current/capacity formatting;
6. resource-slot click routing for every displayed resource;
7. live-storage maximum entity count and no-player/mixed-faction storage.

Record source reads, query iterations, native allocations, semantic versions, model/string construction, slot/TMP/visibility writes, click dispatches, managed allocations, and apply time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged frames on the fully bound production Match HUD;
- each source route and precedence transition independently;
- `100` bind/clear/rebind and World/boundary replacement cycles;
- maximum production live-storage count before and after migration.

Acceptance:

- Credits, Oil, Fuel, and Materials all update through one canonical complete projection identity;
- exactly zero recurring production-owned managed bytes, temporary native arrays, recurring query aggregation, string conversion, TMP writes, or visibility writes while unchanged;
- bind applies immediately and one real semantic transition applies exactly once;
- equal displayed values preserve cached string references and cause zero TMP writes even when a source generation changes;
- source precedence, Oil visibility, formatting, labels, and Resource Exchange click routing remain unchanged;
- no stale values survive clear/rebind, World, boundary, or player-faction replacement;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits:

1. complete source identity and characterization without visible behavior change;
2. canonical all-slot projection and event/version-driven presentation apply;
3. live-storage aggregation migration plus lifecycle/open-surface acceptance.

Rollback if resource values, source precedence, visibility, labels, formatting, click routing, or economy behavior changes; stale values appear; native allocations leak; or the slice introduces polling, `SystemBase`, mutable global authority, protected-file edits, or non-allowlisted overlap.
