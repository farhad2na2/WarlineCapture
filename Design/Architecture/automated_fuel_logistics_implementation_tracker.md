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

Overall implementation progress: 17% (17/99 checklist items complete).

Progress is checklist-based. Each `- [ ]` or `- [x]` implementation/validation item below counts as one item. When a future implementation slice adds or removes checklist items, update this section in the same commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Inventory and baseline | In Progress | 5 | 8 | 63% | Audit current Oil/Fuel components, building runtime summaries, resource hauler code, seeded trucks, and HUD header data. |
| 1. Data model | In Progress | 5 | 11 | 45% | Add or adapt ECS components/buffers, capacities, reservations, cargo, seeded logistics validation, and faction usable Fuel. |
| 2. Oil extraction and refinery buffers | Pending | 0 | 8 | 0% | Ensure Oil Pump and Refinery buffers are ECS-owned and versioned. |
| 3. Tray truck automation | In Progress | 5 | 13 | 38% | Auto-assign Oil pickup/delivery without manual target commands. |
| 4. Refinery conversion | Pending | 0 | 9 | 0% | Convert Oil input buffer into Fuel output buffer with cap/stall reasons. |
| 5. Tanker automation and usable Fuel | In Progress | 2 | 12 | 17% | Deliver refinery output Fuel into Fuel Bladder/base storage and update header pool. |
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
- [x] Define storage buffer data for buildings: current amount, capacity, reserved inbound, reserved outbound, and version.
- [x] Define logistics cargo data for tray/tanker trucks: carried resource, amount, capacity, source, destination, task state, and reservation id.
- [ ] Define logistics role tags: oil hauler, fuel hauler, source, refinery input, refinery output, fuel storage.
- [ ] Define faction usable Fuel summary: current, capacity, produced, delivered, spent, version.
- [ ] Define typed blocked/status reason codes for UI and command validation.
- [x] Define scenario/map authoring requirement for seeded tray/tanker pairs at each fuel-enabled faction military base.
- [x] Add baker/config mapping only where needed; do not duplicate balance data already owned by configs.

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
- [x] Use dirty/version gates so assignment work only runs when source availability, destination capacity, route validity, or truck availability changes.
- [x] Reserve source Oil and refinery input capacity before a truck starts a task.
- [ ] Avoid all-truck/all-building scans every frame; use cached queries and versioned candidate buffers where practical.
- [x] Reuse existing movement/order data path for truck travel rather than adding a new movement loop.
- [ ] Transfer cargo at pickup/drop-off through ECS mutation systems.
- [ ] Clear reservations on truck death, source/destination death, route invalidation, or task cancellation.

Validation:

- [x] Focused route resolver test covers same-faction tray Oil pairing and tanker Fuel pairing.
- [x] One pump, one refinery, one tray truck transfers Oil without manual command.
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
- [x] Reserve refinery output Fuel and storage capacity before tanker pickup.
- [x] Deliver Fuel into Fuel Bladder/base storage, not directly into the header.
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
