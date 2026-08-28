# M02EB-002 Current-Head Reuse Inventory

Date: 2026-08-24
Head inspected: `09458a841dcaa772d70802c8fecefaaa5901587e`
Mission: `saga.ch01.m02.establish_base`
Scenario: `scenario.ch01.m02.establish_base`
Logical operation map: `opmap.ch01.forward_post_01`
Result: Passed; each required M02 behavior has one existing owner or one explicit additive gap.

## 1. Authority And Canonical Identities

| Concern | Existing authority | M02 decision or additive gap |
|---|---|---|
| Mission product contract | `Design/SagaChapters/Saga_Chapter01_First_Response.md` | Implement the detailed M02 build-produce-defend contract without changing M01 device-gate status. |
| Technical ownership | `Design/Architecture/m02_establish_base_technical_architecture.md` | Use the existing Campaign, Match, building, production, combat, UI, narrative, and settlement owners. |
| Tutorial producer | `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset` | `Building_Barrack` is the only M02 build option and must gain one bounded rifle production entry. |
| Tutorial unit | `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset` | Use `Unit_Chr_Soldier_Male_02_Alt_04`; it is the established fallback rifle unit and has complete world/UI presentation. |
| Excluded producers | `Prefab_BuildingDefinition_Tent_Regular_Config.asset`; `Prefab_BuildingDefinition_Road_Barrier_Config.asset` | Do not expose either in M02 and do not retain Tent as the tutorial producer. |

The Barracks currently costs 40,000 Credits, has a 30-second construction duration, and has an empty production list. The approved rifle unit costs 10,000 Credits. M02 starting resources and Materials costs remain data to freeze in M02EB-005 after affordability and pacing validation.

## 2. Mission, Scenario, Catalog, And Progression

| Behavior | Existing sole owner | Reuse | Explicit additive gap |
|---|---|---|---|
| Mission definition schema | `Assets/Game/Scripts/Missions/Contracts/MissionContracts.cs`; `Assets/Game/Scripts/Configs/MissionDefinitionConfig.cs` | Stable ids, objectives, stars, rewards, commands, sequence ids, and readiness | Add default-safe BuildStructure, ProduceUnit, DefendMissionRole, and NoCivilianLoss rule kinds. |
| Scenario schema | `Assets/Game/Scripts/Configs/ScenarioSetupConfig.cs` | Existing faction, unit, role, restriction, and operation-map references | Add default-safe starting economy, mission build catalog, required producer/unit, base role, build zone, and delayed-wave data. |
| Runtime mission database | `Assets/Game/Scripts/Components/CampaignMissionComponents.cs`; `Assets/Game/Scripts/Composition/CampaignMissionCatalogProjection.cs` | Retain one ECS mission database owner | Generalize the one-entry projection to deterministic arrays and retain briefing/comms/debrief and next-mission ids. |
| Campaign mission UI | `Assets/Game/Scripts/UI/Shell/Ecs/UiCampaignMissionProjectionSystem.cs` | Retain typed ECS-to-UI projection | Remove M01/M02 hardcoding and project the selected catalog record. |
| Objective writer | `Assets/Game/Scripts/Systems/CampaignMissionObjectiveProjectionSystem.cs` | Retain one objective projection writer | Make it definition/fact driven for M01 and M02; do not add another objective system. |
| Settlement | `Assets/Game/Scripts/Systems/CampaignMissionProgressSettlementSystem.cs` | Retain one idempotent settlement owner | Replace hardcoded M01-to-M02 progression with per-mission next id; M02 unlocks M03 and Barracks once. |
| Result/debrief route | `Assets/Game/Scripts/UI/Screens/CampaignMissionHudResultBinder.cs` | Retain result acknowledgement owner | Route M02 victory through debrief before Campaign instead of directly to main menu. |
| Authoring | `Assets/Game/Scripts/Editor/M01FirstContactConfigBuilder.cs` | Mirror deterministic builder/validator pattern | Add an M02 builder and stop the M01 builder from rewriting mission/map catalogs to exactly one entry. |

Current catalogs contain only M01. M02 must add exactly one mission, scenario, and logical map entry while preserving M01 and rejecting duplicate, stale, or unresolved identities.

## 3. Operation Map And Dense-City Reuse

Physical source ownership remains unchanged:

- accepted physical identity: `opmap.skirmish.desert_base_01`;
- accepted candidate definition: `Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset`;
- accepted EntityScene, surface, blocker, minimap raster, and render database references;
- existing M01 protected-source hash coverage in `Assets/Tests/Editor/M01FirstContactMapSourceBindingTests.cs`.

M02 creates only `Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_ForwardPost01.asset`. It copies the canonical 2048 x 1024 world/grid/surface/navigation metadata and exact physical references, while owning a logical view of the same Old Market story district used by M01, mission camera metadata, logical hashes, camera ids, minimap id, and mission anchors.

Required logical anchors are player Deployment; forward-post Base; Barracks Build footprint; Resource focus; hostile Spawn; ordered Lane waypoints; Objective/Hostile defense focus; Civilian route/edge; narrative/comms focus; camera and minimap focus. Exact ids, coordinates, radii, and lane indices remain M02EB-009 data.

The reviewed physical district is the exact M01 Old Market story window `(1672,680)-(1912,856)`. Its canonical M02 Barracks lot `(1738,768,48,24)` is flat, accepted by the map surface, clear of authored building placements, and clear of transformed dense-city renderer bounds. A Build anchor alone cannot reserve that lot: current placement validation checks grid, buildings, roads, and blockers but does not consume operation-map anchors or playable bounds. M02 therefore needs one unmanaged mission build-zone/footprint projection consumed by the existing placement validator.

No new source scene, SubScene, physical bake, minimap raster, Addressables owner, runtime city generator, static streamer, or render database is allowed.

## 4. Building, Production, Resources, And Combat

| Behavior | Existing sole owner | M02 integration |
|---|---|---|
| Build catalog/read model | existing building definition catalog and Build Drawer projections | Filter the M02 mission view to Barracks; preserve full catalogs outside M02. |
| Placement command | `BuildingUiPlacementCommandQueueComponent` and `BuildingUiPlacementCommandRequestElement` in `BuildingRuntimeEcsComponents.cs` | ARIA and DO IT submit the same typed placement command as the player. |
| Placement validation | `BuildingPlacementValidationUtilitySystemHelper.cs` | Add mission build-zone data to its existing deterministic checks. |
| Resource spend | `BuildingPlacementConstructionTransaction.cs`, `BuildingConstructionResourceTransactionSystemHelper.cs`, `FactionConstructionResourceUtilitySystemHelper.cs` | Initialize attempt resources once and let these owners debit valid construction once. |
| Construction | existing building placement/construction runtime systems | Observe authoritative Barracks completion; do not create tutorial-only construction. |
| Production request/queue | `BuildingProductionRequestSystemHelper.cs`, `BuildingProductionCompositionSystemHelper.cs`, `BuildingProductionRuntimeTickCompositionSystemHelper.cs`, `BuildingProductionQueueCompositionSystemHelper.cs` | Queue the approved rifle unit through the existing Barracks buffer and timer. |
| Produced-unit truth | `BuildingProducedUnitReadModel` and existing runtime read-model composition | Project one matching completed unit into monotonic mission facts. |
| Patrol movement/combat | existing ECS movement, targeting, weapon, health, death, and building-defense systems | Spawn authored hostiles suppressed; remove suppression once after warning; existing combat owns contact. |
| Base survival | existing building health/destruction components | Observe the authoritative forward-post role/entity; no parallel base-health integer. |
| Civilian losses | existing civilian identity and combat/health truth | Count mission civilians through facts for the independent star; no tutorial invulnerability unless authored separately. |

The required attempt-scoped additions are unmanaged facts for Barracks completion, produced rifle completion, patrol activation, base survival/failure, civilian loss count, and elapsed mission time. Facts are observations of authoritative systems, never replacements for them.

## 5. UI, ARIA, Narrative, And Localization

| Behavior | Existing owner/pattern | M02 integration |
|---|---|---|
| ARIA tutorial surface | `AriaTutorialBriefingPrefabBuilder.cs`; `UiShellEcsGateway.TutorialNarration.cs` | Add typed Build, Barracks selection, placement, producer selection, production, warning, and defense steps. Each step owns distinct English/Farsi display/audio ids, auto indicator, SHOW ME, and DO IT command. |
| Campaign briefing | existing Campaign mission read model/projection | Project M02 objectives, resources, restrictions, rewards, map, and deploy readiness without raw keys. |
| Narrative data | `NarrativeSequenceConfig.cs`; `M01FirstContactNarrativeConfigBuilder.cs` | Add `seq.ch01.m02.brief`, `.comms`, and `.debrief` in one chapter-scoped asset using the generic narrative UI. |
| Comic presentation | `FirstLaunchNarrativeSequence.prefab` and generic presentation helper | Reuse provisionally. Final M02 dual-aspect panels wait for M02EB-029 acceptance. |
| Bilingual text/audio | `NarrativeLocaleConfig.cs`, first-launch catalogs/importers, M01 tutorial importer | Add chapter/M02-scoped catalogs and importers; do not extend FirstLaunch exact-set importers. Final voice production waits for approved copy. |
| Result/debrief | existing HUD result binder and narrative route | Result truth must match settlement, then play M02 debrief and reveal M03. |

The canonical M02 story beats already exist in `Design/Campaign_Narrative_Sequence_And_Comic_Catalog.md`: abandoned JRC post/clinic route in briefing, stolen municipal access list in comms, and Dalia becoming field lead with the warning-sector outage in debrief.

## 6. Persistence, Retry, And Lifecycle

- Launch and retry reuse the existing Campaign launch payload and Match bootstrap.
- Retry retains deterministic seed, increments attempt identity, and recreates mission facts/resources.
- Credits/Materials spent during an attempt never mutate profile economy.
- First-clear rewards, Barracks permanent unlock, and M03 unlock settle once through the existing progression owner.
- Replay may use mission-scoped Barracks access even after permanent unlock, but cannot duplicate rewards.
- World recreation, exit, retry, M01 then M02, and M02 then M01 must leave no stale facts, buffers, entities, narrative handles, or UI owners.

## 7. Validation Seams

Focused validation must cover:

1. mission/scenario/map deterministic authoring and two-entry catalog resolution;
2. M01 contract, launch, objective, narrative, settlement, and UI regressions;
3. objective/star rule validation and fail-closed ambiguity;
4. exact accepted physical-map hashes and M02 logical-to-physical handoff;
5. playable bounds, cameras, minimap round trips, anchors, surface, blockers, and build footprint;
6. Barracks-only catalog filtering, valid/invalid placement, one-time resource spend, construction, queue, timer, and rifle spawn;
7. pre-activation patrol suppression, warning, activation, combat, base damage/destruction, civilian losses, victory/defeat, stars, retry, and replay;
8. ARIA typed commands and indicators, English/Farsi text/audio identity, briefing/result/debrief routes;
9. architecture/source-growth scans and representative zero-GC/performance thresholds.

Unity CLI `1.0.0-beta.6` is installed. At inventory time `unity status` reported no connected Editor, so no live scene command was possible. Acceptance execution remains the checked `Tools/CI/invoke_unity_macos.sh` path required by `AGENTS.md`; Unity CLI will be used for connected-Editor inspection and iteration when an Editor is available.

## 8. Conclusion

M02 requires additive data and bounded generalization of existing sole owners. It does not require a new mission framework, resource system, building system, production system, combat system, managed map visual path, physical city bake, UI shell, or narrative presentation. M02EB-003 can now freeze exact path allowlists, rollback, generated-output policy, and pass markers for implementation.
