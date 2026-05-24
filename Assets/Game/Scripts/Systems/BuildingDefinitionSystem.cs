using System.Collections.Generic;
using System.Globalization;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using UnityEngine;
using ConfiguredSpawnableEntry = BuildingUiCommandSystem.ConfiguredSpawnableEntry;
using ConfiguredUnitEntry = BuildingUiCommandSystem.ConfiguredUnitEntry;

internal sealed class BuildingDefinitionSystem
{
    public delegate void ObjectAction(UnityEngine.Object target);

    private sealed class CachedRuntimeBuildingMetadata
    {
        public BuildingDefinitionAuthoring Authoring;
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

    public IReadOnlyDictionary<string, GameObject> UnitSpawnPrefabsByKey => _unitSpawnPrefabsByKey;
    public IReadOnlyList<GameObject> ConfiguredSpawnablePrefabs => _configuredSpawnablePrefabs;
    public IReadOnlyList<GameObject> ConfiguredUnitSpawnPrefabs => _configuredUnitSpawnPrefabs;
    public IReadOnlyList<BuildingDefinition> ConfiguredSpawnableDefinitions => _configuredSpawnableDefinitions;
    public IReadOnlyDictionary<GameObject, BuildingDefinition> ConfiguredDefinitionsByPrefab => _configuredDefinitionsByPrefab;
    public int ConfiguredSpawnableCount => _configuredSpawnableDefinitions.Count;
    public int ConfiguredUnitCount => _configuredUnitSpawnPrefabs.Count;

    public void RebuildSpawnablesLookup(IReadOnlyList<GameObject> spawnables, IReadOnlyList<GameObject> unitSpawnPrefabs)
    {
        _spawnablesByKey.Clear();
        _unitSpawnPrefabsByKey.Clear();
        _configuredSpawnablePrefabs.Clear();
        _configuredUnitSpawnPrefabs.Clear();

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
                    _configuredUnitSpawnPrefabs.Add(prefab);
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
        }
    }

    public void ClearConfiguredSpawnableDefinitions(ObjectAction destroyObject)
    {
        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
            CleanupCombinedVisualTemplate(_configuredSpawnableDefinitions[i], destroyObject);

        _configuredSpawnableDefinitions.Clear();
        _configuredDefinitionsByPrefab.Clear();
        _runtimeBuildingMetadataCache.Clear();
    }

    public void ClearConfiguredPrefabLookups()
    {
        _spawnablesByKey.Clear();
        _unitSpawnPrefabsByKey.Clear();
        _configuredSpawnablePrefabs.Clear();
        _configuredUnitSpawnPrefabs.Clear();
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
        if (TryGetConfiguredDefinition(index, out BuildingDefinition definition))
        {
            entry = BuildConfiguredSpawnableEntry(definition);
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
            _configuredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition matchedDefinition))
        {
            entry = BuildConfiguredSpawnableEntry(matchedDefinition);
            return true;
        }

        for (int i = 0; i < _configuredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = _configuredSpawnableDefinitions[i];
            if (definition == null || !RuntimeDefinitionMatchesId(definition, normalized))
                continue;

            entry = BuildConfiguredSpawnableEntry(definition);
            return true;
        }

        entry = default;
        return false;
    }

    public bool TryGetConfiguredUnit(int index, out ConfiguredUnitEntry entry)
    {
        if (index >= 0 && index < _configuredUnitSpawnPrefabs.Count)
        {
            GameObject prefab = _configuredUnitSpawnPrefabs[index];
            if (prefab != null)
            {
                UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
                string displayName = ResolveConfiguredUnitDisplayName(prefab, authoring);
                string description = authoring != null ? authoring.ConfiguredDescription : string.Empty;
                Vector2Int footprint = authoring != null ? authoring.GetConfiguredFootprintCells() : Vector2Int.one;
                bool isVehicle = footprint.x > 1 ||
                                 footprint.y > 1 ||
                                 prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0;
                int price = authoring != null ? authoring.Price : (isVehicle ? 15000 : 10000);
                entry = new ConfiguredUnitEntry(displayName, description, prefab, isVehicle, authoring == null || authoring.CanRequest, price);
                return true;
            }
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

        prefab = _configuredUnitSpawnPrefabs[index];
        if (prefab == null)
            return false;

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        displayName = ResolveConfiguredUnitDisplayName(prefab, authoring);
        Vector2Int footprint = authoring != null ? authoring.GetConfiguredFootprintCells() : Vector2Int.one;
        isVehicle = footprint.x > 1 || footprint.y > 1 || prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0;
        price = authoring != null ? authoring.Price : (isVehicle ? 15000 : 10000);
        canRequest = authoring == null || authoring.CanRequest;
        return true;
    }

    private static string ResolveConfiguredUnitDisplayName(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return authoring.ConfiguredDisplayName;

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
        List<BuildingDefinition.ProductionSlotDefinition> productionSlots = BuildProductionSlots(metadata.Authoring, null, null, null, null);

        return new BuildingDefinition
        {
            DisplayName = metadata.Authoring != null && !string.IsNullOrWhiteSpace(metadata.Authoring.ConfiguredDisplayName) ? metadata.Authoring.ConfiguredDisplayName : fallbackDisplayName,
            Description = metadata.Authoring != null && !string.IsNullOrWhiteSpace(metadata.Authoring.ConfiguredDescription) ? metadata.Authoring.ConfiguredDescription : fallbackDescription,
            MaxHealth = metadata.Authoring != null ? Mathf.Max(1, metadata.Authoring.ConfiguredMaxHealth) : Mathf.Max(1, fallbackMaxHealth),
            ProductionSlots = productionSlots,
            SpawnUnitPrefab = GetProductionPrefab(productionSlots, 0),
            SecondarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 1),
            TertiarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 2),
            QuaternarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 3),
            Prefab = prefab,
            FootprintCells = metadata.HasVisualFootprint
                ? metadata.VisualFootprint
                : metadata.Authoring != null
                    ? new Vector2Int(Mathf.Max(1, metadata.Authoring.ConfiguredFootprintCells.x), Mathf.Max(1, metadata.Authoring.ConfiguredFootprintCells.y))
                    : fallbackFootprint,
            Role = metadata.Authoring != null ? metadata.Authoring.ConfiguredRole : BuildingRole.None,
            IsWall = metadata.Authoring != null && metadata.Authoring.ConfiguredIsWall,
            OilBarrelsPerDay = metadata.Authoring != null ? Mathf.Max(0f, metadata.Authoring.ConfiguredOilBarrelsPerDay) : 0f,
            OilStorageCapacity = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredOilStorageCapacity) : 0,
            FuelBarrelsPerDay = metadata.Authoring != null ? Mathf.Max(0f, metadata.Authoring.ConfiguredFuelBarrelsPerDay) : 0f,
            FuelStorageCapacity = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredFuelStorageCapacity) : 0,
            RefugeeCapacity = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredRefugeeCapacity) : 0,
            RefugeeUpkeepPerCitizenPerDay = metadata.Authoring != null ? Mathf.Max(0, metadata.Authoring.ConfiguredRefugeeUpkeepPerCitizenPerDay) : 0,
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
        BuildingDefinitionAuthoring authoring = prefab != null ? prefab.GetComponent<BuildingDefinitionAuthoring>() : null;
        if (authoring != null)
            authoring.ApplyConfigIfAvailable();

        List<BuildingDefinition.ProductionSlotDefinition> productionSlots = BuildProductionSlots(
            authoring,
            fallbackPrimarySpawnUnitPrefab,
            fallbackSecondarySpawnUnitPrefab,
            fallbackTertiarySpawnUnitPrefab);

        bool hasVisualFootprint = TryGetFootprintFromVisualBounds(prefab, out Vector2Int visualFootprint);
        Vector2Int authoringFootprint = authoring != null
            ? new Vector2Int(Mathf.Max(1, authoring.ConfiguredFootprintCells.x), Mathf.Max(1, authoring.ConfiguredFootprintCells.y))
            : Vector2Int.one;
        bool hasLocalBounds = TryGetPrefabLocalBounds(prefab, out Bounds localBounds);
        Vector3 runwayLocalPosition = default;
        Quaternion runwayLocalRotation = Quaternion.identity;
        Vector3 runwayHalfExtents = default;
        bool hasRunway = runwaySystem != null &&
            runwaySystem.TryGetRunwayLocalData(prefab, out runwayLocalPosition, out runwayLocalRotation, out runwayHalfExtents);

        return new BuildingDefinition
        {
            DisplayName = authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName) ? authoring.ConfiguredDisplayName : fallbackDisplayName,
            Description = authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDescription) ? authoring.ConfiguredDescription : fallbackDescription,
            MaxHealth = authoring != null ? Mathf.Max(1, authoring.ConfiguredMaxHealth) : Mathf.Max(1, fallbackMaxHealth),
            ProductionSlots = productionSlots,
            SpawnUnitPrefab = GetProductionPrefab(productionSlots, 0),
            SecondarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 1),
            TertiarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 2),
            QuaternarySpawnUnitPrefab = GetProductionPrefab(productionSlots, 3),
            Prefab = prefab,
            FootprintCells = hasVisualFootprint ? visualFootprint : authoringFootprint,
            Role = authoring != null ? authoring.ConfiguredRole : BuildingRole.None,
            IsWall = authoring != null && authoring.ConfiguredIsWall,
            OilBarrelsPerDay = authoring != null ? Mathf.Max(0f, authoring.ConfiguredOilBarrelsPerDay) : 0f,
            OilStorageCapacity = authoring != null ? Mathf.Max(0, authoring.ConfiguredOilStorageCapacity) : 0,
            FuelBarrelsPerDay = authoring != null ? Mathf.Max(0f, authoring.ConfiguredFuelBarrelsPerDay) : 0f,
            FuelStorageCapacity = authoring != null ? Mathf.Max(0, authoring.ConfiguredFuelStorageCapacity) : 0,
            RefugeeCapacity = authoring != null ? Mathf.Max(0, authoring.ConfiguredRefugeeCapacity) : 0,
            RefugeeUpkeepPerCitizenPerDay = authoring != null ? Mathf.Max(0, authoring.ConfiguredRefugeeUpkeepPerCitizenPerDay) : 0,
            ThreatDetectionKind = authoring != null ? authoring.ConfiguredThreatDetectionKind : ThreatDetectionKind.None,
            ThreatDetectionRadiusCells = authoring != null ? Mathf.Max(0, authoring.ConfiguredThreatDetectionRadiusCells) : 0,
            LocalBounds = localBounds,
            HasLocalBounds = hasLocalBounds,
            ProductionSpawnLocalPositions = FindProductionSpawnLocalPositions(prefab),
            HasRunway = hasRunway,
            RunwayLocalPosition = runwayLocalPosition,
            RunwayLocalRotation = runwayLocalRotation,
            RunwayHalfExtents = runwayHalfExtents
        };
    }

    public static ConfiguredSpawnableEntry BuildConfiguredSpawnableEntry(BuildingDefinition definition)
    {
        if (definition == null)
            return default;

        bool canRequest = true;
        int price = 20000;
        BuildingDefinitionAuthoring authoring = definition.Prefab != null ? definition.Prefab.GetComponent<BuildingDefinitionAuthoring>() : null;
        if (authoring != null)
        {
            canRequest = authoring.ConfiguredCanRequest;
            price = authoring.ConfiguredPrice;
        }

        return new ConfiguredSpawnableEntry(definition.DisplayName, definition.Description, definition.Prefab, canRequest, price);
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

        BuildingDefinitionAuthoring authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return NormalizeSpawnableKey(authoring.ConfiguredDisplayName);

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

    public static bool RuntimeBuildingMatchesId(RuntimeBuildingData building, string normalizedBuildingId)
    {
        return building?.Definition != null && RuntimeDefinitionMatchesId(building.Definition, normalizedBuildingId);
    }

    public static bool UnitPrefabMatchesId(GameObject prefab, string normalizedUnitId)
    {
        if (string.IsNullOrEmpty(normalizedUnitId))
            return true;
        if (prefab == null)
            return false;

        if (NormalizeSpawnableKey(prefab.name) == normalizedUnitId)
            return true;

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        if (authoring != null && NormalizeSpawnableKey(authoring.ConfiguredDisplayName) == normalizedUnitId)
            return true;

        return false;
    }

    public static bool RuntimeDefinitionMatchesId(BuildingDefinition definition, string normalizedBuildingId)
    {
        if (definition == null || string.IsNullOrEmpty(normalizedBuildingId))
            return false;

        if (NormalizeSpawnableKey(definition.DisplayName) == normalizedBuildingId)
            return true;

        if (definition.Prefab != null)
        {
            if (NormalizeSpawnableKey(definition.Prefab.name) == normalizedBuildingId)
                return true;

            BuildingDefinitionAuthoring authoring = definition.Prefab.GetComponent<BuildingDefinitionAuthoring>();
            if (authoring != null && NormalizeSpawnableKey(authoring.ConfiguredDisplayName) == normalizedBuildingId)
                return true;
        }

        return false;
    }

    public static bool TryGetPrefabLocalBounds(GameObject prefab, out Bounds localBounds)
    {
        localBounds = default;
        if (prefab == null)
            return false;

        if (TryGetModelLocalBounds(prefab.transform, out localBounds))
            return true;

        return TryGetLocalBounds(prefab, out localBounds);
    }

    private CachedRuntimeBuildingMetadata GetOrCreateRuntimeBuildingMetadata(GameObject prefab, BuildingRunwaySystem runwaySystem)
    {
        if (prefab == null)
            return new CachedRuntimeBuildingMetadata();

        if (_runtimeBuildingMetadataCache.TryGetValue(prefab, out CachedRuntimeBuildingMetadata cached))
            return cached;

        cached = new CachedRuntimeBuildingMetadata
        {
            Authoring = prefab.GetComponent<BuildingDefinitionAuthoring>()
        };
        if (cached.Authoring != null)
            cached.Authoring.ApplyConfigIfAvailable();

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
        BuildingDefinitionAuthoring authoring,
        int index,
        GameObject fallbackSpawnUnitPrefab)
    {
        if (authoring != null)
        {
            BuildingDefinitionAuthoring.ProductionDefinition production = authoring.GetProductionOrDefault(index);
            if (production != null)
            {
                return new BuildingDefinition.ProductionSlotDefinition
                {
                    SpawnUnitPrefab = production.spawnUnitPrefab
                };
            }
        }

        return new BuildingDefinition.ProductionSlotDefinition
        {
            SpawnUnitPrefab = fallbackSpawnUnitPrefab
        };
    }

    private static List<BuildingDefinition.ProductionSlotDefinition> BuildProductionSlots(
        BuildingDefinitionAuthoring authoring,
        params GameObject[] fallbackSpawnUnitPrefabs)
    {
        int configuredCount = authoring != null ? Mathf.Max(0, authoring.ConfiguredProductionCount) : 0;
        int fallbackCount = fallbackSpawnUnitPrefabs != null ? fallbackSpawnUnitPrefabs.Length : 0;
        int count = Mathf.Max(configuredCount, fallbackCount);
        var slots = new List<BuildingDefinition.ProductionSlotDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            GameObject fallback = i < fallbackCount ? fallbackSpawnUnitPrefabs[i] : null;
            BuildingDefinition.ProductionSlotDefinition slot = GetProductionOrFallback(authoring, i, fallback);
            if (slot == null || slot.SpawnUnitPrefab == null)
                continue;
            slots.Add(slot);
        }

        return slots;
    }

    private static void RegisterSpawnableLookupAliases(Dictionary<string, GameObject> lookup, GameObject prefab)
    {
        if (lookup == null || prefab == null)
            return;

        string prefabNameKey = NormalizeSpawnableKey(prefab.name);
        if (!string.IsNullOrEmpty(prefabNameKey))
            lookup[prefabNameKey] = prefab;

        string displayNameKey = GetSpawnableLookupKey(prefab);
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

    private static bool TryGetModelLocalBounds(Transform root, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (root == null)
            return false;

        Transform modelRoot = root.Find("Model");
        if (modelRoot == null)
            return false;

        MeshRenderer[] renderers = modelRoot.GetComponentsInChildren<MeshRenderer>(true);
        Matrix4x4 worldToLocal = root.worldToLocalMatrix;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds localBounds = TransformRendererBounds(worldToLocal * renderer.localToWorldMatrix, renderer.localBounds);
            if (!hasBounds)
            {
                combinedBounds = localBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(localBounds);
            }
        }

        return hasBounds;
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

    private static Bounds TransformRendererBounds(Matrix4x4 matrix, Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                    min = Vector3.Min(min, transformed);
                    max = Vector3.Max(max, transformed);
                }
            }
        }

        Bounds transformedBounds = new();
        transformedBounds.SetMinMax(min, max);
        return transformedBounds;
    }
}
