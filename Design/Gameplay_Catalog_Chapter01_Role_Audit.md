# WarlineCapture Gameplay Catalog / Chapter 1 Role Audit

Date: 2026-05-06

## Purpose

This audit checks whether the current gameplay design docs and catalogs define the units/buildings needed by Chapter 1, whether catalog items have gameplay specs, and whether Chapter 1 has role gaps or ID mismatches.

Checked sources:

- `Design/Combat_Catalog_And_Upgrade_Design.md`
- `Design/BalanceConfigs/Combat_Balance_Config_v0_1.json`
- `Design/VisualConfigs/Combat_Visual_Config_v0_1.json`
- `Design/Level_And_Mission_Content_Plan.md`
- `Design/SagaChapters/Saga_Chapter01_First_Response.md`
- `Assets/Game/Scripts/Campaign/ChapterOneMissionCatalog.cs`

## Short Answer

The catalog is not empty or undesigned. All 57 units and all 30 buildings in the balance config have core gameplay data such as id, display name, implementation status, cost, stats, unlock gates, visual catalog id, and relevant abilities/production links.

However, Chapter 1 is not cleanly aligned yet:

- Chapter 1 design references valid catalog IDs.
- Current runtime Chapter 1 rewards still use old/non-catalog IDs.
- Some Chapter 1 mission roles are described in prose but do not resolve to concrete catalog IDs.
- Buildings do not have explicit `roleTags`, so building roles must be inferred from family docs, production lists, stats, and abilities.

## Catalog Spec Coverage

### Units

Unit catalog coverage is strong.

- 51 units are `implemented`.
- 6 sea units are `designReadyNeedsUnityPrefab`.
- Every unit has `roleTags`, `stats`, `cost`, `unlock`, `abilityIds`, `upgradeTrackId`, and `visualCatalogId`.

The sea units are designed but not production-ready:

- `Unit_Sea_Coastal_Cutter`
- `Unit_Sea_Drone_Boat`
- `Unit_Sea_Interceptor_Boat`
- `Unit_Sea_Landing_Craft`
- `Unit_Sea_Missile_Craft`
- `Unit_Sea_Patrol_Boat`

### Buildings

Building catalog coverage is usable, but less explicit than units.

- 24 buildings are `implemented`.
- 6 buildings are `designReadyNeedsUnityPrefab`.
- Every building has core cost/stat/unlock/visual data.
- No building currently has explicit `roleTags`.

Buildings needing Unity prefab production:

- `Building_CoastalRadar`
- `Building_CommandPost`
- `Building_Dock`
- `Building_FieldWorkshop`
- `Building_MedicalStation`
- `Building_NavalYard`

Recommendation: add `roleTags` to every building, matching the family roles in `Combat_Catalog_And_Upgrade_Design.md`.

## Chapter 1 Role Coverage

### M01 First Contact

Required roles:

- player rifle squad
- hostile patrol
- civilian block / protectable civilians

Catalog coverage:

- Rifleline infantry exists through multiple `Unit_Chr_Soldier_*` entries.
- Enemy irregulars exist through `Unit_Chr_Insurgent_*`.
- Civilians exist through `Unit_Chr_Civilian_*`.

Gap:

- Chapter 1 prose says "rifle squad" and "hostile patrol", but does not pin the exact unit IDs for the start roster and enemy group.
- Runtime reward now uses `Unit_Chr_Soldier_Male_02_Alt_04`; exact hostile patrol composition is still unassigned.

### M02 Establish The Base

Required roles:

- forward barracks/tent
- produce one rifle squad
- small delayed patrol
- optional road barrier

Catalog coverage:

- `Building_Barrack` exists and unlocks at `ch01.m02.production_loop`.
- `Tent_Regular` exists and produces rifleline units.
- `Building_Road_Barrier` exists.

Gaps:

- `Building_Barrack` has no `productionUnitIds` and no `ability.produce_unit`, while the mission says the player builds barracks and produces a squad.
- `Tent_Regular` is the catalog item that actually produces rifle units.
- Runtime reward now uses `Building_Barrack`; production behavior still needs the Barrack versus Tent decision.
- `Building_Road_Barrier` is allowed in the Chapter 1 mission prose, but its balance unlock gate is `ch02.operation_infrastructure`.

Decision needed:

- Either make `Building_Barrack` the Chapter 1 producer and give it rifle production data, or rewrite M02 to require `Tent_Regular` as the production structure.

### M03 Radar Warning

Required roles:

- convoy attack
- guard tower / radar preparation
- road barrier
- threat warning / radar ping

Catalog coverage:

- `Building_GuardTower` exists and unlocks at `ch01.m03.defense_unlock`.
- `Building_Satelite_Dish` exists and has `threatDetectionRadiusCells`.
- `ability.radar_ping` exists.
- `Unit_Veh_Radar_Tank` exists and unlocks at `ch01.m03.radar_warning`.

Gaps:

- Runtime reward now uses the documented `ability.radar_ping`.
- `Building_Road_Barrier` gate still conflicts with Chapter 1 usage.
- The convoy group is described, but exact enemy vehicle/unit IDs are not assigned in the mission spec.

### M04 Airlift

Required roles:

- transport helicopter or APC support path
- extract/reinforce endangered group
- transport survival objective
- evacuation corridor ability

Catalog coverage:

- `Unit_Veh_Helicopter_Transport` exists and unlocks at `ch01.m04.airlift`.
- `Unit_Veh_Plane_Transport` exists and unlocks at `ch01.m04.airlift`.
- `Building_Helipad` and `Building_Airport` exist and unlock at `ch01.m04.airlift`.
- `skill.load_transport`, `skill.unload_transport`, and `skill.rope_disembark` exist.
- `ability.evacuation_corridor` exists.

Gaps:

- Runtime reward now uses the documented `ability.evacuation_corridor`; the playable tutorial transport still needs a canonical unit selection.
- Mission prose does not choose one canonical transport unit for the tutorial. It says "one transport helicopter or APC support path".

Decision needed:

- Use `Unit_Veh_Helicopter_Transport` as the canonical M04 tutorial transport unless we introduce a catalog alias for Blackhawk.

### M05 Breach Assault

Required roles:

- rifle squads
- APC support
- breach wall/gate target
- fortified enemy core
- counterattack wave
- vehicle survival star

Catalog coverage:

- `Unit_Veh_APC_Heavy` exists and matches the mission prose.
- `Unit_Chr_Bombsuit_Male_01` and `Unit_Chr_Soldier_Female_02_Alt_02` cover breach roles.
- `skill.breach_gate` exists.
- `Unit_Chr_Ghillie_Male_01` exists as the reward.
- `upgrade.vehicle.apc_armor` exists.
- `Wall_Dirt_Straight` and `Wall_Fence_Straight` exist as wall-like catalog items.

Gaps:

- Runtime reward now uses the documented `Unit_Chr_Ghillie_Male_01`; APC support remains covered by the upgrade path.
- The mission does not assign a concrete catalog ID for the breach wall/gate target.
- The mission does not assign a concrete catalog ID for the fortified enemy core. `Building_CommandPost` could fit, but it is `designReadyNeedsUnityPrefab` and currently gated to Chapter 2 in the balance config.
- `ability.breach_charge` is gated to Chapter 3 in the balance config, while Chapter 1 M05 uses breach gameplay through `skill.breach_gate`. This is probably acceptable if M05 teaches the skill rather than the support ability, but the distinction should be explicit.

## Catalog Items With No Chapter 1 Role

This is expected and mostly healthy. Chapter 1 is a teaching chapter, not the full game catalog.

Major groups intentionally outside Chapter 1:

- sea units and naval buildings: Chapter 4+ coastal/naval content
- jets, attack helicopters, missile launchers, tanks, most trucks: Chapter 4+ air/armor/logistics escalation
- medical, repair, field workshop, command post: Chapter 2+ infrastructure/command progression
- most support abilities: later chapters or Operation actions
- most upgrade tracks: progression/Armory/Store surfaces, not first chapter tactical requirements

The issue is not that these items lack a Chapter 1 role. The issue is that Chapter 1 needs a small canonical subset, and that subset should be exact and consistent across docs, balance config, runtime code, UI, and art.

## Concrete ID Mismatches

Runtime Chapter 1 unlock IDs were updated to catalog/design IDs on 2026-05-06:

| Previous runtime ID | Current runtime ID |
|---|---|
| `unit.rifle_squad` | `Unit_Chr_Soldier_Male_02_Alt_04` |
| `building.forward_outpost` | `Building_Barrack` |
| `support.radar_scan` | `ability.radar_ping` |
| `unit.blackhawk_transport` | `ability.evacuation_corridor` |
| `unit.apc` | `Unit_Chr_Ghillie_Male_01` |

Remaining design decision: M04 mission prose still needs to explicitly say whether the playable transport tutorial uses `Unit_Veh_Helicopter_Transport`, while the first-clear reward now follows the documented support unlock `ability.evacuation_corridor`. M05 still has the APC armor upgrade path in the design docs, but the runtime first-clear unlock now follows the documented unit reward `Unit_Chr_Ghillie_Male_01`.

## Missing Roles For Chapter 1

These roles exist conceptually in the mission docs but need concrete catalog/config assignments:

| Mission | Missing concrete assignment |
|---|---|
| M01 | exact player rifle squad unit ID and exact hostile patrol composition |
| M02 | whether the production structure is `Building_Barrack` or `Tent_Regular` |
| M03 | exact convoy enemy composition and whether road barrier should unlock in Chapter 1 |
| M04 | exact tutorial transport unit, preferably `Unit_Veh_Helicopter_Transport` |
| M05 | exact wall/gate target ID, exact enemy core building ID, exact APC variant |

## Recommended Next Step

Create a Chapter 1 scenario catalog that resolves every prose role to exact IDs:

- `scenario.ch01.m01.first_contact`
- `scenario.ch01.m02.establish_base`
- `scenario.ch01.m03.radar_warning`
- `scenario.ch01.m04.airlift`
- `scenario.ch01.m05.breach_assault`

Each scenario should list:

- player start units
- enemy start units
- build catalog
- production catalog
- support abilities
- required target buildings
- reward target IDs
- map/level art IDs

Do this before producing final art for Chapter 1, because the art checklist depends on knowing the exact unit/building IDs that will actually appear.
