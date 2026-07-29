using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Authoring
{
    internal static class OperationMapRenderDatabaseBlobBuilder
    {
        internal static bool TryBuild(
            OperationMapRenderDatabaseBakeConfig config,
            out BlobAssetReference<OperationMapRenderDatabaseBlob> blob,
            out string error)
        {
            blob = default;
            if (config == null)
            {
                error = "A render database config is required.";
                return false;
            }
            if (!config.TryValidateSchema(out error))
                return false;

            using var builder = new BlobBuilder(Allocator.Temp);
            ref OperationMapRenderDatabaseBlob root =
                ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
            root.OperationMapId = new FixedString64Bytes(config.OperationMapId);
            root.ContentHash = new FixedString128Bytes(config.ContentHash);
            root.SchemaVersion = config.SchemaVersion;
            root.CellSize = config.CellSize;
            root.GridOrigin = ToFloat3(config.GridOrigin);
            root.GridDimensions =
                new int2(config.GridDimensions.x, config.GridDimensions.y);

            BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
                builder.Allocate(ref root.Prototypes, config.Prototypes.Count);
            for (int index = 0; index < prototypes.Length; index++)
            {
                OperationMapRenderPrototypeConfigRecord source =
                    config.Prototypes[index];
                prototypes[index] = new OperationMapRenderPrototypeBlob
                {
                    ContentIdentity = new OperationMapRenderIdentity128
                    {
                        Low = source.ContentIdentityLow,
                        High = source.ContentIdentityHigh
                    },
                    FirstPart = source.FirstPart,
                    PartCount = source.PartCount,
                    CombinedLocalBounds = ToBounds(source.CombinedLocalBounds),
                    SemanticCategory = source.SemanticCategory,
                    EligibilityFlags = source.EligibilityFlags
                };
            }

            BlobBuilderArray<OperationMapRenderPrototypePartBlob> parts =
                builder.Allocate(ref root.Parts, config.Parts.Count);
            for (int index = 0; index < parts.Length; index++)
            {
                OperationMapRenderPrototypePartConfigRecord source =
                    config.Parts[index];
                parts[index] = new OperationMapRenderPrototypePartBlob
                {
                    RendererPathHash = new OperationMapRenderIdentity128
                    {
                        Low = source.RendererPathIdentityLow,
                        High = source.RendererPathIdentityHigh
                    },
                    MeshArrayIndex = source.MeshIndex,
                    MaterialArrayIndex = source.MaterialIndex,
                    SubMeshIndex = source.SubMeshIndex,
                    LocalToPlacement = ToFloat4x4(source.LocalToPlacement),
                    LocalBounds = ToBounds(source.LocalBounds),
                    LinearBaseColor = new float4(
                        source.LinearBaseColor.r,
                        source.LinearBaseColor.g,
                        source.LinearBaseColor.b,
                        source.LinearBaseColor.a),
                    PolicyBucket = source.PolicyBucket,
                    PoolBucketIndex = source.PoolBucketIndex,
                    LodFlags = source.LodFlags,
                    ShadowFlags = source.ShadowFlags
                };
            }

            BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
                builder.Allocate(ref root.Placements, config.Placements.Count);
            for (int index = 0; index < placements.Length; index++)
            {
                OperationMapRenderPlacementConfigRecord source =
                    config.Placements[index];
                placements[index] = new OperationMapRenderPlacementBlob
                {
                    StableIdentityHash = new OperationMapRenderIdentity128
                    {
                        Low = source.StableIdentityLow,
                        High = source.StableIdentityHigh
                    },
                    PrototypeIndex = source.PrototypeIndex,
                    WorldMatrix = ToFloat4x4(source.WorldMatrix),
                    CellIndex = source.CellIndex,
                    StateOwnerIndex = source.StateOwnerIndex,
                    RequiredVisualState = source.RequiredVisualState,
                    Priority = source.Priority,
                    SemanticCategory = source.SemanticCategory
                };
            }

            BlobBuilderArray<OperationMapRenderCellBlob> cells =
                builder.Allocate(ref root.Cells, config.Cells.Count);
            for (int index = 0; index < cells.Length; index++)
            {
                OperationMapRenderCellConfigRecord source = config.Cells[index];
                cells[index] = new OperationMapRenderCellBlob
                {
                    Coordinate = new int2(
                        source.Coordinate.x,
                        source.Coordinate.y),
                    WorldBounds = ToBounds(source.WorldBounds),
                    FirstPlacementIndex = source.FirstPlacementIndex,
                    PlacementIndexCount = source.PlacementIndexCount
                };
            }

            BlobBuilderArray<int> cellPlacementIndices =
                builder.Allocate(
                    ref root.CellPlacementIndices,
                    config.CellPlacementIndices.Count);
            for (int index = 0; index < cellPlacementIndices.Length; index++)
                cellPlacementIndices[index] = config.CellPlacementIndices[index];

            BlobBuilderArray<OperationMapRenderPoolBucketBlob> poolBuckets =
                builder.Allocate(ref root.PoolBuckets, config.PoolBuckets.Count);
            for (int index = 0; index < poolBuckets.Length; index++)
            {
                OperationMapRenderPoolBucketConfigRecord source =
                    config.PoolBuckets[index];
                poolBuckets[index] = new OperationMapRenderPoolBucketBlob
                {
                    PolicyBucket = source.PolicyBucket,
                    Layer = source.Layer,
                    RenderingLayerMask = source.RenderingLayerMask,
                    MotionVectorMode = source.MotionVectorMode,
                    ShadowFlags = source.ShadowFlags,
                    FirstSlot = source.FirstSlot,
                    Capacity = source.Capacity,
                    PeakRequiredCount = source.PeakRequiredCount,
                    HeadroomCount = source.HeadroomCount,
                    ReportIdentity = new OperationMapRenderIdentity128
                    {
                        Low = source.ReportIdentityLow,
                        High = source.ReportIdentityHigh
                    }
                };
            }

            blob = builder.CreateBlobAssetReference<
                OperationMapRenderDatabaseBlob>(Allocator.Persistent);
            error = null;
            return true;
        }

        private static OperationMapRenderBoundsBlob ToBounds(Bounds source) =>
            new()
            {
                Center = ToFloat3(source.center),
                Extents = ToFloat3(source.extents)
            };

        private static float3 ToFloat3(Vector3 source) =>
            new(source.x, source.y, source.z);

        private static float4x4 ToFloat4x4(Matrix4x4 source) =>
            new(
                new float4(source.m00, source.m10, source.m20, source.m30),
                new float4(source.m01, source.m11, source.m21, source.m31),
                new float4(source.m02, source.m12, source.m22, source.m32),
                new float4(source.m03, source.m13, source.m23, source.m33));
    }
}
