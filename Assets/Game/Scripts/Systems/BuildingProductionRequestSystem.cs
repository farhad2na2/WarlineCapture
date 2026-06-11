using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using CampRequestFailure = BuildingUiCommandSystem.CampRequestFailure;
using ProductionTransportMode = BuildingProductionSystem.ProductionTransportMode;

internal sealed class BuildingProductionRequestSystem
{
    public enum FactionUnitProductionResultCode
    {
        Queued = 0,
        MissingUnitConfig = 1,
        MissingProducerBuilding = 2,
        ProducerUnavailable = 3
    }

    public readonly struct FactionUnitProductionResult
    {
        public readonly FactionUnitProductionResultCode Code;
        public readonly string ProducerDisplayName;
        public readonly string UnitDisplayName;
        public readonly int Cost;
        public readonly int QueueCount;
        public readonly int ProducedCount;

        public FactionUnitProductionResult(
            FactionUnitProductionResultCode code,
            string producerDisplayName,
            string unitDisplayName,
            int cost,
            int queueCount,
            int producedCount)
        {
            Code = code;
            ProducerDisplayName = producerDisplayName;
            UnitDisplayName = unitDisplayName;
            Cost = cost;
            QueueCount = queueCount;
            ProducedCount = producedCount;
        }
    }

    public delegate GameObject GetProductionPrefabDelegate(BuildingDefinition definition, int index);
    public delegate bool BeginPlacementForConfiguredSpawnableDelegate(GameObject prefab);
    public delegate bool TrySpendDollarsDelegate(int amount);
    public delegate void RefundDollarsDelegate(int amount);
    public delegate void SetActivePlacementCostDelegate(int cost);
    public delegate bool TryQueuePlayerUnitDelegate(RuntimeBuildingEntity building, int productionIndex, GameObject spawnUnitPrefab);
    public delegate void SelectRuntimeBuildingDelegate(int buildingId);
    public delegate void RuntimeGameplayAction();
    public delegate void CameraFocusAction(Vector3 worldPosition);
    public delegate Vector3 ResolveBuildingFocusWorldPositionDelegate(RuntimeBuildingEntity building);
    public delegate void RecordUnitOrderedDelegate(GameObject prefab);
    public delegate void LogWarningDelegate(string message);
    public delegate int CountFactionUnitsDelegate(byte factionId, string unitId);
    public delegate bool TryGetConfiguredUnitReadModelDelegate(
        int index,
        out GameObject prefab,
        out string displayName,
        out int price,
        out bool canRequest,
        out bool isVehicle);

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly IReadOnlyList<BuildingDefinition> ConfiguredSpawnableDefinitions;
        public readonly IReadOnlyDictionary<GameObject, BuildingDefinition> ConfiguredDefinitionsByPrefab;
        public readonly IReadOnlyList<GameObject> UnitSpawnPrefabs;
        public readonly IReadOnlyDictionary<string, GameObject> UnitSpawnPrefabsByKey;
        public readonly int ResourceDollars;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingProductionSystem.QueueContext ProductionQueueContext;
        public readonly BuildingRunwaySystem RunwaySystem;
        public readonly GetProductionPrefabDelegate GetProductionPrefab;
        public readonly BuildingProductionSystem.TryGetPrefabLocalBoundsDelegate TryGetPrefabLocalBounds;
        public readonly BeginPlacementForConfiguredSpawnableDelegate BeginPlacementForConfiguredSpawnable;
        public readonly TrySpendDollarsDelegate TrySpendDollars;
        public readonly RefundDollarsDelegate RefundDollars;
        public readonly SetActivePlacementCostDelegate SetActivePlacementCost;
        public readonly TryQueuePlayerUnitDelegate TryQueuePlayerUnit;
        public readonly SelectRuntimeBuildingDelegate SelectRuntimeBuilding;
        public readonly RuntimeGameplayAction SuppressNextWorldClick;
        public readonly RuntimeGameplayAction RefreshBuildingMarkers;
        public readonly RuntimeGameplayAction ClearFocusedUnit;
        public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;
        public readonly ResolveBuildingFocusWorldPositionDelegate ResolveBuildingFocusWorldPosition;
        public readonly RecordUnitOrderedDelegate RecordUnitOrdered;
        public readonly LogWarningDelegate LogWarning;
        public readonly CountFactionUnitsDelegate CountPendingProductionsForFaction;
        public readonly CountFactionUnitsDelegate CountRuntimeProducedUnitsForFaction;
        public readonly TryGetConfiguredUnitReadModelDelegate TryGetConfiguredUnitReadModel;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            IReadOnlyList<BuildingDefinition> configuredSpawnableDefinitions,
            IReadOnlyDictionary<GameObject, BuildingDefinition> configuredDefinitionsByPrefab,
            IReadOnlyList<GameObject> unitSpawnPrefabs,
            IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
            int resourceDollars,
            BuildingProductionSystem productionSystem,
            BuildingProductionSystem.QueueContext productionQueueContext,
            BuildingRunwaySystem runwaySystem,
            GetProductionPrefabDelegate getProductionPrefab,
            BuildingProductionSystem.TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds,
            BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
            TrySpendDollarsDelegate trySpendDollars,
            RefundDollarsDelegate refundDollars,
            SetActivePlacementCostDelegate setActivePlacementCost,
            TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
            SelectRuntimeBuildingDelegate selectRuntimeBuilding,
            RuntimeGameplayAction suppressNextWorldClick,
            RuntimeGameplayAction refreshBuildingMarkers,
            RuntimeGameplayAction clearFocusedUnit,
            CameraFocusAction smoothMoveCameraGroundCenterTo,
            ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
            RecordUnitOrderedDelegate recordUnitOrdered,
            LogWarningDelegate logWarning,
            CountFactionUnitsDelegate countPendingProductionsForFaction,
            CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
            TryGetConfiguredUnitReadModelDelegate tryGetConfiguredUnitReadModel = null)
        {
            RuntimeBuildings = runtimeBuildings;
            ConfiguredSpawnableDefinitions = configuredSpawnableDefinitions;
            ConfiguredDefinitionsByPrefab = configuredDefinitionsByPrefab;
            UnitSpawnPrefabs = unitSpawnPrefabs;
            UnitSpawnPrefabsByKey = unitSpawnPrefabsByKey;
            ResourceDollars = resourceDollars;
            ProductionSystem = productionSystem;
            ProductionQueueContext = productionQueueContext;
            RunwaySystem = runwaySystem;
            GetProductionPrefab = getProductionPrefab;
            TryGetPrefabLocalBounds = tryGetPrefabLocalBounds;
            BeginPlacementForConfiguredSpawnable = beginPlacementForConfiguredSpawnable;
            TrySpendDollars = trySpendDollars;
            RefundDollars = refundDollars;
            SetActivePlacementCost = setActivePlacementCost;
            TryQueuePlayerUnit = tryQueuePlayerUnit;
            SelectRuntimeBuilding = selectRuntimeBuilding;
            SuppressNextWorldClick = suppressNextWorldClick;
            RefreshBuildingMarkers = refreshBuildingMarkers;
            ClearFocusedUnit = clearFocusedUnit;
            SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
            ResolveBuildingFocusWorldPosition = resolveBuildingFocusWorldPosition;
            RecordUnitOrdered = recordUnitOrdered;
            LogWarning = logWarning;
            CountPendingProductionsForFaction = countPendingProductionsForFaction;
            CountRuntimeProducedUnitsForFaction = countRuntimeProducedUnitsForFaction;
            TryGetConfiguredUnitReadModel = tryGetConfiguredUnitReadModel;
        }
    }

    private int _armedProductionFrame = -1;
    private RuntimeBuildingEntity _lastCampProductionFocusBuilding;
    private GameObject _lastCampProductionFocusPrefab;
    private readonly Dictionary<FixedString128Bytes, string> _unitIdStringCache = new();

    public void CreateUnitFromSelectedBuilding(Context context, int? activeBuildingId, int productionIndex, int frameCount)
    {
        if (!activeBuildingId.HasValue)
            return;

        CreateUnitFromBuilding(context, activeBuildingId.Value, productionIndex, frameCount);
    }

    public void CreateUnitFromBuilding(Context context, int buildingId, int productionIndex, int frameCount)
    {
        if (!ConsumeUiProductionArm(frameCount))
            return;

        if (context.RuntimeBuildings == null ||
            !context.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingEntity building) ||
            building?.Definition == null)
            return;

        GameObject spawnUnitPrefab = context.GetProductionPrefab?.Invoke(building.Definition, productionIndex);
        if (spawnUnitPrefab == null)
            return;

        if (!CanQueueUnitFromBuilding(context, building, spawnUnitPrefab, true))
            return;

        bool queued = context.TryQueuePlayerUnit != null && context.TryQueuePlayerUnit(building, productionIndex, spawnUnitPrefab);
        if (!queued)
            context.LogWarning?.Invoke($"Unable to create a unit for the selected building '{building.Definition.DisplayName}'.");
    }

    public CampRequestFailure GetCampRequestFailure(Context context, GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        requiredBuildingDisplayName = string.Empty;
        if (prefab == null)
            return CampRequestFailure.InvalidSelection;

        int normalizedPrice = Mathf.Max(0, price);
        if (context.ResourceDollars < normalizedPrice)
            return CampRequestFailure.NotEnoughMoney;

        if (context.ConfiguredDefinitionsByPrefab != null && context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab))
            return CampRequestFailure.None;

        if (TryFindFirstFriendlyProducerBuilding(context, prefab, out _, out _, out _))
            return CampRequestFailure.None;

        TryGetRequiredProducerDisplayName(context, prefab, out requiredBuildingDisplayName);
        return CampRequestFailure.MissingProducerBuilding;
    }

    public CampRequestFailure TryRequestCampItem(
        Context context,
        GameObject prefab,
        int price,
        bool focusProducerOnSuccess,
        int frameCount,
        out string requiredBuildingDisplayName)
    {
        CampRequestFailure failure = GetCampRequestFailure(context, prefab, price, out requiredBuildingDisplayName);
        if (failure != CampRequestFailure.None)
            return failure;

        if (context.ConfiguredDefinitionsByPrefab != null && context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab))
        {
            if (context.BeginPlacementForConfiguredSpawnable == null || !context.BeginPlacementForConfiguredSpawnable(prefab))
                return CampRequestFailure.InvalidSelection;

            context.SetActivePlacementCost?.Invoke(Mathf.Max(0, price));
            return CampRequestFailure.None;
        }

        if (!TryFindFirstFriendlyProducerBuilding(context, prefab, out int producerBuildingId, out int productionIndex, out _))
        {
            TryGetRequiredProducerDisplayName(context, prefab, out requiredBuildingDisplayName);
            return CampRequestFailure.MissingProducerBuilding;
        }

        if (context.TrySpendDollars == null || !context.TrySpendDollars(price))
            return CampRequestFailure.NotEnoughMoney;

        if (context.RuntimeBuildings == null ||
            !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingEntity producerBuilding) ||
            producerBuilding == null)
        {
            context.RefundDollars?.Invoke(Mathf.Max(0, price));
            return CampRequestFailure.InvalidSelection;
        }

        if (focusProducerOnSuccess)
            SelectBuildingForProductionRequest(context, producerBuilding, prefab);
        else
            RememberCampProductionFocus(producerBuilding, prefab);

        ArmNextProductionFromUi(frameCount);
        CreateUnitFromBuilding(context, producerBuildingId, productionIndex, frameCount);
        context.RecordUnitOrdered?.Invoke(prefab);
        return CampRequestFailure.None;
    }

    public void FocusLastCampProductionRequest(Context context)
    {
        if (_lastCampProductionFocusBuilding == null || _lastCampProductionFocusPrefab == null)
            return;

        SelectBuildingForProductionRequest(context, _lastCampProductionFocusBuilding, _lastCampProductionFocusPrefab);
        _lastCampProductionFocusBuilding = null;
        _lastCampProductionFocusPrefab = null;
    }

    public void ArmNextProductionFromUi(int frameCount)
    {
        _armedProductionFrame = frameCount;
    }

    public bool CanCreateUnitFromSelectedBuilding(Context context, int? activeBuildingId, int productionIndex)
    {
        if (!activeBuildingId.HasValue ||
            context.RuntimeBuildings == null ||
            !context.RuntimeBuildings.TryGetValue(activeBuildingId.Value, out RuntimeBuildingEntity building) ||
            building?.Definition == null)
            return false;

        GameObject spawnUnitPrefab = context.GetProductionPrefab?.Invoke(building.Definition, productionIndex);
        return spawnUnitPrefab != null && CanQueueUnitFromBuilding(context, building, spawnUnitPrefab, false);
    }

    public bool CanQueueUnitFromBuilding(Context context, RuntimeBuildingEntity building, GameObject spawnUnitPrefab, bool logReason)
    {
        if (building == null || spawnUnitPrefab == null)
            return false;

        BuildingProductionSystem.ProductionTransportSettings transportSettings = context.ProductionSystem.ResolveProductionTransportSettings(
            spawnUnitPrefab,
            context.UnitSpawnPrefabs,
            context.UnitSpawnPrefabsByKey,
            context.TryGetPrefabLocalBounds);

        return CanQueueUnitFromBuilding(context, building, spawnUnitPrefab, transportSettings, logReason);
    }

    private bool CanQueueUnitFromBuilding(
        Context context,
        RuntimeBuildingEntity building,
        GameObject spawnUnitPrefab,
        BuildingProductionSystem.ProductionTransportSettings transportSettings,
        bool logReason)
    {
        if (building == null || spawnUnitPrefab == null)
            return false;

        return CanQueueTransportForAnyProducer(context, spawnUnitPrefab, transportSettings, logReason);
    }

    private static bool CanQueueTransportForAnyProducer(
        Context context,
        GameObject spawnUnitPrefab,
        BuildingProductionSystem.ProductionTransportSettings transportSettings,
        bool logReason)
    {
        if (transportSettings.TransportPrefab == null)
            return true;

        if (transportSettings.RequiresAirportRunway &&
            transportSettings.Mode == ProductionTransportMode.Plane &&
            (context.RuntimeBuildings == null ||
             context.RunwaySystem == null ||
             !context.RunwaySystem.HasAvailableAirportRunway(context.RuntimeBuildings)))
        {
            if (logReason)
                context.LogWarning?.Invoke($"[BuildingSpawn] No airport runway is available for '{spawnUnitPrefab.name}'.");
            return false;
        }

        return true;
    }

    public bool QueueFactionUnitProductionRequest(
        Context context,
        byte factionId,
        string unitId,
        EntityManager entityManager,
        float now,
        ref BuildingFactionUnitProductionRequest request,
        bool unitIdIsNormalized = false)
    {
        if (!TryResolveConfiguredUnit(context, unitId, unitIdIsNormalized, out GameObject unitPrefab, out string unitDisplayName, out int unitPrice, out bool canRequest) ||
            unitPrefab == null ||
            !canRequest)
        {
            request.ResultCode = BuildingFactionUnitProductionRequest.MissingUnitConfig;
            request.ProducerDisplayName = ToFixedString128(string.Empty);
            request.UnitDisplayName = ToFixedString128(string.IsNullOrWhiteSpace(unitDisplayName) ? unitId : unitDisplayName);
            request.Cost = 0;
            return false;
        }

        request.UnitDisplayName = ToFixedString128(unitDisplayName);
        request.Cost = Mathf.Max(0, unitPrice);

        if (!TryFindFirstFactionProducerBuilding(context, factionId, unitPrefab, out int producerBuildingId, out int productionIndex, out string producerDisplayName))
        {
            request.ResultCode = BuildingFactionUnitProductionRequest.MissingProducerBuilding;
            request.ProducerDisplayName = ToFixedString128(string.Empty);
            return false;
        }

        request.ProducerDisplayName = ToFixedString128(producerDisplayName);
        if (context.RuntimeBuildings == null ||
            !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingEntity producerBuilding) ||
            producerBuilding == null)
        {
            request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;
            return false;
        }

        if (context.ProductionSystem == null ||
            !context.ProductionSystem.TryQueuePlayerUnitFromBuilding(
                context.ProductionQueueContext,
                producerBuilding,
                productionIndex,
                unitPrefab,
                entityManager,
                now))
        {
            request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;
            return false;
        }

        request.ResultCode = 0;
        return true;
    }

    public string GetCachedUnitIdString(FixedString128Bytes unitId)
    {
        if (unitId.Length == 0)
            return string.Empty;

        if (_unitIdStringCache.TryGetValue(unitId, out string cached))
            return cached;

        cached = unitId.ToString();
        _unitIdStringCache[unitId] = cached;
        return cached;
    }

    public bool TryQueueFactionUnitProduction(Context context, byte factionId, string unitId, out FactionUnitProductionResult result)
    {
        result = default;
        if (!TryResolveConfiguredUnit(context, unitId, false, out GameObject unitPrefab, out string unitDisplayName, out int unitPrice, out bool canRequest) ||
            unitPrefab == null ||
            !canRequest)
        {
            result = new FactionUnitProductionResult(FactionUnitProductionResultCode.MissingUnitConfig, string.Empty, unitId, 0, 0, 0);
            return false;
        }

        int cost = Mathf.Max(0, unitPrice);
        if (!TryFindFirstFactionProducerBuilding(context, factionId, unitPrefab, out int producerBuildingId, out int productionIndex, out string producerDisplayName))
        {
            result = new FactionUnitProductionResult(
                FactionUnitProductionResultCode.MissingProducerBuilding,
                string.Empty,
                unitDisplayName,
                cost,
                0,
                CountRuntimeProducedUnitsForFaction(context, factionId, unitId));
            return false;
        }

        if (context.RuntimeBuildings == null ||
            !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingEntity producerBuilding) ||
            producerBuilding == null)
        {
            result = new FactionUnitProductionResult(
                FactionUnitProductionResultCode.ProducerUnavailable,
                producerDisplayName,
                unitDisplayName,
                cost,
                0,
                CountRuntimeProducedUnitsForFaction(context, factionId, unitId));
            return false;
        }

        if (context.TryQueuePlayerUnit == null ||
            !context.TryQueuePlayerUnit(producerBuilding, productionIndex, unitPrefab))
        {
            result = new FactionUnitProductionResult(
                FactionUnitProductionResultCode.ProducerUnavailable,
                producerDisplayName,
                unitDisplayName,
                cost,
                CountPendingProductionsForFaction(context, factionId, unitId),
                CountRuntimeProducedUnitsForFaction(context, factionId, unitId));
            return false;
        }

        result = new FactionUnitProductionResult(
            FactionUnitProductionResultCode.Queued,
            producerDisplayName,
            unitDisplayName,
            cost,
            CountPendingProductionsForFaction(context, factionId, unitId),
            CountRuntimeProducedUnitsForFaction(context, factionId, unitId));
        return true;
    }

    public bool TryFindFirstFriendlyProducerBuilding(Context context, GameObject unitPrefab, out int buildingId, out int productionIndex, out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;
        if (unitPrefab == null || context.RuntimeBuildings == null || context.GetProductionPrefab == null)
            return false;

        BuildingProductionSystem.ProductionTransportSettings transportSettings =
            ResolveProductionTransportSettings(context, unitPrefab);
        if (!CanQueueTransportForAnyProducer(context, unitPrefab, transportSettings, false))
            return false;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                {
                    if (TryUseFriendlyProducerBuilding(context, unitPrefab, pass, pair, out buildingId, out productionIndex, out buildingDisplayName))
                        return true;
                }
            }

            return false;
        }

        for (int pass = 0; pass < 2; pass++)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                if (TryUseFriendlyProducerBuilding(context, unitPrefab, pass, pair, out buildingId, out productionIndex, out buildingDisplayName))
                    return true;
            }
        }

        return false;
    }

    private bool TryUseFriendlyProducerBuilding(
        Context context,
        GameObject unitPrefab,
        int pass,
        KeyValuePair<int, RuntimeBuildingEntity> pair,
        out int buildingId,
        out int productionIndex,
        out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;

        RuntimeBuildingEntity building = pair.Value;
        if (building?.Definition == null || building.IsDestroyed)
            return false;
        if (building.IsCityGenerated)
            return false;
        if (!IsFriendlyProducerBuildingForPass(building, pass))
            return false;

        int productionCount = GetProductionCount(building.Definition);
        for (int i = 0; i < productionCount; i++)
        {
            if (context.GetProductionPrefab(building.Definition, i) != unitPrefab)
                continue;
            buildingId = pair.Key;
            productionIndex = i;
            buildingDisplayName = building.Definition.DisplayName ?? string.Empty;
            return true;
        }

        return false;
    }

    private bool TryUseFactionProducerBuilding(
        Context context,
        byte factionId,
        GameObject unitPrefab,
        KeyValuePair<int, RuntimeBuildingEntity> pair,
        out int buildingId,
        out int productionIndex,
        out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;

        RuntimeBuildingEntity building = pair.Value;
        if (building?.Definition == null || building.IsDestroyed)
            return false;
        if (building.IsCityGenerated)
            return false;
        if (!building.HasOwnerFaction || building.OwnerFactionId != factionId)
            return false;

        int productionCount = GetProductionCount(building.Definition);
        for (int i = 0; i < productionCount; i++)
        {
            if (context.GetProductionPrefab(building.Definition, i) != unitPrefab)
                continue;
            buildingId = pair.Key;
            productionIndex = i;
            buildingDisplayName = building.Definition.DisplayName ?? string.Empty;
            return true;
        }

        return false;
    }

    private static bool IsFriendlyProducerBuildingForPass(RuntimeBuildingEntity building, int pass)
    {
        if (building == null)
            return false;

        if (building.HasOwnerFaction && building.OwnerFactionId == FactionIdentitySystem.PlayerFactionId)
            return pass == 0;

        if (!building.HasOwnerFaction || building.OwnerFactionId == FactionIdentitySystem.NeutralFactionId)
            return pass == 1;

        return false;
    }

    private bool TryFindFirstFactionProducerBuilding(Context context, byte factionId, GameObject unitPrefab, out int buildingId, out int productionIndex, out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;
        if (unitPrefab == null || context.RuntimeBuildings == null || context.GetProductionPrefab == null)
            return false;

        BuildingProductionSystem.ProductionTransportSettings transportSettings =
            ResolveProductionTransportSettings(context, unitPrefab);
        if (!CanQueueTransportForAnyProducer(context, unitPrefab, transportSettings, false))
            return false;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
            {
                if (TryUseFactionProducerBuilding(context, factionId, unitPrefab, pair, out buildingId, out productionIndex, out buildingDisplayName))
                    return true;
            }

            return false;
        }

        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
        {
            if (TryUseFactionProducerBuilding(context, factionId, unitPrefab, pair, out buildingId, out productionIndex, out buildingDisplayName))
                return true;
        }

        return false;
    }

    private static BuildingProductionSystem.ProductionTransportSettings ResolveProductionTransportSettings(
        Context context,
        GameObject unitPrefab)
    {
        return context.ProductionSystem.ResolveProductionTransportSettings(
            unitPrefab,
            context.UnitSpawnPrefabs,
            context.UnitSpawnPrefabsByKey,
            context.TryGetPrefabLocalBounds);
    }

    private static int CountPendingProductionsForFaction(Context context, byte factionId, string unitId)
    {
        return context.CountPendingProductionsForFaction != null
            ? context.CountPendingProductionsForFaction(factionId, unitId)
            : 0;
    }

    private static int CountRuntimeProducedUnitsForFaction(Context context, byte factionId, string unitId)
    {
        return context.CountRuntimeProducedUnitsForFaction != null
            ? context.CountRuntimeProducedUnitsForFaction(factionId, unitId)
            : 0;
    }

    private static bool TryResolveConfiguredUnit(
        Context context,
        string unitId,
        bool unitIdIsNormalized,
        out GameObject unitPrefab,
        out string displayName,
        out int price,
        out bool canRequest)
    {
        unitPrefab = null;
        displayName = unitId ?? string.Empty;
        price = 0;
        canRequest = false;

        string normalized = unitIdIsNormalized ? unitId : BuildingDefinitionSystem.NormalizeSpawnableKey(unitId);
        if (string.IsNullOrEmpty(normalized))
            return false;

        if (context.UnitSpawnPrefabsByKey != null &&
            context.UnitSpawnPrefabsByKey.TryGetValue(normalized, out unitPrefab) &&
            unitPrefab != null)
        {
            return TryBuildConfiguredUnit(context, unitPrefab, out displayName, out price, out canRequest);
        }

        if (context.UnitSpawnPrefabs == null)
            return false;

        for (int i = 0; i < context.UnitSpawnPrefabs.Count; i++)
        {
            GameObject candidate = context.UnitSpawnPrefabs[i];
            if (candidate == null || !ConfiguredUnitMatchesId(context, i, candidate, normalized))
                continue;

            unitPrefab = candidate;
            return TryBuildConfiguredUnit(context, candidate, out displayName, out price, out canRequest);
        }

        return false;
    }

    private static bool ConfiguredUnitMatchesId(Context context, int index, GameObject prefab, string normalizedUnitId)
    {
        if (string.IsNullOrEmpty(normalizedUnitId))
            return true;
        if (prefab == null)
            return false;

        if (BuildingDefinitionSystem.NormalizeSpawnableKey(prefab.name) == normalizedUnitId)
            return true;

        return context.TryGetConfiguredUnitReadModel != null &&
               context.TryGetConfiguredUnitReadModel(
                   index,
                   out _,
                   out string displayName,
                   out _,
                   out _,
                   out _) &&
               BuildingDefinitionSystem.NormalizeSpawnableKey(displayName) == normalizedUnitId;
    }

    private static bool TryBuildConfiguredUnit(Context context, GameObject prefab, out string displayName, out int price, out bool canRequest)
    {
        displayName = prefab != null ? prefab.name : string.Empty;
        price = 0;
        canRequest = false;
        if (prefab == null)
            return false;

        if (context.UnitSpawnPrefabs != null && context.TryGetConfiguredUnitReadModel != null)
        {
            for (int i = 0; i < context.UnitSpawnPrefabs.Count; i++)
            {
                if (context.UnitSpawnPrefabs[i] != prefab)
                    continue;

                if (context.TryGetConfiguredUnitReadModel(
                        i,
                        out GameObject readModelPrefab,
                        out string readModelDisplayName,
                        out int readModelPrice,
                        out bool readModelCanRequest,
                        out _) &&
                    readModelPrefab == prefab)
                {
                    displayName = readModelDisplayName;
                    price = Mathf.Max(0, readModelPrice);
                    canRequest = readModelCanRequest;
                    return true;
                }

                break;
            }
        }

        displayName = prefab.name;
        price = 10000;
        canRequest = true;
        return true;
    }

    private static FixedString128Bytes ToFixedString128(string value)
    {
        return new FixedString128Bytes(value ?? string.Empty);
    }

    public bool TryGetRequiredProducerDisplayName(Context context, GameObject unitPrefab, out string buildingDisplayName)
    {
        buildingDisplayName = string.Empty;
        if (unitPrefab == null || context.ConfiguredSpawnableDefinitions == null || context.GetProductionPrefab == null)
            return false;

        for (int i = 0; i < context.ConfiguredSpawnableDefinitions.Count; i++)
        {
            BuildingDefinition definition = context.ConfiguredSpawnableDefinitions[i];
            if (definition == null)
                continue;

            int productionCount = GetProductionCount(definition);
            for (int productionIndex = 0; productionIndex < productionCount; productionIndex++)
            {
                if (context.GetProductionPrefab(definition, productionIndex) != unitPrefab)
                    continue;

                buildingDisplayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? "Building" : definition.DisplayName;
                return true;
            }
        }

        return false;
    }

    private bool ConsumeUiProductionArm(int frameCount)
    {
        if (_armedProductionFrame != frameCount)
            return false;

        _armedProductionFrame = -1;
        return true;
    }

    private void SelectBuildingForProductionRequest(Context context, RuntimeBuildingEntity building, GameObject producedUnitPrefab)
    {
        if (building == null)
            return;

        context.SelectRuntimeBuilding?.Invoke(building.Id);
        context.SuppressNextWorldClick?.Invoke();
        context.RefreshBuildingMarkers?.Invoke();
        context.ClearFocusedUnit?.Invoke();

        Vector3 focusWorldPosition = ResolveProductionRequestFocusWorldPosition(context, building, producedUnitPrefab);
        context.SmoothMoveCameraGroundCenterTo?.Invoke(focusWorldPosition);
    }

    private void RememberCampProductionFocus(RuntimeBuildingEntity building, GameObject producedUnitPrefab)
    {
        _lastCampProductionFocusBuilding = building;
        _lastCampProductionFocusPrefab = producedUnitPrefab;
    }

    private Vector3 ResolveProductionRequestFocusWorldPosition(Context context, RuntimeBuildingEntity producerBuilding, GameObject producedUnitPrefab)
    {
        if (producerBuilding == null)
            return Vector3.zero;

        BuildingProductionSystem.ProductionTransportSettings transportSettings = context.ProductionSystem.ResolveProductionTransportSettings(
            producedUnitPrefab,
            context.UnitSpawnPrefabs,
            context.UnitSpawnPrefabsByKey,
            context.TryGetPrefabLocalBounds);

        if (transportSettings.Mode == ProductionTransportMode.Plane &&
            transportSettings.RequiresAirportRunway &&
            context.RuntimeBuildings != null &&
            context.RunwaySystem != null &&
            context.RunwaySystem.TryGetNearestAirportRunway(
                context.RuntimeBuildings,
                producerBuilding.Instance != null ? producerBuilding.Instance.transform.position : Vector3.zero,
                out _,
                out Vector3 runwayCenter,
                out _,
                out _))
        {
            runwayCenter.y = 0f;
            return runwayCenter;
        }

        return context.ResolveBuildingFocusWorldPosition != null
            ? context.ResolveBuildingFocusWorldPosition(producerBuilding)
            : Vector3.zero;
    }

    private static int GetProductionCount(BuildingDefinition definition)
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
}
