# M02 Establish The Base Implementation Tracker

Date: 2026-08-24
Status: Active; M02EB-006 accepted and M02EB-007 plus M02EB-009 dependency-ready
Progress: 6/34 accepted items (17.6%)
Parent design: `Design/SagaChapters/Saga_Chapter01_First_Response.md` (`M02 Detailed Spec`)
Technical architecture: `Design/Architecture/m02_establish_base_technical_architecture.md`
Mission: `saga.ch01.m02.establish_base`
Scenario: `scenario.ch01.m02.establish_base`
Logical operation map: `opmap.ch01.forward_post_01`

## 1. Purpose

This tracker delivers one reviewable Chapter 1 Mission 2 vertical slice using the accepted Campaign, ECS gameplay, dense-city, building, production, resource, UI-shell, narrative, and progression owners. Work proceeds in dependency order. Each item closes only with focused evidence, affected regression coverage, an honest tracker update, and a bounded commit.

The first project-owner review gate is M02EB-029. Final comic and voice production must not begin before that playable timing review is accepted.

## 2. Product Contract

1. Barracks is the sole M02 tutorial building and canonical rifle producer.
2. Mission-scoped Barracks access becomes a permanent unlock only on first clear.
3. The player places the Barracks in the world, spends real mission Credits/Materials, and queues one real rifle squad.
4. One delayed patrol threatens the forward post. Existing ECS movement, targeting, combat, health, and death systems own the encounter.
5. Victory requires Barracks completion, one produced rifle squad, and survival of the forward post through the defense wave.
6. Stars are completion, zero civilian losses, and build/produce completion under five minutes.
7. Retry does not persist resource spend or first-clear rewards. Settlement remains idempotent.
8. Dalia is introduced as the recurring field lead. ARIA and Samira retain their existing identities and voices.
9. M02 reuses accepted physical map content through a logical operation-map binding; it does not fork the dense city.
10. Only final comic/audio production waits for playable approval. Story copy and storyboard ids are authored earlier.
11. Samsung/Android certification is deferred by the project owner and is not counted as passed.

## 3. Ownership Boundaries

### Owned Families

- `Assets/Game/Configs/Campaign/`
- `Assets/Game/Configs/Missions/Chapter01/`
- `Assets/Game/Configs/Scenarios/Chapter01/`
- `Assets/Game/Configs/OperationMaps/Chapter01/`
- the exact Barracks definition/catalog paths approved by M02EB-003;
- bounded mission contract/component/runtime files needed to generalize the existing single owners;
- existing Campaign, HUD, Build Drawer, ARIA, narrative, and result readers for typed M02 projections;
- `Assets/Tests/Editor/` and `Assets/Tests/PlayMode/` M02-focused tests;
- `Design/AgentReports/M02EstablishBase/` compact evidence;
- this tracker, its technical architecture, and directly affected design authorities.

### Read-Only Families

- accepted dense-city source scenes and candidate SubScenes;
- baked/static/virtualized render databases and generated proxy assets;
- production Addressables groups and rollback packages;
- unrelated Skirmish maps/scenarios/defaults;
- packages, ProjectSettings, CI/Jenkins, Unity installations, and performance thresholds;
- M03-M05 implementation and unrelated gameplay/economy systems.

Every item records an exact path allowlist before editing. Unexpected user changes are preserved.

## 4. Execution And Acceptance Rules

1. Work only from current `main`; do not use the obsolete `WarlineCapture-CodexUnity1` project.
2. Use `Tools/CI/invoke_unity_macos.sh` for all Unity validation and follow `AGENTS.md` licensing rules.
3. Do not invoke Android builds or Samsung validation while the owner deferral is active.
4. Preserve exactly one mission phase/outcome writer, one objective projection writer, one progression settlement owner, and the existing building/production/resource owners.
5. No managed allocations after warmup, structural changes inside entity iteration, managed runtime map visuals, per-frame hierarchy search, or parallel gameplay implementation.
6. Validate changed behavior plus M01 regressions at every shared boundary.
7. Do not produce final comics or voices before M02EB-029 acceptance.
8. A checkbox closes only when its acceptance and evidence exist. Deferred work stays unchecked.

## 5. Dependency-Ordered Checklist

### Phase A - Authority And Inventory

- [x] **M02EB-001 - Accept M02 scope, architecture, and expansion governance**
  **Deliverable:** reconcile the owner-authorized M01 device deferral, canonical Barracks decision, M02 architecture, tracker, and coordination priority.
  **Acceptance:** authorities agree; M01 open gates remain honest; M02 is authorized without weakening architecture/performance contracts.
  **Evidence:** focused documentation diff, consistency scan, clean bounded commit.

- [x] **M02EB-002 - Inventory exact current-head reuse seams**
  **Depends on:** M02EB-001.
  **Deliverable:** map exact mission, scenario, map, building, production, resource, combat, UI, ARIA, narrative, persistence, tests, and source-growth owners.
  **Acceptance:** every required behavior has one existing owner or one explicit additive gap; no parallel framework is proposed.
  **Evidence:** `Design/AgentReports/M02EstablishBase/m02eb_002_current_head_inventory.md`.

- [x] **M02EB-003 - Freeze exact ownership, rollback, and validation matrix**
  **Depends on:** M02EB-002.
  **Deliverable:** exact path allowlists, exclusions, expected tests/pass markers, generated-output policy, and rollback for M02EB-004 through M02EB-034.
  **Acceptance:** no wildcard ownership or protected-path overlap; source-growth ceilings and shared regressions are explicit.
  **Evidence:** checked matrix under `Design/AgentReports/M02EstablishBase/`.

### Phase B - Contracts And Canonical Data

- [x] **M02EB-004 - Add default-safe M02 objective and star rules**
  **Depends on:** M02EB-003.
  **Deliverable:** data-supported BuildStructure, ProduceUnit, DefendMissionRole, and NoCivilianLoss rules.
  **Acceptance:** M01 behavior is byte/behavior compatible; invalid/ambiguous rules fail closed; focused contract and architecture suites pass.
  **Evidence:** `[M02EstablishBaseMissionRuleValidation] result=Passed tests=11`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors; logs `/private/tmp/warline-m02eb-004-rules.log`, `/private/tmp/warline-m02eb-004-m01-regression.log`, and `/private/tmp/warline-m02eb-004-source-growth.log`.

- [x] **M02EB-005 - Add default-safe scenario economy/build/base schema**
  **Depends on:** M02EB-003.
  **Deliverable:** starting resources, mission build catalog, required producer/unit, base role, and delayed-wave data.
  **Acceptance:** legacy Skirmish defaults remain unchanged; M02 affordability and identity validate deterministically.
  **Evidence:** `[M02EstablishBaseScenarioValidation] result=Passed tests=12`; `[M01FirstContactScenarioCompatibilityValidation] result=Passed tests=8`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; production files split to 357/120/109 lines; zero compiler errors; logs `/private/tmp/warline-m02eb-005-scenario-split.log`, `/private/tmp/warline-m02eb-005-m01-scenario-split.log`, and `/private/tmp/warline-m02eb-005-source-growth.log`.

- [x] **M02EB-006 - Make Barracks the bounded canonical rifle producer**
  **Depends on:** M02EB-005.
  **Deliverable:** reviewed Barracks production entry and mission filter for one required rifle squad.
  **Acceptance:** Barracks can queue/spawn the approved unit through existing production ownership; Tent/Road Barrier remain absent from M02; catalog regressions pass.
  **Evidence:** `[M02EstablishBaseConfigBuilder] result=Passed scope=BarracksProduction entries=1 unit=Unit_Chr_Soldier_Male_02_Alt_04`; `[M02EstablishBaseBarracksProductionValidation] result=Passed tests=4`; `[EditorFirstProductionFunctionalBatchValidation] result=Passed suites=8 tests=93`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors; logs `/private/tmp/warline-m02eb-006-builder.log`, `/private/tmp/warline-m02eb-006-focused.log`, `/private/tmp/warline-m02eb-006-production-regression.log`, and `/private/tmp/warline-m02eb-006-source-growth.log`.

- [ ] **M02EB-007 - Author canonical M02 mission definition**
  **Depends on:** M02EB-004 through M02EB-006.
  **Deliverable:** mission identity, objectives, stars, rewards, commands, sequences, replay, and readiness asset.
  **Acceptance:** exact ids and first-clear/replay rules validate; no unavailable resource or feature is required.

- [ ] **M02EB-008 - Author canonical M02 scenario**
  **Depends on:** M02EB-005 through M02EB-007.
  **Deliverable:** deterministic friendly squad, forward post, Barracks footprint, resources, required unit, delayed patrol, restrictions, and civilians.
  **Acceptance:** all config/prefab/role/anchor references resolve and two serializations are stable.

- [ ] **M02EB-009 - Author logical forward-post operation map**
  **Depends on:** M02EB-003.
  **Deliverable:** exact source binding, map bounds, camera/minimap metadata, build surface, base, defense route, civilian, and narrative anchors.
  **Acceptance:** physical content remains unmodified; transformed bounds, placement, sightlines, and logical/source hashes validate.

- [ ] **M02EB-010 - Catalog M02 and prove deterministic canonical data**
  **Depends on:** M02EB-007 through M02EB-009.
  **Deliverable:** Campaign mission/map/scenario catalog entries and consolidated data validator.
  **Acceptance:** M01 and M02 both resolve; duplicate/stale/missing identities fail closed; two-pass assets are byte stable.

### Phase C - Gameplay Vertical Slice

- [ ] **M02EB-011 - Launch, retry, and replay M02 through the existing payload pipeline**
  **Depends on:** M02EB-010.
  **Acceptance:** Campaign deploy enters the correct map/session; retry preserves seed and increments attempt identity; M01 launch remains unchanged.

- [ ] **M02EB-012 - Apply deterministic mission resources and restrictions**
  **Depends on:** M02EB-011.
  **Acceptance:** exact Credits/Materials initialize once; Fuel/Oil and unrelated controls stay hidden; retries reset attempt resources without mutating profile economy.

- [ ] **M02EB-013 - Project the mission-scoped Barracks build catalog**
  **Depends on:** M02EB-006 and M02EB-012.
  **Acceptance:** Build Drawer exposes only Barracks for M02 and preserves normal Skirmish/full-catalog behavior elsewhere.

- [ ] **M02EB-014 - Complete validated Barracks placement and construction**
  **Depends on:** M02EB-009 and M02EB-013.
  **Acceptance:** invalid placement is rejected visibly; valid placement spends resources once and creates the authoritative ECS/runtime building on the accepted surface.

- [ ] **M02EB-015 - Project Barracks completion into mission facts and objectives**
  **Depends on:** M02EB-014.
  **Acceptance:** the sole mission fact owner observes authoritative completion monotonically; UI/tutorial cannot forge it.

- [ ] **M02EB-016 - Queue and complete the required rifle squad**
  **Depends on:** M02EB-006 and M02EB-015.
  **Acceptance:** affordability, queue, timer, spawn, faction, selection, read model, and one-time spend use existing production owners.

- [ ] **M02EB-017 - Project produced-unit completion into mission facts and objectives**
  **Depends on:** M02EB-016.
  **Acceptance:** one authoritative produced-unit completion advances the objective once; destroyed/invalid/unrelated units do not.

- [ ] **M02EB-018 - Activate the delayed patrol and warning**
  **Depends on:** M02EB-008 and M02EB-011.
  **Acceptance:** hostiles cannot move/target/fire before activation; one warning precedes activation; existing ECS AI/combat owns the active wave.

- [ ] **M02EB-019 - Defend the authoritative forward post**
  **Depends on:** M02EB-014 and M02EB-018.
  **Acceptance:** existing building health/destruction truth drives damage, defense objective, and defeat; no parallel base-health state exists.

- [ ] **M02EB-020 - Generalize the sole objective projection writer**
  **Depends on:** M02EB-015, M02EB-017, and M02EB-019.
  **Acceptance:** M01 and M02 project definition/fact-driven objectives with monotonic versions and no second writer.

- [ ] **M02EB-021 - Resolve victory, defeat, stars, and settlement**
  **Depends on:** M02EB-017 through M02EB-020.
  **Acceptance:** all required objectives resolve deterministically; civilian and five-minute stars are independent; Barracks/M03 unlock and rewards settle exactly once.

- [ ] **M02EB-022 - Prove lifecycle, retry, replay, and M01 compatibility**
  **Depends on:** M02EB-011 through M02EB-021.
  **Acceptance:** repeated M01/M02 runs, retry, exit, return, and World recreation have no stale facts, duplicate rewards, entities, handles, or presentation owners.

### Phase D - Guidance, Narrative, And UI

- [ ] **M02EB-023 - Present Campaign card and mission briefing**
  **Depends on:** M02EB-010.
  **Acceptance:** readable localized objective, resource, reward, restriction, map, and deploy truth appears with no raw keys or legacy M01 copy.

- [ ] **M02EB-024 - Guide Build opening and Barracks selection**
  **Depends on:** M02EB-013 and M02EB-023.
  **Acceptance:** distinct typed steps, automatic indicators, SHOW ME, and DO IT target the correct controls without screen coordinates.

- [ ] **M02EB-025 - Guide footprint placement and resource spend**
  **Depends on:** M02EB-014 and M02EB-024.
  **Acceptance:** placement highlight, valid/invalid feedback, displayed cost, spoken/displayed copy, and DO IT use the real command path.

- [ ] **M02EB-026 - Guide rifle production**
  **Depends on:** M02EB-016 and M02EB-025.
  **Acceptance:** selecting Barracks and queueing the unit are separate steps with matching English/Farsi text and typed assistance.

- [ ] **M02EB-027 - Guide warning and defense without taking combat control**
  **Depends on:** M02EB-018 through M02EB-021 and M02EB-026.
  **Acceptance:** ARIA hides between completed steps, warns before contact, identifies the defense lane, and leaves tactical decisions to the player.

- [ ] **M02EB-028 - Integrate result, debrief, progression, and M03 reveal**
  **Depends on:** M02EB-021 and M02EB-027.
  **Acceptance:** result/debrief truth matches settlement; Dalia accepts field-lead role; the dark warning sector points to M03; replay returns correctly.

### Phase E - Review And Media

- [ ] **M02EB-029 - Deliver the playable Editor vertical slice for project-owner review**
  **Depends on:** M02EB-022 through M02EB-028.
  **Deliverable:** graphics-enabled full-screen M02 run from Campaign briefing through build, produce, defend, result, and debrief with provisional storyboard presentation.
  **Acceptance:** automated gates pass; the visible map, camera, controls, tutorial, pacing, combat, and transitions are reviewable; project owner accepts timing or supplies findings.
  **Stop rule:** do not generate final comic panels or voices before this item is accepted.

- [ ] **M02EB-030 - Author and approve final comic panel set**
  **Depends on:** M02EB-029.
  **Acceptance:** brief/debrief panels follow approved beats, preserve Dalia/ARIA/Samira continuity, fit target aspect ratios, and contain no baked text.

- [ ] **M02EB-031 - Author exact bilingual narrative/tutorial text**
  **Depends on:** M02EB-029.
  **Acceptance:** English and Persian are natural, complete, semantically equivalent, correctly shaped, and mapped one-to-one to typed steps/sequences.

- [ ] **M02EB-032 - Produce and integrate final bilingual voices**
  **Depends on:** M02EB-031.
  **Acceptance:** established character voices are used; displayed and spoken messages match; imports are deterministic; missing locale falls back safely.

### Phase F - Final Editor Acceptance

- [ ] **M02EB-033 - Pass integrated correctness, architecture, lifecycle, and performance gates**
  **Depends on:** M02EB-030 through M02EB-032.
  **Acceptance:** compiler zero; focused M02 and affected M01/build/production/resource/combat/UI tests pass; architecture/source-growth pass; representative route meets allocation and performance contracts.

- [ ] **M02EB-034 - Final Editor handoff and documentation reconciliation**
  **Depends on:** M02EB-033.
  **Acceptance:** all Editor findings are closed or explicitly deferred; authorities and evidence agree; main equals origin/main; repository contains no temporary output. Android certification remains explicitly deferred and no completion claim includes it.

## 6. Review Path

The first user review occurs at M02EB-029:

1. Campaign Operations -> Mission 2 briefing;
2. Deploy into the forward post;
3. follow ARIA to open Build and select Barracks;
4. place Barracks on the highlighted valid footprint;
5. observe Credits/Materials spend;
6. queue and complete one rifle squad;
7. respond to the warning and defend the post;
8. inspect result/debrief and M03 reveal.

## 7. Decision Log

| Date | Decision | Status |
|---|---|---|
| 2026-08-24 | Project owner deferred Samsung validation and authorized Mission 2 start. | Accepted expansion authority; M01 device gates remain open |
| 2026-08-24 | `Building_Barrack` is the sole M02 tutorial producer; Tent and Road Barrier are excluded. | Accepted |
| 2026-08-24 | Final comic and voice production follows playable vertical-slice timing approval. | Accepted |
| 2026-08-24 | M02EB-001 documentation consistency gate passed with 34 ordered/unique items and zero unresolved producer-decision text. | Accepted |
| 2026-08-24 | M02EB-002 current-head inventory mapped every required M02 behavior to an existing sole owner or explicit additive gap; Unity CLI 1.0.0-beta.6 was available but no live Editor was connected. | Accepted |
| 2026-08-24 | M02EB-003 froze exact write paths, protected dense-city exclusions, generated-output policy, rollback, pass markers, M01 regressions, and source-growth/performance ceilings. | Accepted |
| 2026-08-24 | M02EB-004 appended three objective rules and one star rule without renumbering M01, added explicit role-versus-config target identity, and passed focused M02, full M01 contract, and source-growth gates. | Accepted |
| 2026-08-24 | M02EB-005 added an opt-in scenario mission-runtime block for resources, build catalog/zone, producer/unit, base, and delayed wave; legacy defaults remain disabled and source growth passes without exception. | Accepted |
| 2026-08-24 | M02EB-006 made the canonical Barracks config expose exactly `Unit_Chr_Soldier_Male_02_Alt_04`; the existing production pipeline passed its 93-test functional batch while Tent and Road Barrier remained unchanged. | Accepted |
