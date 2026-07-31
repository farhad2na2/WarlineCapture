using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal static class OperationMapRenderDatabaseBuilder
    {
        internal const string ConfigPath =
            "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset";
        internal const string ReportPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_database.json";

        private const string PrototypePath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_prototype_recipes.json";
        private const string PlacementPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_logical_placements.json";
        private const string SpatialPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_spatial_cells.json";
        private const string CapacityPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_capacity_budget.json";

        [MenuItem("Tools/Warline/Render Virtualization/Build Candidate Database Config")]
        internal static void BuildMenu() => BuildCandidate();

        public static void Run() => BuildCandidate();

        public static void RunSourceParityValidation()
        {
            OperationMapRenderEligibilityInventoryProbe.Run();
            RunDeterminismValidation();
            DatabaseReport report = JsonUtility.FromJson<DatabaseReport>(
                File.ReadAllText(ReportPath));
            if (report == null ||
                report.logicalParityResult != "Passed" ||
                report.sourceRenderRowCount !=
                    OperationMapRenderEligibilityInventoryProbe.ExpectedPackedRenderRowCount ||
                report.eligibleSourceRowCount != report.logicalRenderRowCount ||
                report.sourceRowsRemoved != 0)
            {
                throw new InvalidOperationException(
                    "Persisted render database logical-parity report is incomplete.");
            }
            Debug.Log(
                "[OperationMapRenderDatabaseBuilder] sourceParity=Passed " +
                $"sourceRows={report.sourceRenderRowCount} eligibleRows={report.eligibleSourceRowCount} " +
                $"residentRows={report.residentSourceRowCount} removedRows={report.sourceRowsRemoved}");
        }

        public static void RunDeterminismValidation()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            BuildCandidate();
            byte[] firstConfig = File.ReadAllBytes(Path.Combine(projectRoot, ConfigPath));
            byte[] firstReport = File.ReadAllBytes(Path.Combine(projectRoot, ReportPath));
            DatabaseReport first = JsonUtility.FromJson<DatabaseReport>(
                new UTF8Encoding(false).GetString(firstReport));

            BuildCandidate();
            byte[] secondConfig = File.ReadAllBytes(Path.Combine(projectRoot, ConfigPath));
            byte[] secondReport = File.ReadAllBytes(Path.Combine(projectRoot, ReportPath));
            DatabaseReport second = JsonUtility.FromJson<DatabaseReport>(
                new UTF8Encoding(false).GetString(secondReport));

            if (!firstConfig.SequenceEqual(secondConfig) ||
                !firstReport.SequenceEqual(secondReport) ||
                first == null ||
                second == null ||
                first.contentHash != second.contentHash ||
                first.recordOrderingSha256 != second.recordOrderingSha256 ||
                first.meshCount != second.meshCount ||
                first.materialCount != second.materialCount ||
                first.prototypeCount != second.prototypeCount ||
                first.partCount != second.partCount ||
                first.placementCount != second.placementCount ||
                first.cellCount != second.cellCount ||
                first.cellPlacementIndexCount != second.cellPlacementIndexCount ||
                first.policyBucketCount != second.policyBucketCount ||
                first.totalPoolSlotCapacity != second.totalPoolSlotCapacity)
            {
                throw new InvalidOperationException(
                    "Unchanged render database inputs produced different records or bytes.");
            }

            Debug.Log(
                "[OperationMapRenderDatabaseBuilder] determinism=Passed " +
                $"contentHash={second.contentHash} orderingHash={second.recordOrderingSha256} " +
                $"configBytes={secondConfig.Length} reportBytes={secondReport.Length}");
        }

        internal static void BuildCandidate()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            string[] transactionPaths =
            {
                "Assets/Game/GeneratedOperationMapEntityPresentationCandidate.meta",
                "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation.meta",
                ConfigPath,
                ConfigPath + ".meta",
                ReportPath
            };
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction transaction =
                OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                    projectRoot,
                    transactionPaths);
            OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot protectedSnapshot =
                OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot.Capture(
                    projectRoot,
                    new[]
                    {
                        OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                        "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset",
                        "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity"
                    },
                    new[]
                    {
                        OperationMapDenseCityCandidateRuntimeContentBuilder.FrozenRollbackRootPath,
                        "Assets/AddressableAssetsData"
                    });

            try
            {
                BuildInputs inputs = LoadInputs(projectRoot);
                BuildRecords(inputs, out GeneratedRecords records);
                string contentHash = ComputeContentHash(inputs);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(Path.Combine(projectRoot, ConfigPath)) ??
                    throw new InvalidOperationException("Candidate config directory is unavailable."));

                OperationMapRenderDatabaseBakeConfig config =
                    AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(ConfigPath);
                if (config == null)
                {
                    if (File.Exists(Path.Combine(projectRoot, ConfigPath)))
                        AssetDatabase.DeleteAsset(ConfigPath);
                    config = ScriptableObject.CreateInstance<OperationMapRenderDatabaseBakeConfig>();
                    AssetDatabase.CreateAsset(config, ConfigPath);
                }

                config.InitializeGeneratedData(
                    inputs.prototypes.operationMapId,
                    contentHash,
                    inputs.spatial.cellSize,
                    Vector3From(inputs.spatial.gridOrigin),
                    new Vector2Int(
                        inputs.spatial.gridDimensions[0],
                        inputs.spatial.gridDimensions[1]),
                    records.meshes,
                    records.materials,
                    records.prototypes,
                    records.parts,
                    records.placements,
                    records.cells,
                    inputs.spatial.cellPlacementIndices,
                    records.poolBuckets);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(ConfigPath, ImportAssetOptions.ForceSynchronousImport);

                OperationMapRenderDatabaseBakeConfig persisted =
                    AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(ConfigPath);
                if (persisted == null)
                    throw new InvalidOperationException(
                        "Persisted render database config could not be loaded.");
                if (!persisted.TryValidateSchema(out string schemaError))
                    throw new InvalidOperationException(
                        "Persisted render database config is invalid: " + schemaError);
                if (!string.Equals(persisted.ContentHash, contentHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Persisted render database content hash changed.");
                RequirePersistedParity(persisted, records, inputs.spatial.cellPlacementIndices);

                string serializedHash = HashFile(Path.Combine(projectRoot, ConfigPath));
                DatabaseReport report = CreateReport(
                    inputs,
                    records,
                    contentHash,
                    ComputeRecordOrderingHash(records, inputs.spatial.cellPlacementIndices),
                    serializedHash);
                WriteAtomic(Path.Combine(projectRoot, ReportPath), JsonUtility.ToJson(report, true) + "\n");
                DatabaseReport persistedReport =
                    JsonUtility.FromJson<DatabaseReport>(
                        File.ReadAllText(Path.Combine(projectRoot, ReportPath)));
                if (persistedReport == null ||
                    persistedReport.result != "Passed" ||
                    persistedReport.contentHash != contentHash ||
                    persistedReport.configSerializedSha256 != serializedHash)
                {
                    throw new InvalidOperationException(
                        "Persisted render database report failed round-trip validation.");
                }

                protectedSnapshot.RequireUnchanged();
                Debug.Log(
                    $"[OperationMapRenderDatabaseBuilder] result=Passed config={ConfigPath} " +
                    $"contentHash={contentHash} placements={records.placements.Length} " +
                    $"slots={records.poolBuckets.Sum(value => value.Capacity)} isolation=Passed");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static BuildInputs LoadInputs(string projectRoot)
        {
            T Load<T>(string relativePath)
            {
                string text = File.ReadAllText(Path.Combine(projectRoot, relativePath));
                T value = JsonUtility.FromJson<T>(text);
                if (value == null)
                    throw new InvalidOperationException($"Could not parse {relativePath}.");
                return value;
            }

            var inputs = new BuildInputs
            {
                prototypes = Load<PrototypeReport>(PrototypePath),
                placements = Load<PlacementReport>(PlacementPath),
                spatial = Load<SpatialReport>(SpatialPath),
                capacity = Load<CapacityReport>(CapacityPath)
            };
            if (inputs.prototypes.result != "Passed" ||
                inputs.placements.result != "Passed" ||
                inputs.spatial.result != "Passed" ||
                inputs.capacity.result != "Passed" ||
                !string.Equals(
                    inputs.prototypes.operationMapId,
                    inputs.placements.operationMapId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    inputs.prototypes.operationMapId,
                    inputs.spatial.operationMapId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    inputs.prototypes.operationMapId,
                    inputs.capacity.operationMapId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Render database inputs must be passed reports for the same operation map.");
            }
            return inputs;
        }

        private static void BuildRecords(BuildInputs inputs, out GeneratedRecords records)
        {
            AssetIdentity[] meshIds = inputs.prototypes.parts
                .Select(value => new AssetIdentity(value.meshAssetGuid, value.meshLocalId))
                .Distinct()
                .OrderBy(value => value.guid, StringComparer.Ordinal)
                .ThenBy(value => value.localId)
                .ToArray();
            AssetIdentity[] materialIds = inputs.prototypes.parts
                .Select(value => new AssetIdentity(value.materialAssetGuid, value.materialLocalId))
                .Distinct()
                .OrderBy(value => value.guid, StringComparer.Ordinal)
                .ThenBy(value => value.localId)
                .ToArray();
            var meshIndices = meshIds.Select((value, index) => (value, index))
                .ToDictionary(value => value.value, value => value.index);
            var materialIndices = materialIds.Select((value, index) => (value, index))
                .ToDictionary(value => value.value, value => value.index);

            OperationMapRenderPoolBucketConfigRecord[] buckets = inputs.capacity.capacityByPolicy
                .OrderBy(value => Enum.Parse<OperationMapRenderPolicyBucket>(value.policyBucket))
                .ThenBy(value => value.layer)
                .ThenBy(value => value.renderingLayerMask)
                .ThenBy(value => Enum.Parse<OperationMapRenderMotionVectorMode>(value.motionVectorMode))
                .ThenBy(value => value.shadowFlags)
                .Select((value, index) =>
                {
                    string source =
                        $"capacity-policy|{value.policyBucket}|{value.layer}|" +
                        $"{value.renderingLayerMask}|{value.motionVectorMode}|" +
                        $"{value.shadowFlags}|{value.sweepSampleCount}|" +
                        $"{value.peakRequiredPartRows}|{value.capacity}";
                    if (!OperationMapRenderIdentityProjection.TryProject(
                            source,
                            out OperationMapRenderIdentity128 identity,
                            out string error))
                        throw new InvalidOperationException(error);
                    int firstSlot = inputs.capacity.capacityByPolicy
                        .Where(other =>
                            ComparePolicy(other, value) < 0)
                        .Sum(other => other.capacity);
                    return new OperationMapRenderPoolBucketConfigRecord(
                        Enum.Parse<OperationMapRenderPolicyBucket>(value.policyBucket),
                        value.layer,
                        value.renderingLayerMask,
                        Enum.Parse<OperationMapRenderMotionVectorMode>(value.motionVectorMode),
                        (OperationMapRenderShadowFlags)value.shadowFlags,
                        firstSlot,
                        value.capacity,
                        value.peakRequiredPartRows,
                        value.headroomCount,
                        identity.Low,
                        identity.High);
                }).ToArray();

            var bucketIndices = buckets.Select((value, index) => (value, index))
                .ToDictionary(
                    value => PolicyKey(
                        value.value.PolicyBucket,
                        value.value.Layer,
                        value.value.RenderingLayerMask,
                        value.value.MotionVectorMode,
                        value.value.ShadowFlags),
                    value => value.index);

            records = new GeneratedRecords
            {
                meshes = meshIds.Select(value =>
                    new OperationMapRenderMeshConfigRecord(
                        value.guid,
                        value.localId,
                        LoadAsset<Mesh>(value))).ToArray(),
                materials = materialIds.Select(value =>
                    new OperationMapRenderMaterialConfigRecord(
                        value.guid,
                        value.localId,
                        LoadAsset<Material>(value))).ToArray(),
                prototypes = inputs.prototypes.prototypes.Select(value =>
                    new OperationMapRenderPrototypeConfigRecord(
                        value.prototypeIdentityLow,
                        value.prototypeIdentityHigh,
                        value.firstPart,
                        value.partCount,
                        BoundsFrom(
                            value.combinedLocalBoundsCenter,
                            value.combinedLocalBoundsExtents),
                         Enum.Parse<DenseCityPresentationSemanticCategory>(
                             value.semanticCategory),
                         Enum.Parse<DenseCityPresentationSemanticCategory>(
                             value.semanticCategory) ==
                         DenseCityPresentationSemanticCategory.GameplayBuildingIntact
                             ? OperationMapRenderEligibilityFlags.Eligible |
                               OperationMapRenderEligibilityFlags.RequiresStateOwner
                             : OperationMapRenderEligibilityFlags.Eligible)).ToArray(),
                parts = inputs.prototypes.parts.Select(value =>
                {
                    if (!OperationMapRenderIdentityProjection.TryProject(
                            "renderer-path|" + value.rendererPath,
                            out OperationMapRenderIdentity128 rendererIdentity,
                            out string error))
                        throw new InvalidOperationException(error);
                    OperationMapRenderPolicyBucket policy =
                        Enum.Parse<OperationMapRenderPolicyBucket>(value.policyBucket);
                    OperationMapRenderMotionVectorMode motion =
                        Enum.Parse<OperationMapRenderMotionVectorMode>(value.motionVectorMode);
                    OperationMapRenderShadowFlags shadows =
                        (OperationMapRenderShadowFlags)value.shadowFlags;
                    return new OperationMapRenderPrototypePartConfigRecord(
                        rendererIdentity.Low,
                        rendererIdentity.High,
                        meshIndices[new AssetIdentity(value.meshAssetGuid, value.meshLocalId)],
                        materialIndices[
                            new AssetIdentity(value.materialAssetGuid, value.materialLocalId)],
                        value.subMeshIndex,
                        MatrixFrom(value.localToPlacement),
                        BoundsFrom(value.localBoundsCenter, value.localBoundsExtents),
                        ColorFrom(value.linearBaseColor),
                        policy,
                        bucketIndices[
                            PolicyKey(
                                policy,
                                value.layer,
                                value.renderingLayerMask,
                                motion,
                                shadows)],
                        (OperationMapRenderLodFlags)value.lodFlags,
                        shadows);
                }).ToArray(),
                placements = inputs.placements.placements.Select(value =>
                    new OperationMapRenderPlacementConfigRecord(
                        value.stableIdentityLow,
                        value.stableIdentityHigh,
                        value.prototypeIndex,
                        MatrixFrom(value.worldMatrix),
                        value.cellIndex,
                        value.stateOwnerIndex,
                        Enum.Parse<OperationMapRenderVisualState>(value.requiredVisualState),
                         value.priority,
                         Enum.Parse<DenseCityPresentationSemanticCategory>(
                             value.semanticCategory),
                         value.sourceOwnerIdentityLow,
                         value.sourceOwnerIdentityHigh)).ToArray(),
                cells = inputs.spatial.cells.Select(value =>
                    new OperationMapRenderCellConfigRecord(
                        new Vector2Int(value.coordinateX, value.coordinateZ),
                        BoundsFrom(value.worldBoundsCenter, value.worldBoundsExtents),
                        value.firstPlacementIndex,
                        value.placementIndexCount)).ToArray(),
                poolBuckets = buckets
            };
        }

        private static T LoadAsset<T>(AssetIdentity identity) where T : UnityEngine.Object
        {
            string path = AssetDatabase.GUIDToAssetPath(identity.guid);
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is T typed &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        asset,
                        out string guid,
                        out long localId) &&
                    guid == identity.guid &&
                    localId == identity.localId)
                    return typed;
            }
            throw new InvalidOperationException(
                $"Could not resolve {typeof(T).Name} {identity.guid}/{identity.localId}.");
        }

        private static string ComputeContentHash(BuildInputs inputs)
        {
            string source =
                $"{inputs.capacity.sourceRowsSha256}\n" +
                $"{inputs.prototypes.prototypeRecipesSha256}\n" +
                $"{inputs.placements.logicalPlacementsSha256}\n" +
                $"{inputs.spatial.spatialCellsSha256}\n" +
                $"{inputs.capacity.totalProvisionalActiveProxySlots}\n";
            using SHA256 sha = SHA256.Create();
            return Hex(sha.ComputeHash(new UTF8Encoding(false).GetBytes(source)));
        }

        private static DatabaseReport CreateReport(
            BuildInputs inputs,
            GeneratedRecords records,
            string contentHash,
            string recordOrderingHash,
            string serializedHash) =>
            new()
            {
                schema = "warline.operation-map.render-virtualization-database",
                schemaVersion = 1,
                operationMapId = inputs.prototypes.operationMapId,
                result = "Passed",
                contentHash = contentHash,
                recordOrderingSha256 = recordOrderingHash,
                configPath = ConfigPath,
                configSerializedSha256 = serializedHash,
                meshCount = records.meshes.Length,
                materialCount = records.materials.Length,
                prototypeCount = records.prototypes.Length,
                partCount = records.parts.Length,
                placementCount = records.placements.Length,
                cellCount = records.cells.Length,
                cellPlacementIndexCount = inputs.spatial.cellPlacementIndices.Length,
                policyBucketCount = records.poolBuckets.Length,
                totalPoolSlotCapacity = records.poolBuckets.Sum(value => value.Capacity),
                sourceRenderRowCount =
                    OperationMapRenderEligibilityInventoryProbe.ExpectedPackedRenderRowCount,
                eligibleSourceRowCount = inputs.prototypes.eligibleSourceRowCount,
                logicalRenderRowCount = inputs.prototypes.eligibleSourceRowCount,
                residentSourceRowCount =
                    OperationMapRenderEligibilityInventoryProbe.ExpectedPackedRenderRowCount -
                    inputs.prototypes.eligibleSourceRowCount,
                sourceRowsRemoved = 0,
                logicalParityResult = "Passed",
                isolationResult = "Passed"
            };

        private static void RequirePersistedParity(
            OperationMapRenderDatabaseBakeConfig persisted,
            GeneratedRecords expected,
            int[] expectedCellPlacementIndices)
        {
            RequireCount(persisted.Meshes.Count, expected.meshes.Length, "meshes");
            RequireCount(persisted.Materials.Count, expected.materials.Length, "materials");
            RequireCount(persisted.Prototypes.Count, expected.prototypes.Length, "prototypes");
            RequireCount(persisted.Parts.Count, expected.parts.Length, "parts");
            RequireCount(persisted.Placements.Count, expected.placements.Length, "placements");
            RequireCount(persisted.Cells.Count, expected.cells.Length, "cells");
            RequireCount(
                persisted.CellPlacementIndices.Count,
                expectedCellPlacementIndices.Length,
                "cell placement indices");
            RequireCount(persisted.PoolBuckets.Count, expected.poolBuckets.Length, "pool buckets");

            for (int index = 0; index < expected.meshes.Length; index++)
            {
                OperationMapRenderMeshConfigRecord actual = persisted.Meshes[index];
                OperationMapRenderMeshConfigRecord value = expected.meshes[index];
                Require(
                    actual.AssetGuid == value.AssetGuid &&
                    actual.LocalId == value.LocalId &&
                    actual.Mesh == value.Mesh,
                    $"meshes[{index}]");
            }
            for (int index = 0; index < expected.materials.Length; index++)
            {
                OperationMapRenderMaterialConfigRecord actual = persisted.Materials[index];
                OperationMapRenderMaterialConfigRecord value = expected.materials[index];
                Require(
                    actual.AssetGuid == value.AssetGuid &&
                    actual.LocalId == value.LocalId &&
                    actual.Material == value.Material,
                    $"materials[{index}]");
            }
            for (int index = 0; index < expected.prototypes.Length; index++)
            {
                OperationMapRenderPrototypeConfigRecord actual = persisted.Prototypes[index];
                OperationMapRenderPrototypeConfigRecord value = expected.prototypes[index];
                Require(
                    actual.ContentIdentityLow == value.ContentIdentityLow &&
                    actual.ContentIdentityHigh == value.ContentIdentityHigh &&
                    actual.FirstPart == value.FirstPart &&
                    actual.PartCount == value.PartCount &&
                    Exact(actual.CombinedLocalBounds, value.CombinedLocalBounds) &&
                    actual.SemanticCategory == value.SemanticCategory &&
                    actual.EligibilityFlags == value.EligibilityFlags,
                    $"prototypes[{index}]");
            }
            for (int index = 0; index < expected.parts.Length; index++)
            {
                OperationMapRenderPrototypePartConfigRecord actual = persisted.Parts[index];
                OperationMapRenderPrototypePartConfigRecord value = expected.parts[index];
                Require(
                    actual.RendererPathIdentityLow == value.RendererPathIdentityLow &&
                    actual.RendererPathIdentityHigh == value.RendererPathIdentityHigh &&
                    actual.MeshIndex == value.MeshIndex &&
                    actual.MaterialIndex == value.MaterialIndex &&
                    actual.SubMeshIndex == value.SubMeshIndex &&
                    Exact(actual.LocalToPlacement, value.LocalToPlacement) &&
                    Exact(actual.LocalBounds, value.LocalBounds) &&
                    Exact(actual.LinearBaseColor, value.LinearBaseColor) &&
                    actual.PolicyBucket == value.PolicyBucket &&
                    actual.PoolBucketIndex == value.PoolBucketIndex &&
                    actual.LodFlags == value.LodFlags &&
                    actual.ShadowFlags == value.ShadowFlags,
                    $"parts[{index}]");
            }
            for (int index = 0; index < expected.placements.Length; index++)
            {
                OperationMapRenderPlacementConfigRecord actual = persisted.Placements[index];
                OperationMapRenderPlacementConfigRecord value = expected.placements[index];
                Require(
                    actual.StableIdentityLow == value.StableIdentityLow &&
                    actual.StableIdentityHigh == value.StableIdentityHigh &&
                    actual.SourceOwnerIdentityLow == value.SourceOwnerIdentityLow &&
                    actual.SourceOwnerIdentityHigh == value.SourceOwnerIdentityHigh &&
                    actual.PrototypeIndex == value.PrototypeIndex &&
                    Exact(actual.WorldMatrix, value.WorldMatrix) &&
                    actual.CellIndex == value.CellIndex &&
                    actual.StateOwnerIndex == value.StateOwnerIndex &&
                    actual.RequiredVisualState == value.RequiredVisualState &&
                    actual.Priority == value.Priority &&
                    actual.SemanticCategory == value.SemanticCategory,
                    $"placements[{index}]");
            }
            for (int index = 0; index < expected.cells.Length; index++)
            {
                OperationMapRenderCellConfigRecord actual = persisted.Cells[index];
                OperationMapRenderCellConfigRecord value = expected.cells[index];
                Require(
                    actual.Coordinate == value.Coordinate &&
                    Exact(actual.WorldBounds, value.WorldBounds) &&
                    actual.FirstPlacementIndex == value.FirstPlacementIndex &&
                    actual.PlacementIndexCount == value.PlacementIndexCount,
                    $"cells[{index}]");
            }
            for (int index = 0; index < expectedCellPlacementIndices.Length; index++)
                Require(
                    persisted.CellPlacementIndices[index] == expectedCellPlacementIndices[index],
                    $"cellPlacementIndices[{index}]");
            for (int index = 0; index < expected.poolBuckets.Length; index++)
            {
                OperationMapRenderPoolBucketConfigRecord actual = persisted.PoolBuckets[index];
                OperationMapRenderPoolBucketConfigRecord value = expected.poolBuckets[index];
                Require(
                    actual.PolicyBucket == value.PolicyBucket &&
                    actual.Layer == value.Layer &&
                    actual.RenderingLayerMask == value.RenderingLayerMask &&
                    actual.MotionVectorMode == value.MotionVectorMode &&
                    actual.ShadowFlags == value.ShadowFlags &&
                    actual.FirstSlot == value.FirstSlot &&
                    actual.Capacity == value.Capacity &&
                    actual.PeakRequiredCount == value.PeakRequiredCount &&
                    actual.HeadroomCount == value.HeadroomCount &&
                    actual.ReportIdentityLow == value.ReportIdentityLow &&
                    actual.ReportIdentityHigh == value.ReportIdentityHigh,
                    $"poolBuckets[{index}]");
            }
        }

        private static bool Exact(Matrix4x4 left, Matrix4x4 right)
        {
            for (int index = 0; index < 16; index++)
            {
                if (BitConverter.SingleToInt32Bits(left[index]) !=
                    BitConverter.SingleToInt32Bits(right[index]))
                    return false;
            }
            return true;
        }

        private static bool Exact(Bounds left, Bounds right) =>
            Exact(left.center, right.center) && Exact(left.extents, right.extents);

        private static bool Exact(Vector3 left, Vector3 right) =>
            BitConverter.SingleToInt32Bits(left.x) == BitConverter.SingleToInt32Bits(right.x) &&
            BitConverter.SingleToInt32Bits(left.y) == BitConverter.SingleToInt32Bits(right.y) &&
            BitConverter.SingleToInt32Bits(left.z) == BitConverter.SingleToInt32Bits(right.z);

        private static bool Exact(Color left, Color right) =>
            BitConverter.SingleToInt32Bits(left.r) == BitConverter.SingleToInt32Bits(right.r) &&
            BitConverter.SingleToInt32Bits(left.g) == BitConverter.SingleToInt32Bits(right.g) &&
            BitConverter.SingleToInt32Bits(left.b) == BitConverter.SingleToInt32Bits(right.b) &&
            BitConverter.SingleToInt32Bits(left.a) == BitConverter.SingleToInt32Bits(right.a);

        private static void RequireCount(int actual, int expected, string label)
        {
            if (actual != expected)
                throw new InvalidOperationException(
                    $"Persisted {label} count changed: {actual} != {expected}.");
        }

        private static void Require(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(
                    $"Persisted render database parity failed at {label}.");
        }

        private static string ComputeRecordOrderingHash(
            GeneratedRecords records,
            int[] cellPlacementIndices)
        {
            var source = new StringBuilder();
            foreach (OperationMapRenderMeshConfigRecord value in records.meshes)
                source.Append("m:").Append(value.AssetGuid).Append(':').Append(value.LocalId).Append('\n');
            foreach (OperationMapRenderMaterialConfigRecord value in records.materials)
                source.Append("a:").Append(value.AssetGuid).Append(':').Append(value.LocalId).Append('\n');
            foreach (OperationMapRenderPrototypeConfigRecord value in records.prototypes)
                source.Append("p:").Append(value.ContentIdentityLow).Append(':')
                    .Append(value.ContentIdentityHigh).Append(':').Append(value.FirstPart)
                    .Append(':').Append(value.PartCount).Append('\n');
            foreach (OperationMapRenderPrototypePartConfigRecord value in records.parts)
                source.Append("r:").Append(value.RendererPathIdentityLow).Append(':')
                    .Append(value.RendererPathIdentityHigh).Append(':').Append(value.MeshIndex)
                    .Append(':').Append(value.MaterialIndex).Append(':').Append(value.SubMeshIndex)
                    .Append(':').Append(value.PoolBucketIndex).Append('\n');
            foreach (OperationMapRenderPlacementConfigRecord value in records.placements)
                source.Append("l:").Append(value.StableIdentityLow).Append(':')
                    .Append(value.StableIdentityHigh).Append(':')
                    .Append(value.SourceOwnerIdentityLow).Append(':')
                    .Append(value.SourceOwnerIdentityHigh).Append(':')
                    .Append(value.PrototypeIndex)
                    .Append(':').Append(value.CellIndex).Append(':').Append(value.StateOwnerIndex)
                    .Append('\n');
            foreach (OperationMapRenderCellConfigRecord value in records.cells)
                source.Append("c:").Append(value.Coordinate.x).Append(':')
                    .Append(value.Coordinate.y).Append(':').Append(value.FirstPlacementIndex)
                    .Append(':').Append(value.PlacementIndexCount).Append('\n');
            foreach (int value in cellPlacementIndices)
                source.Append("i:").Append(value).Append('\n');
            foreach (OperationMapRenderPoolBucketConfigRecord value in records.poolBuckets)
                source.Append("b:").Append((byte)value.PolicyBucket).Append(':')
                    .Append(value.Layer).Append(':').Append(value.RenderingLayerMask).Append(':')
                    .Append((byte)value.MotionVectorMode).Append(':').Append((byte)value.ShadowFlags)
                    .Append(':').Append(value.FirstSlot).Append(':').Append(value.Capacity).Append('\n');
            using SHA256 sha = SHA256.Create();
            return Hex(sha.ComputeHash(new UTF8Encoding(false).GetBytes(source.ToString())));
        }

        private static int ComparePolicy(CapacityPolicy left, CapacityPolicy right)
        {
            int comparison = Enum.Parse<OperationMapRenderPolicyBucket>(left.policyBucket)
                .CompareTo(Enum.Parse<OperationMapRenderPolicyBucket>(right.policyBucket));
            if (comparison != 0) return comparison;
            comparison = left.layer.CompareTo(right.layer);
            if (comparison != 0) return comparison;
            comparison = left.renderingLayerMask.CompareTo(right.renderingLayerMask);
            if (comparison != 0) return comparison;
            comparison = Enum.Parse<OperationMapRenderMotionVectorMode>(left.motionVectorMode)
                .CompareTo(Enum.Parse<OperationMapRenderMotionVectorMode>(right.motionVectorMode));
            return comparison != 0 ? comparison : left.shadowFlags.CompareTo(right.shadowFlags);
        }

        private static string PolicyKey(
            OperationMapRenderPolicyBucket policy,
            int layer,
            uint renderingLayerMask,
            OperationMapRenderMotionVectorMode motion,
            OperationMapRenderShadowFlags shadows) =>
            $"{(byte)policy}:{layer}:{renderingLayerMask}:{(byte)motion}:{(byte)shadows}";

        private static Matrix4x4 MatrixFrom(float[] values)
        {
            if (values == null || values.Length != 16)
                throw new InvalidOperationException("Matrix report field must contain 16 values.");
            Matrix4x4 matrix = default;
            for (int index = 0; index < 16; index++)
                matrix[index] = values[index];
            return matrix;
        }

        private static Vector3 Vector3From(float[] values)
        {
            if (values == null || values.Length != 3)
                throw new InvalidOperationException("Vector report field must contain 3 values.");
            return new Vector3(values[0], values[1], values[2]);
        }

        private static Bounds BoundsFrom(float[] center, float[] extents) =>
            new(Vector3From(center), Vector3.Scale(Vector3From(extents), Vector3.one * 2f));

        private static Color ColorFrom(float[] values)
        {
            if (values == null || values.Length != 4)
                throw new InvalidOperationException("Color report field must contain 4 values.");
            return new Color(values[0], values[1], values[2], values[3]);
        }

        private static string HashFile(string path)
        {
            using Stream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return Hex(sha.ComputeHash(stream));
        }

        private static string Hex(byte[] bytes) =>
            string.Concat(bytes.Select(value => value.ToString("x2")));

        private static void WriteAtomic(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                                      throw new InvalidOperationException("Report directory missing."));
            string temporary = path + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private readonly struct AssetIdentity : IEquatable<AssetIdentity>
        {
            internal readonly string guid;
            internal readonly long localId;
            internal AssetIdentity(string guid, long localId)
            {
                this.guid = guid;
                this.localId = localId;
            }
            public bool Equals(AssetIdentity other) =>
                guid == other.guid && localId == other.localId;
            public override bool Equals(object obj) =>
                obj is AssetIdentity other && Equals(other);
            public override int GetHashCode() =>
                unchecked(((guid != null ? guid.GetHashCode() : 0) * 397) ^ localId.GetHashCode());
        }

        private sealed class GeneratedRecords
        {
            internal OperationMapRenderMeshConfigRecord[] meshes;
            internal OperationMapRenderMaterialConfigRecord[] materials;
            internal OperationMapRenderPrototypeConfigRecord[] prototypes;
            internal OperationMapRenderPrototypePartConfigRecord[] parts;
            internal OperationMapRenderPlacementConfigRecord[] placements;
            internal OperationMapRenderCellConfigRecord[] cells;
            internal OperationMapRenderPoolBucketConfigRecord[] poolBuckets;
        }

        private sealed class BuildInputs
        {
            internal PrototypeReport prototypes;
            internal PlacementReport placements;
            internal SpatialReport spatial;
            internal CapacityReport capacity;
        }

        [Serializable] private sealed class PrototypeReport
        {
            public string operationMapId;
            public string result;
            public string prototypeRecipesSha256;
            public int eligibleSourceRowCount;
            public PrototypeDto[] prototypes;
            public PartDto[] parts;
        }
        [Serializable] private sealed class PrototypeDto
        {
            public ulong prototypeIdentityLow;
            public ulong prototypeIdentityHigh;
            public string semanticCategory;
            public int firstPart;
            public int partCount;
            public float[] combinedLocalBoundsCenter;
            public float[] combinedLocalBoundsExtents;
        }
        [Serializable] private sealed class PartDto
        {
            public string rendererPath;
            public string meshAssetGuid;
            public long meshLocalId;
            public string materialAssetGuid;
            public long materialLocalId;
            public int subMeshIndex;
            public float[] localToPlacement;
            public float[] localBoundsCenter;
            public float[] localBoundsExtents;
            public float[] linearBaseColor;
            public string policyBucket;
            public int layer;
            public uint renderingLayerMask;
            public string motionVectorMode;
            public byte shadowFlags;
            public byte lodFlags;
        }
        [Serializable] private sealed class PlacementReport
        {
            public string operationMapId;
            public string result;
            public string logicalPlacementsSha256;
            public PlacementDto[] placements;
        }
        [Serializable] private sealed class PlacementDto
        {
            public ulong stableIdentityLow;
            public ulong stableIdentityHigh;
            public ulong sourceOwnerIdentityLow;
            public ulong sourceOwnerIdentityHigh;
            public int prototypeIndex;
            public float[] worldMatrix;
            public int cellIndex;
            public int stateOwnerIndex;
            public string requiredVisualState;
            public int priority;
            public string semanticCategory;
        }
        [Serializable] private sealed class SpatialReport
        {
            public string operationMapId;
            public string result;
            public float cellSize;
            public float[] gridOrigin;
            public int[] gridDimensions;
            public string spatialCellsSha256;
            public CellDto[] cells;
            public int[] cellPlacementIndices;
        }
        [Serializable] private sealed class CellDto
        {
            public int coordinateX;
            public int coordinateZ;
            public float[] worldBoundsCenter;
            public float[] worldBoundsExtents;
            public int firstPlacementIndex;
            public int placementIndexCount;
        }
        [Serializable] private sealed class CapacityReport
        {
            public string operationMapId;
            public string result;
            public string sourceRowsSha256;
            public int totalProvisionalActiveProxySlots;
            public CapacityPolicy[] capacityByPolicy;
        }
        [Serializable] private sealed class CapacityPolicy
        {
            public string policyBucket;
            public int layer;
            public uint renderingLayerMask;
            public string motionVectorMode;
            public byte shadowFlags;
            public int sweepSampleCount;
            public int peakRequiredPartRows;
            public int capacity;
            public int headroomCount;
        }
        [Serializable] private sealed class DatabaseReport
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public string contentHash;
            public string recordOrderingSha256;
            public string configPath;
            public string configSerializedSha256;
            public int meshCount;
            public int materialCount;
            public int prototypeCount;
            public int partCount;
            public int placementCount;
            public int cellCount;
            public int cellPlacementIndexCount;
            public int policyBucketCount;
            public int totalPoolSlotCapacity;
            public int sourceRenderRowCount;
            public int eligibleSourceRowCount;
            public int logicalRenderRowCount;
            public int residentSourceRowCount;
            public int sourceRowsRemoved;
            public string logicalParityResult;
            public string isolationResult;
        }
    }
}
