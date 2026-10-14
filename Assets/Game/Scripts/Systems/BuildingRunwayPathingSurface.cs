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
            Vector3 right = rotation * Vector3.right, forward = rotation * Vector3.forward;
            surface = new StaticGridBlockerSurfaceOpeningComponent
            {
                Center = new float2(center.x, center.z),
                Right = new float2(right.x, right.z), Forward = new float2(forward.x, forward.z),
                HalfExtents = new float2(halfExtents.x, halfExtents.z)
            };
            return surface.IsValid;
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
