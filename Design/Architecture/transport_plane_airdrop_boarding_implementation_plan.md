# Transport Plane Boarding And Airdrop Implementation Plan

## Status

Overall status: Pending

This document tracks implementation for `Unit_Veh_Plane_Transport` as a real transport aircraft:

- Soldiers can board the rear ramp.
- Ground vehicles can board the rear ramp.
- The plane flies like fixed-wing jets, but with a higher transport cruise altitude.
- Ground/ramp unload opens the rear door and lets passengers exit from the back.
- Airborne soldier exit uses `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab`.
- Airborne vehicle/cargo exit uses `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_01.prefab`.

Each phase below should be updated to `Pending`, `In Progress`, `Complete`, or `Blocked` as work lands.

## Current Findings

- `Unit_Veh_Plane_Transport` exists at `Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab`.
- Its config exists at `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Plane_Transport_Config.asset`.
- Current plane config has `isAirUnit: 1`, `productionTransportUsesRunwayLanding: 1`, `speed: 30`, `runwayTaxiSpeed: 12`, and `soldierTransportCapacity: 0`.
- `UnitTransportCapacitySystem.ResolveTransportCapacity` currently recognizes APCs, canopy truck, and transport helicopter, but not `Unit_Veh_Plane_Transport`.
- Current transport capacity data only has `SoldierCapacity`; it has no vehicle/cargo capacity.
- Current passenger candidate logic rejects vehicles.
- Current airborne exit path is helicopter rope-only and identifies `Unit_Veh_Helicopter_Transport`.
- The transport plane prefab has a rear door object named `Door_X`.
- `BuildingProductionTransportSystem` already has plane door/interior/rollout logic for delivering newly produced vehicles. The gameplay transport boarding plan should reuse the same door/ramp concept through a narrow runtime owner instead of duplicating private production-transport helpers.
- The requested soldier parachute prefab exists.
- The requested vehicle/cargo emergency drop prefab exists.

## Architecture Contract

### Non-Negotiable Rules

- Do not introduce `TransportPlaneManager`, `AirdropController`, `TransportFacade`, or any broad orchestration shell.
- Do not move boarding, airdrop, or cargo state into UI, bootstrap, scene scripts, or config-only behavior.
- Do not make `UnitTransportBoardingSystem` a helper surface. It should remain only the boarding-completion tick that consumes `UnitTransportBoardingTarget`.
- Do not reuse helicopter rope components for plane parachute/cargo drop state. Plane airdrop gets separate data and systems.
- Do not hardcode UI button behavior in views. UI must request through `SelectionUiCommandSystem` / ECS command intent flow.
- Do not change pathfinding constants, traversal costs, search limits, or unit movement semantics as part of this feature.
- Preserve existing helicopter rope behavior.
- Preserve existing production transport delivery behavior.

### Ownership

- Board command mode and UI request routing stay with `SelectionUiCommandSystem`, `RtsSelectionInputSystem`, `RtsSelectionBoardTargetModeCommandSystem`, and command intent buffers.
- Boarding command validation, selected boarding-source collection, and request creation stay with `TransportBoardingCommandSystem` or a narrower transport command system.
- Boarding completion remains owned by `UnitTransportBoardingSystem`.
- Capacity metadata stays in `UnitTransportCapacitySystem` or a narrower cargo-capacity system.
- Passenger hidden/restored state stays in `UnitTransportPassengerStateSystem` or a narrower cargo-passenger state system.
- Air pickup/air movement requests stay in `UnitTransportAirPickupSystem`, `UnitAirMovementSystem`, and fixed-wing runway systems.
- Helicopter rope drop stays in `UnitTransportRopeDisembarkSystem`.
- Plane parachute/cargo airdrop should be owned by new narrow ECS systems such as `UnitTransportParachuteDropCommandSystem`, `UnitTransportParachuteDropSystem`, and `UnitTransportCargoDropSystem`.
- Runtime rear door/ramp metadata and animation should be owned by a narrow system such as `UnitTransportPlaneDoorSystem`; production transport door helpers should be extracted/reused only through a clean shared boundary.
- Diagnostics must flow through ECS diagnostic buffers/flush systems, not direct static logging.

## Gameplay Behavior

### Plane Movement

- `Unit_Veh_Plane_Transport` should use the existing fixed-wing runway/takeoff/landing style.
- It should cruise higher than combat jets and helicopters to read as a heavy transport plane.
- Recommended initial transport cruise height: `45m-60m` above runway/ground.
- It should not hover like a helicopter.
- Airborne airdrops should happen during a pass over or near the selected drop zone.
- After airdrop completion, the plane returns to runway/home/staging unless the player gives another command.

### Rear Ramp Boarding

Plane boarding must happen through the rear door/ramp.

- The plane must be landed/staged before soldiers or vehicles can board.
- The rear door/ramp opens before boarding starts.
- Soldiers path to a rear-ramp approach cell, then enter and become hidden passengers.
- Vehicles path to the rear-ramp approach lane, align with the ramp, drive into the cargo bay, then become hidden passengers.
- The door/ramp closes after boarding completes or times out.
- This is the reverse of the current production transport delivery flow where the plane opens the door and vehicles roll out.

### Ground/Ramp Unload

When the plane is landed/staged:

- Disembark opens the rear door/ramp.
- Soldiers walk out from the rear ramp and disperse to clear cells.
- Vehicles drive out from the rear ramp to rollout/disperse cells.
- Door/ramp closes after unload completes.
- This mode should not spawn parachutes.

### Airborne Soldier Airdrop

When the plane is airborne and disembark/airdrop is accepted:

- The plane starts an airdrop pass over the selected or current drop zone.
- Soldiers drop one by one from the rear/ramp/belly anchor.
- Each soldier becomes visible near the plane, descends to a valid landing cell, and has a parachute visual attached above them.
- The parachute visual uses `SM_Prop_Parachute_01.prefab`.
- On touchdown, the soldier detaches from the parachute, lands on grounded map height, clears passenger state, becomes selectable/commandable again, then disperses to a nearby clear cell if needed.
- Parachute visuals despawn after a short cleanup delay.

### Airborne Vehicle/Cargo Airdrop

When the plane is airborne and vehicle/cargo passengers are airdropped:

- The vehicle becomes visible as cargo during descent, or a cargo rig visual carries/represents it until touchdown.
- The emergency drop visual uses `SM_Prop_EmergencyDrop_01.prefab`.
- The vehicle descends from the plane to a valid large landing footprint.
- On touchdown, the vehicle clears passenger state, becomes selectable/commandable again, and drives/settles to the assigned rollout/disperse cell.
- Vehicle cargo drops must use footprint-aware landing-cell search and must not land on blocked, occupied, or invalid cells.

## Recommended Initial Tuning

These values are starting points and should be adjusted after visual QA:

| Setting | Recommendation |
|---|---|
| Soldier capacity | 24 soldiers |
| Vehicle capacity | 2 light/medium ground vehicles, or 1 heavy vehicle if footprint/weight is large |
| Cargo budget model | Prefer cargo slots/weight over only `SoldierCapacity` |
| Transport cruise height | 45m-60m |
| Soldier drop interval | 0.45s-0.7s |
| Vehicle drop interval | 1.25s-1.8s |
| Soldier parachute descent | 3.0s-4.5s, altitude dependent |
| Vehicle emergency drop descent | 3.5s-5.5s, altitude dependent |
| Door/ramp open time | 0.9s-1.25s |
| Door/ramp close delay | 0.5s after final board/unload event |

## Data Model Plan

Current `UnitTransportCapacity` only supports soldiers. Do not overload it silently for vehicles.

Recommended additions:

```csharp
public struct UnitTransportCargoCapacity : IComponentData
{
    public int SoldierCapacity;
    public int VehicleCapacity;
    public int CargoWeightCapacity;
}

public struct UnitTransportCargoPassenger : IComponentData
{
    public Entity Transport;
    public byte PassengerKind;
    public int CargoWeight;
}

public struct UnitTransportPlaneDoorState : IComponentData
{
    public float Open01;
    public byte TargetOpen;
}

public struct UnitTransportAirdropRequest : IComponentData
{
    public int2 DropReferenceCell;
    public float NextDropAt;
    public float DropIntervalSeconds;
    public int DropCount;
    public byte DropMode;
}

public struct UnitTransportParachuteDropComponent : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public int2 LandingCell;
    public float StartedAt;
    public float DurationSeconds;
    public Entity Visual;
}

public struct UnitTransportCargoDropComponent : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public int2 LandingCell;
    public float StartedAt;
    public float DurationSeconds;
    public Entity Visual;
}
```

Final field names can change during implementation, but the responsibilities should stay separated:

- capacity metadata
- passenger/cargo membership
- door/ramp state
- airdrop request
- soldier parachute descent
- vehicle cargo descent

## Progress Tracker

| Phase | Status | Owner | Validation / Notes |
|---|---|---|---|
| 0. Audit current transport and aircraft systems | Complete | Gameplay | Existing systems and assets inspected while creating this document. |
| 1. Capacity and cargo data design | Pending | Gameplay | Add soldier/vehicle/cargo capacity without breaking current passenger UI. |
| 2. Plane config and authoring wiring | Pending | Gameplay | Set plane capacity, cruise height, and prefab references. |
| 3. Rear door/ramp runtime owner | Pending | Gameplay/Visual | Reuse `Door_X`; do not duplicate production-only private helpers. |
| 4. Rear-ramp boarding command path | Pending | Gameplay | Soldiers and vehicles board through rear ramp while landed. |
| 5. Ground/ramp unload path | Pending | Gameplay | Soldiers walk out; vehicles drive out. |
| 6. Airdrop command path | Pending | Gameplay | Choose airborne parachute/cargo mode instead of rope. |
| 7. Soldier parachute drop visuals and state | Pending | Gameplay/VFX | Use `SM_Prop_Parachute_01.prefab`. |
| 8. Vehicle emergency cargo drop visuals and state | Pending | Gameplay/VFX | Use `SM_Prop_EmergencyDrop_01.prefab`. |
| 9. Flight behavior and high-altitude pass | Pending | Gameplay | Plane flies like fixed-wing jet, higher cruise. |
| 10. HUD/read-model/feedback updates | Pending | Gameplay/UI | Capacity, passengers, airdrop/unload feedback. |
| 11. Diagnostics, tests, and visual QA | Pending | Gameplay/QA | Record commands/logs when complete. |

## Phase 0 - Audit Current Systems

Status: Complete

Checklist:

- [x] Confirm transport plane prefab exists.
- [x] Confirm parachute prefab exists.
- [x] Confirm emergency drop prefab exists.
- [x] Confirm current plane config has zero soldier capacity.
- [x] Confirm current capacity resolver does not include transport plane.
- [x] Confirm current boarding candidate logic rejects vehicles.
- [x] Confirm current rope exit is helicopter-specific.
- [x] Confirm transport plane has `Door_X`.
- [x] Confirm production transport already has door/interior/rollout behavior to mirror.

## Phase 1 - Capacity And Cargo Data Design

Status: Pending

Implementation checklist:

- [ ] Decide whether to extend `UnitTransportCapacity` or add `UnitTransportCargoCapacity`.
- [ ] Preserve `UnitTransportCapacity.SoldierCapacity` for existing UI/read-model callers.
- [ ] Add vehicle/cargo capacity data for the transport plane.
- [ ] Add passenger kind/weight data so systems can distinguish soldiers from vehicles.
- [ ] Define vehicle eligibility: friendly ground vehicles, not aircraft, not buildings/static blockers, not already passengers, not too large for cargo rules.
- [ ] Define mixed cargo rules: soldiers + vehicles share total cargo budget, or soldiers and vehicles use separate caps.
- [ ] Add typed rejection reasons or map to existing `TransportFull`, `InvalidPassenger`, and `NoEligiblePassengers`.

Acceptance criteria:

- APC/truck/helicopter existing soldier boarding remains unchanged.
- Transport plane reports soldier capacity and vehicle capacity.
- Vehicle passengers can be represented without pretending they are soldiers.

## Phase 2 - Plane Config And Authoring Wiring

Status: Pending

Implementation checklist:

- [ ] Update `Prefab_UnitGrid_Veh_Plane_Transport_Config.asset` soldier capacity from `0` to the approved value.
- [ ] Add or configure vehicle/cargo capacity for `Unit_Veh_Plane_Transport`.
- [ ] Add authoring/config support for explicit air cruise height if current model-bounds-derived height is too low.
- [ ] Set transport plane cruise height to `45m-60m` initial tuning.
- [ ] Keep `productionTransportUsesRunwayLanding` and runway taxi behavior intact.
- [ ] Wire parachute prefab reference through a config/authoring path, not a hardcoded scene lookup.
- [ ] Wire emergency drop prefab reference through a config/authoring path, not a hardcoded scene lookup.

Acceptance criteria:

- Freshly spawned transport plane has transport capacity data.
- Freshly spawned transport plane has high fixed-wing cruise height.
- Prefab references are durable and not looked up by arbitrary scene paths.

## Phase 3 - Rear Door/Ramp Runtime Owner

Status: Pending

Implementation checklist:

- [ ] Add a narrow runtime owner for plane door/ramp metadata and animation, for example `UnitTransportPlaneDoorSystem`.
- [ ] Resolve `Door_X` from the plane prefab/model once and cache it safely.
- [ ] Expose data-only ECS door state for open/close intent.
- [ ] Reuse or extract production transport door/interior/rollout math from `BuildingProductionTransportSystem` into a narrow shared utility/system.
- [ ] Define rear-ramp anchor, interior point, approach point, and rollout point in plane local space.
- [ ] Open door/ramp for board/unload/airdrop preparation.
- [ ] Close door/ramp after board/unload/drop completion.

Acceptance criteria:

- Door/ramp opens visibly before soldiers or vehicles board.
- Door/ramp stays open during boarding/unloading/airdrop.
- Door/ramp closes after the sequence.
- Production delivery behavior still works.

## Phase 4 - Rear-Ramp Boarding Command Path

Status: Pending

Implementation checklist:

- [ ] Extend boardable transport recognition to include `Unit_Veh_Plane_Transport`.
- [ ] Keep plane boarding valid only when the plane is landed/staged and not taking off, landing, returning, or airdropping.
- [ ] Extend passenger candidate logic to allow eligible vehicles when target transport is the plane.
- [ ] Keep vehicles invalid for helicopter rope boarding.
- [ ] Use rear-ramp approach cells for both soldiers and vehicles.
- [ ] For vehicles, require a clear approach lane and enough cargo capacity.
- [ ] Open the rear ramp before the passenger reaches final boarding.
- [ ] Board by hiding the passenger and adding passenger/cargo state through passenger-state systems.
- [ ] Keep selected transport selected after accepted transport-first boarding.

Acceptance criteria:

- Soldiers board the plane from the rear ramp.
- Vehicles board the plane from the rear ramp.
- Vehicles do not board through the center or nose of the plane.
- Helicopter boarding behavior remains unchanged.

## Phase 5 - Ground/Ramp Unload Path

Status: Pending

Implementation checklist:

- [ ] When plane is landed/staged, Disembark/Exit uses ramp unload, not parachute.
- [ ] Open rear ramp.
- [ ] Restore soldier passengers at the rear ramp and disperse to valid nearby cells.
- [ ] Restore vehicle passengers at the rear ramp and drive them to valid rollout cells.
- [ ] Use footprint-aware placement for vehicles.
- [ ] Close rear ramp when unload finishes.
- [ ] Preserve passenger order unless user-selected passenger unload requires reordering.

Acceptance criteria:

- Soldiers walk out from the rear.
- Vehicles drive out from the rear.
- No parachute/emergency drop visuals appear during landed unload.
- Invalid/blocked unload cells keep remaining passengers onboard and report feedback.

## Phase 6 - Airdrop Command Path

Status: Pending

Implementation checklist:

- [ ] Add a plane-specific airborne airdrop command setup path.
- [ ] Select a drop reference cell from clicked target, current command target, or current plane cell.
- [ ] If plane is landed and player requested airborne airdrop, command takeoff and airdrop pass first.
- [ ] If plane is airborne, begin airdrop pass without landing.
- [ ] Route soldier passengers to parachute drop.
- [ ] Route vehicle passengers to emergency cargo drop.
- [ ] Do not add `UnitTransportRopeDisembarkRequest` for the plane.
- [ ] Clear or reject airdrop if the plane is landing/takeoff locked, destroyed, empty, or lacks valid drop cells.

Acceptance criteria:

- Airborne plane disembark uses parachute/cargo drop, not rope.
- Helicopter disembark still uses rope.
- Plane can drop all passengers one by one with correct visual type.

## Phase 7 - Soldier Parachute Drop

Status: Pending

Implementation checklist:

- [ ] Add soldier parachute drop component/request data.
- [ ] Instantiate/attach `SM_Prop_Parachute_01.prefab` visual for each dropping soldier.
- [ ] Start soldier at rear/ramp/belly anchor at plane altitude.
- [ ] Ground target with `MapSurfaceSpawnGrounding`.
- [ ] Descend with controlled drift and constant orientation/readability.
- [ ] Keep soldier non-commandable until touchdown.
- [ ] On touchdown, remove passenger/disabled/drop state, restore visuals, place on valid cell, then disperse if needed.
- [ ] Despawn parachute visual after a short delay.

Acceptance criteria:

- Soldier parachute is obvious and attached during descent.
- Soldier lands on ground height, not in the air or below terrain.
- Soldier becomes controllable after landing.

## Phase 8 - Vehicle Emergency Cargo Drop

Status: Pending

Implementation checklist:

- [ ] Add vehicle cargo drop component/request data.
- [ ] Instantiate/attach `SM_Prop_EmergencyDrop_01.prefab` visual for each dropping vehicle.
- [ ] Use footprint-aware landing-cell search before accepting drop.
- [ ] Start vehicle/cargo at plane rear/ramp/belly anchor.
- [ ] Descend cargo with heavier timing than soldiers.
- [ ] Keep vehicle disabled/non-commandable during descent.
- [ ] On touchdown, remove passenger/disabled/drop state, restore vehicle visual, place footprint on valid cells.
- [ ] Issue a short rollout/settle move if needed.
- [ ] Despawn emergency drop visual after touchdown.

Acceptance criteria:

- Vehicle drop looks heavier than soldier drop.
- Vehicle never lands on blocked/occupied cells.
- Vehicle becomes commandable only after touchdown.

## Phase 9 - Flight Behavior And High-Altitude Pass

Status: Pending

Implementation checklist:

- [ ] Add transport-plane high-cruise-height support through config/authoring.
- [ ] Keep fixed-wing runway takeoff/landing flow.
- [ ] Add an airdrop pass target/exit position similar to attack/move pass behavior.
- [ ] Keep plane level and readable during airdrop.
- [ ] Prevent hover-style behavior.
- [ ] Return to runway/home/staging after airdrop if no follow-up command exists.

Acceptance criteria:

- Transport plane flies higher than combat jets/helicopters.
- Airdrop occurs during a believable pass over/near the drop zone.
- Plane returns safely after completion.

## Phase 10 - HUD, Read Models, And Feedback

Status: Pending

Implementation checklist:

- [ ] Update selected transport capacity read model to handle soldier and vehicle/cargo capacity.
- [ ] Show transport plane passengers in the passenger drawer without losing existing soldier support.
- [ ] Add feedback strings for `Boarding transport plane`, `Loading cargo`, `Airdrop in progress`, `Cargo drop blocked`, and `Transport full`.
- [ ] Ensure Board mode target previews include eligible vehicles only when selected transport is a cargo-capable plane.
- [ ] Ensure passenger-first Board mode highlights the plane as boardable when it has capacity.
- [ ] Keep UI passive; no gameplay policy in view classes.

Acceptance criteria:

- Player can understand free soldier seats and vehicle/cargo space.
- Rejections are typed and visible.
- Existing Board mode visuals still work.

## Phase 11 - Diagnostics, Tests, And Visual QA

Status: Pending

Validation checklist:

- [ ] EditMode: plane capacity resolves above zero.
- [ ] EditMode: plane cargo capacity accepts eligible vehicles.
- [ ] EditMode: APC/helicopter still reject vehicle passengers.
- [ ] EditMode: plane boarding requires landed/staged state.
- [ ] EditMode: plane disembark while grounded chooses ramp unload.
- [ ] EditMode: plane disembark while airborne chooses parachute/cargo drop.
- [ ] EditMode: soldier parachute drop restores passenger to valid ground cell.
- [ ] EditMode: vehicle cargo drop restores vehicle to valid footprint.
- [ ] PlayMode: soldiers board through rear ramp and disappear inside plane.
- [ ] PlayMode: vehicles board through rear ramp and disappear inside plane.
- [ ] PlayMode: landed unload opens rear door and passengers exit from back.
- [ ] PlayMode: airborne soldier airdrop shows parachutes and lands cleanly.
- [ ] PlayMode: airborne vehicle drop shows emergency drop visual and lands cleanly.
- [ ] Visual QA: no drops under terrain, inside plane, or at wrong scale.
- [ ] Visual QA: door/ramp timing looks connected to boarding/unload.

Validation log:

- Pending.

## Implementation Order

1. Add cargo/capacity data and plane config/authoring support.
2. Add rear door/ramp runtime owner and shared plane door anchors.
3. Extend boarding eligibility for plane soldiers and vehicles.
4. Implement rear-ramp boarding.
5. Implement landed ramp unload.
6. Implement airborne airdrop command setup.
7. Implement soldier parachute drop.
8. Implement vehicle emergency cargo drop.
9. Tune transport plane high cruise/aerial pass behavior.
10. Update HUD/read model feedback.
11. Add focused tests and visual QA.

## Open Questions

- Should the plane use separate soldier and vehicle capacity, or a single cargo-weight budget?
- Should heavy tanks fit, or only light/medium vehicles?
- Should airdrop be a separate command button, or should Disembark choose ramp unload vs airborne airdrop based on plane state?
- Should the player choose the drop zone explicitly, or should airborne Disembark drop at the plane's current location?
- Should cargo drop damage vehicles if dropped into invalid/hostile zones, or simply reject invalid drops?
- Should the plane remain selected after issuing airdrop?

Conservative defaults:

- Use separate soldier and vehicle capacities for V1.
- Transport plane capacity starts at 24 soldiers and 2 light/medium vehicles.
- Grounded Disembark means rear-ramp unload.
- Airborne Disembark means airdrop at current/target drop zone.
- Heavy vehicles are rejected until cargo-weight rules are approved.
- Plane remains selected after boarding/disembark commands.
