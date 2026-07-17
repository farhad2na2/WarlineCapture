# AM-WP-013 - Main Menu Resource Header Projection

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves the authoritative Main Menu resource source and active binding seam, `AM-027` accepts projection identity, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-003`.

## 1. Current Ownership And Risk

- The visible Main Menu header is installed through `CommanderProfileRouteLifecyclePresentation` and `UIShellContentView`, but no active dynamic source-to-header apply path is proven.
- `UiShellReadModelAdapter.TryReadMainMenuResources()` reads `UiShellMainMenuResourcesComponent` and converts Credits, Supplies, and Command fixed strings on every call, but no production apply caller was found.
- Default values are duplicated across shell state/default initialization and menu bootstrap composition.
- The dormant ECS component/model has no semantic version or explicit invalidation identity.

Risks are activating a dormant adapter without an authoritative meta-economy owner, presenting duplicated defaults as live progression, repeated conversion if a caller is added, and stale values after profile/session replacement. This package fails closed: it must first prove whether these values are profile progression, session resources, placeholders, or another contract.

## 2. Accepted Future Ownership

- `AM-026` names one authoritative source and proves one active binding seam before implementation begins.
- If the header is intentionally static, remove or quarantine the dormant dynamic adapter and keep one canonical static-default owner.
- If the header is dynamic, one projection owner publishes Credits, Supplies, and Command with a semantic version derived from the authoritative source.
- Managed string construction and TMP application occur only after the semantic identity changes; equal values perform no conversion or UI write.
- Route install applies once; profile/session replacement, sign-in/out, localization, root replacement, and explicit refresh invalidate exactly once.
- Duplicated defaults collapse to one canonical owner without changing displayed values.
- Shell teardown and subsystem registration release query/cache/static binding state idempotently.

No new `SystemBase`, polling, mutable static progression authority, default-World lookup, broad manager/controller/provider/service type, or invented resource economy is allowed.

## 3. Version And Invalidation Contract

Minimum identity if dynamic:

- Credits, Supplies, and Command values;
- authoritative profile/progression/session source generation;
- active profile/account identity;
- localization/formatting generation;
- shell route/root and binding generations;
- explicit invalidation generation and reason.

Rules:

- Equal displayed values do not advance the display generation, rebuild strings, or write TMP.
- Source-generation-only changes may update provenance but reuse cached strings.
- Route re-entry with the same source identity performs zero rebuild.
- Profile/account/session replacement cannot reuse stale cached values.
- Missing or unavailable source follows one characterized fail-closed visible state; placeholder defaults are never silently presented as earned progression.
- Read APIs are pure and never create ECS state.
- Version rollover uses equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Shell/CommanderProfileRouteLifecyclePresentation.cs` only for the proven header binding seam
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for header lifecycle binding
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the versioned Main Menu resource read contract
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` only to consolidate duplicate Main Menu resource defaults
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs` only to consolidate duplicate Main Menu resource defaults
- `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs` only to consolidate duplicate Main Menu resource defaults
- one narrowly named Main Menu resource projection/presentation file and `.meta` if required

Test files allowed:

- existing Main Menu shell/route tests only for direct header characterization;
- `Assets/Tests/Editor/MainMenuResourceHeaderProjectionTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/MainMenuResourceHeaderPerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_013_main_menu_resource_header_projection_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_013_main_menu_resource_header_projection_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only dependencies:

- profile/progression/economy production, account/session ownership, Main Menu prefabs/scenes, visual-lock assets, and localization catalogs.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, Match economy, profile/progression balance, account/session production, scenes, prefabs, visual-lock art, packages, and `ProjectSettings`;
- resource meanings, values, earnings, persistence, labels, formatting targets, layout, or UX changes;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

This package must not overlap implementation of `AM-WP-010` or `AM-WP-011` while the shared gateway contract is claimed; those edits are serialized.

## 5. Characterization Matrix

Required before edits:

1. prove visible header source, binding owner, and whether each field is static, placeholder, or live;
2. route install/re-entry, profile/session replacement, sign-in/out where applicable, root replacement, and subsystem registration;
3. Credits, Supplies, and Command independently and together;
4. equal values, changed values, source-generation-only change, missing source, and malformed/default source;
5. localization/formatting change and large/zero values;
6. dormant adapter retained, activated, or removed, with explicit rationale and call graph;
7. every duplicated default and its current consumer.

Record source reads, semantic generations, fixed-string conversions, model/string construction, TMP writes, default reads, subscribers/query handles, allocations, and apply time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged frames on the fully bound production Main Menu;
- `100` route install/re-entry and profile/session/root replacement cycles;
- equal/changed/missing source and each field independently.

Acceptance:

- one authoritative source and one source-to-header apply path are proven;
- dormant projection code is removed or explicitly characterized and covered;
- duplicated defaults reduce to one canonical owner without changing visible values;
- exactly zero recurring production-owned managed bytes, fixed-string conversions, model construction, TMP writes, or query work while unchanged;
- one real semantic change applies only affected fields exactly once;
- no stale profile/session values survive lifecycle replacement;
- current labels, formatting, layout, visual output, and route behavior remain unchanged;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most two independently stable commits after source ownership is accepted:

1. source/binding characterization, canonical defaults, and projection identity;
2. version-gated apply, dormant-path disposition, and lifecycle/open-surface acceptance.

Rollback if placeholder or Match resources become profile authority; displayed values, labels, formatting, persistence, or route behavior change; stale account/profile data appears; or the slice introduces polling, `SystemBase`, mutable global authority, protected-file edits, or non-allowlisted overlap.
