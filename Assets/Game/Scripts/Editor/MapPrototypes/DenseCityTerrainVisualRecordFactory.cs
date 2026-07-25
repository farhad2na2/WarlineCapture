using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityTerrainVisualPresentationInput
    {
        internal DenseCityTerrainVisualPresentationInput(
            string recordKind,
            string sourceAssetGuid,
            long sourceLocalId,
            IReadOnlyList<string> materialAssetGuids,
            Matrix4x4 worldMatrix,
            bool castsShadows,
            bool batchingEligible,
            byte lodImportance,
            bool allowsProtectedOverlap = false)
        {
            RecordKind = recordKind;
            SourceAssetGuid = sourceAssetGuid;
            SourceLocalId = sourceLocalId;
            MaterialAssetGuids = materialAssetGuids;
            WorldMatrix = worldMatrix;
            CastsShadows = castsShadows;
            BatchingEligible = batchingEligible;
            LodImportance = lodImportance;
            AllowsProtectedOverlap = allowsProtectedOverlap;
        }

        internal string RecordKind { get; }
        internal string SourceAssetGuid { get; }
        internal long SourceLocalId { get; }
        internal IReadOnlyList<string> MaterialAssetGuids { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal bool CastsShadows { get; }
        internal bool BatchingEligible { get; }
        internal byte LodImportance { get; }
        internal bool AllowsProtectedOverlap { get; }
    }

    internal readonly struct DenseCityTerrainVisualRecordInput
    {
        internal DenseCityTerrainVisualRecordInput(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            string terrainRecordKind,
            Matrix4x4 terrainWorldMatrix,
            Vector2 terrainSize,
            float terrainElevation,
            uint movementMask,
            int surfaceLayer,
            Vector2Int chunk,
            IReadOnlyList<DenseCityTerrainVisualPresentationInput> presentations)
        {
            if (sequenceStart < 0)
                throw new ArgumentOutOfRangeException(nameof(sequenceStart));
            if (presentations == null || presentations.Count == 0 || presentations.Count > 16 ||
                sequenceStart > int.MaxValue - presentations.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(presentations));
            }
            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            TerrainRecordKind = terrainRecordKind;
            TerrainWorldMatrix = terrainWorldMatrix;
            TerrainSize = terrainSize;
            TerrainElevation = terrainElevation;
            MovementMask = movementMask;
            SurfaceLayer = surfaceLayer;
            Chunk = chunk;
            Presentations = presentations;
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal int SequenceStart { get; }
        internal string TerrainRecordKind { get; }
        internal Matrix4x4 TerrainWorldMatrix { get; }
        internal Vector2 TerrainSize { get; }
        internal float TerrainElevation { get; }
        internal uint MovementMask { get; }
        internal int SurfaceLayer { get; }
        internal Vector2Int Chunk { get; }
        internal IReadOnlyList<DenseCityTerrainVisualPresentationInput> Presentations { get; }
    }

    internal static class DenseCityTerrainVisualRecordFactory
    {
        internal static DenseCityTerrainVisualRecordGroup Create(DenseCityTerrainVisualRecordInput input)
        {
            DenseCityTerrainVisualPresentationInput first = input.Presentations[0];
            var terrainIdentity = new DenseCityRecordIdentity(
                input.GeneratorSchema,
                input.Seed,
                input.DistrictId,
                input.TerrainRecordKind,
                input.SequenceStart,
                first.SourceAssetGuid,
                first.SourceLocalId);
            var terrain = new DenseCitySurfaceBakeRecord(
                terrainIdentity,
                DenseCitySurfaceRecordKind.Terrain,
                CreateSurfacePolygon(input.TerrainWorldMatrix, input.TerrainSize),
                input.TerrainElevation,
                input.MovementMask,
                input.SurfaceLayer,
                input.Chunk);
            var presentations = new DenseCityPresentationBakeRecord[input.Presentations.Count];
            for (int index = 0; index < presentations.Length; index++)
            {
                DenseCityTerrainVisualPresentationInput presentationInput = input.Presentations[index];
                var identity = new DenseCityRecordIdentity(
                    input.GeneratorSchema,
                    input.Seed,
                    input.DistrictId,
                    presentationInput.RecordKind,
                    input.SequenceStart + index + 1,
                    presentationInput.SourceAssetGuid,
                    presentationInput.SourceLocalId);
                presentations[index] = new DenseCityPresentationBakeRecord(
                    identity,
                    DenseCityPresentationCategory.Infrastructure,
                    presentationInput.SourceAssetGuid,
                    null,
                    presentationInput.MaterialAssetGuids,
                    presentationInput.WorldMatrix,
                    presentationInput.CastsShadows,
                    presentationInput.BatchingEligible,
                    presentationInput.LodImportance,
                    allowsProtectedOverlap: presentationInput.AllowsProtectedOverlap);
            }
            return new DenseCityTerrainVisualRecordGroup(terrain, presentations);
        }

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
