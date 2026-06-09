using System.Collections.Generic;
using UnityEngine;

public readonly struct BuildDrawerCatalogItem
{
    public readonly BuildDrawerCategory Category;
    public readonly GameObject Prefab;
    public readonly string DisplayName;
    public readonly string TypeLabel;
    public readonly string Description;
    public readonly int Price;
    public readonly float ProductionDurationSeconds;
    public readonly Vector2Int FootprintCells;
    public readonly Sprite Portrait;
    public readonly Sprite CardPortrait;
    public readonly Sprite ActionPortrait;

    public BuildDrawerCatalogItem(
        BuildDrawerCategory category,
        GameObject prefab,
        string displayName,
        string typeLabel,
        string description,
        int price,
        float productionDurationSeconds,
        Vector2Int footprintCells,
        Sprite portrait,
        Sprite cardPortrait,
        Sprite actionPortrait)
    {
        Category = category;
        Prefab = prefab;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Item" : displayName;
        TypeLabel = string.IsNullOrWhiteSpace(typeLabel) ? BuildDrawerCategoryFormatter.Format(category) : typeLabel;
        Description = description ?? string.Empty;
        Price = Mathf.Max(0, price);
        ProductionDurationSeconds = Mathf.Max(0f, productionDurationSeconds);
        FootprintCells = new Vector2Int(Mathf.Max(1, footprintCells.x), Mathf.Max(1, footprintCells.y));
        Portrait = portrait;
        CardPortrait = cardPortrait != null ? cardPortrait : portrait;
        ActionPortrait = actionPortrait != null ? actionPortrait : CardPortrait;
    }

    public string ActionLabel => BuildDrawerCategoryFormatter.FormatActionLabel(Category);
}

public static class BuildDrawerCategoryFormatter
{
    public static string Format(BuildDrawerCategory category)
    {
        return category switch
        {
            BuildDrawerCategory.Buildings => "BUILDINGS",
            BuildDrawerCategory.Vehicles => "VEHICLES",
            BuildDrawerCategory.Aircrafts => "AIRCRAFTS",
            BuildDrawerCategory.Soldiers => "SOLDIERS",
            _ => "ITEMS"
        };
    }

    public static string FormatActionLabel(BuildDrawerCategory category)
    {
        return category switch
        {
            BuildDrawerCategory.Buildings => "PLACE",
            BuildDrawerCategory.Vehicles => "PRODUCE",
            BuildDrawerCategory.Aircrafts => "PRODUCE",
            BuildDrawerCategory.Soldiers => "RECRUIT",
            _ => "SELECT"
        };
    }
}

public sealed class BuildDrawerCatalogQuerySystem
{
    public void Collect(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        BuildDrawerCategory category,
        List<BuildDrawerCatalogItem> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (category == BuildDrawerCategory.Buildings)
            CollectBuildings(buildingPlacementConfig, results);
        else
            CollectUnits(unitPrefabRegistryConfig, category, results);

        results.Sort(CompareItems);
    }

    public void CollectAll(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        List<BuildDrawerCatalogItem> results)
    {
        if (results == null)
            return;

        results.Clear();
        AppendBuildings(buildingPlacementConfig, results);
        AppendUnits(unitPrefabRegistryConfig, results);
        results.Sort(CompareItems);
    }

    public bool TryResolvePrefab(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        GameObject prefab,
        out BuildDrawerCatalogItem item)
    {
        item = default;
        if (prefab == null)
            return false;

        BuildingDefinitionAuthoring buildingAuthoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
        if (buildingAuthoring != null && buildingAuthoring.ConfiguredCanRequest)
        {
            item = BuildBuildingItem(prefab, buildingAuthoring);
            return true;
        }

        UnitGridAuthoring unitAuthoring = prefab.GetComponent<UnitGridAuthoring>();
        if (unitAuthoring != null && unitAuthoring.CanRequest)
        {
            bool isAir = unitAuthoring.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, unitAuthoring);
            item = BuildUnitItem(prefab, unitAuthoring, ResolveUnitCategory(isAir, isVehicle), isVehicle, isAir);
            return true;
        }

        return TryResolveFromConfiguredLists(unitPrefabRegistryConfig, buildingPlacementConfig, prefab, out item);
    }

    private static void CollectBuildings(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        List<BuildDrawerCatalogItem> results)
    {
        AppendBuildings(buildingPlacementConfig, results);
    }

    private static void AppendBuildings(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        List<BuildDrawerCatalogItem> results)
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

            results.Add(BuildBuildingItem(prefab, authoring));
        }
    }

    private static BuildDrawerCatalogItem BuildBuildingItem(GameObject prefab, BuildingDefinitionAuthoring authoring)
    {
        return new BuildDrawerCatalogItem(
            BuildDrawerCategory.Buildings,
            prefab,
            string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName) ? prefab.name : authoring.ConfiguredDisplayName,
            ResolveBuildingTypeLabel(authoring),
            ResolveBuildingDescription(authoring),
            authoring.ConfiguredPrice,
            0f,
            authoring.ConfiguredFootprintCells,
            authoring.ConfiguredPortraitSprite,
            authoring.ConfiguredPortraitCardSprite,
            authoring.ConfiguredPortraitActionSprite);
    }

    private static void CollectUnits(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        BuildDrawerCategory category,
        List<BuildDrawerCatalogItem> results)
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
            if (authoring == null || !authoring.CanRequest)
                continue;

            bool isAir = authoring.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, authoring);
            BuildDrawerCategory resolvedCategory = ResolveUnitCategory(isAir, isVehicle);
            if (resolvedCategory != category)
                continue;

            results.Add(BuildUnitItem(prefab, authoring, resolvedCategory, isVehicle, isAir));
        }
    }

    private static void AppendUnits(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        List<BuildDrawerCatalogItem> results)
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
            if (authoring == null || !authoring.CanRequest)
                continue;

            bool isAir = authoring.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, authoring);
            BuildDrawerCategory category = ResolveUnitCategory(isAir, isVehicle);
            results.Add(BuildUnitItem(prefab, authoring, category, isVehicle, isAir));
        }
    }

    private static BuildDrawerCatalogItem BuildUnitItem(
        GameObject prefab,
        UnitGridAuthoring authoring,
        BuildDrawerCategory category,
        bool isVehicle,
        bool isAir)
    {
        return new BuildDrawerCatalogItem(
            category,
            prefab,
            ResolveUnitDisplayName(prefab, authoring),
            ResolveUnitTypeLabel(prefab, authoring, isVehicle, isAir),
            ResolveUnitDescription(prefab, authoring),
            authoring.Price,
            authoring.ProductionDurationSeconds,
            authoring.GetConfiguredFootprintCells(),
            authoring.PortraitSprite,
            authoring.PortraitCardSprite,
            authoring.PortraitActionSprite);
    }

    private static BuildDrawerCategory ResolveUnitCategory(bool isAir, bool isVehicle)
    {
        if (isAir)
            return BuildDrawerCategory.Aircrafts;

        return isVehicle ? BuildDrawerCategory.Vehicles : BuildDrawerCategory.Soldiers;
    }

    private static bool IsVehicle(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null)
        {
            Vector2Int footprint = authoring.GetConfiguredFootprintCells();
            if (footprint.x > 1 || footprint.y > 1 || authoring.SoldierTransportCapacity > 0)
                return true;
        }

        string identity = $"{prefab?.name} {authoring?.ConfiguredDisplayName}";
        return ContainsIdentityToken(identity, "Veh") ||
               ContainsIdentityToken(identity, "Truck") ||
               ContainsIdentityToken(identity, "Tank") ||
               ContainsIdentityToken(identity, "APC") ||
               ContainsIdentityToken(identity, "Launcher") ||
               ContainsIdentityToken(identity, "Drone");
    }

    private static string ResolveUnitDisplayName(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return authoring.ConfiguredDisplayName;

        return prefab != null ? prefab.name : "Unit";
    }

    private static string ResolveUnitTypeLabel(GameObject prefab, UnitGridAuthoring authoring, bool isVehicle, bool isAir)
    {
        bool isTransport = IsTransportUnit(prefab, authoring);
        if (isAir)
            return isTransport ? "TRANSPORT AIRCRAFT" : "AIRCRAFT";

        if (isVehicle)
            return isTransport ? "TRANSPORT VEHICLE" : "VEHICLE";

        string identity = $"{prefab?.name} {authoring?.ConfiguredDisplayName}";
        if (ContainsIdentityToken(identity, "Civilian"))
            return "CIVILIAN";

        if (ContainsIdentityToken(identity, "Contractor"))
            return "CONTRACTOR";

        if (ContainsIdentityToken(identity, "Specialist") ||
            ContainsIdentityToken(identity, "Ghillie") ||
            ContainsIdentityToken(identity, "Bomb"))
        {
            return "SPECIALIST";
        }

        return "SOLDIER";
    }

    private static bool IsTransportUnit(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null &&
            (authoring.SoldierTransportCapacity > 0 || authoring.IsProductionTransportUnit))
        {
            return true;
        }

        string identity = $"{prefab?.name} {authoring?.ConfiguredDisplayName}";
        return ContainsIdentityToken(identity, "Transport") ||
               ContainsIdentityToken(identity, "Truck") ||
               ContainsIdentityToken(identity, "Cargo") ||
               ContainsIdentityToken(identity, "Tanker");
    }

    private static string ResolveUnitDescription(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDescription))
            return authoring.ConfiguredDescription;

        string name = ResolveUnitDisplayName(prefab, authoring);
        return string.IsNullOrWhiteSpace(name) ? "No description configured." : $"{name} has no configured production description.";
    }

    private static string ResolveBuildingTypeLabel(BuildingDefinitionAuthoring authoring)
    {
        if (authoring == null)
            return "STRUCTURE";

        string identity = authoring.ConfiguredDisplayName;
        if (authoring.ConfiguredIsWall ||
            ContainsIdentityToken(identity, "Wall") ||
            ContainsIdentityToken(identity, "Fence") ||
            ContainsIdentityToken(identity, "Barrier"))
        {
            return "WALL";
        }

        if (authoring.ConfiguredRole == BuildingRole.TentRefugee ||
            ContainsIdentityToken(identity, "Tent"))
        {
            return "TENT";
        }

        return "STRUCTURE";
    }

    private static string ResolveBuildingDescription(BuildingDefinitionAuthoring authoring)
    {
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDescription))
            return authoring.ConfiguredDescription;

        return "No description configured.";
    }

    private static bool ContainsIdentityToken(string identity, string token)
    {
        return !string.IsNullOrWhiteSpace(identity) &&
               identity.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int CompareItems(BuildDrawerCatalogItem left, BuildDrawerCatalogItem right)
    {
        int categoryCompare = left.Category.CompareTo(right.Category);
        return categoryCompare != 0
            ? categoryCompare
            : string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveFromConfiguredLists(
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        GameObject prefab,
        out BuildDrawerCatalogItem item)
    {
        IReadOnlyList<GameObject> spawnables = buildingPlacementConfig != null
            ? buildingPlacementConfig.Spawnables
            : null;
        if (spawnables != null)
        {
            for (int i = 0; i < spawnables.Count; i++)
            {
                GameObject candidate = spawnables[i];
                if (candidate != prefab)
                    continue;

                BuildingDefinitionAuthoring authoring = candidate != null ? candidate.GetComponent<BuildingDefinitionAuthoring>() : null;
                if (authoring != null && authoring.ConfiguredCanRequest)
                {
                    item = BuildBuildingItem(candidate, authoring);
                    return true;
                }
            }
        }

        IReadOnlyList<GameObject> unitPrefabs = unitPrefabRegistryConfig != null
            ? unitPrefabRegistryConfig.UnitSpawnPrefabs
            : null;
        if (unitPrefabs != null)
        {
            for (int i = 0; i < unitPrefabs.Count; i++)
            {
                GameObject candidate = unitPrefabs[i];
                if (candidate != prefab)
                    continue;

                UnitGridAuthoring authoring = candidate != null ? candidate.GetComponent<UnitGridAuthoring>() : null;
                if (authoring != null && authoring.CanRequest)
                {
                    bool isAir = authoring.IsAirUnit;
                    bool isVehicle = IsVehicle(candidate, authoring);
                    item = BuildUnitItem(candidate, authoring, ResolveUnitCategory(isAir, isVehicle), isVehicle, isAir);
                    return true;
                }
            }
        }

        item = default;
        return false;
    }
}
