# M01 First Contact Dense-City Implementation Tracker

Date: 2026-08-12
Status: Active; M01DC-028 accepted and M01DC-029 dependency-ready
Progress: 28/43 accepted items (65.1%)
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
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_003_ownership_evidence_rollback_matrix.json`; `m01dc_003_matrix_validation.md`; all `40 / 40` remaining item allowlists present with `173` exact path entries, `63` uniquely assigned types across `11` assemblies, zero forbidden type names, zero wildcard edit paths, zero read-only/protected dense-city overlaps, zero production changes, passing `git diff --check`, and clean pushed head.

### Phase B - Additive Mission Data Contracts

- [x] **M01DC-004 - Add the validated MissionDefinition contract**
  **Depends on:** M01DC-003.
  **Deliverable:** the technical architecture's `Game.Missions.Contracts` assembly and immutable/default-safe `MissionDefinitionConfig`/catalog/validation data for identity, display, sequences, objectives, stars, rewards, allowed commands, replay policy, and readiness requirements.
  **Acceptance:** invalid/duplicate IDs fail closed; existing non-Campaign paths retain defaults; no UI-local mission authority is introduced.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_004_contract_validation.json`; checked-wrapper M01 contract `12 / 12`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; all four production sources remain at or below the `350`-line ceiling; seven incidental validator outputs restored exactly to the entry head; no scene, operation-map asset, or Skirmish-data modification; clean pushed head.

- [x] **M01DC-005 - Add the immutable MissionLaunchPayload contract**
  **Depends on:** M01DC-004.
  **Deliverable:** the exact `MissionLaunchPayload`, related mission enums/results, and shared `MissionLaunchPayloadFactory` specified by the technical architecture, carrying mission, scenario, logical map, launch origin, guidance mode, replay tutorial choice, first-clear/replay intent, and deterministic correlation/seed data.
  **Acceptance:** FirstLaunch and Campaign can construct semantically equal payloads for equal inputs; payload validation fails closed; no route or UI object becomes the mission state owner.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_005_launch_payload_validation.json`; checked-wrapper launch-payload `9 / 9`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; pure `69`-line shared factory, equal-input/value-equality and fail-closed invalid-input coverage, retry identity/session/seed preservation, no UI/route dependency; incidental validator outputs recoverably backed up then restored; clean pushed head.

- [x] **M01DC-006 - Extend ScenarioSetup with campaign-safe fields**
  **Depends on:** M01DC-004.
  **Deliverable:** additive/default-safe scenario fields for forces, restrictions, deterministic configuration, encounter timing, required anchors, and optional ambient presentation.
  **Acceptance:** the current Skirmish scenario serializes and behaves identically with defaults; missing required campaign data fails before runtime launch.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_006_scenario_compatibility.json`; checked-wrapper compatibility `8 / 8`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; legacy Skirmish JSON/defaults remain valid without Campaign data; Campaign seed, force groups, unit identity, patrol references, restrictions, timing, and optional ambient data fail closed before launch; `349` production lines; clean pushed head.

- [x] **M01DC-007 - Add objective, star, and reward definitions**
  **Depends on:** M01DC-004.
  **Deliverable:** typed objective, failure, star, first-clear reward, and replay reward data with explicit no-Intel M01 configuration support.
  **Acceptance:** the under-four-minute rule is a star threshold, not failure; reward validation rejects duplicate or ambiguous settlement; display data is projected from definitions.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_007_rules_validation.json`; checked-wrapper mission-rule `14 / 14`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; four-minute target is an independent `240000` ms star threshold and not a failure rule; typed objective/failure, star, first-clear/replay reward, settlement-identity, definition-projected display, placeholder, duplicate, and no-Intel M01 coverage; seven incidental validator outputs restored to the entry head; clean pushed head.

- [x] **M01DC-008 - Add Campaign mission progress schema and migration**
  **Depends on:** M01DC-004, M01DC-007.
  **Deliverable:** versioned `CampaignMissionProgressSaveData`, sole-owner `CampaignMissionProgressStore`, and atomic save path for availability, first clear, best stars/time, first-clear settlement, replay records, resume state, and next-mission reveal.
  **Acceptance:** default/new/older profile migration is deterministic; repeated settlement is idempotent; interrupted writes preserve the prior valid profile; corrupt or future versions fail safely without inventing progress; settings and quick-game data regress zero.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_008_progress_migration.json`; checked-wrapper progress/migration `15 / 15`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; additive profile/entry schemas, deterministic mission-ID ordering, sole-owner store mutation, idempotent settlement tokens, first-clear/replay/best/resume/reveal state, corrupt/future-version safety, restart, settings/quick-game regression, and interrupted atomic-replace preservation; seven incidental validator outputs restored to the entry head; clean pushed head.

- [x] **M01DC-009 - Author and register canonical M01 data**
  **Depends on:** M01DC-005 through M01DC-008.
  **Deliverable:** canonical `MissionDefinition`, `ScenarioSetup`, objectives, stars, rewards, narrative IDs, exact Courier/Warden/Broker patrol config references, and Campaign catalog registration for the frozen identities.
  **Acceptance:** deterministic regeneration is byte/hash stable; all cross-references resolve; the patrol contains those three exact FirstLaunch continuity identities and excludes Qassem/Male 05 and the Male 02 heavy gunner; the old hard-coded `+260/+1,200/+1 Intel` briefing placeholders are not authoritative and M01 grants no Intel.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_009_canonical_data.json`; checked-wrapper canonical-data `13 / 13`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; two-pass regeneration byte/hash stable for all three canonical assets; frozen mission/scenario/map/objective/narrative identities and all cross-references resolve; exact Courier/Warden/Broker patrol and four approved friendly identities are GUID/path bound; Qassem/Male 05 and the Male 02 heavy gunner are absent; first-clear `260` Commander XP plus `1200` Credits and replay `250` Credits grant zero Intel; seven incidental validator outputs restored; clean pushed head.

### Phase C - Shared Dense-City Old Market View

- [x] **M01DC-010 - Prove and implement physical-source reuse**
  **Depends on:** M01DC-003, M01DC-009.
  **Deliverable:** exact proof that M01's logical map can reference the accepted dense-city physical source; if required, one bounded logical-view-to-source binding with identity/hash validation.
  **Acceptance:** zero cloned city scenes, geometry, generated databases, Addressables content, or `.meta` identities; stale/mismatched source identity or hash fails closed.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_010_physical_source_reuse.json`; checked-wrapper source-binding `10 / 10`, source-growth `17 / 17`, and architecture `23`-suite passes with zero compiler errors; exact accepted definition/authoring/presentation/runtime-binding/surface/minimap GUIDs and SHA-256 hashes remain frozen; blank bindings preserve every existing map; resolved logical bindings require exact source identity/content hashes and source-scene GUID and reject missing, stale, mismatched, self, and chained bindings; duplicate scan finds exactly the two accepted Skirmish dense-city scenes and zero Campaign clone, geometry, generated database, or Addressables content; identity/hash rules decomposed without exception so material `OperationMapDefinition.cs` is `499` lines; seven incidental validator outputs restored; clean pushed head.

- [x] **M01DC-011 - Select the Old Market mission window**
  **Depends on:** M01DC-010.
  **Deliverable:** reviewed logical bounds, playable surfaces, navigation scope, off-window exclusion, landmark orientation, and contact corridor using accepted city data. The approved current FirstLaunch comic panels `FL-P15` through `FL-P18`, with `FL-P18` as the binding handoff frame, are the visual/geographic authority; archived M01 prototype captures and earlier dense-city review images are not design authority.
  **Acceptance:** one player squad and one patrol fit readable M01 scale; reachable space has no road-over-water-without-bridge defect; blue Autobahn/canal isolation remains separately classified and is not conflated with this mission window.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_011_old_market_window.json` and `.png`; approved-current FirstLaunch `FL-P15..18` authority hash-bound at both aspect families with `FL-P18` as handoff and old images explicitly rejected; logical playable bounds XZ `[760,300]..[1000,476]`, contact corridor `[804,348]..[932,428]`, `37,415` connected infantry cells, `17,751` road cells, and zero bridge cells; actual-surface annotated crop review; road-over-water absent in-window while blue Autobahn/canal remains a separate out-of-window classification; two-pass logical asset/report/capture SHA-256 stable; focused `9 / 9`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass; seven incidental architecture-validator outputs restored to exact pre-run hashes; clean pushed head.

- [x] **M01DC-012 - Author planning, battle-start, and minimap projections**
  **Depends on:** M01DC-011.
  **Deliverable:** frozen camera and minimap identities with framing, clamp, zoom, projection, safe-area, and transition metadata.
  **Acceptance:** the mission starts readable at supported aspect ratios; cameras remain inside valid bounds; minimap-to-world projection is deterministic and exact.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_012_camera_minimap.json` and contact sheet; exact `camera.ch01.m01.planning`, `camera.ch01.m01.battle_start`, and `minimap.ch01.m01.projection` records authored in the logical map only; all reviewed subjects remain inside frozen safe frames at `1920x1080`, `2400x1080`, and `1920x1200`; camera poses/FOV stay inside M01 bounds with clamp enabled; minimap origin `[760,0,300]`, size `[240,176]`, orientation `0`, exact world/normalized round trip within `0.001`, and out-of-range clamp pass; normal blend policy `1.25s` and reduced-motion cut `0s` frozen for M01DC-015 implementation; two-pass asset/report/contact-sheet hashes stable; focused `12 / 12`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass; seven incidental validator outputs restored to exact pre-run hashes; clean pushed head.

- [x] **M01DC-013 - Author and validate all required anchors**
  **Depends on:** M01DC-011.
  **Deliverable:** all eleven frozen M01 anchors with surface, clearance, orientation, reachability, and uniqueness validation.
  **Acceptance:** every required anchor resolves exactly once; units do not overlap geometry or each other; patrol timing cannot reach civilians before player control/context.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_013_anchor_manifest.json`; all eleven frozen IDs resolve exactly once in `opmap.ch01.district_edge_01`; deterministic nearest-clear-cell placement proves each authored radius against the accepted, unmodified physical-source surface; unit-bearing anchors remain mutually clear; Courier/Warden/Broker scenario requirements remain exact; conservative direct patrol-to-civilian arrival is `11.989s` against the required `9s` minimum and route waypoints remain outside civilian clearance; asset/report hashes are identical across two focused passes; focused `13 / 13`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass; the test source remains below the 350-line target; seven incidental architecture-suite FirstLaunch outputs were backed up then restored to exact pre-run hashes; protected-path audit and diff check pass; clean pushed head.

- [x] **M01DC-014 - Publish the logical M01 OperationMapDefinition**
  **Depends on:** M01DC-010 through M01DC-013.
  **Deliverable:** `opmap.ch01.district_edge_01` definition binding the mission view, shared source, bounds, surfaces, navigation, cameras, minimap, and anchors.
  **Acceptance:** operation-map validators pass; the physical-source hash matches the accepted source; the existing Skirmish map remains byte/behavior equivalent.
  **Evidence:** `Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset` and `Design/AgentReports/M01FirstContact/m01dc_014_operation_map.json`; the deterministic M01 builder publishes exactly one `BuiltInLocal` `opmap.ch01.district_edge_01` entry with content-pack version/hash equal to its definition; catalog resolution plus a fresh ECS bootstrap publish the exact map/scenario/mission identities; full map/source/scenario validation covers accepted source scene, surface, Old Market bounds, two cameras, minimap, and eleven anchors as one unit; accepted physical-source asset and existing Skirmish catalog remain byte-identical; two-pass map/catalog/report hashes are stable; focused `13 / 13`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass; new sources remain below the 350-line target; seven incidental architecture-suite FirstLaunch outputs were backed up then restored to exact pre-run hashes; protected-path and diff checks pass; clean pushed head.

- [x] **M01DC-015 - Accept FL-P18-to-live camera continuity**
  **Depends on:** M01DC-012, M01DC-014.
  **Deliverable:** covered transition timing and matched final comic/live planning-camera composition for normal and reduced-motion settings.
  **Acceptance:** no Main Menu frame, unrelated scene flash, invalid camera, unlit/streaming-in city, or uncontrolled input is visible; the transition is acceptable at 16:9, 20:9, and tablet landscape.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_015_camera_continuity.json` and contact sheet; corrected logical window `[1672..1912,680..856]`, minimap, eleven anchors, and the two logical cameras now frame the accepted civic-bazaar avenue around `(1792,768)` rather than the unrelated western compound; current approved FL-P18 is the only visual authority; planning and battle-start captures retain the avenue direction and gold-domed civic landmark at 16:9, 20:9, and 16:10, with no invalid camera, unrelated scene, Main Menu, or input leak; report honestly defers live patrol actors to M01DC-019; accepted physical scene SHA-256 remains `c1bc203591b3f32ae3d8410eaa0988e694b1d9d449ba1e938d9f38058698b598`; all ten owned map/catalog/report outputs are byte-identical across two full regeneration passes; focused window `9 / 9`, camera/minimap `12 / 12`, anchors `13 / 13`, Operation Map `13 / 13`, continuity `14 / 14`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass; seven incidental architecture-suite FirstLaunch outputs were restored to HEAD; protected-path and diff checks pass; clean pushed head.

- [x] **M01DC-016 - Close dense-city reuse and map-regression gate**
  **Depends on:** M01DC-014, M01DC-015.
  **Deliverable:** consolidated proof that M01 adds only a logical view and mission metadata.
  **Acceptance:** accepted VRP database/capacities/hashes and protected physical content are unchanged; no duplicate permanent render representation exists; dense-city parity and map correctness suites pass.
  **Evidence:** permanent checked gate `Assets/Tests/Editor/M01FirstContactDenseCityReuseTests.cs` and compact deterministic report `Design/AgentReports/M01FirstContact/m01dc_016_dense_city_reuse_gate.json`; all six protected physical files remain byte-exact, including source definition `f91b7372...eba6`, authoring scene `5a15843d...2b9c`, presentation scene `c1bc2035...b598`, runtime binding `f58a73d0...ade9`, surface `1402d769...b4d`, and minimap `420d9f07...cc3f`; the accepted VRP database remains `04681634...8d73` with record ordering `a931d70e...2fe7`, config `2fb85fd0...68b8`, `76,517` source rows, `61,925` logical rows, `14,592` resident rows, `40,460` placements, and exactly `7,784` fixed slots; exhaustive parity remains `36,304 / 36,304` identities and `62,455 / 62,455` renderers with zero mismatch; M01 owns zero permanent render scene/archive and zero render database; focused reuse `8 / 8`, M01 map correctness `13 / 13`, VRP structural/capacity `48 / 48`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass; seven incidental architecture-suite FirstLaunch outputs were restored to HEAD; protected-path audit and diff check pass; clean pushed head.

### Phase D - Authoritative Mission Runtime

- [x] **M01DC-017 - Add the single mission runtime owner**
  **Depends on:** M01DC-005, M01DC-009, M01DC-016.
  **Deliverable:** one ECS-owned mission phase/outcome state machine with source version and the approved phases from Preparing through ReturnReplay.
  **Acceptance:** exactly one writer is found; invalid transitions fail closed; UI/narrative/ARIA/audio remain readers; no static mutable state or parallel store appears.
  **Evidence:** `Assets/Game/Scripts/Components/CampaignMissionComponents.cs`, `Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.cs`, `Assets/Game/Scripts/Runtime/Missions/CampaignMissionCatalogDisposalSystem.cs`, and `Design/AgentReports/M01FirstContact/m01dc_017_runtime_owner.json`; one `CampaignMissionRuntimeComponent` semantic writer owns all ten approved phases and outcome/version changes, while invalid transitions and result rewrites fail closed and result presentation may advance only while preserving its outcome/return route; all runtime data is unmanaged, no static mutable state or parallel store exists, and the catalog blob has one idempotent disposal owner. Checked runtime ownership/transition/lifecycle `12 / 12`, exact Phase 7 inventory `19 / 19`, source-growth `17 / 17`, architecture `23` suites, deterministic inventory regeneration, and compiler zero pass; both new production systems are registered as `ISystem` without adding a managed exception; seven incidental architecture outputs were restored to HEAD; diff/protected-path checks pass; clean pushed head.

- [x] **M01DC-018 - Integrate one launch/bootstrap path**
  **Depends on:** M01DC-017.
  **Deliverable:** validated payload resolution, mission/scenario/map loading, readiness gating, and controlled entry used by both FirstLaunch and Campaign origins.
  **Acceptance:** equal payloads produce equal runtime setup; stale/missing data fails to a bounded recovery surface; no Campaign path reuses an inappropriate Skirmish shortcut.
  **Evidence:** `CampaignMissionCatalogProjection` validates mission/scenario/logical-map identity as one unit and publishes an owned, versioned blob idempotently; the single `CampaignMissionLaunchSystem` gates both typed origins on exact active-map identity and required readiness, preserves pending requests, returns bounded failures, rejects a Skirmish identity shortcut, and resets retries deterministically. Focused launch/bootstrap `10 / 10`, PlayMode `3 / 3`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, deterministic inventory, and compiler zero pass; the production inventory is exactly `209` declarations / `184` `ISystem` / `25` managed exceptions; seven incidental outputs restored; clean pushed head.

- [x] **M01DC-019 - Spawn deterministic friendly and hostile forces**
  **Depends on:** M01DC-013, M01DC-018.
  **Deliverable:** one player rifle squad; the exact FirstLaunch Courier/Warden/Broker Ash Line patrol; route timing, hostility, command restrictions, and deterministic correlation to scenario data.
  **Acceptance:** the three live hostile entities resolve to `Chr_Insurgent_Male_03`, `Chr_Insurgent_Female_01`, and `Chr_Insurgent_Female_02` with continuity-correct roles/equipment; Qassem/Male 05 and the Male 02 heavy gunner are absent; no active vehicles/air/build/economy; select/move/attack/stop/hold remain available; spawn identity/count/position/route are deterministic and anchor-safe.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_019_forces.json`; the sole spawn path resolves four exact player rifle prefabs and the continuity-correct Courier/Warden/Broker trio from scenario-projected runtime keys, rejects missing/forbidden prefabs before any partial spawn, places all seven deterministically within accepted anchors, and queues the three hostile patrol orders exactly once after `3000 ms`; Qassem/Male 05, Male 02 heavy gunner, vehicles, air, build, production, and economy remain absent while select/move/attack/stop/hold remain enabled. Focused PlayMode `3 / 3`, scenario compatibility/command restrictions `8 / 8`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, exact `211 / 186 / 25` inventory, protected dense-city diff, and restored-output checks pass; clean pushed head.

- [x] **M01DC-020 - Add the single objective writer and projections**
  **Depends on:** M01DC-017, M01DC-019.
  **Deliverable:** exactly one objective system writes `MatchObjectiveRuntimeElement` and source version from mission state; HUD, ARIA, and result read the projection.
  **Acceptance:** destroy-patrol progress/completion and command-squad failure are deterministic; duplicate/stale writes are rejected; no reader derives a competing objective truth.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_020_objective_projection.json`; exactly one `CampaignMissionObjectiveProjectionSystem` writes the two authoritative objective rows from Campaign mission facts through the neutral `MatchObjectiveProjectionBoundaryComponent`. Destroy-patrol progress/completion, command-squad failure, session/attempt/source correlation, duplicate/stale rejection, and projection-only HUD/ARIA consumption pass. Focused writer `5 / 5`, assistant projection `10 / 10`, PlayMode `1 / 1`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, exact `212 / 187 / 25` inventory, protected-path, restored-output, and diff checks pass; clean pushed head.

- [x] **M01DC-021 - Implement failure, retry, unload, and lifecycle cleanup**
  **Depends on:** M01DC-018 through M01DC-020.
  **Deliverable:** command-squad failure, deterministic retry from the same payload/scenario, explicit exit/resume policy, and complete native/pool/entity/event cleanup.
  **Acceptance:** retry has no penalty and cannot duplicate rewards/events/entities; repeated launch/retry/exit cycles have no stale state, native leak, pool growth, or disposed access.
  **Evidence:** repeated lifecycle/transition/pool/native/allocation tests, clean pushed head.

  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_021_lifecycle.json`; accepted retry removes all prior `CampaignMissionUnitRoleComponent` entities, clears attempt-local launch/action/settlement queues and result/facts before publishing the new runtime identity, while system teardown removes remaining mission entities and disposes only its owned blob. Command-squad loss remains the sole deterministic defeat and applies no retry penalty. Focused PlayMode `4 / 4` including eight repeated retry cycles, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, restored-output, protected-path, and diff checks pass; clean pushed head.

- [x] **M01DC-022 - Add bounded ambient civilians**
  **Depends on:** M01DC-019, M01DC-021.
  **Deliverable:** capped ambient/scripted evacuation presentation driven one-way by mission completion and optional fallback when presentation capacity is unavailable.
  **Acceptance:** civilians are non-selectable, non-attackable, never targeted, never authoritative, never counted in objectives/stars/rewards, deterministically bounded, and fully cleaned up.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_022_ambient.json`; the canonical scenario projects its authored eight civilians into the unmanaged mission blob; one presentation-only `ISystem` instantiates only the four approved civilian prefab keys, strips selection/faction/health/combat/attack/target/control components, begins deterministic evacuation only on `SecureCorridor` or victory, and creates zero entities when optional presentation capacity is unavailable. Authored eight and hard maximum twelve are independent and fail closed above twelve. Focused PlayMode `7 / 7` covers inert targeting, bounds, zero fallback, victory/defeat behavior, eight retries, teardown, and `0 B` stable-update managed allocation; Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, restored-output, protected-path, and diff checks pass; clean pushed head.

  **Ownership amendment (2026-08-13):** fresh exact-head review found the canonical scenario's ambient count/anchor/route are validated but not projected into `CampaignMissionDefinitionBlob`; hard-coding them in the ambient system would violate configuration ownership. Add only `CampaignMissionComponents.cs` and `CampaignMissionCatalogProjection.cs` for unmanaged ambient blob projection, plus `systembase_to_isystem_inventory.md` and `NonUiSystemBaseMigrationArchitectureTests.cs` to register the one planned `CampaignMissionAmbientPresentationSystem : ISystem` with exact counts. Deterministic Unity metadata paired with the authorized new system/test is implicit. No scenario/map/prefab/city/objective/result/reward/threshold/package/scene data is changed.

- [x] **M01DC-023 - Evaluate stars and construct the mission result**
  **Depends on:** M01DC-020, M01DC-021.
  **Deliverable:** deterministic completion time, squad-loss, primary/failure, star, and result projection from authoritative runtime facts.
  **Acceptance:** all star combinations are tested; four minutes never fails the mission; result UI cannot alter the result; retries create a new bounded attempt without corrupting best data.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_023_result.json`; canonical authored star rules project into the unmanaged mission blob and the sole result writer emits one attempt-correlated immutable result plus one settlement request. All five star combinations pass, exactly four minutes remains a victory worth two stars with no loss, contradictory facts fail closed, retry attempts do not mutate prior best data, and UI has zero result writers. Focused `8 / 8`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, protected-path, restored-output, and diff checks pass; clean pushed head.

  **Ownership amendment (2026-08-13):** fresh exact-head review found the canonical mission's validated star rules are not projected into `CampaignMissionDefinitionBlob`; hard-coding their rules or four-minute threshold in the result writer would violate `MissionDefinition` ownership. Add only `CampaignMissionComponents.cs` and `CampaignMissionCatalogProjection.cs` for unmanaged star-rule projection and result attempt correlation, plus `systembase_to_isystem_inventory.md` and `NonUiSystemBaseMigrationArchitectureTests.cs` to register the one planned `CampaignMissionResultProjectionSystem : ISystem` with exact counts. Deterministic Unity metadata paired with the authorized new system/test is implicit. No mission asset, threshold, UI, reward, persistence, settlement, map, city, package, scene, or Skirmish behavior is changed.

- [x] **M01DC-024 - Settle progress, rewards, resume, and replay exactly once**
  **Depends on:** M01DC-008, M01DC-023.
  **Deliverable:** one Campaign progression writer for first clear, reduced replay reward, best stars/time, M02 reveal, resume/restart, and replay record.
  **Acceptance:** repeated messages, restarts, crashes, retries, and replay cannot double-grant; first-clear and replay return destinations differ as designed; M01 never grants Intel.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_024_settlement.json`; the sole progress writer consumes attempt-correlated victory requests, grants the exact configured first-clear or reduced replay reward, records additive schema-v2 settlement-token history, persists best stars/time and resume state atomically, reveals canonical M02 only on first clear, and preserves FirstLaunch-to-CommandBase versus Campaign-to-Operations routing across retries. Repeated current or historical messages return accepted prior-success semantics without a grant; crash/restart/migration/replay-before-clear/defeat/Intel cases fail safely. Focused settlement `13 / 13`, progress regression `15 / 15`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, protected-path, restored-output, and diff checks pass; clean pushed head.

  **Ownership amendment (2026-08-13):** exact-head review found three fail-closed gaps in the planned four-path slice. The validated first-clear/replay reward sets were not projected into the mission blob, so the settlement writer would otherwise duplicate balance values; add only `CampaignMissionComponents.cs` and `CampaignMissionCatalogProjection.cs` for unmanaged reward projection. Persisting only `lastSettledToken` could regrant an older message after a newer replay; add only `SaveDataModel.cs` for additive schema-v2 token history. Retry changes `RunKind` to `Retry`, so add only `CampaignMissionRuntimeSystem.cs` to derive the M01 return route from the preserved launch origin and retain FirstLaunch-to-CommandBase versus Campaign-to-Operations behavior. Register the planned writer as an `ISystem` plus one ECS managed-reference injection boundary in the deterministic Phase 7 inventory/test and align this technical authority; this preserves the zero-open-debt gate instead of adding a non-presentation `SystemBase` exception. Deterministic metadata paired with new owned sources is implicit. No mission reward amount, map, city, scene, package, Addressables output, Skirmish behavior, gameplay rule, or unrelated save field changes.

### Phase E - Guidance, Narrative, And FirstLaunch

- [x] **M01DC-025 - Implement Full guidance**
  **Depends on:** M01DC-020.
  **Deliverable:** proactive Find Squad, Move to Cover, Confirm Threat, Engage, and Secure Corridor guidance plus typed Show Me/Do It requests within existing ARIA authority.
  **Acceptance:** guidance reads mission/objective projections, never writes outcome/gameplay truth, respects cooldown/acknowledgement/accessibility, and produces no per-frame allocation.
  **Evidence:** `Design/AgentReports/M01FirstContact/m01dc_025_full_guidance.json`; one unmanaged projection owner publishes all five Full prompts while existing ARIA owners exclusively translate recommendations and execute typed Show Me/Do It requests. Acknowledgement, cooldown, accessibility, replay-tutorial suppression, attempt cleanup, one-writer, and stable `0 B` allocation coverage pass. Guidance `7 / 7`, ARIA `10 / 10`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, protected-path, restored-output, and diff checks pass; clean pushed head.

  **Ownership amendment (2026-08-13):** exact-head review confirms `AssistantRecommendationSystem` remains the sole live ARIA recommendation-buffer writer and `AssistantCommandIntentSystem` remains the sole typed Show Me/Do It command bridge. Add only the unmanaged guidance projection/acknowledgement contracts and root projection, one guidance `ISystem` plus metadata, launch cleanup, ARIA-owned mapping/evaluation contracts and assembly references, the focused test plus metadata, and exact Phase 7 inventory/count paths. Mission guidance is projection-only and cannot write mission facts, objectives, outcome, commands, rewards, maps, scenes, or protected dense-city outputs.

  **Source-growth correction (2026-08-13):** move the existing pure recommendation equality comparison into the existing stateless `AssistantObjectiveProjectionUtility.cs` with guidance mapping. `AssistantReadModelSystems.cs` remains below its exact 599-line ceiling; no exception, threshold, or speculative headroom is added.

- [x] **M01DC-026 - Implement Contextual and Minimal guidance**
  **Depends on:** M01DC-025.
  **Deliverable:** progressive contextual hints and mandatory-information-only minimal guidance using the same scenario and objective state.
  **Acceptance:** all three modes produce identical spawns, patrol behavior, commands, objectives, stars, rewards, and results for equal player actions; only presentation/help differs.
  **Evidence:** cross-mode equivalence matrix and focused tests, clean pushed head.

  **Ownership amendment (2026-08-13):** M01DC-025 established the sole projection writer and its focused suite. M01DC-026 changes only the existing guidance projection component schema, guidance system/test, this tracker, the exact-path matrix, and compact evidence. Contextual escalation derives solely from attempt elapsed time and the existing cooldown; Minimal filters proactive phases and disables execution. No new system, writer, assembly dependency, gameplay fact, scenario/map asset, reward, threshold, or protected output is authorized.

  **Accepted evidence (2026-08-13):** Full retains its existing proactive typed ARIA policy; Contextual presents the same five phase prompts, escalates from strength 1 to 2 only after the existing cooldown, and restricts Do It to the already-safe squad-selection and movement phases; Minimal presents only mandatory threat confirmation and corridor-security information and never executes. The focused cross-mode suite passes `10 / 10`, source growth passes `17 / 17`, and the architecture entrypoint passes `23` suites with zero compiler errors. Equal-action mission/runtime facts remain unchanged, protected dense-city outputs are unchanged, and exact log hashes are recorded in `m01dc_026_guidance_modes.json`.

- [x] **M01DC-027 - Replace the FirstLaunch menu handoff with typed M01 launch**
  **Depends on:** M01DC-015, M01DC-018, M01DC-026.
  **Deliverable:** normal-complete, skip, and interrupted `HandoffPending` flows persist identity/guidance then request the same validated M01 payload without entering Main Menu.
  **Acceptance:** all paths reach M01 once; resume cannot lose or duplicate the launch; unrelated returning-player/menu startup is unchanged; failed mission readiness returns to an explicit recoverable state rather than falsely completing FirstLaunch.
  **Evidence:** normal/skip/interruption/crash/restart/no-menu-frame tests and captures, clean pushed head.

  **Ownership amendment (2026-08-13):** add only the stateless typed handoff operation, additive persisted correlation fields/normalization, append-only `EnterMission`, and narrow changes to the existing FirstLaunch composition/profile/shell and startup-cover seams, affected integration expectations, dedicated Editor/PlayMode tests, exact-path matrix, tracker, and compact evidence. `CampaignMissionLaunchSystem` remains the sole launch acceptor/runtime writer. No menu request, static scene bridge, second state owner, map/scene/config/reward change, or protected dense-city output is authorized.

  **Accepted evidence (2026-08-14):** normal completion, skip, and persisted `HandoffPending` resume now create the same typed FirstLaunch-origin M01 payload and hold the startup cover in `EnterMission`; no Main Menu route is emitted. Correlation is persisted before publication, accepted results alone complete FirstLaunch, rejection retries are bounded at three, and restart reuses the same transition/session identity. Focused handoff passes `5 / 5`, inherited FirstLaunch Gate 89 passes, cross-frame PlayMode passes `1 / 1`, source growth passes `17 / 17` without an exception, and architecture passes `23` suites with compiler zero. Generated validation outputs were restored and protected dense-city outputs are unchanged; exact hashes are recorded in `m01dc_027_first_launch_handoff.json`.

- [x] **M01DC-028 - Integrate brief, comms, debrief, and command-base reveal**
  **Depends on:** M01DC-020, M01DC-024, M01DC-027.
  **Deliverable:** compact interactive brief, in-mission comms, first-clear debrief, Story Archive references, and command-base arrival with M02 highlighted.
  **Acceptance:** narrative emits typed requests only; the brief continues the exact FirstLaunch Courier/Warden/Broker threat; the debrief reveals coordination and a revoked credential trace without revealing or proxying Qassem; skip/reduced-motion/subtitles work; first clear cannot return to Main Menu/Campaign by mistake; replay does not automatically replay the cold open.
  **Evidence:** sequence/route/accessibility/interruption tests and representative captures, clean pushed head.

  **Ownership amendment (2026-08-14):** add only the deterministic `Assets/Game/Configs/Narrative/Chapter01.meta` required by the already-authorized narrative asset directory, plus this tracker and the exact-path matrix so M01DC-028 acceptance can be recorded. Same-name asset/test metadata remains covered by the frozen implicit-meta rule. No runtime owner, UI prefab, scene, map, reward, Campaign state, source-growth exception, package, project setting, or protected dense-city output is authorized.

  **Source-growth decomposition amendment (2026-08-14):** add one stateless Editor-only `M01FirstContactNarrativeConfigBuilder.cs` and its implicit same-name metadata so brief/comms/debrief serialization does not enlarge the existing 271-line mission/scenario/map builder into a mixed material responsibility. The existing builder may delegate one call to this seam. No runtime type, exception, ceiling increase, second narrative owner, production asset beyond the already-authorized M01 narrative asset, or protected output is authorized.

  **Accepted evidence (2026-08-14):** one deterministic asset contains three separately identified brief/comms/debrief sub-sequences referenced by the canonical mission. The compact brief names only Courier, Warden, and Broker and protects civilians; in-mission comms remain typed mission-scoped completion data; the first-clear debrief reports coordination plus only a fragment of a revoked civic-relay credential with unresolved source identity, then emits `DebriefArrival` with canonical M02 highlight context. Every line is an essential caption, every state supports reduced motion, each sequence skips to its typed completion, Story Archive references are explicit, and no FirstLaunch cold-open reference, Qassem/Male 05/heavy-gunner proxy, or complete-Protocol confirmation exists. Two-pass generation is byte-identical; focused narrative passes `8 / 8`, source growth `17 / 17` without an exception, architecture `23` suites with compiler zero, and source/report diff checks are clean. Unity's canonical empty-value spacing is retained in the hash-bound generated YAML rather than rewritten after generation. Representative transcript, exact asset/log hashes, and restored-output audit are recorded in `m01dc_028_narrative_routes.json`; later in-game visual capture remains owned by M01DC-040.

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
| 2026-08-12 | M01DC-003 | Exact-path ownership, evidence, validation, generated-output, and rollback authority is frozen for M01DC-004..043. Static audit covers `40 / 40` item allowlists, `173` exact path entries, and `63` unique types in `11` assemblies with no forbidden names, wildcard edit paths, read-only overlap, or accepted dense-city physical-source edits. | enclosing M01DC-003 acceptance commit | Accepted |
| 2026-08-12 | M01DC-004 | Added dependency-root `Game.Missions.Contracts`, default-safe mission definition/catalog data, and fail-closed identity/reference/objective/star/reward/command/readiness validation. M01 rejects Intel and duplicate/invalid configuration; checked M01 `12 / 12`, source-growth `17 / 17`, and architecture `23`-suite gates pass with compiler zero. | enclosing M01DC-004 acceptance commit | Accepted |
| 2026-08-12 | M01DC-005 | Added the sole stateless `MissionLaunchPayloadFactory` used by both entry origins. Equal inputs yield equal immutable payloads; retry preserves mission/scenario/map/session/seed and increments attempt/correlation only; invalid inputs fail closed. Checked payload `9 / 9`, source-growth `17 / 17`, and architecture `23` suites pass with compiler zero. | enclosing M01DC-005 acceptance commit | Accepted |
| 2026-08-12 | M01DC-006 | Extended `ScenarioSetupConfig` additively with force groups/unit identities, deterministic timing, patrol routes, restrictions, and bounded ambient presentation. Legacy Skirmish defaults bypass Campaign requirements unchanged; Campaign data fails closed before launch. Checked compatibility `8 / 8`, source-growth `17 / 17`, and architecture `23` suites pass with compiler zero. | enclosing M01DC-006 acceptance commit | Accepted |
| 2026-08-12 | M01DC-007 | Added definition-projected star/reward display data and exclusive typed-kind-or-config reward settlement identity. Duplicate, ambiguous, placeholder, and M01 Intel rewards fail closed; the `240000` ms goal remains a star threshold only. Checked rule `14 / 14`, source-growth `17 / 17`, and architecture `23` suites pass with compiler zero. | enclosing M01DC-007 acceptance commit | Accepted |
| 2026-08-12 | M01DC-008 | Added additive Campaign mission progress schemas, sole-owner deterministic store mutation, idempotent settlement tokens, and same-directory flushed atomic profile replacement. New/older/corrupt/future profiles fail safely without invented progress; interrupted writes preserve the prior profile; settings and Quick Game remain unchanged. Checked progress `15 / 15`, source-growth `17 / 17`, and architecture `23` suites pass with compiler zero. | enclosing M01DC-008 acceptance commit | Accepted |
| 2026-08-12 | M01DC-009 ownership amendment | Added only the five deterministic Unity folder `.meta` paths required by M01DC-009's already-authorized nested canonical asset paths. No production/config asset changed; exact-path and protected-path policies remain unchanged. | enclosing ownership-amendment commit | Accepted |
| 2026-08-12 | M01DC-009 metadata amendment | Added only the five deterministic Unity file `.meta` paths paired with M01DC-009's already-authorized canonical assets and source files. No production/config asset changed; exact-path and protected-path policies remain unchanged. | enclosing metadata-amendment commit | Accepted |
| 2026-08-12 | M01DC-009 objective-identity amendment | Added only `MissionDefinitionContractValidation.cs` so M01DC-009 can reconcile the frozen `obj.ch01.*` authority with the prior validator's obsolete `objective.*`-only prefix. The change is bounded to canonical objective-ID acceptance and remains fail closed. | enclosing objective-identity-amendment commit | Accepted |
| 2026-08-12 | M01DC-009 | Authored and catalogued deterministic canonical M01 mission/scenario data. Exact frozen identities, objective/star/reward policy, anchors, restrictions, ambient civilians, approved friendly squad, and Courier/Warden/Broker patrol validate; reserved Qassem/heavy-gunner identities and Intel are excluded. Two-pass assets are byte stable; canonical `13 / 13`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass. | enclosing M01DC-009 acceptance commit | Accepted |
| 2026-08-12 | M01DC-010 metadata amendment | Added only the deterministic Unity `.meta` paired with M01DC-010's already-authorized focused test source. No production/config asset changed; exact-path and protected-path policies remain unchanged. | enclosing metadata-amendment commit | Accepted |
| 2026-08-12 | M01DC-010 source-growth decomposition amendment | Added `OperationMapIdentityRules.cs` and its Unity `.meta` so the existing identity/hash rule responsibility can move out of the material `OperationMapDefinition.cs` while adding the authorized source-binding model. This is a bounded mechanical decomposition, not a source-growth exception or threshold change. | enclosing source-growth-decomposition commit | Accepted |
| 2026-08-12 | M01DC-010 | Added the default-safe `OperationMapSourceBindingConfig` and fail-closed logical-to-physical resolver. Exact accepted dense-city GUID/hash/source-scene evidence passes; existing self-owned maps remain unchanged; stale, missing, mismatched, self, and chained bindings fail. Zero physical-source clones or protected changes exist; focused `10 / 10`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass. | enclosing M01DC-010 acceptance commit | Accepted |
| 2026-08-12 | M01DC-011 authority/validation amendment | Project owner clarified that the current approved FirstLaunch comic, not old prototype or dense-city review images, is the bazaar/Old Market visual authority. Froze `FL-P15..18` at 16:9 and 20:9 read-only and authorized one focused checked-wrapper validator/capture responsibility plus its metadata. No production asset, city source, map data, threshold, or protected content changed. | enclosing amendment commit | Accepted |
| 2026-08-12 | M01DC-011 source-growth decomposition amendment | Authorized one stateless `M01FirstContactOldMarketWindowEvidence` source and metadata so capture/report serialization remains separate from Old Market surface/navigation validation and both new editor files remain within the 350-line target. No exception, threshold, production runtime, or physical-source change is authorized. | enclosing amendment commit | Accepted |
| 2026-08-12 | M01DC-011 | Selected the logical Old Market window from the accepted dense-city surface using only current approved FirstLaunch `FL-P15..18` comic authority. Deterministic connected-route selection validates exact bounds/corridor, readable 4-versus-3 scale, `37,415` connected infantry cells, `17,751` road cells, zero bridge cells, and separate canal classification. Actual-surface capture and logical asset/report are two-pass hash-stable; focused `9 / 9`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass. | enclosing M01DC-011 acceptance commit | Accepted |
| 2026-08-12 | M01DC-012 ownership/source-growth amendment | Added only the deterministic Unity `.meta` paired with the authorized camera/minimap test and one stateless test-side evidence/capture source plus metadata. This keeps validation and image/report serialization separate under the 350-line target; no runtime class, physical city source, map bake, threshold, package, or project setting is authorized. | enclosing amendment commit | Accepted |
| 2026-08-12 | M01DC-012 | Authored the two frozen M01 cameras and exact Old Market minimap crop in the logical map without a new runtime owner. Projection tests retain required subjects inside safe frames at 16:9, 20:9, and 16:10 tablet; camera bounds/zoom/clamp, exact minimap round trip/clamp, and future transition/reduced-motion policy pass. Two-pass outputs are hash-stable; focused `12 / 12`, source-growth `17 / 17`, architecture `23` suites, and compiler zero pass. | enclosing M01DC-012 acceptance commit | Accepted |
| 2026-08-12 | M01DC-013 metadata amendment | Added only the deterministic Unity `.meta` paired with the already-authorized anchor test source. The scenario remains read-only; no physical city/surface, runtime class, threshold, package, or project setting is authorized. | enclosing metadata-amendment commit | Accepted |
| 2026-08-12 | M01DC-013 | Authored all eleven frozen M01 anchors in the logical Old Market map only. Deterministic accepted-surface clearance, uniqueness, unit spacing, exact scenario requirements, and conservative patrol/civilian timing pass; map/report hashes are stable across two focused passes; focused `13 / 13`, source-growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, and restored-output checks pass. | enclosing M01DC-013 acceptance commit | Accepted |
| 2026-08-12 | M01DC-014 | Published the completed logical Old Market definition through one Chapter 01 catalog entry and proved fresh catalog/bootstrap resolution without changing the physical dense-city source or existing Skirmish catalog. Map/catalog/report outputs are deterministic; focused `13 / 13`, source-growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, and restored-output checks pass. | enclosing M01DC-014 acceptance commit | Accepted |
| 2026-08-12 | M01DC-015 capture ownership amendment | Authorized one read-only Editor capture/validation source and deterministic Unity `.meta` to render the already-frozen logical-map cameras against the accepted dense-city presentation and compare only with current approved FL-P18. It may write only the existing M01DC-015 JSON/contact-sheet evidence paths and may not alter scenes, map/camera data, FirstLaunch art, VRP content, thresholds, packages, or project settings. | enclosing amendment commit | Accepted |
| 2026-08-12 | M01DC-015 camera-correction amendment | Fresh accepted-scene rendering rejected the prior frozen pose because it produced overhead industrial/base framing rather than the approved FL-P18 road/bazaar continuity. Authorized correction of only the two logical-map camera records and their dependent catalog/hash/evidence records plus the existing camera validator. City scenes, geometry, source binding, surfaces, navigation, minimap, anchors, VRP data/capacities, FirstLaunch art, runtime code, thresholds, packages, and project settings remain read-only. The rejected generated sheet/report are not acceptance evidence. | enclosing amendment commit | Accepted |
| 2026-08-12 | M01DC-015 logical-window correction amendment | Exact accepted-scene inspection identified the real civic bazaar around runtime-grid center `(1792,768)` while M01DC-011 had selected the unrelated western compound at XZ `[760..1000,300..476]`; current FL-P18-to-live continuity is therefore impossible by camera correction alone. Authorizes correction of only the logical M01 window/corridor, dependent minimap, eleven anchor coordinates, two cameras, Chapter 01 catalog/hash records, and M01DC-011..015 tests/evidence. The accepted physical dense-city scene, surface/navigation payloads, source binding, VRP databases/capacities, FirstLaunch art, runtime code, thresholds, packages, and project settings remain read-only. Every corrected anchor must resolve on the existing accepted surface, and fresh current-FL-P18/live visual evidence must pass before M01DC-015 acceptance. | enclosing amendment commit | Accepted |
| 2026-08-13 | M01DC-015 | Corrected the logical M01 view to the actual civic bazaar and accepted FL-P18-to-live planning/battle-start continuity across 16:9, 20:9, and 16:10. Physical city, FirstLaunch art, surfaces/navigation, VRP data, runtime owners, and thresholds remain unchanged; deterministic focused and architecture gates pass, with live patrol actors explicitly deferred to M01DC-019. | enclosing M01DC-015 acceptance commit | Accepted |
| 2026-08-13 | M01DC-016 | Added the permanent fail-closed dense-city reuse regression. Six protected physical files, the exact VRP database identity/order/config, fixed capacity, row/placement counts, and exhaustive transform/render parity remain accepted; M01 owns no permanent render representation or database. Reuse `8 / 8`, map correctness `13 / 13`, VRP structural `48 / 48`, source growth `17 / 17`, and architecture `23` suites pass with compiler zero and restored incidental outputs. | enclosing M01DC-016 acceptance commit | Accepted |
| 2026-08-13 | M01DC-017 Phase 7 inventory amendment | The full architecture gate correctly rejected the two authorized new production `ISystem` declarations because the deterministic Phase 7 inventory was stale. Added only `Design/Architecture/systembase_to_isystem_inventory.md` to the M01DC-017 allowlist so those exact declarations can be registered and the existing architecture gate can remain fail closed. No threshold, exception, production behavior, protected content, or deferred architecture phase changes. | enclosing amendment commit | Accepted |
| 2026-08-13 | M01DC-017 Phase 7 exact-count amendment | The regenerated inventory passed determinism but the focused Phase 7 gate failed closed at the frozen production declaration count (`206` expected, `208` actual). Added only `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs` to the M01DC-017 allowlist so its exact frozen counts can describe the two new authorized `ISystem` declarations. This changes no cap, exception, disposition, runtime behavior, protected content, or deferred architecture scope. | enclosing amendment commit | Accepted |
| 2026-08-13 | M01DC-017 | Added the sole ECS mission phase/outcome writer and one owned catalog-blob disposer using unmanaged components and the approved ten-phase vocabulary. Result identity is immutable, invalid transitions fail closed, and no UI/narrative/ARIA/audio writer, static mutable state, or parallel store exists. Runtime `12 / 12`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, deterministic inventory, compiler zero, incidental-output restoration, and diff checks pass. | enclosing M01DC-017 acceptance commit | Accepted |
| 2026-08-13 | M01DC-018 Phase 7 registration amendment | M01DC-018's already-planned `CampaignMissionLaunchSystem : ISystem` necessarily adds one production ECS declaration. Added only the deterministic Phase 7 inventory and its exact frozen-count test to the item allowlist so the existing gate can register that declaration fail closed. No cap, exception, disposition, runtime behavior, protected content, or deferred architecture scope is changed. | enclosing amendment commit | Accepted |
| 2026-08-13 | M01DC-018 | Added one shared catalog projection and launch system for FirstLaunch and Campaign origins. Exact canonical identities, readiness, bounded rejection, retry/reload, idempotent blob ownership, and explicit rejection of the Skirmish shortcut pass; focused `10 / 10`, PlayMode `3 / 3`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, and restored-output checks pass. | enclosing M01DC-018 acceptance commit | Accepted |
| 2026-08-13 | M01DC-019 force-projection/Phase 7 amendment | The planned spawn and patrol systems require their canonical scenario force/route data in the already-owned mission blob, and add exactly two production `ISystem` declarations. Added only `CampaignMissionComponents.cs`, `CampaignMissionCatalogProjection.cs`, the deterministic Phase 7 inventory, and its exact-count test to M01DC-019. This authorizes data-only force/route blob projection and exact registration; it does not authorize scenario/map asset edits, a second owner, tactical-system changes, managed exceptions, thresholds, or protected content. | enclosing amendment commit | Accepted |
| 2026-08-13 | M01DC-019 prefab-resolution/test-dependency correction | Exact-head evidence showed authored semantic unit IDs cannot resolve the existing prefab registry, whose keys are prefab names. Added one explicit `runtimePrefabSourceKey` per already-frozen scenario unit plus its generic config/projection field, and the direct mission-contract reference needed by the already-authorized PlayMode assembly to construct the unmanaged runtime fixture. The exact-path matrix now names these bounded paths. Semantic IDs and expected GUIDs remain authoritative; runtime keys are exact lookup data, not identity aliases. No map, prefab, registry, tactical system, threshold, or protected output changes. | enclosing M01DC-019 acceptance commit | Accepted |
| 2026-08-13 | M01DC-019 | Added deterministic fail-closed force spawning and one-shot delayed patrol projection for the exact four-player/three-hostile scenario force. Courier, Warden, and Broker resolve to Male 03, Female 01, and Female 02; forbidden identities and missing prefabs cannot partially spawn. Focused PlayMode `3 / 3`, scenario compatibility `8 / 8`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, and restored-output checks pass. | enclosing M01DC-019 acceptance commit | Accepted |
| 2026-08-13 | M01DC-020 sole-writer/Phase 7 amendment | Exact-head ownership evidence found the existing assistant goal reader also advances `MatchObjectiveRuntimeStateComponent` elapsed/version fields and clears `MatchObjectiveRuntimeElement`, which would conflict with the planned sole objective writer. Added only `AssistantReadModelSystems.cs` so that reader becomes projection-only, plus the deterministic Phase 7 inventory and exact-count test for the one planned `CampaignMissionObjectiveProjectionSystem : ISystem`. This does not authorize HUD layout, ARIA policy, result policy, mission truth, thresholds, protected content, or another writer. | enclosing M01DC-020 acceptance commit | Accepted |
| 2026-08-13 | M01DC-020 correlation/assembly-boundary amendment | Exact-head review showed retries reset `CampaignMissionRuntimeComponent.Version` for the same mission, so duplicate/stale rejection needs the existing session/attempt correlation and explicit mission-source snapshot on the objective read model. Added only `MatchObjectiveComponents.cs` for unmanaged correlation/source fields, the acyclic `Game.Runtime -> Game.UI.Shell.Contracts.Ecs` contract reference needed to own the already-defined UI-shell boundary, the matching PlayMode test reference, and the existing assistant focused test whose boundary-creation assertion must now prove it does not initialize authoritative objective truth. No UI behavior assembly, mission truth writer, result policy, protected content, package, or scene is added. | enclosing M01DC-020 acceptance commit | Accepted |
| 2026-08-13 | M01DC-020 source-growth correction | The first checked source-growth run correctly rejected growth of the already-reviewed 625-line `AssistantReadModelSystems.cs`. Added only its existing stateless `AssistantObjectiveProjectionUtility.cs` to the allowlist and moved objective HUD formatting/clear logic there; no exception or ceiling grows, and the reader system returns below its reviewed size. | enclosing M01DC-020 acceptance commit | Accepted |
| 2026-08-13 | M01DC-020 neutral-boundary correction | The full architecture gate correctly rejected the provisional `Game.Runtime -> Game.UI.Shell.Contracts.Ecs` reference. Removed that reference and added one unmanaged `MatchObjectiveProjectionBoundaryComponent` in `Game.Components`; the existing assistant boundary initializer places the neutral structural marker without writing objective truth, while runtime depends only on `Game.Components`. No architecture exception or UI-shell dependency is added to runtime. | enclosing M01DC-020 acceptance commit | Accepted |
| 2026-08-13 | M01DC-020 metadata amendment | Added only the three deterministic Unity `.meta` paths paired with the already-authorized objective writer and focused Editor/PlayMode test sources. No production asset, scene, map, package, threshold, or project setting is added. | enclosing M01DC-020 acceptance commit | Accepted |
| 2026-08-13 | M01DC-020 | Added the sole objective writer and neutral projection boundary. Deterministic patrol progress/completion and command-squad failure reject stale/duplicate session, attempt, and source versions; assistant/HUD readers project without writing objective truth. Focused writer `5 / 5`, assistant projection `10 / 10`, PlayMode `1 / 1`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, restored-output, and diff checks pass. | enclosing M01DC-020 acceptance commit | Accepted |
| 2026-08-13 | M01DC-021 | Added bounded attempt lifecycle cleanup to the existing launch and catalog-disposal owners. Retry preserves deterministic identity while removing prior mission entities and attempt-local queues/results; teardown removes remaining mission entities and the owned blob. Focused lifecycle PlayMode `4 / 4` with eight retry cycles, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, restored-output, and diff checks pass. | enclosing M01DC-021 acceptance commit | Accepted |
| 2026-08-13 | M01DC-022 ownership/Phase 7 amendment | Exact-head review found authored ambient count/anchor/route data was not in the mission blob. Added only the existing mission component/projection paths for unmanaged ambient projection and the Phase 7 inventory/count paths for the one planned ambient `ISystem`; deterministic new-source metadata is implicit. No scenario, map, prefab, city, gameplay truth, reward, threshold, package, or scene change is authorized. | enclosing M01DC-022 acceptance commit | Accepted |
| 2026-08-13 | M01DC-022 | Added configuration-projected, bounded presentation-only civilians: canonical eight, hard cap twelve, deterministic safe-zone/evacuation placement, zero fallback when optional visual capacity is absent, no evacuation on defeat, and full retry/teardown cleanup. Focused PlayMode `7 / 7`, stable-update `0 B`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, protected-path, restored-output, and diff checks pass. | enclosing M01DC-022 acceptance commit | Accepted |
| 2026-08-13 | M01DC-023 ownership/Phase 7 amendment | Exact-head review found authored star rules were not projected into the mission blob. Added only the existing mission component/projection paths for unmanaged rule projection and attempt correlation plus the Phase 7 inventory/count paths for one planned result `ISystem`; deterministic new-source metadata is implicit. No mission asset, threshold, UI, reward, persistence, settlement owner, map, city, package, scene, or Skirmish behavior is changed. | enclosing M01DC-023 acceptance commit | Accepted |
| 2026-08-13 | M01DC-023 | Added deterministic attempt-correlated result projection from authoritative runtime facts and authored star rules. All five combinations, exact four-minute victory behavior, fail-closed contradictions, immutable same-attempt updates, retry isolation, one settlement request, and zero UI writers pass. Focused `8 / 8`, Phase 7 `19 / 19`, source-growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, protected-path, restored-output, and diff checks pass. | enclosing M01DC-023 acceptance commit | Accepted |
| 2026-08-13 | M01DC-024 | Added the sole Campaign progress/reward writer with injected atomic persistence, configuration-projected first-clear/replay rewards, schema-v2 historical token idempotency, best metrics, resume clearing, canonical M02 reveal, and origin-preserving retry routes. Settlement `13 / 13`, progress regression `15 / 15`, Phase 7 `19 / 19`, source growth `17 / 17`, and architecture `23` suites pass with compiler zero and restored incidental outputs. | enclosing M01DC-024 acceptance commit | Accepted |
| 2026-08-13 | M01DC-025 | Added Full-mode mission guidance as one unmanaged projection reader over mission/objective/map state. Existing ARIA owners retain recommendation and typed Show Me/Do It command authority; guidance writes no gameplay truth. Guidance `7 / 7`, ARIA `10 / 10`, Phase 7 `19 / 19`, source growth `17 / 17`, architecture `23` suites, compiler zero, deterministic inventory, protected-path, and restored-output checks pass. | enclosing M01DC-025 acceptance commit | Accepted |
| 2026-08-13 | M01DC-026 | Added Contextual cooldown-based hint escalation and mandatory-information-only Minimal projection through the existing sole guidance writer. Cross-mode equal-action gameplay truth is unchanged. Guidance `10 / 10`, source growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, and restored-output checks pass. | enclosing M01DC-026 acceptance commit | Accepted |
| 2026-08-14 | M01DC-027 | Replaced the FirstLaunch Main Menu handoff with a persisted, typed, correlated M01 request for normal, skip, and resume paths. Completion waits for the sole launch owner's accepted result; retries are bounded and restart-safe. Focused `5 / 5`, Gate 89, PlayMode `1 / 1`, source growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, restored-output, and diff checks pass. | enclosing M01DC-027 acceptance commit | Accepted |
| 2026-08-14 | M01DC-028 | Authored deterministic brief/comms/debrief sub-sequences with exact mission references, Courier/Warden/Broker continuity, typed completion payloads, essential captions, reduced motion, Story Archive references, fragmentary revoked-credential evidence, and canonical first-clear M02 highlight context. Focused `8 / 8`, source growth `17 / 17`, architecture `23` suites, compiler zero, protected-path, restored-output, source/report diff, and hash-stable Unity-YAML checks pass. | enclosing M01DC-028 acceptance commit | Accepted |

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
