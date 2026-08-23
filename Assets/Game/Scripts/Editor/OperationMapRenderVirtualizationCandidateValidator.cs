#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Game.Configs;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Rendering;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Hash128 = Unity.Entities.Hash128;

    /// <summary>
    /// Two-pass direct bake of the persisted candidate-only virtualization package.
    /// Production remains at its accepted resident/static mode.
    /// </summary>
    internal static class OperationMapRenderVirtualizationCandidateValidator
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-30_dense_city_render_virtualization_pilot_enabled.json";
        internal const string TwoRunBakeAllReportPath =
            "Design/AgentReports/2026-08-08_dense_city_render_virtualization_two_run_bake_all.json";

        private static readonly string[] BakeAllLogicalOutputPaths =
        {
            OperationMapRenderEligibilityInventoryProbe.ReportPath,
            OperationMapRenderEligibilityInventoryProbe.SourceRowsPath,
            OperationMapRenderEligibilityInventoryProbe.PrototypeRecipesPath,
            OperationMapRenderEligibilityInventoryProbe.LogicalPlacementsPath,
            OperationMapRenderEligibilityInventoryProbe.SpatialCellsPath,
            OperationMapRenderEligibilityInventoryProbe.CapacityBudgetPath,
            OperationMapRenderDatabaseBuilder.ConfigPath,
            OperationMapRenderDatabaseBuilder.ConfigPath + ".meta",
            OperationMapRenderDatabaseBuilder.ReportPath
        };

        private const int ExpectedEligibleRows = 61813;
        private const int ExpectedEligibleRenderers = 61783;
        private const int ExpectedSlots = 7765;
        private const int ExpectedPrototypes = 9107;
        private const int ExpectedParts = 12181;
        private const int ExpectedPlacements = 40460;
        private const int ExpectedRenderOnlyPlacements = 31400;
        private const int ExpectedGeneratedBuildingIdentities = 4530;
        private const int ExpectedRetainedGeneratedBuildingIdentities = 4530;
        private const int ExpectedRetainedGeneratedRenderOnlyIdentities = 5758;
        private const int ExpectedCells = 1934;
        private const int ExpectedPoolBuckets = 4;
        private const int PackedMaterialMeshInfoEntityLimit = 24000;
        private const int FixedProxySlotLimit = 8000;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void RunTwoPassValidation()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var protectedSnapshot =
                OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot.Capture(
                    projectRoot,
                    new[]
                    {
                        OperationMapEntityPresentationCandidateSceneBuilder
                            .AcceptedOperationMapScenePath,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                        OperationMapAddressablesLayoutBuilder.DefinitionPath,
                        OperationMapAddressablesLayoutBuilder.SourceScenePath,
                        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                            .DenseCandidateDefinitionPath,
                        OperationMapRenderDatabaseBuilder.ConfigPath,
                        "Assets/AddressableAssetsData/AddressableAssetSettings.asset",
                        DenseCityPresentationBudgetValidator.DensePackedAssetSharingReportPath
                    },
                    new[]
                    {
                        OperationMapEntityPresentationCandidateSceneBuilder.StaticRollbackRoot,
                        "Assets/AddressableAssetsData/AssetGroups"
                    });

            RequirePersistedModes();
            DirectBakeSummary first = BakeAndValidate("first");
            protectedSnapshot.RequireUnchanged();
            RequirePersistedModes();
            DirectBakeSummary second = BakeAndValidate("second");
            protectedSnapshot.RequireUnchanged();
            RequirePersistedModes();

            if (!string.Equals(first.Fingerprint, second.Fingerprint, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Two direct virtualized candidate bakes produced different packed summaries: " +
                    $"{first.Fingerprint} != {second.Fingerprint}.");
            }

            var report = new DirectBakeReport
            {
                schema = "warline.operation-map.render-virtualization-direct-bake",
                schemaVersion = 4,
                result = "Passed",
                operationMapId =
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                renderResidencyMode =
                    OperationMapRenderResidencyMode.VirtualizedProxyPool.ToString(),
                persistedCandidateRenderResidencyMode =
                    OperationMapRenderResidencyMode.VirtualizedProxyPool.ToString(),
                productionPresentationKind =
                    OperationMapPresentationKind.StaticSceneChunks.ToString(),
                productionRenderResidencyMode =
                    OperationMapRenderResidencyMode.ResidentEntities.ToString(),
                productionCutover = 0,
                passCount = 2,
                firstFingerprint = first.Fingerprint,
                secondFingerprint = second.Fingerprint,
                contentHash = second.ContentHash,
                databaseSchemaVersion = second.DatabaseSchemaVersion,
                prototypeCount = second.PrototypeCount,
                partCount = second.PartCount,
                placementCount = second.PlacementCount,
                cellCount = second.CellCount,
                poolBucketCount = second.PoolBucketCount,
                proxySlotCount = second.ProxySlotCount,
                packedMaterialMeshInfoEntityCount =
                    second.PackedMaterialMeshInfoEntityCount,
                packedMaterialMeshInfoEntityLimit =
                    PackedMaterialMeshInfoEntityLimit,
                packedMaterialMeshInfoEntitiesWithinLimit =
                    second.PackedMaterialMeshInfoEntityCount <=
                    PackedMaterialMeshInfoEntityLimit,
                fixedProxySlotLimit = FixedProxySlotLimit,
                fixedProxySlotsWithinLimit =
                    second.ProxySlotCount <= FixedProxySlotLimit,
                sourceRowCount = second.SourceRowCount,
                virtualizedSourceRowCount = second.EligibleSourceRowCount,
                virtualizedSourceRendererCount = second.EligibleSourceRendererCount,
                packedEligibleSourceRowCount = 0,
                packedResidentSourceRowCount = second.ResidentSourceRowCount,
                packedSourceRowsRemoved = second.EligibleSourceRendererCount,
                virtualizedGeneratedRenderOnlyIdentityCount =
                    second.VirtualizedGeneratedRenderOnlyIdentityCount,
                virtualizedGeneratedBuildingIdentityCount =
                    second.VirtualizedGeneratedBuildingIdentityCount,
                retainedVirtualizedGeneratedRenderOnlyIdentityCount =
                    second.RetainedVirtualizedGeneratedRenderOnlyIdentityCount,
                retainedVirtualizedGeneratedBuildingIdentityCount =
                    second.RetainedVirtualizedGeneratedBuildingIdentityCount
            };
            string physicalReportPath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(physicalReportPath) ?? projectRoot);
            File.WriteAllText(
                physicalReportPath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);

            Debug.Log(
                "[OperationMapRenderVirtualizationCandidateValidation] result=Passed " +
                $"passes=2 fingerprint={second.Fingerprint} " +
                $"sourceRows={second.SourceRowCount} " +
                $"virtualizedRows={second.EligibleSourceRowCount} " +
                $"virtualizedRenderers={second.EligibleSourceRendererCount} " +
                $"residentRows={second.ResidentSourceRowCount} " +
                $"packedMmi={second.PackedMaterialMeshInfoEntityCount}/" +
                $"{PackedMaterialMeshInfoEntityLimit} " +
                $"slots={second.ProxySlotCount} " +
                $"slotLimit={FixedProxySlotLimit} " +
                $"buildingIdentities={second.VirtualizedGeneratedBuildingIdentityCount} " +
                $"retainedGeneratedBuildings=" +
                $"{second.RetainedVirtualizedGeneratedBuildingIdentityCount} " +
                $"retainedGeneratedRenderOnly=" +
                $"{second.RetainedVirtualizedGeneratedRenderOnlyIdentityCount} " +
                "productionCutover=0");
        }

        public static void RunTwoFullBakeAllDeterminismValidation()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var protectedSnapshot =
                OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot.Capture(
                    projectRoot,
                    new[]
                    {
                        OperationMapEntityPresentationCandidateSceneBuilder
                            .AcceptedOperationMapScenePath,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                        OperationMapAddressablesLayoutBuilder.DefinitionPath,
                        OperationMapAddressablesLayoutBuilder.SourceScenePath,
                        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                            .DenseCandidateDefinitionPath,
                        "Assets/AddressableAssetsData/AddressableAssetSettings.asset",
                        DenseCityPresentationBudgetValidator.DensePackedAssetSharingReportPath
                    },
                    new[]
                    {
                        OperationMapEntityPresentationCandidateSceneBuilder.StaticRollbackRoot,
                        "Assets/AddressableAssetsData/AssetGroups"
                    });

            BakeAllPassSummary first = RunFullBakeAllPass(projectRoot, "first");
            protectedSnapshot.RequireUnchanged();
            BakeAllPassSummary second = RunFullBakeAllPass(projectRoot, "second");
            protectedSnapshot.RequireUnchanged();

            RequireSameDirectBakeSummary(first.DirectBake, second.DirectBake);
            if (!string.Equals(first.LogicalBytesSha256, second.LogicalBytesSha256,
                    StringComparison.Ordinal) ||
                first.LogicalByteCount != second.LogicalByteCount ||
                !string.Equals(first.DatabaseContentHash, second.DatabaseContentHash,
                    StringComparison.Ordinal) ||
                !string.Equals(first.DatabaseOrderingHash, second.DatabaseOrderingHash,
                    StringComparison.Ordinal) ||
                first.MeshCount != second.MeshCount ||
                first.MaterialCount != second.MaterialCount ||
                first.PrototypeCount != second.PrototypeCount ||
                first.PartCount != second.PartCount ||
                first.PlacementCount != second.PlacementCount ||
                first.CellCount != second.CellCount ||
                first.CellPlacementIndexCount != second.CellPlacementIndexCount ||
                first.PolicyBucketCount != second.PolicyBucketCount ||
                first.TotalPoolSlotCapacity != second.TotalPoolSlotCapacity)
            {
                throw new InvalidOperationException(
                    "Two complete virtualization Bake All runs produced different logical " +
                    "hashes, counts, ordering, or serialized bytes.");
            }

            var report = new TwoRunBakeAllReport
            {
                schema = "warline.operation-map.render-virtualization-two-run-bake-all",
                schemaVersion = 1,
                result = "Passed",
                operationMapId =
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                passCount = 2,
                logicalOutputFileCount = BakeAllLogicalOutputPaths.Length,
                logicalByteCount = second.LogicalByteCount,
                firstLogicalBytesSha256 = first.LogicalBytesSha256,
                secondLogicalBytesSha256 = second.LogicalBytesSha256,
                contentHash = second.DatabaseContentHash,
                orderingHash = second.DatabaseOrderingHash,
                packedFingerprint = second.DirectBake.Fingerprint,
                meshCount = second.MeshCount,
                materialCount = second.MaterialCount,
                prototypeCount = second.PrototypeCount,
                partCount = second.PartCount,
                placementCount = second.PlacementCount,
                cellCount = second.CellCount,
                cellPlacementIndexCount = second.CellPlacementIndexCount,
                policyBucketCount = second.PolicyBucketCount,
                proxySlotCount = second.TotalPoolSlotCapacity,
                productionCutover = 0
            };
            string physicalReportPath = Path.Combine(projectRoot, TwoRunBakeAllReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(physicalReportPath) ?? projectRoot);
            File.WriteAllText(
                physicalReportPath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);

            Debug.Log(
                "[OperationMapRenderVirtualizationTwoRunBakeAll] result=Passed " +
                $"passes=2 logicalFiles={BakeAllLogicalOutputPaths.Length} " +
                $"logicalBytes={second.LogicalByteCount} " +
                $"logicalHash={second.LogicalBytesSha256} " +
                $"contentHash={second.DatabaseContentHash} " +
                $"orderingHash={second.DatabaseOrderingHash} " +
                $"packedFingerprint={second.DirectBake.Fingerprint} " +
                $"placements={second.PlacementCount} slots={second.TotalPoolSlotCapacity} " +
                "productionCutover=0");
        }

        private static BakeAllPassSummary RunFullBakeAllPass(
            string projectRoot,
            string pass)
        {
            RequirePersistedModes();
            OperationMapRenderEligibilityInventoryProbe.Run();
            OperationMapRenderDatabaseBuilder.Run();
            DirectBakeSummary directBake = BakeAndValidate(pass);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            DatabaseLogicalReport database = JsonUtility.FromJson<DatabaseLogicalReport>(
                File.ReadAllText(
                    Path.Combine(projectRoot, OperationMapRenderDatabaseBuilder.ReportPath),
                    Utf8WithoutBom));
            if (database == null || database.result != "Passed" ||
                string.IsNullOrWhiteSpace(database.contentHash) ||
                string.IsNullOrWhiteSpace(database.recordOrderingSha256))
            {
                throw new InvalidOperationException(
                    $"Virtualization Bake All {pass} database report is incomplete.");
            }

            string logicalBytesSha256 = ComputeLogicalOutputFingerprint(
                projectRoot,
                out long logicalByteCount);
            return new BakeAllPassSummary(
                logicalBytesSha256,
                logicalByteCount,
                database,
                directBake);
        }

        private static string ComputeLogicalOutputFingerprint(
            string projectRoot,
            out long totalByteCount)
        {
            var manifest = new StringBuilder();
            totalByteCount = 0;
            foreach (string relativePath in BakeAllLogicalOutputPaths.OrderBy(
                         value => value,
                         StringComparer.Ordinal))
            {
                string physicalPath = Path.Combine(projectRoot, relativePath);
                if (!File.Exists(physicalPath))
                {
                    throw new InvalidOperationException(
                        $"Virtualization Bake All output is missing: {relativePath}");
                }

                byte[] bytes = File.ReadAllBytes(physicalPath);
                totalByteCount += bytes.LongLength;
                manifest.Append(relativePath)
                    .Append('|')
                    .Append(bytes.LongLength)
                    .Append('|')
                    .Append(ComputeSha256(bytes))
                    .Append('\n');
            }

            return ComputeSha256(manifest.ToString());
        }

        private static void RequireSameDirectBakeSummary(
            DirectBakeSummary first,
            DirectBakeSummary second)
        {
            if (first.Fingerprint != second.Fingerprint ||
                first.ContentHash != second.ContentHash ||
                first.DatabaseSchemaVersion != second.DatabaseSchemaVersion ||
                first.PrototypeCount != second.PrototypeCount ||
                first.PartCount != second.PartCount ||
                first.PlacementCount != second.PlacementCount ||
                first.CellCount != second.CellCount ||
                first.PoolBucketCount != second.PoolBucketCount ||
                first.ProxySlotCount != second.ProxySlotCount ||
                first.SourceRowCount != second.SourceRowCount ||
                first.EligibleSourceRowCount != second.EligibleSourceRowCount ||
                first.EligibleSourceRendererCount != second.EligibleSourceRendererCount ||
                first.ResidentSourceRowCount != second.ResidentSourceRowCount ||
                first.PackedMaterialMeshInfoEntityCount !=
                    second.PackedMaterialMeshInfoEntityCount ||
                first.VirtualizedGeneratedRenderOnlyIdentityCount !=
                    second.VirtualizedGeneratedRenderOnlyIdentityCount ||
                first.VirtualizedGeneratedBuildingIdentityCount !=
                    second.VirtualizedGeneratedBuildingIdentityCount ||
                first.RetainedVirtualizedGeneratedRenderOnlyIdentityCount !=
                    second.RetainedVirtualizedGeneratedRenderOnlyIdentityCount ||
                first.RetainedVirtualizedGeneratedBuildingIdentityCount !=
                    second.RetainedVirtualizedGeneratedBuildingIdentityCount)
            {
                throw new InvalidOperationException(
                    "Two complete virtualization Bake All runs produced different packed " +
                    "logical counts or fingerprints.");
            }
        }

        private static DirectBakeSummary BakeAndValidate(string pass)
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene candidateScene = default;
            World world = null;
            object blobAssetStore = null;
            try
            {
                candidateScene = EditorSceneManager.OpenScene(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    OpenSceneMode.Single);
                int expectedSourceRowCount =
                    CountAuthoringSourceRows(candidateScene);
                OperationMapRenderDatabaseBakeConfig config =
                    AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(
                        OperationMapRenderDatabaseBuilder.ConfigPath);
                string configError = null;
                if (config == null || !config.TryValidateSchema(out configError))
                {
                    throw new InvalidOperationException(
                        $"Direct virtualized bake database is invalid: {configError}");
                }

                OperationMapVirtualizedPresentationAuthoring[] authorings =
                    candidateScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<
                            OperationMapVirtualizedPresentationAuthoring>(true))
                        .ToArray();
                string authoringError = null;
                if (authorings.Length != 1 ||
                    authorings[0].DatabaseConfig != config ||
                    authorings[0].SourcePresentationRoot != authorings[0].gameObject ||
                    !authorings[0].TryValidate(out authoringError))
                {
                    throw new InvalidOperationException(
                        "Persisted candidate requires exactly one self-owned valid " +
                        $"virtualization authoring root: {authoringError}");
                }

                world = new World($"VRP038DirectBake-{pass}");
                blobAssetStore = CreateBlobAssetStore();
                BakeScene(world, candidateScene, blobAssetStore);
                return ValidateWorld(
                    world.EntityManager,
                    expectedSourceRowCount);
            }
            finally
            {
                if (world != null)
                    world.Dispose();
                DisposeBlobAssetStore(blobAssetStore);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        private static DirectBakeSummary ValidateWorld(
            EntityManager entityManager,
            int expectedSourceRowCount)
        {
            using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadOnly<OperationMapRenderPackedReadinessComponent>(),
                ComponentType.ReadOnly<OperationMapRenderResidentSourceRowComponent>());
            if (databaseQuery.CalculateEntityCount() != 1)
                throw new InvalidOperationException("Direct bake requires exactly one render database.");

            Entity databaseEntity = databaseQuery.GetSingletonEntity();
            OperationMapRenderDatabaseComponent database =
                entityManager.GetComponentData<OperationMapRenderDatabaseComponent>(
                    databaseEntity);
            if (!database.Blob.IsCreated)
                throw new InvalidOperationException("Direct bake render database blob is not created.");
            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            OperationMapRenderPackedReadinessComponent readiness =
                entityManager.GetComponentData<OperationMapRenderPackedReadinessComponent>(
                    databaseEntity);
            DynamicBuffer<OperationMapRenderResidentSourceRowComponent> residentRows =
                entityManager.GetBuffer<OperationMapRenderResidentSourceRowComponent>(
                    databaseEntity,
                    true);

            int sourceRowCount = CountSourceRows(entityManager);
            int eligibleTaggedCount = CountEligibleTaggedRows(entityManager);
            int packedMaterialMeshInfoEntityCount =
                CountPackedMaterialMeshInfoEntities(entityManager);
            ValidateResidentRows(entityManager, residentRows);
            ValidateSlots(entityManager, ref blob);

            string operationMapId = blob.OperationMapId.ToString();
            string contentHash = blob.ContentHash.ToString();
            if (!string.Equals(
                    operationMapId,
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    contentHash,
                    database.ContentHash.ToString(),
                    StringComparison.Ordinal) ||
                blob.SchemaVersion != database.SchemaVersion)
            {
                throw new InvalidOperationException(
                    "Direct bake database component/blob ownership is inconsistent.");
            }

            RequireCount(
                "source rows",
                sourceRowCount,
                expectedSourceRowCount);
            RequireCount(
                "eligible readiness rows",
                readiness.EligibleSourceRowCount,
                ExpectedEligibleRows);
            RequireCount(
                "eligible readiness renderers",
                readiness.EligibleSourceRendererCount,
                ExpectedEligibleRenderers);
            RequireCount("eligible tagged rows", eligibleTaggedCount, ExpectedEligibleRows);
            RequireCount(
                "resident readiness rows",
                readiness.ResidentSourceRowCount,
                expectedSourceRowCount - ExpectedEligibleRenderers);
            RequireCount(
                "resident buffer rows",
                residentRows.Length,
                expectedSourceRowCount - ExpectedEligibleRenderers);
            RequireCount("proxy readiness slots", readiness.ProxySlotCount, ExpectedSlots);
            if (packedMaterialMeshInfoEntityCount <= 0)
                throw new InvalidOperationException(
                    "Packed candidate contains no MaterialMeshInfo entities.");
            if (packedMaterialMeshInfoEntityCount > PackedMaterialMeshInfoEntityLimit)
            {
                throw new InvalidOperationException(
                    $"Packed MaterialMeshInfo entity count {packedMaterialMeshInfoEntityCount} " +
                    $"exceeds limit {PackedMaterialMeshInfoEntityLimit}.");
            }
            if (readiness.ProxySlotCount > FixedProxySlotLimit)
            {
                throw new InvalidOperationException(
                    $"Fixed proxy slot count {readiness.ProxySlotCount} exceeds limit " +
                    $"{FixedProxySlotLimit}.");
            }
            RequireCount("prototypes", blob.Prototypes.Length, ExpectedPrototypes);
            RequireCount("parts", blob.Parts.Length, ExpectedParts);
            RequireCount("placements", blob.Placements.Length, ExpectedPlacements);
            RequireCount("cells", blob.Cells.Length, ExpectedCells);
            RequireCount("pool buckets", blob.PoolBuckets.Length, ExpectedPoolBuckets);
            RequireCount(
                "virtualized generated render-only identities",
                readiness.VirtualizedGeneratedRenderOnlyIdentityCount,
                ExpectedRenderOnlyPlacements);
            RequireCount(
                "virtualized accepted building identities",
                readiness.VirtualizedAcceptedBuildingIdentityCount,
                0);
            RequireCount(
                "virtualized accepted render-only identities",
                readiness.VirtualizedAcceptedRenderOnlyIdentityCount,
                0);
            RequireCount(
                "virtualized generated building identities",
                readiness.VirtualizedGeneratedBuildingIdentityCount,
                ExpectedGeneratedBuildingIdentities);
            ValidateRetainedIdentityOverlap(readiness);
            RequireCount(
                "retained virtualized generated building identities",
                readiness.RetainedVirtualizedGeneratedBuildingIdentityCount,
                ExpectedRetainedGeneratedBuildingIdentities);
            RequireCount(
                "retained virtualized generated render-only identities",
                readiness.RetainedVirtualizedGeneratedRenderOnlyIdentityCount,
                ExpectedRetainedGeneratedRenderOnlyIdentities);
            if (readiness.ResidencyMode !=
                (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                throw new InvalidOperationException(
                    $"Direct bake residency mode is {readiness.ResidencyMode}.");
            }
            if (sourceRowCount !=
                readiness.EligibleSourceRendererCount +
                    readiness.ResidentSourceRowCount)
            {
                throw new InvalidOperationException(
                    "Direct bake source rows do not reconcile to virtualized plus resident rows.");
            }

            string canonical = string.Join(
                "|",
                operationMapId,
                contentHash,
                blob.SchemaVersion,
                blob.Prototypes.Length,
                blob.Parts.Length,
                blob.Placements.Length,
                blob.Cells.Length,
                blob.PoolBuckets.Length,
                sourceRowCount,
                readiness.EligibleSourceRowCount,
                readiness.EligibleSourceRendererCount,
                readiness.ResidentSourceRowCount,
                readiness.ProxySlotCount,
                packedMaterialMeshInfoEntityCount,
                readiness.VirtualizedGeneratedRenderOnlyIdentityCount,
                readiness.VirtualizedGeneratedBuildingIdentityCount,
                readiness.RetainedVirtualizedGeneratedRenderOnlyIdentityCount,
                readiness.RetainedVirtualizedGeneratedBuildingIdentityCount);
            return new DirectBakeSummary(
                ComputeSha256(canonical),
                contentHash,
                blob.SchemaVersion,
                blob.Prototypes.Length,
                blob.Parts.Length,
                blob.Placements.Length,
                blob.Cells.Length,
                blob.PoolBuckets.Length,
                readiness.ProxySlotCount,
                sourceRowCount,
                readiness.EligibleSourceRowCount,
                readiness.EligibleSourceRendererCount,
                readiness.ResidentSourceRowCount,
                packedMaterialMeshInfoEntityCount,
                readiness.VirtualizedGeneratedRenderOnlyIdentityCount,
                readiness.VirtualizedGeneratedBuildingIdentityCount,
                readiness.RetainedVirtualizedGeneratedRenderOnlyIdentityCount,
                readiness.RetainedVirtualizedGeneratedBuildingIdentityCount);
        }

        private static void ValidateRetainedIdentityOverlap(
            OperationMapRenderPackedReadinessComponent readiness)
        {
            if (readiness.RetainedVirtualizedAcceptedBuildingIdentityCount < 0 ||
                readiness.RetainedVirtualizedAcceptedRenderOnlyIdentityCount < 0 ||
                readiness.RetainedVirtualizedGeneratedBuildingIdentityCount < 0 ||
                readiness.RetainedVirtualizedGeneratedRenderOnlyIdentityCount < 0 ||
                readiness.RetainedVirtualizedAcceptedBuildingIdentityCount >
                    readiness.VirtualizedAcceptedBuildingIdentityCount ||
                readiness.RetainedVirtualizedAcceptedRenderOnlyIdentityCount >
                    readiness.VirtualizedAcceptedRenderOnlyIdentityCount ||
                readiness.RetainedVirtualizedGeneratedBuildingIdentityCount >
                    readiness.VirtualizedGeneratedBuildingIdentityCount ||
                readiness.RetainedVirtualizedGeneratedRenderOnlyIdentityCount >
                    readiness.VirtualizedGeneratedRenderOnlyIdentityCount)
            {
                throw new InvalidOperationException(
                    "Direct bake retained-identity overlap is outside its virtualized class.");
            }
        }

        private static int CountSourceRows(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderSourceRowBakingComponent>());
            using NativeArray<Entity> owners = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            for (int index = 0; index < owners.Length; index++)
            {
                count = checked(
                    count +
                    entityManager.GetBuffer<OperationMapRenderSourceRowBakingComponent>(
                        owners[index],
                        true).Length);
            }
            return count;
        }

        private static int CountPackedMaterialMeshInfoEntities(
            EntityManager entityManager)
        {
            Type bakingOnlyType =
                Type.GetType("Unity.Entities.BakingOnlyEntity, Unity.Entities.Hybrid", true);
            TypeIndex bakingOnlyTypeIndex = TypeManager.GetTypeIndex(bakingOnlyType);
            using EntityQuery query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<MaterialMeshInfo>() },
                    None = new[] { ComponentType.FromTypeIndex(bakingOnlyTypeIndex) }
                });
            return query.CalculateEntityCount();
        }

        private static int CountAuthoringSourceRows(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                DenseCityPresentationIdentityAuthoring[] generatedOwners =
                    roots[rootIndex].GetComponentsInChildren<
                        DenseCityPresentationIdentityAuthoring>(true);
                for (int ownerIndex = 0;
                     ownerIndex < generatedOwners.Length;
                     ownerIndex++)
                {
                    count = checked(
                        count + CountOwnerRenderers(generatedOwners[ownerIndex]));
                }

                OperationMapEntityPresentationIdentityAuthoring[] acceptedOwners =
                    roots[rootIndex].GetComponentsInChildren<
                        OperationMapEntityPresentationIdentityAuthoring>(true);
                for (int ownerIndex = 0;
                     ownerIndex < acceptedOwners.Length;
                     ownerIndex++)
                {
                    count = checked(
                        count + CountOwnerRenderers(acceptedOwners[ownerIndex]));
                }
            }
            if (count <= ExpectedEligibleRows)
            {
                throw new InvalidOperationException(
                    $"Authoring source-row count is not credible: {count}.");
            }
            return count;
        }

        private static int CountOwnerRenderers(Component owner)
        {
            int count = 0;
            Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                if (!HasNestedStableOwner(
                        renderers[rendererIndex].transform,
                        owner.transform))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool HasNestedStableOwner(
            Transform renderer,
            Transform expectedOwner)
        {
            for (Transform current = renderer;
                 current != null && current != expectedOwner;
                 current = current.parent)
            {
                if (current.GetComponent<DenseCityPresentationIdentityAuthoring>() !=
                        null ||
                    current.GetComponent<
                        OperationMapEntityPresentationIdentityAuthoring>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static int CountEligibleTaggedRows(EntityManager entityManager)
        {
            Type bakingOnlyType =
                Type.GetType("Unity.Entities.BakingOnlyEntity, Unity.Entities.Hybrid", true);
            TypeIndex bakingOnlyTypeIndex = TypeManager.GetTypeIndex(bakingOnlyType);
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderEligibleSourceComponent>(),
                ComponentType.FromTypeIndex(bakingOnlyTypeIndex));
            return query.CalculateEntityCount();
        }

        private static void ValidateResidentRows(
            EntityManager entityManager,
            DynamicBuffer<OperationMapRenderResidentSourceRowComponent> residentRows)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < residentRows.Length; index++)
            {
                OperationMapRenderResidentSourceRowComponent row = residentRows[index];
                if (!entityManager.Exists(row.RenderEntity) ||
                    entityManager.HasComponent<OperationMapRenderEligibleSourceComponent>(
                        row.RenderEntity))
                {
                    throw new InvalidOperationException(
                        $"Resident source row {index} has missing or eligible render ownership.");
                }

                string identity =
                    $"{row.OwnerIdentity.Low:x16}{row.OwnerIdentity.High:x16}|" +
                    $"{row.RendererPathIdentity.Low:x16}{row.RendererPathIdentity.High:x16}";
                if (!identities.Add(identity))
                {
                    throw new InvalidOperationException(
                        $"Resident source row {index} duplicates owner/path identity.");
                }
            }
        }

        private static void ValidateSlots(
            EntityManager entityManager,
            ref OperationMapRenderDatabaseBlob blob)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            RequireCount("proxy slots", entities.Length, ExpectedSlots);
            var observed = new bool[ExpectedSlots];
            for (int index = 0; index < entities.Length; index++)
            {
                Entity entity = entities[index];
                OperationMapRenderProxySlotComponent slot =
                    entityManager.GetComponentData<OperationMapRenderProxySlotComponent>(
                        entity);
                if ((uint)slot.SlotIndex >= (uint)observed.Length ||
                    observed[slot.SlotIndex] ||
                    (uint)slot.PoolBucketIndex >= (uint)blob.PoolBuckets.Length)
                {
                    throw new InvalidOperationException(
                        $"Proxy slot {index} has invalid or duplicate ownership.");
                }
                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[slot.PoolBucketIndex];
                if (slot.SlotIndex < bucket.FirstSlot ||
                    slot.SlotIndex >= bucket.FirstSlot + bucket.Capacity ||
                    slot.PlacementIndex != -1 ||
                    slot.PartIndex != -1 ||
                    slot.AssignmentGeneration != 0 ||
                    !entityManager.HasComponent<MaterialMeshInfo>(entity) ||
                    entityManager.IsComponentEnabled<MaterialMeshInfo>(entity))
                {
                    throw new InvalidOperationException(
                        $"Proxy slot {slot.SlotIndex} violates its initial packed contract.");
                }
                observed[slot.SlotIndex] = true;
            }
            if (observed.Any(value => !value))
                throw new InvalidOperationException("Packed proxy slot indices are not contiguous.");
        }

        private static void RequirePersistedModes()
        {
            OperationMapDefinition production =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapAddressablesLayoutBuilder.DefinitionPath);
            OperationMapDefinition candidate =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateDefinitionPath);
            if (production == null ||
                production.PresentationKind != OperationMapPresentationKind.StaticSceneChunks ||
                production.RenderResidencyMode !=
                OperationMapRenderResidencyMode.ResidentEntities)
            {
                throw new InvalidOperationException(
                    "Production cutover is not disabled at the protected static baseline.");
            }
            if (candidate == null ||
                candidate.PresentationKind != OperationMapPresentationKind.EntityScene ||
                candidate.RenderResidencyMode !=
                OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                throw new InvalidOperationException(
                    "VRP-051 requires the persisted candidate definition to use its proxy pool.");
            }
        }

        private static object CreateBlobAssetStore()
        {
            Type type =
                Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities") ??
                Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities.Hybrid");
            if (type == null)
                throw new InvalidOperationException("BlobAssetStore type is unavailable.");
            return Activator.CreateInstance(type, 128);
        }

        private static void DisposeBlobAssetStore(object store)
        {
            if (store is IDisposable disposable)
                disposable.Dispose();
        }

        private static void BakeScene(
            World world,
            Scene scene,
            object blobAssetStore)
        {
            Type bakingUtilityType =
                Type.GetType("Unity.Entities.BakingUtility, Unity.Entities.Hybrid", true);
            Type bakingSettingsType =
                Type.GetType("Unity.Entities.BakingSettings, Unity.Entities.Hybrid", true);
            object settings = Activator.CreateInstance(bakingSettingsType);
            string guid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            bakingSettingsType.GetField("SceneGUID")?.SetValue(
                settings,
                new Hash128(guid));
            object assignName = Enum.Parse(
                bakingUtilityType.GetNestedType("BakingFlags"),
                "AssignName");
            object addGuid = Enum.Parse(
                bakingUtilityType.GetNestedType("BakingFlags"),
                "AddEntityGUID");
            object flags = Enum.ToObject(
                bakingUtilityType.GetNestedType("BakingFlags"),
                Convert.ToUInt32(assignName) | Convert.ToUInt32(addGuid));
            bakingSettingsType.GetProperty("BakingFlags")?.SetValue(settings, flags);
            bakingSettingsType.GetProperty("BlobAssetStore")?.SetValue(
                settings,
                blobAssetStore);

            MethodInfo bakeScene = bakingUtilityType.GetMethod(
                "BakeScene",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (bakeScene == null)
                throw new MissingMethodException("Unity.Entities.BakingUtility.BakeScene");
            try
            {
                object result = bakeScene.Invoke(
                    null,
                    new object[] { world, scene, settings, false, null });
                if (result is bool ok && !ok)
                    throw new InvalidOperationException("BakeScene returned false.");
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] previousSetup)
        {
            if (OperationMapEntitySceneCandidateBakeAll.HasRestorableSceneSetup(previousSetup))
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static string ComputeSha256(string value)
        {
            using SHA256 sha256 = SHA256.Create();
            return string.Concat(
                sha256.ComputeHash(Utf8WithoutBom.GetBytes(value))
                    .Select(item => item.ToString("x2")));
        }

        private static string ComputeSha256(byte[] value)
        {
            using SHA256 sha256 = SHA256.Create();
            return string.Concat(
                sha256.ComputeHash(value)
                    .Select(item => item.ToString("x2")));
        }

        private static void RequireCount(string label, int actual, int expected)
        {
            if (actual != expected)
                throw new InvalidOperationException(
                    $"Direct bake expected {expected} {label}, found {actual}.");
        }

        private readonly struct DirectBakeSummary
        {
            internal DirectBakeSummary(
                string fingerprint,
                string contentHash,
                int databaseSchemaVersion,
                int prototypeCount,
                int partCount,
                int placementCount,
                int cellCount,
                int poolBucketCount,
                int proxySlotCount,
                int sourceRowCount,
                int eligibleSourceRowCount,
                int eligibleSourceRendererCount,
                int residentSourceRowCount,
                int packedMaterialMeshInfoEntityCount,
                int virtualizedGeneratedRenderOnlyIdentityCount,
                int virtualizedGeneratedBuildingIdentityCount,
                int retainedVirtualizedGeneratedRenderOnlyIdentityCount,
                int retainedVirtualizedGeneratedBuildingIdentityCount)
            {
                Fingerprint = fingerprint;
                ContentHash = contentHash;
                DatabaseSchemaVersion = databaseSchemaVersion;
                PrototypeCount = prototypeCount;
                PartCount = partCount;
                PlacementCount = placementCount;
                CellCount = cellCount;
                PoolBucketCount = poolBucketCount;
                ProxySlotCount = proxySlotCount;
                SourceRowCount = sourceRowCount;
                EligibleSourceRowCount = eligibleSourceRowCount;
                EligibleSourceRendererCount = eligibleSourceRendererCount;
                ResidentSourceRowCount = residentSourceRowCount;
                PackedMaterialMeshInfoEntityCount =
                    packedMaterialMeshInfoEntityCount;
                VirtualizedGeneratedRenderOnlyIdentityCount =
                    virtualizedGeneratedRenderOnlyIdentityCount;
                VirtualizedGeneratedBuildingIdentityCount =
                    virtualizedGeneratedBuildingIdentityCount;
                RetainedVirtualizedGeneratedRenderOnlyIdentityCount =
                    retainedVirtualizedGeneratedRenderOnlyIdentityCount;
                RetainedVirtualizedGeneratedBuildingIdentityCount =
                    retainedVirtualizedGeneratedBuildingIdentityCount;
            }

            internal string Fingerprint { get; }
            internal string ContentHash { get; }
            internal int DatabaseSchemaVersion { get; }
            internal int PrototypeCount { get; }
            internal int PartCount { get; }
            internal int PlacementCount { get; }
            internal int CellCount { get; }
            internal int PoolBucketCount { get; }
            internal int ProxySlotCount { get; }
            internal int SourceRowCount { get; }
            internal int EligibleSourceRowCount { get; }
            internal int EligibleSourceRendererCount { get; }
            internal int ResidentSourceRowCount { get; }
            internal int PackedMaterialMeshInfoEntityCount { get; }
            internal int VirtualizedGeneratedRenderOnlyIdentityCount { get; }
            internal int VirtualizedGeneratedBuildingIdentityCount { get; }
            internal int RetainedVirtualizedGeneratedRenderOnlyIdentityCount { get; }
            internal int RetainedVirtualizedGeneratedBuildingIdentityCount { get; }
        }

        private readonly struct BakeAllPassSummary
        {
            internal BakeAllPassSummary(
                string logicalBytesSha256,
                long logicalByteCount,
                DatabaseLogicalReport database,
                DirectBakeSummary directBake)
            {
                LogicalBytesSha256 = logicalBytesSha256;
                LogicalByteCount = logicalByteCount;
                DatabaseContentHash = database.contentHash;
                DatabaseOrderingHash = database.recordOrderingSha256;
                MeshCount = database.meshCount;
                MaterialCount = database.materialCount;
                PrototypeCount = database.prototypeCount;
                PartCount = database.partCount;
                PlacementCount = database.placementCount;
                CellCount = database.cellCount;
                CellPlacementIndexCount = database.cellPlacementIndexCount;
                PolicyBucketCount = database.policyBucketCount;
                TotalPoolSlotCapacity = database.totalPoolSlotCapacity;
                DirectBake = directBake;
            }

            internal string LogicalBytesSha256 { get; }
            internal long LogicalByteCount { get; }
            internal string DatabaseContentHash { get; }
            internal string DatabaseOrderingHash { get; }
            internal int MeshCount { get; }
            internal int MaterialCount { get; }
            internal int PrototypeCount { get; }
            internal int PartCount { get; }
            internal int PlacementCount { get; }
            internal int CellCount { get; }
            internal int CellPlacementIndexCount { get; }
            internal int PolicyBucketCount { get; }
            internal int TotalPoolSlotCapacity { get; }
            internal DirectBakeSummary DirectBake { get; }
        }

        [Serializable]
        private sealed class DatabaseLogicalReport
        {
            public string result;
            public string contentHash;
            public string recordOrderingSha256;
            public int meshCount;
            public int materialCount;
            public int prototypeCount;
            public int partCount;
            public int placementCount;
            public int cellCount;
            public int cellPlacementIndexCount;
            public int policyBucketCount;
            public int totalPoolSlotCapacity;
        }

        [Serializable]
        private sealed class TwoRunBakeAllReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public int passCount;
            public int logicalOutputFileCount;
            public long logicalByteCount;
            public string firstLogicalBytesSha256;
            public string secondLogicalBytesSha256;
            public string contentHash;
            public string orderingHash;
            public string packedFingerprint;
            public int meshCount;
            public int materialCount;
            public int prototypeCount;
            public int partCount;
            public int placementCount;
            public int cellCount;
            public int cellPlacementIndexCount;
            public int policyBucketCount;
            public int proxySlotCount;
            public int productionCutover;
        }

        [Serializable]
        private sealed class DirectBakeReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string renderResidencyMode;
            public string persistedCandidateRenderResidencyMode;
            public string productionPresentationKind;
            public string productionRenderResidencyMode;
            public int productionCutover;
            public int passCount;
            public string firstFingerprint;
            public string secondFingerprint;
            public string contentHash;
            public int databaseSchemaVersion;
            public int prototypeCount;
            public int partCount;
            public int placementCount;
            public int cellCount;
            public int poolBucketCount;
            public int proxySlotCount;
            public int packedMaterialMeshInfoEntityCount;
            public int packedMaterialMeshInfoEntityLimit;
            public bool packedMaterialMeshInfoEntitiesWithinLimit;
            public int fixedProxySlotLimit;
            public bool fixedProxySlotsWithinLimit;
            public int sourceRowCount;
            public int virtualizedSourceRowCount;
            public int virtualizedSourceRendererCount;
            public int packedEligibleSourceRowCount;
            public int packedResidentSourceRowCount;
            public int packedSourceRowsRemoved;
            public int virtualizedGeneratedRenderOnlyIdentityCount;
            public int virtualizedGeneratedBuildingIdentityCount;
            public int retainedVirtualizedGeneratedRenderOnlyIdentityCount;
            public int retainedVirtualizedGeneratedBuildingIdentityCount;
        }
    }
}

#endif
