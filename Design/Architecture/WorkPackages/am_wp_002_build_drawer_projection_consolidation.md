# AM-WP-002 - Build Drawer Projection Consolidation

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts source-first visible-semantic versions, and `AM-028` accepts cache identity, World binding, invalidation, and projection/apply ordering.

Umbrella task: `AM-030`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-013`.

## 1. Current Ownership And Defect

The active production path is the behavioral baseline:

- `UIShellContentView` installs the drawer and composition injects catalog metadata, `IBuildingUiCommand`, and `IBuildingUiQuery`.
- `BuildDrawerCatalogRuntimeView` refreshes the catalog when enabled or selected and polls production queue data every `0.2 s`.
- Queue reads scan the runtime-building dictionary, then the view hides/rebinds rows and formats names, times, percentages, and counts.
- Catalog refresh creates category arrays and destroys/recreates item rows. Queue rows are partly pooled, but unchanged values are still rebound.

Three competing noncanonical owners also exist:

1. `UiBuildDrawerReadModelSystem` is scheduled in `PresentationSystemGroup` and contains another catalog/queue projector, request processor, mutable static lists, and static sprite cache. No production caller configures its source.
2. `TryReadBuildDrawer` has no active production caller. If activated, it performs read-side structural initialization through `EnsureBuildDrawerState`.
3. `UiShellStateSystem` seeds placeholder Build Drawer detail, catalog, and queue data.

The dormant model must not become the canonical path unchanged. It caps catalog/queue rows at `7 + 2`, omits producer details and active action feedback, and formats visible values differently from the live drawer.

## 2. Accepted Future Authority

- Preserve the live Canvas behavior, action routing, formatting, producer details, close behavior, and audio routing as the characterization baseline.
- Introduce one plain managed `UiBuildDrawerManagedProjectionCache` as the sole managed projection owner. It is not an ECS update system and introduces no `SystemBase`.
- Reduce `BuildDrawerCatalogRuntimeView` to input forwarding and visual apply. It must not scan gameplay state, rebuild unchanged rows, or own projection authority.
- Feed the cache from explicit, World-bound source readers with accepted visible-semantic versions. Do not discover `World.DefaultGameObjectInjectionWorld` during recurring reads.
- Retire the dormant fixed-string projector, read-side structural initialization, placeholder state, mutable static lists, and static sprite cache after parity is proven.
- Centralized shell presentation triggers projection reads only after the accepted simulation/source projection order. One explicit composition owner binds and unbinds the World.
- Incomplete boundaries fail closed without adding ECS components or buffers.

## 3. Required Version And Identity Contract

`AM-027` and `AM-028` must name every owner and representation before this package may dispatch. The minimum identity domains are:

- bound `World` and shell/build-drawer boundary identity;
- catalog source identity, catalog/config generation, and metadata-resolver generation;
- availability generation covering credits, materials, compatible producers, per-producer/global capacity, placement state, and placement status;
- queue structure generation and independent queue-progress generation;
- visible catalog count, visible queue count, and retained-row capacity identity;
- selected category/item identity and selection generation;
- localization, settings, resolution/layout, and sprite-catalog generations;
- explicit invalidation generation and reason;
- scene unload/reload, World replacement, boundary replacement, subsystem registration, and version rollover.

Progress-only changes may update retained row values but must not rebuild catalog structure. Changes to irrelevant producers/configs must not rebuild the visible drawer.

Every recurring read checks the complete accepted identity and counts before any gameplay dictionary scan, catalog/queue traversal, formatting, string or sprite conversion, managed-model construction, or visual apply. An unchanged identity performs zero source scans, zero conversions, zero model/row rebuilds, and zero managed allocation.

## 4. Exact File Allowlist

Production files allowed after the dependency gate is accepted:

- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.MinimapBuild.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Presentation.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerManagedProjectionCache.cs` and its `.meta`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresentationSystemHelper.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerProductionQueueUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerReadModelSystem.cs` and its `.meta`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerProjectionSystemHelper.cs` and its `.meta`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs`

Test files allowed:

- `Assets/Tests/Editor/BuildDrawerCatalogQueryUiSystemHelperTests.cs`
- `Assets/Tests/Editor/UiBuildDrawerDualCostReadModelTests.cs`
- `Assets/Tests/Editor/BuildingUiQuerySystemTests.cs`
- `Assets/Tests/Editor/UiBuildDrawerManagedProjectionCacheTests.cs` and its `.meta`
- `Assets/Tests/Editor/BuildDrawerShellPopupPerformanceValidation.cs` and its `.meta`
- `Assets/Tests/Editor/EcsBurstHotPathArchitectureTests.cs`
- `Assets/Tests/Editor/ProductionSourceGrowthArchitectureTests.cs`

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_002_build_drawer_projection_consolidation_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_002_build_drawer_projection_consolidation_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-030` tracker record and progress snapshot

Hard exclusions:

- operation-map, static-map, FirstLaunch, audio production/tests, UI visual-lock art, prefabs, scenes, configs, packages, and `ProjectSettings`;
- economy, production timing, placement rules, catalog content, balance, or user-facing layout changes;
- any file outside the allowlist without a reviewed package amendment.

## 5. Maximum Implementation Slices

The package is delivered as three independently stable commits:

1. **Characterization and source versions:** freeze live behavior, deterministic ordering, visible formatting, action/audio routes, source identities, and invalidation ownership. No authority switch.
2. **Managed cache and retained apply:** add the World-bound cache, real-gateway coverage, retained catalog/queue rows, progress-only apply, and centralized presentation trigger. Parity comparison with the old path is limited to tests or Editor-only instrumentation; both projectors must never execute simultaneously in production, and every commit must have exactly one active production projector.
3. **Authority cutover and retirement:** switch to the accepted cache, remove the direct polling/projection work and dormant projector/placeholder state, then prove no duplicate owner remains.

Do not combine this package with Resource Exchange, placement-bar, selection, ARIA, Menu, or visual changes.

## 6. Required Characterization

Existing coverage provides a starting point:

- `BuildDrawerCatalogQueryUiSystemHelperTests`: live Canvas catalog, action, queue, pooling, and input behavior (`25` cases at audit time).
- `UiBuildDrawerDualCostReadModelTests`: dormant ECS dual-cost behavior (`6` cases at audit time).
- `BuildingUiQuerySystemTests`: bounded gameplay-query behavior.

Missing cases required before authority cutover:

1. Fully bound real production drawer: `180` warmup plus `300` unchanged open frames with exactly zero recurring production-owned managed bytes.
2. Exactly one rebuild/apply for each accepted version or invalidation domain.
3. No rebuild for irrelevant producers, inactive configs, or source-version changes that leave visible data unchanged.
4. Deterministic multi-producer ordering with an explicit stable tie-break independent of dictionary iteration order.
5. Progress-only updates preserve catalog and queue-row identity and do not cause structural rebuild.
6. Catalog/queue growth and shrink, selected-item removal, late binding, version rollover, and queue-capacity changes.
7. World/boundary replacement, scene unload/reload, subsystem registration, localization, settings, resolution/layout, and sprite refresh.
8. Incomplete boundary fails closed with no structural ECS mutation and no stale model.
9. Request/source-projection/cache/apply order prevents one-frame stale actions or values.
10. Existing button actions, close behavior, placement entry, producer focus, feedback text, and direct `ButtonPrimaryClick` versus ECS `DrawerOpen/Close` audio routes remain unchanged.

## 7. Performance And Allocation Acceptance

Capture baseline and final metrics for:

- open, close, unchanged open, category switch, selection switch, queue structure change, queue progress change, World replacement, scene reload, localization, settings, resolution/layout, sprite refresh, and version rollover;
- projection rebuild count, structural apply count, progress-only apply count, row create/destroy/reuse counts, source scan count, and string/sprite conversion counts;
- average, P95, P99, and maximum projection plus apply time;
- exact production-owned managed bytes for every case.

Unchanged open state must report zero recurring production-owned managed bytes after warmup. Transition and invalidation allocations are reported separately and must stay within the accepted per-event budgets defined before implementation.

## 8. Rollback Conditions

Rollback the slice if any of these occurs:

- catalog, cost, material, requirement, producer, capacity, queue, progress, placement, feedback, close, or audio behavior changes;
- visible ordering depends on dictionary iteration or another unstable traversal;
- stale data survives World/boundary/scene/version/invalidation changes;
- a recurring default-World lookup, read-side structural mutation, mutable static model/cache, new `SystemBase`, duplicate projector, or view-owned data authority remains;
- dormant row caps or placeholder content become production behavior;
- unchanged open state allocates after warmup;
- the change requires a protected or non-allowlisted file without prior package amendment.
