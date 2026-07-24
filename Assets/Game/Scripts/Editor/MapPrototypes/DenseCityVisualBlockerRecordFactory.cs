using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityVisualBlockerRecordInput
    {
        internal DenseCityVisualBlockerRecordInput(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            string recordKind,
            string sourceAssetGuid,
            long sourceLocalId,
            IReadOnlyList<string> materialAssetGuids,
            Matrix4x4 worldMatrix,
            Vector2 blockerSize,
            float elevation,
            int surfaceLayer,
            Vector2Int chunk,
            bool castsShadows,
            bool batchingEligible,
            byte lodImportance,
            Matrix4x4? blockerWorldMatrix = null,
            Vector2? clippedBlockerSize = null)
        {
            if (sequenceStart < 0 || sequenceStart > int.MaxValue - 1)
                throw new ArgumentOutOfRangeException(nameof(sequenceStart));
            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            RecordKind = recordKind;
            SourceAssetGuid = sourceAssetGuid;
            SourceLocalId = sourceLocalId;
            MaterialAssetGuids = materialAssetGuids;
            WorldMatrix = worldMatrix;
            BlockerSize = blockerSize;
            Elevation = elevation;
            SurfaceLayer = surfaceLayer;
            Chunk = chunk;
            CastsShadows = castsShadows;
            BatchingEligible = batchingEligible;
            LodImportance = lodImportance;
            BlockerWorldMatrix = blockerWorldMatrix ?? worldMatrix;
            ClippedBlockerSize = clippedBlockerSize ?? blockerSize;
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal int SequenceStart { get; }
        internal string RecordKind { get; }
        internal string SourceAssetGuid { get; }
        internal long SourceLocalId { get; }
        internal IReadOnlyList<string> MaterialAssetGuids { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal Vector2 BlockerSize { get; }
        internal float Elevation { get; }
        internal int SurfaceLayer { get; }
        internal Vector2Int Chunk { get; }
        internal bool CastsShadows { get; }
        internal bool BatchingEligible { get; }
        internal byte LodImportance { get; }
        internal Matrix4x4 BlockerWorldMatrix { get; }
        internal Vector2 ClippedBlockerSize { get; }
    }

    internal static class DenseCityVisualBlockerRecordFactory
    {
        internal static DenseCityVisualBlockerRecordGroup Create(
            DenseCityVisualBlockerRecordInput input)
        {
            var blockerIdentity = new DenseCityRecordIdentity(
                input.GeneratorSchema,
                input.Seed,
                input.DistrictId,
                input.RecordKind,
                input.SequenceStart,
                input.SourceAssetGuid,
                input.SourceLocalId);
            var presentationIdentity = new DenseCityRecordIdentity(
                input.GeneratorSchema,
                input.Seed,
                input.DistrictId,
                string.Concat(input.RecordKind, "-visual"),
                input.SequenceStart + 1,
                input.SourceAssetGuid,
                input.SourceLocalId);
            var blocker = new DenseCitySurfaceBakeRecord(
                blockerIdentity,
                DenseCitySurfaceRecordKind.Blocker,
                CreateFootprint(input.BlockerWorldMatrix, input.ClippedBlockerSize),
                input.Elevation,
                0,
                input.SurfaceLayer,
                input.Chunk);
            var presentation = new DenseCityPresentationBakeRecord(
                presentationIdentity,
                DenseCityPresentationCategory.Infrastructure,
                input.SourceAssetGuid,
                null,
                input.MaterialAssetGuids,
                input.WorldMatrix,
                input.CastsShadows,
                input.BatchingEligible,
                input.LodImportance);
            return new DenseCityVisualBlockerRecordGroup(blocker, presentation);
        }

        private static Vector2[] CreateFootprint(Matrix4x4 worldMatrix, Vector2 size)
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
                ToXZ(center - right - forward),
                ToXZ(center + right - forward),
                ToXZ(center + right + forward),
                ToXZ(center - right + forward)
            };
        }

        private static Vector2 ToXZ(Vector3 value) => new(value.x, value.z);
    }
}
