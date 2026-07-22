using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityInfrastructureRecordInput
    {
        internal DenseCityInfrastructureRecordInput(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            string recordKind,
            DenseCitySurfaceRecordKind surfaceKind,
            string sourceAssetGuid,
            long sourceLocalId,
            IReadOnlyList<string> materialAssetGuids,
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
            if (sequenceStart < 0 || sequenceStart > int.MaxValue - 1)
                throw new ArgumentOutOfRangeException(nameof(sequenceStart));
            if (surfaceKind is DenseCitySurfaceRecordKind.Unknown or DenseCitySurfaceRecordKind.Blocker)
                throw new ArgumentOutOfRangeException(nameof(surfaceKind));

            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            RecordKind = recordKind;
            SurfaceKind = surfaceKind;
            SourceAssetGuid = sourceAssetGuid;
            SourceLocalId = sourceLocalId;
            MaterialAssetGuids = materialAssetGuids;
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
        internal string SourceAssetGuid { get; }
        internal long SourceLocalId { get; }
        internal IReadOnlyList<string> MaterialAssetGuids { get; }
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

    internal readonly struct DenseCityBridgeApproachRecordInput
    {
        internal DenseCityBridgeApproachRecordInput(
            string recordKind,
            Matrix4x4 worldMatrix,
            Vector2 surfaceSize,
            float elevation,
            Vector2Int chunk)
        {
            RecordKind = recordKind;
            WorldMatrix = worldMatrix;
            SurfaceSize = surfaceSize;
            Elevation = elevation;
            Chunk = chunk;
        }

        internal string RecordKind { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal Vector2 SurfaceSize { get; }
        internal float Elevation { get; }
        internal Vector2Int Chunk { get; }
    }

    internal readonly struct DenseCityRoadShoulderRecordInput
    {
        internal DenseCityRoadShoulderRecordInput(
            Matrix4x4 worldMatrix,
            Vector2 surfaceSize,
            float elevation,
            Vector2Int chunk)
        {
            WorldMatrix = worldMatrix;
            SurfaceSize = surfaceSize;
            Elevation = elevation;
            Chunk = chunk;
        }

        internal Matrix4x4 WorldMatrix { get; }
        internal Vector2 SurfaceSize { get; }
        internal float Elevation { get; }
        internal Vector2Int Chunk { get; }
    }

    internal static class DenseCityInfrastructureRecordFactory
    {
        internal static DenseCityInfrastructureRecordGroup CreateVisualized(
            DenseCityInfrastructureRecordInput input)
        {
            DenseCityRecordIdentity surfaceIdentity = CreateIdentity(input, 0, input.RecordKind);
            DenseCityRecordIdentity presentationIdentity = CreateIdentity(
                input,
                1,
                string.Concat(input.RecordKind, "-visual"));
            var surface = CreateSurface(input, surfaceIdentity);
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
            return new DenseCityInfrastructureRecordGroup(surface, presentation);
        }

        internal static DenseCityRoadRecordGroup CreateRoadWithShoulders(
            DenseCityInfrastructureRecordInput roadInput,
            IReadOnlyList<DenseCityRoadShoulderRecordInput> shoulderInputs)
        {
            if (roadInput.SurfaceKind != DenseCitySurfaceRecordKind.Road)
                throw new ArgumentException("Road surface input is required.", nameof(roadInput));
            if (shoulderInputs == null)
                throw new ArgumentNullException(nameof(shoulderInputs));
            if (roadInput.SequenceStart > int.MaxValue - 1 - shoulderInputs.Count)
                throw new ArgumentOutOfRangeException(nameof(roadInput));

            DenseCityInfrastructureRecordGroup road = CreateVisualized(roadInput);
            var shoulders = new DenseCitySurfaceBakeRecord[shoulderInputs.Count];
            for (int index = 0; index < shoulderInputs.Count; index++)
            {
                DenseCityRoadShoulderRecordInput shoulderInput = shoulderInputs[index];
                DenseCityInfrastructureRecordInput surfaceInput = CreateShoulderInput(
                    roadInput,
                    shoulderInput,
                    index + 2);
                shoulders[index] = CreateSurface(
                    surfaceInput,
                    CreateIdentity(surfaceInput, 0, "road-shoulder"));
            }

            return new DenseCityRoadRecordGroup(road.Surface, road.Presentation, shoulders);
        }

        internal static DenseCitySurfaceBakeRecord CreateSurfaceOnlyRamp(
            DenseCityInfrastructureRecordInput input)
        {
            if (input.SurfaceKind != DenseCitySurfaceRecordKind.Ramp)
            {
                throw new ArgumentException(
                    "Only approach ramps may be emitted without a dedicated visual presentation.",
                    nameof(input));
            }

            return CreateSurface(input, CreateIdentity(input, 0, input.RecordKind));
        }

        internal static DenseCityBridgeRecordGroup CreateBridgeWithApproaches(
            DenseCityInfrastructureRecordInput bridgeInput,
            DenseCityBridgeApproachRecordInput firstApproach,
            DenseCityBridgeApproachRecordInput secondApproach)
        {
            if (bridgeInput.SurfaceKind != DenseCitySurfaceRecordKind.Bridge)
                throw new ArgumentException("Bridge surface input is required.", nameof(bridgeInput));
            if (bridgeInput.SequenceStart > int.MaxValue - 3)
                throw new ArgumentOutOfRangeException(nameof(bridgeInput));

            DenseCityInfrastructureRecordGroup bridge = CreateVisualized(bridgeInput);
            DenseCitySurfaceBakeRecord firstRamp = CreateSurfaceOnlyRamp(
                CreateApproachInput(bridgeInput, firstApproach, 2));
            DenseCitySurfaceBakeRecord secondRamp = CreateSurfaceOnlyRamp(
                CreateApproachInput(bridgeInput, secondApproach, 3));
            return new DenseCityBridgeRecordGroup(
                bridge.Surface,
                bridge.Presentation,
                firstRamp,
                secondRamp);
        }

        private static DenseCitySurfaceBakeRecord CreateSurface(
            DenseCityInfrastructureRecordInput input,
            DenseCityRecordIdentity identity) =>
            new(
                identity,
                input.SurfaceKind,
                CreateSurfacePolygon(input.WorldMatrix, input.SurfaceSize),
                input.Elevation,
                input.MovementMask,
                input.SurfaceLayer,
                input.Chunk);

        private static DenseCityRecordIdentity CreateIdentity(
            DenseCityInfrastructureRecordInput input,
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

        private static DenseCityInfrastructureRecordInput CreateApproachInput(
            DenseCityInfrastructureRecordInput bridgeInput,
            DenseCityBridgeApproachRecordInput approach,
            int sequenceOffset) =>
            new(
                bridgeInput.GeneratorSchema,
                bridgeInput.Seed,
                bridgeInput.DistrictId,
                bridgeInput.SequenceStart + sequenceOffset,
                approach.RecordKind,
                DenseCitySurfaceRecordKind.Ramp,
                bridgeInput.SourceAssetGuid,
                bridgeInput.SourceLocalId,
                bridgeInput.MaterialAssetGuids,
                approach.WorldMatrix,
                approach.SurfaceSize,
                approach.Elevation,
                bridgeInput.MovementMask,
                bridgeInput.SurfaceLayer,
                approach.Chunk,
                false,
                false,
                0);

        private static DenseCityInfrastructureRecordInput CreateShoulderInput(
            DenseCityInfrastructureRecordInput roadInput,
            DenseCityRoadShoulderRecordInput shoulder,
            int sequenceOffset) =>
            new(
                roadInput.GeneratorSchema,
                roadInput.Seed,
                roadInput.DistrictId,
                roadInput.SequenceStart + sequenceOffset,
                "road-shoulder",
                DenseCitySurfaceRecordKind.Terrain,
                roadInput.SourceAssetGuid,
                roadInput.SourceLocalId,
                roadInput.MaterialAssetGuids,
                shoulder.WorldMatrix,
                shoulder.SurfaceSize,
                shoulder.Elevation,
                roadInput.MovementMask,
                roadInput.SurfaceLayer,
                shoulder.Chunk,
                false,
                false,
                0);

        private static Vector2[] CreateSurfacePolygon(Matrix4x4 worldMatrix, Vector2 surfaceSize)
        {
            if (!float.IsFinite(surfaceSize.x) || !float.IsFinite(surfaceSize.y) ||
                surfaceSize.x <= 0f || surfaceSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(surfaceSize));
            }

            Vector3 center = worldMatrix.GetColumn(3);
            Vector3 right = worldMatrix.GetColumn(0);
            Vector3 forward = worldMatrix.GetColumn(2);
            right.y = 0f;
            forward.y = 0f;
            if (right.sqrMagnitude <= 0.000001f || forward.sqrMagnitude <= 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(worldMatrix));
            right.Normalize();
            forward.Normalize();
            right *= surfaceSize.x * 0.5f;
            forward *= surfaceSize.y * 0.5f;
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
