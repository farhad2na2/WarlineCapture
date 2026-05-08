# WarlineCapture Level And Mission Content Plan

Date: 2026-05-05

## Purpose

This document is the working plan for level-by-level and mission-by-mission design. It turns the gameplay north star and content grammar into concrete authoring rules for Saga Campaign missions, Persistent Operation mission generation, Quick Custom probes, validation, and balance targets.

Read this document after `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md` and before writing individual mission specs or data configs. For first-time-user teaching beats, contextual recommendations, assistant help, or assistant takeover behavior, also read `WarlineCapture_FTUE_And_Command_Assistant_Design.md`.

For every mission or level that uses map art, also read `WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`. Strategic/zoomed-out map art is for selection, briefing, minimap, and context. Tactical/zoomed-in map packages are the playable combat ground and must include metadata.

## Terminology

WarlineCapture uses these terms consistently:

| Term | Meaning |
|---|---|
| Mission | Player-facing authored content unit with objective, stars, rewards, consequence, and result. |
| ScenarioSetup | Tactical configuration used by a mission: map id, starts, resources, enemy setup, allowed catalog, objective configs, reward configs, and encounters. |
| Level / Map | Reusable battlefield layout referenced by `ScenarioSetup`: terrain, roads, zones, routes, spawn anchors, civilian areas, objective anchors, preview art, and minimap art. |

Relationship:

```text
Mission -> ScenarioSetup -> Level / Map
```

Do not use `Level` as a synonym for `Mission` in config names or UI. A Saga node launches a Mission; the Mission references a ScenarioSetup; the ScenarioSetup references a Level / Map.

## Source Design Inputs

- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `WarlineCapture_Gameplay_Features_High_Level_Spec.md`
- `WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `WarlineCapture_Economy_Reward_Design.md`
- `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
- `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
- `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`
- `WarlineCapture_Balancing_Automated_Test_Plan.md`
- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `WarlineCapture_2D_Isometric_Production_Direction.md`
- `WarlineCapture_2D_Isometric_Art_Bible.md`
- `WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`
- `WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`

## Content Authoring Order

WarlineCapture should author content in this order:

1. Lock the mission template and acceptance checklist in this document.
2. Author Chapter 1 as a five-mission Saga teaching pack.
3. Implement Mission 1 as the first vertical slice.
4. Add one Operation action/mission hook that reuses the same consequence model.
5. Add one Quick Custom balance probe that mirrors the Mission 1 scenario.
6. Validate the full loop: briefing, loadout, tactical objective, result, reward, consequence, save, and balance report.
7. Continue Chapter 1 mission-by-mission only after the vertical slice passes.

Mission 1 implementation must use `WarlineCapture_M01_FirstContact_Production_Contract.md` as the concrete production contract.

## Strategic And Tactical Map Authoring Rules

Every tactical mission must separate planning art from playable ground:

- `MapPreviewArtId`: strategic/zoomed-out mission context for Saga Map, Mission Briefing, Quick Custom, and result context.
- `MinimapArtId`: simplified navigation layer for Battle HUD, threat jump, objective jump, and camera viewport display.
- `IsoMapId`: playable tactical map package used by the match scene.
- `TacticalMapDefinition`: metadata that makes the tactical map playable.

Mission specs must name the strategic preview and the tactical map package. Level specs must name the roads, sidewalks, blockers, build zones, routes, objective anchors, civilian zones, and camera bounds that need metadata. Ground art without metadata is not a playable level.

## Required Mission Spec Template

Every authored mission must use this structure.

| Field | Requirement |
|---|---|
| `MissionId` | Stable id such as `saga.ch01.m01.first_contact`. |
| `Title` | Player-facing mission title. |
| `Mode` | Saga Campaign, Persistent Operation, or Quick Custom probe. |
| `ChapterOrDay` | Saga chapter/mission index or Operation day/event source. |
| `MissionArchetype` | Must come from `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`. |
| `ThreatFamily` | Must come from the threat families in the north-star doc. |
| `TeachingGoal` | One dominant mechanic or decision the mission teaches/tests. |
| `AssistantTeachingHooks` | Optional ARIA tutorial/recommendation/takeover hooks when the mission introduces or reinforces a mechanic. |
| `CityContext` | Why this mission matters to civilians, infrastructure, trust, security, intel, or heat. |
| `ScenarioSetup` | Map id, district id, spawn rules, starting units/buildings/resources, enemy setup, allowed catalog ids from `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`. |
| `MapViewContract` | Required for tactical missions: `LevelId`, `IsoMapId`, `MapPreviewArtId`, `MinimapArtId`, tactical metadata asset, camera bounds, and named anchors for spawns/routes/objectives/build zones. |
| `Objectives` | Required objectives with exact thresholds and HUD visibility. |
| `StarGoals` | Three visible star goals with thresholds and result presentation. |
| `CivilianDistrictConsequences` | Trust/security/intel/infrastructure/heat/civilian deltas, including zero-delta tutorial cases. |
| `Rewards` | `RewardConfig` ids and canonical reward types. |
| `Unlocks` | Units, buildings, support abilities, upgrade parts, gear, cosmetics, or no unlock; gameplay target ids must resolve to the combat balance config. |
| `UISurfaces` | Screens, popups, and HUD panels used. |
| `BalanceTargetBand` | Tutorial, Standard, or Mastery plus concrete metric targets. |
| `ValidationPlan` | Config sanity, objective, reward, UI, save, and balance checks. |
| `FailureRetryRules` | What happens on loss, replay, retry, and partial star completion. |

## Saga Campaign Chapter Set

The main plan keeps chapter structure high-level. Each Saga chapter owns its detailed mission matrix, mission specs, reward pacing, validation plan, and balance targets in a dedicated chapter document.

| Internal Chapter | Player-Facing Name | Dedicated Doc | Campaign Role |
|---|---|---|---|
| Chapter 1 | First Response | `SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md` | Teach squad command, base setup, threat warnings, transport, and breach assault. |
| Chapter 2 | Broken Grid | `SagaChapters/WarlineCapture_Saga_Chapter02_Broken_Grid.md` | Expand infrastructure pressure, resource routes, convoy defense, and repair decisions. |
| Chapter 3 | Hidden Network | `SagaChapters/WarlineCapture_Saga_Chapter03_Hidden_Network.md` | Deepen Intel, raids, ambushes, evidence, and trust-risk decisions. |
| Chapter 4 | Air And Armor | `SagaChapters/WarlineCapture_Saga_Chapter04_Air_And_Armor.md` | Escalate with aircraft, armored columns, combined-arms defense, and stronger AI profiles. |
| Chapter 5 | Citywide Command | `SagaChapters/WarlineCapture_Saga_Chapter05_Citywide_Command.md` | Master mixed threats, multi-objective missions, district consequences, and the final hostile node. |

Chapter documents are the source of truth for their mission lists. This document owns only the shared template, campaign structure, Operation hooks, Quick Custom mapping, and acceptance rules.

## Operation Mission Hooks

Operation should reuse mission archetypes instead of creating a separate unrelated content model.

| Operation Action/Event | Mission Hook | Archetype | Consequence |
|---|---|---|---|
| Patrol discovers hostile movement | Tactical intercept mission. | Patrol Intercept | Security up or enemy influence down on success. |
| Drone Scan reveals suspected cell | Optional raid briefing. | District Raid | OperationIntel confidence affects risk. |
| Aid convoy threatened | Protect route mission. | Convoy Defense | Trust up on success, trust down if convoy fails. |
| Repair action interrupted | Defend repair crews. | Infrastructure Repair | Infrastructure up on success, materials spent through action. |
| Escalation warning | Defensive tactical mission. | Base Defense | Security/heat drift depends on result. |

## Quick Custom And Balance Probe Mapping

Quick Custom presets should mirror mission archetypes so testing and balance reports stay useful.

| Preset / Probe | Mirrors | Purpose |
|---|---|---|
| `QuickCustom_Tutorial_Intercept` | M01 First Contact | Test basic squad command and low-pressure AI. |
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
- Economy/resource exposure matches `WarlineCapture_Economy_Reward_Design.md`.
- Balance targets are concrete enough for an opt-in report.
- The implementation owner can name at least one config sanity test and one gameplay/balance validation path.

## Next Authoring Steps

1. Convert Mission 1 into data configs after `GameLaunchPayload`, `ScenarioSetup`, objectives, results, and rewards exist.
2. Add `Saga_Chapter1_Mission1` as the first Saga balance probe.
3. Use Mission 1 to validate the full loop before implementing Missions 2-5.
4. Add `Saga_Chapter1_Mission2` through `Saga_Chapter1_Mission5` probes as each mission becomes playable.
5. Keep this document updated as mission archetypes or threat families evolve.
