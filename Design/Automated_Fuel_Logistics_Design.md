# WarlineCapture Automated Fuel Logistics Design

Date: 2026-07-05
Status: Proposed design for implementation planning

## Source Order

Read these files together before changing Oil/Fuel gameplay:

1. `Field_Logistics_Oil_Fuel_Design.md`
2. `Automated_Fuel_Logistics_Design.md`
3. `Economy_Reward_Design.md`
4. `Combat_Catalog_And_Upgrade_Design.md`
5. `Gameplay_Features_High_Level_Spec.md`
6. `Architecture/gameplay_solid_ecs_contract.md`
7. `Architecture/performance_regression_contract.md`
8. `Architecture/automated_fuel_logistics_implementation_tracker.md`

`Field_Logistics_Oil_Fuel_Design.md` remains the catalog-aligned baseline for Oil Pump, Refinery, Fuel Bladder, oil truck, and tanker roles. This document defines the player-facing automation model and the meaning of usable Fuel.

## Goal

Fuel should create strategic pressure without requiring slow truck micromanagement. The player should build, protect, expand, and attack logistics infrastructure. The simulation should automatically move Oil and Fuel through the chain with readable feedback, stable performance, and ECS-first data ownership.

The desired loop is:

```text
Oil Pump
  -> automatic tray truck pickup
  -> Refinery input buffer
  -> refinery conversion
  -> Refinery fuel output buffer
  -> automatic tanker pickup
  -> Fuel Bladder / base fuel storage
  -> faction usable Fuel pool
  -> vehicle and aircraft mobility spending
```

## Verification Fixture Requirement

Fuel-enabled maps should not require the player or tester to build logistics trucks before the automated loop can be verified. Each faction military base should have at least:

- one `Unit_Veh_Truck_Tray`
- one `Unit_Veh_Truck_Tanker`

These units should be placed near the faction's military base or logistics yard and baked through the same entity conversion path as normal map units. They are not test-only mock objects and should not use a separate runtime spawn path unless the scenario already owns faction starting-unit setup through data.

Rules:

- Faction 1 and Faction 2 fuel-enabled military bases should each have the seeded tray/tanker pair.
- The seeded units should be selectable and commandable like normal vehicles.
- The seeded units should be eligible for automated logistics assignment immediately after match initialization.
- Non-fuel tutorial missions can omit them when fuel logistics is explicitly disabled.
- Validation should fail or warn when a fuel-enabled map has no tray truck or no tanker near a faction military base.

## Design Pillars

- Logistics is a strategic layer, not direct truck-target micro.
- The header Fuel value is usable faction Fuel, not raw Oil and not refinery output waiting for delivery.
- Oil trucks and tanker trucks are autonomous logistics workers that stop when no valid work exists.
- Vehicle fuel use is faction-level and readable. It is not a deep per-vehicle refuel simulation.
- Aircraft never stop in the air because the faction fuel pool is empty.
- Player decisions come from infrastructure, route protection, truck count, storage capacity, and fuel spending priorities.
- Runtime implementation must stay ECS-first, Burst/job-friendly, allocation-free on hot paths, and compatible with the existing SOLID/ECS contract.

## Resource Meanings

| State | Meaning | Header Fuel? | Primary Owner |
|---|---|---:|---|
| Raw Oil at pump | Extracted Oil waiting for a tray truck. | No | Oil Pump storage/buffer. |
| Oil in transit | Oil carried by `Unit_Veh_Truck_Tray`. | No | Logistics truck cargo component. |
| Oil at refinery | Raw Oil accepted by a refinery input buffer. | No | Refinery storage/buffer. |
| Refinery output Fuel | Fuel produced by conversion but not yet delivered to storage. | No | Refinery output buffer. |
| Fuel in transit | Fuel carried by `Unit_Veh_Truck_Tanker`. | No | Tanker cargo component. |
| Stored usable Fuel | Fuel delivered to `Building_Fuel_Bladder`, base fuel storage, or equivalent faction storage. | Yes | Faction fuel storage summary. |
| Account Fuel | Persistent reward/resource outside a tactical match. | Not the match header unless explicitly mapped. | Economy/reward systems. |

Important rule: Fuel becomes the shared match header pool only when it is delivered into faction fuel storage. Refineries produce Fuel, but their output is a local buffer until a tanker moves it to storage. This keeps Fuel Bladders meaningful and keeps logistics trucks tactically relevant.

## Automated Logistics Behavior

### Tray Truck Oil Hauling

`Unit_Veh_Truck_Tray` should automatically:

- find friendly Oil Pump buffers with available Oil
- find friendly refineries with input capacity
- reserve a pickup amount and destination before moving
- travel to pickup, load Oil, travel to destination, unload Oil
- idle when no source, no destination, no route, or no refinery capacity exists
- resume when any relevant source, destination, route, or capacity changes

The player should not need to select a tray truck and manually target pumps/refineries for the normal economy loop.

### Refinery Conversion

Refineries should:

- convert Oil from their input buffer into output Fuel over time
- respect conversion rate, efficiency, and output capacity
- stall when there is no Oil input
- stall or cap when output Fuel has no buffer capacity
- publish clear selected-building status: Oil input, Fuel output, rate, blocked reason

Large refineries use the same model with different balance values.

### Tanker Fuel Hauling

`Unit_Veh_Truck_Tanker` should automatically:

- find friendly refinery output buffers with available Fuel
- find friendly Fuel Bladder/base storage with free capacity
- reserve a pickup amount and destination before moving
- load Fuel, deliver Fuel, and update faction usable Fuel
- idle when no output Fuel, no storage capacity, no route, or no valid storage exists
- resume from dirty/versioned logistics changes

The normal loop should not require a player to click tanker targets.

## Vehicle Fuel Consumption

Fuel consumption is a faction-level mobility pressure, not a detailed per-vehicle tank model.

Recommended behavior:

- Ground vehicles and aircraft consume usable faction Fuel while performing movement-heavy or fuel-heavy actions.
- New vehicle movement, production, launch, or support orders should be blocked or warned when usable Fuel is below the required reserve.
- Existing ground movement can finish a short committed segment, return, or hold based on unit/domain policy.
- Aircraft must never freeze midair. If usable Fuel is exhausted, active aircraft should transition to return-to-base or emergency-reserve behavior, and new launches/support orders should be blocked.
- Fuel consumption should be aggregated by vehicle class and action type so UI can explain the drain without per-frame string building.

The player-facing mental model is: "Your army uses stored Fuel when vehicles move or aircraft operate. Build the logistics chain to keep the shared Fuel pool available."

## Storage And Capacity

Fuel Bladders and base fuel storage should matter because they define the usable Fuel pool.

Recommended first implementation:

- Fuel Bladder increases faction usable Fuel capacity.
- Delivered Fuel fills available storage.
- If storage is full, tankers idle and refinery output backs up.
- If no storage exists, refineries can produce into a small local buffer but cannot increase the header Fuel pool.

Future optional expansion:

- storage damage can spill or lock Fuel
- local storage can define operational radius
- tanker route safety can affect throughput
- base-level reserve policy can protect aircraft return behavior

## Player-Facing Feedback

The Battle HUD should show usable Fuel. If the mission also teaches extraction, it may show Oil separately.

Required feedback:

- Header Fuel shows delivered usable Fuel and capacity when capacity is relevant.
- Oil Pump selection shows Oil stored, extraction rate, and pickup status.
- Refinery selection shows Oil input, Fuel output, conversion rate, and blocked reason.
- Fuel Bladder selection shows stored Fuel, capacity, and delivery status.
- Truck selection shows current autonomous task: idle, moving to pickup, loading, delivering, unloading, blocked.
- Disabled vehicle/air actions use typed reasons such as `InsufficientFuel`, `NoFuelStorage`, `NoRefinery`, `NoLogisticsTruck`, or `FuelRouteBlocked`.

UI should consume versioned ECS read models and avoid rebuilding logistics summaries every frame.

## Player Decisions

The intended decisions are:

- build more Oil Pumps, refineries, storage, or logistics trucks
- protect routes and fuel buildings
- raid enemy logistics
- choose whether to spend Fuel on tanks, aircraft, transports, support, or extraction
- recover after logistics disruption

The intended decision is not:

- repeatedly select each tray truck or tanker
- manually assign every pickup and drop-off
- inspect hidden per-vehicle fuel tanks during normal play
- watch aircraft stop in the sky because the header value reached zero

## Balance Knobs

| Knob | Purpose |
|---|---|
| Oil extraction rate | Controls raw economy input. |
| Pump buffer capacity | Controls pickup cadence and overflow pressure. |
| Tray truck cargo capacity | Controls Oil throughput. |
| Tray truck speed | Controls route value and distance penalty. |
| Refinery conversion rate | Controls Fuel production pace. |
| Refinery efficiency | Controls Oil-to-Fuel ratio. |
| Refinery input/output capacity | Controls bottlenecks and stall behavior. |
| Tanker cargo capacity | Controls Fuel delivery throughput. |
| Tanker speed | Controls storage fill cadence. |
| Fuel storage capacity | Controls usable pool ceiling. |
| Vehicle movement Fuel cost | Controls ground mobility pressure. |
| Aircraft operation Fuel cost | Controls air power pressure. |
| Emergency aircraft reserve | Prevents bad aircraft failure behavior. |

## ECS And Performance Expectations

Implementation should keep simulation state in ECS components and dynamic buffers. Hot-path logistics assignment, conversion, delivery, and fuel consumption should use unmanaged `ISystem` implementations with Burst-compatible jobs wherever practical.

Runtime UI and GameObject presentation may remain managed only as narrow boundaries that consume versioned read models. They must not own the source of truth, scan all logistics entities every frame, or create garbage during steady-state play.

No new manager/controller/facade pattern should be introduced for this feature. No new updating `MonoBehaviour` loop should be introduced for the simulation.

## Non-Goals

- Full per-vehicle fuel tanks.
- Manual truck target micro as the primary logistics loop.
- Direct Oil banking into the header Fuel pool.
- Direct refinery output banking into the header Fuel pool before delivery.
- Aircraft stopping midair when the usable Fuel pool reaches zero.
- Replacing current building, production, selection, or UI architecture with broad new shells.

## Open Questions

- Should Fuel Bladder range matter in the first implementation, or only capacity? Recommendation: capacity first, operational radius later.
- Should active ground vehicles consume Fuel continuously, per grid segment, or per accepted command? Recommendation: aggregate per movement tick by vehicle class, with stable fixed-point counters.
- Should tactical match Fuel ever bank into account Fuel at match end? Recommendation: only through authored reward configs, not automatic leftover banking.
- Should enemy factions use the same automated logistics rules in the first slice? Recommendation: support the data model for all factions, but validate player-faction behavior first.
