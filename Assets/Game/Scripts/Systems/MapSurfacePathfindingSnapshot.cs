using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct MapSurfacePathfindingSnapshot
    {
        private readonly MapSurfacePathCost _pathCostSystem;

        public readonly struct Context
        {
            public readonly MapSurfaceComponent Surface;
            public readonly MapSurfacePathCostComponent PathCost;
            public readonly byte HasSurfaceData;

            public Context(MapSurfaceComponent surface, MapSurfacePathCostComponent pathCost, bool hasSurfaceData)
            {
                Surface = surface;
                PathCost = pathCost;
                HasSurfaceData = (byte)(hasSurfaceData ? 1 : 0);
            }
        }

        public bool TryCreateContext(EntityManager entityManager, EntityQuery surfaceQuery, out Context context)
        {
            context = default;
            if (surfaceQuery.IsEmptyIgnoreFilter)
                return false;

            Entity surfaceEntity = surfaceQuery.GetSingletonEntity();
            MapSurfaceComponent surface = entityManager.GetComponentData<MapSurfaceComponent>(surfaceEntity);
            MapSurfacePathCostComponent pathCost = entityManager.HasComponent<MapSurfacePathCostComponent>(surfaceEntity)
                ? entityManager.GetComponentData<MapSurfacePathCostComponent>(surfaceEntity)
                : _pathCostSystem.CreateDisabledDefault();
            bool hasSurfaceData = surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
            context = new Context(surface, pathCost, hasSurfaceData);
            return hasSurfaceData;
        }

        public Context CreateFlatFallbackContext()
        {
            return new Context(default, _pathCostSystem.CreateDisabledDefault(), false);
        }
    }
}
