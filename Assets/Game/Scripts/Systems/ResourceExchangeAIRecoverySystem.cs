using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AIBuildPlannerSystem))]
    [UpdateAfter(typeof(MaterialFabricationSystem))]
    [UpdateBefore(typeof(ResourceExchangeRequestValidationSystem))]
    public partial struct ResourceExchangeAIRecoverySystem : ISystem
    {
        private const int FactionCapacity = 256;

        private NativeArray<AIMaterialsRecoveryNeedComponent> _needByFaction;
        private NativeArray<byte> _needCountByFaction;
        private NativeArray<byte> _aiControlledByFaction;
        private NativeList<LocalFabricationRecord> _localFabrications;

        internal struct LocalRecoverySummary
        {
            public byte HasAwaitingOilDepot;
            public int ProjectedMaterials;
        }

        private struct LocalFabricationRecord
        {
            public byte FactionId;
            public byte ProductionEnabled;
            public MaterialFabricationStatusCode Status;
            public MaterialFabricationBlockReasonCode BlockReason;
            public float OilConsumedPerCycle;
            public float AvailableOil;
            public int MaterialsOutputPerCycle;
            public float CycleDurationSeconds;
            public float CycleProgressSeconds;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _needByFaction = new NativeArray<AIMaterialsRecoveryNeedComponent>(FactionCapacity, Allocator.Persistent);
            _needCountByFaction = new NativeArray<byte>(FactionCapacity, Allocator.Persistent);
            _aiControlledByFaction = new NativeArray<byte>(FactionCapacity, Allocator.Persistent);
            _localFabrications = new NativeList<LocalFabricationRecord>(16, Allocator.Persistent);
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
            state.RequireForUpdate<FactionControlConfigTag>();
            state.RequireForUpdate<BuildingRuntimeStateTag>();
            state.RequireForUpdate<ResourceExchangeEnabledComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
                return;

            ClearFactionState();
            DynamicBuffer<FactionControlEntry> controls =
                SystemAPI.GetSingletonBuffer<FactionControlEntry>(true);
            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.AIControlled != 0)
                    _aiControlledByFaction[control.FactionId] = 1;
            }

            foreach (var (planRef, needRef) in
                     SystemAPI.Query<RefRO<AIBuildPlan>, RefRO<AIMaterialsRecoveryNeedComponent>>())
            {
                AIBuildPlan plan = planRef.ValueRO;
                AIMaterialsRecoveryNeedComponent need = needRef.ValueRO;
                if (need.Active == 0 || plan.Enabled == 0 || plan.FactionId != need.FactionId)
                    continue;

                byte count = _needCountByFaction[need.FactionId];
                _needCountByFaction[need.FactionId] = count == byte.MaxValue
                    ? byte.MaxValue
                    : (byte)(count + 1);
                if (count == 0)
                    _needByFaction[need.FactionId] = need;
            }

            foreach (var (fabricationRef, storageRef) in
                     SystemAPI.Query<RefRO<MaterialFabricationComponent>, RefRO<BuildingResourceStorageComponent>>())
            {
                MaterialFabricationComponent fabrication = fabricationRef.ValueRO;
                BuildingResourceStorageComponent storage = storageRef.ValueRO;
                _localFabrications.Add(new LocalFabricationRecord
                {
                    FactionId = fabrication.OwnerFactionId,
                    ProductionEnabled = fabrication.ProductionEnabled,
                    Status = fabrication.Status,
                    BlockReason = fabrication.BlockReason,
                    OilConsumedPerCycle = fabrication.OilConsumedPerCycle,
                    AvailableOil = BuildingResourceStorageTransferSystemHelper.GetAvailableSourceResource(
                        storage,
                        BuildingResourceStorageTransferSystemHelper.OilResourceKind),
                    MaterialsOutputPerCycle = fabrication.MaterialsOutputPerCycle,
                    CycleDurationSeconds = fabrication.CycleDurationSeconds,
                    CycleProgressSeconds = fabrication.CycleProgressSeconds
                });
            }

            float now = (float)math.min(float.MaxValue, SystemAPI.Time.ElapsedTime);
            int frameCount = (int)math.min(int.MaxValue, state.GlobalSystemVersion);
            foreach (var (requestQueueRef, enabledRef, economyRef, materialsRef, recipes, requests, queue) in
                     SystemAPI.Query<
                         RefRW<ResourceExchangeRequestQueueComponent>,
                         RefRO<ResourceExchangeEnabledComponent>,
                         RefRO<FactionEconomy>,
                         RefRO<FactionTacticalMaterialsComponent>,
                         DynamicBuffer<ResourceExchangeRecipeComponent>,
                         DynamicBuffer<ResourceExchangeRequestComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>>())
            {
                ResourceExchangeEnabledComponent enabled = enabledRef.ValueRO;
                byte factionId = enabled.FactionId;
                if (enabled.Enabled == 0 ||
                    enabled.AllowAiExchange == 0 ||
                    _aiControlledByFaction[factionId] == 0 ||
                    _needCountByFaction[factionId] != 1)
                {
                    continue;
                }

                FactionEconomy economy = economyRef.ValueRO;
                FactionTacticalMaterialsComponent materials = materialsRef.ValueRO;
                AIMaterialsRecoveryNeedComponent need = _needByFaction[factionId];
                int missingMaterials = math.max(0, need.RequiredMaterials - math.max(0, materials.Current));
                if (economy.FactionId != factionId ||
                    materials.FactionId != factionId ||
                    need.FactionId != factionId ||
                    missingMaterials <= 0 ||
                    need.RequiredMaterials > math.max(0, materials.Capacity) ||
                    !HasQueueCapacity(requests, queue, factionId, enabled.MaxQueueItems))
                {
                    continue;
                }

                need.MissingMaterials = missingMaterials;

                if (!TrySelectEmergencyMaterialsImport(
                        recipes,
                        need,
                        materials,
                        economy,
                        enabled.ScenarioTag,
                        out ResourceExchangeRecipeComponent recipe,
                        out int inputAmount,
                        out float importDurationSeconds))
                {
                    continue;
                }

                if (HasPendingMaterialsImport(requests, queue, recipes, factionId))
                    continue;

                LocalRecoverySummary localRecovery = EvaluateLocalRecovery(
                    _localFabrications,
                    factionId,
                    missingMaterials,
                    importDurationSeconds);

                if (!ShouldRequestImport(
                        need,
                        localRecovery,
                        importDurationSeconds,
                        now))
                {
                    continue;
                }

                ref ResourceExchangeRequestQueueComponent requestQueue = ref requestQueueRef.ValueRW;
                requestQueue.LastRequestId++;
                requests.Add(new ResourceExchangeRequestComponent
                {
                    RequestId = requestQueue.LastRequestId,
                    RequestKind = ResourceExchangeRequestKind.Start,
                    FactionId = factionId,
                    RecipeId = recipe.RecipeId,
                    InputAmount = inputAmount,
                    FrameCount = frameCount
                });
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_needByFaction.IsCreated)
                _needByFaction.Dispose();
            if (_needCountByFaction.IsCreated)
                _needCountByFaction.Dispose();
            if (_aiControlledByFaction.IsCreated)
                _aiControlledByFaction.Dispose();
            if (_localFabrications.IsCreated)
                _localFabrications.Dispose();
        }

        private void ClearFactionState()
        {
            for (int i = 0; i < FactionCapacity; i++)
            {
                _needByFaction[i] = default;
                _needCountByFaction[i] = 0;
                _aiControlledByFaction[i] = 0;
            }

            _localFabrications.Clear();
        }

        private static LocalRecoverySummary EvaluateLocalRecovery(
            in NativeList<LocalFabricationRecord> fabrications,
            byte factionId,
            int missingMaterials,
            float importDurationSeconds)
        {
            LocalRecoverySummary summary = default;
            for (int i = 0; i < fabrications.Length; i++)
            {
                LocalFabricationRecord fabrication = fabrications[i];
                if (fabrication.FactionId != factionId ||
                    fabrication.ProductionEnabled == 0 ||
                    fabrication.MaterialsOutputPerCycle <= 0 ||
                    !math.isfinite(fabrication.CycleDurationSeconds) ||
                    fabrication.CycleDurationSeconds <= 0f)
                {
                    continue;
                }

                bool awaitingOil = fabrication.Status == MaterialFabricationStatusCode.None ||
                                   (fabrication.Status == MaterialFabricationStatusCode.Blocked &&
                                    (fabrication.BlockReason == MaterialFabricationBlockReasonCode.NoOilInput ||
                                     fabrication.BlockReason == MaterialFabricationBlockReasonCode.NoOilRoute));
                if (awaitingOil)
                    summary.HasAwaitingOilDepot = 1;
                if (fabrication.Status != MaterialFabricationStatusCode.Producing ||
                    fabrication.OilConsumedPerCycle <= 0f ||
                    fabrication.AvailableOil < fabrication.OilConsumedPerCycle)
                {
                    continue;
                }

                double projectedProgress = math.max(0f, fabrication.CycleProgressSeconds) +
                                           math.max(0f, importDurationSeconds);
                int timeCycles = (int)math.min(
                    int.MaxValue,
                    math.floor(projectedProgress / fabrication.CycleDurationSeconds));
                int oilCycles = (int)math.min(
                    int.MaxValue,
                    math.floor(fabrication.AvailableOil / fabrication.OilConsumedPerCycle));
                if (oilCycles < timeCycles)
                    summary.HasAwaitingOilDepot = 1;
                int completedCycles = math.max(0, math.min(timeCycles, oilCycles));
                long projectedMaterials = (long)completedCycles * fabrication.MaterialsOutputPerCycle;
                summary.ProjectedMaterials = projectedMaterials >= int.MaxValue - summary.ProjectedMaterials
                    ? int.MaxValue
                    : summary.ProjectedMaterials + (int)projectedMaterials;
                if (summary.ProjectedMaterials >= missingMaterials)
                    break;
            }

            return summary;
        }

        [BurstCompile]
        internal static bool ShouldRequestImport(
            in AIMaterialsRecoveryNeedComponent need,
            in LocalRecoverySummary localRecovery,
            float importDurationSeconds,
            float now)
        {
            if (need.Active == 0 || need.MissingMaterials <= 0 || importDurationSeconds < 0f)
                return false;
            if (localRecovery.ProjectedMaterials >= need.MissingMaterials)
                return false;

            if (localRecovery.HasAwaitingOilDepot == 0)
                return true;

            float blockedDuration = math.max(0f, now - need.FirstBlockedTimeSeconds);
            return blockedDuration >= importDurationSeconds;
        }

        [BurstCompile]
        internal static bool TryResolveInputAmount(
            in ResourceExchangeRecipeComponent recipe,
            int missingMaterials,
            int availableMaterialsCapacity,
            out int inputAmount,
            out int outputAmount,
            out float durationSeconds)
        {
            inputAmount = 0;
            outputAmount = 0;
            durationSeconds = 0f;
            float outputPerInput = math.max(0f, recipe.OutputPerInput) *
                                   (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            if (missingMaterials <= 0 || availableMaterialsCapacity <= 0 || outputPerInput <= 0f)
                return false;

            int minimum = math.max(0, recipe.InputAmountMin);
            int maximum = math.max(minimum, recipe.InputAmountMax);
            int step = math.max(1, recipe.InputStep);
            int requiredInput = (int)math.ceil(missingMaterials / outputPerInput);
            int steppedInput = requiredInput <= minimum
                ? minimum
                : minimum + (int)math.ceil((requiredInput - minimum) / (float)step) * step;
            if (steppedInput < minimum || steppedInput > maximum)
                return false;

            int resolvedOutput = (int)math.floor(steppedInput * outputPerInput);
            if (resolvedOutput < missingMaterials || resolvedOutput > availableMaterialsCapacity)
                return false;

            int completedSteps = math.max(0, (steppedInput - minimum) / step);
            inputAmount = steppedInput;
            outputAmount = resolvedOutput;
            durationSeconds = math.max(
                0f,
                recipe.DurationSecondsBase + completedSteps * recipe.DurationSecondsPerStep);
            return true;
        }

        private static bool TrySelectEmergencyMaterialsImport(
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            in AIMaterialsRecoveryNeedComponent need,
            in FactionTacticalMaterialsComponent materials,
            in FactionEconomy economy,
            in FixedString64Bytes scenarioTag,
            out ResourceExchangeRecipeComponent selectedRecipe,
            out int inputAmount,
            out float durationSeconds)
        {
            selectedRecipe = default;
            inputAmount = 0;
            durationSeconds = 0f;
            int selectedSortOrder = int.MaxValue;
            FixedString128Bytes selectedRecipeId = default;
            for (int i = 0; i < recipes.Length; i++)
            {
                ResourceExchangeRecipeComponent recipe = recipes[i];
                if (recipe.Enabled == 0 ||
                    recipe.RouteType != ResourceExchangeRouteType.Import ||
                    recipe.InputResource != ResourceExchangeResourceKind.Oil ||
                    recipe.OutputResource != ResourceExchangeResourceKind.Materials ||
                    !recipe.MissionTag.Equals(scenarioTag))
                {
                    continue;
                }

                if (!TryResolveInputAmount(
                        recipe,
                        need.MissingMaterials,
                        math.max(0, materials.Capacity - materials.Current),
                        out int candidateInput,
                        out _,
                        out float candidateDuration) ||
                    candidateInput <= 0)
                {
                    continue;
                }

                bool lowerOrder = recipe.SortOrder < selectedSortOrder;
                bool sameOrderLowerId = recipe.SortOrder == selectedSortOrder &&
                                        (selectedRecipeId.Length == 0 || recipe.RecipeId.CompareTo(selectedRecipeId) < 0);
                if (!lowerOrder && !sameOrderLowerId)
                    continue;

                selectedRecipe = recipe;
                selectedRecipeId = recipe.RecipeId;
                selectedSortOrder = recipe.SortOrder;
                inputAmount = candidateInput;
                durationSeconds = candidateDuration;
            }

            return selectedRecipeId.Length > 0;
        }

        private static bool HasPendingMaterialsImport(
            DynamicBuffer<ResourceExchangeRequestComponent> requests,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            byte factionId,
            bool includeQueuedRequests = true)
        {
            if (includeQueuedRequests)
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    ResourceExchangeRequestComponent request = requests[i];
                    if (request.RequestKind != ResourceExchangeRequestKind.Start || request.FactionId != factionId)
                        continue;
                    for (int recipeIndex = 0; recipeIndex < recipes.Length; recipeIndex++)
                    {
                        ResourceExchangeRecipeComponent recipe = recipes[recipeIndex];
                        if (request.RecipeId.Equals(recipe.RecipeId) &&
                            recipe.RouteType == ResourceExchangeRouteType.Import &&
                            recipe.OutputResource == ResourceExchangeResourceKind.Materials)
                        {
                            return true;
                        }
                    }
                }
            }

            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId ||
                    item.RouteType != ResourceExchangeRouteType.Import ||
                    item.OutputResource != ResourceExchangeResourceKind.Materials)
                {
                    continue;
                }

                if (item.State == ResourceExchangeQueueState.Pending ||
                    item.State == ResourceExchangeQueueState.InProgress ||
                    item.State == ResourceExchangeQueueState.Completing ||
                    item.State == ResourceExchangeQueueState.Blocked)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasQueueCapacity(
            DynamicBuffer<ResourceExchangeRequestComponent> requests,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            byte factionId,
            int maxQueueItems)
        {
            int capacity = math.max(0, maxQueueItems);
            if (capacity == 0)
                return false;

            int occupied = 0;
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId)
                    continue;
                if (item.State == ResourceExchangeQueueState.Pending ||
                    item.State == ResourceExchangeQueueState.InProgress ||
                    item.State == ResourceExchangeQueueState.Completing ||
                    item.State == ResourceExchangeQueueState.Blocked)
                {
                    occupied++;
                }
            }

            for (int i = 0; i < requests.Length; i++)
            {
                ResourceExchangeRequestComponent request = requests[i];
                if (request.FactionId == factionId && request.RequestKind == ResourceExchangeRequestKind.Start)
                    occupied++;
            }

            return occupied < capacity;
        }
    }
}
