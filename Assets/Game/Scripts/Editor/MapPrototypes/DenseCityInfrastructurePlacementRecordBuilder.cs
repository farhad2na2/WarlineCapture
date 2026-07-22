using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityInfrastructurePlacementRecordRequest
    {
        internal DenseCityInfrastructurePlacementRecordRequest(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            string recordKind,
            DenseCitySurfaceRecordKind surfaceKind,
            GameObject sourcePrefab,
            Func<Material, Material> materialResolver,
            Matrix4x4 worldMatrix,
            Vector2 surfaceSize,
            float elevation,
            uint movementMask,
            int surfaceLayer,
            Vector2Int chunk,
            bool castsShadows,
            bool batchingEligible,
            byte lodImportance)
        {
            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            RecordKind = recordKind;
            SurfaceKind = surfaceKind;
            SourcePrefab = sourcePrefab;
            MaterialResolver = materialResolver;
            WorldMatrix = worldMatrix;
            SurfaceSize = surfaceSize;
            Elevation = elevation;
            MovementMask = movementMask;
            SurfaceLayer = surfaceLayer;
            Chunk = chunk;
            CastsShadows = castsShadows;
            BatchingEligible = batchingEligible;
            LodImportance = lodImportance;
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal int SequenceStart { get; }
        internal string RecordKind { get; }
        internal DenseCitySurfaceRecordKind SurfaceKind { get; }
        internal GameObject SourcePrefab { get; }
        internal Func<Material, Material> MaterialResolver { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal Vector2 SurfaceSize { get; }
        internal float Elevation { get; }
        internal uint MovementMask { get; }
        internal int SurfaceLayer { get; }
        internal Vector2Int Chunk { get; }
        internal bool CastsShadows { get; }
        internal bool BatchingEligible { get; }
        internal byte LodImportance { get; }
    }

    internal static class DenseCityInfrastructurePlacementRecordBuilder
    {
        internal static DenseCityInfrastructureRecordGroup CreateVisualized(
            DenseCityInfrastructurePlacementRecordRequest request) =>
            DenseCityInfrastructureRecordFactory.CreateVisualized(CreateInput(request));

        internal static DenseCitySurfaceBakeRecord CreateSurfaceOnlyRamp(
            DenseCityInfrastructurePlacementRecordRequest request) =>
            DenseCityInfrastructureRecordFactory.CreateSurfaceOnlyRamp(CreateInput(request));

        private static DenseCityInfrastructureRecordInput CreateInput(
            DenseCityInfrastructurePlacementRecordRequest request)
        {
            if (request.SourcePrefab == null)
                throw new ArgumentNullException(nameof(request.SourcePrefab));

            DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(
                request.SourcePrefab,
                request.MaterialResolver);
            return new DenseCityInfrastructureRecordInput(
                request.GeneratorSchema,
                request.Seed,
                request.DistrictId,
                request.SequenceStart,
                request.RecordKind,
                request.SurfaceKind,
                metadata.PrefabAssetGuid,
                metadata.PrefabLocalId,
                Copy(metadata.MaterialAssetGuids),
                request.WorldMatrix,
                request.SurfaceSize,
                request.Elevation,
                request.MovementMask,
                request.SurfaceLayer,
                request.Chunk,
                request.CastsShadows,
                request.BatchingEligible,
                request.LodImportance);
        }

        private static string[] Copy(IReadOnlyList<string> values)
        {
            var copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++)
                copy[index] = values[index];
            return copy;
        }
    }
}
