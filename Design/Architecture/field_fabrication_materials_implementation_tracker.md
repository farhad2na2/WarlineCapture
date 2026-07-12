# Field Fabrication And Materials Implementation Tracker

Date: 2026-07-12
Status: Planned - implementation not started
Design source: `../Field_Fabrication_Materials_Design.md`

## Objective

Turn the currently nonfunctional `Building_Ammunition_Depot` into the player-facing Field Fabrication Depot. Reuse the existing physical Oil and tray-truck logistics path, convert delivered Oil into one authoritative faction tactical Materials value, make battlefield construction spend Credits plus Materials, and retain the Resource Exchange as a deliberately expensive recovery option.

This tracker is the implementation and evidence authority. It does not redefine balance or player intent from the high-level design.

## Non-Negotiable Architecture Contract

- Follow `gameplay_solid_ecs_contract.md` and `performance_regression_contract.md` in every phase.
- ECS is authoritative for simulation, resource ownership, requests, results, reservations, and status.
- Prefer Burst-compatible unmanaged `ISystem` for conversion, summary projection, affordability, spending, and route-candidate work.
- Keep managed Unity references at authoring, composition, UI view, audio/VFX presentation, and editor boundaries only.
- Do not add `Manager`, `Controller`, `Facade`, `Bridge`, `Port`, broad `Service`, global registry, mutable static gameplay state, or a new updating `MonoBehaviour` loop.
- Bare `*System` names are ECS systems only. A required managed helper uses an approved reason suffix from the architecture contract.
- Runtime files must compile inside an explicit approved `.asmdef`; no new runtime code may fall into `Assembly-CSharp`.
- Do not add a parallel Materials wallet, dual-write two Materials values, or periodically reconcile conflicting values.
- Do not mirror Oil/Fuel into a new faction wallet. Existing physical storage and summaries remain authoritative.
- No LINQ, closure capture, boxing, recurring managed collections, per-frame string formatting, broad scene searches, or allocating ECS snapshots in touched hot paths.
- Use typed enums, components, request/result buffers, and versioned read models instead of string state.
- Project required components during spawn/config projection; do not add/remove structural components every production tick.
- Keep balance values in config assets or projected config components. Do not hard-code conversion rates, capacities, costs, markups, or timers.
- Validate and document every meaningful slice before advancing.

## Locked Naming

Player-facing:

- `Field Fabrication Depot`
- `Materials`
- `Converts Oil into Materials.`

Compatibility:

- Preserve `Building_Ammunition_Depot` as the V1 internal lookup id.

Expected new runtime names, subject only to a documented collision discovered during implementation:

- `FactionTacticalMaterialsComponent`
- `MaterialFabricationComponent`
- `MaterialFabricationInputTag`
- `MaterialFabricationStatusCode`
- `MaterialFabricationBlockReasonCode`
- `MaterialFabricationSystem`
- `MaterialFabricationRequestComponent` only if an enabled/disabled command is implemented
- `MaterialFabricationResultComponent` only if a request exists
- `UiMaterialFabricationReadModel`

Do not substitute vague names such as `ResourceManager`, `MaterialController`, `DepotService`, `ProductionFacade`, `MaterialCache`, or `BuildingPlayer`.

## Assembly Ownership Map

| Responsibility | Assembly | Allowed content |
|---|---|---|
| ECS simulation data | `Game.Components` | Components, tags, byte-backed enums, request/result buffers, version fields. |
| Authored balance/config | `Game.Configs` | Existing building config extension or a focused `*Config` ScriptableObject. |
| Conversion, affordability, spending, logistics policy | `Game.Runtime` | `ISystem`, Burst jobs, pure stateless math, typed mutation paths. |
| Startup/config projection | `Game.Composition` | Narrow projection/wiring only; no recurring gameplay policy. |
| UI contracts | `Game.UI.Contracts` | View-facing immutable contracts where needed. |
| ECS UI contracts | `Game.UI.Shell.Contracts.Ecs` | ECS read-model components and typed UI requests. |
| ECS UI projection | `Game.UI.Shell.Ecs` | Versioned Materials/header/depot/build affordability projection. |
| Canvas presentation | `Game.UI.Runtime` | Serialized-reference views and localized reason rendering. |
| Authoring/baking | `Game.Authoring` | Authoring components and bakers only if existing config projection cannot supply the data. |
| Editor validation | `Game.Editor` | Config migration/build tools only. |
| Tests | `Game.Tests.Editor`, `Game.Tests.PlayMode` | Architecture, deterministic simulation, UI, integration, and performance validation. |

Dependency direction remains inward toward components/config/contracts/runtime. Runtime must not reference concrete UI, authoring, editor, or test assemblies.

## Progress Summary

Overall implementation progress: 17% (18/103 checklist items complete).

Planning and inventory findings are complete. Runtime implementation is 0% complete.

Progress is checklist-based. Every `- [ ]` or `- [x]` implementation/validation row counts. Update the numerator, denominator, table, status, and evidence log in the same commit as each completed batch.

| Phase | Status | Complete | Total | Progress | Gate |
|---|---|---:|---:|---:|---|
| 0. Inventory and baseline | Complete | 12 | 12 | 100% | Ownership, file targets, compile state, focused behavior, p95/p99, and managed-allocation baselines are recorded. |
| 1. Canonical tactical Materials and Credits | In progress | 6 | 13 | 46% | Canonical Materials data/startup and player Credits ownership are validated. Exchange, HUD/build affordability, profile policy, combined tests, and ownership ratchet remain. |
| 2. Config and building projection | Pending | 0 | 10 | 0% | Depot config is valid, authored, and projected consistently. |
| 3. Oil destination routing | Pending | 0 | 11 | 0% | Tray routing is deterministic, reserved, and stable. |
| 4. Oil-to-Materials conversion | Pending | 0 | 11 | 0% | Conversion is deterministic, capped, typed, and no-GC. |
| 5. Credits + Materials construction | Pending | 0 | 11 | 0% | Placement spends both resources exactly once. |
| 6. HUD and selected-building UI | Pending | 0 | 11 | 0% | No placeholder Supply text; read models are versioned. |
| 7. Exchange and balance safety | Pending | 0 | 8 | 0% | Import is expensive recovery; no arbitrage exists. |
| 8. AI, telemetry, and scenario safety | Pending | 0 | 7 | 0% | AI shares rules; scenarios cannot deadlock silently. |
| 9. Integration, performance, and closeout | Pending | 0 | 9 | 0% | Architecture, GC, profiler, gameplay, and docs pass. |

## Phase 0: Inventory And Baseline

- [x] Confirm `Building_Ammunition_Depot` is used by current base layout, custom game, AI plans, configs, and map content.
- [x] Confirm the current Ammunition Depot config has no Oil/Fuel storage or production and its old ammunition description has no mechanic.
- [x] Confirm physical Oil/Fuel ownership is `BuildingResourceStorageComponent` plus existing faction summaries.
- [x] Confirm `ResourceExchangeWalletComponent` currently stores Materials and also mirrors Credits/Oil/Fuel/Rush Tickets.
- [x] Confirm the Match HUD Supply value is currently seeded as placeholder `92/120` in UI shell state.
- [x] Confirm current building definitions expose Credits price but not a live Materials construction cost.
- [x] Record existing explicit assembly boundaries and naming rules in this tracker.
- [x] Trace every runtime read/write of `ResourceExchangeWalletComponent.Materials` and classify startup, simulation, UI, test, or compatibility ownership.
- [x] Trace the authoritative active-match Credits mutation used by build placement and Exchange; identify the consolidation requirement.
- [x] Trace placement accept/cancel/refund flow and record the exact point where cost is committed.
- [x] Capture baseline profiler markers and `GC.Alloc` call stacks for fuel logistics, Match HUD, build placement, and active Exchange.
- [x] Record the exact initial implementation file list after the ownership traces; add files only with a tracker note explaining responsibility and assembly.

Inventory findings:

- `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Ammunition_Depot_Config.asset` has `price: 45000`, `maxHealth: 900`, zero Oil/Fuel values, and no production entries.
- `Assets/Game/Scripts/Configs/CustomGameMapConfig.cs`, `InitialFactionBaseLayoutPlanner`, `InitialUnitsSpawnSystem`, and AI data use `Building_Ammunition_Depot`; V1 must preserve that internal id.
- `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs` defines physical `ResourceKind` as Oil/Fuel and owns Oil/Fuel building storage and reservations.
- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs` defines the current Exchange wallet. It is too broad to become an additional permanent Materials authority.
- Production Exchange mutation is concentrated in `ResourceExchangeRequestValidationSystem` and `ResourceExchangeQueueTickSystem`; `UiResourceExchangeReadModelSystem` is a read-model consumer. Exchange tests instantiate the wallet directly and must migrate with production code in the same slice.
- `UiShellStateSystem` and `UiShellEcsGateway.ReadModels.DefaultState` seed `SupplyText` as `92/120`; this must be removed from live Match state.
- The existing fuel tracker confirms a no-GC, versioned ECS logistics path that this feature should extend rather than replace.
- Tactical Credits currently have three conflicting owners: player `RuntimeResourceUtilitySystemHelper._dollars`, AI `FactionEconomy.Money`, and `ResourceExchangeWalletComponent.Credits`. `FactionEconomy.Money` is the target ECS authority; player and Exchange paths must migrate to it rather than adding a fourth value.
- Configured building selection passes a UI-supplied Credits price through `BuildingProductionRequestSystemHelper`, stores it in managed `BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacementCost`, and spends through `RuntimeResourceUtilitySystemHelper.TrySpendDollars` during `Confirm` immediately before the managed commit callback.
- Placement cancel before confirmation spends nothing. Current confirmation has only `NotEnoughMoney`, no Materials cost, and no ECS transaction/reservation. The new dual-cost path must resolve authored cost by stable building id and commit/refund through typed ECS data rather than adding another managed delegate.

Initial implementation file map:

| Area | Existing files to extend or migrate | New file only if responsibility cannot fit cleanly |
|---|---|---|
| Faction economy | `Components/FactionAIComponents.cs`, `Systems/FactionEconomyStartupSystem.cs`, `Systems/RuntimeResourceUtilitySystemHelper.cs` | `Components/MaterialFabricationComponents.cs` for focused Materials/fabrication ECS data. |
| Building config/catalog | `Configs/GameplayConfigModels.cs`, `Configs/Prefabs/BuildingDefinitionAuthoringPrefabConfigAsset.cs`, Ammunition Depot config asset, `Systems/BuildingDefinitionPrefabSystemHelper.cs` | Focused `MaterialFabricationConfig` only if generic building conversion data cannot represent the feature. |
| Building projection/conversion | `Systems/BuildingRuntimeEntityCompositionSystemHelper.cs`, `Systems/BuildingResourceProductionEcsSystem.cs` | `Systems/MaterialFabricationSystem.cs` when conversion remains a distinct `ISystem`. |
| Oil hauling/reservations | `Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Systems/ResourceHaulerUtilitySystemHelper.cs`, `Systems/BuildingResourceStorageTransferSystemHelper.cs`, `Components/BuildingRuntimeEcsComponents.cs` | ECS-native route candidate data/system only if required to remove measured managed hot-path debt. |
| Placement economy | `Components/BuildingRuntimeEcsComponents.cs`, `Systems/BuildingProductionRequestSystemHelper.cs`, `Systems/BuildingPlacementLifecycleCompositionSystemHelper.cs`, `Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs`, `Systems/BuildingPlacementCommitCompositionSystemHelper.cs` | Focused ECS placement economy request/reservation system if the current command buffers cannot safely carry atomic dual-cost transactions. |
| Exchange | `Components/ResourceExchangeComponents.cs`, `Systems/ResourceExchangeRequestValidationSystem.cs`, `Systems/ResourceExchangeQueueTickSystem.cs` | No parallel wallet file. |
| HUD and Build Drawer | `UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`, `UI/Shell/Ecs/UiShellStateSystem.cs`, `UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`, `UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs`, `UI/Shell/Ecs/UiBuildDrawerReadModelSystem.cs`, `UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs` | Focused `UiMaterialFabricationReadModelSystem.cs` if selected-building projection is not already generic. |
| Validation | Existing building production/placement, fuel logistics, Exchange, UI-shell, architecture, Burst, and performance tests under `Assets/Tests/Editor` and `Assets/Tests/PlayMode` | Focused Materials/config/conversion/transaction tests named by behavior. |

Phase 0 exit criteria:

- One current owner is named for every resource and transaction.
- Before/after profiling scenarios and marker names are recorded.
- No unresolved duplicate Materials or Credits path remains hidden.
- The implementation file list is locked and respects assembly direction.

## Phase 1: Canonical Tactical Materials And Credits

- [x] Add `FactionTacticalMaterialsComponent` in `Game.Components` with faction id, current amount, capacity, produced/imported/exported/spent counters as required, and version.
- [x] Define overflow-safe integer mutation rules and deterministic version increments.
- [x] Project one component for each participating faction at scenario startup.
- [x] Seed starting Materials and capacity from authored scenario/config data.
- [x] Establish `FactionEconomy.Money` as the one ECS tactical Credits authority for player and AI factions.
- [x] Migrate player build affordability/spend away from `RuntimeResourceUtilitySystemHelper._dollars` ownership to typed ECS transactions against `FactionEconomy.Money`.
- [ ] Migrate Exchange Credits and Materials import/export to `FactionEconomy.Money` plus `FactionTacticalMaterialsComponent`.
- [ ] Migrate Match HUD and build affordability to read this canonical component.
- [ ] Remove `ResourceExchangeWalletComponent.Credits` and `.Materials` authority without a steady-state dual-write period.
- [ ] Remove or narrow Exchange wallet Oil/Fuel mirrors so physical storage/summaries remain authoritative.
- [ ] Document persistent profile Materials/Rush Tickets projection and tactical match-end policy.
- [ ] Add deterministic tests for Credit/Materials grant, atomic spend, capacity, overflow rejection, and version behavior.
- [ ] Add an ownership contract test that fails if another tactical Credits or Materials authority is introduced.

Implementation rule:

If safe migration requires replacing `ResourceExchangeWalletComponent`, do it as one bounded slice: add canonical data, migrate all production consumers and tests, then remove/narrow the old fields. Startup may perform a one-time legacy projection before simulation begins; steady-state dual writes are prohibited.

Phase 1 exit criteria:

- Fabrication, Exchange, HUD, build placement, AI, and telemetry can all depend on one Materials value.
- No resource state is stored in UI views.
- Architecture and assembly validation passes.

## Phase 2: Config And Building Projection

- [ ] Change only the player-facing display name and description of the existing Ammunition Depot config.
- [ ] Preserve `Building_Ammunition_Depot` and serialized prefab references.
- [ ] Add authored Oil input capacity, Oil consumed per cycle, Materials output per cycle, cycle duration, Materials capacity policy, and enabled state.
- [ ] Prefer an existing generic building conversion config if it represents Oil-to-faction-output without special-case code.
- [ ] If required, add a focused `MaterialFabricationConfig`; keep it in `Game.Configs` and validate all fields.
- [ ] Project `MaterialFabricationComponent` and `MaterialFabricationInputTag` for map-placed and player-built depots through the same path.
- [ ] Project required data at creation time; do not add components during every tick.
- [ ] Add stable typed status and block-reason enums.
- [ ] Update AI/catalog-facing display role without changing compatibility ids.
- [ ] Add config/projection tests for missing, negative, zero, overflow-risk, map-placed, and player-built cases.

Phase 2 exit criteria:

- No conversion balance constant exists in runtime code.
- Existing maps and base layouts continue resolving the building.
- Invalid configs fail authoring/editor validation before play.

## Phase 3: Oil Destination Routing

- [ ] Extend the existing tray destination candidate path to include same-faction fabrication inputs.
- [ ] Reuse existing movement, cargo, pickup, unload, and reservation data; do not add a second truck movement system.
- [ ] Reserve source Oil and destination input capacity before assignment.
- [ ] Preserve valid active assignments until completion or an explicit invalidation.
- [ ] Score eligible demand deterministically using starvation/free capacity, route cost, and stable-id tie-breakers.
- [ ] Add reassignment hysteresis/cooldown so nearly equal refinery/depot scores cannot cause oscillation.
- [ ] Release reservations exactly once on death, destruction, route loss, cancellation, or destination invalidation.
- [ ] Publish typed idle/block reasons rather than formatted strings.
- [ ] Test one pump/one truck/one depot delivery.
- [ ] Test one pump/one truck/refinery/depot competition and deterministic destination selection.
- [ ] Test no oscillation, no double reservation, invalid route cleanup, destruction cleanup, and capacity changes.

Performance rules:

- Do not scan every building for every truck every frame.
- Reuse versioned candidate data and existing cached queries.
- Do not call `ToEntityArray` or `ToComponentDataArray` in recurring route assignment unless measured and approved by the hot-path contract.

Phase 3 exit criteria:

- Tray trucks deliver Oil to depots without manual commands.
- Refinery logistics still pass unchanged regression tests.
- A stable scenario produces no left/right target thrash or reservation leaks.

## Phase 4: Oil-To-Materials Conversion

- [ ] Implement `MaterialFabricationSystem` as Burst-compatible unmanaged `ISystem` unless profiling proves a documented managed boundary is unavoidable.
- [ ] Tick at the existing authored/fixed building production cadence.
- [ ] Consume Oil and grant integer Materials deterministically.
- [ ] Clamp or reject output before Materials capacity overflow.
- [ ] Stall with `NoOilInput`, `MaterialsCapacityFull`, `ProductionDisabled`, or applicable typed building state.
- [ ] Resume automatically when the blocking condition clears.
- [ ] Increment building and faction versions only when values/status actually change.
- [ ] Emit typed economy/telemetry events without string formatting in simulation.
- [ ] Test deterministic conversion across different render frame rates.
- [ ] Test empty input, partial input, full Materials capacity, disable/resume, ownership, destruction, and exact-once output.
- [ ] Warm and measure steady-state conversion at `0 B/frame` managed allocation.

Phase 4 exit criteria:

- Map-placed and player-built depots behave identically.
- Oil cannot go negative and Materials cannot exceed capacity.
- No frame-rate-dependent resource drift exists.
- No structural changes or recurring managed allocations occur in the conversion tick.

## Phase 5: Credits Plus Materials Construction

- [ ] Add authored Materials cost to the existing building definition/config path.
- [ ] Keep current Credits price and show both currencies.
- [ ] Stop trusting the UI-supplied `Price`; resolve authored Credits and Materials costs from the stable building id inside the authoritative request path.
- [ ] Add typed affordability evaluation using `FactionEconomy.Money` and `FactionTacticalMaterialsComponent`.
- [ ] Extend request/result data with an economy transaction/reservation id and typed `InsufficientCredits`, `InsufficientMaterials`, and combined-cost rejection behavior.
- [ ] Atomically reserve or spend Credits and Materials exactly once after geometry validation and before managed visual/runtime registration.
- [ ] Finalize the transaction on successful registration; issue an exact-once rollback result if registration fails. Cancelling a preview before confirmation spends nothing.
- [ ] Ensure map-authored structures do not spend tactical resources.
- [ ] Seed enough starting Materials or an authored recovery path in Materials-enabled scenarios.
- [ ] Add tests for affordable, insufficient Credits, insufficient Materials, both insufficient, preview cancel, invalid placement, failed registration rollback, and duplicate requests.
- [ ] Add regression tests for production/build flows that remain Credits-only by authored configuration.

Phase 5 exit criteria:

- Build placement never creates a structure after only one of two required costs is paid.
- Existing zero-Materials-cost definitions remain valid.
- No UI path can bypass authoritative affordability.

## Phase 6: HUD And Selected-Building UI

- [ ] Replace live Match `SupplyText = 92/120` with canonical Materials current/capacity projection.
- [ ] Preserve a non-gameplay placeholder only in isolated UI preview/test fixtures if explicitly required.
- [ ] Add a versioned header Materials read model in the existing UI shell ECS boundary.
- [ ] Avoid per-frame Materials string formatting when source version is unchanged.
- [ ] Extend Build Drawer cards/details to show Credits and Materials costs.
- [ ] Bind typed disabled reasons without calculating affordability in Canvas views.
- [ ] Add a versioned selected-depot read model with Oil input, rate, progress, output, faction Materials, and status.
- [ ] Add production enabled/disabled command only through typed ECS request/result data.
- [ ] Keep Resource Header tap routing to Exchange scenario-gated and input-safe.
- [ ] Validate 16:9 and 20:9 layouts, localization expansion, touch sizes, and no text clipping.
- [ ] Add no-GC unchanged-state read tests and UI contract/prefab tests.

Phase 6 exit criteria:

- No player-facing ammunition-storage claim remains.
- Header, Build Drawer, and selected depot agree on the same Materials value.
- UI views contain presentation only and do not own economy policy.

## Phase 7: Exchange And Balance Safety

- [ ] Reclassify `Credits -> Materials` as an expensive emergency recovery recipe in config and UI copy.
- [ ] Calculate local opportunity value from authored Oil value, conversion rate/time, depot investment, and logistics assumptions.
- [ ] Start import pricing at 1.5x to 2.0x modeled local effective cost.
- [ ] Keep Exchange queue duration, caps, and scenario gates active.
- [ ] Ensure Exchange capacity validation uses canonical tactical Materials.
- [ ] Add balance tests for `Materials -> Credits -> Materials` round-trip retention at or below 85%.
- [ ] Add balance tests proving `Oil -> Materials -> Credits` cannot create Credits profit.
- [ ] Add a simulation report comparing local production, repeated imports, mixed strategy, and destroyed-depot recovery.

Phase 7 exit criteria:

- Local fabrication is the dominant sustained strategy.
- Exchange remains useful for recovery.
- No authored recipe combination creates arbitrage.

## Phase 8: AI, Telemetry, And Scenario Safety

- [ ] Teach AI affordability/build plans to use canonical Materials and authored costs.
- [ ] Teach AI Oil allocation to consider Fuel pressure and construction plans without hidden resources.
- [ ] Gate AI Exchange imports by scenario and recovery need.
- [ ] Record Materials fabricated/imported/exported/rewarded/spent and depot blocked time by typed reason.
- [ ] Record tray assignments/reassignments/failures and refinery-versus-depot Oil delivery.
- [ ] Add scenario validation requiring starting Materials, a viable fabrication chain, rebuildability, or enabled Exchange recovery.
- [ ] Add deterministic AI and deadlock validation scenarios.

Phase 8 exit criteria:

- AI follows the same costs and ownership as the player.
- Balance reports can explain Oil allocation and Materials scarcity.
- Required construction cannot silently become impossible.

## Phase 9: Integration, Performance, And Closeout

- [ ] Run `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`.
- [ ] Run `EcsBurstHotPathArchitectureTests.RunFocusedValidation` and classify any intentional managed boundary.
- [ ] Run focused component/config/conversion/logistics/build/UI/Exchange tests.
- [ ] Re-run existing automated fuel logistics, Resource Exchange, building placement, Match HUD, AI build-plan, and Match start tests.
- [ ] Capture before/after profiler evidence for the same seeded scenario and device/editor configuration.
- [ ] Verify `0 B/frame` managed allocation after warmup for fabrication, unchanged HUD/read-model reads, and stable tray routing.
- [ ] Verify no greater than 5% p95 regression in affected accepted steady-state markers unless a stricter existing budget applies.
- [ ] Run Android target-device validation for touch UI, sustained frame time, memory, and thermal behavior if the feature ships in the mobile build.
- [ ] Reconcile all connected design docs, update this tracker to complete, and attach final evidence paths.

Phase 9 exit criteria:

- All high-level acceptance criteria pass in a playable Match.
- Architecture, naming, assembly, Burst, GC, and performance contracts pass.
- Evidence is reproducible and no known blocker is hidden in narrative notes.

## Required Test Matrix

| Area | Minimum evidence |
|---|---|
| Data ownership | One tactical Materials authority; no stale Exchange mirror; deterministic versions. |
| Config | Valid and invalid conversion/capacity/cost assets; compatibility id preserved. |
| Logistics | Pump-to-depot delivery, refinery competition, reservations, destruction, no oscillation. |
| Conversion | Rate, capacity, disabled states, exact-once grant, render-frame independence. |
| Construction | Dual-cost affordability, exact-once spend, cancel/refund, map-authored bypass. |
| Exchange | Expensive import, capacity, queue, cancel/rush regression, no arbitrage. |
| UI | Live Materials header, depot panel, Build Drawer costs, typed reasons, safe layouts. |
| AI | Same wallet/costs, Oil allocation, recovery route, deterministic decisions. |
| Architecture | Assembly direction, naming ratchets, no singleton/service/controller drift. |
| Performance | Baseline comparison, hot-path markers, 0 B/frame steady-state managed allocation. |
| Mobile | Target-device frame time, touch/readability, memory, and thermal evidence before release. |

## Stop Conditions

Pause implementation and record a blocker rather than improvising if:

- the authoritative Credits owner cannot be identified;
- Exchange and active Match Materials cannot be migrated without dual authority;
- existing placement code cannot guarantee atomic dual-resource spending;
- tray routing lacks a reservation owner that can safely support another destination type;
- a proposed assembly reference points from runtime into UI/authoring/editor code;
- a hot path requires recurring managed allocation without profiler-backed justification;
- a scenario would require Materials but provides no recovery path.

## Implementation Evidence Log

For every completed batch append:

- date and phase;
- exact files changed;
- architecture and assembly impact;
- behavior changed;
- test commands and pass/fail summary;
- profiler/GC evidence when applicable;
- screenshots or captures for visible UI changes;
- remaining work and next action;
- any blocker or approved exception.

### 2026-07-12 - Planning And Initial Inventory

- Added the high-level Field Fabrication and Materials design and this implementation tracker.
- Locked player-facing identity, V1 compatibility id, local-production-first economy, canonical Materials requirement, assembly map, naming, no-GC rules, and implementation gates.
- Confirmed that runtime implementation has not started.
- At this point, the remaining Phase 0 work was the profiler/GC baseline; it is completed in the next log entry.

### 2026-07-12 - Phase 0 Compile, GC, And Performance Baseline

- Source commit: `d42e515e4`.
- The live clone was open in Unity, so validation used an isolated APFS copy at `/private/tmp/wlc-field-fabrication-baseline-d42e515e4`; no live Editor process was closed and no runtime source was changed.
- Sequential compile validation passed with zero errors for `Game.Components`, `Game.Runtime`, `Game.UI.Shell.Ecs`, and `Game.Tests.Editor`. Unity-generated projects reported their existing warnings only.
- Fuel/logistics baseline passed `BuildingResourceProductionEcsSystemTests.RunFocusedValidation`: 38 tests, including `AutomaticFuelLogisticsSteadyState_DoesNotAllocateManagedMemory` at 0 managed bytes. Log: `/private/tmp/wlc-field-fabrication-baseline-fuel.log`.
- Placement baseline passed `BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation`: 16 tests, including warm command-cache allocation checks. Log: `/private/tmp/wlc-field-fabrication-baseline-placement.log`.
- HUD baseline passed all 8 `UiShellEcsGatewayResourceHeaderTests`, including `MatchHudHeader_CachedVersionedUsableFuelSummaryReadDoesNotAllocate` at 0 managed bytes. Results: `/private/tmp/wlc-field-fabrication-baseline-hud-results.xml`; log: `/private/tmp/wlc-field-fabrication-baseline-hud.log`.
- Exchange GC baseline passed 2 tests: queue/validation steady state allocated 0 bytes over 512 measured frames after 64 warmup frames; multi-entity `ResourceExchangeRequestValidationSystem` updates also allocated 0 bytes over 512 measured updates. Log: `/private/tmp/wlc-field-fabrication-baseline-exchange-gc.log`.
- Exchange active steady-state baseline passed over 240 measured frames after 64 warmup frames: average 0.020 ms, p95 0.019 ms, p99 0.025 ms, max 0.050 ms, and 0 managed bytes. Report: `/private/tmp/warlinecapture-resource-exchange-steady-state-performance.json`; log: `/private/tmp/wlc-field-fabrication-baseline-exchange-performance.log`.
- Phase 0 is complete. Phase 1 may begin from these exact before-state gates.

### 2026-07-12 - Phase 1A Canonical Materials Data And Startup Projection

- Added `FactionTacticalMaterialsComponent` and typed source, spend, and mutation-result enums in `Game.Components`. The component owns current/capacity, fabricated/imported/rewarded/exported/spent lifetime counters, faction id, and version.
- Added `FactionTacticalMaterialsUtilitySystemHelper` in `Game.Runtime` as a pure stateless math boundary with the architecture-approved `UtilitySystemHelper` suffix. It performs exact capacity/affordability mutation, saturating lifetime counters, and version wrap without managed allocation.
- Added authored `initialMaterials` and `materialsCapacity` fields to `InitialUnitsSpawnerAuthoringConfig` and `CustomGameMapConfig`, projected them through `InitialUnitsSpawnConfig`, and seeded the companion component on every participating `FactionEconomy` entity. Only the player receives the configured starting amount; all factions receive the authored capacity.
- Extended `FactionEconomyStartupSystem` to project a zeroed companion component when an AI/player-auto economy is created before initial-unit resource seeding.
- The first projection implementation used `ToEntityArray`; `EcsBurstHotPathArchitectureTests` rejected the new snapshot debt. It was replaced with chunk iteration plus a temporary native list of compact faction/entity seed records, and the ratchet returned to baseline.
- Added 7 focused mutation tests, including capacity rejection, insufficient spend, invalid state, saturated telemetry, version wrap, and 512 grant/spend iterations at 0 managed bytes.
- Validation passed in the isolated Unity project: `FactionTacticalMaterialsFocusedValidation` 7 tests, `InitialUnitsSpawnFocusedValidation` 12 tests, `FactionEconomyStartupValidation` 3 tests, `ScriptArchitectureBoundaryValidation` 31 tests, and `EcsBurstHotPathArchitectureValidation` 10 tests.
- Unity-regenerated project builds passed with zero errors for `Game.Components`, `Game.Configs`, `Game.Authoring`, `Game.Runtime`, and `Game.Tests.Editor`. The checked-in workspace `.csproj` files remained untouched because the live Editor owns their generation.
- Logs: `/private/tmp/wlc-field-fabrication-materials-data-validation.log`, `/private/tmp/wlc-field-fabrication-materials-startup-validation.log`, `/private/tmp/wlc-field-fabrication-faction-economy-startup-validation.log`, `/private/tmp/wlc-field-fabrication-architecture-validation.log`, and `/private/tmp/wlc-field-fabrication-burst-validation.log`.
- New files are in existing explicit assemblies only: component data in `Game.Components`, pure mutation math in `Game.Runtime`, and validation in `Game.Tests.Editor`. No runtime default-assembly fallback or new assembly reference was added.

### 2026-07-12 - Phase 1B Player Credits ECS Ownership

- Removed `_dollars` ownership from `RuntimeResourceUtilitySystemHelper`. The helper now stores only an authored pending startup seed until composition configures an `EntityManager`; all live reads, grants, citizen spending, build affordability, production spending, placement confirmation spending, and refunds mutate the cached player `FactionEconomy.Money` value.
- `BuildingGameplayStartupCompositionSystemHelper` configures the helper once through the existing `BuildingEntityManagerAccessSystem`. No new bootstrap, manager, service, global wallet, or per-frame query was added.
- Player economy resolution uses archetype-chunk iteration during configuration, caches the resolved entity, and projects missing `FactionEconomyPolicy` / `FactionTacticalMaterialsComponent` companions. Warm reads and mutations use direct component access only.
- Added 5 focused tests for player economy creation, existing-entity reuse, exact spend/add behavior, citizen-context ownership, and 512 spend/add cycles at 0 managed bytes.
- Validation passed in the isolated Unity project: `RuntimeResourceUtilityFocusedValidation` 5 tests, `BuildingProductionRequestValidation` 26 tests, `BuildingPlacementCommandRequestValidation` 16 tests, `ScriptArchitectureBoundaryValidation` 31 tests, and `EcsBurstHotPathArchitectureValidation` 10 tests.
- Unity-regenerated `Game.Runtime` and `Game.Tests.Editor` builds passed with zero errors. Existing warnings remain unchanged.
- Logs: `/private/tmp/wlc-field-fabrication-player-credits-validation.log`, `/private/tmp/wlc-field-fabrication-building-production-regression.log`, `/private/tmp/wlc-field-fabrication-building-placement-regression.log`, `/private/tmp/wlc-field-fabrication-player-credits-architecture.log`, and `/private/tmp/wlc-field-fabrication-player-credits-burst.log`.
