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

            metadata = new UiUnitCatalogMetadata(
                authoring.ConfiguredDisplayName,
                authoring.ConfiguredDescription,
                authoring.CanRequest && SkirmishCatalogPolicy.AllowsCurrentMatch(prefab, false),
                authoring.MaterialsCost,
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
                authoring.ConfiguredAttackDamage,
                authoring.ConfiguredAttackRange,
                authoring.ConfiguredSpeed,
                authoring.ConfiguredMaxHealth,
                authoring.Price);
            return true;
        }
    }
}
