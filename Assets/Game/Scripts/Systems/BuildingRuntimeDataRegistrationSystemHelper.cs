using System;
using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    // Shared ECS/data adapter. Presentation and instance ownership remain with its caller.
    internal static class BuildingRuntimeDataRegistrationSystemHelper
    {
        internal static RuntimeBuildingEntity InitializeAndRegister(
            RuntimeBuildingCollection<RuntimeBuildingEntity> registry,
            RuntimeBuildingEntity building,
            bool defer,
            Action<RuntimeBuildingEntity> initializeVisuals,
            Action<RuntimeBuildingEntity> attachPathingAndRuntimeLink,
            Action<RuntimeBuildingEntity> publishRuntimeLink,
            Action markPendingMarkerRefresh,
            Action refreshMarkers)
        {
            building.ProductionSpawnLocalPositions = building.Definition.ProductionSpawnLocalPositions;
            building.ProducedUnits = new();
            building.PendingProductions = new();
            building.StoredOilBarrels = 0f;
            building.StoredFuelBarrels = 0f;
            if (building.ProductionSpawnLocalPositions != null && building.ProductionSpawnLocalPositions.Length > 0)
                building.ProducedUnitSlots = new Entity[building.ProductionSpawnLocalPositions.Length];

            initializeVisuals?.Invoke(building);
            attachPathingAndRuntimeLink?.Invoke(building);
            registry.AddBuilding(building.Id, building);
            publishRuntimeLink?.Invoke(building);
            if (defer)
                markPendingMarkerRefresh?.Invoke();
            else
                refreshMarkers?.Invoke();
            return building;
        }

        internal static void ApplyCombatEntityFoundation(
            EntityManager em,
            Entity entity,
            BuildingSurfacePlacementUtilitySystemHelper.Result surfaceResult,
            BuildingSurfacePlacementUtilitySystemHelper surfacePlacementSystem)
        {
            if (entity == Entity.Null || !em.Exists(entity) || surfacePlacementSystem == null)
                return;
            if (em.HasComponent<LocalTransform>(entity))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
                transform.Position.y = surfaceResult.FoundationHeight;
                em.SetComponentData(entity, transform);
            }
            BuildingSurfaceComponent surfaceComponent = surfacePlacementSystem.ToComponent(surfaceResult);
            if (em.HasComponent<BuildingSurfaceComponent>(entity))
                em.SetComponentData(entity, surfaceComponent);
            else
                em.AddComponentData(entity, surfaceComponent);
        }
    }
}
