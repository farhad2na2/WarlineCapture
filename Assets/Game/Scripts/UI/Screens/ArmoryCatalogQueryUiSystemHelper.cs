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

public sealed class ArmoryCatalogQueryUiSystemHelper
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
        ICatalogPrefabSource unitPrefabSource,
        ICatalogPrefabSource buildingPrefabSource,
        ArmoryCatalogCategory category,
        List<ArmoryCatalogItem> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (category == ArmoryCatalogCategory.Buildings)
        {
            CollectBuildingItems(buildingPrefabSource, results);
            return;
        }

        CollectUnitItems(unitPrefabSource, category, results);
    }

    private void CollectUnitItems(
        ICatalogPrefabSource unitPrefabSource,
        ArmoryCatalogCategory category,
        List<ArmoryCatalogItem> results)
    {
        IReadOnlyList<GameObject> prefabs = unitPrefabSource != null
            ? unitPrefabSource.UnitSpawnPrefabs
            : null;
        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;

            if (!TryResolveUnitMetadata(prefab, out UiUnitCatalogMetadata metadata))
                continue;

            bool isAir = metadata.IsAirUnit;
            bool isVehicle = IsVehicle(prefab, metadata);
            bool isTransport = IsTransportUnit(prefab, metadata);
            if (!MatchesUnitCategory(category, isVehicle, isAir))
                continue;

            results.Add(new ArmoryCatalogItem(
                ResolveUnitDisplayName(prefab, metadata),
                ResolveUnitTypeLabel(prefab, metadata, isVehicle, isAir),
                ResolveUnitDescription(prefab, metadata),
                ResolveUnitHealthValue(metadata),
                ResolveUnitDamageValue(metadata),
                ResolveUnitRangeValue(metadata),
                ResolveUnitSpeedValue(metadata),
                ResolveUnitMoveCapability(isVehicle, isAir, isTransport),
                ResolveUnitPatrolCapability(prefab, metadata, isTransport),
                ResolveUnitAttackCapability(metadata),
                ResolveUnitHoldCapability(prefab, metadata, isAir),
                metadata.Portrait,
                metadata.CardPortrait,
                metadata.ActionPortrait,
                category));
        }

        results.Sort(CompareItems);
    }

    private void CollectBuildingItems(
        ICatalogPrefabSource buildingPrefabSource,
        List<ArmoryCatalogItem> results)
    {
        IReadOnlyList<GameObject> spawnables = buildingPrefabSource != null
            ? buildingPrefabSource.BuildingSpawnPrefabs
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

            results.Add(new ArmoryCatalogItem(
                string.IsNullOrWhiteSpace(metadata.DisplayName) ? prefab.name : metadata.DisplayName,
                ResolveBuildingTypeLabel(metadata),
                ResolveBuildingDescription(metadata),
                ResolveBuildingHealthValue(metadata),
                "-",
                ResolveBuildingRangeValue(metadata),
                "-",
                ResolveBuildingMoveCapability(),
                ResolveBuildingPatrolCapability(),
                ResolveBuildingAttackCapability(),
                ResolveBuildingHoldCapability(metadata),
                metadata.Portrait,
                metadata.CardPortrait,
                metadata.ActionPortrait,
                ArmoryCatalogCategory.Buildings));
        }

        results.Sort(CompareItems);
    }

    private static bool IsVehicle(GameObject prefab, UiUnitCatalogMetadata metadata)
    {
        Vector2Int footprint = metadata.FootprintCells;
        if (footprint.x > 1 || footprint.y > 1)
            return true;

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
        return string.IsNullOrWhiteSpace(name)
            ? "No description configured."
            : $"{name} has no configured armory description.";
    }

    private static string ResolveUnitHealthValue(UiUnitCatalogMetadata metadata)
    {
        return FormatInt(metadata.MaxHealth);
    }

    private static string ResolveUnitDamageValue(UiUnitCatalogMetadata metadata)
    {
        if (!metadata.CanAttack || metadata.AttackDamage <= 0)
            return "-";

        return FormatInt(metadata.AttackDamage);
    }

    private static string ResolveUnitRangeValue(UiUnitCatalogMetadata metadata)
    {
        if (!metadata.CanAttack || metadata.AttackRange <= 0f)
            return "-";

        return FormatNumber(metadata.AttackRange);
    }

    private static string ResolveUnitSpeedValue(UiUnitCatalogMetadata metadata)
    {
        return metadata.Speed > 0f
            ? FormatNumber(metadata.Speed)
            : "-";
    }

    private static string ResolveUnitMoveCapability(bool isVehicle, bool isAir, bool isTransport)
    {
        if (isAir)
            return isTransport ? "AIR CARGO" : "AIR";

        if (isVehicle)
            return isTransport ? "TRANSPORT" : "VEHICLE";

        return "FOOT";
    }

    private static string ResolveUnitPatrolCapability(
        GameObject prefab,
        UiUnitCatalogMetadata metadata,
        bool isTransport)
    {
        string identity = $"{prefab?.name} {metadata.DisplayName}";
        if (ContainsIdentityToken(identity, "Civilian"))
            return metadata.AllowIdleWander ? "WANDER" : "N/A";

        if (metadata.ResourceHaulerBarrelCapacity > 0)
            return "HAUL";

        if (isTransport || metadata.SoldierTransportCapacity > 0)
            return "BOARD";

        return "N/A";
    }

    private static string ResolveUnitAttackCapability(UiUnitCatalogMetadata metadata)
    {
        if (!metadata.CanAttack || metadata.AttackDamage <= 0)
            return "UNARMED";

        return $"DMG {FormatInt(metadata.AttackDamage)}";
    }

    private static string ResolveUnitHoldCapability(
        GameObject prefab,
        UiUnitCatalogMetadata metadata,
        bool isAir)
    {
        string identity = $"{prefab?.name} {metadata.DisplayName}";
        if (ContainsIdentityToken(identity, "Civilian"))
            return "N/A";

        return isAir ? "LOITER" : "DEFEND";
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

    private static string ResolveBuildingHealthValue(UiBuildingCatalogMetadata metadata)
    {
        return FormatInt(metadata.MaxHealth);
    }

    private static string ResolveBuildingRangeValue(UiBuildingCatalogMetadata metadata)
    {
        if (metadata.ThreatDetectionRadiusCells <= 0)
            return "-";

        return FormatInt(metadata.ThreatDetectionRadiusCells);
    }

    private static string ResolveBuildingMoveCapability()
    {
        return "STATIC";
    }

    private static string ResolveBuildingPatrolCapability()
    {
        return "N/A";
    }

    private static string ResolveBuildingAttackCapability()
    {
        return "NO WEAPON";
    }

    private static string ResolveBuildingHoldCapability(UiBuildingCatalogMetadata metadata)
    {
        if (metadata.HasThreatDetection && metadata.ThreatDetectionRadiusCells > 0)
        {
            return metadata.DetectsAirThreats ? "AIR WATCH" : "WATCH";
        }

        if (metadata.IsWall)
            return "FORTIFY";

        if (metadata.IsTentRefugee)
            return "SHELTER";

        if (metadata.ProductionCount > 0)
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
