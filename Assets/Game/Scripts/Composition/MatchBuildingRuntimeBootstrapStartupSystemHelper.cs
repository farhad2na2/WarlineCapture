using Unity.Entities;
using Game.Components;
using Game.Runtime;

namespace Game.Composition
{
    internal static class MatchBuildingRuntimeBootstrapStartupSystemHelper
    {
        public static Entity Ensure(Entity currentEntity)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return currentEntity;

            EntityManager em = world.EntityManager;
            Entity boundaryEntity = currentEntity;
            if (boundaryEntity == Entity.Null || !em.Exists(boundaryEntity))
            {
                using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
                if (!query.IsEmptyIgnoreFilter)
                {
                    boundaryEntity = query.GetSingletonEntity();
                }
                else
                {
                    boundaryEntity = em.CreateEntity();
                    em.SetName(boundaryEntity, "BuildingRuntimeStateEntity");
                }
            }

            EnsureBuffers(em, boundaryEntity);
            return boundaryEntity;
        }

        private static void EnsureBuffers(EntityManager em, Entity e)
        {
            if (!em.HasComponent<BuildingRuntimeStateTag>(e))
                em.AddComponent<BuildingRuntimeStateTag>(e);
            EnsureBuffer<BuildingConfiguredSpawnableReadModel>(em, e);
            EnsureBuffer<BuildingConfiguredUnitReadModel>(em, e);
            EnsureBuffer<BuildingProductionSlotReadModel>(em, e);
            EnsureBuffer<BuildingProductionSpawnRequest>(em, e);
            EnsureBuffer<BuildingRecentSpawnReservation>(em, e);
            EnsureBuffer<BuildingProducedUnitReadModel>(em, e);
            EnsureBuffer<MapVehiclePlacementReadModel>(em, e);
            EnsureBuffer<BuildingRuntimeFactionSummary>(em, e);
            EnsureBuffer<BuildingRuntimeFactionUsableFuelSummary>(em, e);
            EnsureBuffer<BuildingRuntimeOwnedBuildingSummary>(em, e);
            EnsureBuffer<BuildingRuntimeUnitProductionSummary>(em, e);
            EnsureBuffer<BuildingFactionProductionSpawnPointReadModel>(em, e);
            EnsureBuffer<BuildingFactionRunwayReadModel>(em, e);
            EnsureBuffer<BuildingFactionUnitProductionRequest>(em, e);
            EnsureBuffer<BuildingFactionResourceSellRequest>(em, e);
            EnsureBuffer<BuildingRuntimeSpawnRequest>(em, e);
            EnsureBuffer<BuildingRuntimeDeleteRequest>(em, e);
            EnsureBuffer<BuildingRuntimeSurfaceOverlay>(em, e);
        }
        private static void EnsureBuffer<T>(EntityManager em, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            if (!em.HasBuffer<T>(entity))
                em.AddBuffer<T>(entity);
        }
    }
}
