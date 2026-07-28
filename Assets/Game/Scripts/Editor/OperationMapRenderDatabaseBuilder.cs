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

                string serializedHash = HashFile(Path.Combine(projectRoot, ConfigPath));
                DatabaseReport report = CreateReport(inputs, records, contentHash, serializedHash);
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
                        OperationMapRenderEligibilityFlags.Eligible)).ToArray(),
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
                            value.semanticCategory))).ToArray(),
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
            string serializedHash) =>
            new()
            {
                schema = "warline.operation-map.render-virtualization-database",
                schemaVersion = 1,
                operationMapId = inputs.prototypes.operationMapId,
                result = "Passed",
                contentHash = contentHash,
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
                isolationResult = "Passed"
            };

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
            public string isolationResult;
        }
    }
}
