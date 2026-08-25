# M02EB-003 Ownership, Rollback, And Validation Matrix

Date: 2026-08-24
Baseline: `2e1b88779`
Result: Passed; exact implementation ownership and acceptance boundaries are frozen for M02EB-004 through M02EB-034.

## 1. Global Rules

1. Every edit must be inside an exact path listed below. A newly discovered required path is added to this matrix before editing.
2. Generated Unity `.asset`, `.prefab`, `.unity`, texture, audio, and `.meta` changes are created through Editor code or a connected Unity CLI command, never hand-authored YAML.
3. Unity CLI may inspect or drive a connected Editor. Checked acceptance always uses `Tools/CI/invoke_unity_macos.sh` and an explicit pass marker.
4. Final comic panels and voices remain prohibited until M02EB-029 is accepted. Android/Samsung work remains deferred.
5. The existing modified `Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset` is user-owned and excluded unless the owner assigns it to a later M02 localization step.

## 2. Protected Exclusions

The following are read-only for the entire tracker:

- `Assets/Game/Scenes/OperationMaps/OperationMap_Compatibility_DesertBase01_EditorSource.unity`
- `Assets/Game/Scenes/OperationMaps/OperationMap_Compatibility_DesertBase01_EntityScene_Candidate.unity`
- `Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset`
- `Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_Surface.asset`
- `Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset`
- accepted minimap rasters, render databases, generated proxy/LOD/impostor assets, production Addressables groups, and rollback packages referenced by that candidate;
- `Packages/`, `ProjectSettings/`, `Tools/CI/`, Jenkins configuration, Unity installations, M03-M05 implementation, and unrelated Skirmish/default assets.

M02 reuses the physical references exactly. No new physical bake, runtime city generator, static streamer, managed visual owner, or duplicate loader is allowed.

## 3. Exact Write Matrix

### Contract And Canonical Data: M02EB-004 Through M02EB-010

Approved existing paths:

- `Assets/Game/Scripts/Missions/Contracts/MissionContracts.cs`
- `Assets/Game/Scripts/Configs/MissionDefinitionConfig.cs`
- `Assets/Game/Scripts/Configs/MissionDefinitionContractValidation.cs`
- `Assets/Game/Scripts/Configs/ScenarioSetupConfig.cs`
- `Assets/Game/Scripts/Configs/ScenarioMissionRuntimeConfig.cs`
- `Assets/Game/Scripts/Configs/ScenarioMissionRuntimeContractValidation.cs`
- `Assets/Game/Scripts/Components/CampaignMissionComponents.cs`
- `Assets/Game/Scripts/Composition/CampaignMissionCatalogProjection.cs`
- `Assets/Game/Scripts/Configs/GameplayConfigModels.cs`
- `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset`
- `Assets/Game/Scripts/Editor/M01FirstContactConfigBuilder.cs`
- `Assets/Tests/Editor/M01FirstContactOperationMapTests.cs`
- `Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset`
- `Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset`

Approved new paths:

- `Assets/Game/Scripts/Editor/M02EstablishBaseConfigBuilder.cs`
- `Assets/Game/Scripts/Editor/M02EstablishBaseForwardPostWindowValidation.cs`
- `Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M02_EstablishBase.asset`
- `Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M02_EstablishBase.asset`
- `Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_ForwardPost01.asset`
- `Assets/Tests/Editor/M02EstablishBaseContractTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseMissionRuleTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseScenarioTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseBarracksProductionTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseCanonicalDataTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseOperationMapTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseForwardPostWindowTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseAnchorTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseCameraMinimapTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseDenseCityReuseTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseContractValidation.cs`

The builder may create Unity-generated `.meta` files beside approved new files. It must preserve catalog entries by stable id, sort deterministically, and produce byte-stable data on a second pass.

### Runtime Gameplay: M02EB-011 Through M02EB-022

Approved existing paths:

- `Assets/Game/Scripts/Configs/MissionDefinitionCatalogConfig.cs`
- `Assets/Game/Scripts/Configs/MissionDefinitionContractValidation.cs`
- `Assets/Game/Scripts/Components/CampaignMissionComponents.cs`
- `Assets/Game/Scripts/Composition/CampaignMissionCatalogProjection.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapView.cs`
- `Assets/Game/Scripts/Composition/CampaignMissionMenuBootstrapRuntime.cs`
- `Assets/Game/Scripts/Editor/M01FirstContactConfigBuilder.cs`
- `Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset`
- `Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset`
- `Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset`
- `Assets/Game/Scenes/Menu.unity`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiCampaignMissionProjectionSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiCampaignMissionProjectionSystem.Catalog.cs`
- `Assets/Game/Scripts/Composition/CampaignMissionCatalogProjection.Matching.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiCampaignMissionComponents.cs`
- `Assets/Tests/Editor/M01FirstContactFirstLaunchHandoffTests.cs`
- `Assets/Tests/Editor/M01FirstContactLaunchBootstrapTests.cs`
- `Design/AgentReports/M01FirstContact/m01dc_014_operation_map.json`
- `Design/AgentReports/M01FirstContact/m01dc_015_camera_continuity.json`
- `Design/AgentReports/M01FirstContact/m01dc_015_camera_continuity_contact_sheet.png`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionLaunchSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionSpawnSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionObjectiveProjectionSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionResultProjectionSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionProgressSettlementSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeProgressUtility.cs`
- `Assets/Game/Scripts/Runtime/Campaign/CampaignMissionProgressStore.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementConstructionTransaction.cs`
- `Assets/Game/Scripts/Systems/BuildingConstructionResourceTransactionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/FactionConstructionResourceUtilitySystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Composition/MatchBuildingRuntimeBootstrapStartupSystemHelper.cs`
- `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiMissionHudRestrictionsModel.cs`
- `Assets/Game/Scripts/UI/Contracts/UiMatchHudResourceValuesModel.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudResourceHeaderPresentation.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs`
- `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.MissionHudRestrictions.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.ResourceValues.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Lifecycle.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`

Approved new paths:

- `Assets/Game/Scripts/Components/CampaignMissionAttemptFactComponents.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryCommands.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeComposition.PublishContext.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionAttemptResourceInitializationSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionAttemptFactProjectionSystem.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionDelayedWaveSystem.cs`
- `Assets/Game/Scripts/Composition/CampaignMissionCatalogProjection.BuildCatalog.cs`
- `Assets/Game/Scripts/UI/Contracts/UiMissionBuildCatalogModel.cs`
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.MissionBuildCatalog.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.MissionBuildCatalog.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerMissionCatalogPrefabSource.cs`
- `Assets/Tests/Editor/M02EstablishBaseLaunchTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseResourceTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseBuildCatalogTests.cs`
- `Assets/Tests/Editor/M02EstablishBasePlacementTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseProductionTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseWaveTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseObjectiveTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseSettlementTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseLifecycleTests.cs`
- `Assets/Tests/PlayMode/M02EstablishBaseVerticalSlicePlayModeTests.cs`

Any fact projection must be unmanaged and monotonic. Structural changes use an EntityCommandBuffer outside entity iteration. Existing placement, resource, construction, production, movement, combat, health, death, and settlement systems remain authoritative.

M02EB-022 may publish a bounded building-delete command on the existing building runtime boundary.
Only the established building runtime composition owner may consume that command through its existing
`DeleteBuildingById` delegate so retry/exit cleanup removes the visual, blocker, production queue,
combat entity, and runtime dictionary entry together. This amendment does not authorize a mission-owned
managed building lookup or deletion path.

M02EB-012 may project the scenario-owned attempt resource seed into the existing Campaign catalog,
apply it once after the canonical startup resource owner is ready, and hide M02-disabled logistics
resources and controls through the existing HUD restriction gateway. The initializer may update only
the canonical player `FactionEconomy` and `FactionTacticalMaterialsComponent`; it may not add a
parallel economy, persist attempt spend, or alter M01/Skirmish defaults.

M02EB-013 may project the scenario-owned build catalog into the existing Campaign mission blob and
expose it through the existing UI gateway. The Build Drawer may wrap its established prefab sources
with a bounded reusable mission filter, but it may not own mission truth, mutate the global catalogs,
or change unrestricted M01/Skirmish behavior. A missing mission entry fails closed.

M02EB-011 may generalize the existing single Campaign selection, catalog projection, and menu map-bootstrap owners only far enough to prove a typed M02 deploy reaches the canonical payload and logical map. It must preserve the M01 serialized fallback and does not authorize final M02 card copy, layout, briefing presentation, or reveal behavior before M02EB-023. `Menu.unity` and the Campaign catalog are changed only through the connected Editor/builder path. The shared builder may normalize default-safe M01 schema fields and refresh checked M01 regression reports after the Chapter 1 catalogs gain M02; those outputs must remain behavior-compatible and deterministic for their current source inputs.

### Campaign UI, ARIA, Narrative, And Review: M02EB-023 Through M02EB-032

Approved existing paths:

- `Assets/Game/Scripts/UI/Shell/Ecs/UiCampaignMissionProjectionSystem.cs`
- `Assets/Game/Scripts/UI/Contracts/UiCampaignMissionModels.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiCampaignMissionComponents.cs`
- `Assets/Game/Scripts/UI/Screens/CampaignMissionScreenBinder.cs`
- `Assets/Game/Scripts/UI/Screens/CampaignMissionHudResultBinder.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresentationSystemHelper.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogQueryUiSystemHelper.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.TutorialNarration.cs`
- `Assets/Game/Scripts/UI/Screens/AssistantHighlightPresentationSystemHelper.Guidance.cs`
- `Assets/Game/Scripts/UI/Screens/AssistantHighlightPresentationSystemHelper.TargetVisuals.cs`
- `Assets/Game/Scripts/UI/Screens/AriaTutorialBriefingView.cs`
- `Assets/Game/Scripts/UI/Screens/AriaCommandAssistantPopupView.cs`
- `Assets/Game/Scripts/Configs/Narrative/NarrativeSequenceConfig.cs`
- `Assets/Game/Scripts/Configs/Narrative/NarrativeLocaleConfig.cs`
- `Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs`

Approved new paths before M02EB-029:

- `Assets/Game/Scripts/Editor/M02EstablishBaseNarrativeConfigBuilder.cs`
- `Assets/Game/Configs/Narrative/Chapter01/M02_EstablishBase_Narrative.asset`
- `Assets/Game/Data/Narrative/Chapter01/M02/m02_english_text_catalog.json`
- `Assets/Game/Data/Narrative/Chapter01/M02/m02_persian_text_catalog.json`
- `Assets/Tests/Editor/M02EstablishBaseCampaignUiTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseGuidanceTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseNarrativeTests.cs`
- `Assets/Tests/Editor/M02EstablishBaseHudResultTests.cs`
- `Assets/Game/Scripts/Editor/M02EstablishBaseVisualCapture.cs`
- `Build/EditorEvidence/M02EstablishBase/` capture output only; never stage unless a tracker evidence item explicitly names the file.

Additional new art/audio paths become writable only after M02EB-029 acceptance and an updated matrix entry. Final media must be chapter-scoped and must not alter FirstLaunch exact-set assets/importers.

### Final Acceptance: M02EB-033 Through M02EB-034

Approved existing paths are only this tracker, the technical architecture, directly affected Chapter 1 authorities, and M02 evidence files. Runtime code changes at this phase require reopening the appropriate earlier item and rerunning its gates.

## 4. Validation Matrix

| Boundary | Required focused marker | Required regression |
|---|---|---|
| Contracts/data | `[M02EstablishBaseContractValidation] result=Passed` | `[M01FirstContactConsolidatedContractValidation] result=Passed suites=23` |
| Barracks production | `[M02EstablishBaseBarracksProductionValidation] result=Passed` | building production, construction resource transaction, faction construction resource, and runtime allocation tests |
| Map binding/window | `[M02EstablishBaseOperationMapValidation] result=Passed` and `[M02EstablishBaseForwardPostWindowValidation] result=Passed` | M01 map source binding, dense-city reuse, operation-map contract, camera/minimap, and anchor tests |
| Launch/lifecycle | `[M02EstablishBaseLaunchValidation] result=Passed` and `[M02EstablishBaseLifecycleValidation] result=Passed` | M01 launch payload/bootstrap, first-launch handoff, runtime ownership, lifecycle, and World recreation coverage |
| Objectives/settlement | `[M02EstablishBaseObjectiveValidation] result=Passed` and `[M02EstablishBaseSettlementValidation] result=Passed` | M01 objective writer, result rule, settlement, progress store, and campaign UI tests |
| Build/produce/defend | `[M02EstablishBaseVerticalSlicePlayModeValidation] result=Passed` | placement validation/commit, construction transaction, production, building combat/destruction, unit movement/combat/death |
| Guidance/narrative/UI | `[M02EstablishBaseGuidanceValidation] result=Passed`, `[M02EstablishBaseNarrativeValidation] result=Passed`, and `[M02EstablishBaseCampaignUiValidation] result=Passed` | M01 guidance, narration, narrative, briefing, HUD result, and bilingual UI regressions |
| Architecture/performance | `[M02EstablishBaseArchitectureValidation] result=Passed` | `ProductionSourceGrowthArchitectureTests`, `ArchitectureHardeningCloseoutValidationRunner`, Burst/AOT architecture tests, `0 B/frame` target after warmup and no recurring residual at or above `1 KB/frame` |
| Visual review | `[M02EstablishBaseVisualCapture] result=Passed` | compiler zero and all preceding focused markers before evidence capture |

Every Unity invocation receives an explicit log under `/private/tmp`, timeout, execute method, and required marker. A missing marker, timeout, compile error, exception, or nonzero wrapper exit fails the item.

## 5. Source-Growth And Runtime Ceilings

- No new `MonoBehaviour.Update`/`LateUpdate` gameplay loop.
- No new static service locator, runtime hierarchy search, duplicate manager/streamer/loader, or managed permanent map visual.
- No per-entity main-thread orchestration where an unmanaged query/job can own the work.
- No structural change inside iteration; defer through an EntityCommandBuffer.
- No managed allocation after warmup in changed Match hot paths; accepted recurring residual remains below `1 KB/frame` only with profiler-backed documentation.
- New systems are narrow, Burst-capable `ISystem` owners unless they are an explicit UI/prefab/Editor managed boundary.
- Shared schema additions are default-safe, serialized compatibly, and covered by legacy scenario/M01 tests.

## 6. Rollback

Each accepted item is one bounded main-branch commit. Rollback is the inverse of that item commit only; it must not restore protected source content, user changes, or unrelated commits. Generated canonical assets are rebuilt from their checked Editor builder after rollback rather than edited manually. The physical map binding and frozen rollback package remain unchanged through Editor and deferred Android acceptance.
