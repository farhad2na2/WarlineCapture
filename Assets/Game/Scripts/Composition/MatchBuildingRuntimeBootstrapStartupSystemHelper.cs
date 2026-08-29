using Unity.Entities;
using Game.Components;
using Game.Runtime;

namespace Game.Composition
{
    internal static class MatchBuildingRuntimeBootstrapStartupSystemHelper
    {
        public static Entity Ensure(Entity current)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return current;

            EntityManager em = world.EntityManager;
            Entity boundary = current;
            if (boundary == Entity.Null || !em.Exists(boundary))
            {
                using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
                if (!query.IsEmptyIgnoreFilter)
                    boundary = query.GetSingletonEntity();
                else
                {
                    boundary = em.CreateEntity();
                    em.SetName(boundary, "BuildingRuntimeStateEntity");
                }
            }

            EnsureState(em, boundary);
            return boundary;
        }

        private static void EnsureState(EntityManager em, Entity e)
        {
            if (!em.HasComponent<BuildingRuntimeStateTag>(e)) em.AddComponent<BuildingRuntimeStateTag>(e);
            if (!em.HasComponent<BuildingProductionDeliveryReadModel>(e)) em.AddComponent<BuildingProductionDeliveryReadModel>(e);
            else em.SetComponentData(e, default(BuildingProductionDeliveryReadModel));
            Ensure<BuildingConfiguredSpawnableReadModel>(em, e);
            Ensure<BuildingConfiguredUnitReadModel>(em, e);
            Ensure<BuildingProductionSlotReadModel>(em, e);
            Ensure<BuildingProductionSpawnRequest>(em, e);
            Ensure<BuildingRecentSpawnReservation>(em, e);
            Ensure<BuildingProducedUnitReadModel>(em, e);
            Ensure<MapVehiclePlacementReadModel>(em, e);
            Ensure<BuildingRuntimeFactionSummary>(em, e);
            Ensure<BuildingRuntimeFactionUsableFuelSummary>(em, e);
            Ensure<BuildingRuntimeOwnedBuildingSummary>(em, e);
            Ensure<BuildingRuntimeUnitProductionSummary>(em, e);
            Ensure<BuildingFactionProductionSpawnPointReadModel>(em, e);
            Ensure<BuildingFactionRunwayReadModel>(em, e);
            Ensure<BuildingFactionUnitProductionRequest>(em, e);
            Ensure<BuildingFactionResourceSellRequest>(em, e);
            Ensure<BuildingRuntimeSpawnRequest>(em, e);
            Ensure<BuildingRuntimeDeleteRequest>(em, e);
            Ensure<BuildingRuntimeSurfaceOverlay>(em, e);
        }
        private static void Ensure<T>(EntityManager em, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            if (!em.HasBuffer<T>(entity))
                em.AddBuffer<T>(entity);
        }
    }
}
