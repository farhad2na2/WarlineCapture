using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MaterialFabricationSystem : ISystem
    {
        private const int FactionCapacity = 256;
        private const float TickIntervalSeconds = 1f;
        private const float MinimumProgressTolerance = 0.00001f;

        private NativeArray<Entity> _materialsEntityByFaction;
        private NativeArray<byte> _materialsEntityCountByFaction;
        private float _elapsedSeconds;

        public readonly struct TickResult
        {
            public readonly int CompletedCycles;
            public readonly float OilConsumedBarrels;
            public readonly int MaterialsProduced;
            public readonly float ActiveSeconds;

            public TickResult(
                int completedCycles,
                float oilConsumedBarrels,
                int materialsProduced,
                float activeSeconds)
            {
                CompletedCycles = completedCycles;
                OilConsumedBarrels = oilConsumedBarrels;
                MaterialsProduced = materialsProduced;
                ActiveSeconds = activeSeconds;
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _materialsEntityByFaction = new NativeArray<Entity>(FactionCapacity, Allocator.Persistent);
            _materialsEntityCountByFaction = new NativeArray<byte>(FactionCapacity, Allocator.Persistent);
            _elapsedSeconds = 0f;
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
            state.RequireForUpdate<MaterialFabricationComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProcessProductionRequests(ref state);

            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
            {
                _elapsedSeconds = 0f;
                return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;
            if (math.isfinite(deltaTime) && deltaTime > 0f)
                _elapsedSeconds += deltaTime;
            if (_elapsedSeconds + MinimumProgressTolerance < TickIntervalSeconds)
                return;

            float fabricationDeltaTime = _elapsedSeconds;
            _elapsedSeconds = 0f;
            RebuildFactionMaterialsLookup(ref state);

            ComponentLookup<FactionTacticalMaterialsComponent> materialsLookup =
                SystemAPI.GetComponentLookup<FactionTacticalMaterialsComponent>();
            ComponentLookup<MaterialFabricationEconomyEventQueueComponent> eventQueueLookup =
                SystemAPI.GetComponentLookup<MaterialFabricationEconomyEventQueueComponent>();
            BufferLookup<MaterialFabricationEconomyEventElement> eventBufferLookup =
                SystemAPI.GetBufferLookup<MaterialFabricationEconomyEventElement>();
            ComponentLookup<FactionMaterialFabricationTelemetryComponent> telemetryLookup =
                SystemAPI.GetComponentLookup<FactionMaterialFabricationTelemetryComponent>();
            ComponentLookup<Disabled> disabledLookup = SystemAPI.GetComponentLookup<Disabled>(true);
            ComponentLookup<UnitDeathAnimationComponent> deathAnimationLookup =
                SystemAPI.GetComponentLookup<UnitDeathAnimationComponent>(true);

            foreach (var (fabricationRef, storageRef, healthRef, entity) in SystemAPI
                         .Query<RefRW<MaterialFabricationComponent>, RefRW<BuildingResourceStorageComponent>, RefRO<UnitHealth>>()
                         .WithAll<MaterialFabricationInputTag>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                ref MaterialFabricationComponent fabrication = ref fabricationRef.ValueRW;
                ref BuildingResourceStorageComponent storage = ref storageRef.ValueRW;
                byte factionId = fabrication.OwnerFactionId;
                bool buildingOperational = !disabledLookup.HasComponent(entity) &&
                                           !deathAnimationLookup.HasComponent(entity) &&
                                           healthRef.ValueRO.Current > 0;
                if (_materialsEntityCountByFaction[factionId] != 1)
                {
                    FactionTacticalMaterialsComponent unavailableMaterials = new()
                    {
                        FactionId = factionId
                    };
                    ApplyTick(
                        ref fabrication,
                        ref storage,
                        ref unavailableMaterials,
                        fabricationDeltaTime,
                        buildingOperational: false);
                    continue;
                }

                Entity materialsEntity = _materialsEntityByFaction[factionId];
                if (materialsEntity == Entity.Null || !materialsLookup.HasComponent(materialsEntity))
                    continue;

                FactionTacticalMaterialsComponent materials = materialsLookup[materialsEntity];
                uint previousMaterialsVersion = materials.Version;
                MaterialFabricationStatusCode previousStatus = fabrication.Status;
                MaterialFabricationBlockReasonCode previousBlockReason = fabrication.BlockReason;
                TickResult result = ApplyTick(
                    ref fabrication,
                    ref storage,
                    ref materials,
                    fabricationDeltaTime,
                    buildingOperational);
                AccumulateTelemetry(
                    telemetryLookup,
                    materialsEntity,
                    fabrication,
                    result,
                    fabricationDeltaTime);
                if (materials.Version != previousMaterialsVersion)
                    materialsLookup[materialsEntity] = materials;

                if (!eventQueueLookup.HasComponent(materialsEntity) ||
                    !eventBufferLookup.HasBuffer(materialsEntity))
                {
                    continue;
                }

                MaterialFabricationEconomyEventQueueComponent eventQueue = eventQueueLookup[materialsEntity];
                DynamicBuffer<MaterialFabricationEconomyEventElement> events = eventBufferLookup[materialsEntity];
                if (result.CompletedCycles > 0)
                {
                    AppendEvent(
                        ref eventQueue,
                        events,
                        fabrication,
                        MaterialFabricationEconomyEventKind.CycleCompleted,
                        result);
                }

                if (fabrication.Status != previousStatus || fabrication.BlockReason != previousBlockReason)
                {
                    AppendEvent(
                        ref eventQueue,
                        events,
                        fabrication,
                        MaterialFabricationEconomyEventKind.StatusChanged,
                        default);
                }

                eventQueueLookup[materialsEntity] = eventQueue;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_materialsEntityByFaction.IsCreated)
                _materialsEntityByFaction.Dispose();
            if (_materialsEntityCountByFaction.IsCreated)
                _materialsEntityCountByFaction.Dispose();
        }

        private void RebuildFactionMaterialsLookup(ref SystemState state)
        {
            for (int i = 0; i < FactionCapacity; i++)
            {
                _materialsEntityByFaction[i] = Entity.Null;
                _materialsEntityCountByFaction[i] = 0;
            }

            foreach (var (economyRef, materialsRef, entity) in SystemAPI
                         .Query<RefRO<FactionEconomy>, RefRO<FactionTacticalMaterialsComponent>>()
                         .WithEntityAccess())
            {
                byte factionId = economyRef.ValueRO.FactionId;
                if (materialsRef.ValueRO.FactionId != factionId)
                    continue;

                byte count = _materialsEntityCountByFaction[factionId];
                _materialsEntityCountByFaction[factionId] = count == byte.MaxValue
                    ? byte.MaxValue
                    : (byte)(count + 1);
                if (count == 0)
                    _materialsEntityByFaction[factionId] = entity;
            }
        }

        public static TickResult ApplyTick(
            ref MaterialFabricationComponent fabrication,
            ref BuildingResourceStorageComponent storage,
            ref FactionTacticalMaterialsComponent materials,
            float deltaTime,
            bool buildingOperational)
        {
            float cycleDuration = fabrication.CycleDurationSeconds;
            float oilPerCycle = fabrication.OilConsumedPerCycle;
            int materialsPerCycle = fabrication.MaterialsOutputPerCycle;
            if (!HasValidConfiguration(fabrication) ||
                storage.OwnerFactionId != fabrication.OwnerFactionId ||
                materials.FactionId != fabrication.OwnerFactionId)
            {
                SetFabricationState(
                    ref fabrication,
                    ClampProgress(fabrication.CycleProgressSeconds, cycleDuration),
                    MaterialFabricationStatusCode.Blocked,
                    MaterialFabricationBlockReasonCode.BuildingDisabled);
                return default;
            }

            float progress = ClampProgress(fabrication.CycleProgressSeconds, cycleDuration);
            float startingProgress = progress;
            if (!buildingOperational)
            {
                SetFabricationState(
                    ref fabrication,
                    progress,
                    MaterialFabricationStatusCode.Blocked,
                    MaterialFabricationBlockReasonCode.BuildingDisabled);
                return default;
            }

            if (fabrication.ProductionEnabled == 0)
            {
                SetFabricationState(
                    ref fabrication,
                    progress,
                    MaterialFabricationStatusCode.Disabled,
                    MaterialFabricationBlockReasonCode.ProductionDisabled);
                return default;
            }

            if (!HasOilForCycle(storage, oilPerCycle))
            {
                SetFabricationState(
                    ref fabrication,
                    progress,
                    MaterialFabricationStatusCode.Blocked,
                    MaterialFabricationBlockReasonCode.NoOilInput);
                return default;
            }

            if (!FactionTacticalMaterialsUtilitySystemHelper.HasCapacity(materials, materialsPerCycle))
            {
                SetFabricationState(
                    ref fabrication,
                    progress,
                    MaterialFabricationStatusCode.Blocked,
                    MaterialFabricationBlockReasonCode.MaterialsCapacityFull);
                return default;
            }

            float safeDeltaTime = math.isfinite(deltaTime) ? math.max(0f, deltaTime) : 0f;
            double totalProgress = progress + (double)safeDeltaTime;

            float tolerance = math.max(MinimumProgressTolerance, cycleDuration * MinimumProgressTolerance);
            double elapsedCycleValue = math.floor((totalProgress + tolerance) / cycleDuration);
            int elapsedCycles = elapsedCycleValue >= int.MaxValue ? int.MaxValue : (int)elapsedCycleValue;
            if (elapsedCycles <= 0)
            {
                SetFabricationState(
                    ref fabrication,
                    (float)totalProgress,
                    MaterialFabricationStatusCode.Producing,
                    MaterialFabricationBlockReasonCode.None);
                return new TickResult(0, 0f, 0, safeDeltaTime);
            }

            float availableOil = BuildingResourceStorageTransferSystemHelper.GetAvailableSourceResource(
                storage,
                BuildingResourceStorageTransferSystemHelper.OilResourceKind);
            double oilCycleValue = math.floor((availableOil + tolerance) / oilPerCycle);
            int oilCycles = oilCycleValue >= int.MaxValue ? int.MaxValue : (int)oilCycleValue;
            int capacityCycles = (materials.Capacity - materials.Current) / materialsPerCycle;
            int completedCycles = math.min(elapsedCycles, math.min(oilCycles, capacityCycles));
            if (completedCycles <= 0)
            {
                MaterialFabricationBlockReasonCode reason = oilCycles <= 0
                    ? MaterialFabricationBlockReasonCode.NoOilInput
                    : MaterialFabricationBlockReasonCode.MaterialsCapacityFull;
                SetFabricationState(ref fabrication, progress, MaterialFabricationStatusCode.Blocked, reason);
                return default;
            }

            int materialsProduced = completedCycles * materialsPerCycle;
            float oilConsumed = completedCycles * oilPerCycle;
            BuildingResourceStorageComponent nextStorage = storage;
            FactionTacticalMaterialsComponent nextMaterials = materials;
            if (!BuildingResourceStorageTransferSystemHelper.TryConsumeAvailableSourceResource(
                    ref nextStorage,
                    BuildingResourceStorageTransferSystemHelper.OilResourceKind,
                    oilConsumed))
            {
                SetFabricationState(
                    ref fabrication,
                    progress,
                    MaterialFabricationStatusCode.Blocked,
                    MaterialFabricationBlockReasonCode.NoOilInput);
                return default;
            }

            FactionTacticalMaterialsMutationResult grantResult =
                FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                    ref nextMaterials,
                    materialsProduced,
                    FactionTacticalMaterialsSourceKind.Fabrication);
            if (grantResult != FactionTacticalMaterialsMutationResult.Applied)
            {
                MaterialFabricationBlockReasonCode reason =
                    grantResult == FactionTacticalMaterialsMutationResult.CapacityExceeded
                        ? MaterialFabricationBlockReasonCode.MaterialsCapacityFull
                        : MaterialFabricationBlockReasonCode.BuildingDisabled;
                SetFabricationState(ref fabrication, progress, MaterialFabricationStatusCode.Blocked, reason);
                return default;
            }

            storage = nextStorage;
            materials = nextMaterials;
            double remainder = totalProgress - elapsedCycles * (double)cycleDuration;
            progress = (float)math.clamp(remainder, 0d, cycleDuration);

            MaterialFabricationBlockReasonCode nextBlockReason = MaterialFabricationBlockReasonCode.None;
            MaterialFabricationStatusCode nextStatus = MaterialFabricationStatusCode.Producing;
            if (!HasOilForCycle(storage, oilPerCycle))
            {
                nextStatus = MaterialFabricationStatusCode.Blocked;
                nextBlockReason = MaterialFabricationBlockReasonCode.NoOilInput;
            }
            else if (!FactionTacticalMaterialsUtilitySystemHelper.HasCapacity(materials, materialsPerCycle))
            {
                nextStatus = MaterialFabricationStatusCode.Blocked;
                nextBlockReason = MaterialFabricationBlockReasonCode.MaterialsCapacityFull;
            }

            if (nextStatus == MaterialFabricationStatusCode.Blocked)
                progress = 0f;

            SetFabricationState(ref fabrication, progress, nextStatus, nextBlockReason);
            float activeSeconds = safeDeltaTime;
            if (completedCycles < elapsedCycles)
            {
                double activeUntilBlocked = completedCycles * (double)cycleDuration - startingProgress;
                activeSeconds = (float)math.clamp(activeUntilBlocked, 0d, safeDeltaTime);
            }
            return new TickResult(completedCycles, oilConsumed, materialsProduced, activeSeconds);
        }

        public static void AccumulateTelemetry(
            ref FactionMaterialFabricationTelemetryComponent telemetry,
            in TickResult result,
            float deltaTime,
            MaterialFabricationStatusCode status,
            MaterialFabricationBlockReasonCode blockReason)
        {
            float safeDeltaTime = math.isfinite(deltaTime) ? math.max(0f, deltaTime) : 0f;
            float activeSeconds = math.clamp(result.ActiveSeconds, 0f, safeDeltaTime);
            float blockedSeconds = safeDeltaTime - activeSeconds;
            bool changed = false;
            if (activeSeconds > 0f)
            {
                telemetry.ActiveSeconds = SaturatingAdd(telemetry.ActiveSeconds, activeSeconds);
                changed = true;
            }

            if (blockedSeconds > 0f && status != MaterialFabricationStatusCode.Producing)
            {
                switch (blockReason)
                {
                    case MaterialFabricationBlockReasonCode.NoOilInput:
                        telemetry.NoOilInputBlockedSeconds =
                            SaturatingAdd(telemetry.NoOilInputBlockedSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.MaterialsCapacityFull:
                        telemetry.MaterialsCapacityFullBlockedSeconds =
                            SaturatingAdd(telemetry.MaterialsCapacityFullBlockedSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.NoOilRoute:
                        telemetry.NoOilRouteBlockedSeconds =
                            SaturatingAdd(telemetry.NoOilRouteBlockedSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.ProductionDisabled:
                        telemetry.ProductionDisabledSeconds =
                            SaturatingAdd(telemetry.ProductionDisabledSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.BuildingDisabled:
                        telemetry.BuildingDisabledSeconds =
                            SaturatingAdd(telemetry.BuildingDisabledSeconds, blockedSeconds);
                        changed = true;
                        break;
                }
            }

            if (changed)
                IncrementVersion(ref telemetry.Version);
        }

        public static MaterialFabricationResultComponent ApplyProductionRequest(
            ref MaterialFabricationComponent fabrication,
            in MaterialFabricationRequestComponent request)
        {
            return MaterialFabricationCommandUtilitySystemHelper.ApplyProductionRequest(
                ref fabrication,
                request);
        }

        public static bool TryEnqueueProductionRequest(
            EntityManager entityManager,
            Entity fabricationEntity,
            byte requesterFactionId,
            bool productionEnabled,
            out int requestId)
        {
            return MaterialFabricationCommandUtilitySystemHelper.TryEnqueueProductionRequest(
                entityManager,
                fabricationEntity,
                requesterFactionId,
                productionEnabled,
                out requestId);
        }

        public static bool TryGetProductionResult(
            EntityManager entityManager,
            Entity fabricationEntity,
            int requestId,
            out MaterialFabricationResultComponent result)
        {
            return MaterialFabricationCommandUtilitySystemHelper.TryGetProductionResult(
                entityManager,
                fabricationEntity,
                requestId,
                out result);
        }

        private void ProcessProductionRequests(ref SystemState state)
        {
            foreach (var (fabricationRef, requests, results) in SystemAPI
                         .Query<RefRW<MaterialFabricationComponent>, DynamicBuffer<MaterialFabricationRequestComponent>, DynamicBuffer<MaterialFabricationResultComponent>>()
                         .WithAll<MaterialFabricationInputTag>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
            {
                ref MaterialFabricationComponent fabrication = ref fabricationRef.ValueRW;
                for (int i = 0; i < requests.Length; i++)
                {
                    while (results.Length >= MaterialFabricationCommandQueueComponent.Capacity)
                        results.RemoveAt(0);
                    results.Add(ApplyProductionRequest(ref fabrication, requests[i]));
                }
                requests.Clear();
            }
        }

        private static bool HasValidConfiguration(in MaterialFabricationComponent fabrication)
        {
            return fabrication.OutputCapacityPolicy ==
                       MaterialFabricationOutputCapacityPolicyCode.RequireFullCycleCapacity &&
                   math.isfinite(fabrication.OilConsumedPerCycle) &&
                   fabrication.OilConsumedPerCycle > 0f &&
                   fabrication.MaterialsOutputPerCycle > 0 &&
                   math.isfinite(fabrication.CycleDurationSeconds) &&
                   fabrication.CycleDurationSeconds > 0f;
        }

        private static bool HasOilForCycle(in BuildingResourceStorageComponent storage, float oilPerCycle)
        {
            float availableOil = BuildingResourceStorageTransferSystemHelper.GetAvailableSourceResource(
                storage,
                BuildingResourceStorageTransferSystemHelper.OilResourceKind);
            return math.isfinite(availableOil) && availableOil + MinimumProgressTolerance >= oilPerCycle;
        }

        private static float ClampProgress(float progress, float cycleDuration)
        {
            if (!math.isfinite(progress) || !math.isfinite(cycleDuration) || cycleDuration <= 0f)
                return 0f;
            return math.clamp(progress, 0f, cycleDuration);
        }

        private static void SetFabricationState(
            ref MaterialFabricationComponent fabrication,
            float progress,
            MaterialFabricationStatusCode status,
            MaterialFabricationBlockReasonCode blockReason)
        {
            if (fabrication.CycleProgressSeconds == progress &&
                fabrication.Status == status &&
                fabrication.BlockReason == blockReason)
            {
                return;
            }

            fabrication.CycleProgressSeconds = progress;
            fabrication.Status = status;
            fabrication.BlockReason = blockReason;
            IncrementVersion(ref fabrication.Version);
        }

        private static void AppendEvent(
            ref MaterialFabricationEconomyEventQueueComponent queue,
            DynamicBuffer<MaterialFabricationEconomyEventElement> events,
            in MaterialFabricationComponent fabrication,
            MaterialFabricationEconomyEventKind eventKind,
            in TickResult result)
        {
            while (events.Length >= MaterialFabricationEconomyEventQueueComponent.Capacity)
                events.RemoveAt(0);

            queue.LastEventId = queue.LastEventId == int.MaxValue ? 1 : queue.LastEventId + 1;
            queue.Version = queue.Version == uint.MaxValue ? 1u : queue.Version + 1u;
            events.Add(new MaterialFabricationEconomyEventElement
            {
                EventId = queue.LastEventId,
                RuntimeBuildingId = fabrication.RuntimeBuildingId,
                FactionId = fabrication.OwnerFactionId,
                EventKind = eventKind,
                Status = fabrication.Status,
                BlockReason = fabrication.BlockReason,
                CompletedCycles = result.CompletedCycles,
                OilConsumedBarrels = result.OilConsumedBarrels,
                MaterialsProduced = result.MaterialsProduced
            });
        }

        private static void IncrementVersion(ref uint version)
        {
            version = version == uint.MaxValue ? 1u : version + 1u;
        }

        private static float SaturatingAdd(float value, float amount)
        {
            if (!math.isfinite(value) || value < 0f)
                value = 0f;
            if (!math.isfinite(amount) || amount <= 0f)
                return value;
            return value >= float.MaxValue - amount ? float.MaxValue : value + amount;
        }

        private static void AccumulateTelemetry(
            ComponentLookup<FactionMaterialFabricationTelemetryComponent> telemetryLookup,
            Entity entity,
            in MaterialFabricationComponent fabrication,
            in TickResult result,
            float deltaTime)
        {
            if (!telemetryLookup.HasComponent(entity))
                return;

            FactionMaterialFabricationTelemetryComponent telemetry = telemetryLookup[entity];
            AccumulateTelemetry(
                ref telemetry,
                result,
                deltaTime,
                fabrication.Status,
                fabrication.BlockReason);
            telemetryLookup[entity] = telemetry;
        }
    }
}
