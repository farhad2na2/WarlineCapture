# Field Fabrication And Materials Implementation Tracker

Date: 2026-07-12
Status: In progress - Phase 8 AI, telemetry, and scenario safety
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

Overall implementation progress: 97% (100/103 checklist items complete).

Planning, canonical resource ownership, depot config/projection, automated Oil routing, Oil-to-Materials conversion, authored construction costs, atomic dual-resource placement, HUD/depot controls, Exchange balance safety, canonical AI construction affordability, demand-aware AI Oil allocation, explicitly gated AI Materials recovery, faction-owned Materials/fabrication telemetry, faction-owned tray logistics telemetry, and fail-closed deterministic scenario recovery validation are complete. Active depots consume only unreserved physical Oil at the existing one-second cadence. The shipping Match now projects an explicitly gated Resource Exchange onto the canonical player faction entity; emergency Materials import costs 1.71x modeled local production, queue/capacity rules remain active, and tested conversion loops cannot create profit. Phase 9 behavior, architecture, Burst, Match-start, HUD, placement, and zero-allocation gates pass. Relative p95 and Android target-device evidence remain open and are recorded as explicit blockers below.

Progress is checklist-based. Every `- [ ]` or `- [x]` implementation/validation row counts. Update the numerator, denominator, table, status, and evidence log in the same commit as each completed batch.

| Phase | Status | Complete | Total | Progress | Gate |
|---|---|---:|---:|---:|---|
| 0. Inventory and baseline | Complete | 12 | 12 | 100% | Ownership, file targets, compile state, focused behavior, p95/p99, and managed-allocation baselines are recorded. |
| 1. Canonical tactical Materials and Credits | Complete | 13 | 13 | 100% | Canonical Materials/Credits ownership, combined allocation-free construction transaction, HUD projection, Exchange ownership, and deterministic mutation coverage pass. |
| 2. Config and building projection | Complete | 10 | 10 | 100% | Depot identity, authored balance, typed data, compatibility id, and map/runtime projection are validated. |
| 3. Oil destination routing | Complete | 11 | 11 | 100% | Tray delivery, demand scoring, stable assignment, reservations, cleanup, and refinery regressions pass. |
| 4. Oil-to-Materials conversion | Complete | 11 | 11 | 100% | Conversion is deterministic, capacity-safe, typed, reservation-aware, and no-GC. |
| 5. Credits + Materials construction | Complete | 11 | 11 | 100% | Atomic placement, rollback, authored cost projection, and Build Drawer dual-cost presentation pass. |
| 6. HUD and selected-building UI | Complete | 11 | 11 | 100% | Live canonical header, typed Build Drawer affordability, selected-depot status/control, fail-closed Exchange routing, and responsive validation pass. |
| 7. Exchange and balance safety | Complete | 8 | 8 | 100% | Shipping startup projection, emergency import markup, queue/capacity gates, anti-arbitrage tests, and strategy report pass. |
| 8. AI, telemetry, and scenario safety | Complete | 7 | 7 | 100% | AI shares rules; invalid Materials scenarios fail startup with typed evidence. |
| 9. Integration, performance, and closeout | Active | 6 | 9 | 67% | Behavior, architecture, Burst, GC, HUD, and Match start pass; p95 and Android evidence remain open. |

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
- [x] Migrate Exchange Credits and Materials import/export to `FactionEconomy.Money` plus `FactionTacticalMaterialsComponent`.
- [x] Migrate Match HUD and build affordability to read this canonical component.
- [x] Remove `ResourceExchangeWalletComponent.Credits` and `.Materials` authority without a steady-state dual-write period.
- [x] Remove or narrow Exchange wallet Oil/Fuel mirrors so physical storage/summaries remain authoritative.
- [x] Document persistent profile Materials/Rush Tickets projection and tactical match-end policy.
- [x] Add deterministic tests for Credit/Materials grant, atomic spend, capacity, overflow rejection, and version behavior.
- [x] Add an ownership contract test that fails if another tactical Credits or Materials authority is introduced.

Implementation rule:

If safe migration requires replacing `ResourceExchangeWalletComponent`, do it as one bounded slice: add canonical data, migrate all production consumers and tests, then remove/narrow the old fields. Startup may perform a one-time legacy projection before simulation begins; steady-state dual writes are prohibited.

Phase 1 exit criteria:

- Fabrication, Exchange, HUD, build placement, AI, and telemetry can all depend on one Materials value.
- No resource state is stored in UI views.
- Architecture and assembly validation passes.

## Phase 2: Config And Building Projection

- [x] Change only the player-facing display name and description of the existing Ammunition Depot config.
- [x] Preserve `Building_Ammunition_Depot` and serialized prefab references.
- [x] Add authored Oil input capacity, Oil consumed per cycle, Materials output per cycle, cycle duration, Materials capacity policy, and enabled state.
- [x] Prefer an existing generic building conversion config if it represents Oil-to-faction-output without special-case code.
- [x] Confirm a focused `MaterialFabricationConfig` is not required; keep fabrication fields in the existing `Game.Configs` building definition and validate all fields.
- [x] Project `MaterialFabricationComponent` and `MaterialFabricationInputTag` for map-placed and player-built depots through the same path.
- [x] Project required data at creation time; do not add components during every tick.
- [x] Add stable typed status and block-reason enums.
- [x] Update AI/catalog-facing display role without changing compatibility ids.
- [x] Add config/projection tests for missing, negative, zero, overflow-risk, map-placed, and player-built cases.

Phase 2 exit criteria:

- No conversion balance constant exists in runtime code.
- Existing maps and base layouts continue resolving the building.
- Invalid configs fail authoring/editor validation before play.

## Phase 3: Oil Destination Routing

- [x] Extend the existing tray destination candidate path to include same-faction fabrication inputs.
- [x] Reuse existing movement, cargo, pickup, unload, and reservation data; do not add a second truck movement system.
- [x] Reserve source Oil and destination input capacity before assignment.
- [x] Preserve valid active assignments until completion or an explicit invalidation.
- [x] Score eligible demand deterministically using starvation/free capacity, route cost, and stable-id tie-breakers.
- [x] Add reassignment hysteresis/cooldown so nearly equal refinery/depot scores cannot cause oscillation.
- [x] Release reservations exactly once on death, destruction, route loss, cancellation, or destination invalidation.
- [x] Publish typed idle/block reasons rather than formatted strings.
- [x] Test one pump/one truck/one depot delivery.
- [x] Test one pump/one truck/refinery/depot competition and deterministic destination selection.
- [x] Test no oscillation, no double reservation, invalid route cleanup, destruction cleanup, and capacity changes.

Performance rules:

- Do not scan every building for every truck every frame.
- Reuse versioned candidate data and existing cached queries.
- Do not call `ToEntityArray` or `ToComponentDataArray` in recurring route assignment unless measured and approved by the hot-path contract.

Phase 3 exit criteria:

- Tray trucks deliver Oil to depots without manual commands.
- Refinery logistics still pass unchanged regression tests.
- A stable scenario produces no left/right target thrash or reservation leaks.

## Phase 4: Oil-To-Materials Conversion

- [x] Implement `MaterialFabricationSystem` as Burst-compatible unmanaged `ISystem` unless profiling proves a documented managed boundary is unavoidable.
- [x] Tick at the existing authored/fixed building production cadence.
- [x] Consume Oil and grant integer Materials deterministically.
- [x] Clamp or reject output before Materials capacity overflow.
- [x] Stall with `NoOilInput`, `MaterialsCapacityFull`, `ProductionDisabled`, or applicable typed building state.
- [x] Resume automatically when the blocking condition clears.
- [x] Increment building and faction versions only when values/status actually change.
- [x] Emit typed economy/telemetry events without string formatting in simulation.
- [x] Test deterministic conversion across different render frame rates.
- [x] Test empty input, partial input, full Materials capacity, disable/resume, ownership, destruction, and exact-once output.
- [x] Warm and measure steady-state conversion at `0 B/frame` managed allocation.

Phase 4 exit criteria:

- Map-placed and player-built depots behave identically.
- Oil cannot go negative and Materials cannot exceed capacity.
- No frame-rate-dependent resource drift exists.
- No structural changes or recurring managed allocations occur in the conversion tick.

## Phase 5: Credits Plus Materials Construction

- [x] Add authored Materials cost to the existing building definition/config path.
- [x] Keep current Credits price and show both currencies.
- [x] Stop trusting the UI-supplied `Price`; resolve authored Credits and Materials costs from the stable building id inside the authoritative request path.
- [x] Add typed affordability evaluation using `FactionEconomy.Money` and `FactionTacticalMaterialsComponent`.
- [x] Extend request/result data with an economy transaction/reservation id and typed `InsufficientCredits`, `InsufficientMaterials`, and combined-cost rejection behavior.
- [x] Atomically reserve or spend Credits and Materials exactly once after geometry validation and before managed visual/runtime registration.
- [x] Finalize the transaction on successful registration; issue an exact-once rollback result if registration fails. Cancelling a preview before confirmation spends nothing.
- [x] Ensure map-authored structures do not spend tactical resources.
- [x] Seed enough starting Materials or an authored recovery path in Materials-enabled scenarios.
- [x] Add tests for affordable, insufficient Credits, insufficient Materials, both insufficient, preview cancel, invalid placement, failed registration rollback, and duplicate requests.
- [x] Add regression tests for production/build flows that remain Credits-only by authored configuration.

Phase 5 exit criteria:

- Build placement never creates a structure after only one of two required costs is paid.
- Existing zero-Materials-cost definitions remain valid.
- No UI path can bypass authoritative affordability.

## Phase 6: HUD And Selected-Building UI

- [x] Replace live Match `SupplyText = 92/120` with canonical Materials current/capacity projection.
- [x] Preserve a non-gameplay placeholder only in isolated UI preview/test fixtures if explicitly required.
- [x] Add a versioned header Materials read model in the existing UI shell ECS boundary.
- [x] Avoid per-frame Materials string formatting when source version is unchanged.
- [x] Extend Build Drawer cards/details to show Credits and Materials costs.
- [x] Bind typed disabled reasons without calculating affordability in Canvas views.
- [x] Add a versioned selected-depot read model with Oil input, rate, progress, output, faction Materials, and status.
- [x] Add production enabled/disabled command only through typed ECS request/result data.
- [x] Keep Resource Header tap routing to Exchange scenario-gated and input-safe.
- [x] Validate 16:9 and 20:9 layouts, localization expansion, touch sizes, and no text clipping.
- [x] Add no-GC unchanged-state read tests and UI contract/prefab tests.

Phase 6 exit criteria:

- No player-facing ammunition-storage claim remains.
- Header, Build Drawer, and selected depot agree on the same Materials value.
- UI views contain presentation only and do not own economy policy.

## Phase 7: Exchange And Balance Safety

- [x] Reclassify `Credits -> Materials` as an expensive emergency recovery recipe in config and UI copy.
- [x] Calculate local opportunity value from authored Oil value, conversion rate/time, depot investment, and logistics assumptions.
- [x] Start import pricing at 1.5x to 2.0x modeled local effective cost.
- [x] Keep Exchange queue duration, caps, and scenario gates active.
- [x] Ensure Exchange capacity validation uses canonical tactical Materials.
- [x] Add balance tests for `Materials -> Credits -> Materials` round-trip retention at or below 85%.
- [x] Add balance tests proving `Oil -> Materials -> Credits` cannot create Credits profit.
- [x] Add a simulation report comparing local production, repeated imports, mixed strategy, and destroyed-depot recovery.

Phase 7 exit criteria:

- Local fabrication is the dominant sustained strategy.
- Exchange remains useful for recovery.
- No authored recipe combination creates arbitrage.

## Phase 8: AI, Telemetry, And Scenario Safety

- [x] Teach AI affordability/build plans to use canonical Materials and authored costs.
- [x] Teach AI Oil allocation to consider Fuel pressure and construction plans without hidden resources.
- [x] Gate AI Exchange imports by scenario and recovery need.
- [x] Record Materials fabricated/imported/exported/rewarded/spent and depot blocked time by typed reason.
- [x] Record tray assignments/reassignments/failures and refinery-versus-depot Oil delivery.
- [x] Add scenario validation requiring starting Materials, a viable fabrication chain, rebuildability, or enabled Exchange recovery.
- [x] Add deterministic AI and deadlock validation scenarios.

Phase 8 exit criteria:

- AI follows the same costs and ownership as the player.
- Balance reports can explain Oil allocation and Materials scarcity.
- Required construction cannot silently become impossible.

## Phase 9: Integration, Performance, And Closeout

- [x] Run `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`.
- [x] Run `EcsBurstHotPathArchitectureTests.RunFocusedValidation` and classify any intentional managed boundary.
- [x] Run focused component/config/conversion/logistics/build/UI/Exchange tests.
- [x] Re-run existing automated fuel logistics, Resource Exchange, building placement, Match HUD, AI build-plan, and Match start tests.
- [x] Capture before/after profiler evidence for the same seeded scenario and device/editor configuration.
- [x] Verify `0 B/frame` managed allocation after warmup for fabrication, unchanged HUD/read-model reads, and stable tray routing.
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

### 2026-07-13 - Phase 1C Resource Exchange Canonical Ownership

- Removed `Credits`, `Materials`, and `MaterialsCapacity` from `ResourceExchangeWalletComponent`. The narrowed wallet now retains only operational Oil, Fuel, Rush Tickets, Oil/Fuel capacities, faction id, and version pending the separate physical Oil/Fuel ownership slice.
- Migrated request validation, cancellation/mission-end refunds, rush completion, queue completion, storage validation, and the Exchange UI read model to `FactionEconomy.Money` and `FactionTacticalMaterialsComponent`.
- Added `ResourceExchangeResourceUtilitySystemHelper` in `Game.Runtime` for allocation-free typed reads, spends, imports, and reserved-input refunds. Materials exports update exported/spent counters, imports update imported counters, and cancellation reverses only the refunded reservation.
- Kept the generated ECS queries within the package's seven-element limit by querying both canonical components directly and resolving ancillary dynamic buffers by entity. No snapshots, LINQ, structural tick changes, managed collections, new update loop, or assembly reference was added.
- Migrated focused EditMode and PlayMode fixtures to project canonical faction components. Added canonical Credits/Materials ownership, capacity, accounting, and 512-iteration no-allocation tests.
- Isolated Unity compile passed with zero errors. Core Exchange simulation, UI projection, architecture, Materials accounting, GC, and performance validation passed 44/44 tests.
- Active Exchange steady-state after the final overflow-safe storage check: average 0.018 ms, p95 0.017 ms, p99 0.019 ms, max 0.021 ms, and 0 managed bytes over 240 measured frames. This is below the Phase 0 p95 baseline of 0.019 ms.
- The focused PlayMode export flow passed 1/1 and confirmed exact-once completion credits the faction economy. `EcsBurstHotPathArchitectureTests` passed 10/10. The broader script architecture suite passed 50/58; its eight failures are existing unrelated project debt in rendering/UI/camera/static-registry ratchets, and none names a file in this slice. Resource Exchange's focused architecture guardrail passed in the 44-test core batch.
- Evidence: `/private/tmp/wlc-field-exchange-compile-2.log`, `/private/tmp/wlc-field-exchange-core-tests-final.xml`, `/private/tmp/wlc-field-exchange-core-tests-final.log`, `/private/tmp/wlc-field-exchange-playmode.xml`, `/private/tmp/wlc-field-exchange-playmode.log`, `/private/tmp/wlc-field-exchange-architecture.xml`, and `/private/tmp/warlinecapture-resource-exchange-steady-state-performance.json`.
- Next action: migrate live Match HUD/build affordability to canonical Materials, then address physical Exchange Oil/Fuel narrowing as its own ownership slice.

### 2026-07-13 - Phase 1D Lifetime Policy, Match Header, And Ownership Ratchet

- Locked the V1 lifetime policy in `Design/Field_Fabrication_Materials_Design.md`: persistent profile Materials and Rush Tickets remain account resources; tactical Materials are scenario-seeded and discarded at match end; mission rewards settle through the typed reward/save path; future profile-funded deployment requires explicit launch reservation and exact-once settlement contracts.
- Added `UiMatchHudResourceReadModelSystem` to `Game.UI.Shell.Ecs`. It selects the player `FactionEconomy` plus `FactionTacticalMaterialsComponent`, writes grouped Credits and Materials current/capacity into the existing shell boundary, and increments `UiMatchHudHeaderComponent.ResourceVersion` only when the boundary or canonical source values change.
- Removed runtime `187,540` Credits and `92/120` Supply seeds from the UI contract fallback, shell startup, and gateway fallback. Explicit test/preview fixtures may retain authored sample values.
- The projection is a Burst-compiled unmanaged `ISystem` using fixed strings, no LINQ, no snapshots, no structural tick changes, no new managed shell, and no new MonoBehaviour update loop. The existing `Game.UI.Shell.Ecs -> Game.Components` project-assembly direction is unchanged; `Game.UI.Shell.Ecs` adds only the package-level `Unity.Burst` reference required to compile the new hot path.
- Added `TacticalResourceOwnershipArchitectureTests`. It rejects `_dollars`, a second ECS Credits/Money field, a direct ECS Materials currency field, or another faction Materials component owner.
- Unity EditMode validation passed `UiMatchHudResourceReadModelSystemTests` 2/2, including player-versus-AI selection, grouped formatting, version changes, and 512 unchanged updates at 0 managed bytes. The first architecture run rejected the system as unclassified non-Burst work; the final implementation Burst-compiles `OnUpdate` while keeping query construction in `OnCreate` outside Burst. Final results: `/private/tmp/wlc-field-hud-burst-corrected-results.xml`; log: `/private/tmp/wlc-field-hud-burst-corrected.log`.
- The ownership architecture ratchet passed 1/1. Results: `/private/tmp/wlc-field-ownership-results.xml`; log: `/private/tmp/wlc-field-ownership.log`.
- Existing `UiShellEcsGatewayResourceHeaderTests` passed 8/8. Results: `/private/tmp/wlc-field-hud-regression-results.xml`; log: `/private/tmp/wlc-field-hud-regression.log`.
- `EcsBurstHotPathArchitectureTests` passed 10/10 with 60 Burst OnUpdate files, 94/94 classified non-Burst systems, and zero unclassified systems. Results: `/private/tmp/wlc-field-hud-burst-corrected-architecture-results.xml`; log: `/private/tmp/wlc-field-hud-burst-corrected-architecture.log`.
- Unity compilation and generated assembly builds for `Game.UI.Shell.Ecs` and `Game.Tests.Editor` completed with zero errors. `git diff --check` passed. Existing generated-project reference-version warnings remain unchanged.
- Next action: narrow the Exchange Oil/Fuel mirror against physical storage/summaries, then implement authored Materials build costs and the combined Credits/Materials transaction in Phase 5.

### 2026-07-13 - Phase 1E Physical Exchange Oil/Fuel Transactions And Ownership Closure

- Added `ResourceExchangePhysicalReservationComponent` in `Game.Components` as an eight-line internal-capacity buffer. Each line identifies the queue item, authoritative `BuildingResourceStorageComponent` entity, Oil/Fuel kind, input/output reservation role, and amount; it is a transaction ledger, not another resource owner.
- Extended `BuildingResourceStorageTransferSystemHelper` with exact reserved-input consumption and reserved-output delivery mutations. Both paths reject missing reservation, insufficient stock, or capacity overflow and increment the existing storage version only on mutation.
- Added `ResourceExchangePhysicalStorageUtilitySystemHelper` in `Game.Runtime`. Start requests deterministically sort eligible same-faction storage by runtime building id and entity, reserve physical input plus output capacity atomically, and roll back all lines on failure. Completion consumes reserved input and delivers reserved output; cancellation either releases or consumes input according to the existing refund window and always releases output capacity.
- Wired request start, cancel, mission end, normal completion, blocked retry, rush completion, and rush-all completion through the physical transaction whenever the Exchange entity has the reservation buffer. Legacy static overloads now reject Oil/Fuel recipes with typed `StorageMissing` instead of mutating or granting a nonphysical fallback.
- The event-driven start path uses temporary native chunk/candidate data only when a request is processed. The recurring active-queue validation iterates the internal buffer and performs direct component reads with no snapshot or managed collection. No new ECS system, managed shell, MonoBehaviour update, assembly reference, or runtime default-assembly fallback was added.
- Added six focused tests for deterministic multi-storage ordering, atomic rollback when output storage is missing, refundable/non-refundable cancellation, end-to-end request plus completion against physical storage, legacy-wallet non-mutation, and 512 steady-state completion validations at 0 managed bytes.
- Validation passed in the isolated Unity project: physical transaction tests 6/6; existing request/queue/rush/GC Exchange regressions 22/22; Resource Exchange architecture plus ECS Burst guardrails 16/16. `Game.Runtime` and `Game.Tests.Editor` generated-project builds passed with zero errors; existing Unity reference-version warnings remain unchanged.
- Evidence: `/private/tmp/wlc-field-fabrication-physical-storage-results.xml`, `/private/tmp/wlc-field-fabrication-physical-storage.log`, `/private/tmp/wlc-field-fabrication-exchange-regression-results.xml`, `/private/tmp/wlc-field-fabrication-exchange-regression.log`, `/private/tmp/wlc-field-fabrication-physical-architecture-results.xml`, and `/private/tmp/wlc-field-fabrication-physical-architecture.log`.
- Removed `ResourceExchangeWalletComponent.Oil`, `.Fuel`, `.OilCapacity`, and `.FuelCapacity`. `UiResourceExchangeReadModelSystem` now reads the player `BuildingRuntimeFactionUsableFuelSummary`; whole-barrel UI amounts floor fractional physical stock so displayed affordability cannot exceed reservable stock.
- Migrated request, queue, rush, event, presentation, GC, performance, and PlayMode fixtures to authoritative `BuildingResourceStorageComponent` setup and reservation behavior. Added a test-only `ResourceExchangePhysicalStorageTestHelper`; no runtime assembly or dependency changed.
- Added `ResourceExchangeWalletDoesNotMirrorPhysicalOilOrFuel` to the focused architecture guardrail and explicit legacy-overload rejection coverage.
- Final isolated Unity validation passed 62/62 Exchange, ownership, Burst, GC, UI-read-model, and performance EditMode tests. The active Exchange path measured 0 managed bytes over 240 frames, average 0.029 ms, p95 0.027 ms, p99 0.032 ms, and max 0.039 ms. Queue/request GC tests remained 0 bytes over 512 measured frames. The focused PlayMode export flow passed 1/1 and verified immediate physical reservation, settlement consumption, and exact-once Credits output.
- Evidence: `/private/tmp/wlc-field-fabrication-wallet-regression-rerun-results.xml`, `/private/tmp/wlc-field-fabrication-wallet-regression-rerun.log`, `/private/tmp/wlc-field-fabrication-wallet-playmode-rerun-results.xml`, `/private/tmp/wlc-field-fabrication-wallet-playmode-rerun.log`, and `/private/tmp/warlinecapture-resource-exchange-steady-state-performance.json`.
- Checklist count is 27/103. Phase 1 is 11/13; canonical build-affordability projection and combined deterministic Credits/Materials transaction tests remain.
- Next action: inspect the existing build affordability read model and placement transaction owner, then close the two remaining Phase 1 items without introducing UI-owned policy or dual resource writes.

### 2026-07-13 - Phase 1F Atomic Construction Resource Boundary And Phase Exit

- Added `FactionConstructionResourceMutationResult` in `Game.Components` and `FactionConstructionResourceUtilitySystemHelper` in `Game.Runtime`. The pure stateless helper evaluates typed insufficient-Credits, insufficient-Materials, and combined failures, then applies both struct mutations only after complete validation.
- Routed the existing player Credits spend through this canonical combined transaction with a zero Materials cost for current legacy building definitions. `RuntimeResourceUtilitySystemHelper` now exposes the same player entity's canonical Materials and a typed combined spend method for Phase 5 wiring; no second wallet, reservation owner, UI policy, or steady-state dual write was introduced.
- Existing zero-Materials-cost behavior preserves the Materials amount, counters, and version. Positive Materials construction spend updates `Current`, `LifetimeSpent`, and `Version` exactly once. Rejected transactions mutate neither `FactionEconomy.Money` nor `FactionTacticalMaterialsComponent`.
- Added six deterministic helper tests for typed affordability, atomic success, atomic rejection, zero-cost compatibility, faction mismatch, and 512 measured spends at 0 managed bytes. Extended the runtime resource fixture with an ECS persistence and combined rejection test.
- Isolated Unity validation passed: atomic helper 6/6, runtime player resource boundary 6/6, building placement regression 16/16, tactical ownership architecture 1/1, and ECS Burst hot-path architecture 10/10. Existing Materials grant/spend tests already cover capacity overflow, counter saturation, invalid state, and version wrap.
- Evidence: `/private/tmp/wlc-field-fabrication-construction-resource.log`, `/private/tmp/wlc-field-fabrication-runtime-resource-rerun.log`, `/private/tmp/wlc-field-fabrication-placement-rerun.log`, `/private/tmp/wlc-field-fabrication-phase1-ownership-rerun.log`, and `/private/tmp/wlc-field-fabrication-phase1-burst-rerun.log`.
- Phase 1 is complete at 13/13. Checklist count is 29/103. Next action: begin Phase 2 by locating the exact `Building_Ammunition_Depot` config and tracing map-placed/player-built building projection through the existing generic storage/production data path.

### 2026-07-13 - Phase 2 Depot Config And Building Projection

- Changed only the existing Ammunition Depot config's player-facing identity to `Field Fabrication Depot` and the approved Oil-to-Materials description. The prefab name, `Building_Ammunition_Depot` lookup id, serialized prefab reference, price, durability, portraits, and existing scene/AI/custom-game references remain unchanged.
- Reused `BuildingDefinitionAuthoringConfig.oilStorageCapacity` and `BuildingResourceStorageComponent` as the authoritative physical Oil input capacity. Added focused building-definition fields for fabrication enabled state, Oil consumed per cycle, integer Materials output, cycle duration, and full-cycle capacity policy; no second config asset or Oil storage owner was introduced.
- Added `MaterialFabricationComponent`, `MaterialFabricationInputTag`, `MaterialFabricationStatusCode`, `MaterialFabricationBlockReasonCode`, and `MaterialFabricationOutputCapacityPolicyCode` in `Game.Components`.
- Extended the existing config -> hidden authoring -> composition metadata -> runtime definition -> cloned definition path. `BuildingRuntimeEntityCompositionSystemHelper.CreateBuildingCombatEntity` is the single projection point for both map-placed and player-built depots and adds all required components at creation time.
- Authored initial tuning is 24 Oil input capacity, 4 Oil consumed per 30-second cycle, and 20 Materials output per cycle. These values remain config data and are not hard-coded in simulation.
- Added typed config validation for missing Oil capacity, invalid/negative Oil consumption, invalid Materials output, invalid cycle duration, and unsupported capacity policy. Invalid runtime definitions do not receive fabrication components; `int.MaxValue` output metadata projects without arithmetic so later conversion must capacity-check before mutation.
- Isolated Unity validation passed: config/compatibility/metadata 3/3, full resource production and logistics regression 40/40, script assembly boundaries 31/31, and ECS Burst hot-path architecture 10/10.
- Evidence: `/private/tmp/wlc-field-fabrication-phase2-config.log`, `/private/tmp/wlc-field-fabrication-phase2-resource-regression.log`, `/private/tmp/wlc-field-fabrication-phase2-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase2-burst.log`.
- Phase 2 is complete at 10/10. Checklist count is 39/103. Next action: extend the existing tray destination candidate and reservation path to include same-faction `MaterialFabricationInputTag` Oil demand while preserving active assignments and refinery regressions.

### 2026-07-13 - Phase 3 Automated Oil Destination Routing

- Extended the existing `BuildingResourceHaulerBridgeCompositionSystemHelper` Oil destination predicate to accept enabled, same-faction entities with `MaterialFabricationInputTag` and `MaterialFabricationComponent`. Disabled fabrication inputs remain ineligible.
- Reused the existing tray-truck query, `UnitResourceHaulOrder`, `UnitResourceHaulReservation`, movement requests, timed loading/unloading, physical `BuildingResourceStorageComponent`, and exact release paths. No second movement system, new updating behavior, new resource owner, or new recurring managed collection was added.
- Added deterministic Oil destination ordering: production starvation first, normalized unreserved input capacity second, existing world-route distance third, and runtime building id last. Source and non-Oil destination distance ties now also use stable building id instead of dictionary iteration order.
- Included fabrication enabled/version state in the existing automatic-assignment signature. Existing active orders are still processed without re-selection, and the existing two-second stable refresh window prevents idle rescans from oscillating on unchanged state.
- Manual tray assignment recognizes fabrication inputs through the same eligibility predicate. Reservation occurs before movement, and all existing death, source/destination destruction, route loss, manual override, orphan, unload, and capacity cleanup paths remain shared.
- Added depot route, refinery/depot competition, starvation priority, stable-id tie, complete pump/tray/depot transfer, and active-assignment retention tests. The full resource/logistics fixture passed 44/44, including its unchanged steady-state 0 managed-byte test and all refinery/tanker/reservation cleanup regressions.
- Script assembly boundaries passed 31/31 and ECS Burst hot-path architecture passed 10/10. The touched runtime remains in `Game.Runtime`; no assembly reference changed.
- Evidence: `/private/tmp/wlc-field-fabrication-phase3-routing-final.log`, `/private/tmp/wlc-field-fabrication-phase3-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase3-burst.log`.
- Phase 3 is complete at 11/11. Checklist count is 50/103. Next action: implement the Burst-compatible unmanaged `MaterialFabricationSystem` against projected fabrication, physical Oil storage, and canonical faction Materials, with exact cycle/capacity/status behavior.

### 2026-07-13 - Phase 4 Oil-To-Materials Conversion

- Added active `MaterialFabricationSystem` in `Game.Runtime` as a Burst-compiled unmanaged `ISystem`. It accumulates simulation delta and processes depots at the existing one-second building-resource cadence only while `RuntimeGameplayStateComponent.SimulationActive` is set.
- Added reusable persistent 256-faction entity/count arrays owned by the system. The arrays rebuild without snapshots or managed collections, require exactly one matching `FactionEconomy` plus `FactionTacticalMaterialsComponent` owner, and reject missing, duplicate, or mismatched ownership deterministically.
- Conversion computes completed cycles arithmetically in O(1), consumes only Oil not reserved for outbound logistics, grants integer Materials through `FactionTacticalMaterialsUtilitySystemHelper`, and commits copied storage/materials state only after both mutations validate. Oil cannot become negative and output cannot exceed the authored Materials capacity.
- Large deltas process every feasible cycle without a catch-up loop or hard cycle cap. Time after the exact point at which Oil or capacity blocks production is discarded, while valid sub-cycle progress is retained only while the next cycle can run. Render-frame partition tests produce identical resource totals.
- Added typed `NoOilInput`, `MaterialsCapacityFull`, `ProductionDisabled`, and `BuildingDisabled` runtime transitions with automatic resume. `Disabled`, dead, and death-animation building state is rejected; ownership changes now update `Faction`, `RuntimeBuildingCombatInfo`, physical storage, and fabrication owner data coherently.
- Added a startup-projected, fixed-capacity per-faction `MaterialFabricationEconomyEventElement` buffer. It emits aggregate cycle-completed and status-transition records with typed ids/status/reasons and no simulation strings; `LifetimeFabricated` remains the cumulative Materials telemetry authority.
- Added `BuildingResourceStorageTransferSystemHelper.TryConsumeAvailableSourceResource` so fabrication shares existing reservation and changed-only storage-version rules. Fabrication and faction versions advance only when their values, progress, status, or reason actually change.
- Focused validation passed 12/12: exact-once conversion, 30/60 fps partitioning, empty/partial/reserved Oil, full capacity and resume, production/building disable and resume, ownership mismatch, arithmetic large delta, changed-only versions, pure helper `0 B`, active world system behavior, bounded typed events, and 256 measured active world updates at `0 B` managed allocation after warmup.
- Existing regressions passed: resource production/logistics 44/44, assembly boundaries 31/31, and ECS Burst hot-path architecture 10/10. The broad 58-test architecture suite still reports eight unrelated existing debts in rendering/UI/camera/static-registry ratchets; its temporary ninth failure was this slice's exported struct-return Burst annotation and was removed before final validation.
- Evidence: `/private/tmp/wlc-field-fabrication-phase4-final.log`, `/private/tmp/wlc-field-fabrication-phase4-resource-regression.log`, `/private/tmp/wlc-field-fabrication-phase4-architecture.log`, `/private/tmp/wlc-field-fabrication-phase4-burst.log`, and `/private/tmp/wlc-field-fabrication-phase4-architecture.xml`.
- Phase 4 is complete at 11/11. Checklist count is 61/103. Next action: add authored Materials construction costs and replace the UI-supplied building price path with stable-id-resolved atomic Credits plus Materials placement transactions.

### 2026-07-13 - Phase 5 Authored Costs And Atomic Placement Core

- Added authored Materials costs to all 23 requestable building configs and preserved their existing Credits prices. Both costs project through config, hidden authoring, metadata, runtime definitions, ECS catalog read models, and footprint clones. Match starts with 120 tactical Materials and 600 capacity, enough to build the 100-Materials Field Fabrication Depot recovery path.
- Added `BuildingDefinition.CreditsCost` so the resolved runtime definition owns both construction costs. Configured-building request and result paths now use the authored Credits cost; the UI-supplied `Price` remains only for legacy unit-production requests and cannot alter building affordability or spending.
- Extended placement request/result data with an economy transaction id and typed Credits, Materials, combined-shortage, duplicate, registration-failure, and transaction-rejection outcomes.
- Added a single in-flight exact-once reservation boundary to `RuntimeResourceUtilitySystemHelper`. It spends `FactionEconomy.Money` and `FactionTacticalMaterialsComponent` atomically after geometry validation, then finalizes on registration or refunds both resources on complete registration failure. Settled or repeated transaction ids are rejected without mutation.
- Replaced the ambiguous null commit contract with `CommitOutcome`, which reports expected and committed instance counts plus optional auto-selection. Failed wall registrations destroy their newly created visual; non-auto-select buildings now report successful registration explicitly.
- Map-authored placement remains on the separate map spawn path and never enters the player placement transaction. Existing zero-Materials definitions and unit-production requests remain Credits-only.
- Focused validation passed: authored cost projection 3/3, lifecycle construction transaction 5/5, commit outcome 5/5, placement command regression 17/17, production request regression 26/26, runtime resource transaction 9/9, assembly boundaries 31/31, and Burst hot-path architecture 10/10. The runtime resource fixture measured 0 managed bytes across 512 reserve/rollback cycles. The resource and final placement reruns logged their passing markers before a Unity Asset Import Worker shutdown crash; the other listed runs exited cleanly.
- Evidence: `/private/tmp/wlc-field-fabrication-phase5-cost-projection.log`, `/private/tmp/wlc-field-fabrication-phase5-placement-transaction.log`, `/private/tmp/wlc-field-fabrication-phase5-commit-outcome.log`, `/private/tmp/wlc-field-fabrication-phase5-placement-regression.log`, `/private/tmp/wlc-field-fabrication-phase5-production-regression.log`, `/private/tmp/wlc-field-fabrication-phase5-resource-transaction.log`, `/private/tmp/wlc-field-fabrication-phase5-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase5-burst.log`.
- Extended the existing Build Drawer catalog contract and read model with authored Materials cost while preserving the existing Credits price. Building cards now populate the existing Supplies fields with grouped invariant Materials values; unit-production cards retain zero Materials cost and do not display a false secondary price.
- Build Drawer dual-cost validation passed 2/2 after Unity compiled the combined runtime and UI changes. No view-owned economy policy, new update loop, default-assembly fallback, or assembly reference was added. Evidence: `/private/tmp/wlc-field-fabrication-phase5-build-drawer.log`.
- Final review found that a failed multi-segment wall registration could previously leave paid partial structures. Wall commit is now atomic: any failed segment immediately removes all registrations and ECS blocker/combat entities created by that command, destroys their visuals, preserves the preview for retry, and permits the resource transaction to roll back. Focused wall commit passed 5/5 and construction settlement passed 6/6, including exact Credits and Materials restoration. Evidence: `/private/tmp/wlc-field-fabrication-phase5-atomic-wall-commit.log` and `/private/tmp/wlc-field-fabrication-phase5-atomic-wall-transaction.log`.
- Final combined validation passed the 31-test assembly/naming boundary contract and the 10-test Burst hot-path ratchet after a full Unity compile. The assembly run emitted its passing marker before Unity force-closed a lingering Asset Import Worker during shutdown; the Burst run exited cleanly. Evidence: `/private/tmp/wlc-field-fabrication-phase5-final-architecture.log` and `/private/tmp/wlc-field-fabrication-phase5-final-burst.log`.
- Phase 5 is complete at 11/11. Checklist count is 73/103. Next action: implement selected Field Fabrication Depot status/progress and typed disabled reasons through the existing selected-building read-model and presentation boundaries.

### 2026-07-13 - Phase 6A Typed Build Affordability Presentation

- Extended configured-building preflight to evaluate authored Credits and Materials through the canonical `FactionConstructionResourceUtilitySystemHelper` boundary. Runtime and UI contracts preserve typed insufficient-Credits, insufficient-Materials, and combined failures without introducing another wallet or affordability calculation.
- Build Drawer ECS catalog rows now publish `Enabled` plus `DisabledReason`; selected detail publishes the same reason and derives `BuildEnabled` from it. The managed gateway preserves the typed row reason.
- The active Canvas Build Drawer consumes `IBuildingUiCommand.GetCampRequestFailure` for row and primary-action state. It no longer subtracts UI prices from a displayed Credits balance or calculates missing resources in the view. Typed localized feedback covers Credits, Materials, and combined shortages.
- Focused Unity validation passed: Build Drawer dual-cost and disabled-reason projection 6/6, building production request regression 30/30, assembly/naming boundaries 31/31, and ECS Burst hot-path architecture 10/10. Generated `Game.Runtime` and `Game.Tests.Editor` builds completed with zero errors; `git diff --check` passed.
- Evidence: `/private/tmp/wlc-field-fabrication-phase6-build-drawer.log`, `/private/tmp/wlc-field-fabrication-phase6-production.log`, `/private/tmp/wlc-field-fabrication-phase6-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase6-burst.log`.
- Checklist count is 74/103. Phase 6 is 6/11. Next action: add the versioned selected-depot read model by joining authoritative fabrication, physical Oil storage, and faction Materials data through the existing selected-building UI query boundary.

### 2026-07-13 - Phase 6B Selected Depot Read Model And HUD Presentation

- Added `UiMaterialFabricationReadModel` in `Game.Runtime`. It joins the selected runtime building's ECS combat entity with `MaterialFabricationComponent`, `MaterialFabricationInputTag`, physical `BuildingResourceStorageComponent`, and the canonical matching `FactionEconomy` plus `FactionTacticalMaterialsComponent` entity.
- Live composition resolves the canonical player faction resource entity directly through `RuntimeResourceUtilitySystemHelper`; recurring selected-building reads perform no query creation, ECS snapshots, managed collections, or scene searches. Test contexts retain explicit multi-faction candidate validation for missing, duplicate, and mismatched owners.
- The read model publishes Oil input, authored Oil consumption and Materials output per cycle, duration, elapsed progress and normalized progress, faction Materials current/capacity, enabled state, typed status/reason, and a changed-only version. Fractional physical Oil is floored so presentation never overstates consumable whole barrels.
- Extended the immutable Match HUD selection contract with a dedicated `MaterialFabrication` storage mode. The selected-depot chip presents Oil, conversion rate/output, progress, faction Materials, and localized typed status; ordinary Oil/Fuel buildings retain the existing storage path. Selection presentation caches on read-model version and does not reformat unchanged state.
- Focused Unity validation passed: building UI query/read model and unchanged-state no-GC 10/10, selected-summary/presentation caching and typed status 20/20, real Match HUD prefab binding and hidden reset 4/4, assembly/naming boundaries 31/31, and ECS Burst hot-path architecture 10/10. Generated runtime and editor-test builds completed with zero errors; `git diff --check` passed.
- Evidence: `/private/tmp/wlc-field-fabrication-phase6-depot-query.log`, `/private/tmp/wlc-field-fabrication-phase6-depot-presentation.log`, `/private/tmp/wlc-field-fabrication-phase6-depot-prefab.log`, `/private/tmp/wlc-field-fabrication-phase6-depot-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase6-depot-burst.log`.
- Checklist count is 76/103. Phase 6 is 8/11. Next action: add typed ECS production enable/disable request/result data and bind the selected-depot control without introducing a new update loop.

### 2026-07-13 - Phase 6C Typed Depot Production Control

- Added fixed-capacity `MaterialFabricationRequestComponent` and `MaterialFabricationResultComponent` buffers plus a per-depot sequence owner in `Game.Components`. Both map-placed and player-built valid depots receive the boundary during existing runtime entity projection; invalid definitions receive none.
- The existing selected-depot HUD chip now issues explicit enable/disable requests and never writes `MaterialFabricationComponent` directly. Rapid taps retain request correlation, use optimistic presentation only until the authoritative read model refreshes, and never open the passenger drawer.
- `MaterialFabricationSystem` drains requests before simulation gating, validates requester ownership and typed payloads, mutates enabled/status/version exactly once, and publishes bounded correlated results. Paused simulation and temporarily missing faction Materials do not stall command acknowledgement.
- The existing selection startup composition boundary captures the selected depot entity, tracks the latest request id, consumes the typed result, and publishes accepted/rejected HUD feedback. No new managed update loop, manager/controller/service shell, assembly dependency, or runtime structural churn was added.
- Focused Unity validation passed: fabrication request/result behavior, idempotence, paused processing, and no-GC 17/17; map/runtime command projection and resource/logistics regression 44/44; Match HUD interaction 4/4; assembly/naming boundaries 31/31; and ECS Burst hot-path architecture 10/10. Generated `Game.Runtime` and `Game.Tests.Editor` builds completed with zero errors; `git diff --check` passed.
- Evidence: `/private/tmp/wlc-field-fabrication-phase6-command.log`, `/private/tmp/wlc-field-fabrication-phase6-command-projection.log`, `/private/tmp/wlc-field-fabrication-phase6-command-hud.log`, `/private/tmp/wlc-field-fabrication-phase6-command-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase6-command-burst.log`.
- Checklist count is 77/103. Phase 6 is 9/11. Next action: make Resource Header tap routing to Exchange scenario-gated and input-safe, then validate responsive layouts, localization expansion, touch sizes, and text clipping.

### 2026-07-13 - Phase 6D Header Routing And Responsive Closeout

- Resource Header actions now resolve exactly one complete player-owned Exchange boundary and fail closed for missing, enemy-only, duplicate-player, disabled, recipe-less, scenario-mismatched, non-Match, transitioning, or intro-locked state. Rejected taps are still consumed so they cannot leak into world input or open later as stale actions.
- The Exchange read model now projects only faction-consistent player data and reports unavailable when player ownership is ambiguous. Focused system tests cover enemy-plus-player selection and duplicate-player fail-closed behavior.
- The legacy serialized `SupplySlot` transform remains for prefab compatibility, while its visible localized label is `Materials`. The selected-depot fabrication chip uses a stable multi-line layout, minimum touch size, text auto-sizing, and wrapping; tests cover long status copy and containment at 1920x1080 (16:9) and 2400x1080 (20:9), then verify the original passenger layout is restored.
- Unity validation passed after a full compile: Exchange read-model ownership 5/5, header routing/input safety 7/7, selected-depot responsive presentation 4/4, assembly/naming boundaries 31/31, and ECS Burst hot-path architecture 10/10. The architecture process emitted its passing marker before a Unity shutdown exit 133; all other final runs exited 0. `git diff --check` passed, with no compiler errors in the Unity runs.
- Evidence: `/private/tmp/wlc-field-fabrication-phase6-read-model.log`, `/private/tmp/wlc-field-fabrication-phase6-header-routing-final.log`, `/private/tmp/wlc-field-fabrication-phase6-responsive-hud-final.log`, `/private/tmp/wlc-field-fabrication-phase6-architecture-final.log`, and `/private/tmp/wlc-field-fabrication-phase6-burst-final.log`.
- Phase 6 is complete at 11/11. Checklist count is 79/103. Next action: begin Phase 7 by locating the production startup/projector and authored recipe source that create the player Exchange capability, then author and validate expensive emergency Materials import without assuming test-fixture capability exists in live scenarios.

### 2026-07-13 - Phase 7 Production Exchange And Balance Safety

- Added the shipping `Game_ResourceExchange_Config` and serialized it on `MatchSceneView`. The active `custom.skirmish.legacy` gate enables two queue slots with timed recipes; `chapter.01.ftue` remains explicitly disabled. `Emergency Materials Airlift` is the player-facing recovery route, with a 90-second base duration and canonical Materials capacity validation.
- Added one-shot `ResourceExchangeStartupProjectionSystemHelper` execution after faction economy startup and before HUD binding. It resolves exactly one canonical player entity already owning `FactionEconomy` and `FactionTacticalMaterialsComponent`, adds/clears Exchange-only data at startup, and projects only recipes matching `CustomGameStartupStateComponent.GameModeId`. Missing gates and duplicate player economies fail closed. No parallel Credits/Materials owner, recurring managed loop, or runtime default-assembly fallback was added.
- Authored balance assumptions produce a local effective cost of 10.5 Credits per Material and an import cost of 18 Credits per Material, a 1.71x emergency markup. Materials round-trip retention is 0.0445 (below 0.85), and Oil -> fabrication -> Materials export returns 4.0 Credits per Oil versus 4.25 for direct Oil export, so fabrication cannot be used to create export profit.
- Added `Field_Fabrication_Materials_Balance_Report.json` comparing 6,000 Materials from local production (63,000 Credits modeled cost), repeated imports (108,000 Credits), a 50/50 mixed strategy (85,500 Credits), and destroyed-depot emergency recovery (18 Credits per Material).
- Focused Unity validation passed after full compilation: shipping startup/balance/capacity/report 8/8, existing config guardrails 10/10, request validation 4/4, queue completion/refund 5/5, assembly/naming boundaries 31/31, and Burst hot-path architecture 10/10. The first Burst run detected one startup `ToEntityArray` debt increase; it was replaced with the established chunk iteration pattern and the rerun passed without raising the ceiling.
- Resource Exchange steady-state GC validation measured 0 allocated bytes across 512 frames after 64 warmup frames. Several Unity processes emitted shutdown-only exits 133/139 after their passing markers; the final startup, request, architecture, and Burst runs exited 0 and no compiler errors were reported.
- Evidence: `/private/tmp/wlc-field-fabrication-phase7-balance.log`, `/private/tmp/wlc-field-fabrication-phase7-config-regression.log`, `/private/tmp/wlc-field-fabrication-phase7-request-regression.log`, `/private/tmp/wlc-field-fabrication-phase7-queue-regression.log`, `/private/tmp/wlc-field-fabrication-phase7-gc.log`, `/private/tmp/wlc-field-fabrication-phase7-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase7-burst-rerun.log`.
- Phase 7 is complete at 8/8. Checklist count is 87/103. Next action: inspect current AI construction affordability and Oil destination pressure inputs, then add shared-rule AI behavior and typed Materials/logistics telemetry without hidden resources or managed hot-path work.

### 2026-07-13 - Phase 8A Canonical AI Construction Affordability

- `AIBuildPlannerSystem` now resolves each authored building's Credits and Materials costs from the configured runtime read model and evaluates both through `FactionConstructionResourceUtilitySystemHelper`. Typed results distinguish Credits, Materials, combined shortages, and invalid resource state without adding another wallet or AI-only balance path.
- AI construction reserves `FactionEconomy.Money` and `FactionTacticalMaterialsComponent` atomically before enqueue. Successful placement retains the exact reservation; failed placement restores both values and corrects lifetime Materials spend. A rollback that cannot yet fit remains unsettled and blocks another reservation until it can complete.
- Spawn requests carry both authored costs for exact settlement and typed diagnostics. The planner updates Materials only when changed, retains deterministic plan/request ordering, and adds no managed collection, new update loop, structural hot-path mutation, or runtime assembly dependency.
- The shipping enemy scenario now authors 655 starting Materials with 655 capacity, exactly matching the current six-building AI construction plan. Player starting Materials remain unchanged; legacy/custom scenarios preserve zero AI Materials unless they explicitly author a reserve. This is visible startup data consumed through the canonical component, not a hidden runtime grant.
- Focused Unity validation passed: AI planner transaction, rollback, typed affordability, and warmed no-GC decision paths 5/5; initial resource projection; shipping authored-cost projection; and the complete AI build, production, squad, targeting, and combat-order integration loop. Synchronous integration fixtures use the existing force-scan boundary because editor batch time does not advance through the production idle-probe intervals.
- Assembly/naming boundary and ECS Burst hot-path validation passed. Generated `Game.Runtime` and `Game.Tests.Editor` builds compile with zero errors; task-scoped `git diff --check` passes. No performance ceiling or Burst exception was added.
- Evidence: `/private/tmp/wlc-field-fabrication-phase8-ai-build.log`, `/private/tmp/wlc-field-fabrication-phase8-initial-units.log`, `/private/tmp/wlc-field-fabrication-phase8-config.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-e2e.log`, `/private/tmp/wlc-field-fabrication-phase8-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase8-burst.log`.
- Phase 8 is 1/7. Checklist count is 88/103. Next action: extend existing deterministic Oil destination scoring with derived AI construction demand and Fuel pressure while preserving active assignments, physical storage authority, and stable no-GC routing.

### 2026-07-13 - Phase 8B Demand-Aware AI Oil Allocation

- Existing automatic Oil destination scoring now derives AI pressure from the enabled `AIBuildPlan`, canonical `FactionControlEntry`, `FactionEconomy` and `FactionTacticalMaterialsComponent`, authored construction costs, and the existing faction usable-Fuel summary. It prioritizes an enabled Field Fabrication Depot when a reachable selected construction plan lacks Materials, prioritizes a refinery when Fuel pressure remains, and gives critical Fuel reserve the highest band to prevent logistics deadlock.
- The pressure band is evaluated only for an idle Oil hauler during the existing two-second assignment scan. Active delivery orders are not reassigned, physical building storage remains authoritative, and player/non-AI routing retains the previous starvation, capacity, distance, and stable-id ordering.
- Query discovery uses native archetype-chunk iteration rather than `ToEntityArray`/`ToComponentDataArray`. AI pressure is resolved once per faction per assignment scan through a fixed-capacity unmanaged cache, including cached negative results. Focused warmup validation measured 0 managed bytes across 128 AI Oil input reads, and the Burst architecture gate confirms array-snapshot debt remains at its zero ceiling.
- Focused production/logistics validation passed 50/50, including AI-to-player control transition, critical Fuel versus construction pressure, unreachable Materials cost above canonical capacity, and one resolver read per faction per scan. Assembly/naming boundary validation passed 31/31, ECS Burst hot-path validation passed 10/10, task-scoped `git diff --check` passed, and serial `Game.Runtime` plus `Game.Tests.Editor` builds completed with zero errors.
- Graphics-capable Menu-to-Match smoke passed through the production shell: `mode=MatchHud`, `route=Match`, `phase=MatchHudReady`, `playRequested=1`, `matchIntro=Complete`, `inputLocked=0`, `matchSceneLoaded=1`, `hudLoaded=1`, and `curtainHidden=1`. No crash, fatal exception, or failed result was logged after readiness.
- Evidence: `/private/tmp/wlc-field-fabrication-phase8-ai-oil-final.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-oil-architecture-final.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-oil-burst-final-rerun.log`, and `/private/tmp/wlc-field-fabrication-phase8-menu-to-match-smoke-final.log`.
- Phase 8 is 2/7. Checklist count is 89/103. Next action: gate AI Exchange imports by explicit scenario permission and typed recovery need without adding hidden resources, parallel authority, or a managed update loop.

### 2026-07-13 - Phase 8C Scenario-Gated AI Materials Recovery

- `AIBuildPlannerSystem` now publishes a typed `AIMaterialsRecoveryNeedComponent` on the preallocated AI build-plan entity when an authored build is blocked by canonical Materials. The need preserves the first blocked time for the same build, carries the authored Credits reserve and Materials requirement, recomputes missing Materials from canonical state, rejects requirements above canonical capacity, and clears on manual control, pending/affordable/complete plans, invalid resources, or missing economy ownership.
- Match startup preprojects the existing Resource Exchange boundary onto canonical non-player faction economy entities only when the authored scenario enables both Exchange and AI Exchange. It snapshots eligible faction ids before structural projection, rejects duplicate faction controls and duplicate canonical ownership, supports later manual/AI transitions, clears stale non-player boundaries when a same-world scenario restart disables the capability, and leaves both shipping AI gates disabled.
- Added Burst `ResourceExchangeAIRecoverySystem` after AI planning and fabrication and before Exchange request validation. It requires canonical AI control and the explicit `AllowAiExchange` gate, preserves Credits needed by the blocked build, respects authored recipe steps, fee/floor output, scenario tag, duration, Materials capacity, queue capacity, active/pending imports, and deterministic recipe order. Existing request validation remains the only input-spend authority and queue completion remains the only import-grant authority.
- Local recovery projection sums all enabled faction depots over the authored import duration, bounded by cycle progress, cycle output, and unreserved physical Oil. An import is suppressed when local output covers the shortage; Oil-starved or route-starved depots receive one authored-duration grace period before the expensive fallback. Player-auto uses the existing player Exchange boundary, and manual takeover stops new AI requests without cancelling accepted canonical jobs.
- Focused validation passed: AI recovery behavior, orphan-plan rejection, canonical request validation, and the 512-update warmed GC path 9/9 with 0 managed bytes; Exchange startup projection and enabled-to-disabled boundary cleanup 12/12; AI startup 1/1; AI planner transaction/recovery 7/7; AI Exchange guardrails 4/4; assembly/naming boundaries 31/31; and ECS Burst hot-path architecture 10/10. `Game.Runtime` and `Game.Tests.Editor` compiled with zero errors. The production Menu-to-Match smoke reached `MatchHudReady` with Match/HUD scenes loaded, intro complete, input unlocked, curtain hidden, and no crash or fatal result. Task-scoped diff checks passed.
- Evidence: `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-review-fixes.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-startup-transition-final.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-ai-startup.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-planner-post-rebase.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-guardrail-post-rebase.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-architecture-post-rebase.log`, `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-burst-post-rebase.log`, and `/private/tmp/wlc-field-fabrication-phase8-ai-exchange-menu-to-match-post-rebase-rerun.log`.
- Phase 8 is 3/7. Checklist count is 90/103. Next action: add typed Materials source/spend and depot blocked-time telemetry without introducing per-frame managed aggregation or another resource authority.

### 2026-07-13 - Phase 8D Faction-Owned Materials And Fabrication Telemetry

- Extended the canonical `FactionTacticalMaterialsComponent` counters with construction, repair, infrastructure, and upgrade spend categories while retaining gross spend and export totals. The existing mutation boundary records and reverses these counters atomically, so fabrication, Exchange, player construction, and AI construction do not gain parallel telemetry mutation paths.
- Added one faction-owned `FactionMaterialFabricationTelemetryComponent` beside canonical Materials. Faction and initial-unit startup preproject/reset it; the existing Burst `MaterialFabricationSystem` accumulates active time and typed blocked durations for all depots without a new ECS system, managed update loop, recurring structural change, or per-depot lifetime authority that would be lost on capture/destruction.
- Large-delta accounting splits active and blocked time at the exact Oil/capacity boundary. `NoOilRoute` remains a typed zero-valued category until the existing logistics owner supplies that state in the next telemetry slice.
- `BalanceMetrics` and `BalanceReportWriter` now expose Materials source/spend totals plus fabrication active/blocked durations in JSON and Markdown, explicitly labeling gross spend as including exports.
- Focused validation passed: fabrication behavior/exact time splitting/warmed GC 19/19 with 0 managed bytes; canonical typed Materials mutations 9/9; faction startup 3/3; initial resource projection 12/12; balance report contracts 4/4; construction transaction regression 8/8; tactical resource ownership 1/1; assembly/naming boundaries 31/31; and ECS Burst hot-path architecture 10/10. `Game.Runtime` and `Game.Tests.Editor` compiled with zero errors. Task-scoped diff checks passed.
- Evidence: `/private/tmp/wlc-field-fabrication-phase8-faction-telemetry-focused-rerun.log`, `/private/tmp/wlc-field-fabrication-phase8-materials-typed-telemetry.log`, `/private/tmp/wlc-field-fabrication-phase8-telemetry-startup.log`, `/private/tmp/wlc-field-fabrication-phase8-telemetry-initial-units.log`, `/private/tmp/wlc-field-fabrication-phase8-telemetry-balance-report.log`, `/private/tmp/wlc-field-fabrication-phase8-telemetry-construction.log`, `/private/tmp/wlc-field-fabrication-phase8-telemetry-ownership.log`, `/private/tmp/wlc-field-fabrication-phase8-telemetry-architecture.log`, and `/private/tmp/wlc-field-fabrication-phase8-telemetry-burst.log`.
- Phase 8 is 4/7. Checklist count is 91/103. Next action: record tray assignments, reassignments, failures, and refinery-versus-depot Oil delivery through the existing resource-hauler/logistics owners.

### 2026-07-14 - Phase 8E Faction-Owned Tray Logistics Telemetry

- Added one unmanaged `FactionFuelLogisticsTelemetryComponent` beside the canonical faction economy and Materials authorities. Faction startup preprojects it and same-world initial-resource startup resets it, so cumulative logistics evidence survives truck/building destruction but never leaks between matches.
- The existing resource-hauler bridge remains the sole route owner. Successful automatic Oil tray routes count assignments; successful manual route changes count reassignments; same-route refreshes do not count. A failure counts only when a tray enters a blocked episode, so stable automatic retries cannot inflate totals. Fuel tankers are excluded.
- Active routes now fail closed when a source or destination is captured by another faction. Reservations are released and the dispatching faction retains the historical assignment/failure evidence.
- Oil delivery is recorded only after physical unload succeeds, using the pre-unload cargo amount. A valid faction-owned fabrication input is classified as a depot before the refinery predicate is evaluated, preventing malformed dual-purpose content from double counting; generic Oil storage is not attributed.
- `BalanceMetrics` and `BalanceReportWriter` expose assignment, reassignment, failure, refinery-delivery, and depot-delivery totals in JSON and Markdown. Mutation uses saturating counters, finite non-negative barrel totals, version wrapping, chunk-based unique faction resolution, and no new ECS system, managed update loop, recurring structural tick change, or parallel resource authority.
- Validation passed: resource-hauler utility and warmed telemetry mutation 28/28 with 0 managed bytes; production/logistics assignment, reassignment, same-route suppression, blocked retry, capture, and delivery regression 52/52; faction startup 3/3; initial-resource lifecycle/reset 12/12; balance reports 6/6; tactical ownership 1/1; assembly/naming boundaries 31/31; and ECS Burst hot-path architecture 10/10. Unity compilation and generated `Game.Components`, `Game.Runtime`, and `Game.Tests.Editor` builds completed with zero errors. Task-scoped diff checks passed.
- Evidence: `/private/tmp/warline-phase8e-resource-hauler.log`, `/private/tmp/warline-phase8e-production-final.log`, `/private/tmp/warline-phase8e-startup.log`, `/private/tmp/warline-phase8e-initial-units.log`, `/private/tmp/warline-phase8e-balance.log`, `/private/tmp/warline-phase8e-ownership.log`, `/private/tmp/warline-phase8e-architecture.log`, `/private/tmp/warline-phase8e-burst.log`, and `/private/tmp/warline-phase8e-compile.log`.
- Phase 8 is 5/7. Checklist count is 92/103. Next action: add startup/scenario validation requiring starting Materials, a viable fabrication chain, rebuildability, or explicitly enabled Exchange recovery.

### 2026-07-14 - Phase 8F Deterministic Scenario Recovery Safety

- Added one non-updating `MaterialsScenarioRecoveryValidationSystemHelper` in `Game.Runtime`. It evaluates every participating faction against typed Starting Materials, seeded depot/Oil-source/tray chain, affordable rebuild chain, and exact scenario-gated Credits-to-Materials Exchange paths. It rejects duplicate factions, missing capacity, unresolved enabled AI construction plans, and scenarios with no recovery path.
- Manual/player factions require enough starting Materials and capacity for at least one authored requestable construction. AI-controlled factions require starting Materials for the complete enabled authored build plan while capacity must fit its largest single planned build; local fabrication, rebuild, or explicitly AI-enabled Exchange can recover a short reserve. Arithmetic is saturating and plan/recipe iteration order is deterministic.
- Match startup runs validation after the existing building startup tick publishes the configured catalog. Catalog readiness has a bounded wait, then fails closed. Startup failures are captured once by `MatchBootstrapCompositionSystemHelper`, surfaced through the existing terminal `MatchStartStatusKind.Failed`, and use bounded FixedString messages instead of retrying forever or throwing truncation errors.
- Focused scenario/deadlock validation passed 10/10, including all four recovery paths, capacity and partial-reserve rejection, duplicate-faction rejection, explicit AI Exchange permission, shipping player/AI reserves, deterministic repeated decisions, and 0 managed bytes across 512 evaluations after warmup. Custom/legacy startup passed 4/4, AI build-plan regression passed 7/7, and Resource Exchange startup passed 12/12.
- Assembly/naming boundaries passed 31/31 and ECS Burst hot-path architecture passed 10/10. The graphics-capable production Menu-to-Match smoke reached `MatchHudReady` with Match/HUD loaded, intro complete, input unlocked, curtain hidden, and no crash or scenario failure. Task-scoped diff checks pass; unrelated Operations Dashboard prefab whitespace remains outside this slice.
- Evidence: `/private/tmp/wlc-field-fabrication-phase8f-scenario.log`, `/private/tmp/wlc-field-fabrication-phase8f-custom-startup.log`, `/private/tmp/wlc-field-fabrication-phase8f-ai-plan.log`, `/private/tmp/wlc-field-fabrication-phase8f-exchange-startup.log`, `/private/tmp/wlc-field-fabrication-phase8f-architecture.log`, `/private/tmp/wlc-field-fabrication-phase8f-burst.log`, and `/private/tmp/wlc-field-fabrication-phase8f-menu-to-match.log`.
- Phase 8 is complete at 7/7. Checklist count is 94/103. Next action: execute Phase 9 integration, profiler/GC comparison, Android target-device validation where available, documentation reconciliation, and final acceptance closeout.

### 2026-07-14 - Phase 9 Integration, GC, And Open Performance Gates

- Added a closeout runner that suppresses the focused suites' individual batch exits and fails the process only after inspecting every suite result. The 17-suite matrix passed conversion, automated fuel logistics, authored cost projection, dual-resource placement, Build Drawer presentation and interaction, Exchange routing/request/queue/startup, faction/custom startup, AI planning/end-to-end behavior, and initial-unit spawn completion. A stale Build Drawer test was reconciled with the existing fail-closed command binding and `BuildingDefinition` cost ownership; production code was not weakened.
- Assembly/naming boundaries passed 31/31. ECS Burst hot-path architecture passed 10/10 with 177 `OnUpdate` files, 62 Burst systems, 44 job-backed systems, 94/94 classified non-Burst `ISystem` files, and zero unclassified systems.
- Existing regressions passed: building placement 17/17, Match HUD 8/8, Match-start PlayMode 1/1, AI planner 7/7, Resource Exchange request/queue/startup, automated fuel logistics 52/52, and the graphics-capable production Menu-to-Match smoke. The Match reached `MatchHudReady` with scenes loaded, intro complete, input unlocked, and no startup crash.
- Fabrication warmed simulation, tray/logistics telemetry, Match HUD cached reads, scenario evaluation, and Exchange request/queue steady state remain at 0 managed bytes. Exchange GC validation measured 0 bytes across 512 frames after 64 warmup frames. Added a no-request early-out to avoid resolving unused Exchange recipe/result/event/physical-reservation buffers during accepted idle steady state; focused request validation remains 4/4.
- The exact Phase 0 Exchange p95 baseline was 0.019 ms. Repeated post-change samples were 0.024 ms, 0.031 ms, and 0.053 ms with 0 allocated bytes, all far below the existing 10 ms absolute budget but above the tracker-owned 5% relative threshold. This gate remains unchecked; the variation at microsecond scale does not justify silently waiving the authored contract.
- Android tooling is installed but `adb devices -l` reports no connected target. Touch, sustained frame time, memory, and thermal validation therefore remains unchecked and requires a physical target device.
- Evidence: `/private/tmp/wlc-field-fabrication-phase9-focused-matrix.log`, `/private/tmp/wlc-field-fabrication-phase9-architecture-final.log`, `/private/tmp/wlc-field-fabrication-phase9-burst-final.log`, `/private/tmp/wlc-field-fabrication-phase9-placement.log`, `/private/tmp/wlc-field-fabrication-phase9-hud-results.xml`, `/private/tmp/wlc-field-fabrication-phase9-match-start-results.xml`, `/private/tmp/wlc-field-fabrication-phase9-exchange-gc-final.log`, `/private/tmp/wlc-field-fabrication-phase9-exchange-performance.log`, `/private/tmp/wlc-field-fabrication-phase9-exchange-performance-run2.log`, `/private/tmp/wlc-field-fabrication-phase9-exchange-performance-optimized.log`, and `/private/tmp/wlc-field-fabrication-phase9-request-early-out.log`.
- Phase 9 is 6/9. Checklist count is 100/103. Next action: establish a statistically stable same-scenario p95 comparison or optimize below the 5% threshold, connect an Android target for device evidence, then reconcile final documentation and close the tracker.
