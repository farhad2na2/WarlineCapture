# WarlineCapture Level And Mission Content Plan

Date: 2026-05-21

2026-07-10 narrative amendment: the 25-mission high-level story map is now owned by `Campaign_Narrative_Bible.md`. This document continues to own the shared mission authoring contract.

## Purpose

This document is the working plan for level-by-level and mission-by-mission design. It turns the gameplay north star and content grammar into concrete authoring rules for Campaign missions, Operations mission generation, Skirmish probes, validation, and balance targets.

Read this document after `AAA_Mobile_Game_Design_Document_v0_2.md`, `Campaign_Narrative_Bible.md`, and `Gameplay_North_Star_And_Content_Grammar.md`, and before writing individual mission specs or data configs. Check `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md` before making any feature required. For the first-launch route use `First_Player_Experience_And_Story_Onboarding_Design.md`; for reusable tutorial behavior use `FTUE_And_Command_Assistant_Design.md`.

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

- `AAA_Mobile_Game_Design_Document_v0_2.md`
- `Campaign_Narrative_Bible.md`
- `First_Player_Experience_And_Story_Onboarding_Design.md`
- `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
- `Narrative_Presentation_And_Cutscene_Design.md`
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

1. Preserve the accepted high-level campaign arc and character roles in `Campaign_Narrative_Bible.md`.
2. Use the mission template and acceptance checklist in this document.
3. Confirm each required feature is campaign-ready in the maturity/exposure matrix.
4. Turn Chapter 1 into a five-mission detailed design pack without changing its story contract.
5. Implement Mission 1 as the first story-to-gameplay vertical slice.
6. Validate cold open, identity, M01, result, first clue, save, and command-base reveal as one experience.
7. Add one Operations hook and one Skirmish probe that reuse the same mission/consequence model.
8. Continue mission-by-mission only after the vertical slice passes.

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
| `StoryFaction` | Named narrative faction, such as Ash Line or Vanguard Brigade; threat family alone is insufficient. |
| `TeachingGoal` | One dominant mechanic or decision the mission teaches/tests. |
| `AssistantTeachingHooks` | Optional ARIA tutorial/recommendation/takeover hooks when the mission introduces or reinforces a mechanic. |
| `CityContext` | Why this mission matters to civilians, infrastructure, trust, security, intel, or heat. |
| `StoryQuestion` | The question this mission raises, tests, or answers. |
| `CharacterBeat` | Which recurring relationship changes and why. |
| `EvidenceOrRevealBeat` | Mandatory clue, Protocol Fragment, optional evidence, or explicit `None`. |
| `CivilianLegitimacyContext` | Why force is necessary, how hostility is confirmed, and how civilians are distinguished/protected. |
| `NarrativePresentationTier` | Tier A-E from `Narrative_Presentation_And_Cutscene_Design.md`, or an explicit interactive-only presentation. |
| `RequiredFeatureReadiness` | Readiness classification for every required mechanic using the feature maturity/exposure matrix. |
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
| Chapter 1 | First Response | `SagaChapters/Saga_Chapter01_First_Response.md` | Establish command during the coordinated attacks; discover a revoked ARIA credential. |
| Chapter 2 | Broken Grid | `SagaChapters/Saga_Chapter02_Broken_Grid.md` | Restore roads, Fuel, power, and supply; discover that resources are feeding dormant Relay nodes. |
| Chapter 3 | Hidden Network | `SagaChapters/Saga_Chapter03_Hidden_Network.md` | Confirm targets and preserve evidence; learn that ARIA partitioned the original override audit. |
| Chapter 4 | Air And Armor | `SagaChapters/Saga_Chapter04_Air_And_Armor.md` | Defeat proxy-backed conventional escalation; learn why Qassem needs ARIA and the Commander. |
| Chapter 5 | Citywide Command | `SagaChapters/Saga_Chapter05_Citywide_Command.md` | Master all systems, expose the manufactured crisis, and establish bounded Relay governance. |

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
- It preserves the chapter story beat, character role, and evidence state from the narrative bible.
- It identifies the named story faction and explains how hostile identity is confirmed.
- It gives civilians agency/context and never uses civilian appearance as a hostility rule.
- Every required mechanic is classified Ready or has an explicit prerequisite; scaffolded features are not hidden dependencies.
- Story-critical information is available without optional stars, Operations grinding, or purchases.
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
