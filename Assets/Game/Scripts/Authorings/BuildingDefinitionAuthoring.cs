using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BuildingDefinitionAuthoring : MonoBehaviour, ISerializationCallbackReceiver
{
    [Serializable]
    public sealed class ProductionDefinition
    {
        public GameObject spawnUnitPrefab;
    }

    [SerializeField] private BuildingDefinitionAuthoringConfig config;

    [Header("Identity")]
    [SerializeField, HideInInspector] private string displayName = "Building";
    [SerializeField, HideInInspector, TextArea] private string description = "Operational building.";
    [SerializeField, HideInInspector] private Sprite portraitSprite;
    [SerializeField, HideInInspector] private Sprite portraitCardSprite;
    [SerializeField, HideInInspector] private Sprite portraitActionSprite;

    [Header("Placement")]
    [SerializeField, HideInInspector] private Vector2Int footprintCells = new(10, 10);

    [Header("Durability")]
    [SerializeField, HideInInspector, Min(1)] private int maxHealth = 500;

    [Header("Role")]
    [SerializeField, HideInInspector] private BuildingRole role;
    [SerializeField, HideInInspector] private bool canRequest = true;
    [SerializeField, HideInInspector, Min(0)] private int price = 20000;
    [SerializeField, HideInInspector, Min(0.01f)] private float productionDurationSeconds = 30f;

    [Header("Resources")]
    [SerializeField, HideInInspector, Min(0f)] private float oilBarrelsPerDay;
    [SerializeField, HideInInspector, Min(0)] private int oilStorageCapacity;
    [SerializeField, HideInInspector, Min(0f)] private float fuelBarrelsPerDay;
    [SerializeField, HideInInspector, Min(0)] private int fuelStorageCapacity;
    [SerializeField, HideInInspector, Min(0)] private int refugeeCapacity;
    [SerializeField, HideInInspector, Min(0)] private int refugeeUpkeepPerCitizenPerDay;
    [SerializeField, HideInInspector] private ThreatDetectionKind threatDetectionKind;
    [SerializeField, HideInInspector, Min(0)] private int threatDetectionRadiusCells;

    [Header("Production")]
    [SerializeField, HideInInspector] private List<ProductionDefinition> productions = new();
    [SerializeField, HideInInspector] private bool isWall;

    [Header("Destroyed Visual")]
    [SerializeField, HideInInspector] private GameObject destroyedVisualPrefab;

    [HideInInspector] public GameObject primarySpawnUnitPrefab;

    [HideInInspector] public GameObject secondarySpawnUnitPrefab;

    [HideInInspector] public GameObject tertiarySpawnUnitPrefab;

    public string ConfiguredDisplayName => displayName;
    public string ConfiguredDescription => description;
    public Sprite ConfiguredPortraitSprite => config != null ? config.PortraitSprite : portraitSprite;
    public Sprite ConfiguredPortraitCardSprite => config != null ? config.PortraitCardSprite : portraitCardSprite;
    public Sprite ConfiguredPortraitActionSprite => config != null ? config.PortraitActionSprite : portraitActionSprite;
    public Vector2Int ConfiguredFootprintCells => footprintCells;
    public int ConfiguredMaxHealth => maxHealth;
    public BuildingRole ConfiguredRole => role;
    public bool ConfiguredCanRequest => canRequest;
    public int ConfiguredPrice => Mathf.Max(0, price);
    public float ConfiguredProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
    public bool ConfiguredIsWall => isWall;
    public float ConfiguredOilBarrelsPerDay => oilBarrelsPerDay;
    public int ConfiguredOilStorageCapacity => oilStorageCapacity;
    public float ConfiguredFuelBarrelsPerDay => fuelBarrelsPerDay;
    public int ConfiguredFuelStorageCapacity => fuelStorageCapacity;
    public int ConfiguredRefugeeCapacity => refugeeCapacity;
    public int ConfiguredRefugeeUpkeepPerCitizenPerDay => refugeeUpkeepPerCitizenPerDay;
    public ThreatDetectionKind ConfiguredThreatDetectionKind => threatDetectionKind;
    public int ConfiguredThreatDetectionRadiusCells => Mathf.Max(0, threatDetectionRadiusCells);
    public int ConfiguredProductionCount => productions != null ? productions.Count : 0;
    public GameObject ConfiguredDestroyedVisualPrefab => destroyedVisualPrefab;

    public ProductionDefinition GetProductionOrDefault(int index)
    {
        ApplyConfigIfAvailable();
        MigrateLegacyProductionsIfNeeded();
        if (productions == null || index < 0 || index >= productions.Count)
            return null;

        return productions[index];
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
    }

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
        MigrateLegacyProductionsIfNeeded();
    }

    public void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        displayName = config.DisplayName;
        description = config.Description;
        portraitSprite = config.PortraitSprite;
        portraitCardSprite = config.PortraitCardSprite;
        portraitActionSprite = config.PortraitActionSprite;
        maxHealth = config.MaxHealth;
        role = config.Role;
        canRequest = config.CanRequest;
        price = config.Price;
        productionDurationSeconds = config.ProductionDurationSeconds;
        isWall = config.IsWall;
        oilBarrelsPerDay = config.OilBarrelsPerDay;
        oilStorageCapacity = config.OilStorageCapacity;
        fuelBarrelsPerDay = config.FuelBarrelsPerDay;
        fuelStorageCapacity = config.FuelStorageCapacity;
        refugeeCapacity = config.RefugeeCapacity;
        refugeeUpkeepPerCitizenPerDay = config.RefugeeUpkeepPerCitizenPerDay;
        threatDetectionKind = config.ThreatDetectionKind;
        threatDetectionRadiusCells = config.ThreatDetectionRadiusCells;
        destroyedVisualPrefab = config.DestroyedVisualPrefab;
        productions ??= new List<ProductionDefinition>();
        productions.Clear();
        if (config.Productions == null)
            return;

        for (int i = 0; i < config.Productions.Count; i++)
        {
            BuildingProductionConfigEntry entry = config.Productions[i];
            if (entry == null)
                continue;

            productions.Add(new ProductionDefinition
            {
                spawnUnitPrefab = entry.SpawnUnitPrefab
            });
        }
    }

    private void MigrateLegacyProductionsIfNeeded()
    {
        productions ??= new List<ProductionDefinition>();
        if (productions.Count > 0)
            return;

        AddLegacyProduction(primarySpawnUnitPrefab);
        AddLegacyProduction(secondarySpawnUnitPrefab);
        AddLegacyProduction(tertiarySpawnUnitPrefab);
    }

    private void AddLegacyProduction(GameObject spawnUnitPrefab)
    {
        if (spawnUnitPrefab == null)
            return;

        productions.Add(new ProductionDefinition
        {
            spawnUnitPrefab = spawnUnitPrefab
        });
    }
}
