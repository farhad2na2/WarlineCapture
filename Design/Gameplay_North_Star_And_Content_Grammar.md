# WarlineCapture Gameplay North Star And Content Grammar

Date: 2026-05-21

2026-07-10 narrative amendment: aligned to the active v0.2 GDD, `Shattered Relay` campaign bible, story-first first-player experience, and feature-readiness matrix.

## Purpose

This document locks the high-level gameplay direction that must be read before authoring level-by-level, mission-by-mission, reward, validation, or balancing content.

The existing gameplay specs define systems. This document defines the content grammar those systems should serve: what WarlineCapture is about, what a good mission asks from the player, how Campaign, Operations, and Skirmish connect, and which balancing targets every authored mission must expose.

Use `AAA_Mobile_Game_Design_Document_v0_2.md` for product authority, `Campaign_Narrative_Bible.md` for setting/story/character authority, and `First_Player_Experience_And_Story_Onboarding_Design.md` for the first-launch route. Use `3D_SingleMap_Gameplay_Direction.md` for the active map direction and prefab-catalog roster source. Use `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md` before making a feature mission-critical. Use `Level_And_Mission_Content_Plan.md` for the shared authoring template, `Campaign_Mission_High_Level_Design_Catalog.md` for all 25 mission contracts, and the dedicated docs under `SagaChapters` for chapter authoring.

Terminology rule: a player-facing Campaign node launches a `Mission`; the mission uses a `ScenarioSetup`; the ScenarioSetup references a reusable 3D `OperationMap`. Do not use Level as a synonym for Mission in config names, UI labels, or validation docs. Legacy docs may still say Saga, Quick Custom, Level, tactical map, or strategic map; new player-facing language should prefer Campaign, Skirmish, and 3D operation map.

## Source Design Inputs

- `GAME_DESIGN_REFERENCE.md`
- `AAA_Mobile_Game_Design_Document_v0_2.md`
- `Campaign_Narrative_Bible.md`
- `First_Player_Experience_And_Story_Onboarding_Design.md`
- `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
- `Narrative_Presentation_And_Cutscene_Design.md`
- `Campaign_Mission_High_Level_Design_Catalog.md`
- `3D_SingleMap_Gameplay_Direction.md`
- `AAA_Mobile_Game_Design_Document_v0_1.md` (historical)
- `Command_Offensive_Premise_Alignment.md`
- `Gameplay_Features_High_Level_Spec.md`
- `Gameplay_Features_Detailed_Spec.md`
- `Level_And_Mission_Content_Plan.md`
- `SagaChapters/Saga_Chapter01_First_Response.md`
- `SagaChapters/Saga_Chapter02_Broken_Grid.md`
- `SagaChapters/Saga_Chapter03_Hidden_Network.md`
- `SagaChapters/Saga_Chapter04_Air_And_Armor.md`
- `SagaChapters/Saga_Chapter05_Citywide_Command.md`
- `Economy_Reward_Design.md`
- `Balancing_Automated_Test_Plan.md`
- `UIUX_Gameplay_Element_Alignment.md`
- `Monetization/Monetization_Strategy.md`

## North Star

WarlineCapture is a mobile 3D command RTS about leading a local joint response force through a fictional Middle Eastern city under terrorist attack and later proxy-backed conventional escalation. The first Campaign, provisionally titled `Shattered Relay`, follows the Field Commander and ARIA as they defend civilians, restore the city's lifelines, expose the Ash Line, and prevent the Civic Relay from becoming an instrument of unilateral rule.

The player fantasy is not only "destroy the enemy." The core fantasy is:

```text
Read the city.
Identify the hostile faction's position.
Prepare the right force.
Deploy into the same 3D operation map.
Strike with tactical control.
Protect civilians and infrastructure.
Live with the district consequences.
```

WarlineCapture should be differentiated by proactive command pressure, hostile cells hidden in civilian space, civilian safety, readable 3D mobile command, and persistent district consequences.

## Design Pillars

| Pillar | Meaning | Design Test |
|---|---|---|
| Tactical Command | The player prepares and executes readable squad, build, move, attack, transport, breach, scan, and support decisions on one 3D operation map. | Does the mission reward active command instead of passive waiting? |
| Story Through Systems | Each major mechanic solves a specific city crisis and advances a character, chapter question, or central revelation. | If this feature were removed, would the mission's story meaning change? |
| Hostile Factions In Civilian Space | Every mission targets a faction, cell, route, node, or threat that is using the city as cover. | Can the mission briefing explain who or what the commander is preparing to hit? |
| Precision Under Constraint | Civilian survival, collateral damage, trust, and stability are part of success. | Can the player win tactically but still lose value through reckless choices? |
| District Consequence | 3D operation results feed Campaign rewards or Operation district state. | Does the result screen explain what changed after the mission? |
| Readable 3D Mobile RTS | Objectives, waves, units, civilians, threats, camera states, and UI remain legible on mobile landscape. | Can the player understand threat direction, objective state, civilian risk, and next action without pausing? |
| Fair Progression | Rewards and store grants support preparation and identity, not victory overrides. | Does the reward/store path respect the economy and monetization guardrails? |

Map readability rule: every mission resolves to one 3D operation map. Planning view, briefing view, minimap, threat jumps, deployment setup, and battle view are camera/UI states over that same world. A mission is not readable if the 3D map cannot support unit scale, civilian identification, movement, attack, build placement, objectives, threat jumps, and feedback overlays.

## Primary Core Loop

Every authored 3D operation mission should fit this loop:

```text
Story Beat -> Briefing -> Intel/Scout -> Loadout -> 3D Operation -> Result/Stars -> City Consequence -> Character/Clue Beat -> Next Decision
```

Mode-specific versions:

| Mode | Loop Variant |
|---|---|
| Campaign | Select node -> read briefing -> prepare loadout -> complete authored 3D operation -> earn stars/rewards -> unlock next node/chapter threshold. |
| Operations | Inspect district -> choose action -> resolve 3D operation or abstract consequence -> update trust/security/intel/infrastructure/heat -> end day -> react to new events. |
| Skirmish | Choose preset/rules -> launch 3D operation sandbox -> review result/report -> adjust setup or replay. Runtime internals may keep QuickCustom naming until migration. |

## Mode Hierarchy

Campaign teaches the game. Operations prove the persistent command layer. Skirmish supports replay, experimentation, and balance testing.

| Mode | Content Role | What It Should Not Do |
|---|---|---|
| Campaign | Controlled teaching, curated mission variety, star mastery, unlock pacing. | Do not expose every system at once or require Operation knowledge to progress. |
| Operations | Long-term pressure, district consequence, action economy, evolving threats. | Do not become a detached menu economy with no 3D operation consequence. |
| Skirmish | Fast replayable configuration, AI tuning, mode/system testing. | Do not become the main progression farm or bypass campaign/operation structure. |

## Content Grammar

Every authored mission must define:

- `MissionId`
- `MissionArchetype`
- `SourceMode`
- `DistrictContext` or `ChapterContext`
- `PrimaryThreatFamily`
- `ScenarioSetup`
- `OperationMapId`
- `ObjectiveSet`
- `StarGoalSet`
- `RewardConfigSet`
- `ConsequenceSet`
- `TargetBalanceBand`
- `ValidationChecklist`
- `StoryQuestion`
- `CharacterBeat`
- `EvidenceOrRevealBeat`
- `CivilianLegitimacyContext`
- `RequiredFeatureReadiness`

No mission should be accepted as design-ready without all fields above.

## Mission Archetypes

| Archetype | Player Verb | Primary Objectives | Common Star Goals | Threat Families | Reward Pattern | Balance Focus |
|---|---|---|---|---|---|---|
| Patrol Intercept | Detect and stop a mobile threat. | Destroy patrol, reach location, prevent escape. | Finish under time, low losses, no civilian panic. | Hidden Cell, Swarm Militia. | CommanderXP, Credits, Intel. | Time to first contact, path clarity, loss rate. |
| Civilian Evacuation | Protect civilians while moving units. | Extract civilians/unit, survive duration, defend route. | Civilian survival, no vehicle loss, fast extraction. | Hidden Cell, Air Assault. | CommanderXP, Fuel, OperationTrust. | Civilian risk, route readability, transport timing. |
| Convoy Defense | Escort or protect a moving asset. | Defend convoy, destroy attackers, reach endpoint. | Convoy health, no breach, finish under time. | Armored Column, Air Assault. | Credits, CommanderXP, authored unlocks/items. | Attack cadence, convoy speed, warning lead time. |
| District Raid | Strike a suspected enemy node. | Destroy target building, capture intel, extract. | High intel confidence, low collateral, low losses. | Hidden Cell, Defensive Garrison. | Intel, BlueprintParts, OperationSecurity. | Intel gate, collateral risk, target durability. |
| Base Defense | Build and hold a defensive line. | Survive duration, prevent base breach, protect civilians. | Build radar, no breach, low casualties. | Armored Column, Air Assault, Swarm Militia. | Materials, BuildingUnlock, RushTicket. | Build timing, first attack timing, resource float. |
| Breach Assault | Break a fortified position. | Breach wall/gate, destroy core, hold captured area. | Use breach route, vehicle survives, fast clear. | Defensive Garrison, Armored Column. | UnitUnlock, GearModule, Credits. | Fortification health, combined-arms pacing, losses. |
| Airlift Extraction | Use air transport under pressure. | Extract/reinforce units, hold landing zone, survive. | No aircraft loss, fast extraction, low civilian loss. | Air Assault, Hidden Cell. | Fuel, SupportAbilityUnlock, transport parts. | Landing-zone readability, warning timing, transport survivability. |
| Infrastructure Repair | Stabilize damaged city systems. | Build/repair structure, defend workers, restore route. | Infrastructure above threshold, no collateral, efficient spend. | Hidden Cell, Armored Column. | Materials, OperationInfrastructure, OperationSupply. | Repair cost, attack pressure, district delta clarity. |

## Threat Families

| Threat Family | Identity | Pressure Pattern | Best Used For | Avoid |
|---|---|---|---|---|
| Tutorial Cell | Small hostile force with limited tech. | Simple patrols, small attacks, predictable movement. | Chapter 1 teaching missions. | Surprise difficulty spikes. |
| Hidden Cell | Ambush, sabotage, uncertain intel, light units. | Delayed reveals, flanking patrols, evidence objectives. | Patrol Intercept, District Raid, Intel missions. | Invisible failure states or unfair spawns. |
| Swarm Militia | Many weak units and frequent pressure. | High cadence, low individual durability. | Early defense and stress tests. | Mobile readability overload. |
| Armored Column | Vehicles, breach pressure, durable threats. | Slow push, high base damage, clear warning. | Convoy Defense, Base Defense, Breach Assault. | Starting too close to player base. |
| Defensive Garrison | Fortified structures, walls, turrets, held zones. | Static defense plus counterattack waves. | Breach Assault and raid targets. | Stalling without clear breach options. |
| Air Assault | Aircraft, landing/rope pressure, anti-air response. | Warning-driven attacks and support disruption. | Radar Warning, Airlift Extraction, base defense. | Unreadable offscreen damage. |
| Mixed Force | Combined threat for mastery missions. | Uses two or more pressure types. | Chapter finales and Operation escalations. | First exposure to a mechanic. |

Ash Line is the Chapter 1-3 story faction and may use Tutorial Cell, Hidden Cell, Swarm Militia, or Defensive Garrison encounter behavior. Vanguard Brigade is the Chapter 4-5 proxy military faction and may use Armored Column, Air Assault, Defensive Garrison, or Mixed Force behavior. Threat Family describes gameplay pressure; it is not a substitute for a named story faction.

Civilian identity rule: civilians remain civilians in config, visuals, story, and gameplay. Armed insurgents may exploit civilian structures and routes, but a civilian model must not transform into a surprise hostile. Hostility is established through confirmed weapons, conduct, Intel, restricted-zone context, and objective state, never through clothing, language, gender, or neighborhood.

## Objective And Star Rules

Objectives define success. Stars define mastery. Rewards and unlocks must not require hidden objectives.

Rules:

- Required objectives must be visible in Mission Briefing and Battle HUD.
- Star goals must be visible before launch and after result.
- Civilian, collateral, breach, and time goals must state exact thresholds.
- A mission can allow tactical victory with strategic damage, but the result screen must explain the consequence.
- Paid resources, premium resources, or store items must never complete objectives or stars directly.

Recommended star layout:

| Star | Meaning | Example |
|---|---|---|
| Star 1 | Complete required objective. | Destroy patrol or survive defense. |
| Star 2 | Protect city value. | No civilian deaths, no base breach, infrastructure above threshold. |
| Star 3 | Mastery pressure. | Finish under time, low losses, efficient spend, no vehicle loss. |

## Civilian And District Consequence Rules

Civilian safety and district recovery should be the emotional and strategic pressure layer.

| Tactical Event | Possible Consequence |
|---|---|
| Civilians saved | OperationTrust up, OperationSecurity stable, bonus reward eligibility. |
| Civilian casualties | OperationTrust down, Heat up, future mission risk increases. |
| Collateral damage | OperationInfrastructure down, repair cost up, trust penalty. |
| Base breach prevented | Security up, enemy influence down. |
| Intel captured | OperationIntel confidence delta or a named Intel Dossier inventory item. |
| Raid with low confidence | Higher collateral/trust risk and possible failed target result. |
| Repair completed | OperationInfrastructure up and district income/readiness improves. |

Operation metric deltas must follow `Economy_Reward_Design.md`: store items and rewards do not directly grant district metrics unless they are earned mission/operation outcomes or authored Operation actions.

## Reward Exposure Pacing

Avoid exposing every resource and reward type in Chapter 1.

| Progression Band | Exposed Resources/Rewards |
|---|---|
| Chapter 1 onboarding | CommanderXP, Credits, first UnitUnlock or BuildingUnlock. Match Materials/Fuel/Oil are scenario grants, not account rewards. |
| Chapter 1 late | Intel, SupportAbilityUnlock, BlueprintParts. |
| Operation intro | OperationTrust, OperationSecurity, OperationIntel, OperationInfrastructure, OperationSupply. |
| Profile/store layer | Command, Rush Tickets, Cosmetics, fixed-content bundles. |
| Season/events | Extra fixed claim nodes, event cosmetics, capped bundles. |

## Chapter 1 Teaching Arc

The five-chapter Campaign escalation is:

| Chapter | Command growth | Story answer |
|---|---|---|
| 1. First Response | Establish command and defeat the first coordinated cells. | Enemy traffic uses a revoked ARIA credential. |
| 2. Broken Grid | Take responsibility for roads, Fuel, power, supply, and displacement. | The attacks are feeding dormant Civic Relay nodes. |
| 3. Hidden Network | Confirm threats, preserve evidence, and resist manipulated Intel. | ARIA partitioned the audit of Qassem's original override. |
| 4. Air And Armor | Control open military escalation across air, armor, and long-range fire. | Qassem needs ARIA and the Commander's authority to seize the Relay. |
| 5. Citywide Command | Coordinate every mastered system without abandoning civilian legitimacy. | The manufactured crisis is exposed and the Relay receives bounded governance. |

The full 25-mission story map and character arcs are owned by `Campaign_Narrative_Bible.md`.

Chapter 1 should teach one dominant mechanic per mission and keep the city-consequence layer visible but light.
The player-facing FTUE, contextual recommendation, and safe assistant takeover design for this arc lives in `FTUE_And_Command_Assistant_Design.md`.

| Mission | Teaching Goal | Recommended Archetype | New Mechanic | Strategic Hook |
|---|---|---|---|---|
| Mission 1: First Contact | Selection, move, attack, objective completion. | Patrol Intercept | Squad command in a 3D town corridor. | First hostile contact in a civilian district. |
| Mission 2: Establish The Base | Building placement, production, resource spend. | Infrastructure Repair or Base Defense Lite | Build/produce in the 3D operation map model. | Restore a forward operating point. |
| Mission 3: Radar Warning | Threat alert, defense timing, warning response. | Base Defense | Radar/threat feed. | Stop a convoy before district damage. |
| Mission 4: Airlift | Transport, extraction/reinforcement, landing-zone safety. | Airlift Extraction | Helicopter/APC transport. | Evacuate or reinforce a threatened block. |
| Mission 5: Breach Assault | Combined arms and fortified objective. | Breach Assault | Breach/walls/core target. | Remove the chapter enemy node. |

## Operation Week 1 Arc

Operation should feel like a district pressure rhythm, not a static management screen.

```text
Day Start -> District Warnings -> 2-3 Player Actions -> Tactical/Abstract Resolution -> End Day Report -> Drift/Escalation -> New Choice
```

Recommended Week 1:

| Day | Focus | Player Decision | Tactical Hook | Expected Learning |
|---|---|---|---|---|
| Day 1 | Stabilize one district. | Patrol or Aid. | Light patrol intercept. | Actions affect trust/security. |
| Day 2 | Reveal hidden activity. | Drone Scan or Raid. | Intel reveal or risky raid. | Intel confidence changes risk. |
| Day 3 | Repair damaged infrastructure. | Repair or Build Outpost. | Defend repair crews. | Materials convert through actions. |
| Day 4 | Respond to escalation. | Choose which warning to answer. | Convoy or air threat. | Ignored threats create drift. |
| Day 5 | Remove a hostile node. | Confirm raid with known evidence. | District raid. | Better intel improves outcome. |

## Balance Target Bands

Every mission config should include target bands before implementation.

| Metric | Tutorial Band | Standard Band | Mastery Band |
|---|---:|---:|---:|
| Mission duration | 3-5 min | 6-10 min | 8-14 min |
| Time to first threat | 45-90 sec | 30-75 sec | 20-60 sec |
| Expected own unit loss | 0-2 | 1-5 | 2-8 |
| Civilian loss target | 0 | 0-2 | 0-3 with explicit risk |
| Resource float at end | Low positive | 10-25% of earned/spent economy | 5-20% with pressure |
| 1-star success rate | 85-95% | 70-85% | 55-75% |
| 2-star success rate | 60-80% | 40-65% | 25-50% |
| 3-star success rate | 25-45% | 15-35% | 8-25% |

These bands are starting points for reports, not build-failing assertions. Balance probes classify outcomes as `Good`, `Watch`, `Problem`, or `InvalidRun` according to `Balancing_Automated_Test_Plan.md`.

## Mission Acceptance Checklist

Before a mission is design-ready:

- Mission archetype is selected from this document or a new archetype is added here first.
- Required objective rows and star rows are visible in briefing, HUD, and result.
- Reward preview and reward grant use the same `RewardConfig`.
- Civilian, collateral, district, or infrastructure consequence is defined, even if the value is zero for a tutorial mission.
- Enemy threat family and AI profile are selected.
- Encounter timing and warning lead time are specified.
- Resource/reward grants use canonical names from `Economy_Reward_Design.md`.
- Monetization cannot override objective, star, or district consequences.
- UI elements have contracts in `UIUX_Gameplay_Element_Alignment.md`.
- Balance target bands are filled in.
- Validation tests or opt-in balance probes are identified.

## Vertical Slice Recommendation

Before authoring the full chapter and operation campaign, build one end-to-end vertical slice:

1. Skirmish launch payload. Runtime internals may keep QuickCustom naming until migration.
2. Campaign Mission 1: First Contact.
3. Objective runtime and Mission Result.
4. Reward preview and grant.
5. One Operation district action that consumes or updates the same consequence model.
6. One balance report for the mission or matching Skirmish scenario.

After this slice passes design, implementation, UI, and balance validation, author Chapter 1 mission-by-mission.
