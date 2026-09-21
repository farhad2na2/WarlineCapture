# CH02-M01 Gridlock — production and acceptance plan

Date: 2026-09-21. Status: **planned; implementation and all gameplay acceptance gates are pending**.

The next campaign mission after Chapter 1's Breach Assault is Chapter 2, Mission 1, **Gridlock**. It opens Broken Grid by teaching the player to restore a hospital relief route while protecting the people doing the work. **Delivery requires ARIA to play and win the actual mission through Watch ARIA Play.** A scripted objective probe or a human-assisted run cannot satisfy that requirement.

This document covers one mission and the Chapter 1 → Chapter 2 transition. It does not authorize implementation as part of the current planning task. Proposed numbers below are initial authoring values, not measured balance. Evidence and any revisions should be recorded in `Design/AgentReports/CH02M01Gridlock/`; raw local logs and captures go under `/private/tmp/warline-gridlock/` with hashes and paths in the report.

## Authority and inspected baseline

- [Campaign mission catalog](Campaign_Mission_High_Level_Design_Catalog.md#ch02-m01-gridlock): mission identity, route-restoration purpose, Fadi's local knowledge, sabotage clue, and authored route-clearing fallback.
- [Broken Grid chapter](SagaChapters/Saga_Chapter02_Broken_Grid.md): chapter question, character arcs, feature and reward direction.
- [Mission template](Level_And_Mission_Content_Plan.md#required-mission-spec-template), [feature readiness](Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md), [narrative bible](Campaign_Narrative_Bible.md), and [sequence catalog](Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-2-broken-grid).
- [Watch ARIA acceptance](Roadmap/Aria_Demonstration/ACCEPTANCE.md) and [architecture](Roadmap/Aria_Demonstration/ARCHITECTURE.md): ordinary touch actuation, public observations, interruption, real outcomes and evidence.

Inspected source baseline: `895744990`. The working tree was clean before this plan. M1–M5 have documented internal Editor acceptance; that history is not a fresh validation of this checkout. No `gridlock`, `saga.ch02` or `scenario.ch02` identity was found in the inspected `Assets/Game` C#, asset and JSON files.

Concrete integration gaps:

| Current owner | Finding and planned change |
|---|---|
| `Editor/M01FirstContactConfigBuilder.cs` | Catalog discovery is limited to Chapter01 directories. Make discovery cover registered campaign chapters, with duplicate-ID and missing-scenario checks; rebuilding Chapter 1 must retain Chapter 2. |
| `UI/Shell/Ecs/UiCampaignMissionProjectionSystem.Catalog.cs` | Availability, mission indexing and next-mission projection enumerate M1–M5. Add chapter-aware Gridlock entry and reconcile already-completed M5 saves. Audit consumers of the five-mission mask/index assumptions. |
| `Runtime/Missions/CampaignMissionProgressSettlementSystem.cs` | Next-mission links stop at M5. Add M5 → Gridlock and Gridlock → Supply Line through the shared progression owner, keeping availability distinct from deploy readiness. |
| `UI/Shell/Ecs/UiShellEcsGateway.AriaPlay.cs` | GuidedCampaign eligibility explicitly lists only M1–M5. Add Gridlock only with supported observations, interactions and acceptance coverage. |
| `Systems/RoadBuild*`, `RoadGridProjectionSystem.cs` | Road command/session/input/grid machinery exists. This is not proof of a campaign-ready repair/road-building interaction. Validate geometry and the chosen player-facing route before committing to it. |

Paths in the table are relative to `Assets/Game/Scripts/`. Use the existing ownership boundaries; do not copy the whole M5 runtime or add a second campaign store.

## Mission specification

| Required field | Planned contract |
|---|---|
| MissionId / Title | `saga.ch02.m01.gridlock` / Gridlock. Do not serialize this as Chapter 1 M06. |
| Mode / ChapterOrDay | Campaign / Chapter 2: Broken Grid, mission 1; sixth campaign mission overall. |
| MissionArchetype / ThreatFamily / StoryFaction | Infrastructure Repair / Hidden Cell / Ash Line. Standard Band with a gentle opening. |
| TeachingGoal | Inspect a broken route, protect a working crew, and verify that the route actually carries relief traffic. One dominant new decision: where and when to restore access. |
| AssistantTeachingHooks | Read the route; select and position escorts; move crews to work sites; understand work interruption; defend the completed section; verify the relief delivery. Watch uses the same visible interactions. |
| CityContext | A hospital and relief depot are separated by deliberate road blocks. Fadi's crew can reopen a locally maintained service lane. |
| StoryQuestion | Why have the saboteurs preserved some routes while blocking others? |
| CharacterBeat | Fadi contributes a usable local lane missing from ARIA's formal map. ARIA acknowledges incomplete data; Dalia protects the work and Samira explains the hospital's need. |
| EvidenceOrRevealBeat | Completion guarantees the clue that sabotage steers traffic toward a dormant substation. Do not reveal the purpose of the Relay network or award Protocol Fragment 2 early. |
| CivilianLegitimacyContext | Workers and relief personnel have explicit friendly identities. Ash Line teams are confirmed armed saboteurs by mission context and conduct. Clear rubble through crew work; do not teach firing at civilians or hospital buildings. |
| NarrativePresentationTier | Tier B chapter opening, Tier C mission brief/debrief and short nonblocking in-mission comms, following the shared presentation authority. |
| RequiredFeatureReadiness | Ordinary command/combat: existing, needs mission regression. Route work, blocker removal, vehicle traversal and Chapter 2 entry: explicit prerequisites below. Free road construction remains Conditional. |
| ScenarioSetup | Proposed `scenario.ch02.m01.gridlock`; two four-person rifle squads, Fadi, two road workers, one mission-supplied relief vehicle. Finite infantry threats. Exact prefab/config IDs, hospital district identity, spawn geometry and allowed catalog are frozen in package G0. |
| MapViewContract | Proposed logical `opmap.ch02.hospital_corridor_01`, planning camera `camera.ch02.m01.overview`, minimap projection `minimap.ch02.m01.hospital_corridor`; authored metadata at `Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_HospitalCorridor01.asset`. These are planned identities, not existing assets. |
| Objectives | Two completed work sites, Fadi and at least one worker alive, a physically connected relief route, and one relief vehicle delivered through it; final arrival zone clear for 20 continuous active seconds. |
| StarGoals | 1: complete the operation. 2: all three starting civilian workers, including Fadi, survive. 3: complete within 7 active minutes. Optional stars never gate the clue or the next mission. |
| CivilianDistrictConsequences | Save actual worker survival, delivery and mission outcome; debrief shows hospital access restored. No new persistent Trust/Infrastructure currency or Operations district mutation in this slice; abstract deltas remain zero unless a canonical campaign owner already supports them. |
| Rewards | Proposed first clear: 800 Commander XP and 4,000 Credits; replay: 300 Credits, zero repeat first-clear XP. Use existing canonical reward kinds/IDs and a single authored reward source for preview and settlement; verify against the economy authority in G0. Tactical Materials/Oil/Fuel are not account Credits. |
| Unlocks | Gridlock completion reveals Supply Line (`saga.ch02.m02.supply_line`), deployable only when its content is ready. No new permanent unit/item unlock in this slice; crew and relief vehicle are supplied for every attempt. |
| UISurfaces | Campaign chapter/mission selection, mission briefing, chapter opening/comics, existing HUD/objectives/selection/commands, minimap, ARIA/Watch/Stop, field guide, pause, results and campaign return. |
| BalanceTargetBand | Standard; target 5–8 active minutes and 8–12 minutes including first-play story/reading. Deadline 12 active minutes. Measure automated execution and human play separately. |
| ValidationPlan | G0–G6 and acceptance matrix below: data, geometry, rules, real manual/ARIA play, progression, language, lifecycle and regressions. |
| FailureRetryRules | Clear defeat reasons; no victory grants on defeat/withdrawal. Retry restores fresh actors, blockers, work progress, waves and clock. Replay retains account progress, uses replay rewards and requires fresh Watch consent. |

## Recommended playable version

Use the catalog's **authored route-clearing variant** for the first delivery. Fadi's local service lane already exists physically; two authored obstructions prevent travel. The player moves protected workers into work areas while rifles secure the approach. Work removes real obstructions and updates traversal. This provides a complete route-restoration mission without making a general road editor a prerequisite.

This is a new, bounded mission work-site behavior, not a claim that a general engineer repair system already exists. Crew movement and combat use ordinary commands. Work begins automatically only when its displayed conditions hold; the player does not receive a button that instantly repairs the route. Reuse existing road/grid/blocker owners for the resulting world change.

Free road placement is a later extension unless G0 demonstrates an already usable path with visible entry, preview, connectivity, confirm/cancel, collision, cost and ARIA gesture support. Adopting that alternative requires updating this plan and repeating its acceptance for that version; never silently ship a mixture of two incomplete paths. The authored variant itself must pass physical traversal and visible work-state checks.

### Opening, map and tactical flow

1. After M5 completion, enter Broken Grid through the ordinary Campaign route. Show its five-panel chapter opening once, with archive/replay access, then Gridlock's brief. Existing M5-complete saves receive the same access without replaying M5 or receiving its rewards again.
2. Show the hospital, unusable formal road, relief depot, first work site and local service lane in a short skippable camera tour. Return to the exact playable view. Mission clock and hostile release start after the shared story/camera/guidance handoff.
3. Clear a small visible hostile screen near site A with the rifle squads. Fadi's report explains why the formal road cannot be used and identifies the local lane. The objective UI and minimap now agree on the usable route.
4. Move the workers to site A. Twenty-five seconds of eligible work opens the first obstruction. Show `Working`, `Crew missing` or `Threat nearby`, plus actual progress. Move rifles ahead while the crew remains sheltered.
5. Advance along the opened section to site B. Another 25 seconds of eligible work opens the hospital approach. A warned saboteur attack threatens the completed section, requiring the player to protect the route as well as the current crew.
6. Once both sites are complete and the route is clear, the supplied relief vehicle travels from the depot to the hospital through ordinary vehicle navigation. It waits with an explicit reason when the next route segment is contested. The player protects it with the already-learned escort commands; no new convoy-control UI is required.
7. Actual vehicle arrival and a 20-second clear hospital-zone hold complete the operation. Show relief arriving, deliver the steering-pattern clue, and point to Fuel shortages in Supply Line. Results show the real survivor count, time, stars and rewards.

Author two feasible approaches to the work front: direct exposed cover and a safer staging lane. The permanently damaged formal route must visibly remain unusable. Preserve an open fallback segment for retreat and worker regrouping. The mission does not require killing distant or irrelevant enemies; passive allied scenery cannot complete it.

Survey the shared physical city before selecting the exact area. Required anchors: depot, squad A/B, Fadi, workers, relief vehicle, hospital, work A/B, both obstruction footprints, route waypoints, sheltered staging, fallback lane, threat approaches, civilian areas and camera bounds. There are no construction zones in the chosen variant. Register roads, sidewalks and all dynamic blockers, including vehicle-width clearance and turn space. Generate a separate logical operation map; changes to shared physical surfaces need an explicit reuse/regression decision.

Initial pressure budget: two three-person hostile screens and one four-person counterattack, using canonical infantry stats. Warn at least 10 active seconds before the counterattack can reach exposed workers; verify actual travel time. Adjust composition/spacing openly after the first real run. No hidden health bonuses, infinite replacement waves or unannounced attacks on an offscreen crew.

### Authoritative rules and recovery

- A site accrues work only while Fadi and at least one living road worker from this attempt are within its authored work area and no confirmed living hostile contests it. UI reads these same facts. Interruptions pause accumulated work; returning resumes it. Completed sites remain completed.
- Site completion removes only its registered obstruction through the owning world/grid boundary. Record completion after the mutation succeeds. A visual disappearance without collision/pathing update is a failure, not a cleared route.
- A completed section may become contested by the warned attack, stopping safe transit. Do not recreate rubble on a timer. Regaining control restores safe transit without repeating completed work.
- Connectivity is necessary but insufficient: victory requires the live relief vehicle to traverse the route and arrive. Spawn at the depot, use ordinary navigation and real movement; do not teleport, count a path query as delivery or turn on an independent player-faction autoplayer.
- Defeat: Fadi dies; both road workers die; all rifle members die before completion; the relief vehicle is destroyed; or the 12-minute active deadline expires. Any mandatory loss takes precedence over victory on the same update. Losing one worker is recoverable and loses only the preservation star.
- An entity missing because of initialization, unload or stale identity is never treated as delivered or dead by assumption. Bounded initialization/integrity failure prevents settlement and presents a retryable diagnostic.
- Hospital hold resets when contested or the delivery vehicle leaves before completion. Pause, field guide and blocking story/camera transitions freeze all mission timers consistently.
- Stop/Hold, retreat, crew reselection and route re-entry must recover from mistaken orders. No work or command from a prior attempt can affect a new one. Exit/retry restores attempt-owned obstructions and unregisters handlers without changing the shared city or other missions.

## Narrative, tutorial and presentation

Preserve these sequence identities: `seq.ch02.open.broken_grid`; `seq.ch02.m01.brief`, `.comms`, `.debrief`. The chapter opening has five panels; the mission catalog supplies three brief beats, one comms beat and three debrief beats. Plan 12 panel compositions in total, allowing reuse only where it genuinely fits the authored beat. The opening is chapter content, not a replacement for the mission brief.

Establish Fadi's appearance from the canonical `Chr_Civilian_Male_01` identity and one consistent portrait/reference. Maintain the current M3–M5 Dalia, Samira and cyan ARIA art direction. Create final bitmap artwork with the image-generation workflow during production, retain provenance, and inspect actual 16:9 and 20:9 crops. Captions and UI text remain outside the raster. Failure cannot play the successful hospital-arrival debrief.

Write English and conversational Iranian Farsi together for mission/chapter labels, objectives, work states, threat warnings, crew selection, hints, guide lessons, Watch status, defeat reasons, rewards and comics. Use the shared localization catalog and current Persian shaping/font pipeline. Use the established Materials/Oil/Fuel semantics and Persian نفت / بنزین labels if those resources are visible. Mission-supplied transit has an explicit fuel allowance; an unbuilt refinery must not become a surprise requirement.

Add concise field-guide lessons for crew selection, work conditions, paused progress, protected staging and safe relief transit. Explain the actual command sequence one action at a time. Show Me exposes the next visible control or target; accepted actions and authoritative progress advance lessons. Normal manual controls remain sufficient to win with guidance disabled. The Watch observation path must remain available independently of whether instructional prose is displayed.

Integrate matching EN/FA story and tutorial voice clips as a production deliverable, following the existing speaker and local playback pipeline. Prepare exact scripts before any external generation; apply the actual provider authorization policy at that stage. Caption-only gameplay can be internally tested while voice is pending, but cannot be described as final voiced acceptance. Language switching, Skip, pause, Stop and results cancel or transition audio correctly.

## Implementation packages

Complete in dependency order. No calendar estimate is asserted before G0 resolves the physical route and crew/vehicle feasibility.

| Package | Work | Exit evidence |
|---|---|---|
| G0 — feasibility and authoring freeze | Survey candidate city area; resolve exact soldier, Fadi/worker and vehicle configs; validate one work site removing a real obstruction and an ordinary vehicle traversing it; freeze geometry, thresholds, reward mapping and the chosen authored variant. | Config/footprint inventory, rendered map review, one manual touch sequence, actual traversal and a documented scope decision. If this fails, repair the owning system before final assets. |
| G1 — chapter and data | Add Chapter02 mission/scenario/map assets through Editor builders. Extend bounded immutable mission definitions, contract validation, catalog discovery, chapter selection/readiness and existing-save reconciliation. Add explicit next links and narrative entry state. | M5-complete fixture exposes Gridlock; incomplete save stays locked; rebuild preserves M1–M5 and Chapter02; Supply Line remains non-deployable without content. |
| G2 — objective runtime | Add narrow work-site, route, relief and survivor facts/state under `Components`, `Composition` and `Runtime/Missions`; append serialized enum values. Reuse normal spawn/combat/navigation, attempt lifecycle and settlement. | Rule and integration checks for real obstruction clearing, contested work, arrival, loss precedence, retry cleanup and reward idempotency. |
| G3 — playable UI and guidance | Bind objectives/work progress, route/crew selection, warnings, minimap, camera handoff, guide and next-action cues through existing UI contracts. | Manual full win plus recoverable interrupted-work run; every visible action has the advertised world result; no debug-only step. |
| G4 — Watch ARIA Play | Add Gridlock eligibility, public work/route/crew/visible-threat observations and bounded interaction/recovery skills. Extend `MatchHudAssistantUiSystemHelper.Watch`, `AriaPlayDecisionSystem` and the existing touch input boundary as needed. | Early end-to-end EN victory using shipping touch input; then all ARIA acceptance rows below. A guidance-disabled observation gap is work to complete, not grounds to waive the gate. |
| G5 — final content | Complete chapter/mission art, EN/FA localization, Fadi reference, voices, readable guidance and result/campaign presentation. | Actual story/audio review, locale and layout captures, no missing keys or reused contradictory lines. |
| G6 — certification and report | Run final-build ARIA and manual journeys, failure/retry/save/return coverage, affected campaign/road/input regressions, then device and player review for release. | Acceptance matrix with build identities, actual pass/fail/unrun results and remaining scope. All ARIA wins rerun after changes affecting mission behavior or input. |

Gameplay truth remains in ECS; UI and narrative consume read models. The planner sees only information exposed to the player, never hidden wave schedules, target entities, private route solutions or health behind fog. New steady-state paths must respect the existing [architecture](Architecture/gameplay_solid_ecs_contract.md) and [performance](Architecture/performance_regression_contract.md) contracts; do not weaken their baselines.

## Mandatory acceptance matrix

All rows start **Not run**. “Internal Editor-ready” requires A01–A10; “release-ready” additionally requires A11–A12. A final-voice row still pending must be reported explicitly even if a captioned internal gameplay checkpoint passes.

| ID | Gate | Required evidence |
|---|---|---|
| A01 | Config and campaign access | All IDs, references, anchors, roles, rewards and localization keys resolve. M5 → Chapter 2 → Gridlock works through public menus for newly completed and existing M5-complete profiles. No duplicate chapter opening/rewards. Unimplemented Supply Line is not deployable. |
| A02 | Physical route and world truth | Both obstructions visibly and physically clear; workers and vehicle fit all paths/turns; relief vehicle moves from depot to hospital. A broken/disconnected route, surviving obstruction or missing vehicle never yields victory. Retry restores the initial geometry. |
| A03 | Rules and failures | Test work pause/resume, completed-route contest/recovery, loss of one worker, every mandatory defeat reason, deadline, hold reset, same-update win/loss precedence, missing/stale actors and delayed callbacks. No idle win or objective progress from unrelated actors. |
| A04 | Manual play | Normal player inputs win with guidance on and off. Demonstrate a mistaken crew order followed by recovery. Result/retry/return work without developer tools or outcome edits. |
| **A05** | **ARIA plays and wins — hard acceptance gate** | **Six uninterrupted, normal-speed wins on the final tested content: one guided baseline, one guidance-disabled run with a changed camera/layout, and one disclosed recoverable mid-mission start, each in EN and FA. All six must win without intervention after Start.** Failure stays recorded and the affected case must be fixed and rerun. |
| A06 | ARIA input, interruption and adverse states | Stop during crew selection, a world-order touch and any added road gesture; physical override, pause/background, story transition, result and scene exit. No later synthetic order, click-through or auto-resume. Reconfirm after handback and complete a fresh run. One loss fixture per language must acknowledge defeat honestly. |
| A07 | Fairness and recovery | ARIA uses visible state and normal input, re-observes moving/occluded targets, and recovers from a rejected/missed command or contested site. No direct command calls, `Button.onClick.Invoke`, health/resource/position edits, forced outcomes, hidden faction automation or fixed-coordinate replay. Normal unit auto-engagement and the disclosed relief vehicle route behavior are allowed game rules. |
| A08 | Results, persistence and replay | First-clear preview equals actual grant; replay grants only replay rewards; duplicate settlement/retry cannot duplicate grants. Save failure has an honest retry. Survivor count/time/stars/clue persist correctly; public Campaign return and fresh replay work. |
| A09 | Story, localization and visual/audio review | Complete chapter opening, mission brief/comms/debrief and final voice reviewed in EN/FA; correct connected Persian text, conversational copy, glyph order and unclipped objectives/Stop at 16:9 and 20:9. Camera/Skip/reduced motion restore control; no stale guidance or speech after result. |
| A10 | Relevant regressions and performance | Focused mission/rule, campaign catalog/readiness/settlement, road/blocker/navigation, guidance, touch/Stop and architecture checks pass. Repeat M1–M5 ARIA baselines when changing their shared observation/input or progression paths. Compare Watch on/off under the existing performance budgets. |
| A11 | Device release | Exact candidate build passes EN/FA play and ARIA win, physical touch takeover, smallest supported phone/tablet layout and Watch on/off profiling on supported hardware. Record identity/build and preserve existing budgets. Editor evidence does not close this row. |
| A12 | Player comprehension and pacing | At least three unfamiliar players can identify the broken route, move/protect a crew, understand interrupted work and find Stop after watching ARIA. Record completion and confusion, first-play time and fairness of attacks; fix consistent confusion. |

### What counts as an ARIA win

The baseline starts through Campaign → Chapter 2 → Gridlock briefing on an isolated profile, with ordinary story/camera flow and the genuine Watch → Start confirmation. The test driver may navigate the pre-session menus and confirm Start, then becomes a read-only observer until ARIA stops at the result. It cannot click lessons, pan the camera, issue orders, rescue the crew, accelerate time or change gameplay facts while ARIA controls the mission.

Guided baseline runs use EN 16:9 and FA 20:9. Guidance-disabled runs swap those layouts and alter the initial camera view. Recoverable starts are prepared before confirmation with site A complete, site B incomplete and a living crew moved out of work range or the route visibly contested; disclose every fixture change. After Start, ARIA must assess the public state, recover, finish both work objectives and deliver the vehicle. These fixtures supplement the full public-entry baselines.

The shipping visible finger must correspond to the submitted input. Each meaningful ARIA action needs a touch trace and a verified normal-handler/world result. A logged tap, a passed rule test or a victory reached by a hidden mission solver is insufficient. At most two corrective attempts at an unchanged failed interaction precede a new observed approach or safe handback; a global no-progress check catches repeated cycles. Safe handback is correct failure handling, but does not count as a winning A05 run.

Completion requires the actual mission outcome, visible victory/result screen, correct stars and saved settlement. ARIA must stop at the result and cannot grant itself authority for another match. The observer may then check Return/replay separately using normal UI. Record active mission time separately from story/setup time and leave failed attempts in the evidence register.

For every run record source/config hashes, platform/build, locale/aspect, initial profile/fixture, Start/Stop/outcome timestamps, required objective facts, touches/retries/stalls, survivor/time/star values, settlement identity, captures and log paths. Use stable proposed case IDs `GRIDLOCK-ARIA-EN-BASE`, `-FA-BASE`, `-EN-NOGUIDE`, `-FA-NOGUIDE`, `-EN-RECOVER`, `-FA-RECOVER`. Retain evidence needed to reproduce the result; do not infer gameplay success from an old campaign report.

## Validation execution and handoff

Use RTK for shell output. On macOS, all Unity executeMethod, test, build and capture runs use `rtk proxy Tools/CI/invoke_unity_macos.sh` with an explicit log and timeout, while Hub remains open and signed in. No direct Unity invocation, `-batchmode`, reset/IPC cleanup or terminating the user's Editor. Live Editor work follows the repository's Unity CLI/Pipeline contract. Use isolated QA profiles and a separate validation checkout when project ownership requires it.

Add focused validation entry points for Gridlock rules, chapter transition and the ARIA journey in G1–G4. Require exact success markers in preserved full logs in addition to a successful wrapper exit; missing markers, timeouts and project locks fail the run. Proposed markers should be defined by the implementation, with case/locale/outcome included so an earlier partial pass cannot close the whole run. Do not cite a planned test method as already callable.

Planning completion: this specification and roadmap link exist and are checked. Production completion: G0–G6 evidence and all applicable acceptance rows are reconciled. **Gridlock remains incomplete until ARIA has played and won it under A05.**
