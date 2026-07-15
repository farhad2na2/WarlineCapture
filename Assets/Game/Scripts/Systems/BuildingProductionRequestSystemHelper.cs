using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;

namespace Game.Runtime
{
    using CampRequestFailure = BuildingUiCommandSystemHelper.CampRequestFailure;
    using ProductionTransportMode = BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode;

    internal sealed class BuildingProductionRequestSystemHelper
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
        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
        public delegate FactionConstructionResourceMutationResult EvaluateConstructionResourcesDelegate(
            int creditsCost,
            int materialsCost);
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
            public readonly int MaxQueuedUnitProductions;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingProductionQueueCompositionSystemHelper.QueueContext ProductionQueueContext;
            public readonly BuildingRunwaySystem RunwaySystem;
            public readonly GetProductionPrefabDelegate GetProductionPrefab;
            public readonly BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate TryGetPrefabLocalBounds;
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
            public readonly TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly EvaluateConstructionResourcesDelegate EvaluateConstructionResources;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                IReadOnlyList<BuildingDefinition> configuredSpawnableDefinitions,
                IReadOnlyDictionary<GameObject, BuildingDefinition> configuredDefinitionsByPrefab,
                IReadOnlyList<GameObject> unitSpawnPrefabs,
                IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
                int resourceDollars,
                int maxQueuedUnitProductions,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingProductionQueueCompositionSystemHelper.QueueContext productionQueueContext,
                BuildingRunwaySystem runwaySystem,
                GetProductionPrefabDelegate getProductionPrefab,
                BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds,
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
                TryGetConfiguredUnitReadModelDelegate tryGetConfiguredUnitReadModel = null,
                TryGetEntityManagerDelegate tryGetEntityManager = null,
                EvaluateConstructionResourcesDelegate evaluateConstructionResources = null)
            {
                RuntimeBuildings = runtimeBuildings;
                ConfiguredSpawnableDefinitions = configuredSpawnableDefinitions;
                ConfiguredDefinitionsByPrefab = configuredDefinitionsByPrefab;
                UnitSpawnPrefabs = unitSpawnPrefabs;
                UnitSpawnPrefabsByKey = unitSpawnPrefabsByKey;
                ResourceDollars = resourceDollars;
                MaxQueuedUnitProductions = Mathf.Max(0, maxQueuedUnitProductions);
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
                TryGetEntityManager = tryGetEntityManager;
                EvaluateConstructionResources = evaluateConstructionResources;
            }
        }

        private readonly Dictionary<FixedString128Bytes, string> _unitIdStringCache = new(64);
        private readonly BuildingProductionCommandEntityCache _commandEntityCache = new();

        public int EnqueueCreateUnitFromSelectedBuilding(EntityManager em, int? activeBuildingId, int productionIndex, int frameCount)
        {
            return EnqueueUiProductionCommand(
                em,
                BuildingUiProductionCommandRequestElement.KindSelectedBuildingUnit,
                activeBuildingId ?? 0,
                productionIndex,
                frameCount);
        }

        public int EnqueueCreateUnitFromBuilding(EntityManager em, int buildingId, int productionIndex, int frameCount)
        {
            return EnqueueUiProductionCommand(
                em,
                BuildingUiProductionCommandRequestElement.KindBuildingUnit,
                buildingId,
                productionIndex,
                frameCount);
        }

        public int EnqueueCancelProduction(EntityManager em, int buildingId, int pendingProductionIndex)
        {
            return EnqueueUiProductionCommand(
                em,
                BuildingUiProductionCommandRequestElement.KindCancelProduction,
                buildingId,
                pendingProductionIndex,
                frameCount: 0);
        }

        public bool EnqueueAndProcessCreateUnitFromSelectedBuilding(
            EntityManager em,
            Context context,
            int? activeBuildingId,
            int productionIndex,
            int frameCount)
        {
            int requestId = EnqueueCreateUnitFromSelectedBuilding(em, activeBuildingId, productionIndex, frameCount);
            ProcessPendingUiProductionCommands(em, context, frameCount);
            return TryGetUiProductionCommandResult(em, requestId, out BuildingUiProductionCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool EnqueueAndProcessCreateUnitFromBuilding(
            EntityManager em,
            Context context,
            int buildingId,
            int productionIndex,
            int frameCount)
        {
            int requestId = EnqueueCreateUnitFromBuilding(em, buildingId, productionIndex, frameCount);
            ProcessPendingUiProductionCommands(em, context, frameCount);
            return TryGetUiProductionCommandResult(em, requestId, out BuildingUiProductionCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool EnqueueAndProcessCancelProduction(
            EntityManager em,
            Context context,
            int buildingId,
            int pendingProductionIndex,
            float now)
        {
            int requestId = EnqueueCancelProduction(em, buildingId, pendingProductionIndex);
            ProcessPendingUiProductionCommands(em, context, frameCount: 0, now: now);
            return TryGetUiProductionCommandResult(em, requestId, out BuildingUiProductionCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool TryGetUiProductionCommandResult(
            EntityManager em,
            int requestId,
            out BuildingUiProductionCommandResultElement result)
        {
            result = default;
            Entity queueEntity = EnsureUiProductionCommandEntity(em);
            DynamicBuffer<BuildingUiProductionCommandResultElement> results =
                em.GetBuffer<BuildingUiProductionCommandResultElement>(queueEntity);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].RequestId == requestId)
                {
                    result = results[i];
                    return true;
                }
            }

            return false;
        }

        public void ProcessPendingUiProductionCommands(EntityManager em, Context context, int frameCount, float now = 0f)
        {
            Entity queueEntity = EnsureUiProductionCommandEntity(em);
            ProcessPendingUiProductionCommands(em, context, frameCount, now, queueEntity);
        }

        public void ProcessPendingUiProductionCommandsIfPresent(EntityManager em, Context context, int frameCount, float now = 0f)
        {
            if (!TryGetUiProductionCommandEntity(em, out Entity queueEntity))
                return;

            ProcessPendingUiProductionCommands(em, context, frameCount, now, queueEntity);
        }

        private void ProcessPendingUiProductionCommands(
            EntityManager em,
            Context context,
            int frameCount,
            float now,
            Entity queueEntity)
        {
            DynamicBuffer<BuildingUiProductionCommandRequestElement> requests =
                em.GetBuffer<BuildingUiProductionCommandRequestElement>(queueEntity);
            if (requests.Length == 0)
                return;

            using NativeList<BuildingUiProductionCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
                pendingRequests.Add(requests[i]);
            requests.Clear();

            DynamicBuffer<BuildingUiProductionCommandResultElement> results =
                em.GetBuffer<BuildingUiProductionCommandResultElement>(queueEntity);
            results.Clear();

            NativeArray<BuildingUiProductionCommandRequestElement> pendingArray = pendingRequests.AsArray();
            for (int i = 0; i < pendingArray.Length; i++)
            {
                BuildingUiProductionCommandRequestElement request = pendingArray[i];
                bool queued = ProcessUiProductionCommand(context, request, frameCount, now, out byte resultCode);
                results = em.GetBuffer<BuildingUiProductionCommandResultElement>(queueEntity);
                results.Add(new BuildingUiProductionCommandResultElement
                {
                    RequestId = request.RequestId,
                    BuildingId = request.BuildingId,
                    ProductionIndex = request.ProductionIndex,
                    RequestKind = request.RequestKind,
                    Accepted = queued ? (byte)1 : (byte)0,
                    ResultCode = resultCode,
                    ReasonCode = (int)ToProductionReasonCode(resultCode)
                });

                TryEmitProductionCommandAudio(em, request.RequestKind, queued, resultCode);
            }
        }

        public static bool TryEmitProductionCommandAudio(
            EntityManager em,
            byte requestKind,
            bool accepted,
            byte resultCode)
        {
            if (!TryResolveProductionCommandAudioEvent(requestKind, accepted, resultCode, out string eventId, out uint eventHash))
                return false;

            AudioEventRequestSystem.EnqueueOneShot(
                em,
                new FixedString64Bytes(eventId),
                eventHash,
                new FixedString32Bytes("Gameplay"),
                AudioPlaybackPriority.Medium,
                Time.time,
                cooldownSeconds: 0.08f);
            return true;
        }

        public static bool TryResolveProductionCommandAudioEvent(
            byte requestKind,
            bool accepted,
            byte resultCode,
            out string eventId,
            out uint eventHash)
        {
            eventId = null;
            eventHash = 0u;

            if (requestKind == BuildingUiProductionCommandRequestElement.KindCancelProduction)
                return false;

            if (accepted && resultCode == BuildingUiProductionCommandResultElement.Queued)
            {
                eventId = AudioEventIds.GameplayProductionQueued;
                eventHash = AudioEventIds.GameplayProductionQueuedHash;
                return true;
            }

            if (!accepted)
            {
                return false;
            }

            return false;
        }

        private bool ProcessUiProductionCommand(
            Context context,
            BuildingUiProductionCommandRequestElement request,
            int frameCount,
            float now,
            out byte resultCode)
        {
            if (request.RequestKind == BuildingUiProductionCommandRequestElement.KindCancelProduction)
            {
                return TryCancelProduction(
                    context,
                    request.BuildingId,
                    request.ProductionIndex,
                    now,
                    out resultCode);
            }

            if (request.RequestKind == BuildingUiProductionCommandRequestElement.KindSelectedBuildingUnit &&
                request.BuildingId == 0)
            {
                resultCode = BuildingUiProductionCommandResultElement.MissingActiveBuilding;
                return false;
            }

            return TryCreateUnitFromBuilding(
                context,
                request.BuildingId,
                request.ProductionIndex,
                request.FrameCount,
                frameCount,
                out resultCode);
        }

        private bool TryCreateUnitFromBuilding(
            Context context,
            int buildingId,
            int productionIndex,
            int requestFrameCount,
            int frameCount,
            out byte resultCode)
        {
            if (requestFrameCount != frameCount)
            {
                resultCode = BuildingUiProductionCommandResultElement.NotArmed;
                return false;
            }

            if (context.RuntimeBuildings == null ||
                !context.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingEntity building) ||
                building?.Definition == null)
            {
                resultCode = BuildingUiProductionCommandResultElement.MissingProducerBuilding;
                return false;
            }

            GameObject spawnUnitPrefab = context.GetProductionPrefab?.Invoke(building.Definition, productionIndex);
            if (spawnUnitPrefab == null)
            {
                resultCode = BuildingUiProductionCommandResultElement.UnavailablePrefab;
                return false;
            }

            if (!HasGlobalQueueCapacity(context))
            {
                resultCode = BuildingUiProductionCommandResultElement.GlobalQueueFull;
                return false;
            }

            if (!CanQueueUnitFromBuilding(context, building, spawnUnitPrefab, true))
            {
                resultCode = BuildingUiProductionCommandResultElement.QueueFull;
                return false;
            }

            if (context.TryQueuePlayerUnit == null)
            {
                resultCode = BuildingUiProductionCommandResultElement.QueueRejected;
                return false;
            }

            bool queued = context.TryQueuePlayerUnit(building, productionIndex, spawnUnitPrefab);
            resultCode = queued
                ? BuildingUiProductionCommandResultElement.Queued
                : BuildingUiProductionCommandResultElement.QueueFull;
            if (!queued)
                context.LogWarning?.Invoke($"Unable to create a unit for the selected building '{building.Definition.DisplayName}'.");

            return queued;
        }

        private static bool TryCancelProduction(
            Context context,
            int buildingId,
            int pendingProductionIndex,
            float now,
            out byte resultCode)
        {
            if (context.RuntimeBuildings == null ||
                context.ProductionSystem == null ||
                !context.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingEntity building) ||
                building == null ||
                building.PendingProductions == null ||
                pendingProductionIndex < 0 ||
                pendingProductionIndex >= building.PendingProductions.Count)
            {
                resultCode = BuildingUiProductionCommandResultElement.MissingPendingProduction;
                return false;
            }

            if (!context.ProductionSystem.RemovePendingAt(building.PendingProductions, pendingProductionIndex))
            {
                resultCode = BuildingUiProductionCommandResultElement.CancelRejected;
                return false;
            }

            context.ProductionSystem.RebuildPendingProductionTimeline(
                building.PendingProductions,
                now,
                preserveActiveProgress: pendingProductionIndex > 0);
            resultCode = BuildingUiProductionCommandResultElement.Cancelled;
            return true;
        }

        public CampRequestFailure GetCampRequestFailure(Context context, GameObject prefab, int price, out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            if (prefab == null)
                return CampRequestFailure.InvalidSelection;

            if (context.ConfiguredDefinitionsByPrefab != null &&
                context.ConfiguredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition buildingDefinition))
            {
                int creditsCost = Mathf.Max(0, buildingDefinition?.CreditsCost ?? 0);
                int materialsCost = Mathf.Max(0, buildingDefinition?.MaterialsCost ?? 0);
                if (context.EvaluateConstructionResources == null)
                    return context.ResourceDollars < creditsCost ? CampRequestFailure.InsufficientCredits : CampRequestFailure.None;

                return BuildingCampItemCommandPolicySystemHelper.MapConstructionResourceFailure(
                    context.EvaluateConstructionResources(creditsCost, materialsCost));
            }

            int normalizedPrice = Mathf.Max(0, price);
            if (context.ResourceDollars < normalizedPrice)
                return CampRequestFailure.NotEnoughMoney;

            if (!TryFindFirstFriendlyProducerBuilding(context, prefab, requireQueueCapacity: false, out _, out _, out string producerDisplayName))
            {
                TryGetRequiredProducerDisplayName(context, prefab, out requiredBuildingDisplayName);
                return CampRequestFailure.MissingProducerBuilding;
            }

            if (!HasGlobalQueueCapacity(context))
                return CampRequestFailure.GlobalProductionQueueFull;

            if (TryFindFirstFriendlyProducerBuilding(context, prefab, requireQueueCapacity: true, out _, out _, out _))
                return CampRequestFailure.None;

            requiredBuildingDisplayName = producerDisplayName;
            return CampRequestFailure.ProductionQueueFull;
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

                return CampRequestFailure.None;
            }

            if (!HasGlobalQueueCapacity(context))
                return CampRequestFailure.GlobalProductionQueueFull;

            if (!TryFindFirstFriendlyProducerBuilding(context, prefab, requireQueueCapacity: true, out int producerBuildingId, out int productionIndex, out _))
            {
                if (TryFindFirstFriendlyProducerBuilding(context, prefab, requireQueueCapacity: false, out _, out _, out string fullProducerDisplayName))
                {
                    requiredBuildingDisplayName = fullProducerDisplayName;
                    return CampRequestFailure.ProductionQueueFull;
                }

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

            if (!TryCreateUnitFromBuilding(context, producerBuildingId, productionIndex, frameCount, frameCount, out byte resultCode))
            {
                context.RefundDollars?.Invoke(Mathf.Max(0, price));
                return resultCode switch
                {
                    BuildingUiProductionCommandResultElement.GlobalQueueFull => CampRequestFailure.GlobalProductionQueueFull,
                    BuildingUiProductionCommandResultElement.QueueFull => CampRequestFailure.ProductionQueueFull,
                    _ => CampRequestFailure.InvalidSelection
                };
            }

            context.RecordUnitOrdered?.Invoke(prefab);
            return CampRequestFailure.None;
        }

        public int EnqueueCampItemRequest(
            EntityManager em,
            GameObject prefab,
            int price,
            bool focusProducerOnSuccess)
        {
            return EnqueueCampItemRequest(
                em,
                BuildingCampItemCommandPolicySystemHelper.ResolveRequestId(prefab),
                price,
                focusProducerOnSuccess);
        }

        public int EnqueueCampItemRequest(
            EntityManager em,
            string itemId,
            int price,
            bool focusProducerOnSuccess)
        {
            Entity queueEntity = EnsureUiCampItemCommandEntity(em);
            BuildingUiCampItemCommandQueueComponent queue =
                em.GetComponentData<BuildingUiCampItemCommandQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<BuildingUiCampItemCommandRequestElement>(queueEntity).Add(new BuildingUiCampItemCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                ItemId = ToFixedString128(itemId),
                Price = Mathf.Max(0, price),
                FocusProducerOnSuccess = focusProducerOnSuccess ? (byte)1 : (byte)0
            });
            return queue.LastRequestId;
        }

        public CampRequestFailure EnqueueAndProcessCampItemRequest(
            EntityManager em,
            Context context,
            GameObject prefab,
            int price,
            bool focusProducerOnSuccess,
            int frameCount,
            out string requiredBuildingDisplayName)
        {
            int requestId = EnqueueCampItemRequest(em, prefab, price, focusProducerOnSuccess);
            ProcessPendingUiCampItemCommands(em, context, frameCount);
            if (TryGetUiCampItemCommandResult(em, requestId, out BuildingUiCampItemCommandResultElement result))
            {
                requiredBuildingDisplayName = result.RequiredBuildingDisplayName.ToString();
                return BuildingCampItemCommandPolicySystemHelper.ToRequestFailure(result.ResultCode);
            }

            requiredBuildingDisplayName = string.Empty;
            return CampRequestFailure.InvalidSelection;
        }

        public bool TryGetUiCampItemCommandResult(
            EntityManager em,
            int requestId,
            out BuildingUiCampItemCommandResultElement result)
        {
            result = default;
            Entity queueEntity = EnsureUiCampItemCommandEntity(em);
            DynamicBuffer<BuildingUiCampItemCommandResultElement> results =
                em.GetBuffer<BuildingUiCampItemCommandResultElement>(queueEntity);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].RequestId == requestId)
                {
                    result = results[i];
                    return true;
                }
            }

            return false;
        }

        public void ProcessPendingUiCampItemCommands(EntityManager em, Context context, int frameCount)
        {
            Entity queueEntity = EnsureUiCampItemCommandEntity(em);
            ProcessPendingUiCampItemCommands(em, context, frameCount, queueEntity);
        }

        public void ProcessPendingUiCampItemCommandsIfPresent(EntityManager em, Context context, int frameCount)
        {
            if (!TryGetUiCampItemCommandEntity(em, out Entity queueEntity))
                return;

            ProcessPendingUiCampItemCommands(em, context, frameCount, queueEntity);
        }

        private void ProcessPendingUiCampItemCommands(
            EntityManager em,
            Context context,
            int frameCount,
            Entity queueEntity)
        {
            DynamicBuffer<BuildingUiCampItemCommandRequestElement> requests =
                em.GetBuffer<BuildingUiCampItemCommandRequestElement>(queueEntity);
            if (requests.Length == 0)
                return;

            using NativeList<BuildingUiCampItemCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
                pendingRequests.Add(requests[i]);
            requests.Clear();

            DynamicBuffer<BuildingUiCampItemCommandResultElement> results =
                em.GetBuffer<BuildingUiCampItemCommandResultElement>(queueEntity);
            results.Clear();

            NativeArray<BuildingUiCampItemCommandRequestElement> pendingArray = pendingRequests.AsArray();
            for (int i = 0; i < pendingArray.Length; i++)
            {
                BuildingUiCampItemCommandRequestElement request = pendingArray[i];
                bool accepted = ProcessUiCampItemCommand(
                    context,
                    request,
                    frameCount,
                    out byte resultCode,
                    out FixedString128Bytes requiredBuildingDisplayName);
                results = em.GetBuffer<BuildingUiCampItemCommandResultElement>(queueEntity);
                results.Add(new BuildingUiCampItemCommandResultElement
                {
                    RequestId = request.RequestId,
                    ItemId = request.ItemId,
                    RequiredBuildingDisplayName = requiredBuildingDisplayName,
                    Price = BuildingCampItemCommandPolicySystemHelper.ResolveResultPrice(context, request),
                    Accepted = accepted ? (byte)1 : (byte)0,
                    ResultCode = resultCode,
                    ReasonCode = (int)BuildingCampItemCommandPolicySystemHelper.ToReasonCode(resultCode)
                });

                TryEmitCampItemAudio(em, accepted, resultCode);
            }
        }

        public static bool TryEmitCampItemAudio(
            EntityManager em,
            bool accepted,
            byte resultCode)
        {
            if (!TryResolveCampItemAudioEvent(accepted, resultCode, out string eventId, out uint eventHash))
                return false;

            AudioEventRequestSystem.EnqueueOneShot(
                em,
                new FixedString64Bytes(eventId),
                eventHash,
                new FixedString32Bytes("Gameplay"),
                AudioPlaybackPriority.Medium,
                Time.time,
                cooldownSeconds: 0.08f);
            return true;
        }

        public static bool TryResolveCampItemAudioEvent(
            bool accepted,
            byte resultCode,
            out string eventId,
            out uint eventHash)
        {
            eventId = null;
            eventHash = 0u;

            if (accepted && resultCode == BuildingUiCampItemCommandResultElement.ProductionQueued)
            {
                eventId = AudioEventIds.GameplayProductionQueued;
                eventHash = AudioEventIds.GameplayProductionQueuedHash;
                return true;
            }

            if (!accepted)
            {
                return false;
            }

            return false;
        }

        public bool CanCreateUnitFromSelectedBuilding(Context context, int? activeBuildingId, int productionIndex)
        {
            if (!activeBuildingId.HasValue ||
                context.RuntimeBuildings == null ||
                !context.RuntimeBuildings.TryGetValue(activeBuildingId.Value, out RuntimeBuildingEntity building) ||
                building?.Definition == null)
                return false;

            GameObject spawnUnitPrefab = context.GetProductionPrefab?.Invoke(building.Definition, productionIndex);
            return spawnUnitPrefab != null &&
                   HasGlobalQueueCapacity(context) &&
                   CanQueueUnitFromBuilding(context, building, spawnUnitPrefab, false);
        }

        public bool CanQueueUnitFromBuilding(Context context, RuntimeBuildingEntity building, GameObject spawnUnitPrefab, bool logReason)
        {
            if (building == null || spawnUnitPrefab == null)
                return false;

            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings = context.ProductionSystem.ResolveProductionTransportSettings(
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
            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings,
            bool logReason)
        {
            if (building == null || spawnUnitPrefab == null)
                return false;

            return CanQueueTransportForAnyProducer(context, spawnUnitPrefab, transportSettings, logReason) &&
                   HasAvailableProductionSlotForRequest(context, building);
        }

        private static bool CanQueueTransportForAnyProducer(
            Context context,
            GameObject spawnUnitPrefab,
            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings,
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

        private static bool HasAvailableProductionSlotForRequest(Context context, RuntimeBuildingEntity building)
        {
            if (building == null)
                return false;

            if (building.ProductionSpawnLocalPositions == null ||
                building.ProducedUnitSlots == null ||
                building.ProductionSpawnLocalPositions.Length <= 0)
            {
                return true;
            }

            int count = Mathf.Min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
            if (count <= 0)
                return false;

            if (context.ProductionSystem != null &&
                context.TryGetEntityManager != null &&
                context.TryGetEntityManager(out EntityManager entityManager) &&
                entityManager.World != null &&
                entityManager.World.IsCreated)
            {
                return context.ProductionSystem.HasAvailableProductionSlot(
                    context.ProductionQueueContext,
                    building,
                    entityManager);
            }

            for (int i = 0; i < count; i++)
            {
                if (context.ProductionQueueContext.ProductionSlotSystem != null &&
                    context.ProductionQueueContext.ProductionSlotSystem.IsProductionSlotReservedByPending(building, i))
                {
                    continue;
                }

                if (building.ProducedUnitSlots[i] != Entity.Null)
                    continue;

                return true;
            }

            return false;
        }

        private static bool HasGlobalQueueCapacity(Context context)
        {
            int limit = Mathf.Max(0, context.MaxQueuedUnitProductions);
            return limit <= 0 || CountFriendlyPendingUnitProductions(context) < limit;
        }

        private static int CountFriendlyPendingUnitProductions(Context context)
        {
            if (context.RuntimeBuildings == null)
                return 0;

            int count = 0;
            if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildings)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                    count += CountPendingProductionsForGlobalLimit(pair.Value);
                return count;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                count += CountPendingProductionsForGlobalLimit(pair.Value);

            return count;
        }

        private static int CountPendingProductionsForGlobalLimit(RuntimeBuildingEntity building)
        {
            return IsFriendlyProductionQueueOwner(building) && building.PendingProductions != null
                ? building.PendingProductions.Count
                : 0;
        }

        private static bool IsFriendlyProductionQueueOwner(RuntimeBuildingEntity building)
        {
            if (building == null || building.IsDestroyed || building.IsCityGenerated)
                return false;

            return (building.HasOwnerFaction && building.OwnerFactionId == FactionIdentity.PlayerFactionId) ||
                   !building.HasOwnerFaction ||
                   building.OwnerFactionId == FactionIdentity.NeutralFactionId;
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
            return TryFindFirstFriendlyProducerBuilding(context, unitPrefab, requireQueueCapacity: false, out buildingId, out productionIndex, out buildingDisplayName);
        }

        private bool TryFindFirstFriendlyProducerBuilding(
            Context context,
            GameObject unitPrefab,
            bool requireQueueCapacity,
            out int buildingId,
            out int productionIndex,
            out string buildingDisplayName)
        {
            buildingId = 0;
            productionIndex = -1;
            buildingDisplayName = string.Empty;
            if (unitPrefab == null || context.RuntimeBuildings == null || context.GetProductionPrefab == null)
                return false;

            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings =
                ResolveProductionTransportSettings(context, unitPrefab);
            if (!CanQueueTransportForAnyProducer(context, unitPrefab, transportSettings, false))
                return false;

            if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildings)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
                    {
                        if (TryUseFriendlyProducerBuilding(context, unitPrefab, pass, requireQueueCapacity, pair, out buildingId, out productionIndex, out buildingDisplayName))
                            return true;
                    }
                }

                return false;
            }

            for (int pass = 0; pass < 2; pass++)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                {
                    if (TryUseFriendlyProducerBuilding(context, unitPrefab, pass, requireQueueCapacity, pair, out buildingId, out productionIndex, out buildingDisplayName))
                        return true;
                }
            }

            return false;
        }

        private bool TryUseFriendlyProducerBuilding(
            Context context,
            GameObject unitPrefab,
            int pass,
            bool requireQueueCapacity,
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
                if (requireQueueCapacity && !CanQueueUnitFromBuilding(context, building, unitPrefab, false))
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

        private int EnqueueUiProductionCommand(
            EntityManager em,
            byte requestKind,
            int buildingId,
            int productionIndex,
            int frameCount)
        {
            Entity queueEntity = EnsureUiProductionCommandEntity(em);
            BuildingUiProductionCommandQueueComponent queue =
                em.GetComponentData<BuildingUiProductionCommandQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<BuildingUiProductionCommandRequestElement>(queueEntity).Add(new BuildingUiProductionCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                BuildingId = buildingId,
                ProductionIndex = productionIndex,
                FrameCount = frameCount,
                RequestKind = requestKind
            });
            return queue.LastRequestId;
        }

        private bool ProcessUiCampItemCommand(
            Context context,
            BuildingUiCampItemCommandRequestElement request,
            int frameCount,
            out byte resultCode,
            out FixedString128Bytes requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = default;
            string normalizedItemId = request.ItemId.ToString();
            if (!TryResolveCampItemPrefab(context, normalizedItemId, out GameObject prefab))
            {
                resultCode = BuildingUiCampItemCommandResultElement.InvalidSelection;
                return false;
            }

            bool isConfiguredBuilding = IsConfiguredBuildingPrefab(context, prefab);
            CampRequestFailure failure = TryRequestCampItem(
                context,
                prefab,
                request.Price,
                request.FocusProducerOnSuccess != 0,
                frameCount,
                out string requiredBuilding);
            requiredBuildingDisplayName = ToFixedString128(requiredBuilding);
            resultCode = BuildingCampItemCommandPolicySystemHelper.ToResultCode(failure, isConfiguredBuilding);
            return failure == CampRequestFailure.None;
        }

        private static bool TryResolveCampItemPrefab(Context context, string normalizedItemId, out GameObject prefab)
        {
            return TryResolveConfiguredBuildingPrefab(context, normalizedItemId, out prefab) ||
                   TryResolveConfiguredUnitPrefab(context, normalizedItemId, out prefab);
        }

        internal static bool TryResolveConfiguredBuildingPrefab(Context context, string normalizedItemId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(normalizedItemId))
                return false;

            if (context.ConfiguredDefinitionsByPrefab != null)
            {
                foreach (KeyValuePair<GameObject, BuildingDefinition> pair in context.ConfiguredDefinitionsByPrefab)
                {
                    GameObject candidatePrefab = pair.Key != null ? pair.Key : pair.Value?.Prefab;
                    if (candidatePrefab == null)
                        continue;

                    if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(candidatePrefab.name) == normalizedItemId ||
                        BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(pair.Value, normalizedItemId))
                    {
                        prefab = candidatePrefab;
                        return true;
                    }
                }
            }

            if (context.ConfiguredSpawnableDefinitions != null)
            {
                for (int i = 0; i < context.ConfiguredSpawnableDefinitions.Count; i++)
                {
                    BuildingDefinition definition = context.ConfiguredSpawnableDefinitions[i];
                    if (definition?.Prefab == null ||
                        !BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(definition, normalizedItemId))
                    {
                        continue;
                    }

                    prefab = definition.Prefab;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveConfiguredUnitPrefab(Context context, string normalizedItemId, out GameObject prefab)
        {
            return TryResolveConfiguredUnit(
                context,
                normalizedItemId,
                unitIdIsNormalized: true,
                out prefab,
                out _,
                out _,
                out _);
        }

        private static bool IsConfiguredBuildingPrefab(Context context, GameObject prefab)
        {
            if (prefab == null)
                return false;

            if (context.ConfiguredDefinitionsByPrefab != null &&
                context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab))
            {
                return true;
            }

            if (context.ConfiguredSpawnableDefinitions == null)
                return false;

            for (int i = 0; i < context.ConfiguredSpawnableDefinitions.Count; i++)
            {
                if (context.ConfiguredSpawnableDefinitions[i]?.Prefab == prefab)
                    return true;
            }

            return false;
        }

        private static TacticalCommandReasonCode ToProductionReasonCode(byte resultCode)
        {
            return resultCode switch
            {
                BuildingUiProductionCommandResultElement.Queued => TacticalCommandReasonCode.None,
                BuildingUiProductionCommandResultElement.Cancelled => TacticalCommandReasonCode.None,
                BuildingUiProductionCommandResultElement.MissingActiveBuilding => TacticalCommandReasonCode.NoSelection,
                BuildingUiProductionCommandResultElement.MissingProducerBuilding => TacticalCommandReasonCode.BuildUnavailable,
                BuildingUiProductionCommandResultElement.MissingUnitConfig => TacticalCommandReasonCode.BuildUnavailable,
                BuildingUiProductionCommandResultElement.UnavailablePrefab => TacticalCommandReasonCode.BuildUnavailable,
                BuildingUiProductionCommandResultElement.QueueFull => TacticalCommandReasonCode.CommandUnavailable,
                BuildingUiProductionCommandResultElement.GlobalQueueFull => TacticalCommandReasonCode.CommandUnavailable,
                _ => TacticalCommandReasonCode.CommandUnavailable
            };
        }

        private Entity EnsureUiCampItemCommandEntity(EntityManager em)
        {
            return _commandEntityCache.GetOrCreateCampItem(em);
        }

        private bool TryGetUiCampItemCommandEntity(EntityManager em, out Entity entity)
        {
            return _commandEntityCache.TryGetCampItem(em, out entity);
        }

        private Entity EnsureUiProductionCommandEntity(EntityManager em)
        {
            return _commandEntityCache.GetOrCreateProduction(em);
        }

        private bool TryGetUiProductionCommandEntity(EntityManager em, out Entity entity)
        {
            return _commandEntityCache.TryGetProduction(em, out entity);
        }

        private static bool IsFriendlyProducerBuildingForPass(RuntimeBuildingEntity building, int pass)
        {
            if (building == null)
                return false;

            if (building.HasOwnerFaction && building.OwnerFactionId == FactionIdentity.PlayerFactionId)
                return pass == 0;

            if (!building.HasOwnerFaction || building.OwnerFactionId == FactionIdentity.NeutralFactionId)
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

            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings =
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

        private static BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings ResolveProductionTransportSettings(
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

            string normalized = unitIdIsNormalized ? unitId : BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(unitId);
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

            if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name) == normalizedUnitId)
                return true;

            return context.TryGetConfiguredUnitReadModel != null &&
                   context.TryGetConfiguredUnitReadModel(
                       index,
                       out _,
                       out string displayName,
                       out _,
                       out _,
                       out _) &&
                   BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(displayName) == normalizedUnitId;
        }

        private static bool TryBuildConfiguredUnit(Context context, GameObject prefab, out string displayName, out int price, out bool canRequest)
        {
            displayName = string.Empty;
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

        private Vector3 ResolveProductionRequestFocusWorldPosition(Context context, RuntimeBuildingEntity producerBuilding, GameObject producedUnitPrefab)
        {
            if (producerBuilding == null)
                return Vector3.zero;

            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings = context.ProductionSystem.ResolveProductionTransportSettings(
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
}
