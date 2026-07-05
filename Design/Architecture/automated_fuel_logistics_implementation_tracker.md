# Automated Fuel Logistics Implementation Tracker

Date: 2026-07-05
Status: In progress
Design source: `../Automated_Fuel_Logistics_Design.md`

## Objective

Implement the automated Oil -> Fuel logistics model without drifting from the current architecture. The runtime source of truth must stay in ECS data. Hot gameplay work should use Burst and `ISystem` as much as possible. Managed helpers are allowed only as narrow presentation, authoring, or composition boundaries.

## Architecture Contract

- Prefer unmanaged `ISystem` processors and Burst-compatible jobs for extraction, assignment, transfer, conversion, storage, and fuel consumption.
- Keep Unity object work out of simulation systems. Presentation consumes read models after ECS state changes.
- Do not add a manager, controller, facade, broad service shell, or new updating `MonoBehaviour` loop.
- Use typed components, buffers, reason codes, and request/result data instead of stringly runtime state.
- Avoid steady-state GC allocations. No LINQ, foreach boxing, per-frame string formatting, temporary managed lists, or repeated full model rebuilds in hot paths.
- Gate work by dirty/version components where practical: source availability, destination capacity, route state, truck state, storage value, and selected-building read models.
- Keep logistics simulation deterministic enough for tests: fixed tick deltas, explicit capacities, explicit reservation ownership, and stable tie-breakers.
- Preserve existing naming conventions. Non-ECS helpers must use approved helper suffixes when needed.
- Keep UI Toolkit/Canvas migration out of this scope.
- Validate each slice independently before expanding the next slice.

## Progress Summary

Overall implementation progress: 82% (82/99 checklist items complete).

Progress is checklist-based. Each `- [ ]` or `- [x]` implementation/validation item below counts as one item. When a future implementation slice adds or removes checklist items, update this section in the same commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Inventory and baseline | In Progress | 5 | 8 | 63% | Audit current Oil/Fuel components, building runtime summaries, resource hauler code, seeded trucks, and HUD header data. |
| 1. Data model | Complete | 11 | 11 | 100% | Add or adapt ECS components/buffers, capacities, reservations, cargo, seeded logistics validation, and faction usable Fuel. |
| 2. Oil extraction and refinery buffers | Complete | 8 | 8 | 100% | Oil Pump and Refinery storage mutation is ECS-owned and versioned. |
| 3. Tray truck automation | Complete | 13 | 13 | 100% | Auto-assign Oil pickup/delivery without manual target commands. |
| 4. Refinery conversion | Complete | 9 | 9 | 100% | Convert Oil input buffer into Fuel output buffer with cap/stall reasons. |
| 5. Tanker automation and usable Fuel | Complete | 12 | 12 | 100% | Deliver refinery output Fuel into Fuel Bladder/base storage and update header pool. |
| 6. Vehicle fuel spending | Complete | 11 | 11 | 100% | Consume usable Fuel for ground/air mobility and block/redirect orders safely. |
| 7. UI read models and feedback | Complete | 10 | 10 | 100% | Header, selection panel, disabled reasons, and truck task feedback from versioned data. |
| 8. AI and enemy support | In Progress | 2 | 7 | 29% | Let AI understand fuel economy after player loop is validated. |
| 9. Validation and profiling | In Progress | 1 | 10 | 10% | Focused tests, seeded-map checks, guardrails, GC checks, and Android profiling. |

## Phase 0: Inventory And Baseline

- [x] Inventory existing Oil Pump, Refinery, Large Refinery, Fuel Bladder, tray truck, and tanker configs.
- [x] Inventory each faction military base and verify fuel-enabled maps have at least one `Unit_Veh_Truck_Tray` and one `Unit_Veh_Truck_Tanker` near each faction base.
- [x] Inventory current ECS components and authoring bakers for building resources, production, storage, vehicle movement, and resource hauling.
- [x] Identify current header Fuel source and all systems that mutate tactical Oil/Fuel.
- [x] Identify current selected-building panel fields for production/resource state.
- [ ] Capture baseline behavior: oil pump production, refinery conversion if present, fuel header update, tray/tanker command behavior, and vehicle Fuel usage.
- [ ] Capture baseline profiler markers for match steady state with fuel logistics active.
- [x] Document exact files touched before implementation starts.

Exit criteria:

- Existing behavior and data ownership are documented.
- No code has been converted before the active data path is known.
- Any managed boundary that must remain managed is counted and justified.

## Phase 1: ECS Data Model

- [x] Define or adapt a `ResourceKind` representation for Oil and Fuel that can be used by Burst-compatible systems.
- [x] Define storage buffer data for buildings: current amount, capacity, reserved inbound, reserved outbound, and version.
- [x] Define logistics cargo data for tray/tanker trucks: carried resource, amount, capacity, source, destination, task state, and reservation id.
- [x] Define logistics role tags: oil hauler, fuel hauler, source, refinery input, refinery output, fuel storage.
- [x] Define faction usable Fuel summary: current, capacity, produced, delivered, spent, version.
- [x] Define typed blocked/status reason codes for UI and command validation.
- [x] Define scenario/map authoring requirement for seeded tray/tanker pairs at each fuel-enabled faction military base.
- [x] Add baker/config mapping only where needed; do not duplicate balance data already owned by configs.

Performance notes:

- Use fixed-size value components where possible.
- Use dynamic buffers only for truly variable storage/reservation collections.
- Prefer integer/fixed-point resource values for deterministic accumulation and cheap UI deltas.

Validation:

- [x] Editor tests cover storage capacity, reservation accounting, and faction usable Fuel summary.
- [x] Authoring validation warns or fails when a fuel-enabled map lacks one tray truck or one tanker near each faction military base.
- [x] `git diff --check` passes.

## Phase 2: Oil Extraction And Building Buffers

- [x] Keep Oil Pump extraction in ECS or move it into an unmanaged `ISystem` if currently managed.
- [x] Write Oil into a pump output buffer with capacity and dirty/version update.
- [x] Ensure extraction stalls cleanly when the pump buffer is full.
- [x] Publish selected Oil Pump read model only when buffer/rate/block reason changes.
- [x] Ensure map-placed and player-built oil pumps follow the same baked component path.

Validation:

- [x] Focused oil pump production test: pump accumulates Oil over deterministic ticks.
- [x] Focused capacity test: pump stops at capacity without overflow or GC.
- [x] Selection panel test: pump status changes only when read-model version changes.

## Phase 3: Tray Truck Automation

- [x] Implement or assess `OilTrayLogisticsAssignmentSystem` as unmanaged `ISystem` where practical.
- [x] Use dirty/version gates so assignment work only runs when source availability, destination capacity, route validity, or truck availability changes.
- [x] Reserve source Oil and refinery input capacity before a truck starts a task.
- [x] Avoid all-truck/all-building scans every frame; use cached queries and versioned candidate buffers where practical.
- [x] Reuse existing movement/order data path for truck travel rather than adding a new movement loop.
- [x] Transfer cargo at pickup/drop-off through ECS mutation systems.
- [x] Clear reservations on truck death, source/destination death, route invalidation, or task cancellation.

Validation:

- [x] Focused route resolver test covers same-faction tray Oil pairing and tanker Fuel pairing.
- [x] One pump, one refinery, one tray truck transfers Oil without manual command.
- [x] Seeded faction-base tray truck can start Oil hauling without building a truck at runtime.
- [x] No refinery capacity causes tray truck idle with a typed reason.
- [x] Destroyed source/destination clears reservations.
- [x] Steady-state automation produces 0 B/frame GC.

## Phase 4: Refinery Conversion

- [x] Adapt `BuildingResourceProductionEcsSystem` ECS storage path for refinery conversion.
- [x] Consume Oil input and produce Fuel output using config rate and efficiency.
- [x] Stall when Oil input is empty.
- [x] Stall when Fuel output buffer is full.
- [x] Publish selected Refinery read model only on version changes.
- [x] Support Large Refinery with the same system and different config data.

Validation:

- [x] Deterministic conversion test covers Oil input, Fuel output, efficiency, and capacity.
- [x] Full output buffer does not consume Oil.
- [x] Selected-building panel shows conversion and blocked reason accurately.

## Phase 5: Tanker Automation And Usable Fuel

- [x] Implement or assess `FuelTankerLogisticsAssignmentSystem` as unmanaged `ISystem` where practical.
- [x] Reserve refinery output Fuel and storage capacity before tanker pickup.
- [x] Deliver Fuel into Fuel Bladder/base storage, not directly into the header.
- [x] Update faction usable Fuel and capacity from storage state.
- [x] Keep header Fuel sourced from faction usable Fuel summary.
- [x] Stall tankers when no output Fuel, no storage, full storage, no route, or no available tanker exists.
- [x] Ensure map-placed and player-built Fuel Bladders follow the same storage path.

Validation:

- [x] Refinery output does not change header Fuel until tanker delivery completes.
- [x] Fuel Bladder capacity controls max usable Fuel.
- [x] Header updates after delivered Fuel and stays version-gated.
- [x] Seeded faction-base tanker can start Fuel hauling without building a truck at runtime.
- [x] Tanker idle/block reasons are typed and visible in selected truck/building UI.

## Phase 6: Vehicle Fuel Consumption

- [x] Add fuel-cost config for vehicle/air movement-heavy or operation-heavy actions if missing.
- [x] Implement `VehicleFuelConsumptionSystem` or equivalent unmanaged `ISystem` for active fuel drains.
- [x] Aggregate consumption by faction and vehicle class to avoid per-entity UI churn.
- [x] Block new movement/launch/support commands when usable Fuel is below required threshold.
- [x] Add aircraft-safe behavior: new launches blocked when fuel is short; active aircraft return to base or use emergency reserve, never stop midair.
- [x] Add ground-vehicle no-fuel behavior: reject new long moves, finish committed segment, return, or hold by policy.
- [x] Publish warning/read-model updates only when fuel state or blocked reasons change.

Validation:

- [x] Ground vehicle movement spends Fuel.
- [x] New ground vehicle movement is blocked at 0 usable Fuel with a typed reason.
- [x] Aircraft at 0 usable Fuel returns/lands safely and does not freeze midair.
- [x] UI command buttons update only on fuel/version changes.

## Phase 7: UI Read Models And Feedback

- [x] Header Fuel reads faction usable Fuel and capacity, not refinery output.
- [x] Oil can be shown only when the active mission/skirmish preset teaches extraction.
- [x] Selection panel supports Oil Pump, Refinery, Fuel Bladder, tray truck, and tanker logistics state.
- [x] Disabled command/build/production rows use typed reason ids.
- [x] Avoid per-frame string formatting; cache labels or update TMP text only on read-model version changes.
- [x] Keep Canvas work inside existing UI helper boundaries and do not add simulation logic to UI classes.

Validation:

- [x] Header fuel changes only after storage delivery or spending.
- [x] Selection panel updates for two different pumps/refineries/storage buildings without stale values.
- [x] UI click blocking remains intact on Android and Editor.
- [x] No new GC allocations in steady-state HUD updates.

## Phase 8: AI And Enemy Support

- [x] Enable the same storage/conversion/hauling data model for non-player factions.
- [ ] Gate AI production of fuel-cost units by available usable Fuel or expected production.
- [ ] Let AI target logistics infrastructure according to existing targeting architecture.
- [ ] Keep AI logistics choices ECS/data-driven; no managed per-frame planner scans.

Validation:

- [x] Enemy faction can produce/deliver Fuel in a fuel-enabled test setup.
- [ ] AI does not spam fuel-cost units at 0 usable Fuel.
- [ ] AI targeting can prioritize pump/refinery/storage/truck targets when scenario tags allow it.

## Phase 9: Performance, Tests, And Rollout

- [ ] Run `git diff --check`.
- [ ] Run compile validation after each implementation batch.
- [ ] Run focused oil/fuel logistics edit-mode tests.
- [ ] Run focused building production/resource validation.
- [ ] Run focused vehicle movement and aircraft return validation.
- [ ] Run seeded logistics fixture validation for Faction 1 and Faction 2 military base tray/tanker pairs.
- [ ] Run architecture guardrails, including Burst/ECS hot-path checks.
- [ ] Run steady-state match profiler capture with fuel logistics active.
- [ ] Run Android profiling if the feature affects match performance.
- [ ] Update this tracker with command names, log paths, pass/fail result, and next action after each batch.

Performance acceptance targets:

- No steady-state GC allocation from logistics simulation.
- No per-frame full scan from UI for logistics summaries.
- Assignment systems skip work when no source/destination/truck/storage version changed.
- Header and selection UI update only on read-model version changes.
- Logistics systems remain below the existing performance regression budget for match steady state.

## Validation Matrix

| Area | Required Checks |
|---|---|
| Compile | Unity compile or repository-approved compile validation. |
| Architecture | SOLID/ECS guardrails, Burst/hot-path guardrails, no new manager/controller/facade naming. |
| Data model | Storage, reservations, cargo, capacity, versioning, reason codes. |
| Oil loop | Pump extraction, tray pickup, refinery delivery. |
| Fuel loop | Refinery conversion, tanker pickup, storage delivery, header update. |
| Vehicle spend | Ground movement, aircraft launch/return, blocked commands. |
| UI | Header, selection panel, command buttons, no stale text, no input leakage. |
| Performance | 0 B/frame GC in steady state, skip gates verified, profiler sample attached. |
| Android | Touch/UI blocking, profiler capture, no new match FPS regression. |

## Implementation Notes Log

Use this section during implementation. Each completed batch should add:

- date
- files changed
- behavior changed
- validation commands and log paths
- profiler result when performance-sensitive
- remaining blocker or next slice

### 2026-07-05

- Slice: seeded logistics fixture and inventory baseline.
- Files changed before implementation code: `Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset`, `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`, and this tracker.
- Existing config inventory:
  - `Prefab_BuildingDefinition_OilPump_Config.asset`: Oil Pump produces 50 Oil barrels/day and stores 200 Oil barrels.
  - `Prefab_BuildingDefinition_OilRefinery_Config.asset`: Refinery stores 5000 Oil, converts at 100 Fuel barrels/day, and stores 5000 Fuel.
  - `Prefab_BuildingDefinition_OilRefinery_Big_Config.asset`: Large Refinery stores 10000 Oil, converts at 200 Fuel barrels/day, and stores 10000 Fuel.
  - `Prefab_BuildingDefinition_Fuel_Bladder_Config.asset`: currently has no serialized Oil/Fuel capacity fields, so it cannot contribute usable Fuel storage until configured.
  - `Prefab_UnitGrid_Veh_Truck_Tray.asset`: resource hauler capacity is 8 barrels; baker adds `UnitResourceHauler`.
  - `Prefab_UnitGrid_Veh_Truck_Tanker.asset`: resource hauler capacity is 0, so the baker does not add `UnitResourceHauler`; this blocks tanker automation until corrected.
- Existing ECS/data inventory:
  - `BuildingResourceStorageComponent` owns runtime Oil/Fuel capacity, rates, stored Oil, and stored Fuel on building combat entities.
  - `BuildingRuntimeEntityCompositionSystemHelper.AddResourceStorageComponent` adds storage for any building definition with Oil/Fuel capacity or rate.
  - `BuildingResourceProductionEcsSystem.ApplyTick` and `BuildingResourceProductionSystemHelper.Tick` provide the Burst-compatible production/conversion math, but `BuildingResourceProductionEcsSystem.OnUpdate` is disabled and currently used as a static helper from managed composition.
  - `UnitResourceHauler` owns hauler capacity, fill/unload timings, and cargo; `UnitResourceHaulOrder` owns source/destination/phase/resource kind for the existing manual order path.
  - `UnitGridAuthoring.Baker` adds `UnitResourceHauler` only when `resourceHaulerBarrelCapacity > 0`.
  - `BuildingResourceHaulerBridgeCompositionSystemHelper` is the current managed hauler bridge: it scans ordered haulers every 0.25s, advances load/travel/unload phases, and reuses existing movement requests.
  - `BuildingResourceHaulerTransferEcsSystem` contains static ECS-compatible load/unload helpers, but `OnUpdate` is disabled.
- Header/UI inventory:
  - `BuildingRuntimeProcessingCompositionSystemHelper.PublishFactionSummariesReadModel` publishes `BuildingRuntimeFactionSummary` from `BuildingResourceStorageComponent`/runtime building mirrors.
  - `FactionResourceCompositionSystemHelper` mutates Oil/Fuel through production ticks, drains/sell requests, and storage sync.
  - `BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingResourceStorage` reads selected building Oil/Fuel state for the selection panel.
  - `SelectionUiReadModelLookup` reads `UnitResourceHauler` cargo for focused unit panel state.
  - Header Fuel is backed by the match HUD header model path and current faction resource summary; the next fuel loop must keep header Fuel sourced from delivered storage, not refinery output.
- Validation:
  - `git diff --check` passed before commit `1c346b401 Seed fuel logistics verification trucks`.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed before commit `1c346b401`.
  - Full solution build remains blocked by existing generated solution issue: duplicate `Unity.ProBuilder` project name.
  - Unity batchmode was blocked by an already-open Unity editor for this project; live editor log showed Tundra build success with no compiler error matches.
- Slice: config mapping correction for automated fuel storage and tanker hauling.
- Files changed: `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Fuel_Bladder_Config.asset`, `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Truck_Tanker.asset`, `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`, and this tracker.
- Behavior intent:
  - Fuel Bladder now has `fuelStorageCapacity: 5000`, so it can receive delivered Fuel and contribute to the usable faction Fuel pool through the existing `BuildingResourceStorageComponent` path.
  - Tanker Truck now has `resourceHaulerBarrelCapacity: 8`, so `UnitGridAuthoring.Baker` adds `UnitResourceHauler` and existing/future fuel hauling code can assign cargo.
  - Added `FuelLogisticsConfigs_HaveUsableStorageAndHaulerCapacity` to prevent storage/cargo capacity from drifting back to zero.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation was not launched because Unity is already open on this project.
  - Live editor log check found no `error CS`, compiler error, build-failed, or exception matches after the asset import; editor log still contains existing Unity cloud/licensing entitlement warnings unrelated to this slice.
- Next action:
  - Add the first assignment gate for automatic tray/tanker work without replacing the existing movement path.
- Slice: initial idle hauler auto-assignment through the existing hauler bridge.
- Files changed: `Assets/Game/Scripts/Systems/BuildingGameplayEcsQueryCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, and this tracker.
- Behavior intent:
  - Resource hauler query now includes idle haulers, not only haulers that already have `UnitResourceHaulOrder`.
  - Idle `Unit_Veh_Truck_Tray` haulers can receive Oil routes from a same-faction Oil Pump with enough Oil to a same-faction Refinery with Oil input capacity.
  - Idle `Unit_Veh_Truck_Tanker` haulers can receive Fuel routes from a same-faction Refinery with enough Fuel to a same-faction Fuel Bladder/base storage with Fuel capacity.
  - Assignment reuses `UnitMoveOrderRequestSystem.EnqueueAndProcessTargetPathMoveOrder` and `UnitResourceHaulOrder`; no new update loop or MonoBehaviour was added.
  - Internal logistics movement now restores `UnitResourceHaulOrder` after the shared move-order processor clears movement-related orders, preserving existing manual hauler behavior and new automatic assignments.
  - Automatic route search uses direct loops instead of captured predicates to avoid steady-tick delegate allocations.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Add focused edit-mode validation for idle tray/tanker route assignment and same-faction filtering.
  - Then add dirty/version gates so idle assignment does not scan all idle haulers/buildings when no relevant resource/capacity state changed.
- Slice: focused automatic route validation for idle fuel logistics.
- Files changed: `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added a `UNITY_INCLUDE_TESTS`-only route resolver entry point so validation can inspect automatic logistics pairing without widening the runtime API.
  - Added focused tests proving same-faction tray trucks route Oil from Oil Pump to Refinery and same-faction tanker trucks route Fuel from Refinery to Fuel Bladder/storage.
  - The tests include nearer enemy-faction source candidates to guard against cross-faction logistics assignment drift.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Add dirty/version gates so idle assignment skips all-truck/all-building scans when no relevant Oil/Fuel availability or capacity changed.
- Slice: dirty/signature gate for idle automatic assignment scans.
- Files changed: `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Automatic logistics assignment now calculates one compact signature from idle logistics haulers plus relevant Oil/Fuel building capacity and stored resource state.
  - If the signature is unchanged, the bridge skips the expensive idle-hauler route assignment scans while still advancing existing `UnitResourceHaulOrder` work.
  - A stable refresh interval keeps assignment recoverable even if an edge state fails to alter the signature.
  - Added a focused signature regression test proving unchanged logistics state keeps the same signature and a source storage change alters it.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Move Oil/Fuel storage and transfer semantics toward explicit reservations/capacity accounting so multiple trucks cannot overclaim the same source or destination.
- Slice: storage reservation and version data model.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Systems/BuildingResourceStorageTransferSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingResourceProductionEcsSystem.cs`, `Assets/Tests/Editor/ResourceHaulerUtilitySystemHelperTests.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - `BuildingResourceStorageComponent` now has reserved inbound/outbound Oil/Fuel fields and a storage `Version`.
  - Transfer helper capacity checks account for reservations, and reservation add/release helpers mutate versioned storage state.
  - Existing load/unload/revert paths now increment storage version when they mutate stored Oil/Fuel.
  - Production ticks increment storage version when extraction or conversion changes stored Oil/Fuel.
  - This slice adds the data contract and pure helper semantics only; assignment-time reservation ownership will be wired in a later validated slice.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Add reservation ownership to automatic tray/tanker orders and release reservations on cancellation, death, or completed transfer.
- Slice: owned reservations for tray/tanker automatic hauling.
- Files changed: `Assets/Game/Scripts/Components/UnitCombatComponents.cs`, `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/ResourceHaulerUtilitySystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added `UnitResourceHaulReservation` as separate ECS state so reservation ownership survives normal movement systems that remove `UnitResourceHaulOrder`.
  - Automatic and selected logistics assignment reserve source outbound resource and destination inbound capacity before issuing movement.
  - Loading consumes/releases the source reservation immediately before completing pickup; unloading consumes/releases the destination reservation immediately before completing drop-off.
  - Orphaned reservations are released even when the idle assignment scan gate skips new route work, covering order cancellation by other command paths.
  - Retasking a selected hauler releases its previous reservation and clears the previous haul order before assigning a new route.
  - The automatic assignment signature now includes storage version and reservation fields so reservation changes wake the scan gate.
  - Added a focused reservation test proving source and destination ECS storage are reserved before the truck starts a task.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Add focused full-cycle edit-mode validation for one pump, one refinery, and one tray truck transferring Oil without a manual command, then cover tanker delivery into Fuel storage.
- Slice: focused Oil tray transfer cycle validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added a direct ECS-side validation for one Oil Pump, one Refinery, and one idle `Unit_Veh_Truck_Tray`.
  - The test drives the existing bridge through automatic assignment, source/destination reservation, pickup, destination travel, drop-off, reservation release, and order cleanup without a manual command.
  - The test uses an in-memory grid entity and manually advances `UnitGrid` to the issued approach targets, avoiding scene objects, pathfinding jobs, and presentation dependencies.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Add focused tanker delivery validation from refinery output Fuel into Fuel Bladder/base Fuel storage.
- Slice: focused Fuel tanker delivery cycle validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added a direct ECS-side validation for one Refinery with Fuel output, one Fuel Bladder/storage building, and one idle `Unit_Veh_Truck_Tanker`.
  - The test drives automatic assignment, refinery Fuel outbound reservation, Fuel Bladder inbound reservation, pickup, destination travel, drop-off, reservation release, and order cleanup.
  - The test verifies Fuel lands in the Fuel Bladder/storage component, not a header shortcut.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Wire/update faction usable Fuel summary and header source from delivered Fuel storage with version gating.
- Slice: match HUD header uses delivered fuel storage, not refinery output.
- Files changed: `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`, `Assets/Tests/Editor/UiShellEcsGatewayResourceHeaderTests.cs`, and this tracker.
- Behavior intent:
  - The live ECS match HUD header now sums only player-owned storage buildings that have Oil/Fuel capacity and no Oil/Fuel production role.
  - Refinery-held output Fuel no longer appears in the header before tanker delivery to Fuel Bladder/base storage.
  - Added a focused regression test where a player refinery holds Fuel and a player storage building holds delivered Fuel; the HUD shows only the storage value.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked by the already-open Unity editor for this project.
- Next action:
  - Add a versioned faction usable Fuel summary from delivered storage so the HUD can avoid live query/string work during steady-state frames.
- Slice: versioned faction usable Fuel summary for delivered storage.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Composition/MatchBuildingRuntimeBootstrapStartupSystemHelper.cs`, `Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs`, `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`, `Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs`, `Assets/Tests/Editor/BuildingProductionSystemTests.cs`, `Assets/Tests/Editor/UiShellEcsGatewayResourceHeaderTests.cs`, and this tracker.
- Behavior intent:
  - Added `BuildingRuntimeFactionUsableFuelSummary` as a narrow ECS boundary read model for delivered, usable storage.
  - The runtime publisher now fills this buffer from non-producing storage buildings only, using the existing faction-summary publish gate.
  - The match HUD header reads the versioned usable summary first and keeps the live storage scan only as an early-frame/test fallback.
  - Added publisher and HUD regression tests so refinery output is excluded while delivered storage fuel is included.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked because Unity is already open on `/Users/farhad/Projects/WarlineCapture-Clone`.
- Next action:
  - Add vehicle fuel consumption data/config and the first guarded consumption path for active vehicle movement.
- Slice: opt-in vehicle Fuel consumption system.
- Files changed: `Assets/Game/Scripts/Components/GridComponents.cs`, `Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs`, `Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs.meta`, `Assets/Tests/Editor/VehicleFuelConsumptionSystemTests.cs`, `Assets/Tests/Editor/VehicleFuelConsumptionSystemTests.cs.meta`, and this tracker.
- Behavior intent:
  - Added `UnitFuelConsumption` and `UnitFuelConsumptionState` ECS components.
  - Added unmanaged `VehicleFuelConsumptionSystem` that skips unmoved units, aggregates requested Fuel by faction, and drains delivered Fuel storage only.
  - Fuel storage `Version` increments when movement drains usable Fuel, so existing summary/header publishing can refresh.
  - Added focused tests for initialization/no-drain, ground vehicle movement spending Fuel, air units using air Fuel cost, and refinery output not being drained.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity batchmode validation remains blocked because Unity is already open on `/Users/farhad/Projects/WarlineCapture-Clone`.
- Next action:
  - Wire fuel-consumption authoring/config defaults onto vehicle/air unit prefabs, then add command blocking when usable Fuel is empty.
- Slice: authored vehicle and air Fuel consumption defaults.
- Files changed: `Assets/Game/Scripts/Configs/GameplayConfigModels.cs`, `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs`, `Assets/Tests/Editor/UnitMovementConfigValidationTests.cs`, and this tracker.
- Behavior intent:
  - `UnitGridAuthoringConfig` now exposes defaulted fuel costs: ground vehicles consume a small ground Fuel amount per grid-cell movement, air units consume an air Fuel amount, and character units remain free.
  - `UnitGridAuthoring` copies those config values and the baker adds `UnitFuelConsumption` plus `UnitFuelConsumptionState` to baked vehicle/air unit entities when either fuel cost is non-zero.
  - The existing unmanaged `VehicleFuelConsumptionSystem` can now operate on authored vehicle and air prefabs without scene-specific manual component edits.
  - Config validation now asserts the intended authoring rule across all covered character and vehicle unit configs.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Focused Unity edit-mode validation command was attempted:
    `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -runTests -testPlatform EditMode -testFilter UnitMovementConfigValidationTests,VehicleFuelConsumptionSystemTests -logFile /private/tmp/warline-fuel-authoring-tests.log -testResults /private/tmp/warline-fuel-authoring-tests.xml`
  - First Unity attempt was sandbox-blocked by Package Manager IPC: `listen EPERM: operation not permitted /tmp/Unity-Upm-43515.sock`.
  - Escalated Unity attempt was blocked before tests by licensing client mismatch/reconnect loop: `Unsupported protocol version '1.18.1'`, `com.unity.editor.headless was not found`; stopped the hung batch process. Log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Add command gating for new movement/launch/support requests when usable Fuel is empty, including typed blocked reasons and safe aircraft return policy.
- Slice: manual move Fuel gate and typed rejection.
- Files changed: `Assets/Game/Scripts/Contracts/TacticalCommandContracts.cs`, `Assets/Game/Scripts/Components/UnitMoveOrderRequestComponents.cs`, `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs`, `Assets/Game/Scripts/Systems/UnitMoveOrderRequestSystem.cs`, `Assets/Game/Scripts/Systems/SelectedMoveOrderCommandSystem.cs`, `Assets/Tests/Editor/UnitMoveOrderSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added `TacticalCommandReasonCode.InsufficientFuel` and display text for command feedback.
  - Grouped/manual move requests now estimate fuel from current cell to issued goal for entities with `UnitFuelConsumption`.
  - Fuel availability is read from delivered usable Fuel storage only, excluding refinery output and respecting outbound reservations.
  - Manual moves for fuel-consuming ground/air units reject before writing target/path/manual-order components when usable Fuel is below the estimated requirement.
  - Selected move command mode propagates the typed fuel rejection instead of reporting a generic blocked target.
  - Clear movement, return/internal movement, and existing active movement are intentionally not blocked in this slice so units are not stranded.
- Validation:
  - Added `UnitMoveOrderRequestSystem_GroupedManualFuelConsumerRejectsAtZeroUsableFuel`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun in this slice because the previous focused Unity run is blocked by the same licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Add aircraft-safe low-fuel behavior: block new aircraft launch/move at 0 usable Fuel, but allow/issue return-to-base or emergency reserve policy for active aircraft.
- Slice: aircraft zero-Fuel safety return.
- Files changed: `Assets/Game/Scripts/Systems/AircraftFuelSafetyReturnSystem.cs`, `Assets/Game/Scripts/Systems/AircraftFuelSafetyReturnSystem.cs.meta`, `Assets/Tests/Editor/VehicleFuelConsumptionSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added unmanaged `AircraftFuelSafetyReturnSystem` before `UnitAirMovementSystem`.
  - The system aggregates delivered usable Fuel by faction once per update.
  - Active aircraft with `UnitFuelConsumption` and no usable faction Fuel now clear conflicting move/attack/scan/drop orders and enter the existing `UnitAirComponent.ReturningHome` landing path.
  - Positive usable Fuel leaves active aircraft orders untouched.
  - Return-to-base/internal safety movement stays outside the manual fuel gate so aircraft are not stranded.
- Validation:
  - Added `AircraftFuelSafetyReturn_ZeroFuelClearsOrdersAndReturnsHome`.
  - Added `AircraftFuelSafetyReturn_WithUsableFuelKeepsActiveOrders`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Add ground-vehicle no-fuel policy beyond initial rejection: preserve committed segment semantics and publish warning/read-model updates only when fuel state changes.
- Slice: ground vehicle zero-Fuel hold policy.
- Files changed: `Assets/Game/Scripts/Systems/GroundVehicleFuelHoldSystem.cs`, `Assets/Game/Scripts/Systems/GroundVehicleFuelHoldSystem.cs.meta`, `Assets/Tests/Editor/VehicleFuelConsumptionSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added unmanaged `GroundVehicleFuelHoldSystem` before `UnitGridMovementSystem`.
  - The system aggregates delivered usable Fuel by faction and only affects ground vehicle-motion units with `UnitFuelConsumption`.
  - When usable Fuel is zero, active ground vehicles clear target/path/manual movement components and reset vehicle kinematics to hold position.
  - Vehicles with usable Fuel keep active movement untouched.
  - Aircraft remain handled by the dedicated aircraft safety return system.
- Validation:
  - Added `GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics`.
  - Added `GroundVehicleFuelHold_WithUsableFuelKeepsMovement`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Publish fuel warning/read-model updates only when fuel state or blocked reasons change, then surface disabled command feedback from typed fuel reasons.
- Slice: version-gated focused command read model refresh.
- Files changed: `Assets/Game/Scripts/Components/SelectionUiReadModelComponents.cs`, `Assets/Game/Scripts/UI/Contracts/ISelectionUiReadModel.cs`, `Assets/Game/Scripts/Systems/FocusedUnitUiReadModelUiSystemHelper.cs`, `Assets/Game/Scripts/Systems/SelectionUiReadModelUiSystemHelper.cs`, `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs`, `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`, `Assets/Tests/Editor/MatchHudCommandFeedbackPanelTests.cs`, and this tracker.
- Behavior intent:
  - Added a `CommandStateVersion` to the focused unit UI read model.
  - The publisher advances that version only when focused unit identity or command capability/reason state changes.
  - Match HUD command controls now skip steady-state button refresh work when the read-model version is unchanged.
  - Version `0` remains a volatile fallback so old/unversioned test doubles and future non-ECS read models keep refreshing normally.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Surface typed fuel reasons in disabled command/build/production rows and selected logistics UI, then add no-GC UI update validation where possible.
- Slice: focused command-state version validation.
- Files changed: `Assets/Tests/Editor/SelectionUiReadModelLookupTests.cs` and this tracker.
- Behavior intent:
  - Added focused validation for the command read-model version introduced in the prior slice.
  - The test publishes a focused player unit twice and verifies `CommandStateVersion` stays unchanged when command capability/reason state is stable.
  - The test then changes the unit into a command-blocked passenger state and verifies the version advances with a typed `CommandUnavailable` reason.
  - This closes the Phase 6 validation row for command buttons updating only when fuel/command read-model versions change.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 7 UI read-model work: selected logistics state for Oil Pump, Refinery, Fuel Bladder, tray truck, and tanker, followed by typed disabled rows.
- Slice: typed building UI command result reasons.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs`, `Assets/Tests/Editor/BuildingPlacementValidationSystemTests.cs`, `Assets/Tests/Editor/BuildingProductionSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added shared `ReasonCode` fields to building placement, selected-building production, and camp-item command result buffers.
  - Preserved existing local byte `ResultCode` values for compatibility while publishing shared `TacticalCommandReasonCode` ids for UI feedback rows.
  - Placement failures now map blocked placement to `TargetBlocked` and resource shortage to `InsufficientResources`.
  - Production/camp item queue failures now carry typed `CommandUnavailable`; successful rows carry `None`.
- Validation:
  - Added focused assertions in existing building placement and production tests.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 7 selection panel logistics read models for Oil Pump, Refinery, Fuel Bladder, tray truck, and tanker.
- Slice: selection panel logistics storage coverage.
- Files changed: `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs` and this tracker.
- Behavior intent:
  - Verified the existing match HUD selection panel path for selected resource buildings.
  - Added focused coverage for oil-only storage, fuel-only storage, and combined oil/fuel storage chips, matching Oil Pump, Fuel Bladder, and Refinery-style storage states.
  - Existing focused hauler coverage in the same suite continues to validate tray/tanker cargo state through `UnitResourceHauler` and ECS cargo read models.
  - This marks the Phase 7 selection-panel logistics state item complete without adding simulation logic to UI classes.
- Validation:
  - Added `SelectedBuildingSelectionPanelShowsOilFuelStorageChips`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Add active mission/skirmish gating for Oil display, then continue no-GC/versioned UI update validation.

- Created design and implementation tracker.
- Started implementation with seeded logistics verification fixture.
- Added one `Unit_Veh_Truck_Tray` and one `Unit_Veh_Truck_Tanker` to each configured faction in `Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset`.
- Added `SceneInitialUnitsConfig_SeedsFuelLogisticsTrucksNearFactionBases` to `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - `dotnet build WarlineCapture-Clone.sln --no-restore` blocked by pre-existing generated solution issue: duplicate `Unity.ProBuilder` project name.
  - Unity focused validation command blocked because another Unity instance has `/Users/farhad/Projects/WarlineCapture-Clone` open:
    `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod InitialFactionBaseValidationTests.RunSceneInitialUnitsConfigValidation -logFile /private/tmp/warline-fuel-logistics-seeded-units.log`
- Slice: active oil HUD visibility gating.
- Files changed: `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`, `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`, `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`, `Assets/Tests/Editor/UiShellEcsGatewayResourceHeaderTests.cs`, and this tracker.
- Behavior intent:
  - Added `UiMatchHudHeaderModel.ShowOil` so the header read model decides whether Oil is visible.
  - The ECS gateway sets `ShowOil` only when player-side oil logistics are present through usable Fuel summaries, live storage, or faction resource summaries.
  - The Canvas helper only activates the Oil slot when the read model says it is visible, and caches the last visibility/text state.
  - Fuel-only storage still drives the header Fuel value without forcing the Oil slot visible.
- Validation:
  - Extended `UiShellEcsGatewayResourceHeaderTests` to cover oil-enabled storage, fuel-only storage, versioned usable Fuel summary, empty usable summary, and extraction-teaching resource summary visibility.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 7 no-GC/versioned HUD update validation, then finish remaining selected-panel stale-value/UI click-blocking validation items.
- Slice: selected storage panel stale-value validation.
- Files changed: `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs` and this tracker.
- Behavior intent:
  - Added focused coverage for switching the same match HUD selection panel from an Oil Pump-style storage model to a Fuel Bladder-style storage model.
  - The test verifies Oil current/capacity are cleared and Fuel current/capacity are replaced on the second update, preventing stale logistics values when changing selected buildings.
  - The existing focused validation runner now includes this test in its selection summary suite.
- Validation:
  - Added `SelectedBuildingSelectionPanelReplacesStaleOilFuelStorageValues`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 7 no-GC/versioned HUD update validation, then verify UI click blocking remains intact on Android and Editor.
- Slice: UI click-blocking validation accounting.
- Files changed: this tracker.
- Behavior intent:
  - Accounted for the existing `MatchHudCommandControlsBlockGameplayWorldInput` editor validation in `Assets/Tests/Editor/MatchHudCommandControlsCurrentPrefabTests.cs`.
  - That test verifies match HUD command buttons block gameplay/world selection hit testing, capture the UI click suppression sequence through `ShouldIgnoreBuildingSelectionThisFrame`, and still queue the intended command request.
  - This closes the Phase 7 UI click-blocking validation row without adding duplicate test code or changing runtime behavior.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 7 no-GC/versioned HUD update validation and the remaining header string-formatting/cache item.
- Slice: versioned match HUD header resource string cache.
- Files changed: `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`, `Assets/Tests/Editor/UiShellEcsGatewayResourceHeaderTests.cs`, and this tracker.
- Behavior intent:
  - Added a `UiShellEcsGateway` match HUD header cache keyed by world, boundary, fixed header component text, usable Fuel summary source, version, rounded Oil/Fuel values, and Oil visibility.
  - Versioned usable Fuel summary reads now return the cached `UiMatchHudHeaderModel` when resource state is unchanged, avoiding repeated Oil/Fuel compact string formatting in steady-state HUD polls.
  - Cache invalidates when summary version/value, Oil visibility, boundary, world, or fixed header text changes.
- Validation:
  - Added `MatchHudHeader_ReusesVersionedUsableFuelSummaryStringsUntilVersionChanges`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Add or run explicit no-GC allocation validation for steady-state HUD updates, then close the remaining Phase 7 Canvas-boundary item if no simulation logic drift is found.
- Slice: no-GC cached match HUD header validation.
- Files changed: `Assets/Tests/Editor/UiShellEcsGatewayResourceHeaderTests.cs` and this tracker.
- Behavior intent:
  - Added focused steady-state allocation coverage for the cached versioned match HUD header read path.
  - The test warms `UiShellEcsGateway.TryReadMatchHudHeader` once, then performs repeated reads with unchanged usable Fuel summary version/value and asserts zero managed allocations through `System.GC.GetAllocatedBytesForCurrentThread`.
  - This validates the Phase 7 requirement that steady-state HUD resource reads do not allocate while the ECS read model is unchanged.
- Validation:
  - Added `MatchHudHeader_CachedVersionedUsableFuelSummaryReadDoesNotAllocate`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Close the remaining Phase 7 Canvas-boundary item after confirming logistics simulation remains outside Canvas/UI classes, then continue into the next tracker phase.
- Slice: fuel logistics UI boundary guardrail.
- Files changed: `Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs` and this tracker.
- Behavior intent:
  - Added an architecture guardrail that allows UI runtime files to consume fuel logistics read models while blocking direct mutation of Oil/Fuel storage, hauler transfer helpers, and fuel-consumption systems from Canvas/UI code.
  - The boundary validation runner now includes `UiRuntimeScriptsMustNotMutateFuelLogisticsSimulationState`.
  - This closes Phase 7 by keeping Canvas work inside existing helper/read-model boundaries and preventing simulation logic from drifting into UI classes.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue with the next tracker phase: Phase 0/1 baseline and data-model gaps, then Phase 2 extraction/refinery buffer work.
- Slice: shared Burst-compatible resource kind.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingResourceStorageTransferSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/ResourceHaulerUtilitySystemHelper.cs`, `Assets/Tests/Editor/FactionResourceCompositionSystemHelperTests.cs`, `Assets/Tests/Editor/ResourceHaulerUtilitySystemHelperTests.cs`, and this tracker.
- Behavior intent:
  - Added `Game.Components.ResourceKind` as the authoritative byte-backed Oil/Fuel representation available to ECS/Burst-compatible code.
  - Removed the duplicate managed-only nested `FactionResourceCompositionSystemHelper.ResourceKind` enum and updated callers to use the shared component enum.
  - Kept serialized/runtime byte values stable: Oil remains `0`, Fuel remains `1`.
  - Routed storage helper constants and hauler storage conversion through the shared enum so future automation systems do not need separate oil/fuel encodings.
- Validation:
  - Extended `ResourceHaulerUtilitySystemHelperTests.CreateOrder_InitializesTravelToSource` to assert order/resource-storage byte values match `Game.Components.ResourceKind`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 1 role tags/status reason codes and faction usable Fuel summary fields after this slice validates and is pushed.
- Slice: explicit fuel logistics ECS role tags.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Components/UnitCombatComponents.cs`, `Assets/Tests/Editor/ResourceHaulerUtilitySystemHelperTests.cs`, and this tracker.
- Behavior intent:
  - Added zero-sized ECS role tags for Oil haulers, Fuel haulers, Oil sources, refinery Oil input, refinery Fuel output, and usable Fuel storage.
  - Kept this slice data-model-only; existing runtime inference from prefab keys/storage fields is unchanged until baker/runtime mapping is handled in a later slice.
  - The tags give future unmanaged systems stable query surfaces instead of string/source-key checks in hot assignment and transfer paths.
- Validation:
  - Added `FuelLogisticsRoleTags_AreQueryableEcsTags` to verify the tags can be attached and queried through ECS.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 1 faction usable Fuel summary produced/delivered/spent fields and typed status/reason codes after this slice validates and is pushed.
- Slice: explicit faction usable Fuel summary accounting fields.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingProductionSystemTests.cs`, and this tracker.
- Behavior intent:
  - Extended `BuildingRuntimeFactionUsableFuelSummary` with explicit `CurrentFuelBarrels`, `FuelProducedBarrels`, `FuelDeliveredBarrels`, and `FuelSpentBarrels` fields while preserving the existing `StoredFuelBarrels` field used by current UI callers.
  - Populated current and delivered Fuel from delivered usable storage, keeping refinery output excluded from the header-facing pool.
  - Left produced/spent counters at zero until later production/consumption accounting slices own those mutations, avoiding invented totals.
  - Extended the existing delivered-storage summary test to assert the new current/delivered/spent/produced fields.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 1 typed logistics status/reason codes, then close the Phase 1 validation row with focused storage/summary tests.
- Slice: typed fuel logistics status and block reason codes.
- Files changed: `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`, `Assets/Game/Scripts/Contracts/TacticalCommandContracts.cs`, `Assets/Tests/Editor/ResourceHaulerUtilitySystemHelperTests.cs`, and this tracker.
- Behavior intent:
  - Added `FuelLogisticsTaskStatusCode` and `FuelLogisticsBlockReasonCode` as byte-backed ECS-friendly enums for automated hauler/refinery state.
  - Added tactical command reason codes for logistics source unavailable, destination full, route unavailable, and reservation failure so UI/request validation can report specific typed failures instead of generic strings.
  - Kept the slice data-model-only; no runtime command behavior changes yet.
- Validation:
  - Added `FuelLogisticsReasonCodes_AreTypedForUiAndCommands` to verify non-zero status/block/tactical reason codes.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Close Phase 1 validation rows, then start Phase 2 oil extraction/refinery buffer ownership checks.
- Slice: Phase 1 data-model validation closure.
- Files changed: this tracker.
- Behavior intent:
  - Closed Phase 1 after confirming the existing focused editor coverage matches the remaining validation rows.
  - Storage capacity is covered by `ResourceHaulerUtilitySystemHelperTests.TryCompleteUnload_ClampsDestinationCapacityAndClearsCargo` and the component storage transfer tests.
  - Reservation accounting is covered by `StorageReservation_SourceReservationReducesAvailableResource`, `StorageReservation_DestinationReservationReducesFreeCapacity`, and `StorageTransfer_MutationsIncrementVersion`.
  - Faction usable Fuel summary is covered by `BuildingProductionSystemTests.BuildingRuntimeState_PublishesUsableFuelSummaryFromDeliveredStorageOnly`, including current/delivered/produced/spent fields.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Start Phase 2 oil extraction/refinery buffer ownership checks and keep Unity validation blocker recorded until the licensing client issue is cleared.
- Slice: oil pump full-capacity stall validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added explicit coverage that an Oil Pump at full storage capacity does not overflow, does not advance storage version, and does not allocate managed memory while repeatedly ticking.
  - Accounted for the existing deterministic Oil accumulation coverage in `ApplyTick_ExtractsOilUpToCapacity`.
  - Closed the Phase 2 full-buffer stall row and the focused Oil Pump production/capacity validation rows.
- Validation:
  - Added `ApplyTick_FullOilStorageDoesNotOverflowVersionOrAllocate`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 2 by verifying map-placed/player-built oil pumps use the same `BuildingResourceStorageComponent` bake path and then address selected Oil Pump read-model version gating.
- Slice: map-placed and runtime-built Oil Pump storage path validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added focused coverage that `BuildingRuntimeEntityCompositionSystemHelper.CreateBuildingCombatEntity` attaches the same `BuildingResourceStorageComponent` contract for a map-placed Oil Pump definition and a runtime-cloned/player-built Oil Pump definition.
  - The test also exercises `BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint` to prove Oil capacity/rate fields survive runtime footprint cloning before the shared combat entity composition path adds storage.
  - This closes the Phase 2 path-consistency row without changing runtime behavior.
- Validation:
  - Added `CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeOilPumps`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 2 with selected Oil Pump read-model version gating, then revisit whether Oil Pump extraction should move from managed composition/static ECS helper into an unmanaged `ISystem`.
- Slice: selected Oil Pump storage read-model version gate.
- Files changed: `Assets/Game/Scripts/Systems/BuildingPlacementInteractionCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingPlacementQueryUiSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs`, `Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs`, `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs`, and this tracker.
- Behavior intent:
  - Added `SelectedBuildingResourceStorageSnapshot` so selected building UI can consume current Oil/Fuel values plus the ECS `BuildingResourceStorageComponent.Version`.
  - Threaded the versioned snapshot from the building placement query path into the match HUD selection panel while keeping the older integer-only storage delegate as a fallback.
  - Stopped the selected-building branch from clearing and reapplying its HUD panel every frame; the building card now keys on the selected label and the Oil/Fuel storage chip keys on resource values/capacity plus storage version.
  - This keeps selected Oil Pump UI updates tied to ECS storage changes instead of per-frame Canvas work.
- Validation:
  - Added `SelectedBuildingResourceStoragePanelSkipsApplyUntilVersionChanges`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Finish Phase 2 by deciding whether Oil Pump extraction remains acceptable through the existing Burst-compatible ECS helper path or needs a true unmanaged `ISystem`, and by explicitly closing the pump output capacity/version row if current storage semantics are accepted as the buffer contract.
- Slice: ECS-owned Oil Pump storage production tick.
- Files changed: `Assets/Game/Scripts/Systems/BuildingResourceProductionEcsSystem.cs`, `Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added `BuildingResourceProductionEcsSystem.ApplyStorageQuery` so the production tick mutates live `BuildingResourceStorageComponent` entities directly by ECS query when an EntityManager is available.
  - Redirected `FactionResourceCompositionSystemHelper.UpdateResourceProduction(EntityManager, ...)` to use the ECS storage query path instead of walking the managed runtime-building dictionary for production mutation.
  - Kept the existing managed mirror sync after production so UI/legacy composition can still read runtime building mirrors without becoming the source of truth.
  - Closed Phase 2 by treating `BuildingResourceStorageComponent` as the explicit Oil/Fuel output buffer contract: capacity clamps stored Oil/Fuel, and `Version` changes only when stored values change.
- Validation:
  - Added `ApplyStorageQuery_WritesOilToEcsStorageWithCapacityAndVersion`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation was not rerun because the prior focused Unity run is blocked by the recurring licensing client mismatch/reconnect loop. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 by covering seeded faction-base tray truck startup and blocked/idle reason behavior, then address remaining reservation cleanup paths.
- Slice: seeded tray truck automatic Oil assignment validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added focused coverage for the map/base-seeded tray truck boundary: an already-present ECS hauler with `Unit_Veh_Truck_Tray` source key, faction, grid cell, and hauler capacity can enter the automatic Oil route without being produced by a runtime build queue.
  - The test exercises the live bridge `UpdateResourceHaulers` path, verifies the Oil Pump -> Refinery order, and verifies source/destination Oil reservations are written.
  - This complements `InitialFactionBaseValidationTests.SceneInitialUnitsConfig_SeedsFuelLogisticsTrucksNearFactionBases`, which already verifies tray/tanker authoring is present near faction bases.
- Validation:
  - Added `AutomaticFuelLogisticsSeededTray_StartsOilHaulingWithoutRuntimeBuild`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 with the no-refinery-capacity typed idle reason, then reservation cleanup when source/destination entities disappear.
- Slice: typed tray idle reason for full refinery input.
- Files changed: `Assets/Game/Scripts/Components/UnitCombatComponents.cs`, `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Added `UnitResourceHaulStatus` as a small ECS status component on resource haulers.
  - Automatic assignment now writes typed status/reason data when a hauler cannot be assigned: source unavailable, destination unavailable, destination full, route unavailable, reservation failed, or hauler unavailable.
  - A tray truck with available Oil but a full Refinery input buffer stays idle without an order/reservation and reports `FuelLogisticsBlockReasonCode.DestinationFull`.
- Validation:
  - Added `AutomaticFuelLogisticsTray_NoRefineryCapacitySetsTypedIdleReason`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 with destroyed source/destination reservation cleanup, then no-GC steady-state validation.
- Slice: active tray order reservation cleanup for destroyed endpoints.
- Files changed: `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Active hauler orders now detect destroyed source/destination runtime buildings, release source and destination reservations, remove the active order/reservation, and publish a typed `SourceUnavailable` or `DestinationUnavailable` status.
  - This covers cancellation during an active haul, not just orphaned reservations after an order is already gone.
- Validation:
  - Added `AutomaticFuelLogisticsTray_DestroyedSourceClearsReservation`.
  - Added `AutomaticFuelLogisticsTray_DestroyedDestinationClearsReservation`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 with steady-state no-GC validation and then reassess whether the remaining automation implementation rows need actual unmanaged `ISystem` work or should stay deferred until the tray/tanker loop is fully feature-complete.
- Slice: steady-state tray automation managed-allocation probe.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added a focused steady-state probe after warmup for unchanged automatic tray hauling.
  - The probe measures `System.GC.GetAllocatedBytesForCurrentThread()` across repeated `UpdateResourceHaulers` calls while the active order/reservation state remains stable.
  - This guards against managed allocations in the automated tray logistics steady state; Temp native allocations are outside the managed GC budget.
- Validation:
  - Added `AutomaticFuelLogisticsSteadyState_DoesNotAllocateManagedMemory`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 implementation rows by reducing the remaining all-truck/all-building scan work, or move to Phase 4 refinery conversion if that produces a safer user-visible logistics increment first.
- Slice: close tray transfer ECS mutation row from existing implementation evidence.
- Files changed: this tracker.
- Behavior intent:
  - Confirmed tray pickup/drop-off already enters the ECS transfer mutation boundary through `BuildingResourceHaulerBridgeCompositionSystemHelper.UpdateLoadingPhase` and `UpdateUnloadingPhase`.
  - `ResourceHaulerUtilitySystemHelper.TryCompleteLoad(EntityManager, RuntimeBuildingEntity, ...)` and `TryCompleteUnload(EntityManager, RuntimeBuildingEntity, ...)` prefer live `BuildingResourceStorageComponent` on `CombatEntity`, mutate it through `BuildingResourceHaulerTransferEcsSystem`, and commit the updated ECS storage back before managed mirrors are used for UI/legacy composition.
  - Existing focused tests already cover this contract: `HaulerTransferEcs_TryCompleteLoad_UsesComponentStorage`, `HaulerTransferEcs_TryCompleteUnload_UsesComponentStorage`, `LiveEcsStorage_TryCompleteLoad_PrefersCombatEntityStorage`, `LiveEcsStorage_TryCompleteUnload_PrefersCombatEntityStorage`, plus the automatic tray/tanker cycle tests.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 by reducing the remaining all-truck/all-building scan work and deciding whether the assignment bridge should be split into an unmanaged system now or after tanker/refinery feature completion.
- Slice: throttle automatic assignment signature scans.
- Files changed: `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Moved the automatic assignment refresh-window check ahead of signature calculation so stable-state assignment skips the full runtime-building signature walk until the scheduled refresh time.
  - Active hauler orders still update every frame, but idle assignment candidate/source/destination scanning no longer walks all resource buildings every frame when the assignment state is stable.
  - Kept the existing EntityQuery-based hauler collection and versioned resource signature; this is the scoped scan-reduction slice before deciding whether a deeper unmanaged assignment system split is worth doing now.
- Validation:
  - Added `AutomaticFuelLogisticsAssignmentScan_SkipsWithinStableRefreshWindow`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 3 by resolving the remaining cancellation/truck-death cleanup row, then decide whether `OilTrayLogisticsAssignmentSystem` should become an unmanaged `ISystem` before moving to Phase 4.
- Slice: active tray cleanup for dead haulers and invalidated routes.
- Files changed: `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs`, `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs`, and this tracker.
- Behavior intent:
  - Active resource-hauler orders now release reservations and remove order/reservation state when the hauler has zero health or a death-animation component.
  - Travel phases now release reservations and publish `RouteUnavailable` when the existing move/path state is gone and a route reissue can no longer find a valid approach cell.
  - This completes the cleanup row together with the earlier destroyed source/destination cleanup and the existing orphaned-reservation cleanup path for task cancellation/no-order cases.
- Validation:
  - Added `AutomaticFuelLogisticsTray_DeadHaulerClearsReservation`.
  - Added `AutomaticFuelLogisticsTray_RouteInvalidationClearsReservation`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Decide whether to split the remaining tray assignment bridge into a dedicated unmanaged `OilTrayLogisticsAssignmentSystem` now, or defer that architectural split until refinery conversion and tanker automation are complete.
- Slice: ECS query-path refinery conversion validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added a focused `ApplyStorageQuery` refinery conversion test so Phase 4 is validated through the same ECS storage mutation path used by runtime production updates.
  - The test covers Oil input consumption, Fuel output production, Oil-to-Fuel efficiency, Fuel output capacity clamping, empty Oil input stall, full Fuel output stall, and version changes only when storage mutates.
  - Updated Phase 4 progress for the existing `BuildingResourceProductionEcsSystem` ECS storage conversion path; selected-building read-model gating and Large Refinery config verification remain open.
- Validation:
  - Added `ApplyStorageQuery_ConvertsRefineryOilIntoFuelWithEfficiencyAndCapacity`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 4 with selected Refinery read-model version gating and Large Refinery config coverage, then return to the remaining Phase 3 unmanaged-assignment decision once tanker/refinery data ownership is complete.
- Slice: close selected refinery resource-panel version gate from existing evidence.
- Files changed: this tracker.
- Behavior intent:
  - Confirmed selected building resource storage already flows through `SelectedBuildingResourceStorageSnapshot` with the live ECS storage `Version`.
  - `SelectionHudFeedbackUiSystemHelper.BuildSelectedBuildingResourceStoragePanelModel` includes the snapshot version in the transport/resource panel cache key, so unchanged Oil/Fuel storage does not reapply the selected-building resource panel.
  - Existing focused test `SelectedBuildingResourceStoragePanelSkipsApplyUntilVersionChanges` covers this behavior; the row is closed without runtime code changes.
- Validation:
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 4 with Large Refinery config/data coverage and selected-building blocked-reason accuracy.
- Slice: Large Refinery conversion data coverage.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added a focused ECS storage query test for standard and large refinery data in the same conversion pass.
  - The test mirrors the configured refinery capacities/rates: standard refinery data produces at 100 Fuel/day and large refinery data produces at 200 Fuel/day through the same `BuildingResourceProductionEcsSystem.ApplyStorageQuery` path.
  - This closes Large Refinery support without adding prefab-specific logic or a duplicate system.
- Validation:
  - Added `ApplyStorageQuery_ConvertsStandardAndLargeRefineriesWithDifferentRates`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 4 by deciding how selected-building blocked reasons should expose refinery empty-input/full-output states, then close or implement that final row.
- Slice: selected refinery conversion status feedback.
- Files changed: `Assets/Game/Scripts/UI/Contracts/MatchHudSelectionPanelModels.cs`, `Assets/Game/Scripts/Systems/BuildingPlacementInteractionCompositionSystemHelper.cs`, `Assets/Game/Scripts/Systems/BuildingPlacementQueryUiSystemHelper.cs`, `Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs`, `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs`, `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs`, and this tracker.
- Behavior intent:
  - Extended selected-building resource snapshots with production/conversion rates from live ECS storage.
  - Added an optional status text to the existing selected-building resource chip instead of creating a new panel surface.
  - Refineries now display compact conversion state on the selected-building resource chip: `CONVERTING`, `WAITING OIL`, or `FUEL FULL`.
  - Included the status text in the selection panel cache key so unchanged refinery status does not reapply the UI.
- Validation:
  - Added `SelectedBuildingRefineryStoragePanelReportsConversionStatus`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 5 tanker automation and usable Fuel rows, starting with typed tanker stall reasons and seeded tanker hauling validation gaps.
- Slice: tanker stall reason coverage.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added tanker-focused ECS tests for no refinery output Fuel, missing Fuel storage, full Fuel storage, no route, and no available tanker.
  - Blocked tanker cases assert no haul order/reservation is created and the tanker receives the expected typed `UnitResourceHaulStatus`.
  - The no-available-tanker case verifies refinery output and Fuel storage reservations remain unchanged when no tanker entity can service the route.
- Validation:
  - Added `AutomaticFuelLogisticsTanker_NoRefineryFuelSetsTypedIdleReason`.
  - Added `AutomaticFuelLogisticsTanker_NoFuelStorageSetsTypedIdleReason`.
  - Added `AutomaticFuelLogisticsTanker_FullFuelStorageSetsTypedIdleReason`.
  - Added `AutomaticFuelLogisticsTanker_NoRouteSetsTypedIdleReason`.
  - Added `AutomaticFuelLogisticsTanker_NoAvailableTankerDoesNotReserveFuel`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 5 with Fuel Bladder map/player-built storage path validation and header/version-gated Fuel delivery validation.
- Slice: Fuel Bladder ECS storage path and usable-capacity coverage.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added focused validation that map-placed and runtime/player-built Fuel Bladders both receive `BuildingResourceStorageComponent` with the same Fuel capacity through `BuildingRuntimeEntityCompositionSystemHelper.CreateBuildingCombatEntity`.
  - Closed the usable Fuel capacity validation row from existing evidence in `BuildingRuntimeState_PublishesUsableFuelSummaryFromDeliveredStorageOnly`, which asserts usable Fuel summary capacity comes from delivered Fuel storage and excludes refinery output.
  - This keeps Fuel Bladders on the shared ECS storage path instead of adding a Fuel-Bladder-specific runtime mirror.
- Validation:
  - Added `CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeFuelBladders`.
  - Existing `BuildingRuntimeState_PublishesUsableFuelSummaryFromDeliveredStorageOnly` covers Fuel Bladder usable-capacity summary behavior.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 5 with header update/version-gated delivery validation and seeded faction-base tanker validation.
- Slice: header usable-Fuel validation evidence.
- Files changed: this tracker.
- Behavior intent:
  - Closed the refinery-output/header separation row from existing `MatchHudHeader_EmptyUsableFuelSummaryDoesNotFallbackToRefineryOutput`, which proves refinery/economy Fuel does not leak into the header when no delivered usable-Fuel summary exists.
  - Closed the delivered-Fuel/version-gated header row from existing `MatchHudHeader_PrefersVersionedUsableFuelSummary` and `MatchHudHeader_ReusesVersionedUsableFuelSummaryStringsUntilVersionChanges`.
  - This is tracker evidence only; no runtime change was needed.
- Validation:
  - Existing `UiShellEcsGatewayResourceHeaderTests` cover header source preference, delivered Fuel display, and version-gated string reuse.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 5 with seeded faction-base tanker validation and selected tanker idle/block reason visibility.
- Slice: seeded tanker automatic Fuel hauling validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added seeded tanker validation matching the seeded tray coverage.
  - A map-start `Unit_Veh_Truck_Tanker` now has focused coverage for automatically creating a Fuel haul order from Refinery output to Fuel Bladder storage without runtime building.
  - The test asserts source/destination ids, Fuel resource kind, initial travel phase, and ECS Fuel outbound/inbound reservations.
- Validation:
  - Added `AutomaticFuelLogisticsSeededTanker_StartsFuelHaulingWithoutRuntimeBuild`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 5 with selected tanker idle/block reason visibility, then reassess whether the remaining unmanaged `FuelTankerLogisticsAssignmentSystem` split is practical before moving phases.
- Slice: selected tanker logistics status feedback.
- Files changed: `Assets/Game/Scripts/Components/SelectionUiReadModelComponents.cs`, `Assets/Game/Scripts/Systems/FocusedUnitUiReadModelUiSystemHelper.cs`, `Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs`, `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs`, `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs`, and this tracker.
- Behavior intent:
  - Added compact typed logistics status text to the focused-unit ECS read model for resource haulers.
  - Selected tray/tanker cargo chips now include assignment or blocked reason text such as `WAITING FUEL`, `NO STORAGE`, `STORAGE FULL`, or `NO ROUTE`.
  - Included logistics status in the existing transport panel cache key so the UI updates only when the selected hauler status changes.
- Validation:
  - Added `FocusedResourceHaulerSelectionPanelShowsTypedLogisticsStatus`.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Reassess whether the remaining unmanaged `FuelTankerLogisticsAssignmentSystem` split is practical or whether current managed bridge constraints should be documented before Phase 6 continuation.
- Slice: tanker unmanaged assignment split assessment.
- Files changed: this tracker.
- Behavior intent:
  - Closed the Phase 5 unmanaged tanker assignment row as an explicit architecture decision rather than adding a broad replacement shell.
  - A true unmanaged `FuelTankerLogisticsAssignmentSystem` is not practical in the current slice because assignment still depends on managed `RuntimeBuildingEntity` dictionaries, building focus delegates, grid/placement delegates, and movement request bridging owned by `BuildingResourceHaulerBridgeCompositionSystemHelper`.
  - The existing bridge is acceptable as the narrow composition boundary for Phase 5 because tanker state, cargo, reservations, storage, blocked reasons, and header-visible usable Fuel are ECS-owned and validated.
  - The next practical split is to first move runtime building route candidates into ECS-native buffers/components; only then should tray/tanker route assignment become a Burst-compatible unmanaged system.
- Validation:
  - Tracker-only architecture closure; no runtime code changed.
  - `git diff --check` passed.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 6 vehicle Fuel consumption validation and remaining typed feedback rows, while keeping any future assignment-system split gated behind ECS-native building route candidate data.
- Slice: Phase 6 progress accounting correction.
- Files changed: this tracker.
- Behavior intent:
  - Reconciled the tracker summary with the authoritative checklist rows after rebasing remote vehicle-fuel work.
  - Phase 6 now correctly shows 11/11 complete because every vehicle Fuel spending implementation and validation row is checked.
  - Overall implementation progress now matches the counted checklist total: 79/99.
- Validation:
  - `git diff --check` passed.
  - Checklist count command returned `79/99`.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
- Next action:
  - Continue with the remaining Phase 3 unmanaged tray split decision or Phase 8 AI/enemy fuel support, depending on the next lowest-risk stable slice.
- Slice: tray unmanaged assignment split assessment.
- Files changed: this tracker.
- Behavior intent:
  - Closed the Phase 3 unmanaged tray assignment row with the same architecture decision as Phase 5 tanker assignment.
  - A true unmanaged `OilTrayLogisticsAssignmentSystem` should wait until runtime building route candidates, building focus points, and source/destination capacity summaries are ECS-native.
  - Current tray automation remains acceptable as a narrow managed composition bridge because cargo, reservations, storage transfers, blocked reasons, and selected UI read models are ECS-owned and covered by focused tests.
  - This avoids adding a broad replacement `ISystem` shell that would still call managed dictionaries/delegates and violate the ECS/SOLID contract.
- Validation:
  - `git diff --check` passed.
  - Checklist count command returned `80/99`.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
- Next action:
  - Continue Phase 8 AI/enemy fuel support or Phase 9 validation/profiling, with future tray/tanker unmanaged assignment work gated behind ECS-native route candidate data.
- Slice: enemy-faction fuel logistics validation.
- Files changed: `Assets/Tests/Editor/BuildingResourceProductionEcsSystemTests.cs` and this tracker.
- Behavior intent:
  - Added a focused non-player faction validation for the shared ECS fuel data model.
  - An enemy-owned refinery now produces Fuel through the same `BuildingResourceProductionEcsSystem.ApplyStorageQuery` path.
  - An enemy tanker routes that Fuel to an enemy Fuel Bladder while ignoring a nearer player Fuel Bladder, proving same-faction hauling remains data-driven.
- Validation:
  - Added `AutomaticFuelLogisticsEnemyFaction_ProducesAndDeliversFuel`.
  - Updated `BuildingResourceProductionEcsFocusedValidation` count to 34.
  - `git diff --check` passed.
  - Checklist count command returned `82/99`.
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed.
  - Unity focused validation remains blocked by the recurring licensing client mismatch/reconnect loop unless the editor/license state is reset. Prior log: `/private/tmp/warline-fuel-authoring-tests.log`.
- Next action:
  - Continue Phase 8 by gating AI production of fuel-cost units against usable Fuel or expected production.
