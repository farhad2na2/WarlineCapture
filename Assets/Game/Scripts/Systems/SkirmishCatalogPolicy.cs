using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishCatalogPolicy
    {
        public static bool Allows(EntityManager em, GameObject prefab, bool building)
        {
            using var session = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (session.IsEmptyIgnoreFilter) return true;
            var preset = Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
            if (prefab == null || preset == null || preset.buildingPlacement == null) return false;
            var allowed = building ? preset.buildingPlacement.Spawnables :
                preset.buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs;
            foreach (var candidate in allowed)
                if (candidate == prefab) return true;
            return false;
        }

        public static bool AllowsCurrentMatch(GameObject prefab, bool building)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            return world == null || !world.IsCreated || Allows(world.EntityManager, prefab, building);
        }
    }
}
