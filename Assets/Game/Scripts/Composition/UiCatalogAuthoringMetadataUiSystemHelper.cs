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
        public static bool TryGetBuildingMetadata(GameObject prefab, out UiBuildingCatalogMetadata metadata)
        {
            metadata = default;
            if (prefab == null || !prefab.TryGetComponent(out BuildingDefinitionAuthoring authoring))
                return false;

            authoring.ApplyConfigIfAvailable();
            metadata = new UiBuildingCatalogMetadata(
                authoring.ConfiguredDisplayName,
                authoring.ConfiguredDescription,
                authoring.ConfiguredCanRequest,
                authoring.ConfiguredPrice,
                authoring.ConfiguredMaterialsCost,
                authoring.ConfiguredProductionDurationSeconds,
                authoring.ConfiguredFootprintCells,
                authoring.ConfiguredPortraitSprite,
                authoring.ConfiguredPortraitCardSprite,
                authoring.ConfiguredPortraitActionSprite,
                authoring.ConfiguredMaxHealth,
                authoring.ConfiguredIsWall,
                authoring.ConfiguredRole == BuildingRole.TentRefugee,
                authoring.ConfiguredThreatDetectionKind != ThreatDetectionKind.None,
                authoring.ConfiguredThreatDetectionKind == ThreatDetectionKind.Air,
                authoring.ConfiguredThreatDetectionRadiusCells,
                authoring.ConfiguredProductionCount);
            return true;
        }

    }
}
