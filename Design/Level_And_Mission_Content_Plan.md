# WarlineCapture Level And Mission Content Plan

Date: 2026-05-21

## Purpose

This document is the working plan for level-by-level and mission-by-mission design. It turns the gameplay north star and content grammar into concrete authoring rules for Campaign missions, Operations mission generation, Skirmish probes, validation, and balance targets.

Read this document after `Gameplay_North_Star_And_Content_Grammar.md` and before writing individual mission specs or data configs. For first-time-user teaching beats, contextual recommendations, assistant help, or assistant takeover behavior, also read `FTUE_And_Command_Assistant_Design.md`.

For every mission or level that uses map/world art, also read `3D_SingleMap_Gameplay_Direction.md`. Planning, briefing, minimap, deployment, threat alerts, and battle view are UI/camera layers on the same 3D operation map. Do not author new missions around separate strategic and tactical maps.

## Terminology

WarlineCapture uses these terms consistently:

| Term | Meaning |
|---|---|
| Mission | Player-facing authored content unit with objective, stars, rewards, consequence, and result. |
| ScenarioSetup | 3D operation configuration used by a mission: map id, starts, resources, enemy setup, allowed catalog, objective configs, reward configs, and encounters. |
| OperationMap | Reusable 3D battlefield layout referenced by `ScenarioSetup`: terrain, roads, zones, routes, spawn anchors, civilian areas, objective anchors, deployment zones, preview cameras, and minimap projection. |

Relationship:

```text
Mission -> ScenarioSetup -> OperationMap
```

Do not use `Level` as a synonym for `Mission` in config names or UI. A Campaign node launches a Mission; the Mission references a ScenarioSetup; the ScenarioSetup references an OperationMap.

## Source Design Inputs

- `Gameplay_North_Star_And_Content_Grammar.md`
- `FTUE_And_Command_Assistant_Design.md`
- `Gameplay_Features_High_Level_Spec.md`
- `Gameplay_Features_Detailed_Spec.md`
- `Economy_Reward_Design.md`
- `Combat_Catalog_And_Upgrade_Design.md`
- `BalanceConfigs/Combat_Balance_Config_v0_1.json`
- `VisualConfigs/Combat_Visual_Config_v0_1.json`
- `Balancing_Automated_Test_Plan.md`
- `UIUX_Gameplay_Element_Alignment.md`
- `3D_SingleMap_Gameplay_Direction.md`

## Content Authoring Order

WarlineCapture should author content in this order:

1. Lock the mission template and acceptance checklist in this document.
2. Author Chapter 1 as a five-mission Campaign teaching pack.
3. Implement Mission 1 as the first vertical slice.
4. Add one Operation action/mission hook that reuses the same consequence model.
5. Add one Skirmish balance probe that mirrors the Mission 1 scenario.
6. Validate the full loop: briefing, loadout, 3D operation objective, result, reward, consequence, save, and balance report.
7. Continue Chapter 1 mission-by-mission only after the vertical slice passes.

Mission 1 implementation must use `M01_FirstContact_Production_Contract.md` as the concrete production contract.

## 3D Operation Map Authoring Rules

Every mission that enters gameplay must resolve to one 3D operation map:

- `OperationMapId`: playable 3D town/base/district map used by the match scene.
- `PlanningCameraId`: zoomed-out planning/briefing camera state on the same map.
- `MinimapProjectionId`: simplified projection layer for Battle HUD, threat jump, objective jump, and camera viewport display.
- `OperationMapDefinition`: metadata that makes the 3D operation map playable.

Mission specs must name the operation map, planning camera, minimap projection, roads, sidewalks, blockers, build zones, routes, objective anchors, deployment zones, civilian zones, hostile-cell zones, and camera bounds that need metadata. A 3D map without metadata is not a playable mission.

## Required Mission Spec Template

Every authored mission must use this structure.

| Field | Requirement |
|---|---|
| `MissionId` | Stable id such as `saga.ch01.m01.first_contact`. |
| `Title` | Player-facing mission title. |
| `Mode` | Campaign, Operations, or Skirmish probe. |
| `ChapterOrDay` | Campaign chapter/mission index or Operations day/event source. |
| `MissionArchetype` | Must come from `Gameplay_North_Star_And_Content_Grammar.md`. |
| `ThreatFamily` | Must come from the threat families in the north-star doc. |
| `TeachingGoal` | One dominant mechanic or decision the mission teaches/tests. |
| `AssistantTeachingHooks` | Optional ARIA tutorial/recommendation/takeover hooks when the mission introduces or reinforces a mechanic. |
| `CityContext` | Why this mission matters to civilians, infrastructure, trust, security, intel, or heat. |
| `ScenarioSetup` | Operation map id, district id, spawn rules, starting units/buildings/resources, enemy setup, allowed catalog ids from `BalanceConfigs/Combat_Balance_Config_v0_1.json` and prefab display data from `Assets/Game/Configs/Prefabs`. |
| `MapViewContract` | Required for playable missions: `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, operation metadata asset, camera bounds, and named anchors for spawns/routes/objectives/deployment/build zones. |
| `Objectives` | Required objectives with exact thresholds and HUD visibility. |
| `StarGoals` | Three visible star goals with thresholds and result presentation. |
| `CivilianDistrictConsequences` | Trust/security/intel/infrastructure/heat/civilian deltas, including zero-delta tutorial cases. |
| `Rewards` | `RewardConfig` ids and canonical reward types. |
| `Unlocks` | Units, buildings, support abilities, upgrade parts, gear, cosmetics, or no unlock; gameplay target ids must resolve to the combat balance config. |
| `UISurfaces` | Screens, popups, and HUD panels used. |
| `BalanceTargetBand` | Tutorial, Standard, or Mastery plus concrete metric targets. |
| `ValidationPlan` | Config sanity, objective, reward, UI, save, and balance checks. |
| `FailureRetryRules` | What happens on loss, replay, retry, and partial star completion. |

## Campaign Chapter Set

The main plan keeps chapter structure high-level. Each Campaign chapter owns its detailed mission matrix, mission specs, reward pacing, validation plan, and balance targets in a dedicated chapter document.

| Internal Chapter | Player-Facing Name | Dedicated Doc | Campaign Role |
|---|---|---|---|
| Chapter 1 | First Response | `SagaChapters/Saga_Chapter01_First_Response.md` | Teach squad command, base setup, threat warnings, transport, and breach assault. |
| Chapter 2 | Broken Grid | `SagaChapters/Saga_Chapter02_Broken_Grid.md` | Expand infrastructure pressure, resource routes, convoy defense, and repair decisions. |
| Chapter 3 | Hidden Network | `SagaChapters/Saga_Chapter03_Hidden_Network.md` | Deepen Intel, raids, ambushes, evidence, and trust-risk decisions. |
| Chapter 4 | Air And Armor | `SagaChapters/Saga_Chapter04_Air_And_Armor.md` | Escalate with aircraft, armored columns, combined-arms defense, and stronger AI profiles. |
| Chapter 5 | Citywide Command | `SagaChapters/Saga_Chapter05_Citywide_Command.md` | Master mixed threats, multi-objective missions, district consequences, and the final hostile node. |

Chapter documents are the source of truth for their mission lists. This document owns only the shared template, campaign structure, Operations hooks, Skirmish mapping, and acceptance rules.

## Operation Mission Hooks

Operation should reuse mission archetypes instead of creating a separate unrelated content model.

| Operation Action/Event | Mission Hook | Archetype | Consequence |
|---|---|---|---|
| Patrol discovers hostile movement | Tactical intercept mission. | Patrol Intercept | Security up or enemy influence down on success. |
| Drone Scan reveals suspected cell | Optional raid briefing. | District Raid | OperationIntel confidence affects risk. |
| Aid convoy threatened | Protect route mission. | Convoy Defense | Trust up on success, trust down if convoy fails. |
| Repair action interrupted | Defend repair crews. | Infrastructure Repair | Infrastructure up on success, materials spent through action. |
| Escalation warning | Defensive tactical mission. | Base Defense | Security/heat drift depends on result. |

## Skirmish And Balance Probe Mapping

Skirmish presets should mirror mission archetypes so testing and balance reports stay useful. Internal/runtime ids may keep QuickCustom names until implementation renames them.

| Preset / Probe | Mirrors | Purpose |
|---|---|---|
| `QuickCustom_Tutorial_Intercept` | M01 First Contact | Test basic squad command and low-pressure AI. Player-facing label: Skirmish Tutorial Intercept. |
| `QuickCustom_BaseDefense_Convoy` | M03 Radar Warning | Tune convoy attack timing and warning lead time. |
| `QuickCustom_Airlift_Extraction` | M04 Airlift | Validate transport timing and landing-zone pressure. |
| `QuickCustom_Breach_Assault` | M05 Breach Assault | Tune fortification durability and combined-arms pressure. |

## Mission Acceptance Gate

A mission is not design-ready until:

- It uses the required mission spec template.
- It maps to a north-star mission archetype and threat family.
- Required objectives, star goals, rewards, and consequences are authored.
- Reward preview and reward grant use the same `RewardConfig`.
- UI surfaces and element contracts are identified.
- Economy/resource exposure matches `Economy_Reward_Design.md`.
- Balance targets are concrete enough for an opt-in report.
- The implementation owner can name at least one config sanity test and one gameplay/balance validation path.

## Next Authoring Steps

1. Convert Mission 1 into data configs after `GameLaunchPayload`, `ScenarioSetup`, objectives, results, and rewards exist.
2. Add `Campaign_Chapter1_Mission1` as the first Campaign balance probe.
3. Use Mission 1 to validate the full loop before implementing Missions 2-5.
4. Add `Campaign_Chapter1_Mission2` through `Campaign_Chapter1_Mission5` probes as each mission becomes playable.
5. Keep this document updated as mission archetypes or threat families evolve.
