using Game.Components;
using Game.Configs;
using Unity.Entities.Graphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Authoring
{
    public readonly struct OperationMapRenderProxySlotBakeDescriptor
    {
        public int SlotIndex { get; }
        public int PoolBucketIndex { get; }
        public RenderFilterSettings FilterSettings { get; }

        public OperationMapRenderProxySlotBakeDescriptor(
            int slotIndex,
            int poolBucketIndex,
            RenderFilterSettings filterSettings)
        {
            SlotIndex = slotIndex;
            PoolBucketIndex = poolBucketIndex;
            FilterSettings = filterSettings;
        }
    }

    public static class OperationMapRenderProxySlotBuilder
    {
        public static bool TryBuild(
            OperationMapRenderDatabaseBakeConfig databaseConfig,
            out OperationMapRenderProxySlotBakeDescriptor[] descriptors,
            out string error)
        {
            descriptors = null;
            if (databaseConfig == null)
            {
                error = "Render-proxy slot baking requires a generated database config.";
                return false;
            }

            if (!databaseConfig.TryValidateSchema(out error))
                return false;

            int totalCapacity = 0;
            for (int bucketIndex = 0;
                 bucketIndex < databaseConfig.PoolBuckets.Count;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketConfigRecord bucket =
                    databaseConfig.PoolBuckets[bucketIndex];
                if (bucket.FirstSlot != totalCapacity ||
                    bucket.Capacity <= 0 ||
                    bucket.Capacity > 24000 - totalCapacity)
                {
                    error =
                        $"poolBuckets[{bucketIndex}] cannot produce a bounded contiguous " +
                        "render-proxy slot range.";
                    return false;
                }

                totalCapacity += bucket.Capacity;
            }

            descriptors = new OperationMapRenderProxySlotBakeDescriptor[totalCapacity];
            for (int bucketIndex = 0;
                 bucketIndex < databaseConfig.PoolBuckets.Count;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketConfigRecord bucket =
                    databaseConfig.PoolBuckets[bucketIndex];
                if (!TryCreateFilterSettings(bucket, out RenderFilterSettings filter, out error))
                {
                    descriptors = null;
                    return false;
                }

                int endSlot = bucket.FirstSlot + bucket.Capacity;
                for (int slotIndex = bucket.FirstSlot; slotIndex < endSlot; slotIndex++)
                {
                    descriptors[slotIndex] =
                        new OperationMapRenderProxySlotBakeDescriptor(
                            slotIndex,
                            bucketIndex,
                            filter);
                }
            }

            error = null;
            return true;
        }

        private static bool TryCreateFilterSettings(
            OperationMapRenderPoolBucketConfigRecord bucket,
            out RenderFilterSettings filter,
            out string error)
        {
            filter = RenderFilterSettings.Default;
            if (!TryConvertMotionMode(bucket.MotionVectorMode, out MotionVectorGenerationMode motion))
            {
                error = $"Unknown render-proxy motion-vector mode: {bucket.MotionVectorMode}.";
                return false;
            }

            OperationMapRenderShadowFlags shadowFlags = bucket.ShadowFlags;
            filter.Layer = bucket.Layer;
            filter.RenderingLayerMask = bucket.RenderingLayerMask;
            filter.MotionMode = motion;
            filter.ShadowCastingMode =
                (shadowFlags & OperationMapRenderShadowFlags.CastShadows) != 0
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;
            filter.ReceiveShadows =
                (shadowFlags & OperationMapRenderShadowFlags.ReceiveShadows) != 0;
            filter.StaticShadowCaster =
                (shadowFlags & OperationMapRenderShadowFlags.StaticShadowCaster) != 0;
            filter.ForceMeshLod = -1;
            filter.MeshLodSelectionBias = 0f;
            error = null;
            return true;
        }

        private static bool TryConvertMotionMode(
            OperationMapRenderMotionVectorMode source,
            out MotionVectorGenerationMode target)
        {
            switch (source)
            {
                case OperationMapRenderMotionVectorMode.Camera:
                    target = MotionVectorGenerationMode.Camera;
                    return true;
                case OperationMapRenderMotionVectorMode.Object:
                    target = MotionVectorGenerationMode.Object;
                    return true;
                case OperationMapRenderMotionVectorMode.ForceNoMotion:
                    target = MotionVectorGenerationMode.ForceNoMotion;
                    return true;
                default:
                    target = default;
                    return false;
            }
        }
    }
}
