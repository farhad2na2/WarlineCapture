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

        private static void EnsureBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<BuildingRuntimeStateTag>(entity))
                em.AddComponent<BuildingRuntimeStateTag>(entity);
            EnsureBuffer<BuildingConfiguredSpawnableReadModel>(em, entity);
            EnsureBuffer<BuildingConfiguredUnitReadModel>(em, entity);
            EnsureBuffer<BuildingProductionSlotReadModel>(em, entity);
            EnsureBuffer<BuildingProductionSpawnRequest>(em, entity);
            EnsureBuffer<BuildingRecentSpawnReservation>(em, entity);
            EnsureBuffer<BuildingProducedUnitReadModel>(em, entity);
            EnsureBuffer<MapVehiclePlacementReadModel>(em, entity);
            EnsureBuffer<BuildingRuntimeFactionSummary>(em, entity);
            EnsureBuffer<BuildingRuntimeFactionUsableFuelSummary>(em, entity);
            EnsureBuffer<BuildingRuntimeOwnedBuildingSummary>(em, entity);
            EnsureBuffer<BuildingRuntimeUnitProductionSummary>(em, entity);
            EnsureBuffer<BuildingFactionProductionSpawnPointReadModel>(em, entity);
            EnsureBuffer<BuildingFactionRunwayReadModel>(em, entity);
            EnsureBuffer<BuildingFactionUnitProductionRequest>(em, entity);
            EnsureBuffer<BuildingFactionResourceSellRequest>(em, entity);
            EnsureBuffer<BuildingRuntimeSpawnRequest>(em, entity);
            EnsureBuffer<BuildingRuntimeSurfaceOverlay>(em, entity);
        }

        private static void EnsureBuffer<T>(EntityManager em, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            if (!em.HasBuffer<T>(entity))
                em.AddBuffer<T>(entity);
        }
    }
}
