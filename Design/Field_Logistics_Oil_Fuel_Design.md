# WarlineCapture Field Logistics: Oil And Fuel Design

Date: 2026-05-22

This document formalizes the existing Oil/Fuel gameplay already present in building and vehicle configs. Oil and Fuel are tactical match logistics resources, not replacements for the main menu resources.

## Source Of Truth

Use this design when implementing or reviewing:

- Oil Pump
- Oil Refinery
- Large Oil Refinery
- Fuel Bladder / Fuel Depot
- oil transport trucks
- fuel tanker trucks
- vehicle/air fuel costs
- tactical resource strips in base-building, vehicle, air, and Skirmish matches

Related economy source:

- `Economy_Reward_Design.md`

## Resource Positioning

| Resource | Layer | Meaning | Where Shown |
|---|---|---|---|
| `Credits` | Account + tactical display label | Money/spendable currency. | Main Menu, Build Drawer, Mission Result, Store, Operation. |
| `Supplies` / `Materials` | Account/logistics display layer | Military supplies, materials, construction stock. | Main Menu, Build Drawer, Operation, rewards. |
| `Command` / `Command Authority` | Account authority layer | Premium/authority resource for strong actions, convenience, cosmetics, or controlled unlocks. | Main Menu, Store, Commander Profile. |
| `Oil` | Tactical match raw resource | Raw extracted resource from oil deposits. | Only in missions where extraction/refining matters. |
| `Fuel` | Tactical + account logistics resource | Processed mobility resource for vehicles, air, deployment, extraction, or readiness. | Battle HUD/Build Drawer in fuel missions; rewards/account only through authored grants. |

Main Menu rule:

- Main Menu shows compact top-level resources: Credits, Supplies, Command.
- Oil is never a main menu resource.
- Fuel is not shown in the main menu top strip unless a future top-level economy review explicitly adds it.

Match rule:

- Battle HUD and Build Drawer may show Oil/Fuel only when the active mission, Skirmish preset, or Operation event uses fuel logistics.
- M01 does not use Oil/Fuel.

## Tactical Logistics Loop

The intended loop is:

```text
Oil Deposit
  -> Oil Pump extracts raw Oil
  -> Oil Truck moves Oil to Refinery or storage
  -> Oil Refinery converts Oil into Fuel
  -> Fuel Bladder / Fuel Depot stores Fuel
  -> Tanker Truck moves Fuel or refuels units
  -> Vehicles, aircraft, generators, radar, or support actions consume Fuel
```

## Existing Config-Aligned Roles

| Config / Gameplay Object | Player-Facing Role | Gameplay Function |
|---|---|---|
| `Prefab_BuildingDefinition_OilPump_Config.asset` | Oil Pump | Extracts raw Oil from nearby deposits. |
| `Prefab_BuildingDefinition_OilRefinery_Config.asset` | Oil Refinery | Converts Oil into usable Fuel and related logistics output. |
| `Prefab_BuildingDefinition_OilRefinery_Big_Config.asset` | Large Oil Refinery | Higher-capacity refinery for larger fuel-production missions. |
| `Prefab_BuildingDefinition_Fuel_Bladder_Config.asset` | Fuel Bladder | Temporary field storage for Fuel; supports vehicles, aircraft, and logistics. |
| `Prefab_UnitGrid_Veh_Truck_Tray.asset` | Oil transport truck | Moves Oil barrels from pumps/storage to refinery or storage. |
| `Prefab_UnitGrid_Veh_Truck_Tanker.asset` | Fuel tanker truck | Moves Fuel to storage or refuels vehicles directly. |

Do not delete these configs. Treat them as existing production intent that needs clear UI/gameplay wiring.

## Gameplay Purpose

Oil/Fuel gives base-building and vehicle-heavy missions a field logistics layer:

- Protect oil pumps and refinery infrastructure.
- Raid enemy fuel logistics.
- Decide whether to spend Fuel on vehicles, aircraft, radar, generators, or support.
- Make transport trucks and tanker trucks meaningful tactical targets.
- Create objective types such as capture refinery, destroy fuel depot, escort tanker, or hold oil field.
- Prevent vehicle/air spam in Skirmish by tying heavy mobility to field logistics.

## Build Menu Integration

Build Drawer tab behavior:

| Tab | Oil/Fuel Role |
|---|---|
| Buildings | Contains Oil Pump, Oil Refinery, Large Oil Refinery, Fuel Bladder/Fuel Depot when mission allows fuel logistics. Buildings require map placement. |
| Vehicles | Vehicles may cost Fuel or require Fuel production/storage. Tanker and oil trucks appear here when logistics is enabled. Vehicles spawn from valid base/vehicle bay/rally locations. |
| Soldiers | Usually do not cost Fuel. Specialist support squads may require Credits/Supplies but should not consume Oil directly. |

UI rule:

- Oil/Fuel costs must be live text/icons, not baked into cards.
- Disabled production rows must show exact reasons: `NoOilDeposit`, `NoRefinery`, `NoFuelStorage`, `InsufficientFuel`, `MissionDoesNotUseFuel`, `NoVehicleDepot`, or equivalent typed ids.

## Match HUD Integration

When a mission uses fuel logistics, the Battle HUD should show:

- tactical Credits/Money
- Fuel
- Oil only if raw extraction is active and player needs to understand the conversion chain
- build capacity / population if relevant

When a mission does not use fuel logistics:

- Hide Oil/Fuel from the Battle HUD.
- Do not show fuel costs on units.
- Do not show disabled fuel buildings unless they are intentionally previewed as locked/unavailable.

## Player Acquisition

Players acquire tactical Oil/Fuel through:

- building Oil Pumps near oil deposits
- protecting or capturing oil infrastructure
- transporting Oil to a refinery
- converting Oil to Fuel at refineries
- building Fuel Bladder/Fuel Depot storage
- completing fuel objectives in a match
- Skirmish preset starting grants, if configured

Players acquire account-level Fuel only through authored rewards:

- Mission Result `RewardConfig`
- Operation rewards
- event/season/profile rewards
- capped store bundles if monetization allows it

Tactical Oil is not sold in the store and is not banked directly after match end.

## Spending And Conversion Rules

| Source | Conversion / Spend |
|---|---|
| Oil Pump | Produces tactical Oil over time if connected/valid. |
| Oil Truck | Moves Oil; does not create resources by itself. |
| Oil Refinery | Converts tactical Oil into tactical Fuel. |
| Fuel Bladder / Depot | Raises storage or enables local refuel. |
| Tanker Truck | Moves Fuel or refuels units; does not create Fuel by itself. |
| Vehicle production | May spend Credits + Fuel, depending on unit. |
| Air production/support | May spend Fuel and/or Command depending on mission rules. |
| Match result | May grant account Fuel through `RewardConfig`; never auto-banks all tactical Fuel unless the mission explicitly rewards it. |

## Mission Usage

| Mission Type | Oil/Fuel Use |
|---|---|
| M01 tutorial | No Oil/Fuel. |
| Early build tutorial | Usually no Oil/Fuel; introduce Build with Credits/Supplies first. |
| Vehicle/air tutorial | Introduce Fuel as a production/deployment requirement. |
| Base-building Skirmish | Fuel economy can be enabled through setup/preset. |
| Logistics objective | Oil/Fuel is central: capture, protect, convoy, refinery, depot, or sabotage. |
| Operations events | Fuel can be an account/logistics cost or reward, not raw Oil. |

## AI And Balance Rules

AI should understand logistics when fuel economy is active:

- build or capture Oil Pump equivalents
- protect refineries and fuel storage
- target player fuel infrastructure when appropriate
- avoid producing fuel-cost units if Fuel is unavailable
- use tanker/oil trucks only when route and storage/refinery targets exist

Balance metrics should track:

- Oil extracted
- Fuel produced
- Fuel spent
- fuel infrastructure built/destroyed
- tanker/oil truck losses
- vehicle/air units delayed by fuel shortage

## UI Copy

Use clear player-facing language:

- Oil Pump: "Extracts Oil from deposits."
- Oil Refinery: "Turns Oil into Fuel."
- Fuel Bladder: "Stores Fuel for vehicles and aircraft."
- Oil Truck: "Moves Oil to refineries."
- Tanker Truck: "Delivers Fuel and refuels vehicles."
- Insufficient Fuel: "Need more Fuel."
- No Refinery: "Build a Refinery to turn Oil into Fuel."

## Acceptance Tests

Implementation should prove:

- Oil Pump, Refinery, Fuel Bladder, Oil Truck, and Tanker Truck configs remain discoverable.
- Build Drawer shows Oil/Fuel buildings only when the mission allows fuel logistics.
- Vehicle rows can require Fuel and show `InsufficientFuel` when short.
- Tactical Oil converts to Fuel only through refinery/production gameplay.
- Fuel storage/capacity affects production or refuel rules when active.
- M01 has no Oil/Fuel HUD or build dependency.
- Main Menu top strip remains Credits, Supplies, Command.
- Tactical Oil is not banked into account wallet at match end unless authored by result rewards.
