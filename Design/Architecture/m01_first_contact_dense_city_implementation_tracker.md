# M01 First Contact Dense-City Implementation Tracker

Date: 2026-08-12
Status: Active; M01DC-004 accepted and M01DC-005 dependency-ready
Progress: 4/43 accepted items (9.3%)
Parent design: `Design/M01_FirstContact_Dense_City_High_Level_Design.md`
Technical architecture: `Design/Architecture/m01_first_contact_dense_city_technical_architecture.md`
Mission: `saga.ch01.m01.first_contact`
Scenario: `scenario.ch01.m01.first_contact`
Logical operation map: `opmap.ch01.district_edge_01`
Physical city source: accepted dense-city presentation currently used by `opmap.skirmish.desert_base_01`

## 1. Purpose

This tracker turns the approved M01 high-level design into dependency-ordered, reviewable implementation slices. It covers the direct FirstLaunch handoff, Campaign launch and replay, shared dense-city Old Market mission view, runtime mission ownership, guidance, objectives, results, rewards, persistence, civilians, agent-operated first-gameplay QA with a mandatory feedback/fix loop, validation, and Android certification.

This document authorizes no implementation until the project owner accepts the tracker. After acceptance, work proceeds in item order. Each accepted item must be evidence-backed, committed, pushed, and leave the repository clean before the next item begins.

The 43 items below are granular acceptance slices, not 43 separate features. Together they deliver one bounded production feature: M01 First Contact.

## 2. Locked Product Contract

The following decisions are final unless the project owner explicitly amends them:

1. The approved comic-style FirstLaunch story remains the opening experience.
2. Completing or skipping FirstLaunch transitions directly into M01. The normal Main Menu must not appear between the story and the live mission.
3. M01 inherits the Full, Contextual, or Minimal guidance level selected during FirstLaunch.
4. Campaign replay defaults to tutorial guidance off and may opt in using the player's current guidance level.
5. Civilians are bounded ambient or scripted-evacuation presentation. They are non-selectable, non-attackable, excluded from enemy targeting, and cannot affect failure, rewards, metrics, or stars.
6. The accepted dense Sahrin city is shared. M01 uses an Old Market mission window and may not clone, rename, or fork the city geometry or its accepted virtualized render database.
7. First-clear completion returns through the M01 debrief to the command base with M02 highlighted. Replay completion returns to Campaign Operations.
8. The only failure condition is destruction of the player's command squad. The primary objective is destruction of the hostile patrol.
9. Stars are: complete the mission, take no squad loss, and finish under four minutes. Four minutes is a result threshold, not a failure timer.
10. Rewards are configuration-owned and idempotent. M01 grants no Intel. Reduced replay Credits must be explicit data.
11. The live M01 patrol is the same three-person Ash Line patrol shown in FirstLaunch FL-P15/FL-P18: Courier (`Chr_Insurgent_Male_03`), Warden (`Chr_Insurgent_Female_01`), and Broker (`Chr_Insurgent_Female_02`). `Chr_Insurgent_Male_05` is reserved exclusively for Nadir Qassem and may not appear or be revealed in M01; the Male 02 heavy gunner is also excluded.
12. M01 is the first actual gameplay experience. Codex or the implementing agent must explicitly play the novice QA role and evaluate comprehension, controls, simplicity, fun, pacing, smoothness, audio, accessibility, recovery, and bugs on a fresh package. This is agent-operated QA, not a human participant study. P0-P2 findings must be fixed and replayed; P3 findings require a bounded fix or explicit project-owner deferral.

## 3. Authority And Dependency Order

This tracker is subordinate to the following accepted authorities:

- `Design/M01_FirstContact_Dense_City_High_Level_Design.md`
- `Design/Architecture/m01_first_contact_dense_city_technical_architecture.md`
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/First_Player_Experience_And_Story_Onboarding_Design.md`
- `Design/Campaign_Mission_High_Level_Design_Catalog.md`
- `Design/Campaign_Narrative_Sequence_And_Comic_Catalog.md`
- `Design/SagaChapters/Saga_Chapter01_First_Response.md`
- `Design/FTUE_And_Command_Assistant_Design.md`
- `Design/Narrative_Presentation_And_Cutscene_Design.md`
- `Design/M01_Metric_Scale_Readability_Contract.md`
- `Design/Architecture/post_hardening_architecture_maturity_tracker.md`
- `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`
- `Design/Architecture/dense_city_virtualized_render_proxy_android_60fps_implementation_tracker.md`
- `Design/Architecture/performance_regression_contract.md`
- `AGENTS.md`

The completed dense-city parent and VRP trackers remain accepted baselines. This work may consume their production outputs but may not reopen, weaken, or silently regenerate them. Architecture maturity work remains governed by its own tracker; M01 must pass its gates but does not activate AM-027 or unrelated deferred architecture phases.

## 4. Identity And Content Contract

The following identities are frozen:

| Concern | Identity |
|---|---|
| Mission | `saga.ch01.m01.first_contact` |
| Scenario | `scenario.ch01.m01.first_contact` |
| Logical map | `opmap.ch01.district_edge_01` |
| Planning camera | `camera.ch01.m01.planning` |
| Battle-start camera | `camera.ch01.m01.battle_start` |
| Minimap projection | `minimap.ch01.m01.projection` |
| Brief sequence | `seq.ch01.m01.brief` |
| Comms sequence | `seq.ch01.m01.comms` |
| Debrief sequence | `seq.ch01.m01.debrief` |
| Primary objective | `obj.ch01.m01.destroy_patrol` |
| Failure objective | `obj.ch01.m01.keep_command_squad_alive` |

Required anchors are:

- `anchor.ch01.m01.player_spawn`
- `anchor.ch01.m01.camera_start`
- `anchor.ch01.m01.move_target`
- `anchor.ch01.m01.patrol_spawn`
- `anchor.ch01.m01.patrol_route_a`
- `anchor.ch01.m01.patrol_route_b`
- `anchor.ch01.m01.patrol_route_c`
- `anchor.ch01.m01.patrol_objective`
- `anchor.ch01.m01.civilian_safe_zone`
- `anchor.ch01.m01.civilian_evacuation`
- `anchor.ch01.m01.minimap_start`

The exact patrol config anchors are:

- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset` - Courier, Ash Line raider/courier threat profile;
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset` - Warden, Ash Line rifle-cell commander threat profile;
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset` - Broker, Ash Line sidearm/logistics operative threat profile.

Their approved visual continuity source is `FACTION-ASH-01_FirstContactPatrol_CandidateB` as recorded by the FirstLaunch source audit and continuity validation. Hostility must remain readable through confirmed weapons, conduct, equipment, and mission context, never through protected local identity. `Prefab_UnitGrid_Chr_Insurgent_Male_05_Config.asset` remains reserved for Nadir Qassem, who receives no portrait, voice, anonymous proxy, or clean reveal in M01. The recovered-order/revoked-credential debrief remains fragmentary.

`opmap.campaign.*` is invalid under the current identity rules and must not be introduced. If the accepted EntityScene's physical identity cannot be referenced by the M01 logical map directly, the only permitted solution is one bounded logical-view-to-source binding with exact map/source identity and hash validation. Duplicating or renaming accepted content is forbidden.

## 5. Ownership And Protected Paths

### 5.1 Expected Owned Path Families

The tracker may add or narrowly modify M01-owned content under these families after the responsible item declares its exact file allowlist:

- `Assets/Game/Configs/Campaign/`
- `Assets/Game/Configs/Missions/Chapter01/`
- `Assets/Game/Configs/Scenarios/Chapter01/`
- `Assets/Game/Configs/OperationMaps/Chapter01/`
- `Assets/Game/Scripts/Runtime/Campaign/`
- `Assets/Game/Scripts/Runtime/Missions/`
- `Assets/Game/Scripts/Runtime/FirstLaunch/` for the typed M01 handoff only
- existing Campaign Operations, Mission Briefing, HUD, Mission Result, narrative, ARIA, and UI shell paths for their bounded read-model or routing changes
- `Assets/Tests/Editor/` and `Assets/Tests/PlayMode/` for M01-focused tests
- `Design/AgentReports/M01FirstContact/` for compact, reviewable evidence manifests and reports
- this tracker, its parent HLD, the linked M01 technical architecture, and directly affected documentation authorities

These are path families, not blanket edit permission. Every implementation item must record the exact files it will touch before editing them.

### 5.2 Read-Only Or Separately Owned Content

Unless a later project-owner decision explicitly changes ownership, these remain read-only:

- the accepted dense-city authoring source, EntityScene presentation, generated static database, virtualized render proxy database, Addressables outputs, and map bake outputs;
- `Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity` and its prototype-only art/tooling;
- `Match.unity`, `Demo.unity`, and `Demo2.unity` except as reference;
- unrelated Skirmish startup, scenario, map, rewards, and UI behavior;
- packages, project settings, render-pipeline settings, quality thresholds, Jenkins configuration, Unity installation paths, and CI infrastructure;
- deferred Phase 6/9 and AM-027 architecture work;
- unrelated gameplay, economy, production, logistics, and feature content.

Mission-specific camera, bounds, anchor, surface, navigation, minimap, and logical-view metadata may be authored as new M01-owned data without mutating the accepted physical city source. If source geometry itself requires a visual change, stop and obtain a separate bounded map-ownership decision.

### 5.3 Single-Owner Runtime Rules

- `MissionDefinition` owns mission identity, display data, sequences, objectives, stars, rewards, allowed commands, replay policy, and readiness requirements.
- `ScenarioSetup` owns map selection, required anchors, friendly and enemy forces, restrictions, deterministic seed/configuration, and ambient presentation request.
- `OperationMapDefinition` owns the logical view, mission bounds, cameras, minimap, surfaces, navigation, anchors, and physical source binding.
- exactly one ECS mission runtime owner writes mission phase and outcome;
- exactly one objective writer writes the objective runtime projection and its source version;
- UI, ARIA, audio, narrative, and result surfaces are readers/projections and may emit typed requests only;
- exactly one Campaign progression owner settles first-clear, best stars, rewards, unlocks, resume state, and replay records;
- no static mutable registry, parallel mission state store, duplicate objective writer, service locator, per-frame hierarchy search, or new broad updating manager is permitted.

## 6. Per-Item Execution Contract

Every item follows this sequence:

1. Re-read `AGENTS.md`, this tracker, the parent HLD, the linked M01 technical architecture, affected authorities, current main/origin, active Unity ownership, and git status.
2. Reconcile any newer accepted item. Start from the actual current head, never a stale hash.
3. Regenerate the item's exact baseline evidence before editing.
4. Record exact allowed files, exact excluded files, expected behavior, validation commands, pass markers, timeout, evidence outputs, and rollback.
5. Diagnose from the failing rule or missing contract. Do not implement speculative adjacent work.
6. Implement only the bounded item while preserving default behavior for existing Skirmish, menu, save, map, and UI paths.
7. Validate through checked repository wrappers. A timeout, project lock, missing pass marker, nonzero exit, incomplete test count, compiler error, or stale package is failure.
8. Run applicable focused behavior, equivalence, lifecycle, allocation, architecture, source-growth, deterministic-regeneration, protected-path, and diff checks.
9. Update this tracker honestly. A checkbox closes only when all listed evidence exists and passes.
10. Commit and push one stable item. The worktree must be clean and local main must equal origin/main before beginning the next item.

Rejected generated outputs, logs, scenes, assets, or implementation changes must be reverted before an item ends. Do not commit ignored build/log artifacts unless an item explicitly requires a compact repository evidence artifact.

## 7. Unity And Device Execution Contract

### 7.1 Unity

- Keep Unity Hub open and signed in.
- Confirm no active Unity process owns this project before starting wrapper validation.
- Resolve the recorded Editor through `Tools/CI/ResolveUnityEditor.ps1`.
- Use `Tools/CI/InvokeUnityExecuteMethodValidation.ps1` for focused execute-method validation.
- Use `Tools/CI/InvokeUnity.ps1` for Unity Test Framework or other wrapper-supported validation.
- Every run requires an explicit log, timeout, expected pass marker, and exact expected test count where applicable.
- Follow the Windows licensing recovery ladder in `AGENTS.md`; never invoke Unity directly or terminate an unrelated process.

### 7.2 Android

Android certification uses only a fresh, checked package built from the exact candidate head. Record commit, package path, SHA-256, package/application ID, version, build timestamp, device serial, model, Android version, install result, route identity, diagnostics state, and evidence paths.

The canonical certification device remains:

- model: Samsung `SM-S918B`;
- serial: `R5CTC1J02VB`.

Other connected devices, including Redmi, may provide functional and visual evidence only unless the performance authority is explicitly amended. Never reinstall a rejected APK or attribute evidence to a package/device/route that did not run.

## 8. Performance And Correctness Gates

All final representative measurements are diagnostics-disabled and use 120-second routes unless the specific gate states otherwise. The inherited final Android contract is:

| Metric | Required gate |
|---|---:|
| Average FPS | >= 54 |
| 10th-percentile FPS | >= 50 |
| Average frame time | <= 18.6 ms |
| Frame-time p95 | <= 20 ms |
| Frame-time p99 | < 25 ms |
| CPU main-thread average | <= 12 ms |
| CPU main-thread p95 | <= 16 ms |
| GPU average | <= 16 ms |
| GPU p95 | <= 18 ms |
| Steady-state allocation | 0 B/frame |
| Proxy/streaming overflow or deficit | 0 |
| Correctness failures | 0 |

The final cooled thermal route is exactly two minutes. Mission additions may not weaken proxy capacities, render thresholds, map correctness, lifecycle, or source-growth gates.

## 9. Progress Legend

- `[ ]` Not started: dependency-ready or waiting on listed dependencies.
- `[~]` In progress: owned changes exist and have not yet been accepted.
- `[x]` Accepted: all acceptance evidence passed, the stable item was committed and pushed, and the worktree was clean.
- `[!]` Blocked: only for a genuinely new authority, credential, physical action, unavailable external resource, or exhausted bounded recovery.

Only `[x]` contributes to progress. Documentation, code existence, partial test success, or local-only commits do not count as accepted.

## 10. Step-By-Step Checklist

### Phase A - Contract, Baseline, And Ownership

- [x] **M01DC-001 - Accept tracker and reconcile the production hold**
  **Depends on:** approved HLD.
  **Deliverable:** record project-owner acceptance of this tracker and the linked technical architecture; reconcile FirstLaunch Phase 10R/Gate 9R evidence without rewriting its historical results; distinguish design approval, production release, and final mission acceptance; leave Gate 10 or equivalent completion open.
  **Acceptance:** the HLD, technical architecture, implementation tracker, and FirstLaunch tracker agree that the visual direction is accepted and M01 production work is authorized only through this tracker; exact type/responsibility/naming/QA/performance boundaries are frozen; no historical evidence is relabeled as a validation that did not run.
  **Evidence:** project-owner messages approved the current FirstLaunch comic/story direction and instructed Codex to continue M01; accepted HLD/technical/tracker design head `edeb9dcf1`; FirstLaunch Gate 9R release recorded without relabeling July technical evidence as a new run; Gate 10 camera/Android/shell prerequisites remain open; authority diff and enclosing clean pushed acceptance commit.

- [x] **M01DC-002 - Capture exact-head baseline inventory**
  **Depends on:** M01DC-001.
  **Deliverable:** machine-readable inventory of current FirstLaunch destinations, Campaign routes, disabled Deploy behavior, existing mission/scenario/map types, objective readers/writers, progress/reward owners, accepted dense-city source GUIDs/hashes, source-growth ceilings, protected paths, and current compiler/test state.
  **Acceptance:** every planned integration point is classified as reuse, narrow extension, new owner, read-only, or absent; the report identifies the current no-objective-writer gap and current menu-handoff behavior from exact source evidence.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_002_baseline_inventory.json`; compact validation manifest `m01dc_002_baseline_validation.md`; exact source-growth `17 / 17` and architecture `23`-suite checked-wrapper passes with zero compiler errors; incidental validator output restored; clean pushed head.

- [x] **M01DC-003 - Freeze item ownership, evidence, and rollback matrix**
  **Depends on:** M01DC-002.
  **Deliverable:** exact file allowlist by remaining item, technical-architecture type/assembly inventory, separately owned/read-only paths, generated-output policy, required validators, pass markers, timeouts, evidence destinations, and per-phase rollback boundaries.
  **Acceptance:** every planned type has one responsibility and assembly, forbidden names/patterns are audited, there is no unbounded path family or overlap with active architecture/map ownership, and no accepted dense-city production asset is planned for modification.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_003_ownership_evidence_rollback_matrix.json`; `m01dc_003_matrix_validation.md`; all `40 / 40` remaining item allowlists present with `164` exact path entries, `61` uniquely assigned types across `11` assemblies, zero forbidden type names, zero wildcard edit paths, zero read-only/protected dense-city overlaps, zero production changes, passing `git diff --check`, and clean pushed head.

### Phase B - Additive Mission Data Contracts

- [x] **M01DC-004 - Add the validated MissionDefinition contract**
  **Depends on:** M01DC-003.
  **Deliverable:** the technical architecture's `Game.Missions.Contracts` assembly and immutable/default-safe `MissionDefinitionConfig`/catalog/validation data for identity, display, sequences, objectives, stars, rewards, allowed commands, replay policy, and readiness requirements.
  **Acceptance:** invalid/duplicate IDs fail closed; existing non-Campaign paths retain defaults; no UI-local mission authority is introduced.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_004_contract_validation.json`; checked-wrapper M01 contract `12 / 12`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; all four production sources remain at or below the `350`-line ceiling; seven incidental validator outputs restored exactly to the entry head; no scene, operation-map asset, or Skirmish-data modification; clean pushed head.

- [ ] **M01DC-005 - Add the immutable MissionLaunchPayload contract**
  **Depends on:** M01DC-004.
  **Deliverable:** the exact `MissionLaunchPayload`, related mission enums/results, and shared `MissionLaunchPayloadFactory` specified by the technical architecture, carrying mission, scenario, logical map, launch origin, guidance mode, replay tutorial choice, first-clear/replay intent, and deterministic correlation/seed data.
  **Acceptance:** FirstLaunch and Campaign can construct semantically equal payloads for equal inputs; payload validation fails closed; no route or UI object becomes the mission state owner.
  **Evidence:** construction/equality/invalid-input tests, clean pushed head.

- [ ] **M01DC-006 - Extend ScenarioSetup with campaign-safe fields**
  **Depends on:** M01DC-004.
  **Deliverable:** additive/default-safe scenario fields for forces, restrictions, deterministic configuration, encounter timing, required anchors, and optional ambient presentation.
  **Acceptance:** the current Skirmish scenario serializes and behaves identically with defaults; missing required campaign data fails before runtime launch.
  **Evidence:** serialization compatibility, Skirmish regression, focused validation, clean pushed head.

- [ ] **M01DC-007 - Add objective, star, and reward definitions**
  **Depends on:** M01DC-004.
  **Deliverable:** typed objective, failure, star, first-clear reward, and replay reward data with explicit no-Intel M01 configuration support.
  **Acceptance:** the under-four-minute rule is a star threshold, not failure; reward validation rejects duplicate or ambiguous settlement; display data is projected from definitions.
  **Evidence:** focused rule tests and placeholder-reward detection, clean pushed head.

- [ ] **M01DC-008 - Add Campaign mission progress schema and migration**
  **Depends on:** M01DC-004, M01DC-007.
  **Deliverable:** versioned `CampaignMissionProgressSaveData`, sole-owner `CampaignMissionProgressStore`, and atomic save path for availability, first clear, best stars/time, first-clear settlement, replay records, resume state, and next-mission reveal.
  **Acceptance:** default/new/older profile migration is deterministic; repeated settlement is idempotent; interrupted writes preserve the prior valid profile; corrupt or future versions fail safely without inventing progress; settings and quick-game data regress zero.
  **Evidence:** migration, idempotency, corruption, and restart tests; clean pushed head.

- [ ] **M01DC-009 - Author and register canonical M01 data**
  **Depends on:** M01DC-005 through M01DC-008.
  **Deliverable:** canonical `MissionDefinition`, `ScenarioSetup`, objectives, stars, rewards, narrative IDs, exact Courier/Warden/Broker patrol config references, and Campaign catalog registration for the frozen identities.
  **Acceptance:** deterministic regeneration is byte/hash stable; all cross-references resolve; the patrol contains those three exact FirstLaunch continuity identities and excludes Qassem/Male 05 and the Male 02 heavy gunner; the old hard-coded `+260/+1,200/+1 Intel` briefing placeholders are not authoritative and M01 grants no Intel.
  **Evidence:** registration/reference validator, two-pass regeneration hashes, clean pushed head.

### Phase C - Shared Dense-City Old Market View

- [ ] **M01DC-010 - Prove and implement physical-source reuse**
  **Depends on:** M01DC-003, M01DC-009.
  **Deliverable:** exact proof that M01's logical map can reference the accepted dense-city physical source; if required, one bounded logical-view-to-source binding with identity/hash validation.
  **Acceptance:** zero cloned city scenes, geometry, generated databases, Addressables content, or `.meta` identities; stale/mismatched source identity or hash fails closed.
  **Evidence:** GUID/hash/source manifest, duplicate-content scan, protected-path diff, clean pushed head.

- [ ] **M01DC-011 - Select the Old Market mission window**
  **Depends on:** M01DC-010.
  **Deliverable:** reviewed logical bounds, playable surfaces, navigation scope, off-window exclusion, landmark orientation, and contact corridor using accepted city data.
  **Acceptance:** one player squad and one patrol fit readable M01 scale; reachable space has no road-over-water-without-bridge defect; blue Autobahn/canal isolation remains separately classified and is not conflated with this mission window.
  **Evidence:** top-down annotated capture, surface/nav report, exact bounds, review record, clean pushed head.

- [ ] **M01DC-012 - Author planning, battle-start, and minimap projections**
  **Depends on:** M01DC-011.
  **Deliverable:** frozen camera and minimap identities with framing, clamp, zoom, projection, safe-area, and transition metadata.
  **Acceptance:** the mission starts readable at supported aspect ratios; cameras remain inside valid bounds; minimap-to-world projection is deterministic and exact.
  **Evidence:** camera/minimap validator and captures at 16:9, 20:9, and tablet landscape; clean pushed head.

- [ ] **M01DC-013 - Author and validate all required anchors**
  **Depends on:** M01DC-011.
  **Deliverable:** all eleven frozen M01 anchors with surface, clearance, orientation, reachability, and uniqueness validation.
  **Acceptance:** every required anchor resolves exactly once; units do not overlap geometry or each other; patrol timing cannot reach civilians before player control/context.
  **Evidence:** anchor manifest, occupancy/reachability/timing validation, clean pushed head.

- [ ] **M01DC-014 - Publish the logical M01 OperationMapDefinition**
  **Depends on:** M01DC-010 through M01DC-013.
  **Deliverable:** `opmap.ch01.district_edge_01` definition binding the mission view, shared source, bounds, surfaces, navigation, cameras, minimap, and anchors.
  **Acceptance:** operation-map validators pass; the physical-source hash matches the accepted source; the existing Skirmish map remains byte/behavior equivalent.
  **Evidence:** focused map validation, source-equivalence report, two-pass deterministic hashes, clean pushed head.

- [ ] **M01DC-015 - Accept FL-P18-to-live camera continuity**
  **Depends on:** M01DC-012, M01DC-014.
  **Deliverable:** covered transition timing and matched final comic/live planning-camera composition for normal and reduced-motion settings.
  **Acceptance:** no Main Menu frame, unrelated scene flash, invalid camera, unlit/streaming-in city, or uncontrolled input is visible; the transition is acceptable at 16:9, 20:9, and tablet landscape.
  **Evidence:** frame sequence/contact sheet and reviewer record, clean pushed head.

- [ ] **M01DC-016 - Close dense-city reuse and map-regression gate**
  **Depends on:** M01DC-014, M01DC-015.
  **Deliverable:** consolidated proof that M01 adds only a logical view and mission metadata.
  **Acceptance:** accepted VRP database/capacities/hashes and protected physical content are unchanged; no duplicate permanent render representation exists; dense-city parity and map correctness suites pass.
  **Evidence:** protected-path audit, database/hash comparison, dense-city focused regression, clean pushed head.

### Phase D - Authoritative Mission Runtime

- [ ] **M01DC-017 - Add the single mission runtime owner**
  **Depends on:** M01DC-005, M01DC-009, M01DC-016.
  **Deliverable:** one ECS-owned mission phase/outcome state machine with source version and the approved phases from Preparing through ReturnReplay.
  **Acceptance:** exactly one writer is found; invalid transitions fail closed; UI/narrative/ARIA/audio remain readers; no static mutable state or parallel store appears.
  **Evidence:** writer inventory, transition tests, lifecycle tests, architecture/source-growth check, clean pushed head.

- [ ] **M01DC-018 - Integrate one launch/bootstrap path**
  **Depends on:** M01DC-017.
  **Deliverable:** validated payload resolution, mission/scenario/map loading, readiness gating, and controlled entry used by both FirstLaunch and Campaign origins.
  **Acceptance:** equal payloads produce equal runtime setup; stale/missing data fails to a bounded recovery surface; no Campaign path reuses an inappropriate Skirmish shortcut.
  **Evidence:** launch equivalence, failure-path, readiness, unload/reload tests; clean pushed head.

- [ ] **M01DC-019 - Spawn deterministic friendly and hostile forces**
  **Depends on:** M01DC-013, M01DC-018.
  **Deliverable:** one player rifle squad; the exact FirstLaunch Courier/Warden/Broker Ash Line patrol; route timing, hostility, command restrictions, and deterministic correlation to scenario data.
  **Acceptance:** the three live hostile entities resolve to `Chr_Insurgent_Male_03`, `Chr_Insurgent_Female_01`, and `Chr_Insurgent_Female_02` with continuity-correct roles/equipment; Qassem/Male 05 and the Male 02 heavy gunner are absent; no active vehicles/air/build/economy; select/move/attack/stop/hold remain available; spawn identity/count/position/route are deterministic and anchor-safe.
  **Evidence:** scenario replay hashes, command-availability checks, spawn/route tests, clean pushed head.

- [ ] **M01DC-020 - Add the single objective writer and projections**
  **Depends on:** M01DC-017, M01DC-019.
  **Deliverable:** exactly one objective system writes `MatchObjectiveRuntimeElement` and source version from mission state; HUD, ARIA, and result read the projection.
  **Acceptance:** destroy-patrol progress/completion and command-squad failure are deterministic; duplicate/stale writes are rejected; no reader derives a competing objective truth.
  **Evidence:** writer inventory, source-version tests, HUD/ARIA/result projection tests, clean pushed head.

- [ ] **M01DC-021 - Implement failure, retry, unload, and lifecycle cleanup**
  **Depends on:** M01DC-018 through M01DC-020.
  **Deliverable:** command-squad failure, deterministic retry from the same payload/scenario, explicit exit/resume policy, and complete native/pool/entity/event cleanup.
  **Acceptance:** retry has no penalty and cannot duplicate rewards/events/entities; repeated launch/retry/exit cycles have no stale state, native leak, pool growth, or disposed access.
  **Evidence:** repeated lifecycle/transition/pool/native/allocation tests, clean pushed head.

- [ ] **M01DC-022 - Add bounded ambient civilians**
  **Depends on:** M01DC-019, M01DC-021.
  **Deliverable:** capped ambient/scripted evacuation presentation driven one-way by mission completion and optional fallback when presentation capacity is unavailable.
  **Acceptance:** civilians are non-selectable, non-attackable, never targeted, never authoritative, never counted in objectives/stars/rewards, deterministically bounded, and fully cleaned up.
  **Evidence:** target/filter/bounds/fallback/lifecycle/allocation tests, clean pushed head.

- [ ] **M01DC-023 - Evaluate stars and construct the mission result**
  **Depends on:** M01DC-020, M01DC-021.
  **Deliverable:** deterministic completion time, squad-loss, primary/failure, star, and result projection from authoritative runtime facts.
  **Acceptance:** all star combinations are tested; four minutes never fails the mission; result UI cannot alter the result; retries create a new bounded attempt without corrupting best data.
  **Evidence:** boundary-value and projection tests, clean pushed head.

- [ ] **M01DC-024 - Settle progress, rewards, resume, and replay exactly once**
  **Depends on:** M01DC-008, M01DC-023.
  **Deliverable:** one Campaign progression writer for first clear, reduced replay reward, best stars/time, M02 reveal, resume/restart, and replay record.
  **Acceptance:** repeated messages, restarts, crashes, retries, and replay cannot double-grant; first-clear and replay return destinations differ as designed; M01 never grants Intel.
  **Evidence:** idempotency/crash/restart/replay/migration tests, clean pushed head.

### Phase E - Guidance, Narrative, And FirstLaunch

- [ ] **M01DC-025 - Implement Full guidance**
  **Depends on:** M01DC-020.
  **Deliverable:** proactive Find Squad, Move to Cover, Confirm Threat, Engage, and Secure Corridor guidance plus typed Show Me/Do It requests within existing ARIA authority.
  **Acceptance:** guidance reads mission/objective projections, never writes outcome/gameplay truth, respects cooldown/acknowledgement/accessibility, and produces no per-frame allocation.
  **Evidence:** phase/request/cooldown/accessibility/allocation tests, clean pushed head.

- [ ] **M01DC-026 - Implement Contextual and Minimal guidance**
  **Depends on:** M01DC-025.
  **Deliverable:** progressive contextual hints and mandatory-information-only minimal guidance using the same scenario and objective state.
  **Acceptance:** all three modes produce identical spawns, patrol behavior, commands, objectives, stars, rewards, and results for equal player actions; only presentation/help differs.
  **Evidence:** cross-mode equivalence matrix and focused tests, clean pushed head.

- [ ] **M01DC-027 - Replace the FirstLaunch menu handoff with typed M01 launch**
  **Depends on:** M01DC-015, M01DC-018, M01DC-026.
  **Deliverable:** normal-complete, skip, and interrupted `HandoffPending` flows persist identity/guidance then request the same validated M01 payload without entering Main Menu.
  **Acceptance:** all paths reach M01 once; resume cannot lose or duplicate the launch; unrelated returning-player/menu startup is unchanged; failed mission readiness returns to an explicit recoverable state rather than falsely completing FirstLaunch.
  **Evidence:** normal/skip/interruption/crash/restart/no-menu-frame tests and captures, clean pushed head.

- [ ] **M01DC-028 - Integrate brief, comms, debrief, and command-base reveal**
  **Depends on:** M01DC-020, M01DC-024, M01DC-027.
  **Deliverable:** compact interactive brief, in-mission comms, first-clear debrief, Story Archive references, and command-base arrival with M02 highlighted.
  **Acceptance:** narrative emits typed requests only; the brief continues the exact FirstLaunch Courier/Warden/Broker threat; the debrief reveals coordination and a revoked credential trace without revealing or proxying Qassem; skip/reduced-motion/subtitles work; first clear cannot return to Main Menu/Campaign by mistake; replay does not automatically replay the cold open.
  **Evidence:** sequence/route/accessibility/interruption tests and representative captures, clean pushed head.

### Phase F - Campaign Operations, Briefing, HUD, And Result UI

- [ ] **M01DC-029 - Make Campaign Operations data-driven for M01**
  **Depends on:** M01DC-008, M01DC-009, M01DC-024.
  **Deliverable:** M01 availability, continue/replay state, best stars/time, next-mission reveal, and selected mission read model from Campaign authority.
  **Acceptance:** UI stores no parallel progress; new/first-clear/replay profiles render correctly; locked/unavailable actions remain fail-closed.
  **Evidence:** read-model/route/profile-state tests and supported-aspect captures, clean pushed head.

- [ ] **M01DC-030 - Activate Mission Briefing Deploy and replay tutorial choice**
  **Depends on:** M01DC-005, M01DC-009, M01DC-029.
  **Deliverable:** definition-driven briefing, accurate objectives/conditions/enemy/rewards, functional Deploy, and replay-only tutorial toggle defaulting off and using current guidance when enabled.
  **Acceptance:** no hard-coded placeholder reward authority remains; first launch bypasses Campaign briefing but may show the compact live brief; Campaign launch emits the validated payload once.
  **Evidence:** briefing projection/payload/toggle/duplicate-click tests and captures, clean pushed head.

- [ ] **M01DC-031 - Complete HUD and result routes**
  **Depends on:** M01DC-020, M01DC-023, M01DC-024, M01DC-030.
  **Deliverable:** objective HUD, guidance presentation, mission result, reward settlement display, retry/continue, first-clear debrief route, and replay Campaign-return route.
  **Acceptance:** all surfaces consume authoritative projections; duplicate input cannot duplicate transitions/rewards; safe area, controller/touch focus, text expansion, subtitles, and reduced motion are valid.
  **Evidence:** projection/route/input/accessibility tests and supported-aspect captures, clean pushed head.

### Phase G - Consolidated Validation And Production Acceptance

- [ ] **M01DC-032 - Pass compiler, architecture, source-growth, and deterministic-output gates**
  **Depends on:** M01DC-004 through M01DC-031.
  **Deliverable:** exact current architecture entrypoint, mission/objective writer audits, source-growth suite, compiler-zero report, two-pass regeneration hashes, protected-path audit, and focused diff review.
  **Acceptance:** every expected suite/test count and pass marker is present; no exception is added/enlarged merely to pass; zero genuine/unclassified architecture debt is introduced.
  **Evidence:** consolidated validation manifest with commands, logs, counts, markers, hashes, and clean pushed head.

- [ ] **M01DC-033 - Pass deterministic gameplay and outcome validation**
  **Depends on:** M01DC-019 through M01DC-024, M01DC-032.
  **Deliverable:** headless/focused scenarios for command availability, patrol behavior, victory, failure, retry, star boundaries, reward idempotency, civilians, and equal-input determinism.
  **Acceptance:** all expected cases pass with identical repeated hashes/state transitions and zero correctness failure.
  **Evidence:** exact case matrix, focused wrapper log, result manifest, clean pushed head.

- [ ] **M01DC-034 - Pass entry, guidance, replay, persistence, and lifecycle validation**
  **Depends on:** M01DC-025 through M01DC-031, M01DC-032.
  **Deliverable:** normal/skip/interrupted FirstLaunch, all guidance modes, Campaign first launch/replay tutorial off/on, first-clear/replay return, save migration, crash/restart, retry/exit, unload/reload, native/pool/event/allocation coverage.
  **Acceptance:** every transition occurs exactly once, no Main Menu appears in first-play handoff, no stale state/leak/double grant remains, and existing Skirmish/menu routes regress zero.
  **Evidence:** transition/equivalence/lifecycle manifest and checked wrapper logs, clean pushed head.

- [ ] **M01DC-035 - Pass visual, camera, minimap, HUD, and accessibility review**
  **Depends on:** M01DC-015, M01DC-031, M01DC-034.
  **Deliverable:** reviewed captures/video for FL-P18 handoff, planning view, battle start, objectives, selected units, move/attack markers, the exact Courier/Warden/Broker patrol, civilians, result, debrief, Campaign replay, safe areas, text expansion, subtitles, and reduced motion.
  **Acceptance:** the live three-person patrol is visually continuous with the approved FirstLaunch patrol and remains distinct from civilians/JRC without profiling local identity; Qassem/Male 05 and the heavy gunner do not appear; readable at 16:9, 20:9, and tablet landscape; no refinery/proxy overlap artifact, hidden city, incorrect brown fallback, invalid road/water crossing, clipping, unreadable scale, or UI overlap in the M01 window.
  **Evidence:** capture manifest/contact sheet and project-owner visual review record, clean pushed head.

- [ ] **M01DC-036 - Pass Editor performance, allocation, and bounded-work gates**
  **Depends on:** M01DC-032 through M01DC-035.
  **Deliverable:** representative live mission instrumentation for allocations, structural changes, objective/guidance cadence, civilians, loading, unload/reload, proxy capacity, and bounded scans.
  **Acceptance:** steady-state 0 B/frame, no unbounded query/hierarchy search, no proxy overflow/deficit, no lifecycle growth, and no regression relative to the accepted dense-city baseline.
  **Evidence:** instrumentation report and focused performance logs, clean pushed head.

- [ ] **M01DC-037 - Build, hash, and install a fresh checked Android package**
  **Depends on:** M01DC-036.
  **Deliverable:** exact-head gameplay-QA candidate Android package built through checked wrappers with provenance, SHA-256, package metadata, installation, launch, and device identity.
  **Acceptance:** the package is fresh, validators pass, install/launch succeeds on the exact authorized device, and no rejected/stale APK is installed or reused. Redmi evidence, if collected, remains supplemental.
  **Evidence:** package provenance/install manifest and checked build logs, clean pushed head.

- [ ] **M01DC-038 - Run agent-operated first-gameplay QA on the candidate**
  **Depends on:** M01DC-037.
  **Deliverable:** Codex/agent-operated real-input sessions and the technical architecture's scored/finding-based QA report: cold novice with Full guidance, low-help Contextual with delayed/wrong actions and recovery, and Minimal Campaign replay with replay tutorial default off; include touch, camera, core commands, objective comprehension, simplicity, fun/pacing, smoothness, sound, guidance, UI, accessibility, recovery, bugs, and first confusion points.
  **Acceptance:** all three required agent-operated sessions execute, including at least one complete fresh-profile story-to-command-base session and one Campaign replay run on a supported Android touch device with audio enabled; evidence names the agent/task, exact package/head/device, and `operatorKind=Agent`; every finding has severity, reproduction, evidence, owner, response, and disposition; no evidence claims a human participant or represents automated scripts/captures as player-role QA.
  **Evidence:** `Design/AgentReports/M01FirstContact/qa/m01dc_038_first_gameplay_qa.md`, timestamped video/screenshots/audio notes/input log, scorecard, findings ledger, clean pushed head.

- [ ] **M01DC-039 - Act on QA feedback and prove the closed loop**
  **Depends on:** M01DC-038.
  **Deliverable:** triage every QA finding, implement all bounded code/content/audio/UI responses covered by the accepted design, rerun affected automated/visual/lifecycle tests and agent-operated reproductions, and build/install a fresh QA-resolved package if any production input changed.
  **Acceptance:** no P0/P1/P2 finding remains; every P3 is fixed or has explicit project-owner deferral/target; no finding is closed by prose alone; comprehension/control/simplicity/smoothness/audio recommendations meet the technical QA gate; fixed issues are replayed on the named fresh package with regressions zero.
  **Evidence:** before/after finding matrix, commits, exact validation/replay logs, fresh package provenance when changed, clean pushed head.

- [ ] **M01DC-040 - Pass diagnostics-disabled 120-second representative Android routes**
  **Depends on:** M01DC-039.
  **Deliverable:** 120-second routes on the QA-resolved package for FirstLaunch-to-M01 entry, active Old Market combat/guidance, and Campaign replay with correctness, CPU/GPU/frame/allocation/proxy evidence.
  **Acceptance:** every inherited FPS/frame-time/CPU/GPU/allocation/proxy/correctness gate passes on the canonical device; route identity and diagnostics-disabled state are proven.
  **Evidence:** raw captures, summaries, provenance links, and checked analyzer pass markers; clean pushed head.

- [ ] **M01DC-041 - Pass final cooled two-minute thermal and device lifecycle route**
  **Depends on:** M01DC-040.
  **Deliverable:** one final cooled exactly two-minute M01 thermal run plus background/foreground, interruption, retry, return, and relaunch checks on the same accepted package/device.
  **Acceptance:** the full inherited performance contract passes; no thermal drift, crash, ANR, missing content, state loss, duplicate reward, or lifecycle leak occurs.
  **Evidence:** thermal/device lifecycle manifest, raw data, analyzer output, and pass markers; clean pushed head.

- [ ] **M01DC-042 - Sign off agent-operated gameplay on the certified package**
  **Depends on:** M01DC-039 through M01DC-041.
  **Deliverable:** agent-operated cold novice Full-guidance story-to-command-base session and Minimal Campaign replay smoke on the exact certified package/head, focused on the fixed findings and final sound/input/pacing/comprehension quality.
  **Acceptance:** no fixed finding regresses; no new P0-P2 appears; all reviewed dimensions remain at or above the technical QA gate; package/head/device identity exactly matches certification evidence; any new finding reopens M01DC-039 and dependent package/performance items.
  **Evidence:** final QA scorecard, closed finding ledger, timestamped captures, package/head/device correlation, clean pushed head.

- [ ] **M01DC-043 - Final acceptance and feature handoff**
  **Depends on:** M01DC-001 through M01DC-042.
  **Deliverable:** reconcile all 43 items, update affected parent/catalog/FirstLaunch/M01 authorities, publish exact final heads and evidence index, mark production M01 accepted, and identify M02 as next without starting it.
  **Acceptance:** 43/43 items are honestly accepted; all trackers agree; main equals origin/main; repository is clean; no protected content changed without authority; no temporary implementation/evidence output remains uncommitted.
  **Evidence:** final acceptance report, commit/push verification, clean status, project-owner handoff.

## 11. Phase Exit Gates

| Phase | Exit condition |
|---|---|
| A | Authority, exact-head baseline, ownership, rollback, and evidence plan are accepted. |
| B | Mission, payload, scenario, objective/star/reward, persistence, and M01 canonical data validate with backward-safe defaults. |
| C | The Old Market logical view, source reuse, cameras, minimap, anchors, continuity, and dense-city regression gates pass without physical-content duplication. |
| D | One authoritative mission/objective/progress pipeline delivers deterministic gameplay, result, retry, civilians, and settlement. |
| E | Full/Contextual/Minimal guidance, FirstLaunch direct handoff, narrative, debrief, and base reveal pass. |
| F | Campaign, briefing, HUD, result, and replay routes are data-driven and accessible. |
| G | Compiler, architecture, source growth, determinism, lifecycle, visual, actual first-gameplay QA and feedback closure, Android, performance, thermal, documentation, and clean-repository gates pass. |

No later phase may be accepted to compensate for a failed earlier phase. Later work may begin only when every listed dependency is accepted.

## 12. Rollback Contract

- Roll back only the current bounded item's owned changes when its acceptance fails.
- Do not reset, amend, squash, or rewrite accepted immutable commits.
- Do not discard unrelated worktree changes. If unexpected changes appear, classify ownership before proceeding.
- New serialized fields must be additive/default-safe until their consumers are accepted. A rollback must preserve compatibility with already accepted assets.
- A failed logical-view approach returns to the last accepted definition and evidence; it never falls back to cloning the city.
- A failed performance candidate is rejected as a package/head. Do not reinstall it or attach its evidence to a later head.
- A failed Android run does not justify weaker gates, larger proxy capacities, altered thresholds, or a different canonical device.

## 13. Stop And Handoff Conditions

Continue autonomously through dependency-ready items after tracker acceptance. Stop and ask the project owner only when completion needs:

- a genuinely new product decision not fixed in the HLD;
- authority to modify accepted dense-city physical content or another protected owner;
- new credentials or unavailable external resources;
- a physical device action that cannot be performed remotely;
- an architecture exception, threshold change, device substitution, reward/economy decision, or scope expansion.

Unity licensing, wrapper, device connection, disk space, or bounded tool interruption is not by itself a passive blocker. Follow `AGENTS.md` recovery rules and continue when safe. Never bypass wrappers, invoke Unity directly, terminate unrelated processes, weaken validation, or claim work that did not run.

## 14. Decision And Implementation Log

| Date | Item | Decision/evidence | Commit | Status |
|---|---|---|---|---|
| 2026-08-12 | HLD | Project owner approved direct FirstLaunch-to-M01, inherited guidance, recommended replay tutorial policy, recommended civilian boundary, shared dense-city Old Market direction, and tracker authoring. | `aa04a6e85` | HLD approved; tracker draft created |
| 2026-08-12 | M01DC-001 | Project owner accepted the current FirstLaunch comic/story presentation, linked M01 technical architecture, 43-item tracker, agent-operated QA amendment, and autonomous continuation. Gate 9R is released; Gate 10 remains honestly open. | enclosing M01DC-001 acceptance commit | Accepted |
| 2026-08-12 | M01DC-002 | Exact-head inventory classifies every integration seam, confirms the current FirstLaunch `EnterMenu` handoff and absent objective/progression/reward owners, hash-binds the accepted dense-city source, freezes protected/source-growth boundaries, and passes source growth `17 / 17` plus architecture `23` suites with zero compiler errors. | enclosing M01DC-002 acceptance commit | Accepted |
| 2026-08-12 | M01DC-003 | Exact-path ownership, evidence, validation, generated-output, and rollback authority is frozen for M01DC-004..043. Static audit covers `40 / 40` item allowlists, `164` exact path entries, and `61` unique types in `11` assemblies with no forbidden names, wildcard edit paths, read-only overlap, or accepted dense-city physical-source edits. | enclosing M01DC-003 acceptance commit | Accepted |
| 2026-08-12 | M01DC-004 | Added dependency-root `Game.Missions.Contracts`, default-safe mission definition/catalog data, and fail-closed identity/reference/objective/star/reward/command/readiness validation. M01 rejects Intel and duplicate/invalid configuration; checked M01 `12 / 12`, source-growth `17 / 17`, and architecture `23`-suite gates pass with compiler zero. | enclosing M01DC-004 acceptance commit | Accepted |

Implementation entries are appended only after an item is accepted and pushed. The final row must record M01DC-043, the final main/origin head, 43/43 progress, Android package/device identities, agent-QA/finding closure, validation summary, and clean worktree state.

## 15. Tracker Acceptance

M01DC-001 acceptance on 2026-08-12 confirmed that this tracker correctly captures:

- the 43-item dependency order and scope;
- direct FirstLaunch entry with no Main Menu;
- Full/Contextual/Minimal guidance and replay tutorial behavior;
- shared dense-city physical-source reuse and Old Market logical view;
- the single-owner mission, objective, and progression architecture;
- first-clear/replay/result/reward/persistence behavior;
- civilian limits;
- the exact FirstLaunch Courier/Warden/Broker patrol continuity and the prohibition on revealing Qassem in M01;
- the required agent-operated first-gameplay QA sessions, scored feedback, mandatory code/content response, replay, and final certified-package sign-off without claiming a human participant;
- Android performance and two-minute thermal gates;
- clean commit-and-push boundaries and stop conditions.

Tracker acceptance authorizes implementation only within these boundaries. It does not authorize M02, AM-027, unrelated feature work, dense-city source redesign, or any weakening of accepted architecture or performance contracts.
