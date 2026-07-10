# WarlineCapture Campaign Chapter 2: Broken Grid

Date: 2026-07-10

Status: Active detailed high-level chapter design. No step-by-step implementation content.

## Purpose And Authority

This document owns Campaign Chapter 2 high-level story, character, feature, mission, consequence, reward, and presentation direction. `../Campaign_Narrative_Bible.md` owns the canonical story facts and `../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md` owns feature-readiness truth.

Read after:

- `../AAA_Mobile_Game_Design_Document_v0_2.md`
- `../Campaign_Narrative_Bible.md`
- `../Gameplay_North_Star_And_Content_Grammar.md`
- `../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
- `../Level_And_Mission_Content_Plan.md`
- `../Narrative_Presentation_And_Cutscene_Design.md`
- `../Field_Logistics_Oil_Fuel_Design.md`
- `../Automated_Fuel_Logistics_Design.md`
- `../Resource_Logistics_Exchange_Design.md`

## Chapter Promise

The Ash Line changes strategy after losing its first command node. Instead of attacking only JRC positions, it breaks roads, redirects Fuel, corrupts trade manifests, disables power, and forces civilians from their homes. The Commander must prove that restoring the city is part of defeating the enemy.

```text
In Chapter 1, the player saved a district.
In Chapter 2, the player learns how the district stays alive.
```

## Story Contract

| Element | Chapter 2 authority |
|---|---|
| Opening state | JRC has a forward command, but hospitals, shelters, markets, and military units compete for damaged routes and limited Fuel. |
| Chapter question | Why is the Ash Line rerouting infrastructure instead of simply destroying it? |
| Story faction | Ash Line logistics and sabotage cells. |
| Primary locations | Hospital corridor, industrial road, Oil field/refinery belt, Old Market exchange yard, power district, logistics hub. |
| Commander arc | Accept responsibility for supplies, workers, displacement, and recovery, not only battlefield victory. |
| ARIA arc | Learn that telemetry and optimal routing do not contain every human need or informal civilian route. |
| Dalia arc | Treat route defense, engineers, and convoy timing as combat power. |
| Samira arc | Become a necessary command partner while retaining authority to challenge unsafe military priorities. |
| Chapter climax | Reopen the district logistics hub and trace the destination of stolen power and Fuel. |
| Protocol Fragment 2 | The attacks are feeding selected dormant Civic Relay nodes. |
| Exit hook | The enemy requires an intact hidden network inside the city to activate those nodes. |

## Feature Contract

| Feature | Chapter role | Readiness rule |
|---|---|---|
| Road repair/building | Make access to hospitals, shelters, convoys, and logistics physical. | M01 cannot require player-built roads until the active entry, connectivity, visuals, and objective path are validated. A route-clearing variant may preserve the story if needed. |
| Oil/Fuel network | Establish extraction, refinery conversion, storage, automated hauling, and route protection. | End-to-end flow and campaign HUD feedback must be validated before M02 is detailed. |
| Automated logistics | Reward network design and protection rather than truck micromanagement. | Hauler state and blocked-route recovery must be visible. |
| Resource Exchange | Express emergency import/export and corrupted manifests. | M03 is conditional until recipes, bootstrap, UI, timing, rewards, and non-premium completion work in Campaign. |
| Civilians/refugees | Show that road and power loss displaces people. | Use light authored consequences until identity, movement, HUD, and result feedback are clear. |
| Building/repair | Restore services while under pressure. | Do not claim worker or construction lifecycle behavior absent from runtime. |

## Principal Character Beats

| Character | Chapter movement |
|---|---|
| Commander | Must decide which lifeline receives scarce protection first, then communicate the consequence. |
| ARIA | Correctly models throughput but initially misses informal roads, shelter behavior, and the social cost of an "efficient" route. |
| Dalia | Learns to treat engineering crews and automated haulers as force multipliers rather than secondary assets. |
| Samira | Gains operational influence because her local knowledge repeatedly changes mission outcomes. |
| Yasin Barakat | Old Market representative who exposes the difference between legitimate trade and Ash Line smuggling. |
| Fadi Mansour | Transit foreman who anchors the road and repair stories in actual city labor. |
| Nadir Qassem | Frames shortages as proof that distributed government cannot protect anyone. |

## Mission Arc

| Mission | Story and objective | Dominant learning/mastery | Character beat | Evidence beat |
|---|---|---|---|---|
| M01 Gridlock | Clear or restore the blocked hospital corridor while defending Fadi's road crews. | Road/route introduction plus crew defense. | ARIA's official map fails to include a viable local service lane; Fadi provides it. | Sabotage charges were placed to preserve one route toward a dormant substation. |
| M02 Supply Line | Restore Oil extraction, refinery conversion, Fuel storage, and automated hauling while Ash Line teams attack the chain. | Oil-to-Fuel network and route protection. | Samira connects Fuel delivery to clinic generators and water pumps; Dalia protects the network as a military objective. | Stolen Fuel shipments share the same hidden destination as M01 power flow. |
| M03 Market Lifeline | Secure an emergency exchange yard, expose a corrupt manifest route, and keep essential supplies moving. | Conditional import/export introduction with escort pressure. | Yasin identifies the legitimate trade pattern the enemy has copied. | A captured manifest names multiple Relay-era storage sites. |
| M04 Power Relay | Reconnect a power substation while moving displaced civilians to shelters and defending engineers. | Repair, refugees, timed defense, logistics combination. | The Commander reconciles ARIA's shortest route with Samira's safer civilian route. | The substation contains a dormant handshake waiting for an ARIA key. |
| M05 Route Reopened | Break the Ash Line hold on the district logistics hub without destroying the systems the city needs. | Roads, Fuel, automated hauling, defense, and breach mastery. | Dalia and Samira coordinate military and civil routes as peers. | Protocol Fragment 2 proves the attacks are powering selected Civic Relay nodes. |

## Mission Consequence Direction

| Outcome family | Story consequence |
|---|---|
| Civilians and workers protected | Trust rises and later routes receive stronger local cooperation. |
| Lifeline restored | Infrastructure improves and mission debrief shows a specific service returning. |
| Route or Fuel chain lost | Tactical victory may remain, but shelters, aircraft, or later preparation start under pressure. |
| Excess collateral | Repairs consume more resources and Samira challenges the result directly. |
| Evidence preserved | The Commander enters Chapter 3 with clearer Intel context; main Protocol Fragment remains guaranteed. |

## Reward And Progression Direction

| Beat | Reward direction |
|---|---|
| Early chapter | Materials, field-repair progression, and road/logistics capability only when the corresponding gameplay is campaign-ready. |
| Mid chapter | Fuel, logistics-truck progression, and fixed infrastructure unlocks. |
| Late chapter | Medical or workshop capability and visible district recovery rewards. |
| Chapter completion | Infrastructure-focused account recognition plus Protocol Fragment 2 and Chapter 3 access. |

Reward ids and amounts remain subordinate to `../Economy_Reward_Design.md` and the combat catalog. Resource Exchange objectives and rewards must not depend on Rush Tickets or payment.

## Presentation Direction

- Chapter-opening Tier B sequence contrasts restored JRC command with dark hospitals, blocked roads, and displaced families.
- Samira and Fadi receive location-specific panels showing work, not passive distress.
- M02 uses a clear visual chain from Oil field to refinery to Fuel storage to destination.
- M03 avoids treating trade or markets as inherently corrupt; one compromised route is the threat.
- M05's finale shows dormant Relay nodes lighting across a city map and unlocks Protocol Fragment 2.

## Balance Direction

Most missions use Standard bands. M01 may begin near Tutorial pacing because it introduces route interaction. M05 combines logistics and combat but should not become a long maintenance simulation.

Key tuning questions:

- Can the player understand why a route failed?
- Does automation reduce repetitive truck control?
- Does Fuel pressure create a choice before it creates a dead end?
- Can the player protect workers and civilians without losing all offensive agency?
- Does every required Resource Exchange action have a free and functioning route?

## High-Level Validation Questions

- Story, HUD, and map agree on which road, service, and population are at risk.
- Required road and exchange interactions are campaign-ready or replaced/deferred openly.
- Oil-to-Fuel flow is observable and recovers from route disruption.
- Civilians and workers remain distinct from Ash Line operatives.
- Each debrief names a human service affected by the result.
- Protocol Fragment 2 is guaranteed by chapter completion.
- No step-by-step implementation assumptions are embedded in this high-level chapter design.
