# WarlineCapture: AAA Mobile Game Design Document

Version: 0.2

Date: 2026-07-10

Status: Active high-level product and game-design authority

Supersedes: `AAA_Mobile_Game_Design_Document_v0_1.md` and its DOCX companion for active product direction

Upstream authority: none inside `Design`; this is the active product-design root.

Direct consumers: `Campaign_Narrative_Bible.md`, `Gameplay_North_Star_And_Content_Grammar.md`, `First_Player_Experience_And_Story_Onboarding_Design.md`, `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`, `Narrative_Presentation_And_Cutscene_Design.md`, `Campaign_Mission_High_Level_Design_Catalog.md`, `Campaign_Narrative_Sequence_And_Comic_Catalog.md`, `3D_SingleMap_Gameplay_Direction.md`, and mode/system design documents.

## Product Vision

WarlineCapture is a mobile-first 3D real-time strategy game about commanding a local joint response force through a fictional Middle Eastern city under terrorist attack and later conventional military escalation.

The game combines readable squad command, base building, production, roads, Oil/Fuel logistics, import/export, boarding and transport, aircraft, parachute and cargo drops, ground-to-air defense, ground-to-ground fire, civilians, district consequences, and the ARIA command assistant. Its differentiator is not the number of systems. It is the way those systems create one coherent command story about precision, legitimacy, and recovery.

## Product Promise

```text
Take command in seconds.
Read a living city under pressure.
Build and move the force the operation requires.
Defeat confirmed hostile threats without losing the people and systems you protect.
Uncover a campaign mystery that changes what command means.
```

## Audience And Platform

| Area | Direction |
|---|---|
| Primary platform | Mobile landscape, with controls and information density designed for touch first. |
| Secondary platform | Desktop/editor validation and possible later PC distribution without compromising mobile usability. |
| Audience | Players who want approachable command fantasy, short authored RTS missions, strategic consequence, and military-system depth without high-APM controls. |
| Session shape | 3-5 minute onboarding missions, 6-10 minute standard missions, 8-14 minute mastery missions. |
| Rating tone | Serious non-graphic conflict. Terrorism, displacement, and infrastructure attacks are acknowledged without gore or exploitation. |

## Design Pillars

| Pillar | Product rule |
|---|---|
| Command Immediately | A first-time player reaches meaningful tactical control before seeing the full menu or progression stack. |
| Readable Combined Arms | Infantry, vehicles, aircraft, buildings, logistics, missiles, civilians, and objectives remain legible at mobile scale. |
| A Living City, Not A Board | Roads, fuel, markets, shelters, workers, and civilians carry human and strategic meaning. |
| Precision Creates Legitimacy | Enemy defeat, civilian safety, evidence quality, and infrastructure recovery all define success. |
| Systems Serve Story | Every major gameplay feature is introduced because the campaign creates a need for it. |
| ARIA Assists; The Player Commands | ARIA explains, recommends, previews, and performs bounded approved actions. She never replaces player authority. |
| Fair Completion | The full campaign story and canonical ending are available through play, never through payment, ads, or mastery grind. |

## Fiction And Campaign

The first campaign is provisionally titled `Shattered Relay`. Coordinated attacks by the Ash Line terrorist network sever command across Sahrin. The player becomes Field Commander under emergency continuity rules and works with the fragmented assistant ARIA to restore the city.

The attacks are part of a plan by former emergency planner Nadir Qassem to reactivate and seize the Civic Relay, a dormant network connecting infrastructure and military response. The campaign moves from local insurgent attacks to logistics warfare, hidden-cell investigation, proxy-backed armor and aircraft, and a citywide final assault.

The detailed authority for setting, factions, character casting, all 25 mission story beats, and the ending is `Campaign_Narrative_Bible.md`.

The complete playable high-level contract for each mission is `Campaign_Mission_High_Level_Design_Catalog.md`. The complete Campaign cinematic/comic inventory and panel/communication beats are in `Campaign_Narrative_Sequence_And_Comic_Catalog.md`.

## Player Role

The player is a locally legitimate Field Commander of the Daryat Joint Response Command. The player selects a name and portrait inside the opening story. The role has professional competence but no fixed gender, ethnicity, personality, or detailed biography.

The Commander is not a silent camera. Results, choices, and operational style define a leadership arc:

1. Respond decisively.
2. Accept responsibility for infrastructure and civilians.
3. Question incomplete intelligence.
4. Control escalation without becoming indiscriminate.
5. Establish legitimate, bounded command after victory.

## Primary Game Loop

```text
Story Beat
-> Mission Briefing
-> Intel And Preparation
-> 3D Operation
-> Tactical Result
-> City Consequence
-> Character And Mystery Beat
-> Next Command Decision
```

The loop is shorter during onboarding and deeper in later chapters. Planning, briefing, minimap, deployment, alerts, and battle remain views over the same 3D operation world wherever the active map architecture permits.

## Game Modes

### Campaign

Campaign is the product's authored spine and complete central story.

- Five chapters, five missions each.
- Controlled feature introduction, reinforcement, combination, and mastery.
- Mandatory chapter revelations and optional supporting evidence.
- Visible required objectives, mastery stars, consequences, and rewards.
- Replayable cutscenes and recovered evidence in a Story Archive.
- No three-star requirement for narrative completion.

### Operations

Operations is the persistent district consequence mode.

- Multi-day or multi-week city stabilization.
- Security, Trust, Infrastructure, Enemy Influence, Intel Confidence, Civilian Density, Heat, and Supply pressures.
- Tactical missions and authored abstract actions.
- Supplemental character and district stories, not mandatory campaign revelations.
- A place for reconstruction and remaining-cell consequences after Campaign chapters.

### Skirmish

Skirmish is the configurable systems and replay mode.

- Fast setup using existing AI, economy, roster, and map controls.
- Hidden Cell, military, mixed, random, and AI-versus-AI possibilities.
- Difficulty, resources, tech, threat count, victory condition, and match-length controls.
- No superior campaign story rewards or progression bypass.

## Campaign Structure

| Chapter | Player fantasy | Dominant feature family | Narrative movement |
|---|---|---|---|
| 1. First Response | Survive the coordinated attack and establish command. | Core command, building, production, warnings, basic transport and breach. | A revoked ARIA credential appears in enemy traffic. |
| 2. Broken Grid | Restore the city's lifelines under attack. | Roads, Oil/Fuel, automated hauling, convoy protection, repair, later resource exchange. | The enemy is rerouting infrastructure into dormant Relay nodes. |
| 3. Hidden Network | Find real threats without treating the city as the enemy. | Scan, Intel, confirmation, raids, evidence, extraction, civilians. | ARIA admits she partitioned the audit of the original command breach. |
| 4. Air And Armor | Defeat proxy-backed conventional escalation. | G2A, radar, armor, G2G, aircraft, parachute and cargo drop. | Qassem needs ARIA and the Commander's authority to unlock the Relay. |
| 5. Citywide Command | Coordinate every system during the final assault. | Full combined arms, logistics, civilians, Intel, multi-objective command. | The conspiracy is exposed and the future of the Relay is decided. |

## Gameplay System Roles

| System family | Product role |
|---|---|
| Select, move, attack, Hold, Stop | Immediate command literacy and the foundation for every mission. |
| Buildings and production | Convert preparation and territory into combat options. |
| Roads and routing | Make city access, repair, evacuation, and logistics physically meaningful. |
| Oil, refinery, Fuel, hauling | Create a visible operational chain supporting vehicles and aircraft. |
| Import and export | Provide authored emergency trade decisions, not a detached market screen. |
| Boarding and transport | Solve spatial, timing, rescue, and reinforcement problems. |
| Aircraft, parachutes, cargo drops | Trade safety, speed, and risk across contested distances. |
| G2A and radar | Reward coverage, anticipation, and protection against air pressure. |
| G2G launchers | Provide deliberate long-range force with explicit minimum range and civilian-risk constraints. |
| Scan and Intel | Separate confirmed hostility from uncertainty and make information actionable. |
| Civilians and refugees | Represent the people affected by routes, attacks, evacuation, and infrastructure loss. |
| ARIA | Connect tutorial, tactical advice, narrative mystery, accessibility, and bounded automation. |

Current runtime maturity and campaign readiness are tracked in `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`. A feature may appear in this target design before its campaign-ready implementation exists; mission production must respect the readiness gate.

## First Player Experience

The first launch is part of the story, not a menu tour.

```text
Minimal logo
-> Coordinated-attack cold open
-> ARIA emergency boot
-> Diegetic Commander name and portrait
-> Guidance choice
-> M01 First Contact
-> First result and mystery clue
-> Command-base main menu reveal
```

The full menu, store, Operations, Skirmish configuration, social/account prompts, and dense settings do not appear before the player has completed or deliberately exited the first mission. The primary first action should occur within roughly 60-90 seconds of launch, with a target closer to 60 seconds after optimization.

The full flow, returning-player behavior, progressive menu disclosure, accessibility, and success measures are defined in `First_Player_Experience_And_Story_Onboarding_Design.md`.

## Mission Design Rules

Every authored mission needs:

- A visible operational necessity.
- A confirmed hostile objective and legal/fictional reason for force.
- One dominant gameplay learning or mastery goal.
- A civilian or infrastructure context when the map is populated.
- A character relationship beat.
- A chapter question and story answer.
- Required objectives visible before and during play.
- Separate mastery stars that do not gate the story.
- A result that explains tactical, city, and narrative consequences.
- A feature-readiness gate matching the current runtime.

The reusable mission grammar remains in `Gameplay_North_Star_And_Content_Grammar.md` and `Level_And_Mission_Content_Plan.md`.

## Progression And Rewards

- Campaign progress follows first-clear mission completion, not star totals alone.
- Stars reward mastery and may unlock optional rewards, challenges, or cosmetic recognition.
- Story-critical Protocol Fragments are guaranteed by chapter completion.
- CommanderXP, persistent Credits/Command, fixed unlocks, inventory items, and blueprint parts follow the canonical economy documents. Materials, Fuel, and Oil exist only inside a match.
- Trust, Evidence, and Infrastructure reflect authored outcomes and drive epilogue emphasis.
- Premium resources, purchases, advertisements, and Rush Tickets never reveal story, complete objectives, or erase mission consequences.
- Returning players receive a clear `Continue Campaign` route and optional recap.

## ARIA Product Role

ARIA must operate consistently across story and gameplay:

| Layer | ARIA role |
|---|---|
| Story | Surviving Civic Relay assistant whose missing archive is part of the central mystery. |
| FTUE | Contextual teacher who introduces one action at a time. |
| Tactical play | Goals, recommendations, alerts, reports, `Show Me`, bounded `Do It`, and cancellable temporary control. |
| Accessibility | Optional narration, subtitles, pace support, and stuck-state recovery. |
| Operations | Staff officer explaining district tradeoffs and uncertainty. |

ARIA does not secretly become the villain, issue unrestricted autonomous attacks, conceal paid choices, or provide abilities the runtime cannot execute.

## Narrative Presentation

The recommended format is a grounded illustrated motion-comic package using approved project models and environments as continuity references.

- 30-45 second opening cold open.
- 10-20 second mission brief and debrief sequences.
- 3-8 second non-blocking in-mission communication beats.
- 45-75 second chapter transitions and finale sequences.
- Subtitles and replay controls from the first release.
- Pre-generated, reviewed assets only; no runtime generative AI dependency.
- One reusable sequence format for most scenes; exceptional hero moments may use Unity Timeline.

See `Narrative_Presentation_And_Cutscene_Design.md` for format and production rules. See `Campaign_Narrative_Sequence_And_Comic_Catalog.md` for the complete first-Campaign sequence inventory.

## World, Character, And Visual Direction

- Grounded stylized 3D at readable mobile scale.
- Middle Eastern-inspired urban and rural environments with cultural specificity added through reviewed references, not stereotypes.
- A rich lived-in ground layer: dirt, grass, bushes, stones, tracks, rubble, road wear, irrigation, walls, and human use.
- Distinct project character models retain consistent narrative roles.
- Civilians, JRC soldiers, contractors, pilots, and insurgents are not interchangeable skins.
- Faction recognition uses silhouette, equipment, insignia, posture, context, and UI confirmation without relying only on color.
- Real extremist, religious, and national symbols are prohibited unless a later legal and cultural review explicitly approves documentary use.

## Audio Direction

- ARIA: calm, concise, operational, never cute or sarcastic.
- Dalia: immediate field clarity and human concern for forces under command.
- Samira: grounded civilian authority, neither helpless nor anti-military by default.
- Qassem: controlled and persuasive without theatrical villain cliches.
- Regional musical influence requires authentic instrumentation and cultural consultation.
- Combat mix prioritizes warnings, orders, impacts, transport state, missile preparation, civilian alerts, and objective feedback.
- All critical narrative information must remain available through text and visual feedback.

## Ethical And Cultural Guardrails

- The game depicts fictional bad actors and fictional forces only.
- Terrorism is represented through deliberate criminal violence and coercion, never as an ethnic or religious trait.
- Armed insurgents may operate near civilians; civilian appearance is never proof of hostility.
- Civilian models never transform into hidden enemies as a surprise mechanic.
- No collective punishment, torture, celebratory civilian harm, or objectives that reward indiscriminate fire.
- Women occupy meaningful civilian, military, aviation, contractor, and hostile roles.
- Local civilians and institutions have agency and contribute to victory.
- Language, names, signage, clothing, architecture, and music require cultural review before production lock.

## Monetization Guardrails

- Sell identity, convenience within existing fair limits, and fixed transparent content; do not sell victory or narrative access.
- No energy gate prevents completion of the central campaign.
- No premium strike, revive, or resource can directly satisfy objectives or stars.
- No paid choice produces the morally or narratively "correct" ending.
- Story Archive access is earned through play and remains available offline where platform policy permits.
- First-session monetization does not interrupt the cold open, M01, or first debrief.

## Current Product Reality

The Match runtime contains substantial tactical and logistics capability, but the complete Campaign product layer is not yet present. Scene launch, authored objectives, mission results, chapter progression, story sequence playback, and campaign persistence must be treated as target systems until validated in the current runtime.

Design documents must distinguish:

| Label | Meaning |
|---|---|
| Implemented | Present and usable in the active runtime path. |
| Partial | Present but missing campaign-facing behavior, UX, validation, or completeness. |
| Scaffolded | Code/data exists but is not reliably available in the current player path. |
| Designed | Target behavior only; not a runtime claim. |

## Product Success Measures

High-level targets for later instrumentation and tuning:

- First meaningful command within 60-90 seconds on first launch.
- M01 completion without external explanation for most target players.
- Players can state who ARIA is, who attacked the city, and why M02 matters after the first session.
- Chapter feature exposure does not exceed one dominant new system per mission.
- Players recognize civilians and confirmed hostiles without relying only on color.
- Story completion does not correlate with spending requirements.
- Trust, Evidence, and Infrastructure outcomes are understood from result screens.
- Returning players can resume the current mission or recap within two primary actions.

## High-Level Document Precedence

1. `AAA_Mobile_Game_Design_Document_v0_2.md`
2. `Campaign_Narrative_Bible.md`
3. `Gameplay_North_Star_And_Content_Grammar.md`
4. `First_Player_Experience_And_Story_Onboarding_Design.md`
5. `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
6. `Narrative_Presentation_And_Cutscene_Design.md`
7. `Level_And_Mission_Content_Plan.md`
8. `Campaign_Mission_High_Level_Design_Catalog.md`
9. `Campaign_Narrative_Sequence_And_Comic_Catalog.md`
10. `SagaChapters/README.md` and the five chapter documents
11. System-specific and implementation documents

When a lower document conflicts with this hierarchy, the higher active authority wins. Historical v0.1 and archived documents remain reference material only.

## Deferred To Later Implementation Planning

This version intentionally does not prescribe class names, scene hierarchy, data schemas, asset-generation batches, task ownership, estimates, or step-by-step production work. Those belong in implementation plans created after the high-level design is accepted.
