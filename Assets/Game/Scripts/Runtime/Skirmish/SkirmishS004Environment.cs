using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishS004Environment
    {
        public const string ResourceName = "SkirmishS004TownPlacements";
        public static bool IsActive(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            if (query.CalculateEntityCount() != 1) return false;
            var session = query.GetSingletonEntity();
            return em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy == 0 &&
                em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup?.CatalogId == "S004";
        }
        public static OperationMapDefinition LoadMapForSession(EntityManager em) =>
            IsActive(em) ? Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap") : null;
        // S004 reuses the campaign scenery without an additional procedural town.
        public static MapBuildingPlacementConfig LoadForSession(EntityManager em) => null;
    }
}
