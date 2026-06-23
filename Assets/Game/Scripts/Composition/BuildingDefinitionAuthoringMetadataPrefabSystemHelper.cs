using UnityEngine;

internal static class BuildingDefinitionAuthoringMetadataPrefabSystemHelper
{
    public static bool TryGetBuildingDefinitionMetadata(
        GameObject prefab,
        out BuildingDefinitionSystem.BuildingDefinitionMetadata metadata)
    {
        metadata = default;
        if (prefab == null || !prefab.TryGetComponent(out BuildingDefinitionAuthoring authoring))
            return false;

        authoring.ApplyConfigIfAvailable();
        int productionCount = Mathf.Max(0, authoring.ConfiguredProductionCount);
        GameObject[] productionPrefabs = productionCount > 0 ? new GameObject[productionCount] : null;
        for (int i = 0; i < productionCount; i++)
            productionPrefabs[i] = authoring.GetProductionOrDefault(i)?.spawnUnitPrefab;

        metadata = new BuildingDefinitionSystem.BuildingDefinitionMetadata
        {
            DisplayName = authoring.ConfiguredDisplayName,
            Description = authoring.ConfiguredDescription,
            MaxHealth = authoring.ConfiguredMaxHealth,
            DestroyedVisualPrefab = authoring.ConfiguredDestroyedVisualPrefab,
            FootprintCells = authoring.ConfiguredFootprintCells,
            Role = authoring.ConfiguredRole,
            IsWall = authoring.ConfiguredIsWall,
            CanRequest = authoring.ConfiguredCanRequest,
            Price = authoring.ConfiguredPrice,
            ProductionDurationSeconds = authoring.ConfiguredProductionDurationSeconds,
            OilBarrelsPerDay = authoring.ConfiguredOilBarrelsPerDay,
            OilStorageCapacity = authoring.ConfiguredOilStorageCapacity,
            FuelBarrelsPerDay = authoring.ConfiguredFuelBarrelsPerDay,
            FuelStorageCapacity = authoring.ConfiguredFuelStorageCapacity,
            RefugeeCapacity = authoring.ConfiguredRefugeeCapacity,
            RefugeeUpkeepPerCitizenPerDay = authoring.ConfiguredRefugeeUpkeepPerCitizenPerDay,
            ThreatDetectionKind = authoring.ConfiguredThreatDetectionKind,
            ThreatDetectionRadiusCells = authoring.ConfiguredThreatDetectionRadiusCells,
            ProductionSpawnUnitPrefabs = productionPrefabs
        };
        return true;
    }

    public static bool TryGetUnitDefinitionMetadata(
        GameObject prefab,
        out BuildingDefinitionSystem.UnitDefinitionMetadata metadata)
    {
        metadata = default;
        if (prefab == null || !prefab.TryGetComponent(out UnitGridAuthoring authoring))
            return false;

        metadata = new BuildingDefinitionSystem.UnitDefinitionMetadata
        {
            DisplayName = authoring.ConfiguredDisplayName,
            Description = authoring.ConfiguredDescription,
            FootprintCells = authoring.GetConfiguredFootprintCells(),
            CanRequest = authoring.CanRequest,
            Price = authoring.Price
        };
        return true;
    }
}
