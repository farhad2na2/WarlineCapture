using System.Collections.Generic;
using UnityEngine;

public readonly struct ArmoryCatalogItem
{
    public readonly string DisplayName;
    public readonly string TypeLabel;
    public readonly string Description;
    public readonly string HealthValue;
    public readonly string DamageValue;
    public readonly string RangeValue;
    public readonly string SpeedValue;
    public readonly string MoveCapability;
    public readonly string PatrolCapability;
    public readonly string AttackCapability;
    public readonly string HoldCapability;
    public readonly Sprite Portrait;
    public readonly Sprite CardPortrait;
    public readonly Sprite InspectionPortrait;
    public readonly ArmoryCatalogCategory Category;

    public ArmoryCatalogItem(string displayName, Sprite portrait, ArmoryCatalogCategory category)
        : this(displayName, ArmoryCatalogCategoryFormatter.Format(category), string.Empty, "-", "-", "-", "-", "-", "-", "-", "-", portrait, null, null, category)
    {
    }

    public ArmoryCatalogItem(
        string displayName,
        Sprite portrait,
        Sprite portraitCard,
        Sprite portraitAction,
        ArmoryCatalogCategory category)
        : this(displayName, ArmoryCatalogCategoryFormatter.Format(category), string.Empty, "-", "-", "-", "-", "-", "-", "-", "-", portrait, portraitCard, portraitAction, category)
    {
    }

    public ArmoryCatalogItem(
        string displayName,
        string typeLabel,
        string description,
        Sprite portrait,
        Sprite portraitCard,
        Sprite portraitAction,
        ArmoryCatalogCategory category)
        : this(displayName, typeLabel, description, "-", "-", "-", "-", "-", "-", "-", "-", portrait, portraitCard, portraitAction, category)
    {
    }

    public ArmoryCatalogItem(
        string displayName,
        string typeLabel,
        string description,
        string healthValue,
        string damageValue,
        string rangeValue,
        string speedValue,
        string moveCapability,
        string patrolCapability,
        string attackCapability,
        string holdCapability,
        Sprite portrait,
        Sprite portraitCard,
        Sprite portraitAction,
        ArmoryCatalogCategory category)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Item" : displayName;
        TypeLabel = string.IsNullOrWhiteSpace(typeLabel) ? ArmoryCatalogCategoryFormatter.Format(category) : typeLabel;
        Description = description ?? string.Empty;
        HealthValue = string.IsNullOrWhiteSpace(healthValue) ? "-" : healthValue;
        DamageValue = string.IsNullOrWhiteSpace(damageValue) ? "-" : damageValue;
        RangeValue = string.IsNullOrWhiteSpace(rangeValue) ? "-" : rangeValue;
        SpeedValue = string.IsNullOrWhiteSpace(speedValue) ? "-" : speedValue;
        MoveCapability = string.IsNullOrWhiteSpace(moveCapability) ? "-" : moveCapability;
        PatrolCapability = string.IsNullOrWhiteSpace(patrolCapability) ? "-" : patrolCapability;
        AttackCapability = string.IsNullOrWhiteSpace(attackCapability) ? "-" : attackCapability;
        HoldCapability = string.IsNullOrWhiteSpace(holdCapability) ? "-" : holdCapability;
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
            bool isTransport = IsTransportUnit(prefab, authoring);
            if (!MatchesUnitCategory(category, isVehicle, isAir))
                continue;

            results.Add(new ArmoryCatalogItem(
                ResolveUnitDisplayName(prefab, authoring),
                ResolveUnitTypeLabel(prefab, authoring, isVehicle, isAir),
                ResolveUnitDescription(prefab, authoring),
                ResolveUnitHealthValue(authoring),
                ResolveUnitDamageValue(authoring),
                ResolveUnitRangeValue(authoring),
                ResolveUnitSpeedValue(authoring),
                ResolveUnitMoveCapability(authoring, isVehicle, isAir, isTransport),
                ResolveUnitPatrolCapability(prefab, authoring, isTransport),
                ResolveUnitAttackCapability(authoring),
                ResolveUnitHoldCapability(prefab, authoring, isAir),
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
                ResolveBuildingTypeLabel(authoring),
                ResolveBuildingDescription(authoring),
                ResolveBuildingHealthValue(authoring),
                "-",
                ResolveBuildingRangeValue(authoring),
                "-",
                ResolveBuildingMoveCapability(authoring),
                ResolveBuildingPatrolCapability(),
                ResolveBuildingAttackCapability(authoring),
                ResolveBuildingHoldCapability(authoring),
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

        if (ContainsIdentityToken(identity, "Pilot"))
            return "PILOT";

        if (ContainsIdentityToken(identity, "Contractor"))
            return "CONTRACTOR";

        if (ContainsIdentityToken(identity, "Commander") || ContainsIdentityToken(identity, "Leader"))
            return "COMMAND";

        if (ContainsIdentityToken(identity, "Bomb Suit") || ContainsIdentityToken(identity, "Bombsuit"))
            return "SPECIALIST";

        return "INFANTRY";
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
        return string.IsNullOrWhiteSpace(name)
            ? "No description configured."
            : $"{name} has no configured armory description.";
    }

    private static string ResolveUnitHealthValue(UnitGridAuthoring authoring)
    {
        return authoring != null ? FormatInt(authoring.ConfiguredMaxHealth) : "-";
    }

    private static string ResolveUnitDamageValue(UnitGridAuthoring authoring)
    {
        if (authoring == null || !authoring.ConfiguredCanAttack || authoring.ConfiguredAttackDamage <= 0)
            return "-";

        return FormatInt(authoring.ConfiguredAttackDamage);
    }

    private static string ResolveUnitRangeValue(UnitGridAuthoring authoring)
    {
        if (authoring == null || !authoring.ConfiguredCanAttack || authoring.ConfiguredAttackRange <= 0f)
            return "-";

        return FormatNumber(authoring.ConfiguredAttackRange);
    }

    private static string ResolveUnitSpeedValue(UnitGridAuthoring authoring)
    {
        return authoring != null && authoring.ConfiguredSpeed > 0f
            ? FormatNumber(authoring.ConfiguredSpeed)
            : "-";
    }

    private static string ResolveUnitMoveCapability(
        UnitGridAuthoring authoring,
        bool isVehicle,
        bool isAir,
        bool isTransport)
    {
        if (authoring == null)
            return "N/A";

        if (isAir)
            return isTransport ? "AIR CARGO" : "AIR";

        if (isVehicle)
            return isTransport ? "TRANSPORT" : "VEHICLE";

        return "FOOT";
    }

    private static string ResolveUnitPatrolCapability(
        GameObject prefab,
        UnitGridAuthoring authoring,
        bool isTransport)
    {
        if (authoring == null)
            return "N/A";

        string identity = $"{prefab?.name} {authoring.ConfiguredDisplayName}";
        if (ContainsIdentityToken(identity, "Civilian"))
            return authoring.ConfiguredAllowIdleWander ? "WANDER" : "N/A";

        if (authoring.ConfiguredResourceHaulerBarrelCapacity > 0)
            return "HAUL";

        if (isTransport || authoring.SoldierTransportCapacity > 0)
            return "BOARD";

        return "N/A";
    }

    private static string ResolveUnitAttackCapability(UnitGridAuthoring authoring)
    {
        if (authoring == null || !authoring.ConfiguredCanAttack || authoring.ConfiguredAttackDamage <= 0)
            return "UNARMED";

        return $"DMG {FormatInt(authoring.ConfiguredAttackDamage)}";
    }

    private static string ResolveUnitHoldCapability(
        GameObject prefab,
        UnitGridAuthoring authoring,
        bool isAir)
    {
        if (authoring == null)
            return "N/A";

        string identity = $"{prefab?.name} {authoring.ConfiguredDisplayName}";
        if (ContainsIdentityToken(identity, "Civilian"))
            return "N/A";

        return isAir ? "LOITER" : "DEFEND";
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

    private static string ResolveBuildingHealthValue(BuildingDefinitionAuthoring authoring)
    {
        return authoring != null ? FormatInt(authoring.ConfiguredMaxHealth) : "-";
    }

    private static string ResolveBuildingRangeValue(BuildingDefinitionAuthoring authoring)
    {
        if (authoring == null || authoring.ConfiguredThreatDetectionRadiusCells <= 0)
            return "-";

        return FormatInt(authoring.ConfiguredThreatDetectionRadiusCells);
    }

    private static string ResolveBuildingMoveCapability(BuildingDefinitionAuthoring authoring)
    {
        return authoring != null ? "STATIC" : "N/A";
    }

    private static string ResolveBuildingPatrolCapability()
    {
        return "N/A";
    }

    private static string ResolveBuildingAttackCapability(BuildingDefinitionAuthoring authoring)
    {
        return authoring != null ? "NO WEAPON" : "N/A";
    }

    private static string ResolveBuildingHoldCapability(BuildingDefinitionAuthoring authoring)
    {
        if (authoring == null)
            return "N/A";

        if (authoring.ConfiguredThreatDetectionKind != ThreatDetectionKind.None &&
            authoring.ConfiguredThreatDetectionRadiusCells > 0)
        {
            return authoring.ConfiguredThreatDetectionKind == ThreatDetectionKind.Air ? "AIR WATCH" : "WATCH";
        }

        if (authoring.ConfiguredIsWall)
            return "FORTIFY";

        if (authoring.ConfiguredRole == BuildingRole.TentRefugee)
            return "SHELTER";

        if (authoring.ConfiguredProductionCount > 0)
            return "PRODUCE";

        return "STATIC";
    }

    private static bool ContainsIdentityToken(string identity, string token)
    {
        return !string.IsNullOrWhiteSpace(identity) &&
               identity.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string FormatInt(int value)
    {
        return Mathf.Max(0, value).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(float value)
    {
        float clamped = Mathf.Max(0f, value);
        return Mathf.Approximately(clamped, Mathf.Round(clamped))
            ? Mathf.RoundToInt(clamped).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : clamped.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int CompareItems(ArmoryCatalogItem left, ArmoryCatalogItem right)
    {
        return string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase);
    }
}
