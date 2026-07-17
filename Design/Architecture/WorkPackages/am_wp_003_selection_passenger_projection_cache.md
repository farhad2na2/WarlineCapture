# AM-WP-003 - Selection And Passenger Projection Cache

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts the visible-semantic source versions below, and `AM-028` accepts the World binding, cache identity, invalidation, and projection/apply order.

Umbrella task: `AM-032`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, rows `UI-008` and `UI-010`.

## 1. Current Authority And Measured Defect

The active Match path is the behavioral baseline:

- `SelectionGameplayStartupSystemHelper` refreshes focused selection read models and the visible panel when `SelectionVersion` changes or every `0.1 s`.
- `FocusedUnitUiReadModelUiSystemHelper.Publish` clears and repopulates the passenger buffer, recomputes focused-unit fields, and writes the component on every refresh.
- `SelectionHudFeedbackUiSystemHelper` already owns semantic cache keys for focused panels, group summaries, selected buildings, passenger/cargo/fabrication panels, and hidden state. It correctly avoids managed model construction and view apply when those keys are unchanged.
- The remaining unchanged-state work occurs before those cache hits: focused projection publication, selected-tag counting, focused/status reads, passenger hashing, and multi-selection fingerprint scans. Group fingerprinting uses `ToArchetypeChunkArray(Allocator.Temp)` every refresh.

Two dormant shell adapters duplicate the projection:

- `UiShellReadModelAdapter.TryReadMatchHudSelection` discovers `World.DefaultGameObjectInjectionWorld`, creates World-bound queries, converts fixed strings, reconstructs managed models, and scans selected chunks through a temporary native array.
- `UiShellReadModelAdapter.TryReadMatchHudPassengerDrawer` independently discovers the default World, converts passenger names and health strings, and reconstructs up to three rows.

No production caller was proven for either dormant adapter. They must not become canonical unchanged. The package preserves the active Canvas behavior and existing cache semantics; it does not replace them with the dormant shell model.

## 2. Accepted Future Ownership

- ECS/source owners publish visible-semantic versions for selection structure, focused-unit presentation, selected-group presentation, passenger/cargo content, selected-building storage/fabrication, and command/order feedback.
- One World-bound managed `MatchHudSelectionManagedProjectionCache` owns conversion from accepted ECS/source snapshots into retained managed selection and passenger models.
- `SelectionHudFeedbackUiSystemHelper` remains the Match presentation composition edge unless `AM-028` explicitly selects another owner. Its existing cache behavior is the parity baseline and may be absorbed by the managed cache only after focused characterization proves equivalent invalidation.
- `MatchHudSelectionPanelView` and `MatchHudTransportPassengerDrawerView` remain apply-only retained views. They do not scan ECS, own gameplay truth, or rebuild unchanged rows.
- Recurring reads use an explicitly bound World and boundary. They never discover `World.DefaultGameObjectInjectionWorld`.
- Incomplete or destroyed boundaries fail closed without creating entities, components, buffers, queries, views, or rows.
- The dormant selection/passenger shell adapters are removed or reduced to calls into the canonical cache after parity is proven. No second projection authority remains.

This package introduces no new ECS update loop and no `SystemBase`. New ECS behavior, if required by accepted source-version ownership, uses unmanaged `ISystem`; the managed cache remains a plain lifecycle-owned C# object on the main-thread presentation boundary.

## 3. Required Semantic Versions And Cache Identity

`AM-027` and `AM-028` must name the writer and rollover contract for every identity before dispatch. The minimum cache identity is:

- bound `World` identity and lifecycle generation;
- Match/UI boundary entity identity and boundary generation;
- selection-structure version covering focused entity, selected entity set, selected building, and deselection;
- focused-visible version covering identity, ownership, unit category, health, visible status/order, move/board capability, portrait identity, and visible labels;
- command-state version covering attack-mode snapshot, hold/stop/scan availability and reasons, board availability, and command feedback;
- group-visible version covering member identity/category, aggregate health, visible order state, selected-building inclusion, and portrait kind;
- passenger-structure version covering transport identity, passenger identities/order, count, capacities, and drawer visibility;
- passenger-content version covering visible names, roles, health, and portraits;
- cargo/storage/fabrication version covering visible quantities, capacity, progress, status, and active recipe identity;
- selected-building version covering building identity, visible label, portrait, storage/fabrication mode, and action availability;
- localization, settings/accessibility, sprite-catalog, and resolution/layout generations;
- explicit invalidation generation and reason;
- visible row count and retained-row capacity identity.

Versions advance only when their visible semantic domain changes. Simulation counters, time, unrelated entities, inactive passengers, invisible config fields, and repeated writes of equal values must not rebuild the managed projection.

Time-dependent values are explicit. A visible progress or countdown owner may advance its narrow progress version at the required display cadence; it must not force selection structure, labels, portraits, or passenger rows to rebuild.

Every recurring read checks the complete accepted identity before:

1. selected-entity traversal or temporary allocation;
2. passenger-buffer traversal or hashing;
3. fixed-string conversion, localization, formatting, or sprite resolution;
4. managed model/list construction;
5. retained-row mutation or Canvas apply.

An unchanged identity performs zero source traversal beyond fixed-size version reads, zero conversion, zero model rebuild, zero visual apply, and zero recurring managed allocation.

## 4. Exact File Allowlist

Production files allowed after the dispatch gate is accepted:

- `Assets/Game/Scripts/Components/SelectionUiReadModelComponents.cs`
- `Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/FocusedUnitUiReadModelUiSystemHelper.cs`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs`
- `Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Selection.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs`
- `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs`
- `Assets/Game/Scripts/UI/Components/MatchHudTransportPassengerDrawerView.cs`
- `Assets/Game/Scripts/UI/Components/MatchHudTransportPassengerItemView.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/MatchHudSelectionManagedProjectionCache.cs` and its `.meta` if a new cache owner is required

Test files allowed:

- `Assets/Tests/Editor/SelectionStateSystemTests.cs`
- `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs`
- `Assets/Tests/Editor/SelectionUiReadModelLookupTests.cs`
- `Assets/Tests/Editor/MatchHudTransportPassengerDrawerTests.cs`
- `Assets/Tests/Editor/UnitTransportValidationTests.cs`
- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
- `Assets/Tests/Editor/MatchGcAllocationCallstackCaptureTests.cs`
- `Assets/Tests/Editor/MatchHudSelectionProjectionPerformanceValidation.cs` and its `.meta` if a focused real-path fixture is required
- `Assets/Tests/Editor/MatchHudSelectionManagedProjectionCacheTests.cs` and its `.meta` if a focused cache fixture is required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_003_selection_passenger_projection_cache_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_003_selection_passenger_projection_cache_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-032` tracker record and progress snapshot

Read-only fixtures:

- Match HUD prefabs and scenes;
- unit, vehicle, building, portrait, localization, and transport configs;
- command, audio, and visual-lock assets.

Hard exclusions:

- operation-map/static-map production files, scenes, trackers, and evidence;
- FirstLaunch, audio, UI visual-lock art, unrelated UI surfaces, packages, and `ProjectSettings`;
- unit selection rules, transport capacity rules, command behavior, balance, layout, art, localization content, or gameplay simulation;
- any file outside this allowlist without a reviewed package amendment.

## 5. Lifecycle, Order, And Thread Contract

- Selection and transport gameplay authority remains ECS/source-owned. The cache stores presentation snapshots only.
- Required order is gameplay mutation, ECS/source read-model publication, managed cache projection, then retained Canvas apply.
- The accepted composition owner binds the World once and clears the cache before World teardown, boundary replacement, scene unload, subsystem registration, or Match exit.
- Cache access and Unity object resolution are main-thread only. Source-version writers remain Burst-compatible where their domain permits it.
- The cache owns no `NativeContainer`, job, ECS entity, query, event subscription, GameObject, or sprite lifetime. It must not retain an `EntityManager` across a World identity check.
- Query ownership remains explicit and World-scoped. No query is recreated on recurring reads and no stale query survives World replacement.
- Version rollover is equality-based under the accepted `AM-028` contract; ordering comparisons are forbidden unless rollover behavior is separately proven.

## 6. Required Characterization Before Production Edits

Freeze all of these behaviors before authority changes:

1. no selection, single soldier, single vehicle, single aircraft, and selected building;
2. homogeneous and mixed multi-selection, including aggregate health and mixed orders;
3. selection change, focus change without membership change, deselect, selected entity destruction, and World replacement;
4. hold, stop, scan, attack-mode, move, board availability, and order/status changes;
5. passenger transport empty, partially occupied, full, soldier-only, vehicle-only, mixed, passenger damage, board, unload, passenger destruction, and reordered buffer;
6. resource cargo, selected-building storage, and material fabrication states, including progress-only changes;
7. passenger drawer hidden/open, selection change while open, transport removal while open, Match exit/re-entry, and view rebind;
8. localization, settings/accessibility, sprite-catalog, and resolution/layout invalidation;
9. missing boundary, incomplete read-model entity, destroyed World, and stale entity handles;
10. parity between current cache-key hit/miss behavior and the future source-version cache.

Characterization records visible model fields, apply counts, source traversals, conversion counts, row create/rebind counts, and allocations. It must prove whether the dormant shell methods have an active caller; absence of a caller is recorded as dormant debt, not assumed from naming.

## 7. Baseline And Allocation Gates

Capture the real active Match path before edits:

- `180` warmup frames plus `300` measured unchanged frames for no selection, single selection, multi-selection, passenger drawer hidden, and passenger drawer open;
- focused publication count, selected-query count, chunk-array creation count, passenger traversal count, conversion count, model rebuild count, view apply count, and retained-row mutation count;
- average, P95, P99, and maximum time for focused publication, panel projection, passenger projection, and view apply;
- transition allocations for every characterization case, reported separately from unchanged state;
- the existing Editor selection phase probes as attribution controls.

Acceptance targets after warmup:

- exactly zero recurring production-owned managed bytes for every unchanged measured state;
- zero `ToArchetypeChunkArray(Allocator.Temp)` calls from managed selection/passenger projection on unchanged frames;
- zero fixed-string conversion, localization, formatting, sprite resolution, model/list rebuild, row mutation, or Canvas apply on unchanged identity;
- one and only one rebuild/apply for each accepted semantic invalidation;
- no increase to global frame-time, GC, memory, or visual-quality budgets.

The global player-relevant `1,024`-byte allowance is not permission for this owner to allocate. Instrumentation allocations are measured and reported separately; they cannot be subtracted without raw attribution evidence.

## 8. Maximum Implementation Slices

The package is delivered as at most three independently stable commits:

1. **Characterization and semantic versions:** add real-path tests/evidence and accepted narrow versions without switching presentation authority.
2. **World-bound cache and retained apply:** move unchanged-state checks before scans/conversions, preserve active cache parity, and prove lifecycle invalidation.
3. **Duplicate retirement:** remove or delegate dormant default-World adapters and close the final broad architecture/allocation gates.

Each slice must compile with zero errors, pass its focused behavior and performance tests, pass integrated architecture checks, update evidence, and be independently reversible. Do not combine squad tray, minimap, command wheel, Resource Exchange, Build Drawer, placement, or unrelated UI work.

## 9. Acceptance And Rollback

Required acceptance:

1. All characterization states preserve visible fields, button availability, ordering, audio/command routes, and interaction behavior.
2. Real active-path unchanged measurements pass `180 + 300` for every required state with exactly zero recurring production-owned managed bytes.
3. Source versions advance only for visible semantic changes and each accepted invalidation rebuilds exactly once.
4. World/boundary replacement and Match exit/re-entry clear stale cache state without structural mutation during reads.
5. No recurring path discovers `World.DefaultGameObjectInjectionWorld`, allocates a temporary chunk array, recreates a query, or retains a stale `EntityManager`.
6. Only one selection/passenger projection authority remains.
7. Production and test assemblies compile with zero errors; focused and integrated architecture checks pass.
8. Evidence binds baseline and implementation commit/tree, source hashes, compressed logs, metrics, and focused review.

Rollback the affected slice if any of these occurs:

- stale title, health, order, command availability, passenger, cargo, storage, fabrication, portrait, or drawer state;
- selection, command, boarding, unload, or drawer interaction behavior changes;
- unchanged-state allocation or traversal remains above the accepted target;
- a new polling loop, `SystemBase`, service locator, default-World dependency, mutable static cache, unmanaged lifetime, or view-owned gameplay authority is introduced;
- the change requires a protected/non-allowlisted file or overlaps active map, FirstLaunch, audio, or visual-lock ownership.
