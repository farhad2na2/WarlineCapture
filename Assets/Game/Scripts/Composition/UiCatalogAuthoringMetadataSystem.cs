using UnityEngine;

internal static class UiCatalogAuthoringMetadataSystem
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

    public static bool TryGetUnitMetadata(GameObject prefab, out UiUnitCatalogMetadata metadata)
    {
        metadata = default;
        if (prefab == null || !prefab.TryGetComponent(out UnitGridAuthoring authoring))
            return false;

        metadata = new UiUnitCatalogMetadata(
            authoring.ConfiguredDisplayName,
            authoring.ConfiguredDescription,
            authoring.CanRequest,
            authoring.Price,
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
            authoring.ConfiguredMaxHealth);
        return true;
    }
}
