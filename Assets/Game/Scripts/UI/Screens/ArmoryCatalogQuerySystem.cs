using System.Collections.Generic;
using UnityEngine;

public readonly struct ArmoryCatalogItem
{
    public readonly string DisplayName;
    public readonly Sprite Portrait;
    public readonly Sprite CardPortrait;
    public readonly Sprite InspectionPortrait;
    public readonly ArmoryCatalogCategory Category;

    public ArmoryCatalogItem(string displayName, Sprite portrait, ArmoryCatalogCategory category)
        : this(displayName, portrait, null, null, category)
    {
    }

    public ArmoryCatalogItem(
        string displayName,
        Sprite portrait,
        Sprite portraitCard,
        Sprite portraitAction,
        ArmoryCatalogCategory category)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Item" : displayName;
        Portrait = portrait;
        CardPortrait = portraitCard != null ? portraitCard : portrait;
        InspectionPortrait = portraitAction != null ? portraitAction : CardPortrait;
        Category = category;
    }
}

public sealed class ArmoryCatalogQuerySystem
{
    public void Collect(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        ArmoryCatalogCategory category,
        List<ArmoryCatalogItem> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (category == ArmoryCatalogCategory.Buildings)
        {
            CollectBuildingItems(buildingPlacementConfig, results);
            return;
        }

        CollectUnitItems(unitPrefabRegistryConfig, category, results);
    }

    private static void CollectUnitItems(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        ArmoryCatalogCategory category,
        List<ArmoryCatalogItem> results)
    {
        IReadOnlyList<GameObject> prefabs = unitPrefabRegistryConfig != null
            ? unitPrefabRegistryConfig.UnitSpawnPrefabs
            : null;
        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;

            UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
            bool isAir = authoring != null && authoring.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, authoring);
            if (!MatchesUnitCategory(category, isVehicle, isAir))
                continue;

            results.Add(new ArmoryCatalogItem(
                ResolveUnitDisplayName(prefab, authoring),
                authoring != null ? authoring.PortraitSprite : null,
                authoring != null ? authoring.PortraitCardSprite : null,
                authoring != null ? authoring.PortraitActionSprite : null,
                category));
        }

        results.Sort(CompareItems);
    }

    private static void CollectBuildingItems(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        List<ArmoryCatalogItem> results)
    {
        IReadOnlyList<GameObject> spawnables = buildingPlacementConfig != null
            ? buildingPlacementConfig.Spawnables
            : null;
        if (spawnables == null)
            return;

        for (int i = 0; i < spawnables.Count; i++)
        {
            GameObject prefab = spawnables[i];
            if (prefab == null)
                continue;

            BuildingDefinitionAuthoring authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
            if (authoring == null || !authoring.ConfiguredCanRequest)
                continue;

            results.Add(new ArmoryCatalogItem(
                string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName) ? prefab.name : authoring.ConfiguredDisplayName,
                authoring.ConfiguredPortraitSprite,
                authoring.ConfiguredPortraitCardSprite,
                authoring.ConfiguredPortraitActionSprite,
                ArmoryCatalogCategory.Buildings));
        }

        results.Sort(CompareItems);
    }

    private static bool IsVehicle(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null)
        {
            Vector2Int footprint = authoring.GetConfiguredFootprintCells();
            if (footprint.x > 1 || footprint.y > 1)
                return true;
        }

        return prefab != null && prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool MatchesUnitCategory(ArmoryCatalogCategory category, bool isVehicle, bool isAir)
    {
        return category switch
        {
            ArmoryCatalogCategory.Characters => !isVehicle && !isAir,
            ArmoryCatalogCategory.Vehicles => isVehicle && !isAir,
            ArmoryCatalogCategory.Aircrafts => isAir,
            _ => false
        };
    }

    private static string ResolveUnitDisplayName(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return authoring.ConfiguredDisplayName;

        return prefab != null ? prefab.name : "Unit";
    }

    private static int CompareItems(ArmoryCatalogItem left, ArmoryCatalogItem right)
    {
        return string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase);
    }
}
