#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Components;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using UnityEngine;

    /// <summary>
    /// Writes the compact expected-data side of dense packed-runtime parity while the validated
    /// in-memory bake world is still alive.
    /// </summary>
    internal static class OperationMapDenseCityRuntimeParityManifestWriter
    {
        internal const string ManifestPath =
            "Library/OperationMapDenseCityRuntimeParity/dense_candidate_runtime_parity.bin";
        internal const string SummaryPath =
            "Design/AgentReports/2026-07-24_dense_city_runtime_parity_manifest.json";
        internal const uint Magic = 0x57444350; // WDCP
        internal const int FormatVersion = 3;

        private const string OperationMapId = "opmap.skirmish.desert_base_01";
        private const string EntitySceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        private const int ExpectedLegacyIdentityCount = 9544;
        private const int ExpectedDenseIdentityCount = 35796;
        private const int ExpectedRenderRowCount = 78325;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        internal static DenseRuntimeParityManifestSummary Write(
            string projectRoot,
            EntityManager entityManager)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("A project root is required.", nameof(projectRoot));

            LegacyIdentityRow[] legacyIdentities = ReadLegacyIdentities(entityManager);
            DenseIdentityRow[] denseIdentities = ReadDenseIdentities(entityManager);
            RenderRow[] renderRows = ReadRenderRows(entityManager);
            RequireExpectedCounts(legacyIdentities, denseIdentities, renderRows);
            string candidateSubSceneSha256 = ComputeSha256(Resolve(
                projectRoot,
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath));
            string directBakeParityReportSha256 = ComputeSha256(Resolve(
                projectRoot,
                OperationMapDenseCityGeneratedTransformParityValidator.DefaultReportPath));

            string manifestPath = Resolve(projectRoot, ManifestPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(manifestPath) ??
                throw new InvalidOperationException("Dense runtime parity manifest has no parent."));
            string temporaryPath = manifestPath + ".tmp";
            string backupPath = manifestPath + ".bak";
            DeleteIfExists(temporaryPath);
            DeleteIfExists(backupPath);

            try
            {
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           1024 * 1024,
                           FileOptions.SequentialScan))
                using (var writer = new BinaryWriter(stream, Utf8WithoutBom, leaveOpen: false))
                {
                    writer.Write(Magic);
                    writer.Write(FormatVersion);
                    WriteString(writer, OperationMapId);
                    WriteString(writer, EntitySceneGuid);
                    WriteString(writer, candidateSubSceneSha256);
                    WriteString(writer, directBakeParityReportSha256);
                    writer.Write(OperationMapDenseCityGeneratedTransformParityValidator.MatrixTolerance);
                    writer.Write(OperationMapDenseCityGeneratedTransformParityValidator.BoundsTolerance);
                    writer.Write(legacyIdentities.Length);
                    writer.Write(denseIdentities.Length);
                    writer.Write(renderRows.Length);

                    for (int i = 0; i < legacyIdentities.Length; i++)
                    {
                        WriteString(writer, legacyIdentities[i].SourceGlobalObjectId);
                        writer.Write(legacyIdentities[i].Role);
                        writer.Write(legacyIdentities[i].PlacementIndex);
                        WriteMatrix(writer, legacyIdentities[i].WorldMatrix);
                    }
                    for (int i = 0; i < denseIdentities.Length; i++)
                    {
                        WriteString(writer, denseIdentities[i].StableId);
                        writer.Write(denseIdentities[i].Role);
                        WriteMatrix(writer, denseIdentities[i].WorldMatrix);
                    }
                    for (int i = 0; i < renderRows.Length; i++)
                        renderRows[i].Write(writer);
                }

                Publish(temporaryPath, manifestPath, backupPath);
                string sha256 = ComputeSha256(manifestPath);
                var summary = new DenseRuntimeParityManifestSummary
                {
                    schema = "warline.operation-map.dense-city-runtime-parity-manifest",
                    schemaVersion = 1,
                    result = "DenseCityRuntimeParityManifestWritten",
                    operationMapId = OperationMapId,
                    entitySceneGuid = EntitySceneGuid,
                    candidateSubSceneSha256 = candidateSubSceneSha256,
                    directBakeParityReportSha256 = directBakeParityReportSha256,
                    formatVersion = FormatVersion,
                    matrixTolerance =
                        OperationMapDenseCityGeneratedTransformParityValidator.MatrixTolerance,
                    boundsTolerance =
                        OperationMapDenseCityGeneratedTransformParityValidator.BoundsTolerance,
                    legacyIdentityCount = legacyIdentities.Length,
                    denseIdentityCount = denseIdentities.Length,
                    renderRowCount = renderRows.Length,
                    manifestBytes = new FileInfo(manifestPath).Length,
                    manifestSha256 = sha256,
                    productionCutover = 0
                };
                WriteSummary(projectRoot, summary);
                return summary;
            }
            catch
            {
                DeleteIfExists(temporaryPath);
                if (!File.Exists(manifestPath) && File.Exists(backupPath))
                    File.Move(backupPath, manifestPath);
                throw;
            }
            finally
            {
                DeleteIfExists(temporaryPath);
                DeleteIfExists(backupPath);
            }
        }

        private static LegacyIdentityRow[] ReadLegacyIdentities(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationIdentity>());
            using NativeArray<OperationMapEntityPresentationIdentity> values =
                query.ToComponentDataArray<OperationMapEntityPresentationIdentity>(Allocator.Temp);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var result = new LegacyIdentityRow[values.Length];
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i++)
            {
                string operationMapId = values[i].OperationMapId.ToString();
                string sourceId = values[i].SourceGlobalObjectId.ToString();
                if (!string.Equals(operationMapId, OperationMapId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Dense parity legacy identity has wrong map id: {operationMapId}");
                }
                if (string.IsNullOrWhiteSpace(sourceId) || !unique.Add(sourceId))
                {
                    throw new InvalidOperationException(
                        $"Dense parity legacy identity is empty or duplicated: {sourceId}");
                }
                result[i] = new LegacyIdentityRow(
                    sourceId,
                    values[i].Role,
                    values[i].PlacementIndex,
                    ReadWorldMatrix(entityManager, entities[i]));
            }
            Array.Sort(result, LegacyIdentityRowComparer.Instance);
            return result;
        }

        private static DenseIdentityRow[] ReadDenseIdentities(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DenseCityPresentationIdentity>());
            using NativeArray<DenseCityPresentationIdentity> values =
                query.ToComponentDataArray<DenseCityPresentationIdentity>(Allocator.Temp);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var result = new DenseIdentityRow[values.Length];
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i++)
            {
                string stableId = values[i].StableId.ToString();
                if (string.IsNullOrWhiteSpace(stableId) || !unique.Add(stableId))
                {
                    throw new InvalidOperationException(
                        $"Dense parity generated identity is empty or duplicated: {stableId}");
                }
                result[i] = new DenseIdentityRow(
                    stableId,
                    values[i].Role,
                    ReadWorldMatrix(entityManager, entities[i]));
            }
            Array.Sort(result, DenseIdentityRowComparer.Instance);
            return result;
        }

        private static RenderRow[] ReadRenderRows(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>());
            using NativeArray<RenderBounds> bounds =
                query.ToComponentDataArray<RenderBounds>(Allocator.Temp);
            using NativeArray<LocalToWorld> transforms =
                query.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
            if (bounds.Length != transforms.Length)
                throw new InvalidOperationException("Dense parity render component counts differ.");

            var result = new RenderRow[bounds.Length];
            for (int i = 0; i < result.Length; i++)
            {
                Matrix4x4 world = ToMatrix(transforms[i].Value);
                float3 center = bounds[i].Value.Center;
                float3 extents = bounds[i].Value.Extents;
                result[i] = RenderRow.Create(world, center, extents);
            }
            Array.Sort(result, RenderRowComparer.Instance);
            return result;
        }

        private static void RequireExpectedCounts(
            LegacyIdentityRow[] legacyIdentities,
            DenseIdentityRow[] denseIdentities,
            RenderRow[] renderRows)
        {
            if (legacyIdentities.Length != ExpectedLegacyIdentityCount ||
                denseIdentities.Length != ExpectedDenseIdentityCount ||
                renderRows.Length != ExpectedRenderRowCount)
            {
                throw new InvalidOperationException(
                    $"Dense parity manifest counts rejected: legacy={legacyIdentities.Length}/" +
                    $"{ExpectedLegacyIdentityCount}, dense={denseIdentities.Length}/" +
                    $"{ExpectedDenseIdentityCount}, renders={renderRows.Length}/" +
                    $"{ExpectedRenderRowCount}.");
            }
        }

        private static void WriteSummary(
            string projectRoot,
            DenseRuntimeParityManifestSummary summary)
        {
            string path = Resolve(projectRoot, SummaryPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Dense parity summary has no parent."));
            File.WriteAllText(path, JsonUtility.ToJson(summary, true) + "\n", Utf8WithoutBom);
        }

        private static void Publish(string temporaryPath, string path, string backupPath)
        {
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, backupPath);
                DeleteIfExists(backupPath);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }

        private static string Resolve(string projectRoot, string path) =>
            Path.GetFullPath(Path.Combine(projectRoot, path));

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8WithoutBom.GetBytes(value ?? string.Empty);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteMatrix(BinaryWriter writer, Matrix4x4 matrix)
        {
            for (int i = 0; i < 16; i++)
                writer.Write(matrix[i]);
        }

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            return string.Concat(
                sha256.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static Matrix4x4 ToMatrix(float4x4 value)
        {
            var matrix = new Matrix4x4();
            matrix.SetColumn(0, value.c0);
            matrix.SetColumn(1, value.c1);
            matrix.SetColumn(2, value.c2);
            matrix.SetColumn(3, value.c3);
            return matrix;
        }

        private static Matrix4x4 ReadWorldMatrix(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<LocalToWorld>(entity))
            {
                throw new InvalidOperationException(
                    $"Dense parity identity has no baked LocalToWorld: {entity}");
            }
            Matrix4x4 matrix = ToMatrix(
                entityManager.GetComponentData<LocalToWorld>(entity).Value);
            for (int i = 0; i < 16; i++)
                RequireFinite(matrix[i]);
            return matrix;
        }

        private static float[] TransformBounds(
            float3 center,
            float3 extents,
            Matrix4x4 matrix)
        {
            Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = (Vector3)center +
                                 Vector3.Scale((Vector3)extents, new Vector3(x, y, z));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            return new[] { min.x, min.y, min.z, max.x, max.y, max.z };
        }

        private static void RequireFinite(float value)
        {
            if (!float.IsFinite(value))
                throw new InvalidOperationException(
                    $"Dense parity manifest contains a non-finite render value: {value}");
        }

        private readonly struct LegacyIdentityRow
        {
            internal LegacyIdentityRow(
                string sourceGlobalObjectId,
                byte role,
                int placementIndex,
                Matrix4x4 worldMatrix)
            {
                SourceGlobalObjectId = sourceGlobalObjectId;
                Role = role;
                PlacementIndex = placementIndex;
                WorldMatrix = worldMatrix;
            }

            internal string SourceGlobalObjectId { get; }
            internal byte Role { get; }
            internal int PlacementIndex { get; }
            internal Matrix4x4 WorldMatrix { get; }
        }

        private sealed class LegacyIdentityRowComparer : IComparer<LegacyIdentityRow>
        {
            internal static readonly LegacyIdentityRowComparer Instance = new();

            public int Compare(LegacyIdentityRow left, LegacyIdentityRow right)
            {
                int identity = string.CompareOrdinal(
                    left.SourceGlobalObjectId,
                    right.SourceGlobalObjectId);
                if (identity != 0)
                    return identity;
                int role = left.Role.CompareTo(right.Role);
                return role != 0 ? role : left.PlacementIndex.CompareTo(right.PlacementIndex);
            }
        }

        private readonly struct DenseIdentityRow
        {
            internal DenseIdentityRow(
                string stableId,
                byte role,
                Matrix4x4 worldMatrix)
            {
                StableId = stableId;
                Role = role;
                WorldMatrix = worldMatrix;
            }

            internal string StableId { get; }
            internal byte Role { get; }
            internal Matrix4x4 WorldMatrix { get; }
        }

        private sealed class DenseIdentityRowComparer : IComparer<DenseIdentityRow>
        {
            internal static readonly DenseIdentityRowComparer Instance = new();

            public int Compare(DenseIdentityRow left, DenseIdentityRow right)
            {
                int identity = string.CompareOrdinal(left.StableId, right.StableId);
                return identity != 0 ? identity : left.Role.CompareTo(right.Role);
            }
        }

        private readonly struct RenderRow
        {
            private readonly float[] values;

            private RenderRow(float[] values)
            {
                this.values = values;
            }

            internal int Length => values.Length;
            internal float this[int index] => values[index];

            internal static RenderRow Create(
                Matrix4x4 world,
                float3 center,
                float3 extents)
            {
                var values = new float[28];
                for (int i = 0; i < 16; i++)
                {
                    RequireFinite(world[i]);
                    values[i] = world[i];
                }
                float[] local =
                {
                    center.x, center.y, center.z,
                    extents.x, extents.y, extents.z
                };
                for (int i = 0; i < local.Length; i++)
                {
                    RequireFinite(local[i]);
                    values[16 + i] = local[i];
                }
                float[] transformed = TransformBounds(center, extents, world);
                for (int i = 0; i < transformed.Length; i++)
                {
                    RequireFinite(transformed[i]);
                    values[22 + i] = transformed[i];
                }
                return new RenderRow(values);
            }

            internal void Write(BinaryWriter writer)
            {
                for (int i = 0; i < values.Length; i++)
                    writer.Write(values[i]);
            }
        }

        private sealed class RenderRowComparer : IComparer<RenderRow>
        {
            internal static readonly RenderRowComparer Instance = new();

            public int Compare(RenderRow left, RenderRow right)
            {
                int count = Math.Min(left.Length, right.Length);
                for (int i = 0; i < count; i++)
                {
                    int value = left[i].CompareTo(right[i]);
                    if (value != 0)
                        return value;
                }
                return left.Length.CompareTo(right.Length);
            }
        }

        [Serializable]
        internal sealed class DenseRuntimeParityManifestSummary
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string entitySceneGuid;
            public string candidateSubSceneSha256;
            public string directBakeParityReportSha256;
            public int formatVersion;
            public float matrixTolerance;
            public float boundsTolerance;
            public int legacyIdentityCount;
            public int denseIdentityCount;
            public int renderRowCount;
            public long manifestBytes;
            public string manifestSha256;
            public int productionCutover;
        }
    }
}

#endif
