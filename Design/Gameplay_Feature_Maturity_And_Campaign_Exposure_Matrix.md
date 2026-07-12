# WarlineCapture Gameplay Feature Maturity And Campaign Exposure Matrix

Date: 2026-07-10

Status: Active high-level campaign exposure authority

Scope: Current maturity classification, narrative purpose, intended first authored use, reinforcement, mastery, and campaign-readiness conditions. This is not an implementation tracker.

Upstream authorities: current validated runtime evidence, `AAA_Mobile_Game_Design_Document_v0_2.md`, and `Campaign_Narrative_Bible.md`.

Direct consumers: `Gameplay_North_Star_And_Content_Grammar.md`, `Level_And_Mission_Content_Plan.md`, `Campaign_Mission_High_Level_Design_Catalog.md`, `SagaChapters/README.md`, feature-specific design documents, and later implementation prerequisites. When code and this matrix disagree, re-audit and update the matrix; do not reinterpret code from a stale design claim.

## Purpose

WarlineCapture already contains more gameplay systems than the older Campaign documents acknowledge. This matrix prevents two failures:

1. Shipping a story that ignores the project's strongest mechanics.
2. Writing missions around features that exist only partially or are not reachable in the active player path.

Every mission author must check this matrix before making a feature required.

## Maturity Vocabulary

| Label | Meaning |
|---|---|
| Implemented | Present and usable in the active Match runtime path. Campaign integration may still be absent. |
| Implemented/Partial | Core behavior exists, but campaign UX, edge cases, balance, feedback, or validation remains incomplete. |
| Partial | A meaningful slice exists, but the feature is not ready to carry a required authored objective. |
| Scaffolded | Code, data, or UI exists, but the current player path does not reliably enable or complete it. |
| Designed | High-level target only. Do not present as current runtime behavior. |

Maturity is an audit snapshot, not a percentage or permanent promise. Implementation plans must revalidate it against the current branch.

## Campaign Exposure Rules

- `Introduce`: one dominant new decision with strong ARIA support.
- `Reinforce`: reuse the decision with less guidance and a real consequence.
- `Combine`: pair it with one or two mastered systems.
- `Master`: require independent use under pressure.
- A feature cannot become a required objective while `Partial`, `Scaffolded`, or `Designed` unless the mission is explicitly blocked on a readiness gate.
- Story dialogue cannot claim functionality the runtime cannot perform.
- Optional spectacle is not the same as authored mastery.

## Master Matrix

| Feature | Current maturity | Narrative purpose | Introduce | Reinforce / combine | Mastery | Readiness gate before required use |
|---|---|---|---|---|---|---|
| Core select, move, attack | Implemented | Establish immediate field command and confirmed hostile engagement. | Ch1 M01 | Every Chapter 1 mission | Every finale | Mobile-readable selection, destination, attack, invalid-target, order, objective, and result feedback. |
| Hold and Stop | Implemented/Partial | Show disciplined control and safe cancellation, especially near civilians. | Ch1 M01 | Ch1 M03-M05 | Ch3 and Ch5 precision missions | Behavior and feedback must be consistent for all required unit types. |
| Building placement | Implemented/Partial | Reestablish JRC presence, restore services, and shape defense. | Ch1 M02 | Ch2 repair and logistics | Ch5 multi-front operations | Do not narrate construction phases or worker behavior unless they exist; placement validity and consequences must be clear. |
| Unit production | Implemented | Turn preparation, economy, and time into force composition. | Ch1 M02 | Ch1 M03 and Ch2 | Ch5 | Queue, cost, completion, and blocked-state feedback must be mission-readable. |
| Roads and route construction | Partial/Scaffolded | Reconnect hospitals, markets, fuel, evacuation, and convoy access. | Ch2 M01 after readiness | Ch2 M04-M05 | Ch5 M04 | A clear player entry, connected placement behavior, route validation, visuals, and objective hooks must be available. |
| Oil extraction, refinery, Fuel storage | Implemented/Partial | Make civilian services and combined arms depend on a visible logistics chain. | Ch2 M02 | Ch2 M04-M05 | Ch4 and Ch5 | End-to-end Oil-to-Fuel flow, capacity, resource feedback, mission start state, and failure recovery must be validated. |
| Automated tray-truck and tanker hauling | Implemented/Partial | Let the player design and protect a network without truck micromanagement. | Ch2 M02 | Ch2 M05 | Ch4 M02 and Ch5 M04 | Automation must be observable, interruptible through world conditions, and recover from blocked routes. |
| Field Fabrication Depot and tactical Materials | Designed | Turn Oil allocation into a choice between mobility and construction while giving Materials a local production path. | Ch2 M02 only after readiness | Ch2 M04-M05 | Ch5 M04 | One canonical tactical Materials value, stable tray routing, Oil-to-Materials conversion, dual-cost building placement, live HUD, recovery rules, and no-GC validation must pass. |
| Resource import/export | Scaffolded | Frame emergency trade, shortages, and logistics compromise. | Ch2 M03 only after enablement | Ch2 M05 | Ch5 M04 | Recipes, bootstrap, UI access, timing, rewards, and non-premium completion path must work in the active Campaign route. |
| APC boarding and transport | Implemented | Solve protected movement, evacuation, and evidence transport. | Ch1 M04 | Ch3 M04 | Ch5 M02-M04 | Capacity, board/unboard, ownership, destruction, objective tracking, and mobile feedback must be reliable. |
| Helicopter boarding, insertion, extraction | Implemented/Partial | Create landing-zone risk, urgent rescue, and fast reinforcement. | Ch1 M04 | Ch3 M04 | Ch5 M04 | Landing, boarding, rope/exit state, Fuel, anti-air interaction, and extraction objective must be validated. |
| Transport plane runway unload | Implemented | Provide safe high-capacity reinforcement when airfield control is possible. | Ch4 M04 | Ch5 M04 | Optional late mastery | Runway access, unload location, capacity, Fuel, and mission objective state must be clear. |
| Parachute personnel drop | Implemented | Trade safety and formation for rapid insertion behind contested lines. | Ch4 M04 | Ch5 M01 | Ch5 M04 | Drop zone validity, survival/readability, scatter behavior, and unit ownership after landing must be validated. |
| Vehicle cargo drop | Implemented | Deliver emergency mobility or supply to isolated fronts. | Ch4 M04 | Ch5 M01 | Ch5 M04 | Cargo selection, valid drop state, landing presentation, ownership, and no-overlap safety must be validated. |
| Radar, satellite warnings, threat feed | Implemented/Partial | Turn anticipation and information into defensive power. | Ch1 M03 | Ch4 M01 | Ch5 M01 | Warning source, lead time, camera/minimap focus, accessibility, and false/confirmed state must be readable. |
| Automatic ground-to-air launcher | Implemented | Defend air corridors through coverage and preparation rather than target tapping. | Ch4 M01 | Ch4 M02-M05 | Ch5 M01 | Detection, engagement rules, minimum/maximum range, ammunition/readiness, protection, and feedback must be clear. |
| Manual ground-to-ground launcher | Implemented | Give the Commander deliberate long-range power with visible restraint costs. | Ch4 M03 | Ch4 M05 | Ch5 M03/M05 | Range, minimum range, preparation, impact timing, target confirmation, civilian risk, and cancellation must be explicit. |
| Scan and Intel | Partial | Separate evidence from assumption and make hidden-cell conflict fair. | Ch3 M01 after readiness | Ch3 M02-M04 | Ch3 M05 and Ch5 M03 | Universal enemy minimap exposure must be removed; confidence, reveal, expiration, false lead, and objective behavior must be coherent. |
| Civilians | Partial | Make protection, route choice, and collateral consequences human. | Light exposure Ch1 M01 | Ch1 M04 and Ch2 | Ch3 and Ch5 | Civilian identity, movement, danger, casualty/collateral state, HUD communication, and result consequences must be understandable. |
| Households, displacement, refugees | Partial | Show how infrastructure and combat reshape the city after battle. | Ch2 M04 | Ch3 M03 | Ch5 M02 | Population state must be visible and connected to authored objectives/results before severe penalties are used. |
| Authorized security contractors | Implemented/Partial | Add local perimeter, escort, and support roles distinct from regular military forces. | Ch2 or Operations | Ch3 escort/raid | Optional Operations mastery | Faction identity, acquisition, command behavior, and narrative authority must be unambiguous. |
| Bomb-suit specialist | Implemented/Partial | Support hazardous-objective and EOD stories without graphic bomb spectacle. | Ch2/Ch3 authored specialist mission | Ch4 relay sabotage | Ch5 M05 optional route | Hazard interaction must exist; until then use as durable specialist, not fictional bomb-disposal magic. |
| Ghillie heavy specialist | Implemented | Support reconnaissance, overwatch, and concealed heavy response. | Late Ch2 or Ch3 | Ch4 armor response | Ch5 | Visual role, range, reveal behavior, and civilian-risk rules must match the equipped weapon. |
| ARIA goals, recommendations, alerts, reports | Partial | Tie tutorial, tactical decisions, city consequence, and character arc together. | Cold open and Ch1 M01 | Every chapter | Ch5 bounded multi-objective support | Narrative lines must use live read models, executable support must be honest, and stale recommendations must cancel. |
| ARIA `Show Me`, `Do It`, bounded control | Implemented/Partial | Provide accessible teaching while proving the player remains Commander. | Ch1 M01 where supported | One authored beat per chapter at most | Ch5 M01/M05 with explicit consent | Only typed, validated commands; visible ownership; immediate player override; no unrestricted autopilot. |
| Tactical follow attack cinematic | Implemented | Create selected hero moments for major air or long-range actions. | Late Ch3 or Ch4 | Ch4 finale | Selected Ch5 beats | Must preserve command readability, avoid repetition, and never hide urgent state or civilian risk. |
| Mission objectives, authored result, chapter progression | Designed/absent as a complete product path | Convert the Match sandbox into a finishable Campaign. | Required before production M01 | Every mission | Campaign completion | Launch payload, objective evaluation, result publishing, persistence, replay, and default Match compatibility must be validated. |
| Story sequences and Story Archive | Designed | Deliver character, mystery, recap, and emotional continuity. | First launch | Every mission/chapter | Finale and replay | Skippable/replayable sequence path, subtitles, save state, asset continuity, and offline fallback must exist. |

## Chapter Feature Budget

| Chapter | New dominant systems | Systems deliberately deferred |
|---|---|---|
| 1. First Response | Core command, build, produce, warnings, basic boarding, breach. | Roads, full Oil/Fuel network, exchange, Intel deception, G2G/G2A mastery, parachute/cargo. |
| 2. Broken Grid | Roads, Oil/Fuel, automated hauling, local Materials fabrication after readiness, repair, later exchange, displacement. | Full Intel campaign, proxy military escalation, long-range fire. |
| 3. Hidden Network | Scan/Intel, target confirmation, evidence, civilian-risk raids, extraction. | Heavy conventional combined-arms mastery. |
| 4. Air And Armor | G2A, armor, G2G, aircraft, parachute, cargo, high Fuel pressure. | Full citywide multi-objective synthesis. |
| 5. Citywide Command | Combination and mastery of all campaign-ready systems. | No major new mechanic in the finale. |

## Character And Feature Alignment

| Character | Feature family they humanize |
|---|---|
| ARIA | Objectives, Intel, warnings, recommendations, bounded automation, central mystery. |
| Major Dalia Rahim | Core command, production, boarding, combined arms, troop survival. |
| Engineer Samira Haddad | Roads, Oil/Fuel, Field Fabrication, import/export, power, civilians, refugees, infrastructure consequences. |
| Captain Laila Nasser | Helicopter, aircraft, air corridors, G2A, Fuel, extraction. |
| Chief Yusuf Darzi | Hazardous objectives, controlled breach, EOD context. |
| Nadir Qassem | Sabotage, false Intel, infrastructure capture, military escalation, unrestricted control. |

## Mission Readiness Decision

Before a mission moves from high-level design into implementation planning, classify every required feature:

| Decision | Rule |
|---|---|
| Ready | Feature is implemented and its mission-specific UX/objective path is validated. |
| Conditional | Core exists, but the mission plan must include a bounded readiness prerequisite. |
| Replace | A simpler implemented feature can carry the same story beat. |
| Defer | Move the mission or feature exposure later; do not hide the gap with dialogue. |

## Design Implications

- M01 must use the reliable command core and should not wait for the full economy or Campaign feature set to teach play.
- Chapter 2's Market Lifeline remains high-level conditional content until Resource Exchange is enabled in the Campaign path.
- Chapter 3 cannot depend on hidden-threat gameplay while all enemies remain universally visible on the minimap.
- Bomb-suit fiction must not promise bomb-disposal interactions that do not exist.
- Chapter 4 is where the project's mature aircraft, missiles, parachute, and cargo systems become story-critical.
- Chapter 5 adds complexity through combination, not by introducing another large system.

## Related Authorities

- `AAA_Mobile_Game_Design_Document_v0_2.md`
- `Campaign_Narrative_Bible.md`
- `Gameplay_North_Star_And_Content_Grammar.md`
- `Level_And_Mission_Content_Plan.md`
- `Campaign_Mission_High_Level_Design_Catalog.md`
- `FTUE_And_Command_Assistant_Design.md`
- `ARIA_Assistant_ECS_Design.md`
- `Field_Fabrication_Materials_Design.md`
- `Campaign_Narrative_And_Content_Redesign_Recommendations.md`
