# Transport Plane Boarding And Airdrop Implementation Plan

## Status

Overall status: Complete

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
- Current plane config has `isAirUnit: 1`, `productionTransportUsesRunwayLanding: 1`, `speed: 30`, `runwayTaxiSpeed: 12`, `soldierTransportCapacity: 24`, `vehicleTransportCapacity: 2`, and `transportCruiseHeight: 55`.
- `UnitTransportCapacitySystem.ResolveTransportCapacity` recognizes APCs, canopy truck, transport helicopter, and `Unit_Veh_Plane_Transport`; `ResolveTransportCargoCapacity` resolves the transport plane to 24 soldiers and 2 vehicle slots.
- Current transport capacity data now preserves legacy `UnitTransportCapacity.SoldierCapacity` and adds `UnitTransportCargoCapacity` for plane vehicle/cargo capacity.
- Legacy soldier-only passenger candidate logic rejects vehicles; plane-aware boarding now allows eligible player ground vehicles when the target transport is the cargo-capable transport plane.
- Current airborne exit path is helicopter rope-only and identifies `Unit_Veh_Helicopter_Transport`.
- The transport plane prefab has a rear door object named `Door_X`; `UnitGridAuthoring` now bakes it into `UnitTransportPlaneDoorReference` and `UnitTransportPlaneDoorState`.
- `BuildingProductionTransportSystem` already has plane door/interior/rollout logic for delivering newly produced vehicles. The gameplay transport boarding plan should mirror the same door/ramp concept through pure ECS data and unmanaged systems; any shared code must be stateless math/data extraction, not a managed prefab/VFX bridge.
- The requested soldier parachute prefab exists.
- The requested vehicle/cargo emergency drop prefab exists.

## Architecture Contract

### Non-Negotiable Rules

- Do not introduce `TransportPlaneManager`, `AirdropController`, `TransportFacade`, or any broad orchestration shell.
- Do not introduce new managed runtime systems for this feature. New runtime systems must be unmanaged `ISystem` implementations.
- Do not use existing managed prefab/VFX bridge patterns for parachutes, emergency cargo drops, ramp visuals, door motion, or airdrop feedback.
- Do not instantiate GameObjects at runtime for this feature. Visuals must be baked as entity prefabs and spawned with ECS command buffers.
- Do not move boarding, airdrop, or cargo state into UI, bootstrap, scene scripts, or config-only behavior.
- Do not make `UnitTransportBoardingSystem` a helper surface. It should remain only the boarding-completion tick that consumes `UnitTransportBoardingTarget`.
- Do not reuse helicopter rope components for plane parachute/cargo drop state. Plane airdrop gets separate data and systems.
- Do not hardcode UI button behavior in views. UI must request through `SelectionUiCommandSystem` / ECS command intent flow.
- Do not change pathfinding constants, traversal costs, search limits, or unit movement semantics as part of this feature.
- Preserve existing helicopter rope behavior.
- Preserve existing production transport delivery behavior.
- Authoring and baker code may hold prefab references only to convert them into entity-prefab references. Runtime systems must consume `Entity` prefab references, components, buffers, blob data, and `EntityCommandBuffer` playback only.

### Ownership

- Board command mode and UI request routing stay with `SelectionUiCommandSystem`, `RtsSelectionInputSystem`, `RtsSelectionBoardTargetModeCommandSystem`, and command intent buffers.
- Boarding command validation, selected boarding-source collection, and request creation stay with `TransportBoardingCommandSystem` or a narrower transport command system.
- Boarding completion remains owned by `UnitTransportBoardingSystem`.
- Capacity metadata stays in `UnitTransportCapacitySystem` or a narrower cargo-capacity system.
- Passenger hidden/restored state stays in `UnitTransportPassengerStateSystem` or a narrower cargo-passenger state system.
- Air pickup/air movement requests stay in `UnitTransportAirPickupSystem`, `UnitAirMovementSystem`, and fixed-wing runway systems.
- Helicopter rope drop stays in `UnitTransportRopeDisembarkSystem`.
- Plane parachute/cargo airdrop should be owned by new narrow unmanaged ECS systems such as `UnitTransportParachuteDropCommandSystem`, `UnitTransportParachuteDropSystem`, and `UnitTransportCargoDropSystem`.
- Runtime rear door/ramp metadata and animation should be owned by a narrow unmanaged ECS system such as `UnitTransportPlaneDoorSystem`; production transport door helpers may only be reused as stateless ECS-safe math/data utilities.
- Diagnostics must flow through ECS diagnostic buffers/flush systems, not direct static logging.

### Pure ECS Visual Spawn Pattern

This feature is the reference pattern for replacing legacy managed prefab/VFX bridges.

Runtime visual and effect ownership must follow this shape:

1. Authoring/baker stage stores source prefab references and bakes them into entity-prefab references on an ECS config component or buffer.
2. Runtime command systems write data-only request components or buffer elements.
3. Runtime spawn systems are unmanaged `ISystem` jobs or main-thread `ISystem` loops that instantiate baked entity prefabs through `EntityCommandBuffer`.
4. Runtime visual state is represented by ECS components such as lifetime, parent/follow target, open amount, descent progress, opacity, or cleanup delay.
5. Runtime animation is transform/component mutation performed by ECS systems. Do not call `Animator`, coroutines, scene lookup, or GameObject APIs.
6. Cleanup destroys or disables ECS entities through command buffers.

For this plan, the parachute and emergency-drop assets are source art only:

- `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab` must become a baked soldier-parachute entity prefab reference.
- `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_01.prefab` must become a baked vehicle/cargo emergency-drop entity prefab reference.

The same pure ECS pattern should later replace legacy systems that currently project runtime VFX by holding managed prefab references, calling GameObject instantiate/destroy, or forwarding gameplay events into bridge objects.

### Forbidden Runtime Patterns

Do not add these patterns while implementing transport plane boarding or airdrop:

- `SystemBase` for runtime transport, boarding, airdrop, door, or drop-visual behavior.
- `MonoBehaviour`, manager, controller, facade, or bridge as a runtime owner.
- `Object.Instantiate`, `Object.Destroy`, `Resources.Load`, `AssetDatabase`, `FindObjectOfType`, scene hierarchy lookup, `GetComponent`, or runtime prefab path lookup.
- Direct `Debug.Log` diagnostics from hot gameplay systems; use ECS diagnostic buffers.
- Managed `List<>`, LINQ, or delegate-heavy loops in hot gameplay behavior where ECS containers/jobs fit.
- Reusing helicopter rope state/components for plane parachute or cargo drops.

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
- The parachute visual uses a baked entity-prefab reference converted from `SM_Prop_Parachute_01.prefab`.
- On touchdown, the soldier detaches from the parachute, lands on grounded map height, clears passenger state, becomes selectable/commandable again, then disperses to a nearby clear cell if needed.
- Parachute visuals despawn after a short cleanup delay.

### Airborne Vehicle/Cargo Airdrop

When the plane is airborne and vehicle/cargo passengers are airdropped:

- The vehicle becomes visible as cargo during descent, or a cargo rig visual carries/represents it until touchdown.
- The emergency drop visual uses a baked entity-prefab reference converted from `SM_Prop_EmergencyDrop_01.prefab`.
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

public struct UnitTransportBoardingTarget : IComponentData
{
    public Entity Transport;
    public int2 Goal;
    public byte PassengerKind;
    public int CargoWeight;
}

public struct UnitTransportPlaneDoorState : IComponentData
{
    public float Open01;
    public byte TargetOpen;
}

public struct UnitTransportPlaneDoorReference : IComponentData
{
    public Entity DoorEntity;
    public quaternion ClosedLocalRotation;
    public quaternion OpenLocalRotation;
    public float OpenSeconds;
    public float CloseSeconds;
    public float3 DoorLocalPosition;
    public float3 InteriorLocalPosition;
    public float3 ApproachLocalPosition;
    public float3 RolloutLocalPosition;
}

public struct UnitTransportAirdropVisualPrefabs : IComponentData
{
    public Entity SoldierParachuteVisualPrefab;
    public Entity VehicleEmergencyDropVisualPrefab;
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
    public Entity VisualEntity;
}

public struct UnitTransportCargoDropComponent : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public int2 LandingCell;
    public float StartedAt;
    public float DurationSeconds;
    public Entity VisualEntity;
}
```

Final field names can change during implementation, but the responsibilities should stay separated:

- capacity metadata
- passenger/cargo membership
- door/ramp state
- door/ramp linked entity and local anchors
- baked visual entity-prefab references
- airdrop request
- soldier parachute descent
- vehicle cargo descent

## Progress Tracker

| Phase | Status | Owner | Validation / Notes |
|---|---|---|---|
| 0. Audit current transport and aircraft systems | Complete | Gameplay | Existing systems and assets inspected while creating this document. |
| 0A. Pure ECS runtime and visual-spawn architecture gate | Complete | Gameplay | Document now forbids managed runtime systems and prefab/VFX bridges for this feature. |
| 1. Capacity and cargo data design | Complete | Gameplay | ECS cargo data, passenger kind data, resolver, vehicle eligibility, existing reason-code mapping, and tests landed. Validation command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`. Result: `[UnitTransportValidation] result=Passed tests=42`. |
| 2. Plane config and authoring/baker wiring | Complete | Gameplay | Plane config set to 24 soldiers, 2 vehicles, 55m cruise; baker emits cargo capacity and airdrop visual entity-prefab refs. Static bridge guard added. Validation: `/private/tmp/warline-unit-transport-validation.log`. |
| 3. Rear door/ramp pure ECS runtime owner | Complete | Gameplay | Baked `Door_X` metadata, local anchors, ECS state, and `UnitTransportPlaneDoorSystem` landed. Boarding-target open/close, grounded unload open/expiry, airborne airdrop open, and close after airdrop request completion are wired. Validation: `/private/tmp/warline-unit-transport-validation.log`. |
| 4. Rear-ramp boarding command path | Complete | Gameplay | Plane-aware board commands and previews now accept eligible ground vehicles, reject vehicles for helicopters, require landed/staged plane boarding, route soldiers/vehicles to baked rear-ramp approach cells, enforce separate soldier/vehicle slots, write cargo passenger state at boarding completion, and keep the selected transport selected after transport-first boarding. Validation commands: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`, `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-rts-selection-input-validation.log`. Results: `[UnitTransportValidation] result=Passed tests=31`, `[RtsSelectionInputSystemValidation] result=Passed tests=55`. |
| 5. Ground/ramp unload path | Complete | Gameplay | Landed transport-plane Disembark uses rear-ramp exit placement, opens the rear door through a data-only request, restores soldier/vehicle passengers, removes passenger/cargo state, uses footprint-aware vehicle cells, issues visible rollout/disperse movement from the ramp, preserves onboard passengers when blocked, and returns typed `NoDisembarkCell` feedback. Validation: `/private/tmp/warline-unit-transport-validation.log`, `[UnitTransportValidation] result=Passed tests=34`. |
| 6. Airdrop command path | Complete | Gameplay | Airborne cargo-plane Disembark creates a pure ECS `UnitTransportAirdropRequest`; landed cargo-plane Disembark with an explicit target cell creates a takeoff-to-airdrop request; `UnitAirMovementSystem` marks the fixed-wing pass ready over the drop zone; `UnitTransportAirdropSystem` releases passengers one by one only after pass readiness, restores passenger commandability, and removes the request when the sequence completes. Validation: `/private/tmp/warline-unit-transport-validation.log`, `[UnitTransportValidation] result=Passed tests=39`. |
| 7. Soldier parachute drop visuals and state | Complete | Gameplay | Soldier parachute drop component, baked parachute entity spawn through ECB, descent interpolation, visual cleanup, touchdown restore, and short post-touchdown settle/disperse landed. Validation: `/private/tmp/warline-unit-transport-validation.log`, `[UnitTransportValidation] result=Passed tests=41`. |
| 8. Vehicle emergency cargo drop visuals and state | Complete | Gameplay | Vehicle cargo drop component, footprint-aware landing-cell search, baked emergency-drop entity spawn through ECB, heavier descent timing, visual cleanup, touchdown restore, and short rollout/settle landed. Validation: `/private/tmp/warline-unit-transport-validation.log`, `[UnitTransportValidation] result=Passed tests=41`. |
| 9. Flight behavior and high-altitude pass | Complete | Gameplay | Transport plane uses fixed-wing runway taxi/takeoff, high cruise, airdrop pass readiness, extended pass while drops release, and return-home after drop completion. Validation: `/private/tmp/warline-unit-transport-validation.log`, `[UnitTransportValidation] result=Passed tests=39`. |
| 10. HUD/read-model/feedback updates | Complete | Gameplay/UI | Focused-unit ECS read model now publishes total, soldier, and vehicle transport capacity; HUD passenger drawer shows cargo-plane soldier/vehicle slot breakdown while preserving existing soldier-only transports. Feedback strings and Board-mode preview rules are covered. Validation commands: `git diff --check`; `rg -n "Object\.Instantiate|Object\.Destroy|Resources\.Load|FindObjectOfType|GameObject\.Find|SystemBase|MonoBehaviour|TransportPlaneManager|AirdropController|TransportFacade" Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs Assets/Game/Scripts/Systems/UnitTransportPlaneDoorSystem.cs Assets/Game/Scripts/Components/GridComponents.cs Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs`; `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`. Result: `[UnitTransportValidation] result=Passed tests=47`. |
| 11. Diagnostics, tests, and visual QA | Complete | Gameplay/QA | EditMode validation and deterministic PlayMode transport tests now cover rear-ramp boarding, grounded unload, airdrop visual tracking, touchdown grounding, post-release door timing, boarding/unload door open-close timing, and the plane rear-ramp boarding reach fix. Validation: `/private/tmp/warline-unit-transport-validation.log`, `[UnitTransportValidation] result=Passed tests=50`; `/private/tmp/warline-transport-playmode-results.xml`, `GameSceneTransportBoardingPlayModeTests` passed 7/7. |

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

Status: Complete

Implementation checklist:

- [x] Decide whether to extend `UnitTransportCapacity` or add `UnitTransportCargoCapacity`.
- [x] Preserve `UnitTransportCapacity.SoldierCapacity` for existing UI/read-model callers.
- [x] Add vehicle/cargo capacity data for the transport plane.
- [x] Add passenger kind/weight data so systems can distinguish soldiers from vehicles.
- [x] Define vehicle eligibility: friendly ground vehicles, not aircraft, not buildings/static blockers, not already passengers, not too large for cargo rules.
- [x] Define mixed cargo rules: soldiers + vehicles share total cargo budget, or soldiers and vehicles use separate caps.
- [x] Add typed rejection reasons or map to existing `TransportFull`, `InvalidPassenger`, and `NoEligiblePassengers`.

Acceptance criteria:

- APC/truck/helicopter existing soldier boarding remains unchanged.
- Transport plane reports soldier capacity and vehicle capacity.
- Vehicle passengers can be represented without pretending they are soldiers.

## Phase 2 - Plane Config And Authoring Wiring

Status: Complete

Implementation checklist:

- [x] Update `Prefab_UnitGrid_Veh_Plane_Transport_Config.asset` soldier capacity from `0` to the approved value.
- [x] Add or configure vehicle/cargo capacity for `Unit_Veh_Plane_Transport`.
- [x] Add authoring/config support for explicit air cruise height if current model-bounds-derived height is too low.
- [x] Set transport plane cruise height to `45m-60m` initial tuning.
- [x] Keep `productionTransportUsesRunwayLanding` and runway taxi behavior intact.
- [x] Bake the parachute source prefab into a soldier-parachute entity-prefab reference on ECS config data.
- [x] Bake the emergency-drop source prefab into a vehicle/cargo emergency-drop entity-prefab reference on ECS config data.
- [x] Add architecture tests/static checks preventing runtime GameObject prefab/VFX bridge use for this feature.

Acceptance criteria:

- Freshly spawned transport plane has transport capacity data.
- Freshly spawned transport plane has high fixed-wing cruise height.
- Visual prefab references are durable baked `Entity` references and are not looked up by arbitrary scene paths.

## Phase 3 - Rear Door/Ramp Runtime Owner

Status: Complete

Implementation checklist:

- [x] Add a narrow unmanaged `ISystem` runtime owner for plane door/ramp metadata and animation, for example `UnitTransportPlaneDoorSystem`.
- [x] Bake `Door_X` into linked-entity metadata or bake stable local-space door/ramp anchors; do not resolve or cache scene objects at runtime.
- [x] Expose data-only ECS door state for open/close intent.
- [x] Extract only stateless ECS-safe production transport door/interior/rollout math if reuse is needed.
- [x] Define rear-ramp anchor, interior point, approach point, and rollout point in plane local space.
- [x] Open door/ramp for boarding preparation.
- [x] Close door/ramp after boarding targets complete or are removed.
- [x] Open door/ramp for grounded unload preparation.
- [x] Close door/ramp after grounded unload request expiry.
- [x] Open door/ramp for airborne airdrop preparation.
- [x] Close door/ramp after airborne drop completion.

Acceptance criteria:

- Door/ramp opens visibly before soldiers or vehicles board.
- Door/ramp stays open during boarding/unloading/airdrop.
- Door/ramp closes after the sequence.
- Production delivery behavior still works.

## Phase 4 - Rear-Ramp Boarding Command Path

Status: Complete

Implementation checklist:

- [x] Extend boardable transport recognition to include `Unit_Veh_Plane_Transport`.
- [x] Keep plane boarding valid only when the plane is landed/staged and not taking off or landing.
- [x] Extend passenger candidate logic to allow eligible vehicles when target transport is the plane.
- [x] Keep vehicles invalid for helicopter rope boarding.
- [x] Use rear-ramp approach cells for both soldiers and vehicles.
- [x] For vehicles, require clear approach cells and enough cargo capacity.
- [x] Open the rear ramp before the passenger reaches final boarding.
- [x] Board by hiding the passenger and adding passenger/cargo state through passenger-state systems.
- [x] Keep selected transport selected after accepted transport-first boarding.

Acceptance criteria:

- Soldiers board the plane from the rear ramp.
- Vehicles board the plane from the rear ramp.
- Vehicles do not board through the center or nose of the plane.
- Helicopter boarding behavior remains unchanged.

## Phase 5 - Ground/Ramp Unload Path

Status: Complete

Implementation checklist:

- [x] When plane is landed/staged, Disembark/Exit uses ramp unload, not parachute.
- [x] Open rear ramp through a data-only `UnitTransportPlaneDoorOpenRequest`.
- [x] Restore soldier passengers at rear-ramp-adjacent valid cells.
- [x] Restore vehicle passengers at rear-ramp-adjacent valid cells.
- [x] Use footprint-aware placement for vehicles.
- [x] Close rear ramp after the open request expires and no active boarding/unload request remains.
- [x] Add visible soldier walk-out/disperse movement from ramp cells.
- [x] Add visible vehicle drive-out/rollout movement from ramp cells.
- [x] Preserve passenger order unless user-selected passenger unload requires reordering.
- [x] Add typed invalid/blocked unload feedback for passengers left onboard.

Acceptance criteria:

- Soldiers walk out from the rear.
- Vehicles drive out from the rear.
- No parachute/emergency drop visuals appear during landed unload.
- Invalid/blocked unload cells keep remaining passengers onboard and report feedback.

## Phase 6 - Airdrop Command Path

Status: Complete

Implementation checklist:

- [x] Add a plane-specific airborne airdrop command setup path.
- [x] Select a drop reference cell from clicked target, current command target, or current plane cell.
- [x] If plane is landed and player requested airborne airdrop, command takeoff and airdrop pass first.
- [x] If plane is airborne, begin airdrop setup without landing.
- [x] Route soldier passengers to parachute drop counts.
- [x] Route vehicle passengers to emergency cargo drop counts.
- [x] Do not add `UnitTransportRopeDisembarkRequest` for the plane.
- [x] Clear or reject airdrop if the plane is landing/takeoff locked, destroyed, empty, or lacks valid drop cells.
- [x] Add passenger drop execution around the drop reference cell.
- [x] Add full airdrop flight-pass steering and return behavior around the drop reference cell.

Acceptance criteria:

- Airborne plane disembark uses parachute/cargo drop, not rope.
- Helicopter disembark still uses rope.
- Plane can drop all passengers one by one with correct visual type.

## Phase 7 - Soldier Parachute Drop

Status: Complete

Implementation checklist:

- [x] Add soldier parachute drop component/request data.
- [x] Spawn/attach the baked parachute entity prefab for each dropping soldier through `EntityCommandBuffer`.
- [x] Start soldier at rear/ramp/belly anchor at plane altitude.
- [x] Ground target with `MapSurfaceSpawnGrounding`.
- [x] Descend with controlled interpolation and constant orientation/readability.
- [x] Keep soldier hidden/non-commandable while onboard; restore it for the controlled descent state.
- [x] On touchdown, remove passenger/disabled/drop state, restore visuals, and place on valid cell.
- [x] Add optional touchdown disperse if needed.
- [x] Despawn parachute visual after a short delay.

Acceptance criteria:

- Soldier parachute is obvious and attached during descent.
- Soldier lands on ground height, not in the air or below terrain.
- Soldier becomes controllable after landing.

## Phase 8 - Vehicle Emergency Cargo Drop

Status: Complete

Implementation checklist:

- [x] Add vehicle cargo drop component/request data.
- [x] Spawn/attach the baked emergency-drop entity prefab for each dropping vehicle through `EntityCommandBuffer`.
- [x] Use footprint-aware landing-cell search before accepting drop.
- [x] Start vehicle/cargo at plane rear/ramp/belly anchor.
- [x] Descend cargo with heavier timing than soldiers.
- [x] Keep vehicle hidden/non-commandable while onboard; restore it for the controlled descent state.
- [x] On touchdown, remove passenger/disabled/drop state, restore vehicle visual, place footprint on valid cells.
- [x] Issue a short rollout/settle move if needed.
- [x] Despawn emergency drop visual after touchdown.

Acceptance criteria:

- Vehicle drop looks heavier than soldier drop.
- Vehicle never lands on blocked/occupied cells.
- Vehicle becomes commandable only after touchdown.

## Phase 9 - Flight Behavior And High-Altitude Pass

Status: Complete

Implementation checklist:

- [x] Add transport-plane high-cruise-height support through config/authoring.
- [x] Keep fixed-wing runway takeoff/landing flow.
- [x] Add an airdrop pass target/exit position similar to attack/move pass behavior.
- [x] Keep plane level and readable during airdrop.
- [x] Prevent hover-style behavior.
- [x] Return to runway/home/staging after airdrop if no follow-up command exists.

Acceptance criteria:

- Transport plane flies higher than combat jets/helicopters.
- Airdrop occurs during a believable pass over/near the drop zone.
- Plane returns safely after completion.

## Phase 10 - HUD, Read Models, And Feedback

Status: Complete

Implementation checklist:

- [x] Update selected transport capacity read model to handle soldier and vehicle/cargo capacity.
- [x] Show transport plane passengers in the passenger drawer without losing existing soldier support.
- [x] Add feedback strings for `Boarding transport plane`, `Loading cargo`, `Airdrop in progress`, `Cargo drop blocked`, and `Transport full`.
- [x] Ensure Board mode target previews include eligible vehicles only when selected transport is a cargo-capable plane.
- [x] Ensure passenger-first Board mode highlights the plane as boardable when it has capacity.
- [x] Keep UI passive; no gameplay policy in view classes.

Acceptance criteria:

- Player can understand free soldier seats and vehicle/cargo space.
- Rejections are typed and visible.
- Existing Board mode visuals still work.

## Phase 11 - Diagnostics, Tests, And Visual QA

Status: Complete

Validation checklist:

- [x] EditMode: plane capacity resolves above zero.
- [x] EditMode: plane cargo capacity accepts eligible vehicles.
- [x] EditMode: APC/helicopter still reject vehicle passengers.
- [x] EditMode: plane boarding requires landed/staged state.
- [x] EditMode: plane disembark while grounded chooses ramp unload.
- [x] EditMode: plane disembark while airborne chooses parachute/cargo drop.
- [x] EditMode: landed plane with explicit airdrop target creates a takeoff-to-airdrop request.
- [x] EditMode: airborne airdrop waits for fixed-wing pass readiness before releasing passengers.
- [x] EditMode: soldier parachute drop restores passenger to valid ground cell.
- [x] EditMode: vehicle cargo drop restores vehicle to valid footprint.
- [x] EditMode: soldier airdrop touchdown starts and completes settle/disperse.
- [x] EditMode: vehicle cargo airdrop touchdown starts and completes rollout/settle.
- [x] EditMode: focused transport read model publishes cargo-plane soldier/vehicle capacity breakdown.
- [x] EditMode: transport feedback reports boarding, cargo loading, full transport, airdrop progress, and blocked cargo drop messages.
- [x] EditMode: Board-mode preview accepts vehicle passengers only for cargo-capable transport planes.
- [x] EditMode: airdrop door request remains open briefly after final passenger release, then closes.
- [x] EditMode: soldier parachute visual tracks the descending soldier with stable height offset and scale.
- [x] EditMode: vehicle emergency-drop visual tracks the descending vehicle with stable height offset and scale.
- [x] PlayMode: soldiers board through rear ramp and disappear inside plane.
- [x] PlayMode: vehicles board through rear ramp and disappear inside plane.
- [x] PlayMode: landed unload opens rear door and passengers exit from back.
- [x] PlayMode: airborne soldier airdrop shows parachutes and lands cleanly.
- [x] PlayMode: airborne vehicle drop shows emergency drop visual and lands cleanly.
- [x] Automated visual QA: no drops under terrain, inside plane, or at wrong scale in deterministic ECS PlayMode coverage.
- [x] Automated visual QA: door/ramp timing stays connected to boarding/unload with deterministic open/close assertions.

Validation log:

- 2026-06-16: `git diff --check` passed.
- 2026-06-16: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=31`.
- 2026-06-16: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-rts-selection-input-validation.log`
  - Result: `[RtsSelectionInputSystemValidation] result=Passed tests=55`.
- 2026-06-16: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=33`.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=37`.
- 2026-06-16: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=33`.
  - Covered grounded cargo-plane ramp exit placement plus visible soldier/vehicle rollout move orders.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=34`.
  - Covered blocked grounded ramp unload feedback: passenger remains onboard and command result reports `NoDisembarkCell`.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=35`.
  - Covered airborne cargo-plane Disembark creating `UnitTransportAirdropRequest`, counting soldier/vehicle drops, preserving onboard passengers, and avoiding helicopter rope state.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=39`.
  - Covered landed explicit target-cell airdrop request, fixed-wing pass readiness before release, passenger release after pass readiness, and return-home after airdrop sequence completion.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `rg -n "Object\.Instantiate|Object\.Destroy|Resources\.Load|FindObjectOfType|GameObject\.Find|SystemBase|MonoBehaviour|TransportPlaneManager|AirdropController|TransportFacade" Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs Assets/Game/Scripts/Systems/UnitTransportPlaneDoorSystem.cs Assets/Game/Scripts/Components/GridComponents.cs`
  - Result: no forbidden managed runtime bridge patterns found.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=41`.
  - Covered soldier airdrop touchdown settle/disperse and vehicle cargo touchdown rollout/settle.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=42`.
  - Covered focused transport read-model total/soldier/vehicle cargo-plane capacity breakdown while preserving soldier-only transport capacity rows.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `rg -n "Object\.Instantiate|Object\.Destroy|Resources\.Load|FindObjectOfType|GameObject\.Find|SystemBase|MonoBehaviour|TransportPlaneManager|AirdropController|TransportFacade" Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs Assets/Game/Scripts/Systems/UnitTransportPlaneDoorSystem.cs Assets/Game/Scripts/Components/GridComponents.cs Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs`
  - Result: no forbidden managed runtime bridge patterns found.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=47`.
  - Covered transport command feedback strings, blocked cargo-drop feedback, and Board-mode preview rules for cargo-plane vehicle passengers.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `rg -n "Object\.Instantiate|Object\.Destroy|Resources\.Load|FindObjectOfType|GameObject\.Find|SystemBase|MonoBehaviour|TransportPlaneManager|AirdropController|TransportFacade" Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs Assets/Game/Scripts/Systems/UnitTransportPlaneDoorSystem.cs Assets/Game/Scripts/Components/GridComponents.cs Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs`
  - Result: no forbidden managed runtime bridge patterns found.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=50`.
  - Covered airdrop door close delay after final passenger release, parachute visual tracking above soldiers during descent, and emergency-drop visual tracking above vehicles during descent.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-Clone -runTests -testPlatform PlayMode -testFilter GameSceneTransportBoardingPlayModeTests -testResults /private/tmp/warline-transport-playmode-results.xml -logFile /private/tmp/warline-transport-playmode.log`
  - Result: `GameSceneTransportBoardingPlayModeTests` passed 7/7.
  - Covered helicopter regression, transport-plane soldier rear-ramp boarding, transport-plane vehicle rear-ramp boarding, grounded rear-ramp unload, soldier parachute airdrop touchdown, and vehicle emergency-drop touchdown.
  - During this validation, a real bug was found and fixed: transport-plane passengers reached the rear-ramp cell but did not board because air-transport reach used only the aircraft center. `UnitTransportBoardingSystem` now accepts exact reached ramp goals for transports with `UnitTransportPlaneDoorReference`.
  - Also fixed cargo passenger tagging so soldiers boarding a cargo plane remain regular `UnitTransportPassenger` entries and only vehicles get `UnitTransportCargoPassenger`.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `rg -n "Object\.Instantiate|Object\.Destroy|Resources\.Load|FindObjectOfType|GameObject\.Find|SystemBase|MonoBehaviour|TransportPlaneManager|AirdropController|TransportFacade" Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs Assets/Game/Scripts/Systems/UnitTransportPlaneDoorSystem.cs Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs Assets/Game/Scripts/Components/GridComponents.cs Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs`
  - Result: no forbidden managed runtime bridge patterns found.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=50`.
  - Revalidated the broader transport EditMode suite after the rear-ramp boarding reach and cargo-tagging fixes.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-Clone -runTests -testPlatform PlayMode -testFilter GameSceneTransportBoardingPlayModeTests -testResults /private/tmp/warline-transport-playmode-results.xml -logFile /private/tmp/warline-transport-playmode.log`
  - Result: `GameSceneTransportBoardingPlayModeTests` passed 7/7.
  - Covered boarding door opens while soldier/vehicle rear-ramp boarding is pending, closes after boarding completes, stays open during landed unload, and closes after landed unload hold expires.
- 2026-06-17: `git diff --check` passed.
- 2026-06-17: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-validation.log`
  - Result: `[UnitTransportValidation] result=Passed tests=50`.
  - Revalidated the broader transport EditMode suite after adding deterministic PlayMode ramp timing assertions.

## Implementation Order

1. Add cargo/capacity ECS data model.
2. Add authoring/baker support for transport plane cargo capacity and airdrop visual entity-prefab registry.
3. Add rear door/ramp pure ECS runtime owner and shared plane door anchors.
4. Extend boarding eligibility for plane soldiers and vehicles.
5. Implement rear-ramp boarding.
6. Implement landed ramp unload.
7. Implement airborne airdrop command setup.
8. Implement soldier parachute drop.
9. Implement vehicle emergency cargo drop.
10. Tune transport plane high cruise/aerial pass behavior.
11. Update HUD/read model feedback.
12. Add focused tests and visual QA.

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
