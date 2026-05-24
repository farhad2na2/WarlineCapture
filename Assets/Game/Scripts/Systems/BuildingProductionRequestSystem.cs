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
    public delegate bool TryQueuePlayerUnitDelegate(RuntimeBuildingData building, int productionIndex, GameObject spawnUnitPrefab);
    public delegate void SelectRuntimeBuildingDelegate(int buildingId);
    public delegate void RuntimeGameplayAction();
    public delegate void CameraFocusAction(Vector3 worldPosition);
    public delegate Vector3 ResolveBuildingFocusWorldPositionDelegate(RuntimeBuildingData building);
    public delegate void RecordUnitOrderedDelegate(GameObject prefab);
    public delegate void LogWarningDelegate(string message);

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
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

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
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
            LogWarningDelegate logWarning)
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
        }
    }

    private int _armedProductionFrame = -1;
    private RuntimeBuildingData _lastCampProductionFocusBuilding;
    private GameObject _lastCampProductionFocusPrefab;

    public void CreateUnitFromBuilding(Context context, int buildingId, int productionIndex, int frameCount)
    {
        if (!ConsumeUiProductionArm(frameCount))
            return;

        if (context.RuntimeBuildings == null ||
            !context.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingData building) ||
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
            !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingData producerBuilding) ||
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
            !context.RuntimeBuildings.TryGetValue(activeBuildingId.Value, out RuntimeBuildingData building) ||
            building?.Definition == null)
            return false;

        GameObject spawnUnitPrefab = context.GetProductionPrefab?.Invoke(building.Definition, productionIndex);
        return spawnUnitPrefab != null && CanQueueUnitFromBuilding(context, building, spawnUnitPrefab, false);
    }

    public bool CanQueueUnitFromBuilding(Context context, RuntimeBuildingData building, GameObject spawnUnitPrefab, bool logReason)
    {
        if (building == null || spawnUnitPrefab == null)
            return false;

        BuildingProductionSystem.ProductionTransportSettings transportSettings = context.ProductionSystem.ResolveProductionTransportSettings(
            spawnUnitPrefab,
            context.UnitSpawnPrefabs,
            context.UnitSpawnPrefabsByKey,
            context.TryGetPrefabLocalBounds);

        if (transportSettings.TransportPrefab == null)
            return true;

        if (transportSettings.RequiresAirportRunway &&
            transportSettings.Mode == ProductionTransportMode.Plane &&
            (context.RuntimeBuildings == null ||
             context.RunwaySystem == null ||
             !context.RunwaySystem.TryGetNearestAirportRunway(
                 context.RuntimeBuildings,
                 building.Instance != null ? building.Instance.transform.position : Vector3.zero,
                 out _,
                 out _,
                 out _,
                 out _)))
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
        ref BuildingFactionUnitProductionRequest request)
    {
        if (!TryResolveConfiguredUnit(context, unitId, out GameObject unitPrefab, out string unitDisplayName, out int unitPrice, out bool canRequest) ||
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
            !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingData producerBuilding) ||
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

    public bool TryFindFirstFriendlyProducerBuilding(Context context, GameObject unitPrefab, out int buildingId, out int productionIndex, out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;
        if (unitPrefab == null || context.RuntimeBuildings == null || context.GetProductionPrefab == null)
            return false;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;
            if (building.IsCityGenerated)
                continue;
            if (building.HasOwnerFaction && building.OwnerFactionId != 0)
                continue;

            int productionCount = GetProductionCount(building.Definition);
            for (int i = 0; i < productionCount; i++)
            {
                if (context.GetProductionPrefab(building.Definition, i) != unitPrefab)
                    continue;
                if (!CanQueueUnitFromBuilding(context, building, unitPrefab, false))
                    continue;

                buildingId = pair.Key;
                productionIndex = i;
                buildingDisplayName = building.Definition.DisplayName ?? string.Empty;
                return true;
            }
        }

        return false;
    }

    private bool TryFindFirstFactionProducerBuilding(Context context, byte factionId, GameObject unitPrefab, out int buildingId, out int productionIndex, out string buildingDisplayName)
    {
        buildingId = 0;
        productionIndex = -1;
        buildingDisplayName = string.Empty;
        if (unitPrefab == null || context.RuntimeBuildings == null || context.GetProductionPrefab == null)
            return false;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;
            if (building.IsCityGenerated)
                continue;
            if (!building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;

            int productionCount = GetProductionCount(building.Definition);
            for (int i = 0; i < productionCount; i++)
            {
                if (context.GetProductionPrefab(building.Definition, i) != unitPrefab)
                    continue;
                if (!CanQueueUnitFromBuilding(context, building, unitPrefab, false))
                    continue;

                buildingId = pair.Key;
                productionIndex = i;
                buildingDisplayName = building.Definition.DisplayName ?? string.Empty;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveConfiguredUnit(
        Context context,
        string unitId,
        out GameObject unitPrefab,
        out string displayName,
        out int price,
        out bool canRequest)
    {
        unitPrefab = null;
        displayName = unitId ?? string.Empty;
        price = 0;
        canRequest = false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(unitId);
        if (string.IsNullOrEmpty(normalized))
            return false;

        if (context.UnitSpawnPrefabsByKey != null &&
            context.UnitSpawnPrefabsByKey.TryGetValue(normalized, out unitPrefab) &&
            unitPrefab != null)
        {
            return TryBuildConfiguredUnit(unitPrefab, out displayName, out price, out canRequest);
        }

        if (context.UnitSpawnPrefabs == null)
            return false;

        for (int i = 0; i < context.UnitSpawnPrefabs.Count; i++)
        {
            GameObject candidate = context.UnitSpawnPrefabs[i];
            if (candidate == null || !BuildingDefinitionSystem.UnitPrefabMatchesId(candidate, normalized))
                continue;

            unitPrefab = candidate;
            return TryBuildConfiguredUnit(candidate, out displayName, out price, out canRequest);
        }

        return false;
    }

    private static bool TryBuildConfiguredUnit(GameObject prefab, out string displayName, out int price, out bool canRequest)
    {
        displayName = prefab != null ? prefab.name : string.Empty;
        price = 0;
        canRequest = false;
        if (prefab == null)
            return false;

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        displayName = ResolveConfiguredUnitDisplayName(prefab, authoring);
        price = authoring != null ? Mathf.Max(0, authoring.Price) : 10000;
        canRequest = authoring == null || authoring.CanRequest;
        return true;
    }

    private static string ResolveConfiguredUnitDisplayName(GameObject prefab, UnitGridAuthoring authoring)
    {
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return authoring.ConfiguredDisplayName;
        return prefab != null ? prefab.name : "Unit";
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

    private void SelectBuildingForProductionRequest(Context context, RuntimeBuildingData building, GameObject producedUnitPrefab)
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

    private void RememberCampProductionFocus(RuntimeBuildingData building, GameObject producedUnitPrefab)
    {
        _lastCampProductionFocusBuilding = building;
        _lastCampProductionFocusPrefab = producedUnitPrefab;
    }

    private Vector3 ResolveProductionRequestFocusWorldPosition(Context context, RuntimeBuildingData producerBuilding, GameObject producedUnitPrefab)
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
