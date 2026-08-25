using UnityEngine;

namespace Game.Runtime
{
    using ConfiguredUnitEntry = BuildingUiCommandSystemHelper.ConfiguredUnitEntry;

    internal sealed partial class BuildingDefinitionPrefabSystemHelper
    {
        public bool TryResolveConfiguredUnitResourceCosts(
            GameObject prefab,
            int fallbackMaterialsCost,
            out int creditsCost,
            out int materialsCost)
        {
            creditsCost = 0;
            materialsCost = Mathf.Max(0, fallbackMaterialsCost);
            if (!TryGetUnitDefinitionMetadata(prefab, out UnitDefinitionMetadata metadata))
                return false;

            creditsCost = Mathf.Max(0, metadata.CreditsCost);
            materialsCost = Mathf.Max(0, metadata.Price);
            return true;
        }

        private ConfiguredUnitEntry BuildConfiguredUnitEntry(GameObject prefab)
        {
            bool hasMetadata = TryGetUnitDefinitionMetadata(prefab, out UnitDefinitionMetadata metadata);
            string displayName = ResolveConfiguredUnitDisplayName(prefab, hasMetadata, metadata);
            string description = hasMetadata ? metadata.Description : string.Empty;
            Vector2Int footprint = hasMetadata ? metadata.FootprintCells : Vector2Int.one;
            bool isVehicle = footprint.x > 1 ||
                             footprint.y > 1 ||
                             (prefab != null && prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0);
            int price = hasMetadata ? metadata.Price : (isVehicle ? 15000 : 10000);
            return new ConfiguredUnitEntry(displayName, description, prefab, isVehicle, !hasMetadata || metadata.CanRequest, price);
        }

        private static string ResolveConfiguredUnitDisplayName(
            GameObject prefab,
            bool hasMetadata,
            UnitDefinitionMetadata metadata)
        {
            if (hasMetadata && !string.IsNullOrWhiteSpace(metadata.DisplayName))
                return metadata.DisplayName;

            return prefab != null ? prefab.name : "Unit";
        }
    }
}
