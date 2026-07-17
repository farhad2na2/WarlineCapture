# AM-WP-010 - Armory Projection And Row Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts projection versions, `AM-028` accepts lifecycle/invalidation ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-017`.

## 1. Current Ownership And Risk

- `ArmoryContentListView.Update()` and `ArmoryCategoryNavigationView.Update()` independently read the same Armory category every frame.
- `UiShellArmoryCategorySystem` is already an unmanaged `ISystem`, but category state has no accepted semantic version.
- `ArmoryContentListView` recollects and sorts catalog metadata, then destroys and reinstantiates every generated row on refresh. Row listeners, names, formatted values, and inspection models are rebuilt with the rows.
- Reconfiguration of metadata resolvers forces refresh without an identity proving the source changed.
- Selection is held as a view reference and falls back to the first row after rebuild rather than a stable catalog-item identity.
- List and navigation initialize independently; list enable requests Characters and can overwrite a retained category.
- Gateway reads call `EnsureArmoryCategoryState`, so a nominal read can structurally mutate ECS state.
- Support is declared but currently has no catalog source. This package preserves that behavior; it does not invent Support content.

Risks are duplicated recurring work, open-surface allocations, transient category disagreement, stale selection, row/listener churn, and hidden write-on-read behavior. The package must not add another polling owner or create managed Armory authority outside existing ECS/category and catalog contracts.

## 2. Accepted Future Ownership

- `UiShellArmoryCategorySystem` remains the category transition owner and advances a semantic category version only when category actually changes.
- One Armory projection owner combines category version with an explicit catalog/resolver generation and produces a stable projection identity.
- `ArmoryContentListView` becomes an apply-only serialized presentation boundary. It does not independently poll ECS or recollect unchanged catalog data.
- Item views are retained in a bounded pool keyed by a stable `ArmoryCatalogItemKey` derived from source kind and source-prefab identity. Ordinary category changes reuse rows after warm-up.
- Selected state is a stable item key. It survives refresh when valid and otherwise falls back deterministically to the first valid item; empty and Support categories clear list/details without stale state.
- Navigation and list consume the same projection/category identity in deterministic order.
- Resolver/source rebinding increments catalog generation only for a real identity change.
- Gateway reads are pure. Boundary/default creation occurs in explicit bootstrap ownership, never in a read API.
- Shell/root teardown releases row listeners, pooled Unity-object references, query handles, and projection identity idempotently. Subsystem registration clears static gateway state.

No new `SystemBase`, view-local polling, mutable static Armory model, default-World lookup, broad manager/controller/provider/service type, or catalog/design-content change is allowed.

## 3. Version And Invalidation Contract

Minimum identity:

- Armory category and category version;
- catalog generation and metadata-resolver generation;
- stable source kind plus source-prefab identity for every item;
- ordered item-key/content fingerprint;
- selected item key;
- localization/label-catalog generation;
- shell/root lifecycle and binding generations;
- explicit invalidation generation and reason.

Rules:

- Equal category requests do not increment category version or rebuild navigation/list/details.
- Equal catalog/resolver bindings do not increment catalog generation.
- One real category or catalog change performs at most one collect, sort, projection, and apply.
- Item order remains deterministic under current category and metadata ordering contracts.
- Selection persists only when its stable key remains in the new projection.
- Empty projections clear row visibility and inspection details once.
- Read APIs never add components, buffers, roots, or other ECS structure.
- Pool capacity grows only to observed peak demand, has an explicit ceiling/exhaustion policy, and releases on terminal root teardown.
- Version rollover uses equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Screens/ArmoryContentListView.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationView.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCatalogQueryUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCatalogItemView.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryInspectionPanelView.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCatalogCategoryVisualSet.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCatalogProjectionIdentity.cs` and its `.meta` if required
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the versioned Armory read contract
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for centralized Armory apply and lifecycle binding
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for Armory projection/query lifecycle
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Settings.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Contracts.cs` only for the versioned Armory read contract
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellArmoryCategorySystem.cs`

Test files allowed:

- `Assets/Tests/Editor/ArmoryCurrentContentPrefabTests.cs`
- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs` only for route/lifecycle characterization
- `Assets/Tests/Editor/ArmoryProjectionLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/ArmoryOpenSurfacePerformanceValidation.cs` and its `.meta` if required
- existing ECS/architecture tests only when the Armory contract genuinely changes

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_010_armory_projection_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_010_armory_projection_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only dependencies:

- catalog prefab sources/configs, Armory prefabs/scenes, visual-lock assets, localization catalogs, and Support design/content.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, gameplay, scenes, prefabs, visual-lock art, catalog/config content, packages, and `ProjectSettings`;
- category membership, item stats/descriptions/availability, ordering semantics, Support content, navigation, visual state, art, or UX changes;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

This package must not overlap implementation of `AM-WP-011` or `AM-WP-013` while the shared gateway contract is claimed; those edits are serialized.

## 5. Characterization Matrix

Required before edits:

1. initial route install, repeated route entry, equal/changed category request, and resolver/source rebind;
2. Characters, Vehicles, Buildings, Aircraft, Support, and empty category projections;
3. selection retained, selected item removed, no selection, first-item fallback, and inspection update;
4. duplicate/null prefab sources, missing metadata, equal display names, and deterministic ordering;
5. one item, production maximum item count, pool ceiling, and exhaustion behavior;
6. enable/disable, root replacement, World replacement, subsystem registration, and terminal teardown;
7. gateway reads against present and missing state, proving no read-side structural mutation;
8. repeated lifecycle cycles proving one callback per row/category button.

Record category/catalog generations, gateway reads/writes, collections, sorts, projection builds, row creates/reuses/releases, listeners, selected keys, inspection applies, Canvas mutations, allocations, and apply time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged open frames on the fully bound production Armory route;
- every category switch after warming maximum required row capacity;
- `100` route open/close, enable/disable, World/root replacement, and resolver-rebind cycles;
- equal request/bind, changed request/bind, selection retention/fallback, empty/Support, and pool exhaustion independently.

Acceptance:

- exactly zero recurring production-owned managed bytes, collection, sorting, row creation/destruction, or Canvas mutation while Armory is open and unchanged;
- no Armory view-local polling remains and one centralized version-gated projection/apply path owns updates;
- equal category and resolver/source identities produce zero projection rebuild;
- one real change produces one collect, sort, projection, and apply;
- warmed ordinary category changes instantiate/destroy zero rows;
- selected item identity is retained or deterministically falls back as specified;
- read APIs perform zero ECS structural changes;
- one callback per interaction after repeated lifecycle cycles and no stale static/query/pool state after release;
- existing categories, catalog content, ordering semantics, details, navigation, and visible output remain unchanged;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits:

1. category/catalog identities, pure gateway reads, and characterization without visible behavior change;
2. centralized projection plus keyed retained-row pool and selection identity;
3. lifecycle/exhaustion hardening and complete open-surface acceptance.

Rollback if category/content/order/detail/navigation behavior changes; Support gains unintended content; selection becomes nondeterministic; listeners duplicate; rows leak or disappear; read APIs mutate ECS; or the slice introduces polling, `SystemBase`, mutable global authority, protected-file edits, or non-allowlisted overlap.
