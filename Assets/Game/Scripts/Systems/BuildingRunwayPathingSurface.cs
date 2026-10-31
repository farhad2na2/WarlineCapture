using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal static class BuildingRunwayPathingSurface
    {
        public static bool TryResolve(RuntimeBuildingEntity building, out StaticGridBlockerSurfaceOpeningComponent surface)
        {
            surface = default;
            if (building?.Definition == null || !building.Definition.HasRunway ||
                !BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(building.Definition,
                    BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Airport")) ||
                !BuildingRunwaySystem.TryResolveRuntimeRunwayWorldData(building, out Vector3 center,
                    out Quaternion rotation, out Vector3 halfExtents)) return false;
            return StaticGridBlockerSurfaceOpeningComponent.TryCreateFromWorldRunway(
                new float3(center.x, center.y, center.z),
                new quaternion(rotation.x, rotation.y, rotation.z, rotation.w),
                new float3(halfExtents.x, halfExtents.y, halfExtents.z), out surface);
        }

        public static void Attach(EntityManager em, RuntimeBuildingEntity building)
        {
            if (building == null || building.BlockerEntity == Entity.Null || !em.Exists(building.BlockerEntity) ||
                !TryResolve(building, out var surface)) return;
            if (em.HasComponent<StaticGridBlockerSurfaceOpeningComponent>(building.BlockerEntity))
                em.SetComponentData(building.BlockerEntity, surface);
            else em.AddComponentData(building.BlockerEntity, surface);
        }
    }
}
