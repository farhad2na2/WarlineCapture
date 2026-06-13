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
    private TryResolveUiBuildingCatalogMetadata _tryResolveBuildingMetadata;
    private TryResolveUiUnitCatalogMetadata _tryResolveUnitMetadata;

    public void ConfigureMetadataResolvers(
        TryResolveUiBuildingCatalogMetadata tryResolveBuildingMetadata,
        TryResolveUiUnitCatalogMetadata tryResolveUnitMetadata)
    {
        _tryResolveBuildingMetadata = tryResolveBuildingMetadata;
        _tryResolveUnitMetadata = tryResolveUnitMetadata;
    }

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

        if (TryResolveBuildingMetadata(prefab, out UiBuildingCatalogMetadata buildingMetadata) &&
            buildingMetadata.CanRequest)
        {
            item = BuildBuildingItem(prefab, buildingMetadata);
            return true;
        }

        if (TryResolveUnitMetadata(prefab, out UiUnitCatalogMetadata unitMetadata) &&
            unitMetadata.CanRequest)
        {
            bool isAir = unitMetadata.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, unitMetadata);
            item = BuildUnitItem(prefab, unitMetadata, ResolveUnitCategory(isAir, isVehicle), isVehicle, isAir);
            return true;
        }

        return TryResolveFromConfiguredLists(unitPrefabRegistryConfig, buildingPlacementConfig, prefab, out item);
    }

    private void CollectBuildings(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        List<BuildDrawerCatalogItem> results)
    {
        AppendBuildings(buildingPlacementConfig, results);
    }

    private void AppendBuildings(
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

            if (!TryResolveBuildingMetadata(prefab, out UiBuildingCatalogMetadata metadata) ||
                !metadata.CanRequest)
            {
                continue;
            }

            results.Add(BuildBuildingItem(prefab, metadata));
        }
    }

    private static BuildDrawerCatalogItem BuildBuildingItem(GameObject prefab, UiBuildingCatalogMetadata metadata)
    {
        return new BuildDrawerCatalogItem(
            BuildDrawerCategory.Buildings,
            prefab,
            string.IsNullOrWhiteSpace(metadata.DisplayName) ? prefab.name : metadata.DisplayName,
            ResolveBuildingTypeLabel(metadata),
            ResolveBuildingDescription(metadata),
            metadata.Price,
            metadata.ProductionDurationSeconds,
            metadata.FootprintCells,
            metadata.Portrait,
            metadata.CardPortrait,
            metadata.ActionPortrait);
    }

    private void CollectUnits(
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

            if (!TryResolveUnitMetadata(prefab, out UiUnitCatalogMetadata metadata) ||
                !metadata.CanRequest)
            {
                continue;
            }

            bool isAir = metadata.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, metadata);
            BuildDrawerCategory resolvedCategory = ResolveUnitCategory(isAir, isVehicle);
            if (resolvedCategory != category)
                continue;

            results.Add(BuildUnitItem(prefab, metadata, resolvedCategory, isVehicle, isAir));
        }
    }

    private void AppendUnits(
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

            if (!TryResolveUnitMetadata(prefab, out UiUnitCatalogMetadata metadata) ||
                !metadata.CanRequest)
            {
                continue;
            }

            bool isAir = metadata.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, metadata);
            BuildDrawerCategory category = ResolveUnitCategory(isAir, isVehicle);
            results.Add(BuildUnitItem(prefab, metadata, category, isVehicle, isAir));
        }
    }

    private static BuildDrawerCatalogItem BuildUnitItem(
        GameObject prefab,
        UiUnitCatalogMetadata metadata,
        BuildDrawerCategory category,
        bool isVehicle,
        bool isAir)
    {
        return new BuildDrawerCatalogItem(
            category,
            prefab,
            ResolveUnitDisplayName(prefab, metadata),
            ResolveUnitTypeLabel(prefab, metadata, isVehicle, isAir),
            ResolveUnitDescription(prefab, metadata),
            metadata.Price,
            metadata.ProductionDurationSeconds,
            metadata.FootprintCells,
            metadata.Portrait,
            metadata.CardPortrait,
            metadata.ActionPortrait);
    }

    private static BuildDrawerCategory ResolveUnitCategory(bool isAir, bool isVehicle)
    {
        if (isAir)
            return BuildDrawerCategory.Aircrafts;

        return isVehicle ? BuildDrawerCategory.Vehicles : BuildDrawerCategory.Soldiers;
    }

    private static bool IsVehicle(GameObject prefab, UiUnitCatalogMetadata metadata)
    {
        Vector2Int footprint = metadata.FootprintCells;
        if (footprint.x > 1 || footprint.y > 1 || metadata.SoldierTransportCapacity > 0)
            return true;

        string identity = $"{prefab?.name} {metadata.DisplayName}";
        return ContainsIdentityToken(identity, "Veh") ||
               ContainsIdentityToken(identity, "Truck") ||
               ContainsIdentityToken(identity, "Tank") ||
               ContainsIdentityToken(identity, "APC") ||
               ContainsIdentityToken(identity, "Launcher") ||
               ContainsIdentityToken(identity, "Drone");
    }

    private static string ResolveUnitDisplayName(GameObject prefab, UiUnitCatalogMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.DisplayName))
            return metadata.DisplayName;

        return prefab != null ? prefab.name : "Unit";
    }

    private static string ResolveUnitTypeLabel(GameObject prefab, UiUnitCatalogMetadata metadata, bool isVehicle, bool isAir)
    {
        bool isTransport = IsTransportUnit(prefab, metadata);
        if (isAir)
            return isTransport ? "TRANSPORT AIRCRAFT" : "AIRCRAFT";

        if (isVehicle)
            return isTransport ? "TRANSPORT VEHICLE" : "VEHICLE";

        string identity = $"{prefab?.name} {metadata.DisplayName}";
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

    private static bool IsTransportUnit(GameObject prefab, UiUnitCatalogMetadata metadata)
    {
        if (metadata.SoldierTransportCapacity > 0 || metadata.IsProductionTransportUnit)
            return true;

        string identity = $"{prefab?.name} {metadata.DisplayName}";
        return ContainsIdentityToken(identity, "Transport") ||
               ContainsIdentityToken(identity, "Truck") ||
               ContainsIdentityToken(identity, "Cargo") ||
               ContainsIdentityToken(identity, "Tanker");
    }

    private static string ResolveUnitDescription(GameObject prefab, UiUnitCatalogMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Description))
            return metadata.Description;

        string name = ResolveUnitDisplayName(prefab, metadata);
        return string.IsNullOrWhiteSpace(name) ? "No description configured." : $"{name} has no configured production description.";
    }

    private static string ResolveBuildingTypeLabel(UiBuildingCatalogMetadata metadata)
    {
        string identity = metadata.DisplayName;
        if (metadata.IsWall ||
            ContainsIdentityToken(identity, "Wall") ||
            ContainsIdentityToken(identity, "Fence") ||
            ContainsIdentityToken(identity, "Barrier"))
        {
            return "WALL";
        }

        if (metadata.IsTentRefugee || ContainsIdentityToken(identity, "Tent"))
        {
            return "TENT";
        }

        return "STRUCTURE";
    }

    private static string ResolveBuildingDescription(UiBuildingCatalogMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Description))
            return metadata.Description;

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

    private bool TryResolveFromConfiguredLists(
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

                if (TryResolveBuildingMetadata(candidate, out UiBuildingCatalogMetadata metadata) &&
                    metadata.CanRequest)
                {
                    item = BuildBuildingItem(candidate, metadata);
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

                if (TryResolveUnitMetadata(candidate, out UiUnitCatalogMetadata metadata) &&
                    metadata.CanRequest)
                {
                    bool isAir = metadata.IsAirUnit;
                    bool isVehicle = IsVehicle(candidate, metadata);
                    item = BuildUnitItem(candidate, metadata, ResolveUnitCategory(isAir, isVehicle), isVehicle, isAir);
                    return true;
                }
            }
        }

        item = default;
        return false;
    }

    private bool TryResolveBuildingMetadata(GameObject prefab, out UiBuildingCatalogMetadata metadata)
    {
        metadata = default;
        return _tryResolveBuildingMetadata != null &&
               _tryResolveBuildingMetadata(prefab, out metadata);
    }

    private bool TryResolveUnitMetadata(GameObject prefab, out UiUnitCatalogMetadata metadata)
    {
        metadata = default;
        return _tryResolveUnitMetadata != null &&
               _tryResolveUnitMetadata(prefab, out metadata);
    }
}
