# AM-WP-001 - Resource Exchange Managed Projection Cache

Status: draft, dependency-blocked, and not dispatchable. Do not implement until `AM-020` through `AM-025`, `AM-027`, and the cache identity, World-binding owner, update-order, and invalidation rules in `AM-028` are accepted and this allowlist is amended to match those accepted owners.

Umbrella task: `AM-029`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-015`.

## 1. Responsibility And Ownership

Current responsibility:

- `UiShellReadModelAdapter.TryReadResourceExchange` discovers the default World through `TryGetBoundary`, calls `EnsureResourceExchangeUiState`, converts every fixed string in state, detail, recipe-card, and queue-row data, and constructs the complete managed `UiResourceExchangeModel` on every call.
- `EnsureResourceExchangeUiState` can add missing components and buffers. A read-model adapter therefore performs structural mutation today instead of failing closed when its boundary is incomplete.
- `ResourceExchangePopupRuntimeView.RefreshNow` receives that already-rebuilt model and only then rejects an unchanged `Version`.

Future responsibility:

- A managed `UiResourceExchangeManagedProjectionCache` owned by the UI ECS gateway retains one immutable managed model per exact cache identity.
- Cache identity must include the bound `World`, shell boundary entity, the accepted visible-projection version, recipe-card count, queue-row count, accepted settings version, accepted localization version, resolution/layout version, and an explicit invalidation-generation/reason value. `UiResourceExchangeStateComponent.Version` may fill the visible-version slot only if its fingerprint is first narrowed to visible semantics; otherwise a separate accepted visible-projection identity replaces it in this cache key. `AM-028` may refine representation but may not remove any identity domain without a recorded acceptance amendment.
- The cache check occurs before fixed-string conversion or managed-model construction. Resource Exchange conversion does not resolve sprites; sprite resolution belongs to the Build Drawer path and is outside this package.
- Cache invalidation occurs on subsystem registration, World replacement/destruction, boundary replacement/destruction, source-version change, item-count change, scene unload, localization refresh, settings change, resolution/layout change, and explicit invalidation.
- Missing Resource Exchange components or buffers fail closed without structural changes. Their creation remains an initialization/writer responsibility outside the read adapter.
- One accepted composition owner binds the active World to the gateway once per lifecycle. Recurring UI reads must not discover `World.DefaultGameObjectInjectionWorld`.

No new ECS update owner is allowed. This package introduces no `SystemBase`; the preferred runtime ECS writers remain unmanaged `ISystem` implementations.

## 2. Exact File Allowlist

Production files allowed:

- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.MinimapBuild.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeProjectionFingerprintUtilitySystemHelper.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeManagedProjectionCache.cs` and its `.meta` if the cache is a new owner

Test files allowed:

- `Assets/Tests/Editor/ResourceExchangeShellPopupPerformanceValidation.cs`
- `Assets/Tests/Editor/UiResourceExchangeReadModelSystemTests.cs`
- `Assets/Tests/Editor/UiResourceExchangeManagedProjectionCacheTests.cs` and its `.meta` if a focused fixture is required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_001_resource_exchange_projection_cache_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_001_resource_exchange_projection_cache_acceptance.json`
- Task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- The `AM-029` tracker record and progress snapshot

Read-only fixtures:

- `Assets/Game/Scenes/Menu.unity`
- `Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab`
- Resource Exchange configs and localization catalogs

Explicit exclusions:

- Operation-map files, scenes, trackers, and evidence
- FirstLaunch, audio, UI visual-lock art, unrelated UI surfaces, gameplay economy behavior, prefabs, packages, and `ProjectSettings`
- `ResourceExchangePopupRuntimeView` and popup view layout unless a separately reviewed characterization failure proves they must change

Dispatch gate:

- The current allowlist reserves `UIShellEcsPresentationSystem` as the likely lifecycle binding edge, but this is not acceptance of that owner. `AM-028` must name the sole World binder, its bind/unbind timing, and its stale-owner protection. If it selects another file, amend this allowlist before implementation.
- The package cannot dispatch while any required cache-identity version or invalidation owner is unnamed.

## 3. Assembly, Order, Authority, Lifecycle, And Thread Contract

- Production assembly: `Game.UI.Shell.Ecs`.
- Managed view assembly: `Game.UI.Runtime`; it remains a consumer and does not become data authority.
- Test assembly: `Game.Tests.Editor`.
- ECS authority remains `UiResourceExchangeReadModelSystem`. Its current fingerprint includes broad upstream version counters and every recipe/queue record, so it is not yet proven to increment `UiResourceExchangeStateComponent.Version` only after visible projection changes. Before cache acceptance, the allowlisted fingerprint/read-model owners must either narrow the version to visible semantics or add a separately accepted visible-projection identity that the managed cache uses.
- Required future order is simulation/read-model projection first, managed projection-cache read second, and Canvas apply last. The current MonoBehaviour/ECS ordering does not encode that guarantee; implementation must add an explicit ordering contract or a characterization test that proves the required order before acceptance.
- The managed cache is main-thread only because it reads `EntityManager` and constructs managed strings/models. Resource Exchange itself does not resolve Unity sprites.
- The cache owns no `NativeContainer`, job, subscription, GameObject, or ECS entity. It must not retain an `EntityManager` across a World identity check.
- World/boundary liveness is validated before returning a cached model. A dead World or missing/replaced boundary fails closed and clears the cache.

## 4. Characterization And Missing Cases

Existing characterization:

- `UiResourceExchangeReadModelSystemTests` covers ECS semantic projection and version behavior.
- `ResourceExchangePopupPrefabTests` covers button routing, active-view ownership, and overlapping-instance fallback.
- `ResourceExchangeShellPopupPerformanceValidation` covers `180` warmup plus `300` unchanged open frames and open/close transitions with a stable already-managed fake gateway.
- Resource Exchange simulation/request allocation tests cover gameplay owners, not the UI gateway.

Missing cases required before implementation acceptance:

1. The real ECS gateway is called for `180` warmup and `300` measured unchanged open frames.
2. Repeated reads return the same managed string references and allocate exactly zero recurring production-owned bytes.
3. A source-version change rebuilds exactly once and updates every visible field.
4. A source-version-only or inactive-record change that leaves every visible field unchanged does not advance the accepted visible-projection identity or rebuild the managed model.
5. Recipe-card or queue-row count change rebuilds exactly once even if a malformed fixture does not advance the version; acceptance may instead fail closed if `AM-028` forbids this fallback.
6. World replacement, boundary replacement, destroyed World, scene unload, missing boundary, and subsystem registration invalidate without stale data.
7. Version rollover is handled according to the `AM-028` comparison contract.
8. Localization, settings, and resolution/layout invalidation each rebuild exactly once and record their explicit invalidation reason.
9. An incomplete boundary returns false, allocates no projection, and causes no structural change; a separate writer/initialization fixture proves the complete boundary can still be created before presentation.
10. Recurring reads use only the explicitly bound World. A guard fails if the path reaches `World.DefaultGameObjectInjectionWorld` or silently changes World identity.
11. Required simulation-projection-presentation order is encoded or characterized and fails when stale data would be presented for a frame.
12. Popup open/close behavior and active-view fallback remain unchanged.

## 5. Required Baseline Metrics

Capture before production edits:

- Real ECS gateway bytes over `180` warmup plus `300` unchanged reads.
- Conversion/rebuild count, source version, card count, queue-row count, World identity, and boundary entity.
- Average, P95, P99, and maximum gateway-read time.
- Existing fake-gateway popup measurement as an instrumentation/view control.
- Real-gateway opening, closing, World replacement, scene unload, localization refresh, settings change, resolution/layout change, and version rollover allocation measured as eight separate cases outside unchanged open state.

The accepted target is exactly zero recurring production-owned managed bytes after warmup. The global player-relevant `1,024`-byte budget is not permission for this changed owner to allocate.

## 6. Validation And Acceptance

Required focused markers:

- Existing popup marker: `[ResourceExchangeShellPopupPerformanceValidation] result=Passed tests=2`
- New real-gateway marker: `[UiResourceExchangeManagedProjectionCacheValidation] result=Passed`
- New marker must report `warmupFrames=180`, `measuredFrames=300`, `productionAllocatedBytes=0`, rebuild count, World/boundary invalidation cases, and instrumentation-control bytes.

Required checks:

1. Focused Resource Exchange read-model, popup behavior, and cache tests pass.
2. Real-gateway unchanged-state measurement reports exactly zero recurring production-owned managed bytes.
3. Real-gateway opening, closing, World replacement, scene unload, localization, settings, resolution/layout, and version-rollover cases each report their allocation independently; opening/closing cannot rely only on the fake gateway.
4. Existing fake-gateway fully bound popup control remains exactly zero bytes as an instrumentation/view control.
5. Missing-boundary reads produce no structural changes and fail closed; recurring reads never discover the default World.
6. Scene unload, localization, settings, resolution/layout, World, boundary, count, and source-version invalidations each rebuild once without stale presentation.
7. Non-visible source changes do not advance the accepted visible-projection identity or rebuild the managed model.
8. Required update order is encoded or proven by a focused characterization test.
9. `Game.UI.Shell.Ecs`, `Game.UI.Runtime`, and `Game.Tests.Editor` compile with zero errors.
10. Integrated architecture checks and `git diff --check` pass.
11. Evidence binds the exact baseline, implementation commit/tree, source hashes, validation logs, and independent focused review.

Visual acceptance is limited to confirming no stale text, card, queue, resource, selection, or progress state after each invalidation case. No layout or art change is allowed.

## 7. Maximum Slice And Rollback

Maximum slice: one managed Resource Exchange projection cache and its focused tests/evidence. Do not combine Build Drawer, placement, selection, ARIA, Menu, or general gateway decomposition.

Rollback if any of these occurs:

- Visible data is stale after source, World, boundary, count, localization, or settings change.
- Recurring allocation remains above zero after warmup.
- A new `SystemBase`, broad service locator, default-World dependency, unmanaged lifetime, or view-owned data authority is introduced.
- Popup behavior, route behavior, or simulation authority changes.
- The change requires editing a protected or non-allowlisted file without a separately approved package amendment.
