# AI Controller Design

This document captures the planned AI controller architecture so the feature can be implemented incrementally and referenced by step number.

## Goal

Implement faction-agnostic AI controllers that can control enemy factions and optionally control the player faction when Auto Mode is enabled.

The AI should be able to:

- Build its own camp and expand it over time.
- Produce units, vehicles, and buildings from its own economy.
- Own its own money, oil, and fuel resources.
- Generate revenue by selling oil and fuel.
- Form squads and attack enemy units/buildings based on difficulty and AI settings.
- Control the player's faction when player Auto Mode is enabled.
- Stop controlling the player's faction when Manual Mode is enabled.

## Core Architecture

The AI should not be enemy-only. It should be faction-based.

```text
AIController
  controls one faction
  owns economy policy
  owns build policy
  owns production policy
  owns squad policy
  owns attack/defense policy
  issues the same ECS orders as the player
```

Example setup:

```text
Player Faction 0
  Manual Mode:
    player issues commands

  Auto Mode:
    PlayerAIController controls faction 0

Enemy Faction 1
  EnemyAIController controls faction 1
```

The same AI systems should work for enemy factions and for the player faction in Auto Mode.

## Step Summary

Use these short names when referring to future implementation work.

1. `AI Configs`
   Create AI config assets for difficulty, economy, production, build priorities, attack settings, and player Auto Mode.

2. `Faction Economy`
   Add per-faction money, oil, fuel, income, selling, and resource spending.

3. `AI Ownership And Control`
   Add control mode so a faction can be Manual or AI controlled. Add the player Auto/Manual UI toggle.

4. `AI Building Placement`
   Let AI choose a base center, reserve cells, pay costs, and place camp/building structures.

5. `AI Unit Production`
   Let AI use owned production buildings to queue and spawn soldiers/vehicles based on config and available resources.

6. `AI Squads`
   Group units into squads instead of making every unit decide independently.

7. `AI Target Selection`
   Score enemy targets such as production buildings, resource buildings, isolated units, and threats.

8. `AI Combat Orders`
   Issue movement and attack orders to squads using the same ECS command path as player orders.

9. `Player Auto Mode`
   Reuse the same AI systems for the player faction when Auto Mode is enabled.

10. `Tests And Validation`
    Add PlayMode tests and validation logs for economy, building, production, squads, targeting, and Auto Mode.

## Step 1: AI Configs

Create config assets first. No behavior should depend on hardcoded values.

Suggested assets:

```text
Game_AI_Difficulty_Config
Game_AI_PlayerAuto_Config
Game_AI_Enemy_Config
```

Suggested fields:

```text
factionId
enabled
difficulty
startingMoney
incomeMultiplier
oilSellPrice
fuelSellPrice
buildIntervalSeconds
unitProductionIntervalSeconds
attackIntervalSeconds
maxActiveAttackGroups
defenseRadius
aggression
preferredBuildings
preferredUnits
preferredVehicles
```

Difficulty should affect:

```text
income multiplier
reaction speed
attack group size
how early it attacks
how many productions it queues
how aggressively it expands
```

Validation log:

```text
[AIConfig] faction=1 difficulty=Hard enabled=1 money=50000 aggression=0.8
```

## Step 2: Faction Economy

The AI needs its own budget and resource state before it can make meaningful production decisions.

Suggested ECS data:

```text
FactionEconomy
  money
  oil
  fuel
  oilIncomeRate
  fuelIncomeRate
  lastSellTime
```

Suggested system:

```text
AIEconomySystem
```

Responsibilities:

```text
produce oil/fuel from owned buildings
sell oil/fuel based on AI policy
add revenue to faction money
deduct money when requesting units/buildings
```

Important: do not create separate enemy-only economy logic. Player and enemy factions should use the same faction economy data.

Validation log:

```text
[AIEconomy] faction=1 money=62000 oil=80 fuel=45 soldOil=20 soldFuel=10 revenue=6000
```

Implemented in Step 2:

- Added `FactionEconomy` and `FactionEconomyPolicy` ECS data.
- Seeded per-faction economy entities from `AIControllerConfig` when gameplay begins.
- Added `AIEconomySystem` to sync faction-owned resource buildings, sell oil/fuel on policy cadence, add revenue, and emit `[AIEconomy]` validation logs.
- Added narrow `BuildingPlacementSystem` APIs for faction resource snapshots and faction resource selling so later AI build/production systems can reuse the same economy path.

## Step 3: AI Ownership And Control

Unit ownership and unit control are different concepts.

Ownership:

```text
Faction
```

Control mode:

```text
AIControlledTag
ManualControlledTag
```

For the player faction:

```text
Auto Mode ON:
  player units/buildings receive AIControlledTag

Auto Mode OFF:
  player units/buildings receive ManualControlledTag
```

For enemy factions:

```text
always AIControlledTag
```

Canvas UI:

```text
Button_AutoMode
```

States:

```text
Manual
Auto
```

First implementation should block manual movement/attack orders while Auto Mode is enabled. Later we can add manual override behavior if needed.

Validation log:

```text
[AIControlMode] faction=0 mode=Auto controlledUnits=52 controlledBuildings=8
```

Implemented in Step 3:

- Added `AIControlledTag`, `ManualControlledTag`, and `FactionControlEntry` ECS control data.
- Seeded faction control entries from `AIControllerConfig` when gameplay begins.
- Added `AIFactionControlSystem` to apply Auto/Manual tags by faction, remove manual movement/commanded attack orders from AI-controlled factions, and emit `[AIControlMode]` validation logs.
- Added Editor validation coverage for control tags, manual-order blocking, and validation logs.

## Step 4: AI Building Placement

Suggested system:

```text
AIBuildPlannerSystem
```

Responsibilities:

```text
choose a base center
choose building slots around base center
reserve cells
request building placement
pay cost
spawn building
avoid occupied/blocked cells
```

Start with a simple base layout:

```text
command tent
soldier tent
refinery
oil pump
vehicle factory
```

Use the existing building placement/spawn pipeline where possible. Avoid a separate duplicate AI-only spawn path.

Validation log:

```text
[AIBuild] faction=1 building=Soldier Tent cell=int2(140,90) cost=20000 result=Placed
```

Implemented in Step 4:

- Added `AIBuildPlan` and `AIBuildPlanEntry` ECS data seeded from each `AIControllerConfig` preferred building list.
- Added `AIBuildPlannerSystem` to run only for AI-controlled factions, choose a base center, place the next missing preferred building through `BuildingPlacementSystem`, and deduct cost from `FactionEconomy`.
- Added `BuildingPlacementSystem` lookup/count APIs for configured building IDs so AI planning reuses the existing runtime building placement path instead of a duplicate spawn path.
- Added Editor validation coverage for AI building placement, faction ownership, faction money spending, and `[AIBuild]` validation logs.

## Step 5: AI Unit Production

Suggested system:

```text
AIProductionSystem
```

Responsibilities:

```text
inspect owned production buildings
choose units based on config
check money/resources
queue production
spawn from the correct producer building
```

Examples:

```text
Soldier Tent produces infantry
Vehicle Factory produces APCs
Airport produces aircraft
```

The AI should use the same production logic as the player request button.

Validation log:

```text
[AIProduction] faction=1 producer=Soldier Tent unit=Rifleman cost=10000 queue=3 result=Queued
```

Implemented in Step 5:

- Added `AIProductionPlan` and `AIProductionPlanEntry` ECS data seeded from each `AIControllerConfig` preferred unit and vehicle list.
- Added `AIProductionSystem` to run only for AI-controlled factions, inspect faction-owned producer buildings, queue preferred unit production, and deduct cost from `FactionEconomy`.
- Added `BuildingPlacementSystem` APIs for configured unit lookup, faction producer lookup, pending production counting, and queued faction production so AI production uses the same pending-production path as player requests.
- Updated produced units spawned from owned buildings to inherit the producer building faction instead of always becoming faction 0.
- Added Editor validation coverage for AI unit queueing, producer ownership, faction money spending, queue counts, and `[AIProduction]` validation logs.

## Step 6: AI Squads

Do not make every unit think independently. Create squads and issue group orders.

Suggested data:

```text
AISquad
  factionId
  purpose: Attack / Defend / Scout / Harass
  rallyCell
  targetCell
  minUnits
  maxUnits
```

Responsibilities:

```text
group idle units into squads
assign one purpose per squad
assign formation goals around targets
issue group movement through the existing group movement/pathfinding pipeline
```

Validation log:

```text
[AISquad] faction=1 squad=3 purpose=Attack units=12 targetFaction=0 targetCell=int2(80,45)
```

Implemented in Step 6:

- Added `AISquadPlan`, `AISquad`, `AISquadMember`, and `AISquadUnit` ECS data.
- Seeded per-faction squad plans from `AIControllerConfig` difficulty, aggression, and max attack group settings.
- Added `AISquadSystem` to group idle AI-controlled faction units into squad entities, tag members, choose an initial target faction/cell, and emit `[AISquad]` validation logs.
- Kept target scoring and combat order issuing out of this step so Step 7 and Step 8 can own those decisions cleanly.
- Added Editor validation coverage for squad creation, member tagging, squad buffers, target faction/cell assignment, and `[AISquad]` validation logs.

## Step 7: AI Target Selection

Suggested system:

```text
AITargetingSystem
```

Targets should be scored by value, distance, threat, and difficulty behavior.

Target examples:

```text
enemy production buildings
enemy resource buildings
isolated units
nearby threats
player base center
```

Difficulty behavior:

```text
Easy:
  attacks nearby weak targets

Normal:
  attacks exposed units and economy

Hard:
  attacks production, expands faster, uses larger groups
```

Validation log:

```text
[AITarget] faction=1 target=Oil Pump score=92 reason=Economy value=High distance=64
```

Implemented in Step 7:

- Added target assignment fields to `AISquad` so squads can hold a target entity, target kind, target faction, target cell, and score.
- Added `AITargetingSystem` to score enemy ECS targets visible through `Faction`, `UnitGrid`, and `UnitHealth`.
- Target scoring currently prioritizes threats, then building/static targets, then regular units, with distance and health folded into the score.
- Kept movement and attack order issuing out of this step so Step 8 can consume squad targets through the normal command/pathfinding path.
- Added Editor validation coverage for target scoring, target assignment, and `[AITarget]` validation logs.

## Step 8: AI Combat Orders

Suggested system:

```text
AICombatOrderSystem
```

Responsibilities:

```text
move squads toward target
attack visible enemy units/buildings
defend base if attacked
retreat damaged squads if configured
```

This system should issue the same ECS order components as player commands. It should not move transforms directly.

Validation log:

```text
[AICombat] faction=1 squad=3 order=Attack target=Entity(123:4) units=12
```

Implemented in Step 8:

- Added `AICombatOrderSystem` to consume `AISquad` target assignments and issue commanded `EngageTarget` orders to squad members.
- AI combat orders remove stale movement, manual movement, group movement, and idle wander state from ordered units, matching the player attack-order cleanup path.
- Squad members use the existing engaged movement and attack systems; no duplicate AI-only combat path was added.
- Added Editor validation coverage for commanded engage orders, movement-state cleanup, squad order timestamps, and `[AICombat]` validation logs.

## Step 9: Player Auto Mode

Reuse the same AI planner and combat systems for the player faction.

Suggested behavior:

```text
player Auto AI prioritizes base defense
player Auto AI produces balanced units
player Auto AI attacks only when army strength is sufficient
player Manual Mode disables AI order issuing for faction 0
```

Initial implementation should block player manual orders while Auto Mode is enabled. If needed later, manual player orders can become temporary overrides.

Implemented in Step 9:

- Player faction Auto Mode now uses the same AI build, production, squad, target, and combat systems through the existing faction-control buffer.
- AI combat orders are marked with `AICombatOrderTag` so Manual Mode can distinguish and clear AI-issued orders without treating all commanded orders as player input.
- `AICombatOrderSystem` now also checks faction control state directly, so stale player squads cannot issue new orders after faction 0 switches back to Manual.
- Added Editor validation coverage for Manual Mode suppressing faction 0 combat orders and clearing AI-issued player orders when control returns to Manual.

## Step 10: Tests And Validation

Useful PlayMode tests:

```text
Enemy AI receives economy and money increases over time
Enemy AI places first camp building
Enemy AI queues a unit when it has enough money
Auto Mode changes player faction to AI controlled
Manual Mode prevents AI from issuing player orders
AI attack squad eventually receives a target
```

Stress validation:

```text
1 enemy AI with 50 units
2 AIs with 100 units each
player auto + enemy AI together
```

Implemented in Step 10:

- Added an Editor end-to-end validation test for the recommended vertical slice.
- The test proves one enemy AI can place its producer building, queue a unit, form an attack squad, select a threat target, and issue commanded combat orders through the existing ECS order path.
- The same test asserts the `[AIBuild]`, `[AIProduction]`, `[AISquad]`, `[AITarget]`, and `[AICombat]` validation logs so future changes keep the manual validation checkpoints covered.

Implemented AI settings panel:

- Added a scene-owned UI_Canvas AI Settings section under `Panel_Settings`; the controls are serialized `MenuView` references, not runtime name lookups.
- Added functional dropdowns for difficulty, starting money, income, build speed, production speed, attack group size, attack frequency, aggression, expansion, target priority, player Auto AI, and enemy AI count.
- Settings apply to new gameplay bootstrap values and also update existing enemy economy/build/production/squad/target-priority ECS plans without requiring a restart.
- Added Editor validation for real scene references and settings math, plus PlayMode validation for Canvas dropdown interaction and live ECS updates.

Implemented initial faction bases:

- Added a `Create Faction Bases` checkbox and base layout settings to `InitialUnitsSpawnerAuthoringConfig`.
- The scene initial spawn config now starts each configured faction from a walled base with a road-barrier gate, barracks, multiple tents, and at least six starting units.
- Initial base placement uses existing configured building prefabs and preserves ownership by faction so AI systems see the starting buildings immediately.
- Added `[InitialBase]` logs and Editor validation for the layout, scene config, base prefabs, tent count, and minimum unit count.
- Reworked the base layout to use the runtime wall-run placement path, `Building_Road_Barrier` gates, `Building_Ammunition_Depot` as the core building, all configured base buildings at least once, separated airport/runway placement, adjacent tent clusters, and larger mixed starting unit groups.

## Resource Dependency Notes

Vehicle fuel and unit ammo are not required before starting the AI controller, but the AI should be designed with resource checks from day one.

Add placeholder checks now:

```text
CanAffordUnit(faction, unitConfig)
HasRequiredResources(faction, unitConfig)
NeedsFuel(unit)
NeedsAmmo(unit)
```

For the first AI implementation:

```text
fuel requirement can return true / not required
ammo requirement can return true / not required
```

Later implementation:

```text
vehicle fuel drain while moving
ammo drain while attacking
refuel/rearm at base
AI resupply decisions
```

Recommended dependency order:

```text
AI foundation first
money/oil/fuel economy next
vehicle fuel and ammo requirements later
AI learns to respect fuel/ammo after they exist
```

## Recommended Vertical Slice

Start with the smallest complete AI loop:

```text
Enemy AI starts with money
Enemy AI places Soldier Tent
Enemy AI produces soldiers
Enemy AI forms one squad
Enemy AI attacks nearest player unit/building
```

After that works, expand to:

```text
oil/fuel economy
vehicles
difficulty tuning
player Auto Mode
ammo/fuel consumption
advanced targeting
```
