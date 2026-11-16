#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // This measures canonical starting grants through normal runtime composition.
        // The production owner constrains explicit-bound grants to their launch-bound
        // typed pad; the probe independently audits that binding and actual result.
        static JObject ProbeS049StandardRuntimePlacement(SkirmishScenarioDefinitionConfig definition,
            SkirmishMapLayoutConfig layout, System.Collections.Generic.IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            JObject candidateBounds)
        {
            var result = new JObject
            {
                ["scope"] = "S049_Standard_CanonicalStartingBuildingRequests_NormalRuntimeComposition_PostPlacementAudit",
                ["candidateOnly"] = true,
                ["requestedSize"] = SkirmishSizeId.Standard.ToString(),
                ["requestedCatalogId"] = definition.CatalogId,
                ["requestedDefinitionId"] = definition.DefinitionId,
                ["requestedCampaignOperationMapId"] = definition.CampaignMap?.OperationMapId,
                ["requestedCampaignContentHash"] = definition.CampaignMap?.ContentHash,
                ["canonicalStartingBuildingOwnerUsed"] = true,
                ["normalGameplayCompositionUsed"] = true,
                ["typedPadConstrainedDuringSearch"] = false,
                ["typedPadConstraintOwner"] = "BuildingRuntimeProcessingCompositionSystemHelper_WithPlacementValidation",
                ["postPlacementTypedPadAndBoundsAudit"] = false,
                ["typedPadConstraintExpectedForExplicitBoundLaunch"] = true,
                ["transientCandidateBoundsConstrainedDuringRuntimePlacement"] = false,
                ["transientCandidateBoundsAuditedAfterPlacementOnly"] = true,
                ["liveUnitOccupancyMeasured"] = false,
                ["liveRepairMeasured"] = false,
                ["otherSizesMeasured"] = false,
                ["sourceFitCertified"] = false,
                ["missionReady"] = false,
                ["playerAccepted"] = false,
                ["status"] = "Incomplete"
            };
            bool exactSource = definition.CatalogId == "S049" && definition.CampaignMap != null &&
                definition.CampaignMap.OperationMapId == "opmap.ch04.armor_break" &&
                definition.CampaignMap.ContentHash == "da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86";
            result["exactS049ArmorBreakCandidate"] = exactSource;
            if (!exactSource)
            {
                result["status"] = "Incomplete_ExactS049ArmorBreakCandidateRequired";
                return result;
            }
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1,
                new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                out var setup, out var reasons))
            {
                result["compileFailures"] = new JArray(reasons);
                result["status"] = "Incomplete_ExactStandardSetupCompileFailed";
                return result;
            }
            result["setupHash"] = setup.SetupHash.ToString("x8");
            result["canonicalStructureCount"] = setup.Structures?.Length ?? 0;
            result["canonicalForceCount"] = setup.Forces?.Length ?? 0;
            if (setup.Structures == null || setup.Structures.Length == 0)
            {
                result["status"] = "Incomplete_ExactStandardSetupHasNoStructures";
                return result;
            }

            var previousDefault = World.DefaultGameObjectInjectionWorld;
            var worldsBefore = CaptureMaterializationWorldCensus();
            var scenesBefore = Ground30SceneOwnership();
            int scenes = UnityEngine.SceneManagement.SceneManager.sceneCount;
            int previews = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            BuildingGameplayResultCompositionSystemHelper.Result? runtime = null;
            BuildingGameplayCompositionSystemHelper composition = null;
            SkirmishProductionCatalog productionCatalog = null;
            GameObject root = null;
            World runtimeWorld = null;
            Exception failure = null;
            Exception cleanupFailure = null;
            bool complete = false;
            var runtimeInputPins = new System.Collections.Generic.List<(UnityEngine.Object Asset, string Path, string Sha256)>();
            void PinAsset(UnityEngine.Object asset)
            {
                if (asset == null) return;
                string path = AssetDatabase.GetAssetPath(asset);
                Require(!string.IsNullOrEmpty(path) && !EditorUtility.IsDirty(asset), "Clean direct runtime input required: " + asset.name);
                runtimeInputPins.Add((asset, path, Hash(path)));
            }
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                    "Idle Editor required for disposable S049 runtime placement probe");
                string sceneGuid = definition.CampaignMap.NavigationMetadata.AuthoredSubSceneGuid;
                string surfacePath = AssetDatabase.GUIDToAssetPath(definition.CampaignMap.MapSurfaceDataReference.AssetGUID);
                Require(!string.IsNullOrEmpty(sceneGuid) && !string.IsNullOrEmpty(surfacePath),
                    "Exact candidate Campaign scene/surface references required");
                runtimeWorld = S049PlacementRuntimeWorld();
                Load(runtimeWorld, sceneGuid);
                Load(runtimeWorld, AssetDatabase.AssetPathToGUID(Support));
                // Run the normal blocker lifecycle once after both immutable source
                // scenes are streamed. These are the production owners, selected
                // explicitly so the disposable world does not run unrelated gameplay.
                runtimeWorld.Update();
                using var runtimeSurface = PublishMissingPinnedSurface(runtimeWorld, definition.CampaignMap, surfacePath, result);
                World.DefaultGameObjectInjectionWorld = runtimeWorld;
                EntityManager em = runtimeWorld.EntityManager;
                using (var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad)))
                using (var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent)))
                {
                    Require(grids.CalculateEntityCount() == 1 && surfaces.CalculateEntityCount() == 1,
                        "One original Campaign grid/surface pair required by runtime owner");
                    var runtimeGridSnapshot = grids.GetSingleton<GridConfig>();
                    var surface = surfaces.GetSingleton<MapSurfaceComponent>();
                    Require(runtimeGridSnapshot.Width == definition.CampaignMap.GridMetadata.Dimensions.x &&
                        runtimeGridSnapshot.Height == definition.CampaignMap.GridMetadata.Dimensions.y &&
                        runtimeGridSnapshot.CellSize == definition.CampaignMap.GridMetadata.CellSize &&
                        math.all(runtimeGridSnapshot.Origin == (float3)definition.CampaignMap.GridMetadata.Origin) &&
                        surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated &&
                        surface.Dimensions.Equals(new int2(runtimeGridSnapshot.Width, runtimeGridSnapshot.Height)) &&
                        surface.CellSize == runtimeGridSnapshot.CellSize && math.all(surface.GridOrigin == runtimeGridSnapshot.Origin),
                        "Runtime probe loaded the exact original Campaign grid/surface frame");
                    result["exactOriginalCampaignGridSurfaceObserved"] = true;
                    Entity gridEntity = grids.GetSingletonEntity();
                    using var sourceBlockers = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
                    int sourceBlockerCount = sourceBlockers.CalculateEntityCount();
                    using var runtimeUnits = em.CreateEntityQuery(typeof(UnitGrid));
                    bool blockerComponentsPresent = em.HasComponent<DynamicBlockerComponent>(gridEntity) &&
                        em.HasComponent<DynamicOccupancyComponent>(gridEntity) && em.HasComponent<PathPoolComponent>(gridEntity);
                    var blockerState = blockerComponentsPresent ? em.GetComponentData<DynamicBlockerComponent>(gridEntity) : default;
                    var occupancyState = blockerComponentsPresent ? em.GetComponentData<DynamicOccupancyComponent>(gridEntity) : default;
                    var pathPoolState = blockerComponentsPresent ? em.GetComponentData<PathPoolComponent>(gridEntity) : default;
                    int gridCellCount = runtimeGridSnapshot.Width * runtimeGridSnapshot.Height;
                    bool blockerStorageValid = blockerComponentsPresent && blockerState.GridSize == gridCellCount &&
                        blockerState.Counts.IsCreated && blockerState.Counts.Length == gridCellCount &&
                        blockerState.Blocked.IsCreated && blockerState.Blocked.Length == gridCellCount &&
                        blockerState.FriendlyPassFactionIds.IsCreated && blockerState.FriendlyPassFactionIds.Length == gridCellCount &&
                        occupancyState.GridSize == gridCellCount && occupancyState.Occupied.IsCreated &&
                        occupancyState.Occupied.Length == gridCellCount && pathPoolState.Cells.IsCreated;
                    bool staticSourcePassCommitted = blockerStorageValid &&
                        RuntimeGridBlockerEpoch.IsCommitted(runtimeGridSnapshot, blockerState);
                    result["normalBlockerLifecycleSystemsInstalled"] = new JArray("RuntimeGridDeduplicationSystem",
                        "DynamicBlockerInitSystem", "StaticGridBlockerUpdateSystem", "DynamicOccupancyRebuildSystem");
                    result["originalSourceStaticBlockerEntityCount"] = sourceBlockerCount;
                    result["dynamicOccupancyRebuildEligibleUnitCount"] = runtimeUnits.CalculateEntityCount();
                    result["dynamicBlockerStorageValid"] = blockerStorageValid;
                    result["sourceStaticBlockerPassCommitted"] = staticSourcePassCommitted;
                    result["dynamicBlockerAppliedRevision"] = blockerState.AppliedRevision;
                    Require(blockerStorageValid && staticSourcePassCommitted,
                        "Normal dynamic blocker initialization and source static blocker pass did not commit");
                }

                const string placementPath = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
                var scenePlacement = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(placementPath);
                Require(scenePlacement != null && !EditorUtility.IsDirty(scenePlacement), "Clean normal scene building placement config required");
                var baselinePreset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
                Require(baselinePreset != null && baselinePreset.buildingPlacement != null &&
                    !EditorUtility.IsDirty(baselinePreset), "Exact normal Desert Base source preset required for the S049 scene composition");
                var baselinePlacement = baselinePreset.buildingPlacement;
                Require(!EditorUtility.IsDirty(baselinePlacement), "Clean normal baseline building placement config required");
                PinAsset(baselinePreset);
                PinAsset(baselinePlacement);
                PinAsset(baselinePlacement.UnitPrefabRegistryConfig);
                PinAsset(baselinePlacement.InitialUnitsConfig);
                foreach (var prefab in baselinePlacement.Spawnables ?? new System.Collections.Generic.List<GameObject>()) PinAsset(prefab);
                PinAsset(scenePlacement);
                PinAsset(scenePlacement.UnitPrefabRegistryConfig);
                PinAsset(scenePlacement.InitialUnitsConfig);
                foreach (var prefab in scenePlacement.Spawnables ?? new System.Collections.Generic.List<GameObject>()) PinAsset(prefab);
                runtimeInputPins.Add((null, Support, Hash(Support)));
                result["baselinePresetScenarioIndex"] = SkirmishPresetConfig.DesertBaseScenarioIndex;
                result["baselinePresetPath"] = AssetDatabase.GetAssetPath(baselinePreset);
                result["baselinePlacementPath"] = AssetDatabase.GetAssetPath(baselinePlacement);
                result["runtimeInputPins"] = new JArray(runtimeInputPins.Select(pin => new JObject
                    { ["path"] = pin.Path, ["sha256"] = pin.Sha256 }));
                result["placementConfigPath"] = placementPath;
                result["placementConfigSha256"] = Hash(placementPath);
                // MatchSceneView.ResolvedSkirmishBuildingPlacement uses this exact
                // setup-bound production catalog. It includes every structure in
                // the compiled setup from the existing baseline/scene prefab catalogs.
                productionCatalog = SkirmishProductionCatalog.Create(setup, baselinePlacement, scenePlacement,
                    BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata);
                Require(productionCatalog?.Config != null, "Normal setup-bound SkirmishProductionCatalog overlay required");
                result["setupBoundProductionCatalogOverlayUsed"] = true;
                result["productionCatalogSpawnableCount"] = productionCatalog.Config.Spawnables?.Count ?? 0;
                result["productionCatalogIncludesEveryCanonicalStructure"] = setup.Structures.All(structure =>
                    productionCatalog.Config.Spawnables.Any(prefab => prefab != null &&
                        BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name) ==
                        BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(SkirmishStructureIds.VisualKey(structure.StructureId))));
                Require((bool)result["productionCatalogIncludesEveryCanonicalStructure"],
                    "Normal setup-bound production catalog omitted a canonical structure prefab");
                root = new GameObject("S049_Standard_DisposableRuntimePlacementRoot");
                composition = new BuildingGameplayCompositionSystemHelper();
                runtime = composition.Initialize(
                    buildingPlacementConfig: productionCatalog.Config,
                    worldCamera: null,
                    runtimeTransportsRoot: root.transform,
                    runtimeUiRoot: root.transform,
                    roadFootprintState: default,
                    factionVisuals: null,
                    dayNight: null,
                    prepareTransportDropVisual: BuildingProductionUnitMetadataPrefabSystemHelper.PrepareTransportDropVisual,
                    tryGetUnitProductionMetadata: BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata,
                    resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
                    tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                    tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);

                // MatchBootstrap owns ECS boundary creation after managed gameplay
                // composition initialization. Reuse that production lifecycle owner;
                // never synthesize the state entity or its buffers in this probe.
                Entity bootstrappedBoundary = MatchBuildingRuntimeBootstrapStartupSystemHelper.Ensure(Entity.Null);
                Require(bootstrappedBoundary != Entity.Null && em.Exists(bootstrappedBoundary) &&
                    em.HasComponent<BuildingRuntimeStateTag>(bootstrappedBoundary),
                    "Normal MatchBootstrap boundary owner did not create/reuse its runtime boundary");
                result["normalMatchBootstrapBoundaryOwnerUsed"] = true;
                result["bootstrappedBoundaryEntity"] = new JObject
                    { ["index"] = bootstrappedBoundary.Index, ["version"] = bootstrappedBoundary.Version };

                // Its normal startup tick publishes configured-spawnable read models
                // before the canonical starting-building service runs.
                runtime.Value.RuntimeUpdate.UpdateStartup(runtime.Value.RuntimeUpdateContext);
                var configuredFootprints = new System.Collections.Generic.Dictionary<string, int2>(StringComparer.Ordinal);
                using (var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingRuntimeSpawnRequest), typeof(BuildingConfiguredSpawnableReadModel)))
                {
                    Require(boundaries.CalculateEntityCount() == 1 && boundaries.GetSingletonEntity() == bootstrappedBoundary,
                        "Normal composition did not publish one complete building runtime boundary");
                    var configured = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaries.GetSingletonEntity(), true);
                    result["configuredSpawnableReadModelCount"] = configured.Length;
                    var structureReadiness = new JArray();
                    result["canonicalStructureSpawnableReadiness"] = structureReadiness;
                    var configuredEntries = configured.ToNativeArray(Allocator.Temp);
                    try
                    {
                    foreach (var structure in setup.Structures)
                    {
                        string visualKey = SkirmishStructureIds.VisualKey(structure.StructureId);
                        var found = configuredEntries.FirstOrDefault(item =>
                            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(item.BuildingId.ToString()) ==
                            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(visualKey));
                        bool available = found.FootprintCells.x > 0 && found.FootprintCells.y > 0;
                        if (available)
                            configuredFootprints[BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(visualKey)] = found.FootprintCells;
                        structureReadiness.Add(new JObject { ["faction"] = structure.FactionId,
                            ["structureId"] = structure.StructureId, ["requiredVisualKey"] = visualKey,
                            ["configuredReadModelKey"] = found.BuildingId.ToString(),
                            ["footprintCells"] = new JArray(found.FootprintCells.x, found.FootprintCells.y),
                            ["available"] = available });
                    }
                    }
                    finally { configuredEntries.Dispose(); }
                    result["missingCanonicalStructureReadModelCount"] = structureReadiness.OfType<JObject>()
                        .Count(row => (bool?)row["available"] != true);
                }

                var sessionId = new FixedString64Bytes("S049.Standard.RuntimeProbe");
                var payload = new SkirmishLaunchPayload
                {
                    SessionId = sessionId.ToString(), CatalogId = definition.CatalogId,
                    DefinitionId = definition.DefinitionId, ContentVersion = definition.ContentVersion,
                    AllConfigHashes = setup.SetupHash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture),
                    MapId = definition.CampaignMap.OperationMapId, LayoutId = definition.MapLayoutId,
                    Seed = setup.Seed, SizeId = setup.SizeId, DifficultyId = setup.DifficultyId,
                    IsLegacy = false
                };
                var battleCatalog = SkirmishBattleCatalogConfig.Load();
                Require(battleCatalog != null, "Current production Skirmish battle catalog required for launch preflight");
                PinAsset(battleCatalog);
                result["runtimeInputPins"] = new JArray(runtimeInputPins.Select(pin => new JObject
                    { ["path"] = pin.Path, ["sha256"] = pin.Sha256 }));
                var catalogRows = battleCatalog.Entries?.Where(entry => entry.ScenarioId == definition.CatalogId).ToArray()
                    ?? System.Array.Empty<SkirmishBattleCatalogEntry>();
                bool catalogRowUnique = catalogRows.Length == 1;
                SkirmishBattleCatalogEntry catalogRow = catalogRowUnique ? catalogRows[0] : default;
                bool genericCatalogLookup = SkirmishMissionDispatch.TryGetGenericById(
                    battleCatalog, definition.CatalogId, out var genericCatalogEntry);
                bool catalogDefinitionMatches = genericCatalogLookup &&
                    SkirmishMissionDispatch.MatchesDefinition(genericCatalogEntry, definition);
                string actualLayoutFingerprint = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout);
                string reviewedLayoutFingerprint = definition.MissionLogicalBinding?.ReviewedLayoutSemanticFingerprint;
                bool reviewedBindingFingerprintMatches = !string.IsNullOrEmpty(reviewedLayoutFingerprint) &&
                    string.Equals(reviewedLayoutFingerprint, actualLayoutFingerprint, StringComparison.Ordinal);
                Game.Missions.Contracts.ContentAccessState contentAccessState =
                    ContentAccessRuntime.Evaluate(definition.DefinitionId);
                var blockedProductionLaunchGates = new JArray();
                if (!catalogRowUnique || !catalogDefinitionMatches || !catalogRow.IsPlayable)
                    blockedProductionLaunchGates.Add("PublishedPlayableS049BattleCatalogEntryRequired");
                if (!reviewedBindingFingerprintMatches)
                    blockedProductionLaunchGates.Add("ReviewedMissionLayoutFingerprintMustMatchCandidateLayout");
                if (contentAccessState != Game.Missions.Contracts.ContentAccessState.Allowed)
                    blockedProductionLaunchGates.Add("ContentAccessMustBeAllowed");
                bool launchPreflightEligible = blockedProductionLaunchGates.Count == 0;
                var launchPreflight = new JObject
                {
                    ["scope"] = "ReadOnlyCurrentProductionLaunchPreflight",
                    ["catalogAssetPath"] = AssetDatabase.GetAssetPath(battleCatalog),
                    ["catalogAssetSha256"] = Hash(AssetDatabase.GetAssetPath(battleCatalog)),
                    ["catalogRowCountForRequestedId"] = catalogRows.Length,
                    ["catalogEntryStatus"] = catalogRowUnique ? catalogRow.Status.ToString() : null,
                    ["catalogEntryPlayableScenarioIndex"] = catalogRowUnique ? (JToken)catalogRow.PlayableScenarioIndex : JValue.CreateNull(),
                    ["catalogEntryDefinitionId"] = catalogRowUnique ? catalogRow.DefinitionId : null,
                    ["catalogEntryContentVersion"] = catalogRowUnique ? (JToken)catalogRow.ContentVersion : JValue.CreateNull(),
                    ["catalogEntryReadinessManifestId"] = catalogRowUnique ? catalogRow.ReadinessManifestId : null,
                    ["genericCatalogLookupSucceeded"] = genericCatalogLookup,
                    ["catalogDefinitionMatchesRequestedDefinition"] = catalogDefinitionMatches,
                    ["catalogPublicationGatePassed"] = catalogRowUnique && catalogRow.IsPlayable && catalogDefinitionMatches,
                    ["reviewedLayoutFingerprint"] = reviewedLayoutFingerprint,
                    ["candidateLayoutFingerprint"] = actualLayoutFingerprint,
                    ["reviewedLayoutFingerprintMatchesCandidate"] = reviewedBindingFingerprintMatches,
                    ["reviewedBindingFingerprintGatePassed"] = reviewedBindingFingerprintMatches,
                    ["contentAccessState"] = contentAccessState.ToString(),
                    ["contentAccessGatePassed"] = contentAccessState == Game.Missions.Contracts.ContentAccessState.Allowed,
                    ["payloadAllConfigHashes"] = payload.AllConfigHashes,
                    ["payloadSetupHashMatchesCompiledSetup"] = payload.AllConfigHashes ==
                        setup.SetupHash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture),
                    ["blockedRequiredGates"] = blockedProductionLaunchGates,
                    ["productionLaunchPreflightEligible"] = launchPreflightEligible,
                    ["productionLaunchAttemptMade"] = false
                };
                result["productionLaunchPreflight"] = launchPreflight;
                if (!launchPreflightEligible)
                {
                    result["productionLaunchAttemptMade"] = false;
                    result["status"] = "Incomplete_ProductionLaunchPreflightBlocked";
                    complete = true;
                    return result;
                }
                launchPreflight["productionLaunchAttemptMade"] = true;
                result["productionLaunchAttemptMade"] = true;
                Require(SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup, null, definition),
                    "S049 runtime placement must use the production expanded launch/session binding path");
                using var launchedSessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),
                    typeof(SkirmishLaunchDefinitionRecord), typeof(SkirmishResolvedSetupRecord));
                Require(launchedSessions.CalculateEntityCount() == 1,
                    "Production expanded launch must retain its exact resolved setup and launch definition records");
                Entity session = launchedSessions.GetSingletonEntity();
                Require(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.Equals(sessionId),
                    "Production launch session identity differs from the pinned S049 runtime probe identity");

                bool ready = false;
                SkirmishReasonCode reason = SkirmishReasonCode.None;
                for (int tick = 0; tick < 20 && !ready; tick++)
                {
                    runtimeWorld.SetTime(new Unity.Core.TimeData(runtimeWorld.Time.ElapsedTime + 0.2, 0.2f));
                    runtime.Value.RuntimeUpdate.UpdateStartup(runtime.Value.RuntimeUpdateContext);
                    ready = SkirmishStartingBuildingsService.Step(em, session, setup, out reason);
                    if (!ready && reason == SkirmishReasonCode.None)
                        runtime.Value.RuntimeUpdate.Update(runtime.Value.RuntimeUpdateContext);
                    else if (reason != SkirmishReasonCode.None)
                        break;
                }
                result["startingBuildingServiceCompleted"] = ready;
                result["startingBuildingFailureCode"] = reason.ToString();
                var placements = new JArray();
                result["placements"] = placements;
                result["startingBuildingRequestBufferPresent"] = em.HasBuffer<SkirmishStartingBuildingRequest>(session);
                SkirmishLaunchDefinitionRecord launchRecord = null;
                SkirmishResolvedSetupRecord setupRecord = null;
                if (em.HasComponent<SkirmishLaunchDefinitionRecord>(session))
                    launchRecord = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
                if (em.HasComponent<SkirmishResolvedSetupRecord>(session))
                    setupRecord = em.GetComponentObject<SkirmishResolvedSetupRecord>(session);
                bool launchRecordMatchesSetup = launchRecord != null && setupRecord != null &&
                    ReferenceEquals(launchRecord.Definition, definition) && ReferenceEquals(launchRecord.Definition.MapLayout, layout) &&
                    ReferenceEquals(setupRecord.Setup, setup) &&
                    launchRecord.Matches(setup, em.GetComponentData<SkirmishExpandedSessionComponent>(session));
                result["exactLiveLaunchRecordMatchesSetup"] = launchRecordMatchesSetup;
                int submittedCanonicalRequests = 0;
                using (var submittedBoundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingRuntimeSpawnRequest)))
                {
                    if (submittedBoundaries.CalculateEntityCount() == 1)
                    {
                        var submitted = em.GetBuffer<BuildingRuntimeSpawnRequest>(submittedBoundaries.GetSingletonEntity(), true);
                        for (int i = 0; i < submitted.Length; i++)
                            if (submitted[i].AuthoredStartingGrant != 0 && submitted[i].PlanEntity == session)
                                submittedCanonicalRequests++;
                    }
                }
                result["canonicalRuntimeSpawnRequestsSubmitted"] = submittedCanonicalRequests;
                if (!em.HasBuffer<SkirmishStartingBuildingRequest>(session))
                {
                    // The service preflights every required structure before adding
                    // its request buffer. Preserve that owner result instead of
                    // manufacturing a buffer or replacing its reason with GetBuffer's exception.
                    result["observedCanonicalGrantCount"] = 0;
                    result["expectedCanonicalStructureCount"] = setup.Structures.Length;
                    result["missingCanonicalGrantCount"] = setup.Structures.Length;
                    result["allCanonicalStructuresPlacedAndAudited"] = false;
                    result["status"] = reason == SkirmishReasonCode.MissingReference
                        ? "Incomplete_MissingCanonicalStructureBinding_NoGrantBufferCreated"
                        : "Incomplete_StartingGrantBufferAbsent_ReasonPreserved";
                    complete = true;
                    return result;
                }
                using var gridQuery = em.CreateEntityQuery(typeof(GridConfig));
                GridConfig grid = gridQuery.GetSingleton<GridConfig>();
                var stagedRequests = em.GetBuffer<SkirmishStartingBuildingRequest>(session, true);
                using var requestBoundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingRuntimeSpawnRequest));
                Require(requestBoundaries.CalculateEntityCount() == 1, "Runtime request boundary disappeared during placement");
                var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(requestBoundaries.GetSingletonEntity(), true);
                var standardFacilities = candidateBounds["standardFacilities"] as JArray ?? new JArray();
                bool allPlaced = ready && stagedRequests.Length == setup.Structures.Length;
                int missingGrantCount = Math.Max(0, setup.Structures.Length - stagedRequests.Length);
                for (int missing = stagedRequests.Length; missing < setup.Structures.Length; missing++)
                    placements.Add(new JObject { ["structureIndex"] = missing, ["status"] = "MissingCanonicalStartingGrant" });
                for (int i = 0; i < stagedRequests.Length; i++)
                {
                    var queued = stagedRequests[i];
                    var request = requests.ToNativeArray(Allocator.Temp);
                    try
                    {
                        BuildingRuntimeSpawnRequest actual = default;
                        bool found = false;
                        foreach (var row in request)
                            if (row.RequestId == queued.RequestId) { actual = row; found = true; break; }
                        if (!found)
                        {
                            allPlaced = false;
                            placements.Add(new JObject { ["structureIndex"] = queued.StructureIndex, ["status"] = "MissingRuntimeRequest" });
                            continue;
                        }
                        var structure = setup.Structures[queued.StructureIndex];
                        string visualKey = SkirmishStructureIds.VisualKey(structure.StructureId);
                        string normalizedVisualKey = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(visualKey);
                        Require(configuredFootprints.TryGetValue(normalizedVisualKey, out int2 configuredFootprint),
                            "Canonical configured read-model footprint required for request-origin audit");
                        var expected = standardFacilities.OfType<JObject>().SingleOrDefault(r =>
                            (string)r["structureId"] == structure.StructureId && (int?)r["faction"] == structure.FactionId);
                        Require(expected != null, "Exact Standard static typed-pad manifest entry required for each actual structure");
                        string padId = (string)expected["padId"];
                        var pads = layout.Pads.Where(p => p.FactionId == structure.FactionId && p.PadId == padId).ToArray();
                        Require(pads.Length == 1, "Unique exact typed facility pad required for runtime structure");
                        RectInt typed = PilotExactAxisAlignedPadCells(layout, pads[0], grid);
                        SkirmishLegalPadConfig runtimePad = default;
                        bool runtimePadResolved = launchRecordMatchesSetup &&
                            SkirmishStartingGrantPlacementConstraint.TryResolvePad(
                                launchRecord.Definition.MapLayout,
                                setup, queued.StructureIndex, out runtimePad);
                        bool runtimePadMatchesManifest = runtimePadResolved && runtimePad.PadId == padId &&
                            runtimePad.AnchorId == pads[0].AnchorId && runtimePad.FactionId == structure.FactionId &&
                            runtimePad.Kind == pads[0].Kind && Math.Abs(runtimePad.CenterX - pads[0].CenterX) <= .001f &&
                            Math.Abs(runtimePad.CenterZ - pads[0].CenterZ) <= .001f &&
                            Math.Abs(runtimePad.WidthMetres - pads[0].WidthMetres) <= .001f &&
                            Math.Abs(runtimePad.DepthMetres - pads[0].DepthMetres) <= .001f;
                        var declaredWorld = candidateBounds[structure.FactionId.ToString()] as JArray;
                        Require(declaredWorld != null && declaredWorld.Count == 4, "Exact declared candidate bounds required for faction");
                        RectInt declared = PilotRoundPadCells((float)declaredWorld[0], (float)declaredWorld[1],
                            (float)declaredWorld[2], (float)declaredWorld[3], grid, false);
                        var rect = new RectInt(actual.ActualOrigin.x, actual.ActualOrigin.y,
                            actual.ActualFootprint.x, actual.ActualFootprint.y);
                        int2 compiledAnchor = GridUtils.WorldToCell(grid,
                            new float3(structure.SpawnWorldX, 0f, structure.SpawnWorldZ));
                        int2 expectedPreferred = compiledAnchor - configuredFootprint / 2;
                        bool requestedOriginMatchesCompiledAnchor = actual.PreferredOrigin.Equals(expectedPreferred);
                        bool inTyped = actual.Status == BuildingRuntimeSpawnRequest.Succeeded && PilotCompoundContains(typed, rect);
                        bool insideTypedPadByRuntimePredicate = actual.Status == BuildingRuntimeSpawnRequest.Succeeded &&
                            launchRecordMatchesSetup && runtimePadMatchesManifest &&
                            SkirmishStartingGrantPlacementConstraint.ContainsFootprint(
                                launchRecord.Definition.MapLayout,
                                runtimePad, grid, launchRecord.Definition.CampaignMap.Bounds, rect);
                        bool inDeclared = actual.Status == BuildingRuntimeSpawnRequest.Succeeded && PilotCompoundContains(declared, rect);
                        bool entityBound = queued.BoundEntity != Entity.Null && em.Exists(queued.BoundEntity) &&
                            em.HasComponent<SkirmishStructureIdentityComponent>(queued.BoundEntity) &&
                            em.GetComponentData<SkirmishStructureIdentityComponent>(queued.BoundEntity).StructureId.ToString() == structure.StructureId &&
                            em.HasComponent<SkirmishSharedActorTag>(queued.BoundEntity);
                        Entity combat = Entity.Null, blocker = Entity.Null;
                        if (runtime.Value.RuntimeBuildings.TryGetValue(actual.BuildingRuntimeId, out var runtimeBuilding))
                        { combat = runtimeBuilding.CombatEntity; blocker = runtimeBuilding.BlockerEntity; }
                        bool runtimeEntities = combat != Entity.Null && blocker != Entity.Null && em.Exists(combat) && em.Exists(blocker) &&
                            em.HasComponent<RuntimeBuildingCombatInfo>(combat) && em.HasComponent<StaticGridBlocker>(blocker);
                        bool typedPadRequired = SkirmishStartingGrantPlacementConstraint.RequiresTypedConstraint(
                            authoredStartingGrant: true, explicitBoundLaunch: SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup));
                        bool typedPadFlagObserved = actual.RequireTypedPad == (byte)(typedPadRequired ? 1 : 0);
                        bool exactRequest = actual.AuthoredStartingGrant != 0 && actual.PlanEntity == session &&
                            actual.EntryIndex == queued.StructureIndex && actual.FactionId == structure.FactionId &&
                            actual.RequestKind == BuildingRuntimeSpawnRequest.KindBuilding &&
                            actual.BuildingId.ToString() == SkirmishStructureIds.VisualKey(structure.StructureId) &&
                            typedPadRequired && typedPadFlagObserved && launchRecordMatchesSetup && runtimePadMatchesManifest;
                        bool passed = actual.Status == BuildingRuntimeSpawnRequest.Succeeded && exactRequest &&
                            requestedOriginMatchesCompiledAnchor &&
                            insideTypedPadByRuntimePredicate && inTyped && inDeclared && entityBound && runtimeEntities;
                        allPlaced &= passed;
                        placements.Add(new JObject
                        {
                            ["structureIndex"] = queued.StructureIndex, ["structureId"] = structure.StructureId,
                            ["faction"] = structure.FactionId, ["requestId"] = actual.RequestId,
                            ["requestStatus"] = actual.Status == BuildingRuntimeSpawnRequest.Succeeded ? "Succeeded" :
                                actual.Status == BuildingRuntimeSpawnRequest.Failed ? "Failed" : "Pending",
                            ["requestResultCode"] = actual.ResultCode,
                            ["requestResultCodeName"] = actual.ResultCode == BuildingRuntimeSpawnRequest.Blocked ? "Blocked" :
                                actual.ResultCode == BuildingRuntimeSpawnRequest.MissingConfig ? "MissingConfig" :
                                actual.ResultCode == 0 ? "None" : "Unknown",
                            ["runtimeBuildingId"] = actual.BuildingRuntimeId,
                            ["requestPreferredOrigin"] = new JArray(actual.PreferredOrigin.x, actual.PreferredOrigin.y),
                            ["expectedPreferredOriginFromCompiledAnchor"] = new JArray(expectedPreferred.x, expectedPreferred.y),
                            ["configuredReadModelFootprint"] = new JArray(configuredFootprint.x, configuredFootprint.y),
                            ["requestPreferredOriginMatchesCompiledAnchor"] = requestedOriginMatchesCompiledAnchor,
                            ["exactCanonicalStartingGrantRequest"] = exactRequest,
                            ["requestRequiresTypedPad"] = actual.RequireTypedPad != 0,
                            ["requestTypedPadFlagMatchesExpected"] = typedPadFlagObserved,
                            ["launchRecordMatchesSetup"] = launchRecordMatchesSetup,
                            ["resolvedRuntimePadId"] = runtimePadResolved ? runtimePad.PadId : null,
                            ["resolvedRuntimeAnchorId"] = runtimePadResolved ? runtimePad.AnchorId : null,
                            ["runtimePadMatchesManifestIdentity"] = runtimePadMatchesManifest,
                            ["actualOrigin"] = new JArray(actual.ActualOrigin.x, actual.ActualOrigin.y),
                            ["actualFootprint"] = new JArray(actual.ActualFootprint.x, actual.ActualFootprint.y),
                            ["actualRect"] = RectJson(rect), ["typedPadId"] = padId, ["typedPadCells"] = RectJson(typed),
                            ["declaredBoundsCells"] = RectJson(declared), ["insideTypedPad"] = inTyped,
                            ["insideTypedPadByRuntimePredicate"] = insideTypedPadByRuntimePredicate,
                            ["insideDeclaredBounds"] = inDeclared, ["canonicalStructureBindingObserved"] = entityBound,
                            ["requestMatchesCanonicalStructure"] = exactRequest,
                            ["combatEntityObserved"] = combat != Entity.Null && em.Exists(combat),
                            ["staticBlockerEntityObserved"] = blocker != Entity.Null && em.Exists(blocker),
                            ["runtimeCombatAndBlockerContractsObserved"] = runtimeEntities,
                            ["passed"] = passed
                        });
                    }
                    finally { request.Dispose(); }
                }
                result["expectedCanonicalStructureCount"] = setup.Structures.Length;
                result["observedCanonicalGrantCount"] = stagedRequests.Length;
                result["missingCanonicalGrantCount"] = missingGrantCount;
                result["typedPadConstrainedDuringSearch"] = stagedRequests.Length == setup.Structures.Length &&
                    placements.OfType<JObject>().Count() == setup.Structures.Length &&
                    placements.OfType<JObject>().All(row => (bool?)row["requestRequiresTypedPad"] == true &&
                        (bool?)row["requestTypedPadFlagMatchesExpected"] == true &&
                        (bool?)row["launchRecordMatchesSetup"] == true &&
                        (bool?)row["runtimePadMatchesManifestIdentity"] == true);
                result["postPlacementTypedPadAndBoundsAudit"] = stagedRequests.Length > 0;
                result["allCanonicalStructuresPlacedAndAudited"] = allPlaced;
                result["actualRuntimePlacementMeasured"] = placements.OfType<JObject>().Any(r => (string)r["requestStatus"] == "Succeeded");
                result["status"] = allPlaced ? "StandardCanonicalStructurePlacementObserved_PadAndBoundsPassed" :
                    "IncompleteOrFailed_StandardCanonicalPlacementOrPostAudit";
                complete = true;
            }
            catch (Exception error)
            {
                failure = error;
                result["error"] = error.ToString();
                result["status"] = "Incomplete_RuntimePrerequisiteOrPlacementFailure";
            }
            finally
            {
                try { runtime?.Dispose?.Invoke(); }
                catch (Exception disposeError) { cleanupFailure = disposeError; }
                try { productionCatalog?.Dispose(); }
                catch (Exception catalogDisposeError)
                { cleanupFailure = cleanupFailure == null ? catalogDisposeError : new AggregateException(cleanupFailure, catalogDisposeError); }
                try { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
                catch (Exception rootError) { cleanupFailure = cleanupFailure == null ? rootError : new AggregateException(cleanupFailure, rootError); }
                try { World.DefaultGameObjectInjectionWorld = previousDefault; }
                catch (Exception restoreError) { cleanupFailure = cleanupFailure == null ? restoreError : new AggregateException(cleanupFailure, restoreError); }
                try { if (runtimeWorld != null && runtimeWorld.IsCreated) runtimeWorld.Dispose(); }
                catch (Exception worldError) { cleanupFailure = cleanupFailure == null ? worldError : new AggregateException(cleanupFailure, worldError); }
                bool restored = false;
                try
                {
                    bool pinsRestored = runtimeInputPins.All(pin =>
                        (pin.Asset == null || !EditorUtility.IsDirty(pin.Asset)) && Hash(pin.Path) == pin.Sha256);
                    bool worldsRestored = worldsBefore.SequenceEqual(CaptureMaterializationWorldCensus());
                    bool defaultRestored = ReferenceEquals(previousDefault, World.DefaultGameObjectInjectionWorld);
                    bool countsRestored = scenes == UnityEngine.SceneManagement.SceneManager.sceneCount &&
                        previews == UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
                    bool ownershipRestored = JToken.DeepEquals(scenesBefore, Ground30SceneOwnership());
                    restored = worldsRestored && defaultRestored && countsRestored && ownershipRestored && pinsRestored && cleanupFailure == null;
                    result["runtimeInputsUnchanged"] = pinsRestored;
                    result["worldCensusRestored"] = worldsRestored;
                    result["defaultWorldIdentityRestored"] = defaultRestored;
                    result["sceneAndPreviewCountsRestored"] = countsRestored;
                    result["sceneOwnershipRestored"] = ownershipRestored;
                }
                catch (Exception censusError) { cleanupFailure = cleanupFailure == null ? censusError : new AggregateException(cleanupFailure, censusError); }
                result["cleanupVerified"] = restored;
                if (cleanupFailure != null) result["cleanupFailure"] = cleanupFailure.ToString();
                if (!restored)
                {
                    result["status"] = "Failed_DisposableRuntimeCleanupNotRestored";
                    complete = false;
                }
                if (!complete && failure == null) result["incompleteReason"] = "Runtime attempt did not reach a complete audited pass";
            }
            return result;
        }

        static World S049PlacementRuntimeWorld()
        {
            var streaming = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
            var systems = streaming.Concat(new[] { typeof(RuntimeGridDeduplicationSystem),
                typeof(DynamicBlockerInitSystem), typeof(StaticGridBlockerUpdateSystem),
                typeof(DynamicOccupancyRebuildSystem) }).Distinct().ToArray();
            var world = new World("S049_DisposablePlacementRuntime", WorldFlags.Game);
            try
            {
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                return world;
            }
            catch
            {
                if (world.IsCreated) world.Dispose();
                throw;
            }
        }
    }
}
#endif
