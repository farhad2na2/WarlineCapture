using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Rendering.Contracts
{
    public struct RenderSurfaceFootprint
    {
        public Matrix4x4 WorldToLocal;
        public Bounds LocalBounds, WorldBounds;
    }
    // Geometry queries expose values, never renderer implementation components or persistent world state.
    public interface IRenderBoundsPresentation
    {
        bool TryReadWorldBounds(EntityManager manager, Entity entity, out Bounds bounds);
        void CollectStaticBounds(EntityManager manager, Rect horizontalArea, float minExtent, float maxExtent,
            ref NativeList<Bounds> results);
        void CollectWaterFootprints(EntityManager manager, ref NativeList<RenderSurfaceFootprint> results);
    }

    public static class RenderBoundsGateway
    {
        public static IRenderBoundsPresentation Provider { get; private set; }
        public static void Bind(IRenderBoundsPresentation provider) => Provider = provider;
    }
}
