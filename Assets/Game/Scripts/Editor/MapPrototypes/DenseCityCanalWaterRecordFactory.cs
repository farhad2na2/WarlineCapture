using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityCanalWaterRecordInput
    {
        internal DenseCityCanalWaterRecordInput(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            string sourceAssetGuid,
            long sourceLocalId,
            IReadOnlyList<string> bedMaterialAssetGuids,
            IReadOnlyList<string> waterMaterialAssetGuids,
            Matrix4x4 bedWorldMatrix,
            Matrix4x4 waterWorldMatrix,
            Vector2 exclusionSize,
            float exclusionElevation,
            int surfaceLayer,
            Vector2Int chunk)
        {
            if (sequenceStart < 0 || sequenceStart > int.MaxValue - 2)
                throw new ArgumentOutOfRangeException(nameof(sequenceStart));

            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            SourceAssetGuid = sourceAssetGuid;
            SourceLocalId = sourceLocalId;
            BedMaterialAssetGuids = bedMaterialAssetGuids;
            WaterMaterialAssetGuids = waterMaterialAssetGuids;
            BedWorldMatrix = bedWorldMatrix;
            WaterWorldMatrix = waterWorldMatrix;
            ExclusionSize = exclusionSize;
            ExclusionElevation = exclusionElevation;
            SurfaceLayer = surfaceLayer;
            Chunk = chunk;
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal int SequenceStart { get; }
        internal string SourceAssetGuid { get; }
        internal long SourceLocalId { get; }
        internal IReadOnlyList<string> BedMaterialAssetGuids { get; }
        internal IReadOnlyList<string> WaterMaterialAssetGuids { get; }
        internal Matrix4x4 BedWorldMatrix { get; }
        internal Matrix4x4 WaterWorldMatrix { get; }
        internal Vector2 ExclusionSize { get; }
        internal float ExclusionElevation { get; }
        internal int SurfaceLayer { get; }
        internal Vector2Int Chunk { get; }
    }

    internal static class DenseCityCanalWaterRecordFactory
    {
        internal static DenseCityCanalWaterRecordGroup Create(DenseCityCanalWaterRecordInput input)
        {
            DenseCityRecordIdentity exclusionIdentity = CreateIdentity(input, 0, "canal-water-exclusion");
            DenseCityRecordIdentity bedIdentity = CreateIdentity(input, 1, "canal-bed-visual");
            DenseCityRecordIdentity waterIdentity = CreateIdentity(input, 2, "canal-water-visual");
            var exclusion = new DenseCitySurfaceBakeRecord(
                exclusionIdentity,
                DenseCitySurfaceRecordKind.Blocker,
                CreateSurfacePolygon(input.WaterWorldMatrix, input.ExclusionSize),
                input.ExclusionElevation,
                0,
                input.SurfaceLayer,
                input.Chunk);
            var bedPresentation = new DenseCityPresentationBakeRecord(
                bedIdentity,
                DenseCityPresentationCategory.Infrastructure,
                input.SourceAssetGuid,
                null,
                input.BedMaterialAssetGuids,
                input.BedWorldMatrix,
                false,
                true,
                1);
            var waterPresentation = new DenseCityPresentationBakeRecord(
                waterIdentity,
                DenseCityPresentationCategory.Infrastructure,
                input.SourceAssetGuid,
                null,
                input.WaterMaterialAssetGuids,
                input.WaterWorldMatrix,
                false,
                true,
                2);
            return new DenseCityCanalWaterRecordGroup(exclusion, bedPresentation, waterPresentation);
        }

        private static DenseCityRecordIdentity CreateIdentity(
            DenseCityCanalWaterRecordInput input,
            int sequenceOffset,
            string kind) =>
            new(
                input.GeneratorSchema,
                input.Seed,
                input.DistrictId,
                kind,
                input.SequenceStart + sequenceOffset,
                input.SourceAssetGuid,
                input.SourceLocalId);

        private static Vector2[] CreateSurfacePolygon(Matrix4x4 worldMatrix, Vector2 size)
        {
            if (!float.IsFinite(size.x) || !float.IsFinite(size.y) || size.x <= 0f || size.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(size));

            Vector3 center = worldMatrix.GetColumn(3);
            Vector3 right = worldMatrix.GetColumn(0);
            Vector3 forward = worldMatrix.GetColumn(2);
            right.y = 0f;
            forward.y = 0f;
            if (right.sqrMagnitude <= 0.000001f || forward.sqrMagnitude <= 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(worldMatrix));
            right.Normalize();
            forward.Normalize();
            right *= size.x * 0.5f;
            forward *= size.y * 0.5f;
            return new[]
            {
                new Vector2(center.x - right.x - forward.x, center.z - right.z - forward.z),
                new Vector2(center.x + right.x - forward.x, center.z + right.z - forward.z),
                new Vector2(center.x + right.x + forward.x, center.z + right.z + forward.z),
                new Vector2(center.x - right.x + forward.x, center.z - right.z + forward.z)
            };
        }
    }
}
