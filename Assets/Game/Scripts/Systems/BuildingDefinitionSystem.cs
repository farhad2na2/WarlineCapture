using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using ConfiguredSpawnableEntry = BuildingUiCommandBoundary.ConfiguredSpawnableEntry;
using ConfiguredUnitEntry = BuildingUiCommandBoundary.ConfiguredUnitEntry;

internal sealed class BuildingDefinitionSystem
{
    public delegate void ObjectAction(UnityEngine.Object target);
    public delegate bool TryGetBuildingDefinitionMetadataDelegate(GameObject prefab, out BuildingDefinitionMetadata metadata);
    public delegate bool TryGetUnitDefinitionMetadataDelegate(GameObject prefab, out UnitDefinitionMetadata metadata);

    public struct BuildingDefinitionMetadata
    {
        public string DisplayName;
        public string Description;
        public int MaxHealth;
        public GameObject DestroyedVisualPrefab;
        public Vector2Int FootprintCells;
        public BuildingRole Role;
        public bool IsWall;
        public bool CanRequest;
        public int Price;
        public float ProductionDurationSeconds;
        public float OilBarrelsPerDay;
        public int OilStorageCapacity;
        public float FuelBarrelsPerDay;
        public int FuelStorageCapacity;
        public int RefugeeCapacity;
        public int RefugeeUpkeepPerCitizenPerDay;
        public ThreatDetectionKind ThreatDetectionKind;
        public int ThreatDetectionRadiusCells;
        public GameObject[] ProductionSpawnUnitPrefabs;
    }

    public struct UnitDefinitionMetadata
    {
        public string DisplayName;
        public string Description;
        public Vector2Int FootprintCells;
        public bool CanRequest;
        public int Price;
    }

    private sealed class CachedRuntimeBuildingMetadata
    {
        public bool HasDefinitionMetadata;
        public BuildingDefinitionMetadata DefinitionMetadata;
        public bool HasVisualFootprint;
        public Vector2Int VisualFootprint;
        public Bounds LocalBounds;
        public bool HasLocalBounds;
        public bool HasRunway;
        public Vector3 RunwayLocalPosition;
        public Quaternion RunwayLocalRotation;
        public Vector3 RunwayHalfExtents;
        public Vector3[] ProductionSpawnLocalPositions;
    }

    private readonly Dictionary<GameObject, CachedRuntimeBuildingMetadata> _runtimeBuildingMetadataCache = new();
    private readonly Dictionary<string, GameObject> _spawnablesByKey = new();
    private readonly Dictionary<string, GameObject> _unitSpawnPrefabsByKey = new();
    private readonly List<GameObject> _configuredSpawnablePrefabs = new();
    private readonly List<GameObject> _configuredUnitSpawnPrefabs = new();
    private readonly List<BuildingDefinition> _configuredSpawnableDefinitions = new();
    private readonly Dictionary<GameObject, BuildingDefinition> _configuredDefinitionsByPrefab = new();
    private readonly List<ConfiguredSpawnableEntry> _configuredSpawnableEntries = new();
    private readonly Dictionary<GameObject, ConfiguredSpawnableEntry> _configuredSpawnableEntriesByPrefab = new();
    private readonly List<ConfiguredUnitEntry> _configuredUnitEntries = new();
    private TryGetBuildingDefinitionMetadataDelegate _tryGetBuildingDefinitionMetadata;
    private TryGetUnitDefinitionMetadataDelegate _tryGetUnitDefinitionMetadata;

    public IReadOnlyDictionary<string, GameObject> UnitSpawnPrefabsByKey => _unitSpawnPrefabsByKey;
    public IReadOnlyList<GameObject> ConfiguredSpawnablePrefabs => _configuredSpawnablePrefabs;
    public IReadOnlyList<GameObject> ConfiguredUnitSpawnPrefabs => _configuredUnitSpawnPrefabs;
    public IReadOnlyList<BuildingDefinition> ConfiguredSpawnableDefinitions => _configuredSpawnableDefinitions;
    public IReadOnlyDictionary<GameObject, BuildingDefinition> ConfiguredDefinitionsByPrefab => _configuredDefinitionsByPrefab;
    public int ConfiguredSpawnableCount => _configuredSpawnableDefinitions.Count;
    public int ConfiguredUnitCount => _configuredUnitSpawnPrefabs.Count;

    public void ConfigureAuthoringMetadataResolvers(
        TryGetBuildingDefinitionMetadataDelegate tryGetBuildingDefinitionMetadata,
        TryGetUnitDefinitionMetadataDelegate tryGetUnitDefinitionMetadata)
    {
        _tryGetBuildingDefinitionMetadata = tryGetBuildingDefinitionMetadata;
        _tryGetUnitDefinitionMetadata = tryGetUnitDefinitionMetadata;
    }

    public void RebuildSpawnablesLookup(IReadOnlyList<GameObject> spawnables, IReadOnlyList<GameObject> unitSpawnPrefabs)
    {
        _spawnablesByKey.Clear();
        _unitSpawnPrefabsByKey.Clear();
        _configuredSpawnablePrefabs.Clear();
        _configuredUnitSpawnPrefabs.Clear();
        _configuredUnitEntries.Clear();

        if (spawnables != null)
        {
            for (int i = 0; i < spawnables.Count; i++)
            {
                GameObject prefab = spawnables[i];
                if (prefab != null && !_configuredSpawnablePrefabs.Contains(prefab))
                    _configuredSpawnablePrefabs.Add(prefab);
                RegisterSpawnableLookupAliases(_spawnablesByKey, prefab);
            }
        }

        if (unitSpawnPrefabs != null)
        {
            for (int i = 0; i < unitSpawnPrefabs.Count; i++)
            {
                GameObject prefab = unitSpawnPrefabs[i];
                if (prefab != null && !_configuredUnitSpawnPrefabs.Contains(prefab))
                {
                    _configuredUnitSpawnPrefabs.Add(prefab);
                    _configuredUnitEntries.Add(BuildConfiguredUnitEntry(prefab));
                }
                RegisterSpawnableLookupAliases(_unitSpawnPrefabsByKey, unitSpawnPrefabs[i]);
            }
        }
    }

    public void RebuildConfiguredSpawnableDefinitions(
        BuildingRunwaySystem runwaySystem,
        ObjectAction destroyObject)
    {
        ClearConfiguredSpawnableDefinitions(destroyObject);

        if (_configuredSpawnablePrefabs == null)
            return;

        for (int i = 0; i < _configuredSpawnablePrefabs.Count; i++)
        {
            GameObject prefab = _configuredSpawnablePrefabs[i];
            if (prefab == null)
                continue;

            BuildingDefinition definition = CreateDefinition(
                prefab,
                prefab.name,
                "Operational building.",
                500,
                null,
                null,
                null,
                runwaySystem);
            BuildCombinedVisualTemplate(definition);
            CacheBuildingBounds(definition, destroyObject);
            _configuredSpawnableDefinitions.Add(definition);
            _configuredDefinitionsByPrefab[prefab] = definition;
            ConfiguredSpawnableEntry entry = BuildConfiguredSpawnableEntryForDefinition(definition);
            _configuredSpawnableEntries.Add(entry);
            _configuredSpawnableEntriesByPrefab[prefab] = entry;
        }
    }

    public void ClearConfiguredSpawnableDefinitions(ObjectAction destroyObject)
    {
        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
            CleanupCombinedVisualTemplate(_configuredSpawnableDefinitions[i], destroyObject);

        _configuredSpawnableDefinitions.Clear();
        _configuredDefinitionsByPrefab.Clear();
        _configuredSpawnableEntries.Clear();
        _configuredSpawnableEntriesByPrefab.Clear();
        _runtimeBuildingMetadataCache.Clear();
    }

    public void ClearConfiguredPrefabLookups()
    {
        _spawnablesByKey.Clear();
        _unitSpawnPrefabsByKey.Clear();
        _configuredSpawnablePrefabs.Clear();
        _configuredUnitSpawnPrefabs.Clear();
        _configuredUnitEntries.Clear();
    }

    public BuildingDefinition FindConfiguredDefinition(string displayName)
    {
        string key = NormalizeSpawnableKey(displayName);
        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = _configuredSpawnableDefinitions[i];
            if (NormalizeSpawnableKey(definition?.DisplayName) == key)
                return definition;
        }

        return null;
    }

    public bool TryGetConfiguredDefinition(int index, out BuildingDefinition definition)
    {
        if (index >= 0 && index < _configuredSpawnableDefinitions.Count)
        {
            definition = _configuredSpawnableDefinitions[index];
            return definition != null;
        }

        definition = null;
        return false;
    }

    public bool TryGetConfiguredDefinition(GameObject prefab, out BuildingDefinition definition)
    {
        definition = null;
        return prefab != null &&
               _configuredDefinitionsByPrefab.TryGetValue(prefab, out definition) &&
               definition != null;
    }

    public bool IsConfiguredSpawnablePrefab(GameObject prefab)
    {
        return prefab != null && _configuredDefinitionsByPrefab.ContainsKey(prefab);
    }

    public bool TryGetConfiguredSpawnable(int index, out ConfiguredSpawnableEntry entry)
    {
        if (index >= 0 && index < _configuredSpawnableEntries.Count)
        {
            entry = _configuredSpawnableEntries[index];
            return entry.Prefab != null;
        }

        entry = default;
        return false;
    }

    public bool TryGetConfiguredSpawnable(GameObject prefab, out ConfiguredSpawnableEntry entry)
    {
        if (prefab != null &&
            _configuredSpawnableEntriesByPrefab.TryGetValue(prefab, out entry) &&
            entry.Prefab != null)
        {
            return true;
        }

        entry = default;
        return false;
    }

    public bool TryGetConfiguredSpawnable(string buildingId, out ConfiguredSpawnableEntry entry)
    {
        string normalized = NormalizeSpawnableKey(buildingId);
        if (!string.IsNullOrEmpty(normalized) &&
            _spawnablesByKey.TryGetValue(normalized, out GameObject prefab) &&
            prefab != null &&
            _configuredSpawnableEntriesByPrefab.TryGetValue(prefab, out entry) &&
            entry.Prefab != null)
        {
            return true;
        }

        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = _configuredSpawnableDefinitions[i];
            if (definition == null || !RuntimeDefinitionMatchesId(definition, normalized))
                continue;

            return TryGetConfiguredSpawnable(i, out entry);
        }

        entry = default;
        return false;
    }

    public bool TryGetConfiguredUnit(int index, out ConfiguredUnitEntry entry)
    {
        if (index >= 0 && index < _configuredUnitEntries.Count)
        {
            entry = _configuredUnitEntries[index];
            return entry.Prefab != null;
        }

        entry = default;
        return false;
    }

    public bool TryResolveConfiguredSpawnablePrefab(string lookupKey, out GameObject prefab)
    {
        prefab = null;
        string key = GetSpawnableLookupKey(lookupKey);
        return !string.IsNullOrEmpty(key) && _spawnablesByKey.TryGetValue(key, out prefab) && prefab != null;
    }

    public bool TryResolveConfiguredUnitSpawnPrefab(string lookupKey, out GameObject prefab)
    {
        prefab = null;
        string key = GetSpawnableLookupKey(lookupKey);
        return !string.IsNullOrEmpty(key) && _unitSpawnPrefabsByKey.TryGetValue(key, out prefab) && prefab != null;
    }

    public bool TryGetConfiguredUnitReadModel(
        int index,
        out GameObject prefab,
        out string displayName,
        out int price,
        out bool canRequest,
        out bool isVehicle)
    {
        prefab = null;
        displayName = string.Empty;
        price = 0;
        canRequest = false;
        isVehicle = false;

        if (index < 0 || index >= _configuredUnitSpawnPrefabs.Count)
            return false;

        if (index >= _configuredUnitEntries.Count)
            return false;

        ConfiguredUnitEntry entry = _configuredUnitEntries[index];
        prefab = entry.Prefab;
        if (prefab == null)
            return false;

        displayName = entry.DisplayName;
        isVehicle = entry.IsVehicle;
        price = entry.Price;
        canRequest = entry.CanRequest;
        return true;
    }

    private ConfiguredUnitEntry BuildConfiguredUnitEntry(GameObject prefab)
    {
        bool hasMetadata = TryGetUnitDefinitionMetadata(prefab, out UnitDefinitionMetadata metadata);
        string displayName = ResolveConfiguredUnitDisplayName(prefab, hasMetadata, metadata);
        string description = hasMetadata ? metadata.Description : string.Empty;
        Vector2Int footprint = hasMetadata ? metadata.FootprintCells : Vector2Int.one;
        bool isVehicle = footprint.x > 1 ||
                         footprint.y > 1 ||
                         (prefab != null && prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0);
        int price = hasMetadata ? metadata.Price : (isVehicle ? 15000 : 10000);
        return new ConfiguredUnitEntry(displayName, description, prefab, isVehicle, !hasMetadata || metadata.CanRequest, price);
    }

    private static string ResolveConfiguredUnitDisplayName(GameObject prefab, bool hasMetadata, UnitDefinitionMetadata metadata)
    {
        if (hasMetadata && !string.IsNullOrWhiteSpace(metadata.DisplayName))
            return metadata.DisplayName;

        return prefab != null ? prefab.name : "Unit";
    }

    public BuildingDefinition CreateRuntimeBuildingDefinition(
        GameObject prefab,
        string fallbackDisplayName,
        string fallbackDescription,
        Vector2Int fallbackFootprint,
        int fallbackMaxHealth,
        BuildingRunwaySystem runwaySystem)
    {
        CachedRuntimeBuildingMetadata metadata = GetOrCreateRuntimeBuildingMetadata(prefab, runwaySystem);
        List<BuildingDefinition.ProductionSlotDefinition> productionSlots = BuildProductionSlots(
            metadata.HasDefinitionMetadata,
            metadata.DefinitionMetadata,
            null,
            null,
            null,
            null);

        return new BuildingDefinition
        {
            DisplayName = metadata.HasDefinitionMetadata && !string.IsNullOrWhiteSpace(metadata.DefinitionMetadata.DisplayName) ? metadata.DefinitionMetadata.DisplayName : fallbackDisplayName,
            Description = metadata.HasDefinitionMetadata && !string.IsNullOrWhiteSpace(metadata.DefinitionMetadata.Description) ? metadata.DefinitionMetadata.Description : fallbackDescription,
            MaxHealth = metadata.HasDefinitionMetadata ? Mathf.Max(1, metadata.DefinitionMetadata.MaxHealth) : Mathf.Max(1, fallbackMaxHealth),
            ProductionSlots = productionSlots,
            SpawnUnitPrefab = GetProductionPrefab(productionSlots, 0),
            SecondarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 1),
            TertiarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 2),
            QuaternarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 3),
            Prefab = prefab,
            DestroyedVisualPrefab = metadata.HasDefinitionMetadata ? metadata.DefinitionMetadata.DestroyedVisualPrefab : null,
            FootprintCells = metadata.HasVisualFootprint
                ? metadata.VisualFootprint
                : metadata.HasDefinitionMetadata
                    ? NormalizeFootprint(metadata.DefinitionMetadata.FootprintCells)
                    : fallbackFootprint,
            Role = metadata.HasDefinitionMetadata ? metadata.DefinitionMetadata.Role : BuildingRole.None,
            IsWall = metadata.HasDefinitionMetadata && metadata.DefinitionMetadata.IsWall,
            ProductionDurationSeconds = metadata.HasDefinitionMetadata ? Mathf.Max(0.01f, metadata.DefinitionMetadata.ProductionDurationSeconds) : 30f,
            OilBarrelsPerDay = metadata.HasDefinitionMetadata ? Mathf.Max(0f, metadata.DefinitionMetadata.OilBarrelsPerDay) : 0f,
            OilStorageCapacity = metadata.HasDefinitionMetadata ? Mathf.Max(0, metadata.DefinitionMetadata.OilStorageCapacity) : 0,
            FuelBarrelsPerDay = metadata.HasDefinitionMetadata ? Mathf.Max(0f, metadata.DefinitionMetadata.FuelBarrelsPerDay) : 0f,
            FuelStorageCapacity = metadata.HasDefinitionMetadata ? Mathf.Max(0, metadata.DefinitionMetadata.FuelStorageCapacity) : 0,
            RefugeeCapacity = metadata.HasDefinitionMetadata ? Mathf.Max(0, metadata.DefinitionMetadata.RefugeeCapacity) : 0,
            RefugeeUpkeepPerCitizenPerDay = metadata.HasDefinitionMetadata ? Mathf.Max(0, metadata.DefinitionMetadata.RefugeeUpkeepPerCitizenPerDay) : 0,
            LocalBounds = metadata.LocalBounds,
            HasLocalBounds = metadata.HasLocalBounds,
            ProductionSpawnLocalPositions = metadata.ProductionSpawnLocalPositions,
            HasRunway = metadata.HasRunway,
            RunwayLocalPosition = metadata.RunwayLocalPosition,
            RunwayLocalRotation = metadata.RunwayLocalRotation,
            RunwayHalfExtents = metadata.RunwayHalfExtents
        };
    }

    public BuildingDefinition CreateDefinition(
        GameObject prefab,
        string fallbackDisplayName,
        string fallbackDescription,
        int fallbackMaxHealth,
        GameObject fallbackPrimarySpawnUnitPrefab,
        GameObject fallbackSecondarySpawnUnitPrefab,
        GameObject fallbackTertiarySpawnUnitPrefab,
        BuildingRunwaySystem runwaySystem)
    {
        bool hasMetadata = TryGetBuildingDefinitionMetadata(prefab, out BuildingDefinitionMetadata metadata);

        List<BuildingDefinition.ProductionSlotDefinition> productionSlots = BuildProductionSlots(
            hasMetadata,
            metadata,
            fallbackPrimarySpawnUnitPrefab,
            fallbackSecondarySpawnUnitPrefab,
            fallbackTertiarySpawnUnitPrefab);

        bool hasVisualFootprint = TryGetFootprintFromVisualBounds(prefab, out Vector2Int visualFootprint);
        Vector2Int configuredFootprint = hasMetadata ? NormalizeFootprint(metadata.FootprintCells) : Vector2Int.one;
        bool hasLocalBounds = TryGetPrefabLocalBounds(prefab, out Bounds localBounds);
        Vector3 runwayLocalPosition = default;
        Quaternion runwayLocalRotation = Quaternion.identity;
        Vector3 runwayHalfExtents = default;
        bool hasRunway = runwaySystem != null &&
            runwaySystem.TryGetRunwayLocalData(prefab, out runwayLocalPosition, out runwayLocalRotation, out runwayHalfExtents);

        return new BuildingDefinition
        {
            DisplayName = hasMetadata && !string.IsNullOrWhiteSpace(metadata.DisplayName) ? metadata.DisplayName : fallbackDisplayName,
            Description = hasMetadata && !string.IsNullOrWhiteSpace(metadata.Description) ? metadata.Description : fallbackDescription,
            MaxHealth = hasMetadata ? Mathf.Max(1, metadata.MaxHealth) : Mathf.Max(1, fallbackMaxHealth),
            ProductionSlots = productionSlots,
            SpawnUnitPrefab = GetProductionPrefab(productionSlots, 0),
            SecondarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 1),
            TertiarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 2),
            QuaternarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 3),
            Prefab = prefab,
            DestroyedVisualPrefab = hasMetadata ? metadata.DestroyedVisualPrefab : null,
            FootprintCells = hasVisualFootprint ? visualFootprint : configuredFootprint,
            Role = hasMetadata ? metadata.Role : BuildingRole.None,
            IsWall = hasMetadata && metadata.IsWall,
            ProductionDurationSeconds = hasMetadata ? Mathf.Max(0.01f, metadata.ProductionDurationSeconds) : 30f,
            OilBarrelsPerDay = hasMetadata ? Mathf.Max(0f, metadata.OilBarrelsPerDay) : 0f,
            OilStorageCapacity = hasMetadata ? Mathf.Max(0, metadata.OilStorageCapacity) : 0,
            FuelBarrelsPerDay = hasMetadata ? Mathf.Max(0f, metadata.FuelBarrelsPerDay) : 0f,
            FuelStorageCapacity = hasMetadata ? Mathf.Max(0, metadata.FuelStorageCapacity) : 0,
            RefugeeCapacity = hasMetadata ? Mathf.Max(0, metadata.RefugeeCapacity) : 0,
            RefugeeUpkeepPerCitizenPerDay = hasMetadata ? Mathf.Max(0, metadata.RefugeeUpkeepPerCitizenPerDay) : 0,
            ThreatDetectionKind = hasMetadata ? metadata.ThreatDetectionKind : ThreatDetectionKind.None,
            ThreatDetectionRadiusCells = hasMetadata ? Mathf.Max(0, metadata.ThreatDetectionRadiusCells) : 0,
            LocalBounds = localBounds,
            HasLocalBounds = hasLocalBounds,
            ProductionSpawnLocalPositions = FindProductionSpawnLocalPositions(prefab),
            HasRunway = hasRunway,
            RunwayLocalPosition = runwayLocalPosition,
            RunwayLocalRotation = runwayLocalRotation,
            RunwayHalfExtents = runwayHalfExtents
        };
    }

    private ConfiguredSpawnableEntry BuildConfiguredSpawnableEntryForDefinition(BuildingDefinition definition)
    {
        if (definition == null)
            return default;

        bool canRequest = true;
        int price = 20000;
        if (TryGetBuildingDefinitionMetadata(definition.Prefab, out BuildingDefinitionMetadata metadata))
        {
            canRequest = metadata.CanRequest;
            price = Mathf.Max(0, metadata.Price);
        }

        return new ConfiguredSpawnableEntry(definition.DisplayName, definition.Description, definition.Prefab, canRequest, price);
    }

    public static ConfiguredSpawnableEntry BuildConfiguredSpawnableEntry(BuildingDefinition definition)
    {
        if (definition == null)
            return default;

        return new ConfiguredSpawnableEntry(definition.DisplayName, definition.Description, definition.Prefab, true, 20000);
    }

    public static int GetProductionCount(BuildingDefinition definition)
    {
        if (definition == null)
            return 0;

        if (definition.ProductionSlots != null && definition.ProductionSlots.Count > 0)
            return definition.ProductionSlots.Count;

        int count = 0;
        if (definition.SpawnUnitPrefab != null) count = 1;
        if (definition.SecondarySpawnUnitPrefab != null) count = 2;
        if (definition.TertiarySpawnUnitPrefab != null) count = 3;
        if (definition.QuaternarySpawnUnitPrefab != null) count = 4;
        return count;
    }

    public static GameObject GetProductionPrefab(BuildingDefinition definition, int index)
    {
        if (definition == null || index < 0)
            return null;

        if (definition.ProductionSlots != null && index < definition.ProductionSlots.Count)
            return definition.ProductionSlots[index]?.SpawnUnitPrefab;

        return index switch
        {
            0 => definition.SpawnUnitPrefab,
            1 => definition.SecondarySpawnUnitPrefab,
            2 => definition.TertiarySpawnUnitPrefab,
            3 => definition.QuaternarySpawnUnitPrefab,
            _ => null
        };
    }

    public static string GetSpawnableLookupKey(GameObject prefab)
    {
        if (prefab == null)
            return string.Empty;

        return NormalizeSpawnableKey(prefab.name);
    }

    public static string GetSpawnableLookupKey(string name)
    {
        return NormalizeSpawnableKey(name);
    }

    public static string NormalizeSpawnableKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToLowerInvariant();
    }

    public static bool RuntimeBuildingMatchesId(RuntimeBuildingEntity building, string normalizedBuildingId)
    {
        return building?.Definition != null && RuntimeDefinitionMatchesId(building.Definition, normalizedBuildingId);
    }

    public static bool UnitPrefabMatchesId(GameObject prefab, string normalizedUnitId)
    {
        if (string.IsNullOrEmpty(normalizedUnitId))
            return true;
        if (prefab == null)
            return false;

        return NormalizeSpawnableKey(prefab.name) == normalizedUnitId;
    }

    public static bool RuntimeDefinitionMatchesId(BuildingDefinition definition, string normalizedBuildingId)
    {
        if (definition == null || string.IsNullOrEmpty(normalizedBuildingId))
            return false;

        if (NormalizeSpawnableKey(definition.DisplayName) == normalizedBuildingId)
            return true;

        if (definition.Prefab != null)
            return NormalizeSpawnableKey(definition.Prefab.name) == normalizedBuildingId;

        return false;
    }

    public static bool TryGetPrefabLocalBounds(GameObject prefab, out Bounds localBounds)
    {
        localBounds = default;
        if (prefab == null)
            return false;

        return TryGetLocalBounds(prefab, out localBounds);
    }

    private bool TryGetBuildingDefinitionMetadata(GameObject prefab, out BuildingDefinitionMetadata metadata)
    {
        metadata = default;
        return prefab != null &&
               _tryGetBuildingDefinitionMetadata != null &&
               _tryGetBuildingDefinitionMetadata(prefab, out metadata);
    }

    private bool TryGetUnitDefinitionMetadata(GameObject prefab, out UnitDefinitionMetadata metadata)
    {
        metadata = default;
        return prefab != null &&
               _tryGetUnitDefinitionMetadata != null &&
               _tryGetUnitDefinitionMetadata(prefab, out metadata);
    }

    private string ResolveConfiguredSpawnableLookupKey(GameObject prefab)
    {
        if (prefab == null)
            return string.Empty;

        if (TryGetBuildingDefinitionMetadata(prefab, out BuildingDefinitionMetadata buildingMetadata) &&
            !string.IsNullOrWhiteSpace(buildingMetadata.DisplayName))
        {
            return NormalizeSpawnableKey(buildingMetadata.DisplayName);
        }

        if (TryGetUnitDefinitionMetadata(prefab, out UnitDefinitionMetadata unitMetadata) &&
            !string.IsNullOrWhiteSpace(unitMetadata.DisplayName))
        {
            return NormalizeSpawnableKey(unitMetadata.DisplayName);
        }

        return NormalizeSpawnableKey(prefab.name);
    }

    private static Vector2Int NormalizeFootprint(Vector2Int footprint)
    {
        return new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
    }

    private CachedRuntimeBuildingMetadata GetOrCreateRuntimeBuildingMetadata(GameObject prefab, BuildingRunwaySystem runwaySystem)
    {
        if (prefab == null)
            return new CachedRuntimeBuildingMetadata();

        if (_runtimeBuildingMetadataCache.TryGetValue(prefab, out CachedRuntimeBuildingMetadata cached))
            return cached;

        cached = new CachedRuntimeBuildingMetadata();
        cached.HasDefinitionMetadata = TryGetBuildingDefinitionMetadata(prefab, out cached.DefinitionMetadata);

        if (TryGetFootprintFromVisualBounds(prefab, out Vector2Int visualFootprint))
        {
            cached.HasVisualFootprint = true;
            cached.VisualFootprint = visualFootprint;
        }

        cached.HasLocalBounds = TryGetPrefabLocalBounds(prefab, out cached.LocalBounds);
        cached.HasRunway = runwaySystem != null &&
            runwaySystem.TryGetRunwayLocalData(prefab, out cached.RunwayLocalPosition, out cached.RunwayLocalRotation, out cached.RunwayHalfExtents);
        cached.ProductionSpawnLocalPositions = FindProductionSpawnLocalPositions(prefab);
        _runtimeBuildingMetadataCache[prefab] = cached;
        return cached;
    }

    private static void CacheBuildingBounds(BuildingDefinition definition, ObjectAction destroyObject)
    {
        if (definition == null || definition.HasLocalBounds || (definition.VisualTemplate == null && definition.Prefab == null))
            return;

        GameObject temp = definition.VisualTemplate != null
            ? Object.Instantiate(definition.VisualTemplate)
            : Object.Instantiate(definition.Prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        if (TryGetLocalBounds(temp, out Bounds localBounds))
        {
            definition.LocalBounds = localBounds;
            definition.HasLocalBounds = true;
        }

        destroyObject?.Invoke(temp);
    }

    private static void CleanupCombinedVisualTemplate(BuildingDefinition definition, ObjectAction destroyObject)
    {
        if (definition == null)
            return;

        if (definition.VisualTemplate != null)
            destroyObject?.Invoke(definition.VisualTemplate);

        if (definition.GeneratedMeshes != null)
        {
            for (int i = 0; i < definition.GeneratedMeshes.Count; i++)
            {
                Mesh mesh = definition.GeneratedMeshes[i];
                if (mesh != null)
                    destroyObject?.Invoke(mesh);
            }
        }

        definition.VisualTemplate = null;
        definition.GeneratedMeshes = null;
    }

    private static void BuildCombinedVisualTemplate(BuildingDefinition definition)
    {
        if (definition == null)
            return;

        definition.VisualTemplate = null;
    }

    private static GameObject GetProductionPrefab(List<BuildingDefinition.ProductionSlotDefinition> slots, int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;

        return slots[index]?.SpawnUnitPrefab;
    }

    private static BuildingDefinition.ProductionSlotDefinition GetProductionOrFallback(
        bool hasMetadata,
        BuildingDefinitionMetadata metadata,
        int index,
        GameObject fallbackSpawnUnitPrefab)
    {
        if (hasMetadata && metadata.ProductionSpawnUnitPrefabs != null && index >= 0 && index < metadata.ProductionSpawnUnitPrefabs.Length)
        {
            GameObject configuredPrefab = metadata.ProductionSpawnUnitPrefabs[index];
            if (configuredPrefab != null)
            {
                return new BuildingDefinition.ProductionSlotDefinition
                {
                    SpawnUnitPrefab = configuredPrefab
                };
            }
        }

        return new BuildingDefinition.ProductionSlotDefinition
        {
            SpawnUnitPrefab = fallbackSpawnUnitPrefab
        };
    }

    private static List<BuildingDefinition.ProductionSlotDefinition> BuildProductionSlots(
        bool hasMetadata,
        BuildingDefinitionMetadata metadata,
        params GameObject[] fallbackSpawnUnitPrefabs)
    {
        int configuredCount = hasMetadata && metadata.ProductionSpawnUnitPrefabs != null ? metadata.ProductionSpawnUnitPrefabs.Length : 0;
        int fallbackCount = fallbackSpawnUnitPrefabs != null ? fallbackSpawnUnitPrefabs.Length : 0;
        int count = Mathf.Max(configuredCount, fallbackCount);
        var slots = new List<BuildingDefinition.ProductionSlotDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            GameObject fallback = i < fallbackCount ? fallbackSpawnUnitPrefabs[i] : null;
            BuildingDefinition.ProductionSlotDefinition slot = GetProductionOrFallback(hasMetadata, metadata, i, fallback);
            if (slot == null || slot.SpawnUnitPrefab == null)
                continue;
            slots.Add(slot);
        }

        return slots;
    }

    private void RegisterSpawnableLookupAliases(Dictionary<string, GameObject> lookup, GameObject prefab)
    {
        if (lookup == null || prefab == null)
            return;

        string prefabNameKey = NormalizeSpawnableKey(prefab.name);
        if (!string.IsNullOrEmpty(prefabNameKey))
            lookup[prefabNameKey] = prefab;

        string displayNameKey = ResolveConfiguredSpawnableLookupKey(prefab);
        if (!string.IsNullOrEmpty(displayNameKey) && displayNameKey != prefabNameKey && !lookup.ContainsKey(displayNameKey))
            lookup[displayNameKey] = prefab;
    }

    private static Vector3[] FindProductionSpawnLocalPositions(GameObject prefab)
    {
        if (prefab == null)
            return null;

        List<(int index, Vector3 position)> matches = new();
        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
                continue;

            if (!TryParseSpawnPointIndex(candidate.name, out int index))
                continue;

            matches.Add((index, candidate.localPosition));
        }

        if (matches.Count == 0)
            return null;

        matches.Sort((a, b) => a.index.CompareTo(b.index));
        Vector3[] ordered = new Vector3[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            ordered[i] = matches[i].position;
        return ordered;
    }

    private static bool TryParseSpawnPointIndex(string name, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("Spawn_", System.StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = name.Substring("Spawn_".Length);
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private static bool TryGetFootprintFromVisualBounds(GameObject prefab, out Vector2Int footprint)
    {
        footprint = default;
        if (prefab == null)
            return false;

        if (!TryGetPrefabLocalBounds(prefab, out Bounds localBounds))
            return false;

        int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(localBounds.size.x)));
        int height = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(localBounds.size.z)));
        footprint = new Vector2Int(width, height);
        return true;
    }

    private static bool TryGetLocalBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
            return false;

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;
        foreach (Renderer renderer in renderers)
        {
            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasBounds;
    }

}
