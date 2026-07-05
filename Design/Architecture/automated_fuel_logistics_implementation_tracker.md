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

Overall implementation progress: 7% (7/98 checklist items complete).

Progress is checklist-based. Each `- [ ]` or `- [x]` implementation/validation item below counts as one item. When a future implementation slice adds or removes checklist items, update this section in the same commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Inventory and baseline | In Progress | 5 | 8 | 63% | Audit current Oil/Fuel components, building runtime summaries, resource hauler code, seeded trucks, and HUD header data. |
| 1. Data model | In Progress | 2 | 11 | 18% | Add or adapt ECS components/buffers for buffers, capacities, reservations, cargo, seeded logistics validation, and faction usable Fuel. |
| 2. Oil extraction and refinery buffers | Pending | 0 | 8 | 0% | Ensure Oil Pump and Refinery buffers are ECS-owned and versioned. |
| 3. Tray truck automation | Pending | 0 | 12 | 0% | Auto-assign Oil pickup/delivery without manual target commands. |
| 4. Refinery conversion | Pending | 0 | 9 | 0% | Convert Oil input buffer into Fuel output buffer with cap/stall reasons. |
| 5. Tanker automation and usable Fuel | Pending | 0 | 12 | 0% | Deliver refinery output Fuel into Fuel Bladder/base storage and update header pool. |
| 6. Vehicle fuel spending | Pending | 0 | 11 | 0% | Consume usable Fuel for ground/air mobility and block/redirect orders safely. |
| 7. UI read models and feedback | Pending | 0 | 10 | 0% | Header, selection panel, disabled reasons, and truck task feedback from versioned data. |
| 8. AI and enemy support | Pending | 0 | 7 | 0% | Let AI understand fuel economy after player loop is validated. |
| 9. Validation and profiling | Pending | 0 | 10 | 0% | Focused tests, seeded-map checks, guardrails, GC checks, and Android profiling. |

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

- [ ] Define or adapt a `ResourceKind` representation for Oil and Fuel that can be used by Burst-compatible systems.
- [ ] Define storage buffer data for buildings: current amount, capacity, reserved inbound, reserved outbound, and version.
- [ ] Define logistics cargo data for tray/tanker trucks: carried resource, amount, capacity, source, destination, task state, and reservation id.
- [ ] Define logistics role tags: oil hauler, fuel hauler, source, refinery input, refinery output, fuel storage.
- [ ] Define faction usable Fuel summary: current, capacity, produced, delivered, spent, version.
- [ ] Define typed blocked/status reason codes for UI and command validation.
- [x] Define scenario/map authoring requirement for seeded tray/tanker pairs at each fuel-enabled faction military base.
- [ ] Add baker/config mapping only where needed; do not duplicate balance data already owned by configs.

Performance notes:

- Use fixed-size value components where possible.
- Use dynamic buffers only for truly variable storage/reservation collections.
- Prefer integer/fixed-point resource values for deterministic accumulation and cheap UI deltas.

Validation:

- [ ] Editor tests cover storage capacity, reservation accounting, and faction usable Fuel summary.
- [x] Authoring validation warns or fails when a fuel-enabled map lacks one tray truck or one tanker near each faction military base.
- [ ] `git diff --check` passes.

## Phase 2: Oil Extraction And Building Buffers

- [ ] Keep Oil Pump extraction in ECS or move it into an unmanaged `ISystem` if currently managed.
- [ ] Write Oil into a pump output buffer with capacity and dirty/version update.
- [ ] Ensure extraction stalls cleanly when the pump buffer is full.
- [ ] Publish selected Oil Pump read model only when buffer/rate/block reason changes.
- [ ] Ensure map-placed and player-built oil pumps follow the same baked component path.

Validation:

- [ ] Focused oil pump production test: pump accumulates Oil over deterministic ticks.
- [ ] Focused capacity test: pump stops at capacity without overflow or GC.
- [ ] Selection panel test: pump status changes only when read-model version changes.

## Phase 3: Tray Truck Automation

- [ ] Implement `OilTrayLogisticsAssignmentSystem` as unmanaged `ISystem` where practical.
- [ ] Use dirty/version gates so assignment work only runs when source availability, destination capacity, route validity, or truck availability changes.
- [ ] Reserve source Oil and refinery input capacity before a truck starts a task.
- [ ] Avoid all-truck/all-building scans every frame; use cached queries and versioned candidate buffers where practical.
- [ ] Reuse existing movement/order data path for truck travel rather than adding a new movement loop.
- [ ] Transfer cargo at pickup/drop-off through ECS mutation systems.
- [ ] Clear reservations on truck death, source/destination death, route invalidation, or task cancellation.

Validation:

- [ ] One pump, one refinery, one tray truck transfers Oil without manual command.
- [ ] Seeded faction-base tray truck can start Oil hauling without building a truck at runtime.
- [ ] No refinery capacity causes tray truck idle with a typed reason.
- [ ] Destroyed source/destination clears reservations.
- [ ] Steady-state automation produces 0 B/frame GC.

## Phase 4: Refinery Conversion

- [ ] Implement or adapt a Burst-compatible refinery conversion `ISystem`.
- [ ] Consume Oil input and produce Fuel output using config rate and efficiency.
- [ ] Stall when Oil input is empty.
- [ ] Stall when Fuel output buffer is full.
- [ ] Publish selected Refinery read model only on version changes.
- [ ] Support Large Refinery with the same system and different config data.

Validation:

- [ ] Deterministic conversion test covers Oil input, Fuel output, efficiency, and capacity.
- [ ] Full output buffer does not consume Oil.
- [ ] Selected-building panel shows conversion and blocked reason accurately.

## Phase 5: Tanker Automation And Usable Fuel

- [ ] Implement `FuelTankerLogisticsAssignmentSystem` as unmanaged `ISystem` where practical.
- [ ] Reserve refinery output Fuel and storage capacity before tanker pickup.
- [ ] Deliver Fuel into Fuel Bladder/base storage, not directly into the header.
- [ ] Update faction usable Fuel and capacity from storage state.
- [ ] Keep header Fuel sourced from faction usable Fuel summary.
- [ ] Stall tankers when no output Fuel, no storage, full storage, no route, or no available tanker exists.
- [ ] Ensure map-placed and player-built Fuel Bladders follow the same storage path.

Validation:

- [ ] Refinery output does not change header Fuel until tanker delivery completes.
- [ ] Fuel Bladder capacity controls max usable Fuel.
- [ ] Header updates after delivered Fuel and stays version-gated.
- [ ] Seeded faction-base tanker can start Fuel hauling without building a truck at runtime.
- [ ] Tanker idle/block reasons are typed and visible in selected truck/building UI.

## Phase 6: Vehicle Fuel Consumption

- [ ] Add fuel-cost config for vehicle/air movement-heavy or operation-heavy actions if missing.
- [ ] Implement `VehicleFuelConsumptionSystem` or equivalent unmanaged `ISystem` for active fuel drains.
- [ ] Aggregate consumption by faction and vehicle class to avoid per-entity UI churn.
- [ ] Block new movement/launch/support commands when usable Fuel is below required threshold.
- [ ] Add aircraft-safe behavior: new launches blocked when fuel is short; active aircraft return to base or use emergency reserve, never stop midair.
- [ ] Add ground-vehicle no-fuel behavior: reject new long moves, finish committed segment, return, or hold by policy.
- [ ] Publish warning/read-model updates only when fuel state or blocked reasons change.

Validation:

- [ ] Ground vehicle movement spends Fuel.
- [ ] New ground vehicle movement is blocked at 0 usable Fuel with a typed reason.
- [ ] Aircraft at 0 usable Fuel returns/lands safely and does not freeze midair.
- [ ] UI command buttons update only on fuel/version changes.

## Phase 7: UI Read Models And Feedback

- [ ] Header Fuel reads faction usable Fuel and capacity, not refinery output.
- [ ] Oil can be shown only when the active mission/skirmish preset teaches extraction.
- [ ] Selection panel supports Oil Pump, Refinery, Fuel Bladder, tray truck, and tanker logistics state.
- [ ] Disabled command/build/production rows use typed reason ids.
- [ ] Avoid per-frame string formatting; cache labels or update TMP text only on read-model version changes.
- [ ] Keep Canvas work inside existing UI helper boundaries and do not add simulation logic to UI classes.

Validation:

- [ ] Header fuel changes only after storage delivery or spending.
- [ ] Selection panel updates for two different pumps/refineries/storage buildings without stale values.
- [ ] UI click blocking remains intact on Android and Editor.
- [ ] No new GC allocations in steady-state HUD updates.

## Phase 8: AI And Enemy Support

- [ ] Enable the same storage/conversion/hauling data model for non-player factions.
- [ ] Gate AI production of fuel-cost units by available usable Fuel or expected production.
- [ ] Let AI target logistics infrastructure according to existing targeting architecture.
- [ ] Keep AI logistics choices ECS/data-driven; no managed per-frame planner scans.

Validation:

- [ ] Enemy faction can produce/deliver Fuel in a fuel-enabled test setup.
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
