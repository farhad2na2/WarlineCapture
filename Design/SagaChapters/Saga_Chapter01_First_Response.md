# WarlineCapture Saga Chapter 1: First Response

Date: 2026-05-21

Status: Active chapter content, updated for the 3D single-map direction. File names and some internal ids may keep `Saga` / `QuickCustom` for runtime compatibility, but player-facing language should be Campaign / Skirmish.

2026-07-10 narrative amendment: aligned to the `Shattered Relay` story, story-first M01 opening, named Ash Line threat, actual project character roster, and Protocol Fragment progression.

## Purpose

This document owns the detailed level-by-level and mission-by-mission design for Campaign Chapter 1. Internally this is `Chapter 1`; player-facing title is `First Response`.

Read after:

- `../AAA_Mobile_Game_Design_Document_v0_2.md`
- `../Campaign_Narrative_Bible.md`
- `../First_Player_Experience_And_Story_Onboarding_Design.md`
- `../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
- `../Narrative_Presentation_And_Cutscene_Design.md`
- `../Campaign_Mission_High_Level_Design_Catalog.md`
- `../Campaign_Narrative_Sequence_And_Comic_Catalog.md`
- `../Gameplay_North_Star_And_Content_Grammar.md`
- `../Level_And_Mission_Content_Plan.md`
- `../FTUE_And_Command_Assistant_Design.md`
- `../Economy_Reward_Design.md`
- `../Balancing_Automated_Test_Plan.md`
- `../UIUX_Gameplay_Element_Alignment.md`
- `../3D_SingleMap_Gameplay_Direction.md`
- `../M01_FirstContact_Production_Contract.md`

## Terminology

WarlineCapture uses these terms for Chapter 1 content:

| Term | Meaning | Chapter 1 Example |
|---|---|---|
| Mission | Player-facing authored content unit with objective, stars, rewards, consequence, and result. | `First Contact` |
| ScenarioSetup | 3D operation configuration used by a mission: map id, starts, resources, enemy setup, allowed catalog, objectives, rewards, and encounters. | `scenario.ch01.m01.first_contact` |
| OperationMap | Reusable 3D battlefield layout referenced by `ScenarioSetup`. It defines terrain, roads, zones, routes, spawn anchors, civilian areas, objective anchors, deployment zones, planning cameras, and minimap projection. | `opmap.ch01.district_edge_01` |

Relationship:

```text
Mission -> ScenarioSetup -> OperationMap
```

Do not use `Level` as a synonym for `Mission` in config names or UI. A Campaign node launches a Mission; the Mission references a ScenarioSetup; the ScenarioSetup references an OperationMap.

Chapter 1 follows the 3D single-map direction in `../3D_SingleMap_Gameplay_Direction.md`: planning, briefing, minimap, deployment, threat, and battle views are camera/UI layers on the same 3D operation map.

## Chapter Role

First Response begins during coordinated Ash Line terrorist attacks on Sahrin. It teaches the tactical foundation while establishing the Commander, ARIA, Dalia, Samira, and the central mystery. It does not expose the full economy, Store, Operations complexity, or all reward types.

Chapter theme:

```text
Command has failed during a coordinated attack. The player answers ARIA's emergency authentication, protects Old Market, restores a forward post, survives a prepared convoy strike, risks an airlift to save people, and removes the first fortified Ash Line node. The recovered node proves that the enemy is using a revoked ARIA credential.
```

## Chapter Story Contract

| Element | Chapter 1 authority |
|---|---|
| Opening state | Ordinary life in Sahrin is interrupted by coordinated bombings, blackout, transit sabotage, and command failure. |
| Chapter question | How did the Ash Line know exactly how JRC and ARIA would respond? |
| Story faction | Ash Line, using Tutorial Cell, Hidden Cell, Armored Column, and Defensive Garrison encounter behavior. |
| Commander arc | Move from isolated survivor to accepted Field Commander with a functioning forward command. |
| ARIA arc | Serve as the only command continuity system while detecting impossible use of her revoked credentials. |
| Dalia arc | Become the Commander's principal field lead and test whether decisive command is executable on the ground. |
| Samira arc | Move from justified skepticism toward conditional cooperation after JRC protects civilians and services. |
| Chapter climax | Breach the Ash Line communications node without treating the populated district as expendable. |
| Protocol Fragment 1 | Enemy traffic used an obsolete ARIA credential revoked before the current activation. |
| Exit hook | Qassem addresses the Commander and implies the response was predicted. |

## Story Presentation Contract

- First launch uses the Tier A `Command Lost` cold open and direct-to-M01 route in `../First_Player_Experience_And_Story_Onboarding_Design.md`.
- The stable sequence IDs and complete Chapter 1 panel/communication beats are owned by `../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-1-first-response`.
- The campaign-wide high-level gameplay/story contracts for all five missions are owned by `../Campaign_Mission_High_Level_Design_Catalog.md#chapter-1-first-response`; the detailed objective, balance, UI, reward, and validation specifications in this file refine those contracts.
- M01's final prologue panel should transition into the same Old Market operation-map location and time of day.
- M02-M04 use concise Tier C brief/debrief beats and non-blocking Tier D communications.
- M05 uses a Tier B chapter-finale reveal and unlocks Protocol Fragment 1 in Story Archive.
- No story sequence may use civilian appearance as hostile evidence or show graphic attack victims.

## Character Casting

| Character | Chapter use | Project visual anchor |
|---|---|---|
| Commander | Player-selected portrait; optional current battlefield proxy only. | `Chr_Leader_Male_01` |
| ARIA | Emergency boot, tutorial, warnings, and first mystery clue. | Dedicated ARIA avatar |
| Major Dalia Rahim | Field lead introduced in M02; recurring through M05. | `Chr_Soldier_Female_02_Alt_02` |
| Engineer Samira Haddad | Old Market/civilian voice from M01 onward. | `Chr_Civilian_Female_01` |
| Captain Laila Nasser | Airlift lead in M04. | `Chr_Pilot_Female_01` |
| Nadir Qassem | Voice or obscured portrait after M05; do not fully explain him yet. | `Chr_Insurgent_Male_05` |

## Mission Story Arc

| Mission | Immediate story | Relationship beat | Evidence beat |
|---|---|---|---|
| M01 First Contact | Intercept a confirmed armed patrol moving toward civilians after the Old Market attack. | ARIA authenticates the Commander; Samira reports from beyond the corridor. | The patrol carries unusually precise command-route information. |
| M02 Establish The Base | Reopen an abandoned JRC post before a second cell reaches it. | Dalia becomes the Commander's field lead. | A stolen municipal access list suggests inside preparation. |
| M03 Radar Warning | Stop a stolen convoy using a warning-sector outage known in advance. | Dalia begins trusting ARIA's timing while questioning the leak. | The outage was scheduled before the attack. |
| M04 Airlift | Extract a medical/engineering team and civilians from a threatened landing zone. | Laila joins; Samira sees JRC accept risk to protect people. | The ambush route matches a sealed emergency plan. |
| M05 Breach Assault | Destroy the first fortified Ash Line communications node and preserve its archive. | Commander, Dalia, Samira, and ARIA act as one command team. | Protocol Fragment 1: revoked ARIA credential; first Qassem message. |

## Chapter-Level Progression

| Mission | Primary Unlock Or Progression |
|---|---|
| M01 First Contact | CommanderXP and Credits; unlock Mission 2. |
| M02 Establish The Base | Credits, `Building_Barrack` unlock, unlock Mission 3. |
| M03 Radar Warning | Credits, `Building_GuardTower` unlock, `ability.radar_ping` support unlock, unlock Mission 4. |
| M04 Airlift | Credits, `upgrade.air.transport_aircraft` BlueprintParts, `ability.evacuation_corridor` support unlock, unlock Mission 5. |
| M05 Breach Assault | Chapter completion reward, `Unit_Chr_Ghillie_Male_01` unlock, `upgrade.vehicle.apc_armor` BlueprintParts, chapter reward threshold. |

## Level Library

These are the Chapter 1 3D operation-map layouts. The same operation map can support a Campaign mission, a Skirmish preset/probe, or an Operations event if the ScenarioSetup changes the objective package.

| OperationMapId | Player-Facing Use | Layout Purpose | Reused By |
|---|---|---|---|
| `opmap.ch01.district_edge_01` | First unstable civilian block. | Small road, command point, civilian block, visible patrol route, simple cover. | M01 First Contact, Skirmish Tutorial Intercept, Operations Patrol Intercept. |
| `opmap.ch01.forward_post_01` | Forward operating point. | Buildable lot, repair site, short approach lanes, civilian edge zone. | M02 Establish The Base, Infrastructure Repair Operation hook. |
| `opmap.ch01.convoy_approach_01` | Convoy approach to district core. | Defensive line, radar/guard-tower anchor, road convoy lane, base breach boundary. | M03 Radar Warning, Skirmish Base Defense Convoy. |
| `opmap.ch01.landing_zone_01` | Threatened landing zone. | Landing pad/road junction, extraction zone, rooftop/side-street ambush lanes. | M04 Airlift, Skirmish Airlift Extraction. |
| `opmap.ch01.fortified_node_01` | Fortified hostile node. | Outer approach, wall/gate breach point, enemy core, flank route, civilian exclusion zone. | M05 Breach Assault, Skirmish Breach Assault. |

## Chapter 1 Operation Map Contract

Each Chapter 1 operation map needs a planning camera, minimap projection, 3D playable map, and metadata-backed validation scene.

| OperationMapId | Planning Camera | Minimap Projection | Operation Metadata Must Include |
|---|---|---|---|
| `opmap.ch01.district_edge_01` | `camera.ch01.first_contact.planning` | `minimap.ch01.first_contact` | player squad spawn, hostile patrol spawn/route, walkable road/sidewalk, civilian edge zone, objective target group, camera bounds. |
| `opmap.ch01.forward_post_01` | `camera.ch01.establish_base.planning` | `minimap.ch01.establish_base` | buildable lot/pads, forward barracks socket, resource/cost tutorial anchors, civilian edge zone, first defense route, camera bounds. |
| `opmap.ch01.convoy_approach_01` | `camera.ch01.radar_warning.planning` | `minimap.ch01.radar_warning` | convoy route, radar/guard-tower anchors, base breach boundary, road/sidewalk surfaces, threat jump anchor, camera bounds. |
| `opmap.ch01.landing_zone_01` | `camera.ch01.airlift.planning` | `minimap.ch01.airlift` | landing zone, extraction zone, transport route, ambush lanes, fuel/transport tutorial anchors, camera bounds. |
| `opmap.ch01.fortified_node_01` | `camera.ch01.breach_assault.planning` | `minimap.ch01.breach_assault` | breach target, enemy core, flank route, approach cells, civilian exclusion zone, counterattack route, camera bounds. |

Static background art must not include baked soldiers, vehicles, aircraft, player buildings, enemy targets, health bars, objective icons, selection rings, or command markers. Those are runtime entities/overlays.

## Mission Matrix

| Mission | OperationMap | Archetype | Threat Family | Teaching Goal | Required Objective | Star Goals | Rewards | Balance Band |
|---|---|---|---|---|---|---|---|---|
| M01 First Contact | `opmap.ch01.district_edge_01` | Patrol Intercept | Tutorial Cell | Select, move, attack, read objective. | Destroy hostile patrol. | Complete mission; no own unit loss; finish under 4:00. | CommanderXP, Credits. | Tutorial |
| M02 Establish The Base | `opmap.ch01.forward_post_01` | Infrastructure Repair / Base Defense Lite | Tutorial Cell | Build, spend, produce. | Build forward barracks and produce squad. | Build under 5:00; keep civilians safe; no base breach. | CommanderXP, Credits, `Building_Barrack`. | Tutorial |
| M03 Radar Warning | `opmap.ch01.convoy_approach_01` | Base Defense | Armored Column | Read warning, prepare defense, stop convoy. | Survive convoy attack and prevent core breach. | Build radar/guard tower; no civilian deaths; destroy convoy before base damage. | CommanderXP, Credits, `Building_GuardTower`, `ability.radar_ping`. | Standard |
| M04 Airlift | `opmap.ch01.landing_zone_01` | Airlift Extraction | Hidden Cell / Air Assault | Use transport and landing-zone safety. | Extract endangered squad/civilians or reinforce landing zone. | No aircraft loss; complete under 6:00; low civilian loss. | CommanderXP, Credits, `upgrade.air.transport_aircraft` BlueprintParts, `ability.evacuation_corridor`. | Standard |
| M05 Breach Assault | `opmap.ch01.fortified_node_01` | Breach Assault | Defensive Garrison | Combined arms, breach route, fortified target. | Destroy fortified enemy core. | Use breach route; vehicle survives; complete under 9:00. | CommanderXP, Credits, `Unit_Chr_Ghillie_Male_01`, `upgrade.vehicle.apc_armor` BlueprintParts. | Standard |

## M01 Detailed Spec: First Contact

Implementation handoff: use `../M01_FirstContact_Production_Contract.md` for concrete tactical metadata anchors, UI command feedback, FTUE targets, asset manifest, audio/VFX requirements, and validation gates.

### Identity

| Field | Value |
|---|---|
| MissionId | `saga.ch01.m01.first_contact` |
| Title | First Contact |
| Mode | Campaign |
| ChapterOrDay | Chapter 1, Mission 1 |
| MissionArchetype | Patrol Intercept |
| ThreatFamily | Tutorial Cell |
| StoryFaction | Ash Line |
| TeachingGoal | Selection, move, attack, objective tracker, result flow. |
| CityContext | A confirmed armed Ash Line patrol is moving toward civilians stranded by the Old Market attack. The Commander is the nearest surviving JRC authority. |
| StoryQuestion | How did the patrol know the emergency response route? |
| CharacterBeat | ARIA authenticates the Commander; Samira reports civilians beyond the corridor. |
| EvidenceOrRevealBeat | The patrol carries precise command-route data; the revoked credential is revealed in the first debrief. |
| CivilianLegitimacyContext | Hostility is confirmed by visible weapons, hostile movement, attack context, and objective state. Civilian models remain non-hostile. |

### Scenario Setup

| Field | Value |
|---|---|
| ScenarioSetupId | `scenario.ch01.m01.first_contact` |
| OperationMapId | `opmap.ch01.district_edge_01` |
| PlanningCameraId | `camera.ch01.first_contact.planning` |
| MinimapProjectionId | `minimap.ch01.first_contact` |
| Player Start | One rifle squad near command point. |
| Enemy Start | One small hostile patrol on a visible road route. |
| Starting Credits | Low tutorial amount; no required spending. |
| Starting Materials | Hidden or zero. |
| Starting Fuel | Hidden or zero. |
| Allowed Build Catalog | None; Build Drawer disabled with tutorial reason. |
| Allowed Commands | Select, Move, Attack, Stop, Hold. |
| Threat Warning | Optional low-severity patrol warning after tutorial delay. |

### Objectives

| ObjectiveId | Type | Requirement | HUD Rule |
|---|---|---|---|
| `obj.ch01.m01.destroy_patrol` | DestroyAllEnemies or DestroyTargetGroup | Destroy the hostile patrol. | Visible from mission start. |
| `obj.ch01.m01.keep_command_squad_alive` | ProtectUnit | At least one player squad member/group survives. | Visible as failure condition. |

### Star Goals

| Star | GoalId | Threshold |
|---|---|---|
| 1 | `star.ch01.m01.complete_mission` | Complete required objective. |
| 2 | `star.ch01.m01.no_own_losses` | Own unit losses equal 0. |
| 3 | `star.ch01.m01.finish_under_4_min` | Complete mission in under 4:00. |

### Civilian And District Consequence

| Event | Consequence |
|---|---|
| Victory | Corridor secured, civilians move to safety, and the first debrief reveals a revoked ARIA credential. No district penalty is applied in the tutorial. |
| Defeat | No permanent penalty in tutorial; result explains replay. |
| Fast clean win | Eligible for 3-star result and first-clear reward. |

### Rewards

| RewardId | Reward Type | Amount / Item | Rule |
|---|---|---|---|
| `reward.ch01.m01.commander_xp.first_clear` | CommanderXP | Small first-clear XP grant. | First clear only. |
| `reward.ch01.m01.credits.first_clear` | Credits | Small Credit grant. | First clear only. |
| `reward.ch01.m01.credits.repeat` | Credits | Reduced replay grant. | Repeatable replay grant. |

Do not grant match Materials, Fuel, Oil, Command, Rush Tickets, store items, Operation metrics, or unlocks in Mission 1 unless a reviewed economy pass changes this document.

### UI Surfaces

| Surface | Purpose |
|---|---|
| Tier A cold open / identity | Fresh profile only: establishes the attack, ARIA, Commander identity, guidance, and direct M01 entry. |
| SCN-05 Campaign Map | Replay/existing-profile route; fresh first launch does not stop here before M01. |
| SCN-06 Mission Briefing | Replay route or compact in-story brief; fresh first launch keeps the objective concise and direct. |
| SCN-08 Battle HUD | Shows objective tracker, squad tray, command buttons, minimap if available. |
| POP-05 Mission Result | Shows outcome, first clue, and a direct transition to the earned command-base menu with M02 highlighted. |
| POP-07 Pause | Supports retry/exit safely. |

### Balance Targets

| Metric | Target |
|---|---:|
| Mission duration | 2:30-4:00 |
| Time to first enemy contact | 30-60 sec |
| Expected own unit loss | 0 |
| Civilian loss target | 0 |
| Resource spend required | 0 |
| 1-star success rate | 90-95% |
| 2-star success rate | 65-85% |
| 3-star success rate | 30-50% |

### Validation Plan

| Validation | Required Check |
|---|---|
| Config sanity | Mission id, scenario id, objective ids, reward ids, and map ids are nonempty and unique. |
| Objective test | Destroying the patrol completes the mission; losing the player squad fails or blocks completion. |
| Star test | Completion, no-loss, and under-time stars evaluate independently. |
| Reward test | Briefing reward preview and Mission Result grant reference the same `RewardConfig`. |
| UI contract test | Mission Briefing, HUD objective row, and Mission Result fields map to `../UIUX_Gameplay_Element_Alignment.md`. |
| Balance probe | `Campaign_Chapter1_Mission1` writes report with duration, first contact, losses, and reward data. |

### Failure And Retry Rules

- Defeat does not consume Campaign progress.
- Retry rebuilds the same `GameLaunchPayload`.
- Best star count is stored only on victory.
- First-clear rewards grant once.
- Repeat rewards must use explicit repeat rules from `RewardConfig`.

## M02 Detailed Spec: Establish The Base

### Identity

| Field | Value |
|---|---|
| MissionId | `saga.ch01.m02.establish_base` |
| Title | Establish The Base |
| Mode | Campaign |
| ChapterOrDay | Chapter 1, Mission 2 |
| MissionArchetype | Infrastructure Repair / Base Defense Lite |
| ThreatFamily | Tutorial Cell |
| TeachingGoal | Building placement, Credits/Materials spend, production queue, basic base defense. |
| CityContext | Command needs a forward operating point to stabilize the district edge and support civilian response. |

### Scenario Setup

| Field | Value |
|---|---|
| ScenarioSetupId | `scenario.ch01.m02.establish_base` |
| OperationMapId | `opmap.ch01.forward_post_01` |
| PlanningCameraId | `camera.ch01.establish_base.planning` |
| MinimapProjectionId | `minimap.ch01.establish_base` |
| Player Start | One rifle squad, command point, one buildable forward lot. |
| Enemy Start | Small delayed patrol wave entering from one approach lane. |
| Starting Credits | Enough to place required barracks and queue one squad. |
| Starting Materials | Enough for the required structure; no optional upgrades. |
| Starting Fuel | Hidden or zero. |
| Allowed Build Catalog | `Building_Barrack`, `Tent_Regular`, `Building_Road_Barrier`. |
| Allowed Commands | Select, Move, Attack, Stop, Hold, Build, Produce. |
| Threat Warning | Low-severity warning before the delayed patrol reaches the forward lot. |

### Objectives

| ObjectiveId | Type | Requirement | HUD Rule |
|---|---|---|---|
| `obj.ch01.m02.build_forward_barracks` | BuildStructure | Build the required barracks/tent at a valid footprint. | Visible from mission start. |
| `obj.ch01.m02.produce_rifle_squad` | ProduceUnit | Produce one additional rifle squad from the new structure. | Revealed after structure is complete. |
| `obj.ch01.m02.defend_forward_post` | DefendBase | Prevent the forward post from being destroyed before production completes. | Visible once first warning appears. |

### Star Goals

| Star | GoalId | Threshold |
|---|---|---|
| 1 | `star.ch01.m02.complete_mission` | Complete required objectives. |
| 2 | `star.ch01.m02.keep_civilians_safe` | Civilian losses equal 0. |
| 3 | `star.ch01.m02.build_under_5_min` | Build and produce required squad in under 5:00. |

### Civilian And District Consequence

| Event | Consequence |
|---|---|
| Forward post established | District infrastructure tutorial marker improves; no permanent Operation metric grant until Operation unlock. |
| Forward post damaged | Result explains repair cost pressure; no permanent Chapter 1 tutorial penalty. |
| Civilian-safe clear | Eligible for 2-star city-protection result. |

### Rewards

| RewardId | Reward Type | Amount / Item | Rule |
|---|---|---|---|
| `reward.ch01.m02.commander_xp.first_clear` | CommanderXP | Small XP grant. | First clear only. |
| `reward.ch01.m02.credits.first_clear` | Credits | Small Credit grant. | First clear only. |
| `reward.ch01.m02.production_unlock` | BuildingUnlock | `Building_Barrack`. | First clear only; duplicate converts to `BlueprintParts` for `upgrade.building.training_facilities`. |

Do not grant match Materials/Fuel/Oil, Command, Rush Tickets, store items, or direct Operation metric rewards.

### UI Surfaces

| Surface | Purpose |
|---|---|
| SCN-05 Campaign Map | Shows Mission 2 locked until M01 victory. |
| SCN-06 Mission Briefing | Shows build/produce objectives, city hook, rewards. |
| SCN-08 Battle HUD | Shows objective tracker, resources, squad tray, build toggle. |
| SCN-09 Build Drawer | Shows required structure, cost, build availability, production queue. |
| POP-03 Build Placement | Confirms footprint, rotation state, and resource cost. |
| POP-05 Mission Result | Shows build/produce completion, stars, rewards. |
| POP-07 Pause | Supports retry/exit safely. |

### Balance Targets

| Metric | Target |
|---|---:|
| Mission duration | 3:30-5:30 |
| Time to first enemy contact | 90-150 sec |
| Expected own unit loss | 0-1 |
| Civilian loss target | 0 |
| Resource spend required | One required structure plus one unit queue. |
| Resource float at end | Low positive after required spend. |
| 1-star success rate | 85-95% |
| 2-star success rate | 60-80% |
| 3-star success rate | 25-45% |

### Validation Plan

| Validation | Required Check |
|---|---|
| Config sanity | Required building, producer, unit, objective, and reward ids exist. |
| Objective test | Building placement and unit production complete objectives in order. |
| Resource test | Required costs use Credits/Materials and do not require unavailable resources. |
| Star test | Civilian-safe and under-time goals evaluate independently. |
| Reward test | Unlock reward has duplicate fallback if it can be owned already. |
| UI contract test | Build Drawer and Build Placement popup expose costs, locks, and invalid placement feedback. |
| Balance probe | `Campaign_Chapter1_Mission2` writes build timing, production timing, resource float, and first threat data. |

### Failure And Retry Rules

- Defeat does not consume first-clear rewards.
- Retry rebuilds the same ScenarioSetup.
- Resource spend is not persisted on failed tutorial attempt.
- Best star count is stored only on victory.

## M03 Detailed Spec: Radar Warning

### Identity

| Field | Value |
|---|---|
| MissionId | `saga.ch01.m03.radar_warning` |
| Title | Radar Warning |
| Mode | Campaign |
| ChapterOrDay | Chapter 1, Mission 3 |
| MissionArchetype | Base Defense |
| ThreatFamily | Armored Column |
| TeachingGoal | Threat warnings, defensive preparation, convoy timing, base breach prevention. |
| CityContext | A hostile convoy is moving toward the forward post. Command must read the warning, prepare defense, and stop the attack before it damages the district line. |

### Scenario Setup

| Field | Value |
|---|---|
| ScenarioSetupId | `scenario.ch01.m03.radar_warning` |
| OperationMapId | `opmap.ch01.convoy_approach_01` |
| PlanningCameraId | `camera.ch01.radar_warning.planning` |
| MinimapProjectionId | `minimap.ch01.radar_warning` |
| Player Start | Forward post, one or two rifle squads, existing build/defense anchor. |
| Enemy Start | Armored convoy wave on road approach, light escort units. |
| Starting Credits | Enough for one defensive preparation choice. |
| Starting Materials | Enough to place `Building_GuardTower` and one `Building_Road_Barrier`. |
| Starting Fuel | Hidden or zero. |
| Allowed Build Catalog | `Building_GuardTower`, `Building_Satelite_Dish`, `Building_Road_Barrier`, `Tent_Regular`, `Building_Barrack`. |
| Allowed Commands | Select, Move, Attack, Stop, Hold, Build, Produce. |
| Threat Warning | Required medium-severity convoy warning with ETA and route. |

### Objectives

| ObjectiveId | Type | Requirement | HUD Rule |
|---|---|---|---|
| `obj.ch01.m03.survive_convoy` | SurviveDuration or DefendBase | Survive the convoy attack window. | Visible from mission start. |
| `obj.ch01.m03.prevent_core_breach` | PreventBaseBreach | Prevent convoy from breaching the forward post core boundary. | Visible from mission start. |
| `obj.ch01.m03.destroy_convoy` | DestroyTargetGroup | Destroy the convoy group. | Visible when warning is issued. |

### Star Goals

| Star | GoalId | Threshold |
|---|---|---|
| 1 | `star.ch01.m03.complete_mission` | Complete required defense objective. |
| 2 | `star.ch01.m03.no_civilian_deaths` | Civilian losses equal 0. |
| 3 | `star.ch01.m03.no_base_damage` | Destroy convoy before base/core damage occurs. |

### Civilian And District Consequence

| Event | Consequence |
|---|---|
| Convoy stopped before base damage | Security tutorial marker improves; result explains district line held. |
| Base damaged but mission won | Result explains infrastructure/security risk; future Operation version would reduce infrastructure/security. |
| Civilian casualties | Star loss and trust-risk explanation; no permanent tutorial penalty unless Operation integration is enabled. |

### Rewards

| RewardId | Reward Type | Amount / Item | Rule |
|---|---|---|---|
| `reward.ch01.m03.commander_xp.first_clear` | CommanderXP | Standard Chapter 1 XP grant. | First clear only. |
| `reward.ch01.m03.credits.first_clear` | Credits | Standard Chapter 1 Credit grant. | First clear only. |
| `reward.ch01.m03.guard_tower_unlock` | BuildingUnlock | `Building_GuardTower`. | First clear only; duplicate converts to `BlueprintParts` for `upgrade.building.base_defense`. |
| `reward.ch01.m03.radar_ping_unlock` | SupportAbilityUnlock | `ability.radar_ping`. | First clear only; duplicate converts to `BlueprintParts` for `ability.radar_ping`. |
| `reward.ch01.m03.credits.repeat` | Credits | Reduced replay grant. | Repeatable replay grant. |

Do not grant match Materials/Fuel/Oil, Command, Rush Tickets, store items, or direct Operation metric rewards.

### UI Surfaces

| Surface | Purpose |
|---|---|
| SCN-05 Campaign Map | Shows Mission 3 locked until M02 victory. |
| SCN-06 Mission Briefing | Shows convoy threat, warning route, defensive rewards. |
| SCN-08 Battle HUD | Shows threat feed, objective tracker, resources, squad tray, minimap. |
| POP-01 Threat Alert | Shows convoy ETA, route, strength, jump-to-threat action. |
| SCN-09 Build Drawer | Shows defensive build/production options. |
| POP-05 Mission Result | Shows base damage, civilian safety, stars, rewards. |

### Balance Targets

| Metric | Target |
|---|---:|
| Mission duration | 5:00-7:00 |
| Threat warning lead time | 45-75 sec |
| Time to first enemy contact | 60-120 sec |
| Expected own unit loss | 0-3 |
| Civilian loss target | 0 |
| Base/core damage target | 0 for 3-star; minor allowed for 1-star. |
| Resource float at end | 10-25% of starting/earned resources after defense spend. |
| 1-star success rate | 75-90% |
| 2-star success rate | 45-70% |
| 3-star success rate | 20-40% |

### Validation Plan

| Validation | Required Check |
|---|---|
| Config sanity | Convoy wave, route, warning event, defense unlock, and objective ids exist. |
| Threat test | POP-01 receives route, ETA, strength, and jump target. |
| Objective test | Destroying convoy and preventing breach completes mission. |
| Star test | No-civilian and no-base-damage stars evaluate independently. |
| Reward test | Defense unlock maps to canonical reward type. |
| UI contract test | Threat feed, warning popup, and objective tracker expose severity and route data. |
| Balance probe | `Campaign_Chapter1_Mission3` writes warning lead time, base damage, convoy timing, resource float, and losses. |

### Failure And Retry Rules

- Defeat occurs if the forward post core is destroyed or required defense objective fails.
- Retry rebuilds the same convoy route and warning timings.
- Victory with base damage can still earn 1 star but not the no-base-damage star.
- Best star count is stored only on victory.

## M04 Detailed Spec: Airlift

### Identity

| Field | Value |
|---|---|
| MissionId | `saga.ch01.m04.airlift` |
| Title | Airlift |
| Mode | Campaign |
| ChapterOrDay | Chapter 1, Mission 4 |
| MissionArchetype | Airlift Extraction |
| ThreatFamily | Hidden Cell / Air Assault |
| TeachingGoal | Transport flow, landing-zone safety, extraction/reinforcement timing. |
| CityContext | A unit group and civilians are cut off near a landing zone. Command must secure the route and use transport before hostile pressure closes in. |

### Scenario Setup

| Field | Value |
|---|---|
| ScenarioSetupId | `scenario.ch01.m04.airlift` |
| OperationMapId | `opmap.ch01.landing_zone_01` |
| PlanningCameraId | `camera.ch01.airlift.planning` |
| MinimapProjectionId | `minimap.ch01.airlift` |
| Player Start | One rifle squad near landing zone, one transport helicopter or APC support path. |
| Enemy Start | Hidden Cell ambush group plus light air/anti-transport warning. |
| Starting Credits | Minimal; no required building. |
| Starting Materials | Hidden or zero. |
| Starting Fuel | Enough to cover required tutorial transport/deploy action. |
| Allowed Build Catalog | No building placement in this mission; Build Drawer remains hidden and POP-03 is unavailable. |
| Allowed Commands | Select, Move, Attack, Stop, Hold, Board/Extract, Rope Drop if helicopter is used. |
| Threat Warning | Landing-zone warning before ambush or air pressure arrives. |

### Objectives

| ObjectiveId | Type | Requirement | HUD Rule |
|---|---|---|---|
| `obj.ch01.m04.secure_landing_zone` | ReachLocation or DefendBase | Hold the landing zone until transport is available. | Visible from mission start. |
| `obj.ch01.m04.extract_cutoff_group` | ExtractUnit | Extract or reinforce the endangered group through transport flow. | Visible after landing zone is secure. |
| `obj.ch01.m04.transport_survives` | ProtectUnit | Transport must survive required extraction flow. | Visible as failure condition. |

### Star Goals

| Star | GoalId | Threshold |
|---|---|---|
| 1 | `star.ch01.m04.complete_mission` | Complete extraction/reinforcement objective. |
| 2 | `star.ch01.m04.no_aircraft_loss` | Transport survives. |
| 3 | `star.ch01.m04.finish_under_6_min` | Complete mission in under 6:00. |

### Civilian And District Consequence

| Event | Consequence |
|---|---|
| Clean extraction | Trust tutorial marker improves; result explains civilians saw command respond. |
| Transport damaged but survives | Star impact only; result calls out repair/readiness pressure. |
| Transport destroyed | Mission failure if extraction can no longer complete. |
| Civilian casualties | Star loss and trust-risk explanation. |

### Rewards

| RewardId | Reward Type | Amount / Item | Rule |
|---|---|---|---|
| `reward.ch01.m04.commander_xp.first_clear` | CommanderXP | Standard Chapter 1 XP grant. | First clear only. |
| `reward.ch01.m04.credits.first_clear` | Credits | Standard Chapter 1 Credit grant. | First clear only. |
| `reward.ch01.m04.transport_parts` | BlueprintParts | `upgrade.air.transport_aircraft` x25. | First clear only; item-specific parts. |
| `reward.ch01.m04.support_unlock` | SupportAbilityUnlock | `ability.evacuation_corridor`. | First clear only; duplicate fallback grants item-specific BlueprintParts. |

Do not grant match Materials/Fuel/Oil, Command, Rush Tickets, store items, or direct Operation metric rewards.

### UI Surfaces

| Surface | Purpose |
|---|---|
| SCN-05 Campaign Map | Shows Mission 4 locked until M03 victory. |
| SCN-06 Mission Briefing | Shows landing zone, transport objective, starting match Fuel rule, and persistent Credits/unlock rewards. |
| SCN-07 Loadout | Shows required/locked transport support if loadout is active by this phase. |
| SCN-08 Battle HUD | Shows landing-zone objective, squad tray, transport status, minimap. |
| SCN-10 Command Wheel | Shows Board/Extract/Rope Drop commands when relevant. |
| POP-01 Threat Alert | Shows landing-zone or air-pressure warning. |
| POP-05 Mission Result | Shows extraction, transport survival, civilian safety, rewards. |

### Balance Targets

| Metric | Target |
|---|---:|
| Mission duration | 5:00-7:00 |
| Time to landing-zone threat | 45-90 sec |
| Transport exposure window | 60-120 sec |
| Expected own unit loss | 0-3 |
| Civilian loss target | 0-1 |
| Fuel spend required | Required tutorial transport cost only. |
| 1-star success rate | 75-90% |
| 2-star success rate | 45-70% |
| 3-star success rate | 20-40% |

### Validation Plan

| Validation | Required Check |
|---|---|
| Config sanity | Transport unit/support, extraction zone, objective ids, and reward ids exist. |
| Objective test | Transport boarding/extraction completes objective and destroyed transport blocks completion. |
| Star test | Transport-survival and under-time stars evaluate independently. |
| Resource test | Required starting match Fuel is seeded by scenario configuration and never injected through account/store flow. |
| UI contract test | Transport command, landing-zone objective, and threat warning map to gameplay contracts. |
| Balance probe | `Campaign_Chapter1_Mission4` writes transport timing, exposure window, Fuel spend, losses, and extraction result. |

### Failure And Retry Rules

- Defeat occurs if extraction becomes impossible or required protected group is lost.
- Retry restores required Fuel/tutorial transport setup.
- Victory with transport damage can still earn 1 star but not the transport-survival star.
- Best star count is stored only on victory.

## M05 Detailed Spec: Breach Assault

### Identity

| Field | Value |
|---|---|
| MissionId | `saga.ch01.m05.breach_assault` |
| Title | Breach Assault |
| Mode | Campaign |
| ChapterOrDay | Chapter 1, Mission 5 |
| MissionArchetype | Breach Assault |
| ThreatFamily | Defensive Garrison |
| TeachingGoal | Combined arms, breach target, fortified enemy core, chapter result. |
| CityContext | The hostile node behind the district wall network is coordinating attacks. Command must breach the position and remove the chapter threat. |

### Scenario Setup

| Field | Value |
|---|---|
| ScenarioSetupId | `scenario.ch01.m05.breach_assault` |
| OperationMapId | `opmap.ch01.fortified_node_01` |
| PlanningCameraId | `camera.ch01.breach_assault.planning` |
| MinimapProjectionId | `minimap.ch01.breach_assault` |
| Player Start | Rifle squads, `Unit_Veh_APC_Heavy` mission support, forward staging point. |
| Enemy Start | Defensive garrison, wall/gate breach target, core building, light counterattack wave. |
| Starting Credits | Enough to reinforce or produce limited support if production is active. |
| Starting Materials | Enough for limited defensive/build support, not enough to brute-force spam. |
| Starting Fuel | Optional transport/vehicle support budget if M04 systems are active. |
| Allowed Build Catalog | Basic production/defense from prior missions; no new untaught systems. |
| Allowed Commands | Select, Move, Attack, Stop, Hold, Build/Produce if active, Breach/Attack target, transport commands if active. |
| Threat Warning | Counterattack warning after breach begins. |

### Objectives

| ObjectiveId | Type | Requirement | HUD Rule |
|---|---|---|---|
| `obj.ch01.m05.breach_wall` | DestroyTargetBuilding | Destroy or breach the marked wall/gate. | Visible from mission start. |
| `obj.ch01.m05.destroy_enemy_core` | DestroyTargetBuilding | Destroy fortified enemy core building. | Visible from mission start. |
| `obj.ch01.m05.hold_capture_zone` | ReachLocation or SurviveDuration | Hold the core area briefly after destruction if capture flow exists. | Revealed after core destruction. |

### Star Goals

| Star | GoalId | Threshold |
|---|---|---|
| 1 | `star.ch01.m05.complete_mission` | Destroy enemy core and complete required capture/hold rule. |
| 2 | `star.ch01.m05.vehicle_survives` | `Unit_Veh_APC_Heavy` survives. |
| 3 | `star.ch01.m05.finish_under_9_min` | Complete mission in under 9:00. |

### Civilian And District Consequence

| Event | Consequence |
|---|---|
| Enemy core destroyed | Chapter district stabilizes; enemy influence tutorial marker drops. |
| High collateral route | Result explains infrastructure risk and blocks relevant star if configured. |
| Low-loss clear | Chapter completion feels clean and unlocks next chapter path. |

### Rewards

| RewardId | Reward Type | Amount / Item | Rule |
|---|---|---|---|
| `reward.ch01.m05.commander_xp.first_clear` | CommanderXP | Larger Chapter 1 XP grant. | First clear only. |
| `reward.ch01.m05.credits.first_clear` | Credits | Chapter finale Credit grant. | First clear only. |
| `reward.ch01.m05.unit_unlock.ghillie` | UnitUnlock | `Unit_Chr_Ghillie_Male_01`. | First clear only; duplicate fallback grants item-specific BlueprintParts. |
| `reward.ch01.m05.apc_armor_parts` | BlueprintParts | `upgrade.vehicle.apc_armor` x35. | First clear only; item-specific parts. |
| `reward.ch01.m05.campaign_stars` | CampaignStars / legacy `SagaStars` storage | Best star result for mission/chapter thresholds. | Stored as best result, never spent. |

Do not grant Command, Rush Tickets, store items, or direct Operation metric rewards from the store path. Campaign stars come only from mission completion/star result.

### UI Surfaces

| Surface | Purpose |
|---|---|
| SCN-05 Campaign Map | Shows Mission 5 locked until M04 victory and chapter-completion state after victory. |
| SCN-06 Mission Briefing | Shows fortified target, breach route, star goals, chapter reward preview. |
| SCN-07 Loadout | Shows recommended squads/support and deploy cost if loadout is active. |
| SCN-08 Battle HUD | Shows breach/core objectives, resources, squad tray, minimap. |
| SCN-10 Command Wheel | Shows breach/attack/extract context where relevant. |
| POP-01 Threat Alert | Shows counterattack warning after breach begins. |
| POP-04 Reward Unlock | Shows major unlock if first-clear reward grants a unit/gear/support item. |
| POP-05 Mission Result | Shows chapter completion, stars, stats, rewards, next route. |

### Balance Targets

| Metric | Target |
|---|---:|
| Mission duration | 7:00-9:30 |
| Time to first enemy contact | 30-75 sec |
| Time to breach | 3:00-5:30 |
| Expected own unit loss | 1-5 |
| Vehicle/support survival target | Survives for 2-star. |
| Civilian loss target | 0 |
| Resource float at end | 5-20% with pressure. |
| 1-star success rate | 70-85% |
| 2-star success rate | 35-60% |
| 3-star success rate | 15-35% |

### Validation Plan

| Validation | Required Check |
|---|---|
| Config sanity | Breach target, enemy core, capture area, reward ids, and chapter unlock route exist. |
| Objective test | Breach and core destruction complete in order; core cannot complete before breach unless alternate route is authored. |
| Star test | Vehicle/support survival and under-time stars evaluate independently. |
| Reward test | Major unlock has duplicate fallback and Campaign stars store best result only. |
| UI contract test | Briefing, HUD, reward unlock popup, and Mission Result expose chapter completion state. |
| Balance probe | `Campaign_Chapter1_Mission5` writes breach timing, core destruction timing, losses, resource float, and star distribution. |

### Failure And Retry Rules

- Defeat occurs if the player loses all controllable squads or fails any required hold/capture rule.
- Retry rebuilds the same ScenarioSetup.
- Chapter completion rewards grant once.
- Best star count can improve on replay without consuming stars.

## Chapter 1 Probe Set

| Probe Id | Mission | Metrics Focus |
|---|---|---|
| `Campaign_Chapter1_Mission1` | M01 First Contact | Tutorial length, first contact timing, no-loss star, first-clear rewards. |
| `Campaign_Chapter1_Mission2` | M02 Establish The Base | Build timing, production timing, resource float, first threat. |
| `Campaign_Chapter1_Mission3` | M03 Radar Warning | Warning lead time, convoy timing, base damage, losses. |
| `Campaign_Chapter1_Mission4` | M04 Airlift | Transport timing, extraction result, Fuel spend, transport survival. |
| `Campaign_Chapter1_Mission5` | M05 Breach Assault | Breach timing, core destruction, vehicle/support survival, star distribution. |

## Chapter Acceptance

First Response is chapter-ready when:

- All five missions use the Mission -> ScenarioSetup -> OperationMap terminology.
- All five operation maps satisfy the Chapter 1 Operation Map Contract: planning camera, minimap projection, `OperationMapId`, operation metadata, and validation scene.
- All mission specs define objectives, star goals, rewards, UI surfaces, balance targets, validation, and retry rules.
- Chapter rewards and mission rewards use canonical reward types from `../Economy_Reward_Design.md`.
- Mission probes are listed in `../Balancing_Automated_Test_Plan.md`.
- Mission 1 validates the end-to-end loop before Mission 2 implementation begins.
