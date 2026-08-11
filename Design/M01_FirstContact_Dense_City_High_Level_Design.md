# WarlineCapture M01 First Contact Dense-City High-Level Design

Date: 2026-08-12
Status: Draft for user review
Scope owner: Mission 1 product, gameplay, narrative, operation-map integration, Campaign replay, and validation contract
Implementation status: Design only; no implementation tracker or production change is authorized by this document

## 1. Purpose

This document defines the first production Campaign mission, `M01 First Contact`, on the accepted dense Sahrin city presentation. It reconciles the earlier mission contract, the completed dense-city operation-map work, the approved FirstLaunch sequence, the current UI shell, the current operation-map identity rules, and the Android performance contract before implementation begins.

The design has two mandatory entry paths:

1. A new profile completes or skips the approved FirstLaunch story and transitions directly into M01 without seeing the normal Main Menu.
2. A returning player launches or replays M01 from Campaign Operations through the Mission Briefing surface.

Both paths must construct the same authoritative mission payload and the same deterministic `ScenarioSetup`.

## 2. Authority And Reconciliation

This HLD refines, but does not silently replace, the following authorities:

- `M01_FirstContact_Production_Contract.md`
- `First_Player_Experience_And_Story_Onboarding_Design.md`
- `Campaign_Mission_High_Level_Design_Catalog.md`
- `Campaign_Narrative_Sequence_And_Comic_Catalog.md`
- `SagaChapters/Saga_Chapter01_First_Response.md`
- `FTUE_And_Command_Assistant_Design.md`
- `Narrative_Presentation_And_Cutscene_Design.md`
- `Level_And_Mission_Content_Plan.md`
- `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
- `M01_Metric_Scale_Readability_Contract.md`
- `Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`
- `Architecture/dense_city_virtualized_render_proxy_android_60fps_implementation_tracker.md`
- `Architecture/post_hardening_architecture_maturity_tracker.md`

The earlier M01 contract assumed a missing standalone town scene. The accepted decision is now to use the dense Sahrin city and its bazaar/Old Market district. The city source is shared; M01 does not clone or fork the authored city geometry.

The current operation-map validator permits `opmap.skirmish.*` and `opmap.chNN.*` identities. Therefore this HLD preserves the valid and already-published M01 logical identity `opmap.ch01.district_edge_01`. It does not introduce the invalid `opmap.campaign.*` namespace. The M01 logical operation-map view may reuse the accepted dense-city source references and generated render database, but it retains mission-specific bounds, cameras, minimap projection, surface metadata, and anchors.

## 3. Approved Product Decisions

| Decision | Approved contract |
|---|---|
| FirstLaunch presentation | The current comic-style FirstLaunch dialogue and story presentation is approved. Gate 9R may be treated as accepted when the implementation tracker reconciles the parent evidence. |
| First-time M01 entry | No Main Menu appears between FirstLaunch and M01. The final story panel transitions into the live Old Market mission view. |
| Guidance inheritance | M01 uses the Full, Contextual, or Minimal guidance level selected during FirstLaunch. The choice remains changeable later. |
| Replay tutorial | First play uses the selected guidance. Later replay offers `Replay Tutorial`, default off. When enabled, it uses the player's current guidance level. |
| Civilians | M01 shows a bounded ambient civilian/responder group and a deterministic post-victory evacuation. Civilians are not attackable, do not create a casualty simulation, and cannot fail the mission or reduce stars. |
| City reuse | M01 uses the accepted dense city and bazaar/Old Market district. No second authored city and no cloned production geometry are created. |

## 4. Player Experience Goals

M01 must:

- continue the final FirstLaunch frame spatially and emotionally;
- place the player in command without exposing the normal menu first;
- teach one concept at a time: select, move, confirm hostile, attack, read objective, finish result;
- prove that ARIA guidance reflects the player's selected assistance level;
- present civilians as the reason for the operation without making an unfinished civilian simulation a hidden dependency;
- finish in approximately four to six minutes for a first-time player;
- impose no permanent penalty for tutorial defeat;
- reveal the revoked ARIA credential in the first debrief;
- reveal the command-base menu only after the first clear;
- make replay available through Campaign Operations without replaying the cold open.

## 5. Non-Goals

M01 does not introduce:

- building placement, production, Oil, Fuel, Materials, import/export, or economy teaching;
- roster construction, loadout optimization, transport, air, missile, road-building, or base-defense mechanics;
- a general civilian life, casualty, household, displacement, or district-consequence simulation;
- a second dense-city source, a duplicate EntityScene package, or camera-driven simulation partitioning;
- a new unrestricted ARIA automation path;
- a mid-mission strategic menu;
- M02 implementation;
- unrelated post-hardening architecture tasks such as AM-027.

## 6. Stable Identity And Naming Contract

### 6.1 Mission And Content IDs

| Role | Stable ID |
|---|---|
| Mission | `saga.ch01.m01.first_contact` |
| Scenario | `scenario.ch01.m01.first_contact` |
| Logical operation-map view | `opmap.ch01.district_edge_01` |
| Planning/handoff camera | `camera.ch01.m01.planning` |
| Battle-start camera | `camera.ch01.m01.battle_start` |
| Minimap projection | `minimap.ch01.m01.projection` |
| Brief | `seq.ch01.m01.brief` |
| In-mission communications | `seq.ch01.m01.comms` |
| Debrief | `seq.ch01.m01.debrief` |
| Primary objective | `obj.ch01.m01.destroy_patrol` |
| Failure guard | `obj.ch01.m01.keep_command_squad_alive` |

### 6.2 Required Typed Anchors

The prior unscoped draft names are migrated to IDs accepted by `OperationMapIdentityRules`:

| Purpose | Stable ID |
|---|---|
| Friendly deployment | `anchor.ch01.m01.player_spawn` |
| Planning/battle start focus | `anchor.ch01.m01.camera_start` |
| First move destination | `anchor.ch01.m01.move_target` |
| Hostile patrol spawn | `anchor.ch01.m01.patrol_spawn` |
| Patrol waypoint A | `anchor.ch01.m01.patrol_route_a` |
| Patrol waypoint B | `anchor.ch01.m01.patrol_route_b` |
| Patrol waypoint C/contact | `anchor.ch01.m01.patrol_route_c` |
| Patrol objective focus | `anchor.ch01.m01.patrol_objective` |
| Protected civilian area | `anchor.ch01.m01.civilian_safe_zone` |
| Civilian evacuation destination | `anchor.ch01.m01.civilian_evacuation` |
| Initial minimap viewport | `anchor.ch01.m01.minimap_start` |

Names describe stable semantic roles rather than current scene object names or coordinates. Exact world positions are generated and reviewed only after the Old Market mission window is selected.

## 7. Shared Dense-City Operation-Map Design

### 7.1 Source Reuse

The accepted dense-city authoring source, EntityScene presentation, virtualized proxy database, materials, meshes, and canonical gameplay-building state remain the physical world source. M01 adds a logical Campaign view over that source.

The M01 view must:

- reference the exact accepted dense-city source identity and content hashes;
- materialize the same virtualized presentation rules and fixed capacities;
- add no copied authored city objects;
- add no second permanent render representation;
- keep inactive city districts presentation-only and free of M01 simulation entities;
- preserve Skirmish behavior and identity when M01 is not active.

If the current source-scene identity cannot be safely referenced by a second logical operation-map definition, implementation must add a bounded, validated logical-view-to-source binding. It must not duplicate the EntityScene or rename the accepted production Skirmish identity as an expedient shortcut.

### 7.2 Old Market Mission Window

M01 uses a bounded Old Market/bazaar route containing:

- the visual landmark and approach needed to match FirstLaunch panel `FL-P18`;
- one readable friendly deployment area;
- one safe tutorial move destination with cover context;
- one hostile road patrol route;
- one protected civilian location beyond the patrol;
- one deterministic evacuation destination;
- camera bounds that exclude empty edges and unrelated industrial/base areas;
- a minimap crop aligned exactly with the playable window.

The playable window is a mission rule, not a city deletion. The rest of the dense city remains visible where camera framing needs depth, but is not targetable or simulated by M01.

### 7.3 Camera Continuity

`camera.ch01.m01.planning` is the binding 3D continuation of `FL-P18`. The first live frame must preserve recognizable route direction, bazaar landmarks, time-of-day intent, and civilian-route geography.

The transition is:

```text
FL-P18 final composition
-> covered load/preparation while the panel remains visible
-> matched live planning camera
-> compact interactive brief and Deploy control
-> bounded blend to camera.ch01.m01.battle_start
-> player control
```

No Main Menu frame, unrelated loading scene, or generic Skirmish camera may appear in the transition.

## 8. Mission Runtime Architecture

### 8.1 Authoritative Data Flow

```text
FirstLaunch handoff OR Campaign replay selection
-> immutable MissionLaunchPayload
-> MissionDefinition
-> ScenarioSetup
-> OperationMap logical view + shared dense-city source
-> one authoritative MissionRuntimeState
-> objective/result/progression events
-> read-only UI, ARIA, audio, and narrative projections
```

Both entry paths must use the same payload builder and validation. UI buttons and narrative state machines may request a launch; they may not construct gameplay entities, own mission progress, grant rewards, or write objective state.

### 8.2 Ownership Rules

- `MissionDefinition` owns mission identity, display metadata, sequence IDs, objective/reward references, allowed commands, replay policy, and required feature readiness.
- `ScenarioSetup` owns operation-map selection, required anchors, starting forces, enemy setup, mission restrictions, and deterministic scenario seed/configuration.
- `OperationMapDefinition` owns the logical map view, cameras, minimap, bounds, surfaces, navigation metadata, and stable typed anchors.
- One ECS mission-runtime owner writes mission phase and terminal outcome.
- One objective-runtime owner writes `MatchObjectiveRuntimeElement` and its source version.
- UI, ARIA, audio, and result surfaces consume versioned projections and never become gameplay state owners.
- One Campaign-progress owner persists best stars, first clear, replay availability, first-clear reward state, and debrief completion.
- Narrative owns presentation and publishes typed completion/handoff requests; it does not own Campaign progress.

No static mutable registry, duplicate objective writer, per-frame GameObject search, or second World-independent state store is permitted.

### 8.3 Mission Phases

| Phase | Exit condition |
|---|---|
| Preparing | Required map content, mission configs, and UI are ready; failure remains recoverable. |
| InteractiveBrief | Player receives the compact M01 brief and can Deploy. |
| FindSquad | The command squad is selected or the guidance policy permits progression without a forced prompt. |
| MoveToCover | A valid move order reaches the authored tutorial destination envelope. |
| ConfirmThreat | The hostile patrol is focused/confirmed through typed entity identity. |
| Engage | The player issues an attack and may use Stop/Hold/reposition. |
| SecureCorridor | The patrol is destroyed; civilian evacuation and completion presentation begin. |
| Result | Stars, statistics, and rewards are published exactly once for the result instance. |
| DebriefFirstClear | First clear plays the M01 debrief and reveals the command base. |
| ReturnReplay | Replay returns to Campaign Operations after result acknowledgement. |

The mission runtime may skip tutorial presentation phases when replay tutorial is off, but it must preserve the same spawn, objective, failure, and result rules.

## 9. Gameplay Contract

### 9.1 Starting State

- One controllable JRC rifle squad starts at `anchor.ch01.m01.player_spawn`.
- One small Ash Line infantry patrol starts at `anchor.ch01.m01.patrol_spawn`.
- No active hostile vehicle or air threat is present.
- The patrol follows or holds along the three typed patrol anchors until engagement.
- The starting squad has enough survivability to tolerate tutorial hesitation.
- The patrol cannot reach or threaten the civilian presentation before the mission runtime grants player control and the required context is visible.

### 9.2 Allowed Commands

Allowed: Select, Move, Attack, Stop, Hold.
Unavailable: Build, Produce, transport, support abilities, missiles, economy actions, and advanced roster commands.

Unavailable controls should be absent when possible. If the shared HUD exposes Build, it is disabled with the typed reason `MissionDoesNotAllowBuild`.

### 9.3 Objectives And Failure

- Primary victory: destroy the hostile patrol.
- Narrative completion: secure the corridor so the ambient civilian group can move.
- Failure: the command squad is fully destroyed.
- Civilian presentation cannot be damaged and is not part of the failure calculation.
- Defeat grants no permanent penalty and offers immediate Retry.
- Retry rebuilds the same mission payload and ScenarioSetup.

### 9.4 Stars And Rewards

The existing Chapter 1 contract remains authoritative:

1. Complete the mission.
2. Suffer no own-unit losses.
3. Finish in under four minutes.

The under-four-minute goal is a result metric, not a failure timer. It must not pressure the first-time tutorial UI or hide guidance.

First-clear Commander XP and Credits grant once. Repeat play uses the explicit reduced replay Credit reward. Exact amounts are balance-config-owned. Hard-coded Mission Briefing values are placeholders and must be replaced by the same `RewardConfig` read by result/grant logic. M01 does not grant Intel, Materials, Fuel, Oil, store items, or unrelated unlocks unless a later reviewed economy amendment changes the authority.

## 10. Guidance And FTUE Contract

The FirstLaunch guidance selection is persisted before M01 handoff and read by the mission guidance policy.

| Guidance | M01 behavior |
|---|---|
| Full | Step-by-step welcome, Deploy, objective, select, move, confirm-hostile, attack, completion, and result guidance. Typed highlights are proactive. Safe `Show Me` and bounded `Do It` are available. |
| Contextual | Brief instruction when a new action becomes necessary; progressively stronger hints after inactivity, repeated invalid input, or a failed command. Typed `Show Me` is available; `Do It` appears only where safe and explicitly supported. |
| Minimal | Required objective, hostile/civilian distinction, invalid-command reason, critical warning, failure/retry, and result information only. No proactive takeover. |

All modes retain mandatory objective, safety, accessibility, and failure information. Guidance level never changes mission rules, enemy strength, rewards, or result evaluation.

Required stable steps remain:

- `ftue.m01.welcome`
- `ftue.m01.deploy`
- `ftue.m01.objectives`
- `ftue.m01.select_squad`
- `ftue.m01.move`
- `ftue.m01.attack`
- `ftue.m01.complete`

ARIA operates only through typed UI targets, entity identities, operation-map anchors, and validated command intents. Player input immediately cancels an ARIA preview or bounded action.

## 11. Ambient Civilian Contract

The civilian group exists to make the mission purpose visible without activating the deferred general civilian simulation.

- Civilians/responders occupy the protected authored location beyond the patrol.
- They are non-selectable, non-attackable, excluded from hostile targeting, and do not write mission outcome.
- Their stable presentation may use a bounded deterministic animation or waypoint sequence.
- The evacuation starts only after the patrol objective reaches Complete.
- A mission-owned one-shot event requests the evacuation presentation; presentation does not write objective state back.
- Missing optional civilian presentation must not block mission completion. Dialogue/subtitles still communicate the secured route.
- No casualty count, legitimacy penalty, district metric, or star dependency is introduced.

## 12. Entry, Resume, Replay, And Narrative Flow

### 12.1 First Play

```text
FirstLaunch story and identity/guidance selection
-> first_launch.m01_handoff
-> validated M01 launch payload
-> matched live Old Market planning camera
-> compact seq.ch01.m01.brief / Deploy interaction
-> M01 gameplay
-> Mission Result
-> seq.ch01.m01.debrief
-> command-base reveal with M02 highlighted
```

Skipping FirstLaunch preserves Commander identity defaults, guidance choice/default, required M01 context, and the same typed M01 handoff. Skip cannot route to Main Menu.

### 12.2 Exit Or Interruption

- `HandoffPending` resumes the M01 handoff rather than completing FirstLaunch into Main Menu.
- Exiting an active first play exposes `Resume First Contact` as the dominant command-base action.
- The first implementation may restart the deterministic M01 scenario from its beginning instead of serializing exact mid-combat entity state.
- Restart/resume does not regrant first-clear rewards or replay the full cold open.

### 12.3 Replay

```text
Campaign Operations
-> M01 selected
-> Mission Briefing
-> Replay Tutorial toggle (default off)
-> Deploy
-> same M01 launch payload and ScenarioSetup
-> result
-> Campaign Operations
```

Replay never automatically plays the cold open. The M01 brief/debrief and FirstLaunch story remain separately replayable through Story Archive according to the narrative catalog.

## 13. UI And Feedback Surfaces

### Required Surfaces

- Campaign Operations: data-driven M01 availability, best stars, Continue/Replay state, and launch to briefing.
- Mission Briefing: real mission/scenario/map/objective/reward data, Replay Tutorial toggle on replay, and functional Deploy.
- Match HUD: objective row, selection state, current order, invalid reason, minimap, camera/objective focus, and guidance overlays.
- Mission Result: mission identity, outcome, independent stars, bounded statistics, configured rewards, Retry/Continue/Return behavior.
- Command base after first clear: Continue Campaign points to M02; Campaign, Commander, Settings, and Story Archive are revealed according to the first-player design.

### Required World Feedback

- grounded squad selection marker;
- move destination marker;
- attack marker/hostile target highlight;
- invalid-command marker/toast with typed reason;
- objective focus pulse at `anchor.ch01.m01.patrol_objective`;
- minimap focus/ripple and viewport;
- compact completion presentation that does not obscure the civilian evacuation.

All mobile layouts must preserve critical battlefield visibility at 16:9, 20:9, and representative tablet landscape dimensions.

## 14. Performance And Lifecycle Contract

M01 inherits the accepted dense-city Android contract without relaxation:

- average FPS `>= 54` after warmup on each representative diagnostics-disabled 120-second route;
- 10th-percentile FPS `>= 50`;
- average frame time `<= 18.6 ms`;
- p95 frame time `<= 20 ms`;
- p99 frame time `< 25 ms`;
- CPU-main average `<= 12 ms`, p95 `<= 16 ms`;
- GPU average `<= 16 ms`, p95 `<= 18 ms`;
- steady-state managed allocation exactly `0 B/frame`;
- virtualization overflow and deficit exactly zero;
- no correctness, fatal, stale ownership, or missing-presentation marker;
- final cooled two-minute thermal route passes the same budgets.

Mission-specific constraints:

- objective evaluation is event/change driven and does not scan the full city each frame;
- civilian presentation uses bounded entities and no unbounded navigation query;
- guidance/UI projections rebuild only from relevant semantic versions;
- stable camera motion inside the materialized envelope starts no scene, Addressables, or static-streamer operation;
- camera travel changes no authoritative simulation entity count;
- no synchronous job completion is introduced in steady state;
- retry and replay fully unload M01-owned runtime state and return pooled presentation safely;
- mission configs, payloads, generated map-view data, and evidence bind exact hashes and revision identity.

The canonical accepted Samsung device remains the performance-certification authority. Other authorized Android devices may provide functional/readability evidence but do not replace the canonical performance gate unless a later tracker amendment explicitly changes device authority.

## 15. Validation Strategy

The implementation tracker must decompose and fail closed on at least these gates:

1. Contract and identity reconciliation, including valid IDs and exact source references.
2. Dense-city source reuse with zero cloned authored geometry and zero protected-path mutation.
3. Old Market district, camera, minimap, surface, navigation, and anchor review.
4. `FL-P18` illustrated-to-live camera continuity at required aspects.
5. MissionDefinition, ScenarioSetup, payload, objective, result, reward, and persistence config validation.
6. Single-writer mission/objective/progress ownership and ECS source-version architecture checks.
7. FirstLaunch normal/skip/interrupted handoff into M01 with no Main Menu frame.
8. Full, Contextual, and Minimal first-play guidance behavior.
9. Campaign Operations -> Briefing -> Deploy replay with tutorial off/on.
10. Select, move, invalid move, confirm hostile, attack, Stop/Hold, victory, failure, Retry, and result behavior.
11. Ambient civilian boundedness, non-targetability, one-shot evacuation, and optional-presentation fallback.
12. Independent star evaluation, exactly-once first-clear rewards, explicit repeat reward, and no unintended grants.
13. First-clear debrief/command-base reveal and replay return routing.
14. Save/restart/idempotency and Menu -> M01 -> result -> Menu lifecycle.
15. Compiler zero, focused tests, architecture suite, source-growth gates, deterministic regeneration, protected-path audit, and `git diff --check`.
16. Mobile visual/readability evidence and the complete inherited Android performance/thermal contract.

No checkbox may be accepted from a test that did not run, a missing marker, a stale package, a rejected APK, or evidence from a different scenario/source identity.

## 16. Risks And Required Mitigations

| Risk | Required mitigation |
|---|---|
| Logical M01 map identity cannot reuse the current EntityScene identity | Add one bounded logical-view-to-source binding with exact identity/hash validation; do not clone or rename accepted production content. |
| Bazaar composition does not match `FL-P18` | Select and adjust only the mission camera/window and authorized mission metadata first; visual city changes require separate map ownership and review. |
| Objective state has readers but no writer | Introduce exactly one mission objective owner and versioned projection boundary. |
| Campaign screens contain hard-coded placeholder data | Replace display/availability/reward content with read models from MissionDefinition, progress, and RewardConfig. |
| FirstLaunch currently requests a menu handoff | Replace only the mission-handoff destination with the typed launch request while retaining crash-safe profile state and unrelated menu startup behavior. |
| Guidance mode changes gameplay | Enforce one scenario and outcome contract; guidance changes presentation/help only. |
| Ambient civilians grow into a second simulation | Keep bounded, non-targetable, presentation-only behavior with one-way mission completion event. |
| Mission additions regress dense-city Android performance | Require mission-specific 120-second routes, zero-allocation checks, and the final thermal route before acceptance. |

## 17. HLD Acceptance Conditions

This HLD is ready for implementation-tracker authoring when the user confirms:

- the approved decisions are represented correctly;
- the shared dense-city/Old Market approach is correct;
- the first-play, guidance, replay, civilian, result, and debrief flows match the intended experience;
- the preserved IDs and map-source reuse strategy are acceptable;
- no additional M01 feature or narrative decision is required before task decomposition.

After HLD acceptance, create a separate step-by-step implementation tracker. That tracker must declare dependencies, exact path ownership, evidence, pass markers, rollback, stop/handoff conditions, commit/push boundaries, and honest progress accounting. Implementation must not begin merely because this design document exists.
