using UnityEngine;

public delegate bool TryResolveUiBuildingCatalogMetadata(GameObject prefab, out UiBuildingCatalogMetadata metadata);

public delegate bool TryResolveUiUnitCatalogMetadata(GameObject prefab, out UiUnitCatalogMetadata metadata);

public readonly struct UiBuildingCatalogMetadata
{
    public readonly string DisplayName;
    public readonly string Description;
    public readonly bool CanRequest;
    public readonly int Price;
    public readonly float ProductionDurationSeconds;
    public readonly Vector2Int FootprintCells;
    public readonly Sprite Portrait;
    public readonly Sprite CardPortrait;
    public readonly Sprite ActionPortrait;
    public readonly int MaxHealth;
    public readonly bool IsWall;
    public readonly bool IsTentRefugee;
    public readonly bool HasThreatDetection;
    public readonly bool DetectsAirThreats;
    public readonly int ThreatDetectionRadiusCells;
    public readonly int ProductionCount;

    public UiBuildingCatalogMetadata(
        string displayName,
        string description,
        bool canRequest,
        int price,
        float productionDurationSeconds,
        Vector2Int footprintCells,
        Sprite portrait,
        Sprite cardPortrait,
        Sprite actionPortrait,
        int maxHealth,
        bool isWall,
        bool isTentRefugee,
        bool hasThreatDetection,
        bool detectsAirThreats,
        int threatDetectionRadiusCells,
        int productionCount)
    {
        DisplayName = displayName;
        Description = description;
        CanRequest = canRequest;
        Price = Mathf.Max(0, price);
        ProductionDurationSeconds = Mathf.Max(0f, productionDurationSeconds);
        FootprintCells = new Vector2Int(Mathf.Max(1, footprintCells.x), Mathf.Max(1, footprintCells.y));
        Portrait = portrait;
        CardPortrait = cardPortrait;
        ActionPortrait = actionPortrait;
        MaxHealth = Mathf.Max(0, maxHealth);
        IsWall = isWall;
        IsTentRefugee = isTentRefugee;
        HasThreatDetection = hasThreatDetection;
        DetectsAirThreats = detectsAirThreats;
        ThreatDetectionRadiusCells = Mathf.Max(0, threatDetectionRadiusCells);
        ProductionCount = Mathf.Max(0, productionCount);
    }
}

public readonly struct UiUnitCatalogMetadata
{
    public readonly string DisplayName;
    public readonly string Description;
    public readonly bool CanRequest;
    public readonly int Price;
    public readonly float ProductionDurationSeconds;
    public readonly Vector2Int FootprintCells;
    public readonly Sprite Portrait;
    public readonly Sprite CardPortrait;
    public readonly Sprite ActionPortrait;
    public readonly bool IsAirUnit;
    public readonly bool IsProductionTransportUnit;
    public readonly int SoldierTransportCapacity;
    public readonly bool AllowIdleWander;
    public readonly int ResourceHaulerBarrelCapacity;
    public readonly bool CanAttack;
    public readonly int AttackDamage;
    public readonly float AttackRange;
    public readonly float Speed;
    public readonly int MaxHealth;

    public UiUnitCatalogMetadata(
        string displayName,
        string description,
        bool canRequest,
        int price,
        float productionDurationSeconds,
        Vector2Int footprintCells,
        Sprite portrait,
        Sprite cardPortrait,
        Sprite actionPortrait,
        bool isAirUnit,
        bool isProductionTransportUnit,
        int soldierTransportCapacity,
        bool allowIdleWander,
        int resourceHaulerBarrelCapacity,
        bool canAttack,
        int attackDamage,
        float attackRange,
        float speed,
        int maxHealth)
    {
        DisplayName = displayName;
        Description = description;
        CanRequest = canRequest;
        Price = Mathf.Max(0, price);
        ProductionDurationSeconds = Mathf.Max(0f, productionDurationSeconds);
        FootprintCells = new Vector2Int(Mathf.Max(1, footprintCells.x), Mathf.Max(1, footprintCells.y));
        Portrait = portrait;
        CardPortrait = cardPortrait;
        ActionPortrait = actionPortrait;
        IsAirUnit = isAirUnit;
        IsProductionTransportUnit = isProductionTransportUnit;
        SoldierTransportCapacity = Mathf.Max(0, soldierTransportCapacity);
        AllowIdleWander = allowIdleWander;
        ResourceHaulerBarrelCapacity = Mathf.Max(0, resourceHaulerBarrelCapacity);
        CanAttack = canAttack;
        AttackDamage = Mathf.Max(0, attackDamage);
        AttackRange = Mathf.Max(0f, attackRange);
        Speed = Mathf.Max(0f, speed);
        MaxHealth = Mathf.Max(0, maxHealth);
    }
}
