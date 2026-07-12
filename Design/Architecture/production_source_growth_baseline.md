# Production Source Growth Baseline

This APH-701/702 baseline was captured from the production tree at commit `9280ead856fd0bf117fdb3601cc2216c3a35e0f4` on 2026-07-10.

- The helper freeze covers every baseline `Assets/Game/Scripts/**/*SystemHelper.cs` path. All 265 entries carry exact line and UTF-8 byte ceilings measured from the baseline Git blobs. A committed deletion retires the exact path; recreation requires `system-helper-recreation`, while later growth requires `system-helper-growth`.
- Production source means C# below `Assets/Game/Scripts` with any path containing an exact `Editor` directory segment excluded. The manifest records all 108 current production files above 500 lines.
- Every recorded line and UTF-8 byte count is an immutable companion ceiling, including all 81 reviewed files between 501 and 1,000 lines. The 27 entries above 1,000 lines retain `strictNoGrowth: true` and use the stricter growth scope.
- Git first-parent history from the full baseline commit ratchets both ceilings to the lowest committed positive measurements, even after a file drops below 500 lines. Deliberate shrinkage passes without editing this manifest; later line or byte regrowth requires an exact exception. A committed deletion permanently retires the reviewed path, and same-path recreation requires `production-path-recreation`.
- The helper and production path/line/byte tuples are cryptographically frozen and verified against baseline Git blobs. Case-insensitive identity prevents Windows casing bypasses while exact repository spelling remains mandatory.
- Line counts are logical text lines, equivalent to `File.ReadAllLines(path).Length` for these newline-terminated sources.

`approvedExceptions` is intentionally empty at creation. Every future object must contain exactly `path`, `trackerTaskId`, `decisionId`, `maxLines`, `maxBytes`, and `scope`. Paths are exact project-relative C# paths without globs. Tracker tasks must be marked active (`[~]`) or completed (`[x]`). The cited decision must have one unique row in the canonical five-column `## Decision Log` table containing this exact marker: `` `source-growth-exception(path=<path>;scope=<scope>;maxLines=<maxLines>;maxBytes=<maxBytes>;task=<trackerTaskId>)` ``. Markers elsewhere do not authorize exceptions.

Allowed scopes are `system-helper`, `system-helper-growth`, `production-over-500-review`, `production-over-1000-growth`, `production-path-recreation`, and `system-helper-recreation`. A new helper above 500 lines requires separate helper and size authorizations because each scope grants only one policy exception. Exceptions must name an existing source, preserve repository spelling, cover its current line and byte counts, and correspond to an active violation; stale, duplicated, or unused authorizations fail validation.

<!-- production-source-growth-manifest:start -->
```json
{
  "schemaVersion": 3,
  "baselineCommit": "9280ead856fd0bf117fdb3601cc2216c3a35e0f4",
  "productionRoot": "Assets/Game/Scripts",
  "productionEditorPathSegment": "Editor",
  "helperSuffix": "SystemHelper.cs",
  "reviewThresholdLines": 500,
  "strictNoGrowthThresholdLines": 1000,
  "frozenSystemHelpers": [
    {
      "path": "Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs",
      "baselineLines": 304,
      "baselineBytes": 12266
    },
    {
      "path": "Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationSystemHelper.cs",
      "baselineLines": 461,
      "baselineBytes": 16895
    },
    {
      "path": "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
      "baselineLines": 83,
      "baselineBytes": 4416
    },
    {
      "path": "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
      "baselineLines": 36,
      "baselineBytes": 1442
    },
    {
      "path": "Assets/Game/Scripts/Composition/BuildingSpawnPrefabLookupKeyPrefabSystemHelper.cs",
      "baselineLines": 21,
      "baselineBytes": 633
    },
    {
      "path": "Assets/Game/Scripts/Composition/GameRuntimeStatsUnitPrefabClassifierPrefabSystemHelper.cs",
      "baselineLines": 41,
      "baselineBytes": 1581
    },
    {
      "path": "Assets/Game/Scripts/Composition/GameplayFeatureStartupCompositionSystemHelper.cs",
      "baselineLines": 109,
      "baselineBytes": 5479
    },
    {
      "path": "Assets/Game/Scripts/Composition/GameplaySceneBindingSceneSystemHelper.cs",
      "baselineLines": 26,
      "baselineBytes": 693
    },
    {
      "path": "Assets/Game/Scripts/Composition/MapSurfaceRuntimeBootstrapSceneSystemHelper.cs",
      "baselineLines": 355,
      "baselineBytes": 14345
    },
    {
      "path": "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
      "baselineLines": 1164,
      "baselineBytes": 55424
    },
    {
      "path": "Assets/Game/Scripts/Composition/MatchBuildingRuntimeBootstrapStartupSystemHelper.cs",
      "baselineLines": 65,
      "baselineBytes": 2859
    },
    {
      "path": "Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs",
      "baselineLines": 63,
      "baselineBytes": 1815
    },
    {
      "path": "Assets/Game/Scripts/Composition/MatchStartSceneSystemHelper.cs",
      "baselineLines": 270,
      "baselineBytes": 10636
    },
    {
      "path": "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs",
      "baselineLines": 828,
      "baselineBytes": 33889
    },
    {
      "path": "Assets/Game/Scripts/Composition/PerformanceDiagnosticsReferenceDiagnosticsSystemHelper.cs",
      "baselineLines": 45,
      "baselineBytes": 1409
    },
    {
      "path": "Assets/Game/Scripts/Composition/SelectionPortraitSpriteResolverUiSystemHelper.cs",
      "baselineLines": 45,
      "baselineBytes": 1678
    },
    {
      "path": "Assets/Game/Scripts/Composition/UiCatalogAuthoringMetadataUiSystemHelper.cs",
      "baselineLines": 68,
      "baselineBytes": 2844
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityArchwaySpawnPrefabSystemHelper.cs",
      "baselineLines": 104,
      "baselineBytes": 4096
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityBuildingPlacementPrefabSystemHelper.cs",
      "baselineLines": 242,
      "baselineBytes": 10173
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityBuildingPlotUtilitySystemHelper.cs",
      "baselineLines": 240,
      "baselineBytes": 9175
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnContextCompositionSystemHelper.cs",
      "baselineLines": 137,
      "baselineBytes": 7521
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper.cs",
      "baselineLines": 138,
      "baselineBytes": 8025
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityBulkPlotPlanUtilitySystemHelper.cs",
      "baselineLines": 79,
      "baselineBytes": 3044
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityChainUtilitySystemHelper.cs",
      "baselineLines": 308,
      "baselineBytes": 12014
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityClothCoverSpawnPrefabSystemHelper.cs",
      "baselineLines": 110,
      "baselineBytes": 4337
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityCompositionSystemHelper.cs",
      "baselineLines": 788,
      "baselineBytes": 42890
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityConfigCompositionSystemHelper.cs",
      "baselineLines": 235,
      "baselineBytes": 10305
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityCorridorBuildingSpawnPrefabSystemHelper.cs",
      "baselineLines": 66,
      "baselineBytes": 2923
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityDecorationBuildingSpawnPrefabSystemHelper.cs",
      "baselineLines": 114,
      "baselineBytes": 4545
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityDecorationGroupPrefabSystemHelper.cs",
      "baselineLines": 62,
      "baselineBytes": 2338
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityDiagnosticsSystemHelper.cs",
      "baselineLines": 75,
      "baselineBytes": 3293
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityEntryBuildingSpawnPrefabSystemHelper.cs",
      "baselineLines": 118,
      "baselineBytes": 4473
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityFreeScatterDecorationPrefabSystemHelper.cs",
      "baselineLines": 105,
      "baselineBytes": 4011
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityGenerationCompositionSystemHelper.cs",
      "baselineLines": 375,
      "baselineBytes": 19812
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityHallSpawnPrefabSystemHelper.cs",
      "baselineLines": 94,
      "baselineBytes": 3944
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityHouseYardWallPrefabSystemHelper.cs",
      "baselineLines": 180,
      "baselineBytes": 7679
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityIngressUtilitySystemHelper.cs",
      "baselineLines": 147,
      "baselineBytes": 5418
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityLandmarkOffsetUtilitySystemHelper.cs",
      "baselineLines": 126,
      "baselineBytes": 3818
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityLandmarkSpawnPrefabSystemHelper.cs",
      "baselineLines": 206,
      "baselineBytes": 8383
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityLayoutUtilitySystemHelper.cs",
      "baselineLines": 360,
      "baselineBytes": 14616
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityLifecycleCompositionSystemHelper.cs",
      "baselineLines": 173,
      "baselineBytes": 5534
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityMinimapEventUiSystemHelper.cs",
      "baselineLines": 37,
      "baselineBytes": 886
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityPrefabSelectionPrefabSystemHelper.cs",
      "baselineLines": 161,
      "baselineBytes": 5369
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityReadModelCompositionSystemHelper.cs",
      "baselineLines": 16,
      "baselineBytes": 521
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityReadinessQueryCompositionSystemHelper.cs",
      "baselineLines": 145,
      "baselineBytes": 5542
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityRoadBuildBridgeCompositionSystemHelper.cs",
      "baselineLines": 192,
      "baselineBytes": 7237
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityRoadCommitCompositionSystemHelper.cs",
      "baselineLines": 188,
      "baselineBytes": 6988
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityRoadLayoutUtilitySystemHelper.cs",
      "baselineLines": 329,
      "baselineBytes": 14595
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper.cs",
      "baselineLines": 248,
      "baselineBytes": 9647
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityRuralBuildingSpawnPrefabSystemHelper.cs",
      "baselineLines": 113,
      "baselineBytes": 4470
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCitySpawnBridgePrefabSystemHelper.cs",
      "baselineLines": 129,
      "baselineBytes": 4439
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityStartupSystemHelper.cs",
      "baselineLines": 237,
      "baselineBytes": 10368
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCitySurfaceIntegrationUtilitySystemHelper.cs",
      "baselineLines": 94,
      "baselineBytes": 3311
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs",
      "baselineLines": 166,
      "baselineBytes": 6363
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityWalkabilityUtilitySystemHelper.cs",
      "baselineLines": 224,
      "baselineBytes": 7947
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityYardGateUtilitySystemHelper.cs",
      "baselineLines": 52,
      "baselineBytes": 1879
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityYardWallPlanUtilitySystemHelper.cs",
      "baselineLines": 110,
      "baselineBytes": 4129
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityYardWallVisualPresentationSystemHelper.cs",
      "baselineLines": 206,
      "baselineBytes": 9859
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs",
      "baselineLines": 570,
      "baselineBytes": 23784
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs",
      "baselineLines": 896,
      "baselineBytes": 37139
    },
    {
      "path": "Assets/Game/Scripts/Rendering/StaticMapChunkBatchingPresentationSystemHelper.cs",
      "baselineLines": 491,
      "baselineBytes": 19610
    },
    {
      "path": "Assets/Game/Scripts/Rendering/UnitAttackTracePresentationSystemHelper.cs",
      "baselineLines": 328,
      "baselineBytes": 12580
    },
    {
      "path": "Assets/Game/Scripts/Rendering/UnitImpostorPresentationSystemHelper.cs",
      "baselineLines": 928,
      "baselineBytes": 37326
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs",
      "baselineLines": 895,
      "baselineBytes": 38524
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingCitizenPopulationCompositionSystemHelper.cs",
      "baselineLines": 267,
      "baselineBytes": 11781
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingCombatUtilitySystemHelper.cs",
      "baselineLines": 531,
      "baselineBytes": 22279
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
      "baselineLines": 982,
      "baselineBytes": 45901
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingDestroyedVisualPresentationSystemHelper.cs",
      "baselineLines": 78,
      "baselineBytes": 2953
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingFoundationVisualPresentationSystemHelper.cs",
      "baselineLines": 43,
      "baselineBytes": 1603
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayBindingCompositionSystemHelper.cs",
      "baselineLines": 30,
      "baselineBytes": 1440
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs",
      "baselineLines": 771,
      "baselineBytes": 48202
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayDependencyCompositionSystemHelper.cs",
      "baselineLines": 144,
      "baselineBytes": 6101
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayDisposalCompositionSystemHelper.cs",
      "baselineLines": 34,
      "baselineBytes": 1647
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayDisposalExecutionCompositionSystemHelper.cs",
      "baselineLines": 85,
      "baselineBytes": 4051
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayEcsQueryCompositionSystemHelper.cs",
      "baselineLines": 85,
      "baselineBytes": 4081
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayGridDataCompositionSystemHelper.cs",
      "baselineLines": 65,
      "baselineBytes": 2453
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayResultCompositionSystemHelper.cs",
      "baselineLines": 250,
      "baselineBytes": 15529
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs",
      "baselineLines": 161,
      "baselineBytes": 14090
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayStartupCompositionSystemHelper.cs",
      "baselineLines": 34,
      "baselineBytes": 1498
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingMarkerVisualPresentationSystemHelper.cs",
      "baselineLines": 22,
      "baselineBytes": 652
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs",
      "baselineLines": 144,
      "baselineBytes": 6814
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementCommandCompositionSystemHelper.cs",
      "baselineLines": 177,
      "baselineBytes": 11655
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs",
      "baselineLines": 459,
      "baselineBytes": 19831
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementCommitCompositionSystemHelper.cs",
      "baselineLines": 257,
      "baselineBytes": 11879
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementContextCompositionSystemHelper.cs",
      "baselineLines": 267,
      "baselineBytes": 15533
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementGridCameraSystemHelper.cs",
      "baselineLines": 105,
      "baselineBytes": 4010
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickUiSystemHelper.cs",
      "baselineLines": 254,
      "baselineBytes": 12048
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementInputTickCompositionSystemHelper.cs",
      "baselineLines": 60,
      "baselineBytes": 3764
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementInputUiSystemHelper.cs",
      "baselineLines": 456,
      "baselineBytes": 18519
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementInteractionCompositionSystemHelper.cs",
      "baselineLines": 292,
      "baselineBytes": 11921
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextCompositionSystemHelper.cs",
      "baselineLines": 142,
      "baselineBytes": 8260
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.cs",
      "baselineLines": 114,
      "baselineBytes": 4698
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementLifecycleCompositionSystemHelper.cs",
      "baselineLines": 263,
      "baselineBytes": 11484
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementPreviewPresentationSystemHelper.cs",
      "baselineLines": 341,
      "baselineBytes": 13243
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementQueryUiSystemHelper.cs",
      "baselineLines": 308,
      "baselineBytes": 13013
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementRedirectCompositionSystemHelper.cs",
      "baselineLines": 394,
      "baselineBytes": 17012
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs",
      "baselineLines": 358,
      "baselineBytes": 18191
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs",
      "baselineLines": 74,
      "baselineBytes": 4287
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystemHelper.cs",
      "baselineLines": 141,
      "baselineBytes": 7300
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementSessionCompositionSystemHelper.cs",
      "baselineLines": 131,
      "baselineBytes": 5999
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementStartupSystemHelper.cs",
      "baselineLines": 142,
      "baselineBytes": 6617
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs",
      "baselineLines": 410,
      "baselineBytes": 16786
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementVisualCompositionPresentationSystemHelper.cs",
      "baselineLines": 248,
      "baselineBytes": 14136
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementVisualPresentationSystemHelper.cs",
      "baselineLines": 328,
      "baselineBytes": 12249
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateCompositionSystemHelper.cs",
      "baselineLines": 279,
      "baselineBytes": 15880
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionCompositionSystemHelper.cs",
      "baselineLines": 101,
      "baselineBytes": 6777
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionContextCompositionSystemHelper.cs",
      "baselineLines": 362,
      "baselineBytes": 23048
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
      "baselineLines": 895,
      "baselineBytes": 37741
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs",
      "baselineLines": 1697,
      "baselineBytes": 74162
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs",
      "baselineLines": 249,
      "baselineBytes": 12126
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionSlotUtilitySystemHelper.cs",
      "baselineLines": 104,
      "baselineBytes": 3837
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionTickCompositionSystemHelper.cs",
      "baselineLines": 76,
      "baselineBytes": 3864
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs",
      "baselineLines": 289,
      "baselineBytes": 11580
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
      "baselineLines": 1914,
      "baselineBytes": 87282
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionUpdateCompositionSystemHelper.cs",
      "baselineLines": 217,
      "baselineBytes": 9623
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs",
      "baselineLines": 1634,
      "baselineBytes": 73709
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingResourceProductionSystemHelper.cs",
      "baselineLines": 109,
      "baselineBytes": 4268
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingResourceStorageTransferSystemHelper.cs",
      "baselineLines": 229,
      "baselineBytes": 8153
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.cs",
      "baselineLines": 172,
      "baselineBytes": 7296
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeCompositionSystemHelper.cs",
      "baselineLines": 32,
      "baselineBytes": 2290
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeContextCompositionSystemHelper.cs",
      "baselineLines": 304,
      "baselineBytes": 19112
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeContextFactoryCompositionSystemHelper.cs",
      "baselineLines": 553,
      "baselineBytes": 35388
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeCreationCompositionSystemHelper.cs",
      "baselineLines": 220,
      "baselineBytes": 11892
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs",
      "baselineLines": 301,
      "baselineBytes": 14469
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeFocusPositionPresentationSystemHelper.cs",
      "baselineLines": 20,
      "baselineBytes": 709
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeObjectPresentationSystemHelper.cs",
      "baselineLines": 19,
      "baselineBytes": 430
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipCompositionSystemHelper.cs",
      "baselineLines": 106,
      "baselineBytes": 4629
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
      "baselineLines": 1678,
      "baselineBytes": 74511
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs",
      "baselineLines": 82,
      "baselineBytes": 4326
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeQueryCompositionSystemHelper.cs",
      "baselineLines": 133,
      "baselineBytes": 5151
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs",
      "baselineLines": 619,
      "baselineBytes": 25874
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabCompositionSystemHelper.cs",
      "baselineLines": 36,
      "baselineBytes": 1879
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextCompositionSystemHelper.cs",
      "baselineLines": 229,
      "baselineBytes": 10394
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeSideEffectCompositionSystemHelper.cs",
      "baselineLines": 63,
      "baselineBytes": 4208
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCommandSystemHelper.cs",
      "baselineLines": 204,
      "baselineBytes": 7346
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.cs",
      "baselineLines": 524,
      "baselineBytes": 25804
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeTickCompositionSystemHelper.cs",
      "baselineLines": 53,
      "baselineBytes": 4789
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs",
      "baselineLines": 45,
      "baselineBytes": 1547
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeVisualPresentationSystemHelper.cs",
      "baselineLines": 202,
      "baselineBytes": 9329
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionClickCompositionSystemHelper.cs",
      "baselineLines": 34,
      "baselineBytes": 1661
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionClickUtilitySystemHelper.cs",
      "baselineLines": 91,
      "baselineBytes": 3398
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionCompositionSystemHelper.cs",
      "baselineLines": 44,
      "baselineBytes": 2979
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionMarkerPresentationSystemHelper.cs",
      "baselineLines": 398,
      "baselineBytes": 15900
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionPortraitUiSystemHelper.cs",
      "baselineLines": 21,
      "baselineBytes": 637
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionRuntimeCompositionSystemHelper.cs",
      "baselineLines": 639,
      "baselineBytes": 28385
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSpawnCellUtilitySystemHelper.cs",
      "baselineLines": 124,
      "baselineBytes": 4510
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs",
      "baselineLines": 1824,
      "baselineBytes": 78233
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSurfacePlacementUtilitySystemHelper.cs",
      "baselineLines": 149,
      "baselineBytes": 5804
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingUiCommandSystemHelper.cs",
      "baselineLines": 237,
      "baselineBytes": 10142
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingUiCompositionSystemHelper.cs",
      "baselineLines": 246,
      "baselineBytes": 15127
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingUiContextCompositionSystemHelper.cs",
      "baselineLines": 301,
      "baselineBytes": 16397
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingUiQueryUiSystemHelper.cs",
      "baselineLines": 553,
      "baselineBytes": 24194
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenBuildingReadCompositionSystemHelper.cs",
      "baselineLines": 186,
      "baselineBytes": 8419
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenDangerCompositionSystemHelper.cs",
      "baselineLines": 194,
      "baselineBytes": 7334
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenHouseholdRegistrationCompositionSystemHelper.cs",
      "baselineLines": 427,
      "baselineBytes": 19841
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystemHelper.cs",
      "baselineLines": 247,
      "baselineBytes": 11959
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationDebugDiagnosticsSystemHelper.cs",
      "baselineLines": 82,
      "baselineBytes": 4557
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationDiagnosticsSystemHelper.cs",
      "baselineLines": 170,
      "baselineBytes": 7612
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationEcsProjectionCompositionSystemHelper.cs",
      "baselineLines": 231,
      "baselineBytes": 10048
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationEventCompositionSystemHelper.cs",
      "baselineLines": 106,
      "baselineBytes": 4742
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationLifecycleCompositionSystemHelper.cs",
      "baselineLines": 190,
      "baselineBytes": 7374
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationReadModelCompositionSystemHelper.cs",
      "baselineLines": 138,
      "baselineBytes": 4674
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationRuntimeUpdateCompositionSystemHelper.cs",
      "baselineLines": 353,
      "baselineBytes": 15835
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationStateCompositionSystemHelper.cs",
      "baselineLines": 104,
      "baselineBytes": 4078
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenPopulationTotalsCompositionSystemHelper.cs",
      "baselineLines": 91,
      "baselineBytes": 3597
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenRefugeeCompositionSystemHelper.cs",
      "baselineLines": 654,
      "baselineBytes": 28948
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenResourceCompositionSystemHelper.cs",
      "baselineLines": 65,
      "baselineBytes": 1925
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenScheduleCompositionSystemHelper.cs",
      "baselineLines": 251,
      "baselineBytes": 10758
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenStatusTransitionCompositionSystemHelper.cs",
      "baselineLines": 285,
      "baselineBytes": 11411
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenVisibleUnitPresentationSystemHelper.cs",
      "baselineLines": 365,
      "baselineBytes": 18059
    },
    {
      "path": "Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs",
      "baselineLines": 960,
      "baselineBytes": 44503
    },
    {
      "path": "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs",
      "baselineLines": 864,
      "baselineBytes": 34218
    },
    {
      "path": "Assets/Game/Scripts/Systems/FocusableUnitLookupCameraSystemHelper.cs",
      "baselineLines": 530,
      "baselineBytes": 22766
    },
    {
      "path": "Assets/Game/Scripts/Systems/FocusedUnitLifecycleCompositionSystemHelper.cs",
      "baselineLines": 311,
      "baselineBytes": 14733
    },
    {
      "path": "Assets/Game/Scripts/Systems/FocusedUnitUiReadModelUiSystemHelper.cs",
      "baselineLines": 308,
      "baselineBytes": 14834
    },
    {
      "path": "Assets/Game/Scripts/Systems/GameplayAudioFeedbackSystemHelper.cs",
      "baselineLines": 174,
      "baselineBytes": 5461
    },
    {
      "path": "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs",
      "baselineLines": 469,
      "baselineBytes": 22223
    },
    {
      "path": "Assets/Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs",
      "baselineLines": 351,
      "baselineBytes": 19377
    },
    {
      "path": "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs",
      "baselineLines": 744,
      "baselineBytes": 29464
    },
    {
      "path": "Assets/Game/Scripts/Systems/MapVehiclePlacementSpawnPrefabSystemHelper.cs",
      "baselineLines": 576,
      "baselineBytes": 23388
    },
    {
      "path": "Assets/Game/Scripts/Systems/MatchHudSquadTraySelectionUiSystemHelper.cs",
      "baselineLines": 417,
      "baselineBytes": 17630
    },
    {
      "path": "Assets/Game/Scripts/Systems/MatchStartRequestStartupSystemHelper.cs",
      "baselineLines": 69,
      "baselineBytes": 2644
    },
    {
      "path": "Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystemHelper.cs",
      "baselineLines": 909,
      "baselineBytes": 39101
    },
    {
      "path": "Assets/Game/Scripts/Systems/ResourceExchangeVisualPresentationSystemHelper.cs",
      "baselineLines": 494,
      "baselineBytes": 17177
    },
    {
      "path": "Assets/Game/Scripts/Systems/ResourceHaulerUtilitySystemHelper.cs",
      "baselineLines": 550,
      "baselineBytes": 21102
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildBuildingPlacementCompositionSystemHelper.cs",
      "baselineLines": 245,
      "baselineBytes": 10534
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs",
      "baselineLines": 234,
      "baselineBytes": 9826
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildCompositionContextCompositionSystemHelper.cs",
      "baselineLines": 393,
      "baselineBytes": 21748
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildCompositionLifecycleCompositionSystemHelper.cs",
      "baselineLines": 93,
      "baselineBytes": 5051
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildCompositionSourceCompositionSystemHelper.cs",
      "baselineLines": 162,
      "baselineBytes": 10315
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildCompositionSystemHelper.cs",
      "baselineLines": 118,
      "baselineBytes": 5572
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildContextCompositionSystemHelper.cs",
      "baselineLines": 46,
      "baselineBytes": 2644
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildDependencyCompositionSystemHelper.cs",
      "baselineLines": 63,
      "baselineBytes": 2639
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildDisposalCompositionSystemHelper.cs",
      "baselineLines": 75,
      "baselineBytes": 3945
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildEcsCompositionSystemHelper.cs",
      "baselineLines": 265,
      "baselineBytes": 12770
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildInputCompositionSystemHelper.cs",
      "baselineLines": 229,
      "baselineBytes": 9583
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildInteractionCompositionSystemHelper.cs",
      "baselineLines": 143,
      "baselineBytes": 6191
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildInteractionContextCompositionSystemHelper.cs",
      "baselineLines": 157,
      "baselineBytes": 8055
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildMutationCompositionSystemHelper.cs",
      "baselineLines": 70,
      "baselineBytes": 2551
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildPlacementStorageCompositionSystemHelper.cs",
      "baselineLines": 102,
      "baselineBytes": 3598
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildReadModelCompositionSystemHelper.cs",
      "baselineLines": 170,
      "baselineBytes": 7785
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildRuntimeActionCompositionSystemHelper.cs",
      "baselineLines": 94,
      "baselineBytes": 3336
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildSessionCompositionSystemHelper.cs",
      "baselineLines": 193,
      "baselineBytes": 7505
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadBuildVisualContextPresentationSystemHelper.cs",
      "baselineLines": 112,
      "baselineBytes": 5911
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadDeletePromptUiSystemHelper.cs",
      "baselineLines": 76,
      "baselineBytes": 2735
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadMinimapEventUiSystemHelper.cs",
      "baselineLines": 36,
      "baselineBytes": 820
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadNetworkCompositionSystemHelper.cs",
      "baselineLines": 374,
      "baselineBytes": 12261
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadPathPlanningUtilitySystemHelper.cs",
      "baselineLines": 167,
      "baselineBytes": 6204
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadPreviewPresentationSystemHelper.cs",
      "baselineLines": 280,
      "baselineBytes": 10439
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadRuntimeGenerationCompositionSystemHelper.cs",
      "baselineLines": 172,
      "baselineBytes": 6616
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadRuntimeGenerationContextCompositionSystemHelper.cs",
      "baselineLines": 52,
      "baselineBytes": 2547
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadRuntimeRootSceneSystemHelper.cs",
      "baselineLines": 66,
      "baselineBytes": 2365
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadSurfacePlacementUtilitySystemHelper.cs",
      "baselineLines": 140,
      "baselineBytes": 4994
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadVisualRefreshPresentationSystemHelper.cs",
      "baselineLines": 118,
      "baselineBytes": 5537
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionCommandResultFlushCompositionSystemHelper.cs",
      "baselineLines": 1634,
      "baselineBytes": 81709
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionFocusCommandCompositionSystemHelper.cs",
      "baselineLines": 360,
      "baselineBytes": 19287
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionInputCompositionSystemHelper.cs",
      "baselineLines": 929,
      "baselineBytes": 38098
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionInputStateCompositionSystemHelper.cs",
      "baselineLines": 138,
      "baselineBytes": 5363
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandCompositionSystemHelper.cs",
      "baselineLines": 1568,
      "baselineBytes": 75786
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs",
      "baselineLines": 823,
      "baselineBytes": 35495
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputCompositionSystemHelper.cs",
      "baselineLines": 816,
      "baselineBytes": 43697
    },
    {
      "path": "Assets/Game/Scripts/Systems/RuntimeGridBootstrapStartupSystemHelper.cs",
      "baselineLines": 137,
      "baselineBytes": 6843
    },
    {
      "path": "Assets/Game/Scripts/Systems/RuntimeResourceUtilitySystemHelper.cs",
      "baselineLines": 41,
      "baselineBytes": 1016
    },
    {
      "path": "Assets/Game/Scripts/Systems/RuntimeRootSceneSystemHelper.cs",
      "baselineLines": 35,
      "baselineBytes": 1362
    },
    {
      "path": "Assets/Game/Scripts/Systems/SceneLifecycleSceneSystemHelper.cs",
      "baselineLines": 396,
      "baselineBytes": 17718
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectedUnitOrderSnapshotCompositionSystemHelper.cs",
      "baselineLines": 127,
      "baselineBytes": 5089
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionBuildingInteractionCompositionSystemHelper.cs",
      "baselineLines": 187,
      "baselineBytes": 7339
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs",
      "baselineLines": 1589,
      "baselineBytes": 84092
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs",
      "baselineLines": 2065,
      "baselineBytes": 89390
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs",
      "baselineLines": 1347,
      "baselineBytes": 59925
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionRectangleRequestCompositionSystemHelper.cs",
      "baselineLines": 145,
      "baselineBytes": 6502
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionRuntimeConfigStartupSystemHelper.cs",
      "baselineLines": 173,
      "baselineBytes": 7934
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionRuntimeDiagnosticsSystemHelper.cs",
      "baselineLines": 305,
      "baselineBytes": 13295
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionScreenMarkerUiSystemHelper.cs",
      "baselineLines": 27,
      "baselineBytes": 772
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs",
      "baselineLines": 123,
      "baselineBytes": 3977
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionUiCameraSystemHelper.cs",
      "baselineLines": 366,
      "baselineBytes": 15645
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionUiCommandUiSystemHelper.cs",
      "baselineLines": 278,
      "baselineBytes": 10041
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionUiReadModelUiSystemHelper.cs",
      "baselineLines": 269,
      "baselineBytes": 10490
    },
    {
      "path": "Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicCameraSystemHelper.cs",
      "baselineLines": 69,
      "baselineBytes": 2707
    },
    {
      "path": "Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicPresentationSystemHelper.cs",
      "baselineLines": 49,
      "baselineBytes": 1360
    },
    {
      "path": "Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicVfxSystemHelper.cs",
      "baselineLines": 52,
      "baselineBytes": 1735
    },
    {
      "path": "Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs",
      "baselineLines": 1573,
      "baselineBytes": 67625
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingApproachSystemHelper.cs",
      "baselineLines": 671,
      "baselineBytes": 26106
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingCapacitySystemHelper.cs",
      "baselineLines": 301,
      "baselineBytes": 12913
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingCommandRoutingSystemHelper.cs",
      "baselineLines": 114,
      "baselineBytes": 5520
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingDiagnosticSystemHelper.cs",
      "baselineLines": 97,
      "baselineBytes": 4582
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingOrderPlanningSystemHelper.cs",
      "baselineLines": 429,
      "baselineBytes": 15450
    },
    {
      "path": "Assets/Game/Scripts/Systems/VisibleUnitSelectionCameraSystemHelper.cs",
      "baselineLines": 123,
      "baselineBytes": 3852
    },
    {
      "path": "Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs",
      "baselineLines": 271,
      "baselineBytes": 9448
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/ArmoryCatalogQueryUiSystemHelper.cs",
      "baselineLines": 479,
      "baselineBytes": 18116
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/AssistantHighlightPresentationSystemHelper.cs",
      "baselineLines": 163,
      "baselineBytes": 6268
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/AssistantNarrationPresentationSystemHelper.cs",
      "baselineLines": 39,
      "baselineBytes": 1159
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/AssistantPanelUiSystemHelper.cs",
      "baselineLines": 126,
      "baselineBytes": 4397
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/BattleHudRuntimeFeedbackUiSystemHelper.cs",
      "baselineLines": 301,
      "baselineBytes": 12610
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogQueryUiSystemHelper.cs",
      "baselineLines": 469,
      "baselineBytes": 17823
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.cs",
      "baselineLines": 425,
      "baselineBytes": 15806
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchHudCurrentOrderBannerUiSystemHelper.cs",
      "baselineLines": 134,
      "baselineBytes": 8312
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputUiSystemHelper.cs",
      "baselineLines": 1247,
      "baselineBytes": 51847
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchHudMinimapProjectionUiSystemHelper.cs",
      "baselineLines": 340,
      "baselineBytes": 15173
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs",
      "baselineLines": 559,
      "baselineBytes": 24832
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandTabFeedbackUiSystemHelper.cs",
      "baselineLines": 141,
      "baselineBytes": 4652
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandTabVisualUiSystemHelper.cs",
      "baselineLines": 60,
      "baselineBytes": 1869
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/QuickCustomScreenFlowUiSystemHelper.cs",
      "baselineLines": 37,
      "baselineBytes": 1196
    },
    {
      "path": "Assets/Game/Scripts/UI/Settings/SettingsScreenFlowUiSystemHelper.cs",
      "baselineLines": 42,
      "baselineBytes": 1238
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/UIScreenRouteFlowUiSystemHelper.cs",
      "baselineLines": 125,
      "baselineBytes": 3802
    }
  ],
  "productionFilesOver500": [
    {
      "path": "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
      "baselineLines": 1229,
      "baselineBytes": 68356,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
      "baselineLines": 1164,
      "baselineBytes": 55424,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs",
      "baselineLines": 828,
      "baselineBytes": 33889,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Composition/UiRuntimeAdapters.cs",
      "baselineLines": 789,
      "baselineBytes": 32622,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Configs/Audio/AudioEventIds.cs",
      "baselineLines": 1203,
      "baselineBytes": 73170,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
      "baselineLines": 757,
      "baselineBytes": 57207,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs",
      "baselineLines": 551,
      "baselineBytes": 22545,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Environment/DayNightSystem.cs",
      "baselineLines": 532,
      "baselineBytes": 21835,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeCityCompositionSystemHelper.cs",
      "baselineLines": 788,
      "baselineBytes": 42890,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs",
      "baselineLines": 570,
      "baselineBytes": 23784,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs",
      "baselineLines": 896,
      "baselineBytes": 37139,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Rendering/SharedPrefabPreviewCache.cs",
      "baselineLines": 721,
      "baselineBytes": 33765,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Rendering/Systems/UnitHelicopterBladeSpinSystem.cs",
      "baselineLines": 525,
      "baselineBytes": 23541,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs",
      "baselineLines": 736,
      "baselineBytes": 34565,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Rendering/Systems/UnitSelectionMarkerSystem.cs",
      "baselineLines": 920,
      "baselineBytes": 39469,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Rendering/Systems/UnitSelectionObjectOutlinePresentationSystem.cs",
      "baselineLines": 857,
      "baselineBytes": 37567,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Rendering/UnitImpostorPresentationSystemHelper.cs",
      "baselineLines": 928,
      "baselineBytes": 37326,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/ScenarioLab/BattleScenarioAd011Runner.cs",
      "baselineLines": 548,
      "baselineBytes": 23697,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/ScenarioLab/BattleScenarioLabModels.cs",
      "baselineLines": 598,
      "baselineBytes": 22568,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/ScenarioLab/BattleScenarioLabVisualPlayback.cs",
      "baselineLines": 2848,
      "baselineBytes": 137970,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs",
      "baselineLines": 648,
      "baselineBytes": 27105,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs",
      "baselineLines": 916,
      "baselineBytes": 37282,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/AIProductionSystem.cs",
      "baselineLines": 594,
      "baselineBytes": 25122,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/AIStartupSystem.cs",
      "baselineLines": 542,
      "baselineBytes": 24846,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/AITargetingSystem.cs",
      "baselineLines": 556,
      "baselineBytes": 25398,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/AirMissileLauncherSystems.cs",
      "baselineLines": 1153,
      "baselineBytes": 54746,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/AttackOrderCommandSystem.cs",
      "baselineLines": 545,
      "baselineBytes": 24606,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs",
      "baselineLines": 895,
      "baselineBytes": 38524,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingCombatUtilitySystemHelper.cs",
      "baselineLines": 531,
      "baselineBytes": 22279,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingDefenseAttackSystem.cs",
      "baselineLines": 529,
      "baselineBytes": 21472,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
      "baselineLines": 982,
      "baselineBytes": 45901,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs",
      "baselineLines": 771,
      "baselineBytes": 48202,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
      "baselineLines": 895,
      "baselineBytes": 37741,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs",
      "baselineLines": 1697,
      "baselineBytes": 74162,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
      "baselineLines": 1914,
      "baselineBytes": 87282,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs",
      "baselineLines": 1634,
      "baselineBytes": 73709,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeContextFactoryCompositionSystemHelper.cs",
      "baselineLines": 553,
      "baselineBytes": 35388,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
      "baselineLines": 1678,
      "baselineBytes": 74511,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs",
      "baselineLines": 619,
      "baselineBytes": 25874,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.cs",
      "baselineLines": 524,
      "baselineBytes": 25804,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs",
      "baselineLines": 508,
      "baselineBytes": 21240,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSelectionRuntimeCompositionSystemHelper.cs",
      "baselineLines": 639,
      "baselineBytes": 28385,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs",
      "baselineLines": 1824,
      "baselineBytes": 78233,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/BuildingUiQueryUiSystemHelper.cs",
      "baselineLines": 553,
      "baselineBytes": 24194,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenRefugeeCompositionSystemHelper.cs",
      "baselineLines": 654,
      "baselineBytes": 28948,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/CitizenTravelSystem.cs",
      "baselineLines": 595,
      "baselineBytes": 27926,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs",
      "baselineLines": 960,
      "baselineBytes": 44503,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs",
      "baselineLines": 864,
      "baselineBytes": 34218,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/FocusableUnitLookupCameraSystemHelper.cs",
      "baselineLines": 530,
      "baselineBytes": 22766,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs",
      "baselineLines": 852,
      "baselineBytes": 38243,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs",
      "baselineLines": 1888,
      "baselineBytes": 85734,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs",
      "baselineLines": 744,
      "baselineBytes": 29464,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/MapVehiclePlacementSpawnPrefabSystemHelper.cs",
      "baselineLines": 576,
      "baselineBytes": 23388,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs",
      "baselineLines": 756,
      "baselineBytes": 32548,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystemHelper.cs",
      "baselineLines": 909,
      "baselineBytes": 39101,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs",
      "baselineLines": 608,
      "baselineBytes": 24771,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs",
      "baselineLines": 1133,
      "baselineBytes": 47400,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/ResourceHaulerUtilitySystemHelper.cs",
      "baselineLines": 550,
      "baselineBytes": 21102,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs",
      "baselineLines": 682,
      "baselineBytes": 27070,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs",
      "baselineLines": 799,
      "baselineBytes": 38144,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsCameraRequestSystem.cs",
      "baselineLines": 542,
      "baselineBytes": 25432,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsCameraSystem.cs",
      "baselineLines": 749,
      "baselineBytes": 28732,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionCommandResultFlushCompositionSystemHelper.cs",
      "baselineLines": 1634,
      "baselineBytes": 81709,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs",
      "baselineLines": 741,
      "baselineBytes": 29338,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionInputCompositionSystemHelper.cs",
      "baselineLines": 929,
      "baselineBytes": 38098,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandCompositionSystemHelper.cs",
      "baselineLines": 1568,
      "baselineBytes": 75786,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs",
      "baselineLines": 823,
      "baselineBytes": 35495,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputCompositionSystemHelper.cs",
      "baselineLines": 816,
      "baselineBytes": 43697,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs",
      "baselineLines": 717,
      "baselineBytes": 31360,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/ScanIntelCommandSystem.cs",
      "baselineLines": 1043,
      "baselineBytes": 46101,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectedMoveOrderCommandSystem.cs",
      "baselineLines": 746,
      "baselineBytes": 36346,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectedUnitDebugFireSystem.cs",
      "baselineLines": 707,
      "baselineBytes": 32095,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs",
      "baselineLines": 1589,
      "baselineBytes": 84092,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionHudFeedbackUiSystemHelper.cs",
      "baselineLines": 2065,
      "baselineBytes": 89390,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs",
      "baselineLines": 1347,
      "baselineBytes": 59925,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/SelectionUiReadModelLookup.cs",
      "baselineLines": 826,
      "baselineBytes": 33960,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicHelper.cs",
      "baselineLines": 534,
      "baselineBytes": 24291,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs",
      "baselineLines": 1573,
      "baselineBytes": 67625,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs",
      "baselineLines": 614,
      "baselineBytes": 28165,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingApproachSystemHelper.cs",
      "baselineLines": 671,
      "baselineBytes": 26106,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs",
      "baselineLines": 3226,
      "baselineBytes": 147895,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs",
      "baselineLines": 1859,
      "baselineBytes": 91402,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitAttackOrderRequestSystem.cs",
      "baselineLines": 557,
      "baselineBytes": 24271,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitAttackSystem.cs",
      "baselineLines": 1239,
      "baselineBytes": 55234,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs",
      "baselineLines": 820,
      "baselineBytes": 37014,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs",
      "baselineLines": 583,
      "baselineBytes": 27276,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitSurfaceTrackingSystem.cs",
      "baselineLines": 614,
      "baselineBytes": 28218,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs",
      "baselineLines": 603,
      "baselineBytes": 27467,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs",
      "baselineLines": 1141,
      "baselineBytes": 46268,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs",
      "baselineLines": 604,
      "baselineBytes": 30880,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/Systems/UnitTransportRopeDisembarkSystem.cs",
      "baselineLines": 977,
      "baselineBytes": 41059,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs",
      "baselineLines": 651,
      "baselineBytes": 26620,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs",
      "baselineLines": 1908,
      "baselineBytes": 63256,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/UI/MainMenuPlayUI.cs",
      "baselineLines": 921,
      "baselineBytes": 36715,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/AriaCommandAssistantPopupView.cs",
      "baselineLines": 649,
      "baselineBytes": 25081,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs",
      "baselineLines": 1030,
      "baselineBytes": 45352,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputUiSystemHelper.cs",
      "baselineLines": 1247,
      "baselineBytes": 51847,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchHudMinimapView.cs",
      "baselineLines": 580,
      "baselineBytes": 21934,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs",
      "baselineLines": 559,
      "baselineBytes": 24832,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/AssistantCommandIntentSystem.cs",
      "baselineLines": 862,
      "baselineBytes": 38264,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs",
      "baselineLines": 635,
      "baselineBytes": 31421,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs",
      "baselineLines": 850,
      "baselineBytes": 42215,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerReadModelSystem.cs",
      "baselineLines": 594,
      "baselineBytes": 29553,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs",
      "baselineLines": 701,
      "baselineBytes": 31595,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs",
      "baselineLines": 2802,
      "baselineBytes": 123307,
      "strictNoGrowth": true
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs",
      "baselineLines": 501,
      "baselineBytes": 23391,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs",
      "baselineLines": 756,
      "baselineBytes": 33610,
      "strictNoGrowth": false
    },
    {
      "path": "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs",
      "baselineLines": 955,
      "baselineBytes": 41029,
      "strictNoGrowth": false
    }
  ],
  "approvedExceptions": [
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeAudioPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-024",
      "maxLines": 76,
      "maxBytes": 3040,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeCompositionSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-025",
      "maxLines": 453,
      "maxBytes": 19785,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeInteractivePresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-047",
      "maxLines": 159,
      "maxBytes": 6036,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeModelUtilitySystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-042",
      "maxLines": 71,
      "maxBytes": 3132,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativePanelPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-044",
      "maxLines": 130,
      "maxBytes": 5310,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeProfileCompositionSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-041",
      "maxLines": 135,
      "maxBytes": 5172,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeReviewUtilitySystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-027",
      "maxLines": 28,
      "maxBytes": 653,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeReviewPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-037",
      "maxLines": 161,
      "maxBytes": 6297,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeShellCompositionSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-036",
      "maxLines": 84,
      "maxBytes": 3061,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-048",
      "maxLines": 441,
      "maxBytes": 19352,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/Narrative/NarrativePunctuationUtilitySystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-029",
      "maxLines": 23,
      "maxBytes": 847,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Narrative/Runtime/FirstLaunchNarrativeRouteUtilitySystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-038",
      "maxLines": 96,
      "maxBytes": 3640,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Narrative/Runtime/FirstLaunchNarrativeSequenceUtilitySystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-039",
      "maxLines": 321,
      "maxBytes": 12366,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/UI/Narrative/NarrativeDialoguePresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-030",
      "maxLines": 140,
      "maxBytes": 4693,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/UI/Narrative/NarrativeDialogueRevealPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-031",
      "maxLines": 132,
      "maxBytes": 4335,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/UI/Narrative/NarrativePanelAssetResidencyPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-045",
      "maxLines": 170,
      "maxBytes": 6456,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/UI/Narrative/NarrativePanelMotionPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-033",
      "maxLines": 135,
      "maxBytes": 4542,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/UI/Narrative/NarrativeVoicePlaybackPresentationSystemHelper.cs",
      "trackerTaskId": "APH-712",
      "decisionId": "D-034",
      "maxLines": 59,
      "maxBytes": 1633,
      "scope": "system-helper"
    },
    {
      "path": "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs",
      "trackerTaskId": "APH-607",
      "decisionId": "D-022",
      "maxLines": 932,
      "maxBytes": 38015,
      "scope": "system-helper-growth"
    },
    {
      "path": "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs",
      "trackerTaskId": "APH-607",
      "decisionId": "D-023",
      "maxLines": 932,
      "maxBytes": 38015,
      "scope": "production-over-500-review"
    }
  ]
}
```
<!-- production-source-growth-manifest:end -->
