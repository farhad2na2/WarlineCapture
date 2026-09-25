using UnityEngine;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal static partial class UiCatalogAuthoringMetadataUiSystemHelper
    {
        public static bool TryGetUnitMetadata(GameObject prefab, out UiUnitCatalogMetadata metadata)
        {
            metadata = default;
            if (prefab == null || !prefab.TryGetComponent(out UnitGridAuthoring authoring))
                return false;

            Game.Skirmish.Contracts.SkirmishRoleOverlay overlay = default;
            bool hasOverlay = false;
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                using var catalog = em.CreateEntityQuery(typeof(SkirmishProductionCatalogRecord), typeof(SkirmishExpandedSessionComponent));
                if (catalog.CalculateEntityCount() == 1)
                {
                    var session = catalog.GetSingletonEntity();
                    if (em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase == Game.Skirmish.Contracts.SkirmishSessionPhase.Playing)
                        hasOverlay = em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog?.TryRole(prefab.name, out overlay) == true;
                }
            }

            metadata = new UiUnitCatalogMetadata(
                authoring.ConfiguredDisplayName,
                authoring.ConfiguredDescription,
                authoring.CanRequest && SkirmishCatalogPolicy.AllowsCurrentMatch(prefab, false),
                hasOverlay ? overlay.MaterialsCost : authoring.MaterialsCost,
                authoring.ProductionDurationSeconds,
                authoring.GetConfiguredFootprintCells(),
                authoring.PortraitSprite,
                authoring.PortraitCardSprite,
                authoring.PortraitActionSprite,
                authoring.IsAirUnit,
                authoring.IsProductionTransportUnit,
                authoring.SoldierTransportCapacity,
                authoring.ConfiguredAllowIdleWander,
                authoring.ConfiguredResourceHaulerBarrelCapacity,
                authoring.ConfiguredCanAttack,
                hasOverlay ? overlay.Damage : authoring.ConfiguredAttackDamage,
                hasOverlay ? overlay.RangeWorld : authoring.ConfiguredAttackRange,
                authoring.ConfiguredSpeed,
                hasOverlay ? overlay.MaxHealth : authoring.ConfiguredMaxHealth,
                hasOverlay ? 0 : authoring.Price);
            return true;
        }
    }
}
