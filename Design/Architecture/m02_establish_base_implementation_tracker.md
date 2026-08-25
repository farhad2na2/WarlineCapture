# M02 Establish The Base Implementation Tracker

Date: 2026-08-25
Status: Active; M02EB-026 accepted and M02EB-027 dependency-ready
Progress: 26/34 accepted items (76.5%)
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

- [x] **M02EB-007 - Author canonical M02 mission definition**
  **Depends on:** M02EB-004 through M02EB-006.
  **Deliverable:** mission identity, objectives, stars, rewards, commands, sequences, replay, and readiness asset.
  **Acceptance:** exact ids and first-clear/replay rules validate; no unavailable resource or feature is required.
  **Evidence:** `[M02EstablishBaseConfigBuilder] result=Passed scope=MissionDefinition mission=saga.ch01.m02.establish_base`; `[M02EstablishBaseContractValidation] result=Passed tests=8`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors; logs `/private/tmp/warline-m02eb-007-builder.log`, `/private/tmp/warline-m02eb-007-contract.log`, `/private/tmp/warline-m02eb-007-m01-regression.log`, and `/private/tmp/warline-m02eb-007-source-growth.log`.

- [x] **M02EB-008 - Author canonical M02 scenario**
  **Depends on:** M02EB-005 through M02EB-007.
  **Deliverable:** deterministic friendly squad, forward post, Barracks footprint, resources, required unit, delayed patrol, restrictions, and civilians.
  **Acceptance:** all config/prefab/role/anchor references resolve and two serializations are stable.
  **Evidence:** canonical scenario SHA-256 `aefa375b500d5ae045724ce21268da99a2616ccb99c31c4b2486dbc25ab7f43f`; `[M02EstablishBaseCanonicalDataValidation] result=Passed tests=10`; `[M02EstablishBaseScenarioValidation] result=Passed tests=12`; `[M02EstablishBaseContractValidation] result=Passed tests=8`; `[M01FirstContactScenarioCompatibilityValidation] result=Passed tests=8`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors. The canonical data leaves 5,000 Credits and 10 Materials after the required Barracks and rifle order, fits the resolved 20x10 Barracks footprint inside a 24x14 lot, warns at 90 seconds, activates at 120 seconds, and regenerates byte-identically. Logs: `/private/tmp/warline-m02eb-008-builder.log`, `/private/tmp/warline-m02eb-008-canonical.log`, `/private/tmp/warline-m02eb-008-schema.log`, `/private/tmp/warline-m02eb-008-contract.log`, `/private/tmp/warline-m02eb-008-m01-compatibility.log`, `/private/tmp/warline-m02eb-008-m01-regression.log`, and `/private/tmp/warline-m02eb-008-source-growth.log`.

- [x] **M02EB-009 - Author logical forward-post operation map**
  **Depends on:** M02EB-003.
  **Deliverable:** exact source binding, map bounds, camera/minimap metadata, build surface, base, defense route, civilian, and narrative anchors.
  **Acceptance:** physical content remains unmodified; transformed bounds, placement, sightlines, and logical/source hashes validate.
  **Evidence:** logical map SHA-256 `1920a6d4a8566934324855156ff3287cfc0f259d976600416af3c6eaa090b8e1`; accepted physical definition SHA-256 `f91b737280d8950d97264b54589b963f605a8d8911a0f4e17397bef667e4eba6`, placements SHA-256 `f5d54abe4dca19b4b2deca889f46fb8196bef98e0e4ee7cb3daa511a2606358b`, and surface SHA-256 `1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d` remained exact. The logical window is `(780,270)-(1100,470)`, the deterministic clear Barracks lot is `(1018,392,24,14)`, and both camera shots, minimap projection, 14 safe-surface anchors, and defense-route sightlines validate. `[M02EstablishBaseForwardPostWindowValidation] result=Passed tests=9`; `[M02EstablishBaseOperationMapValidation] result=Passed tests=10`; `[M01FirstContactMapSourceBindingValidation] result=Passed tests=14`; `[M01FirstContactDenseCityReuseValidation] result=Passed tests=8`; `[OperationMapEcsContractValidation] result=Passed tests=7`; `[M01FirstContactCameraMinimapValidation] result=Passed tests=12`; `[M01FirstContactAnchorValidation] result=Passed tests=13`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors. Logs: `/private/tmp/warline-m02eb-009-builder-final.log`, `/private/tmp/warline-m02eb-009-focused-clean-generator.log`, `/private/tmp/warline-m02eb-009-reg-source-binding.log`, `/private/tmp/warline-m02eb-009-reg-dense-reuse.log`, `/private/tmp/warline-m02eb-009-reg-ecs-contract.log`, `/private/tmp/warline-m02eb-009-reg-camera-minimap.log`, `/private/tmp/warline-m02eb-009-reg-anchor.log`, and `/private/tmp/warline-m02eb-009-source-growth-final.log`.

- [x] **M02EB-010 - Catalog M02 and prove deterministic canonical data**
  **Depends on:** M02EB-007 through M02EB-009.
  **Deliverable:** Campaign mission/map/scenario catalog entries and consolidated data validator.
  **Acceptance:** M01 and M02 both resolve; duplicate/stale/missing identities fail closed; two-pass assets are byte stable.
  **Evidence:** the Chapter 1 mission catalog resolves exactly ordered `saga.ch01.m01.first_contact` and `saga.ch01.m02.establish_base`; the operation-map catalog resolves exactly ordered `opmap.ch01.district_edge_01` and `opmap.ch01.forward_post_01` with index-aligned built-in content packs. Canonical SHA-256 values after two-pass generation are mission `e2b9d68aee3e4d020cfb36d22439a86c71ee9d62e0c7cf71bfd1f23ca9603443`, scenario `aefa375b500d5ae045724ce21268da99a2616ccb99c31c4b2486dbc25ab7f43f`, map `1920a6d4a8566934324855156ff3287cfc0f259d976600416af3c6eaa090b8e1`, mission catalog `6810e32ea833e8199e72cf8cf68e86e97190377fd90cc5003e26fdb924d1322c`, and map catalog `a09971ed5e9ae2125ad04c7dcfed6059d1b0ee05f7545cd16658f818f8c68e15`. `[M02EstablishBaseConfigBuilder] result=Passed scope=Catalogs missions=2 maps=2`; `[M02EstablishBaseCanonicalDataValidation] result=Passed tests=15`; `[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors. Logs: `/private/tmp/warline-m02eb-010-consolidated-final2.log`, `/private/tmp/warline-m02eb-010-m01-regression.log`, and `/private/tmp/warline-m02eb-010-source-growth.log`.

### Phase C - Gameplay Vertical Slice

- [x] **M02EB-011 - Launch, retry, and replay M02 through the existing payload pipeline**
  **Depends on:** M02EB-010.
  **Acceptance:** Campaign deploy enters the correct map/session; retry preserves seed and increments attempt identity; M01 launch remains unchanged.
  **Evidence:** the Chapter 1 catalog now closes every mission over its canonical scenario, and the Menu bootstrap projects both definitions while retaining the serialized M01 fallback only when no chapter catalog is configured. Campaign selection and deploy publish `saga.ch01.m02.establish_base`, `scenario.ch01.m02.establish_base`, `opmap.ch01.forward_post_01`, seed `2002001`, and a bounded `campaign-m02-*` session through the existing payload, route, map-bootstrap, launch, retry, and replay owners. Payload schema validation remains independent from definition schema; valid caller-owned deterministic seeds survive launch/retry/replay, including M01's existing seed contract. Same-World M01-to-M02 replacement preserves one operation-map root and advances its generation; World recreation rebinds cleanly. Missing or duplicate scenarios, catalog misses, ambiguous identities, and wrong maps fail closed, while same-source-version content changes force exact reprojection across objectives, forces, routes, ambient presentation, stars, and rewards. Canonical catalog SHA-256 is `fb39e2ebd73cda13ebb5b20b39768d2941bc24d3a9dc89a9dc52a0a9f19cc9a1`; normalized default-safe M01 mission/scenario SHA-256 values are `b029211c747e5b9f9e9c3c655f2264da38b65bfbcfca13fac80b26cb4f886ca7` and `65ecdd65adef6a6992b5b72ccb817d382e57668db5490146e71ce25618090509`. `[M02EstablishBaseLaunchValidation] result=Passed tests=14`; `[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBaseLaunchRegressionValidation] result=Passed suites=3`; compiler and final Editor console errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-012 - Apply deterministic mission resources and restrictions**
  **Depends on:** M02EB-011.
  **Acceptance:** exact Credits/Materials initialize once; Fuel/Oil and unrelated controls stay hidden; retries reset attempt resources without mutating profile economy.
  **Evidence:** the sole Campaign mission projection now carries default-safe starting Credits, starting Materials, and mission-runtime enablement into one unmanaged attempt initializer. M02 applies exactly 55,000 Credits and 120 Materials once per session/attempt, clears mission Oil/Fuel, rearms on retry, and leaves profile persistence untouched; M01 remains disabled by default. The existing Match HUD projects Credits through its established resource header, hides Oil/logistics, Support, and unrelated squad controls only for M02, and preserves M01 visible-disabled behavior. `[M02EstablishBaseResourceValidation] result=Passed tests=9`; `[M02EstablishBaseLaunchValidation] result=Passed tests=14`; `[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBaseLaunchRegressionValidation] result=Passed suites=3`; compiler and final Editor console errors are zero. Unity CLI drove the wrapper-launched live Editor; log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-013 - Project the mission-scoped Barracks build catalog**
  **Depends on:** M02EB-006 and M02EB-012.
  **Acceptance:** Build Drawer exposes only Barracks for M02 and preserves normal Skirmish/full-catalog behavior elsewhere.
  **Evidence:** the canonical scenario build catalog is projected into the existing unmanaged Campaign mission blob and read through the established UI gateway. The Build Drawer wraps its existing prefab sources with a mission-scoped, fail-closed adapter: active M02 exposes exactly `Building_Barrack` with max count 1 and no unrelated unit/building catalog entries, while disabled mission runtime preserves the original M01/Skirmish lists exactly. Same-version build-catalog content changes force reprojection; a missing global Barracks definition fails closed. `[M02EstablishBaseBuildCatalogValidation] result=Passed tests=7`; `[BuildDrawerCatalogQueryValidation] result=Passed tests=25`; `[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5`; `[M02EstablishBaseLaunchValidation] result=Passed tests=14`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBaseBuildCatalogRegressionValidation] result=Passed suites=6`; compiler errors are zero. Unity CLI drove the wrapper-launched live Editor; log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-014 - Complete validated Barracks placement and construction**
  **Depends on:** M02EB-009 and M02EB-013.
  **Acceptance:** invalid placement is rejected visibly; valid placement spends resources once and creates the authoritative ECS/runtime building on the accepted surface.
  **Evidence:** the sole existing placement preview/confirm owner now applies the active Campaign mission's exact build zone and catalog before its canonical surface/occupancy validator. M02 resolves `anchor.ch01.m02.build_lot` through the active operation-map grid to `(1018,392,24,14)`, accepts only the 20x10 `Building_Barrack` fully contained in that lot, and fails closed on stale, missing, or ambiguous mission/map data; disabled mission runtime preserves unrestricted M01/Skirmish placement. The authoritative construction transaction now reserves, debits, finalizes, and rolls back both Credits and Materials exactly once before the existing ECS/runtime registration path. `[M02EstablishBasePlacementValidation] result=Passed tests=8`; `[BuildingPlacementConstructionTransactionValidation] result=Passed tests=6`; `[BuildingPlacementCommitFocusedValidation] result=Passed tests=5`; `[BuildingPlacementLiveOccupancyValidation] result=Passed tests=2`; `[M02EstablishBaseBuildCatalogValidation] result=Passed tests=7`; `[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBasePlacementRegressionValidation] result=Passed suites=8`; warm placement-policy allocation is zero and compiler errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-015 - Project Barracks completion into mission facts and objectives**
  **Depends on:** M02EB-014.
  **Acceptance:** the sole mission fact owner observes authoritative completion monotonically; UI/tutorial cannot forge it.
  **Evidence:** the existing attempt-facts schema now carries additive required-building placed/completed counts, while one Burst-capable unmanaged projection system resolves the exact `BuildStructure` target and count from the active mission definition. Each attempt captures the existing building-request maximum as a replay-safe baseline, then accepts only a post-baseline successful player-owned `KindBuilding` request for `Building_Barrack` that correlates to one live authoritative `RuntimeBuildingCombatInfo` entity with matching faction, runtime id, origin, footprint, and valid authored health. Successful requests without ECS buildings, ECS buildings without successful transactions, pre-attempt results, failed/wrong/non-player requests, and ambiguous objective definitions fail closed; accepted counts remain monotonic after later removal. Retry/session/source changes establish a new baseline, M01/default-disabled mission runtime remains unchanged, and the fact is ready for the sole objective writer's planned M02 generalization in M02EB-020 without adding a second writer. `[M02EstablishBaseObjectiveValidation] result=Passed tests=10`; `[M02EstablishBasePlacementValidation] result=Passed tests=8`; `[BuildingPlacementConstructionTransactionValidation] result=Passed tests=6`; `[BuildingPlacementCommitFocusedValidation] result=Passed tests=5`; `[BuildingPlacementLiveOccupancyValidation] result=Passed tests=2`; `[M02EstablishBaseBuildCatalogValidation] result=Passed tests=7`; `[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBasePlacementRegressionValidation] result=Passed suites=8`; `[M02EstablishBaseObjectiveRegressionValidation] result=Passed suites=2`; compiler errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-016 - Queue and complete the required rifle squad**
  **Depends on:** M02EB-006 and M02EB-015.
  **Acceptance:** affordability, queue, timer, spawn, faction, selection, read model, and one-time spend use existing production owners.
  **Evidence:** the existing unit-definition metadata owner now projects the canonical rifle's distinct 10,000-Credit and 20-Material costs while preserving the established Materials-facing catalog price. The sole camp production request path evaluates and spends both resources atomically through `RuntimeFactionResourceSystemHelper`, restores both exactly once after any rejected queue mutation, and preserves the legacy Materials-only fallback for callers without the additive transaction delegates. Both runtime-building and operation-map producer paths retain their established queue, five-second timer, ECS spawn, player-faction, focus/selection, and produced-unit read-model owners; no M02-only queue, scheduler, spawner, or resource ledger was added. `[M02EstablishBaseProductionValidation] result=Passed tests=8`; `[OperationMapCampProductionBridgeValidation] result=Passed tests=6`; `[EditorFirstProductionFunctionalBatchValidation] result=Passed suites=8 tests=96`; `[M02EstablishBaseObjectiveValidation] result=Passed tests=10`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBaseProductionRegressionValidation] result=Passed suites=5`; compiler errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-017 - Project produced-unit completion into mission facts and objectives**
  **Depends on:** M02EB-016.
  **Acceptance:** one authoritative produced-unit completion advances the objective once; destroyed/invalid/unrelated units do not.
  **Evidence:** the existing attempt-facts schema now carries the additive required-unit produced count and the sole Campaign mission fact projection resolves exactly one active `ProduceUnit` objective. Each attempt captures the append-only `BuildingProducedUnitReadModel` length as its replay-safe baseline, then accepts only distinct post-baseline player-owned rows whose source key matches the exact objective target and whose correlated authoritative ECS unit is live, non-prefab, player-faction, positively healthy, and carries the same `UnitSourcePrefabKey`. Pre-attempt, duplicate, destroyed, missing, wrong-faction, wrong-source, source-mismatched, and ambiguous-definition rows fail closed; accepted completion saturates at the required count and remains monotonic after later destruction. Retry/session/source changes establish a new baseline, M01/default-disabled mission runtime remains unchanged, and the sole objective writer remains reserved for M02EB-020. `[M02EstablishBaseObjectiveValidation] result=Passed tests=16`; `[M02EstablishBaseProductionValidation] result=Passed tests=8`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; `[M02EstablishBaseProductionRegressionValidation] result=Passed suites=5`; `[M02EstablishBaseObjectiveRegressionValidation] result=Passed suites=2`; compiler errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-018 - Activate the delayed patrol and warning**
  **Depends on:** M02EB-008 and M02EB-011.
  **Acceptance:** hostiles cannot move/target/fire before activation; one warning precedes activation; existing ECS AI/combat owns the active wave.
  **Evidence:** the existing Campaign catalog projection now carries the canonical delayed-wave group, route, target role, 90-second warning, and 120-second activation into unmanaged attempt state. The exact hostile roster spawns with movement and combat suppression, remains absent from enemy minimap/acquisition populations, and cannot take or deliver attack damage before activation. One dedicated Burst-capable system validates session, attempt, source version, route, faction, and exact roster count; it issues one existing ground-threat warning, then removes suppression through one ECB on a later update and restores normal `UnitCombat`/patrol/attack ownership. Retry resets the state, elapsed-time jumps still warn before activating, stale or ambiguous data fails closed, player minimap visibility and M01 opening suppression remain unchanged, and no parallel AI/combat path was added. `[M02EstablishBaseWaveValidation] result=Passed tests=9`; `[ThreatWarningValidation] result=Passed`; `[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=5`; `[M02EstablishBaseWaveRegressionValidation] result=Passed suites=5`; `[M02EstablishBaseLaunchValidation] result=Passed tests=14`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; compiler errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-019 - Defend the authoritative forward post**
  **Depends on:** M02EB-014 and M02EB-018.
  **Acceptance:** existing building health/destruction truth drives damage, defense objective, and defeat; no parallel base-health state exists.
  **Evidence:** the existing Campaign catalog projection now carries the canonical `role.friendly.forward_post` and `anchor.ch01.m02.forward_post` identities into unmanaged mission data. The sole attempt-fact projection binds that role to exactly one player-owned authoritative operation-map building whose physical source map and footprint contain the canonical base anchor, then derives monotonic bound/damaged/destroyed facts only from the building's existing `UnitHealth` and enableable `OperationMapBuildingDestroyedComponent`. Wrong-map, wrong-faction, outside-footprint, ambiguous-building, ambiguous-objective, stale-session, and conflicting-role candidates fail closed; retry rebinds the same building to the new attempt without copying health into a parallel base-health value. The forward-post logic was split into 384-line and 145-line partials to remain below the enforced production review threshold. `[M02EstablishBaseObjectiveValidation] result=Passed tests=22`; `[OperationMapBuildingDestructionValidation] result=Passed tests=4`; `[M02EstablishBaseObjectiveRegressionValidation] result=Passed suites=3`; `[M02EstablishBaseLaunchRegressionValidation] result=Passed suites=3`; `[M02EstablishBaseWaveValidation] result=Passed tests=9`; `[M02EstablishBaseWaveRegressionValidation] result=Passed suites=5`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; compiler errors are zero. Wrapper-launched live-Editor log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-020 - Generalize the sole objective projection writer**
  **Depends on:** M02EB-015, M02EB-017, and M02EB-019.
  **Acceptance:** M01 and M02 project definition/fact-driven objectives with monotonic versions and no second writer.
  **Evidence:** the existing `CampaignMissionObjectiveProjectionSystem` is now the sole definition-driven writer for both M01 and M02. It validates exact catalog/runtime source identity, authored objective order, unique ids, supported rule and target shape, required counts, and canonical anchors before replacing the objective buffer once. M01 retains its exact destroy/protect presentation and phase completion; M02 BuildStructure and ProduceUnit progress only from authoritative attempt facts, while DefendMissionRole publishes blocked, active, warning, or failed state from the bound/damaged/destroyed forward-post facts. Stale or regressive facts cannot advance progress, retry resets attempt progress while the published boundary version remains monotonic, and M01-to-M02 replacement leaves no stale rows. The shared fact projector now creates its queries through Burst-safe `EntityQueryBuilder` definitions rather than managed `ComponentType[]` allocation. `[M02EstablishBaseObjectiveWriterValidation] result=Passed tests=8`; `[M02EstablishBaseObjectiveValidation] result=Passed tests=22`; `[M02EstablishBaseObjectiveRegressionValidation] result=Passed suites=4`; `[M02EstablishBaseLaunchValidation] result=Passed tests=14`; `[M02EstablishBaseWaveValidation] result=Passed tests=9`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; live recompile completed with zero errors and the post-fix log slice contains no compiler, Burst, exception, or failed-validation marker. Unity CLI Pipeline drove the wrapper-launched live Editor; log: `/private/tmp/warline-m02eb-011-live-editor.log`.

- [x] **M02EB-021 - Resolve victory, defeat, stars, and settlement**
  **Depends on:** M02EB-017 through M02EB-020.
  **Acceptance:** all required objectives resolve deterministically; civilian and five-minute stars are independent; Barracks/M03 unlock and rewards settle exactly once.
  **Evidence:** the existing mission runtime remains the sole outcome writer and now resolves the canonical M02 definition only after Barracks completion, rifle production, forward-post survival, delayed-wave activation, and authoritative hostile defeat; destruction of the failure-on-break forward post resolves defeat. The sole result projector validates the authored objective schema against attempt facts, projects independent no-civilian-loss and five-minute stars, and records civilian losses. The existing settlement owner maps M02 to M03, while the progress store accepts only the exact M02 first-clear production-unlock reward, grants `Building_Barrack` once, converts an already-owned Barracks to one Training Facilities Blueprint Part once, and preserves duplicate-token idempotence across restart. `[M02EstablishBaseResultSettlementValidation] result=Passed tests=10`; `[M02EstablishBaseResultSettlementRegressionValidation] result=Passed suites=6`; `[M02EstablishBaseLaunchRegressionValidation] result=Passed suites=3`; `[M02EstablishBaseLaunchValidation] result=Passed tests=14`; `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23`; `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; final Unity CLI Pipeline recompile is up to date with zero compiler errors. The aggregate ECS/Burst debt probe still reports its pre-existing 26 array snapshots in seven unrelated `Assets/Game/Scripts/Systems/` files; no M02EB-021 write path uses those snapshot APIs, so this step adds zero debt. Pipeline log: `/private/tmp/warline-m02eb-021-pipeline-editor.log`.

- [x] **M02EB-022 - Prove lifecycle, retry, replay, and M01 compatibility**
  **Depends on:** M02EB-011 through M02EB-021.
  **Acceptance:** repeated M01/M02 runs, retry, exit, return, and World recreation have no stale facts, duplicate rewards, entities, handles, or presentation owners.
  **Evidence:** attempt cleanup now preserves accepted operation-map buildings while removing mission roles from them, destroys exact transient mission units and ambient civilians, queues the attempt-created Barracks through the canonical building-delete owner, destroys only post-baseline attempt-produced rifle entities, and clears warning, camera, resource, fact, delayed-wave, opening/finale, guidance/result, and command state. Retry settlement now derives first-clear versus replay rewards from the durable progress store, so retry-before-first-clear grants the first-clear reward exactly once and retry-after-clear grants replay rewards only. The existing result read model now resolves the exact mission from a multi-mission Chapter catalog instead of assuming catalog index zero. Unity CLI Pipeline 0.5.0-exp.1 drove a wrapper-launched warm Editor: recompile completed with zero errors; lifecycle 6/6, result settlement 12/12, launch 14/14, objective 22/22, building composition 2/2, and source-growth 17/17 passed. Final checked-wrapper evidence is `[M02EstablishBaseLifecycleValidation] result=Passed tests=14` in `/private/tmp/warline-m02eb-022-focused.log` and `[M02EstablishBaseLifecycleRegressionValidation] result=Passed suites=5` in `/private/tmp/warline-m02eb-022-regression.log`, including `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23` and `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`. The broader building-runtime suite remains 9/12 only because of its pre-existing authored `Tent_Regular` fixture expectations; every lifecycle/delete boundary test affected by this step passes and this task does not alter that authored-content debt.

### Phase D - Guidance, Narrative, And UI

- [x] **M02EB-023 - Present Campaign card and mission briefing**
  **Depends on:** M02EB-010.
  **Acceptance:** readable localized objective, resource, reward, restriction, map, and deploy truth appears with no raw keys or legacy M01 copy.
  **Evidence:** the existing Campaign projection, gateway, binder, and screen views now select either cataloged Chapter 1 mission through typed requests, reject unavailable M02 fail-closed, and present canonical M02 identity, three objectives, 55,000 Credits, 120 Materials, Barracks x1, transport/air restrictions, forward-post map identity, three delayed hostiles, first-clear rewards, replay state, and deploy action through resolver-backed text with no M01 copy or raw keys. Both shared prefabs were regenerated by their existing `Game/UI/...` builders through the warm Unity CLI Pipeline; no raw prefab YAML or parallel UI owner was added. Pipeline compile was up to date with zero errors and passed M02 campaign UI 8/8, M01 Campaign 10/10, M01 briefing 11/11, and production source-growth 17/17. Final checked-wrapper evidence is `[M02EstablishBaseCampaignUiValidation] result=Passed tests=8`, `[M01FirstContactCampaignUiValidation] result=Passed tests=10 captures=3`, `[M01FirstContactMissionBriefingValidation] result=Passed tests=11 captures=3`, `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`, and `[M02EstablishBaseCampaignUiRegressionValidation] result=Passed suites=4` in `/private/tmp/warline-m02eb-023-regression-final.log`. The Pipeline log is `/private/tmp/warline-m02eb-023-pipeline-final.log`; the shared Oxanium font atlas remained unchanged after replacing the temporary non-ASCII separator.

- [x] **M02EB-024 - Guide Build opening and Barracks selection**
  **Depends on:** M02EB-013 and M02EB-023.
  **Acceptance:** distinct typed steps, automatic indicators, SHOW ME, and DO IT target the correct controls without screen coordinates.
  **Evidence:** the sole Campaign guidance projection now publishes distinct M02 `EstablishBaseOpenBuild` and `EstablishBaseSelectBarracks` prompts as typed `UiSurface` recommendations targeting `ui.match.build` and `ui.build_drawer.barracks`. Automatic cues, SHOW ME, and DO IT bind the existing Build and Barracks `Button` instances and invoke their real `onClick` paths without screen coordinates, synthetic placement, or parallel UI ownership. The Build drawer acknowledges opening only after its real `OnEnable`, suppresses legacy first-item auto-selection for the M02 steps, and advances Barracks selection through the existing guidance acknowledgment buffer. Guarded production owners remain at or below their ratcheted ceilings through narrow partials. Unity Pipeline passed M02 guidance 11/11, Build catalog 7/7, Match HUD 21/21, and source-growth architecture 17/17. Final checked-wrapper evidence is `[M02EstablishBaseGuidanceValidation] result=Passed tests=11`, `[M01FirstContactGuidanceValidation] result=Passed tests=14`, `[M02EstablishBaseBuildCatalogValidation] result=Passed tests=7`, `[MatchHudAssistantUiValidation] result=Passed tests=21`, `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`, and `[M02EstablishBaseGuidanceRegressionValidation] result=Passed suites=5` with zero compiler errors in `/private/tmp/warline-m02eb-024-regression.log`.

- [x] **M02EB-025 - Guide footprint placement and resource spend**
  **Depends on:** M02EB-014 and M02EB-024.
  **Acceptance:** placement highlight, valid/invalid feedback, displayed cost, spoken/displayed copy, and DO IT use the real command path.
  **Evidence:** the sole Campaign guidance owner now advances from Barracks selection to the exact `anchor.ch01.m02.build_lot`, while the established placement lifecycle remains the sole owner of the green/red footprint, blocked/invalid feedback, confirmation availability, and active definition. DO IT invokes the real Build Drawer `PLACE` button and then the real confirmation-bar `CONFIRM` button; the authoritative construction transaction remains the sole writer and spends 40,000 Credits plus 90 Materials atomically. The confirmation bar displays `40,000 CR / 90 MAT`, the real resource strip receives a typed CONTINUE review step, and the M02 count-nine sequence is isolated from M01 text/audio until final bilingual copy and voice production at M02EB-031/032. Guarded helper files stay within their exact ratcheted line/byte ceilings through the narrow `BuildingUiPlacementCostReadModel` and M02 guidance partial. Final checked-wrapper evidence in `/private/tmp/warline-m02eb-025-regression-final2.log` is `[M02EstablishBaseGuidanceValidation] result=Passed tests=17`, `[M02EstablishBasePlacementValidation] result=Passed tests=8`, `[BuildingPlacementConstructionTransactionValidation] result=Passed tests=6`, `[BuildingConstructionResourceTransactionValidation] result=Passed tests=3`, `[M01FirstContactGuidanceValidation] result=Passed tests=14`, `[M02EstablishBaseBuildCatalogValidation] result=Passed tests=7`, `[MatchHudAssistantUiValidation] result=Passed tests=21`, `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`, and `[M02EstablishBaseGuidanceRegressionValidation] result=Passed suites=8` with zero compiler errors.

- [x] **M02EB-026 - Guide rifle production**
  **Depends on:** M02EB-016 and M02EB-025.
  **Acceptance:** selecting Barracks and queueing the unit are separate steps with matching English/Farsi text and typed assistance.
  **Evidence:** the sole Campaign guidance owner now advances to rifle production only after the authoritative completed-Barracks fact. The mission-scoped Build Drawer resolves the exact canonical `ProduceUnit` target, exposes only that rifle after completion, and keeps Barracks selection and rifle queueing as distinct typed targets with exact English/Persian display copy. Automatic cues, SHOW ME, and DO IT bind the existing Soldiers tab, rifle catalog item, and RECRUIT button; DO IT invokes their real `Button.onClick` paths and guidance acknowledges only after the established dual-resource `TryRequestCampItem` transaction accepts the request. No parallel queue, resource writer, produced-unit fact writer, or provisional M01 voice reuse was added. The frozen Build Drawer owner remains at 426 lines and 20,180 bytes. Final checked-wrapper evidence in `/private/tmp/warline-m02eb-026-regression-final.log` is `[M02EstablishBaseGuidanceValidation] result=Passed tests=22`, `[M02EstablishBasePlacementValidation] result=Passed tests=8`, `[BuildingPlacementConstructionTransactionValidation] result=Passed tests=6`, `[BuildingConstructionResourceTransactionValidation] result=Passed tests=3`, `[M01FirstContactGuidanceValidation] result=Passed tests=14`, `[M02EstablishBaseBuildCatalogValidation] result=Passed tests=8`, `[M02EstablishBaseProductionValidation] result=Passed tests=8`, `[MatchHudAssistantUiValidation] result=Passed tests=21`, `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`, and `[M02EstablishBaseGuidanceRegressionValidation] result=Passed suites=9` with zero compiler errors.

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
| 2026-08-24 | M02EB-007 authored the canonical M02 definition with three ordered objectives, three independent stars, explicit first-clear/replay rewards, Build-enabled commands, stable sequences, and only established readiness features. | Accepted |
| 2026-08-24 | M02EB-008 authored the deterministic M02 scenario with exact approved units, a bounded Barracks lot, positive post-action resource float, suppressed delayed patrol timing, transport/air restrictions, civilians, closed anchor references, and byte-stable regeneration. | Accepted |
| 2026-08-24 | M02EB-009 bound a cropped logical forward-post mission window to the exact accepted dense-city EntityScene, surface, minimap raster, and building placements; its deterministic lot, anchors, route, cameras, and all affected M01/architecture regressions passed without changing physical content. | Accepted |
| 2026-08-25 | M02EB-010 made the existing Chapter 1 Editor builder the deterministic merge-and-sort owner for mission and map catalogs, added exact M01/M02 graph validation, and proved duplicate, missing, stale, and cross-builder preservation failures close safely. | Accepted |
| 2026-08-25 | M02EB-011 generalized the sole Campaign catalog, selection, payload, map-bootstrap, and launch owners to M02; exact catalog reprojection, typed deploy/retry/replay, same-World map generation, World recreation, fail-closed identity checks, caller-seed preservation, and M01 compatibility passed without adding a parallel mission pipeline. | Accepted |
| 2026-08-25 | M02EB-012 added one attempt-scoped ECS resource initializer and default-safe HUD restrictions: M02 receives exact Credits/Materials once per attempt, retry rearms without profile mutation, logistics and unrelated controls hide, and shared M01/source-growth contracts remain intact. | Accepted |
| 2026-08-25 | M02EB-013 projected the canonical Barracks-only mission catalog through the existing Campaign blob/UI gateway and applied a bounded reusable Build Drawer source filter; unrestricted catalogs remain exact, missing definitions fail closed, and all six focused/shared regression suites pass. | Accepted |
| 2026-08-25 | M02EB-014 routed the exact mission build lot through the existing placement preview/confirm owner, rejected out-of-zone and stale-data placement visibly, corrected the authoritative transaction to spend and roll back both Credits and Materials exactly once, and passed all eight focused/shared suites without source-growth regression. | Accepted |
| 2026-08-25 | M02EB-015 added one unmanaged, attempt-correlated Barracks fact projection that requires a post-baseline successful authoritative building transaction plus its matching live ECS building, advances monotonically, rejects stale/forged/unrelated state, and preserves the sole objective writer for M02EB-020. | Accepted |
| 2026-08-25 | M02EB-016 routed the required rifle through the existing production owners with exact dual-resource preflight/spend/rollback, five-second queue completion, authoritative ECS spawn/faction/read-model projection, and no parallel M02 production implementation. | Accepted |
| 2026-08-25 | M02EB-017 projected exact produced-rifle completion from the append-only production read model into attempt facts using a retry-safe baseline and live ECS source/faction/health correlation; invalid, duplicate, stale, destroyed, and unrelated units fail closed while accepted progress stays monotonic. | Accepted |
| 2026-08-25 | M02EB-018 added one exact delayed-wave lifecycle owner: the canonical patrol is movement/combat/minimap suppressed until one 90-second warning and later 120-second activation release it into the existing patrol, targeting, attack, health, and death systems; stale identity and roster ambiguity fail closed. | Accepted |
| 2026-08-25 | M02EB-019 bound the canonical forward-post role to the unique authoritative operation-map building at the base anchor and projected only monotonic damage/destruction facts from existing building health and destruction truth; no parallel base-health state or combat path was added. | Accepted |
| 2026-08-25 | M02EB-020 generalized the sole objective projection writer across M01 and M02, preserving exact M01 behavior while projecting ordered Build, Produce, and Defend state from authoritative facts with retry-safe monotonic publication and fail-closed schema/source validation. | Accepted |
| 2026-08-25 | M02EB-021 generalized the sole runtime/result/settlement path for canonical M02 victory, forward-post defeat, independent civilian/time stars, exact Barracks reward handling, and one-time M03 progression without adding a parallel mission or persistence owner. | Accepted |
| 2026-08-25 | M02EB-022 proved exact retry/replay/exit/return cleanup, canonical attempt-building deletion, durable first-clear settlement, multi-mission result resolution, World recreation, and M01 compatibility through the warm Unity CLI Pipeline and final checked-wrapper regression gates. | Accepted |
| 2026-08-25 | M02EB-023 generalized the existing Campaign card and briefing projection/view/binder path to exact M02 catalog truth, regenerated both shared prefabs through Unity Pipeline, preserved locked-mission fail-closed behavior and M01 presentation, and passed focused UI plus production architecture gates. | Accepted |
| 2026-08-25 | M02EB-024 added typed Build and Barracks UI-surface guidance through the existing Campaign/ARIA owners, bound automatic cues and DO IT to the real controls, preserved explicit catalog selection, and passed Pipeline plus checked-wrapper M01/UI/architecture regressions. | Accepted |
| 2026-08-25 | M02EB-025 bound the canonical Barracks footprint, real PLACE/CONFIRM controls, exact dual-resource cost display, and resource-strip review to the existing placement and transaction owners; M02 cannot borrow M01 tutorial copy/audio, and all eight functional/architecture suites pass. | Accepted |
| 2026-08-25 | M02EB-026 exposed the exact canonical rifle only after Barracks completion, bound typed assistance to the real Soldiers/item/RECRUIT controls, acknowledged only accepted production transactions, preserved distinct English/Persian Barracks and rifle prompts, and passed all nine functional/architecture suites. | Accepted |

## 8. Current Validation And Blockers

| Item | Result | Evidence |
|---|---|---|
| M02EB-026 rifle-production guidance | Passed | Checked M02 guidance 22/22, Build catalog 8/8, and production 8/8; exact rifle availability follows authoritative Barracks completion and the real RECRUIT transaction remains the sole queue/resource path |
| Shared M01 guidance | Passed | Checked `[M01FirstContactGuidanceValidation] result=Passed tests=14`; M01 prompt sequencing and world-target behavior remain unchanged |
| Architecture/source growth and compilation | Passed | Checked `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17`; zero compiler errors and the frozen Build Drawer owner remains at 426 lines and 20,180 bytes |
| Checked wrapper workflow | Passed | `Tools/CI/invoke_unity_macos.sh` drove the final nine-suite aggregate and required every functional plus architecture marker in `/private/tmp/warline-m02eb-026-regression-final.log` |
| Building authored-content aggregate | Pre-existing debt, no task regression | `BuildingRuntimeValidationTests` remains 9/12 on the existing authored `Tent_Regular` fixture expectations; all affected canonical bootstrap/delete boundary tests pass |
| Aggregate ECS/Burst debt probe | Pre-existing debt, no task regression | Current snapshot calls remain confined to unrelated established systems; M02EB-022 adds no managed hot-loop snapshot path |

No blocker prevents M02EB-027. Android/Samsung certification remains owner-deferred and is not counted as passed. Final bilingual voice, comic, and polished copy production remains gated by M02EB-029 through M02EB-032; M02EB-026 supplies exact English/Persian display copy while intentionally preventing provisional M02 steps from resolving M01 media. The pre-existing aggregate ECS/Burst and authored Tent fixture debts remain closeout concerns and are not widened by M02EB-026.
