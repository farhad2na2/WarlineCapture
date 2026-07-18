using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(AIBuildPlannerSystem))]
    public partial struct AIProductionSystem : ISystem
    {
        private const float LogIntervalSeconds = 10f;
        private int _nextProductionRequestId;
        private EntityQuery _buildingRuntimeBoundaryQuery;
        private EntityQuery _runtimeDiagnosticsQuery;
        private EntityQuery _diagnosticLogQueueQuery;
        private EntityQuery _planQuery;
        private EntityQuery _economyQuery;
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<AIProductionPlan> _aiProductionPlanType;
        private BufferTypeHandle<AIProductionPlanEntry> _aiProductionPlanEntryType;
        private ComponentTypeHandle<FactionEconomy> _factionEconomyType;

        private enum ProductionDecisionResult : byte
        {
            None = 0,
            Pending = 1,
            MissingConfig = 2,
            InsufficientFunds = 3,
            InsufficientFuel = 4,
            Request = 5
        }

        private struct ProductionDecision
        {
            public ProductionDecisionResult Result;
            public int EntryIndex;
            public FixedString128Bytes UnitId;
            public BuildingConfiguredUnitReadModel Unit;
            public int Cost;
        }

        public void OnCreate(ref SystemState state)
        {
            _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingConfiguredUnitReadModel>(),
                ComponentType.ReadOnly<BuildingRuntimeUnitProductionSummary>(),
                ComponentType.ReadWrite<BuildingFactionUnitProductionRequest>());
            _runtimeDiagnosticsQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            _diagnosticLogQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<AIDiagnosticLogComponent>());
            _planQuery = state.GetEntityQuery(ComponentType.ReadWrite<AIProductionPlan>(), ComponentType.ReadOnly<AIProductionPlanEntry>());
            _economyQuery = state.GetEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
            _entityType = state.GetEntityTypeHandle();
            _aiProductionPlanType = state.GetComponentTypeHandle<AIProductionPlan>(false);
            _aiProductionPlanEntryType = state.GetBufferTypeHandle<AIProductionPlanEntry>(true);
            _factionEconomyType = state.GetComponentTypeHandle<FactionEconomy>(true);
            state.RequireForUpdate(_buildingRuntimeBoundaryQuery);
            state.RequireForUpdate<AIProductionPlan>();
            state.RequireForUpdate<FactionEconomy>();
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
                return;

            if (!TryGetBuildingRuntimeStateEntity(ref state, out Entity boundaryEntity))
                return;

            double elapsedTime = SystemAPI.Time.ElapsedTime;
            float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
            bool shouldLog = ShouldQueueDiagnostics(_runtimeDiagnosticsQuery);
            if (shouldLog)
                EnsureDiagnosticLogQueue(ref state);

            bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
            DynamicBuffer<FactionControlEntry> controls = hasControls
                ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true)
                : default;

            EntityManager em = state.EntityManager;
            _entityType.Update(ref state);
            _aiProductionPlanType.Update(ref state);
            _aiProductionPlanEntryType.Update(ref state);
            _factionEconomyType.Update(ref state);
            NativeList<FactionEconomyRecord> economyRecords = BuildFactionEconomyRecords();
            try
            {
                using NativeArray<ArchetypeChunk> planChunks = _planQuery.ToArchetypeChunkArray(Allocator.Temp);
                for (int chunkIndex = 0; chunkIndex < planChunks.Length; chunkIndex++)
                {
                    ArchetypeChunk chunk = planChunks[chunkIndex];
                    NativeArray<AIProductionPlan> plans = chunk.GetNativeArray(ref _aiProductionPlanType);
                    BufferAccessor<AIProductionPlanEntry> entriesByPlan = chunk.GetBufferAccessor(ref _aiProductionPlanEntryType);
                    for (int planIndex = 0; planIndex < plans.Length; planIndex++)
                    {
                        AIProductionPlan plan = plans[planIndex];
                        if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                            continue;

                        if (!TryFindEconomyRecord(economyRecords, plan.FactionId, out int economyRecordIndex, out FactionEconomyRecord economyRecord))
                            continue;

                        FactionEconomy economy = economyRecord.Economy;
                        ProcessCompletedProductionRequests(ref state, boundaryEntity, ref economy, shouldLog);
                        em.SetComponentData(economyRecord.Entity, economy);
                        economyRecords[economyRecordIndex] = new FactionEconomyRecord(economyRecord.Entity, economy);

                        ProcessProductionPlan(
                            ref state,
                            boundaryEntity,
                            ref plan,
                            entriesByPlan[planIndex],
                            ref economy,
                            now,
                            shouldLog);
                        em.SetComponentData(economyRecord.Entity, economy);
                        economyRecords[economyRecordIndex] = new FactionEconomyRecord(economyRecord.Entity, economy);
                        plans[planIndex] = plan;
                    }
                }
            }
            finally
            {
                economyRecords.Dispose();
            }

        }

        private NativeList<FactionEconomyRecord> BuildFactionEconomyRecords()
        {
            int count = _economyQuery.CalculateEntityCount();
            NativeList<FactionEconomyRecord> records = new(count, Allocator.Temp);
            using NativeArray<ArchetypeChunk> chunks = _economyQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(_entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref _factionEconomyType);
                for (int i = 0; i < chunk.Count; i++)
                    records.Add(new FactionEconomyRecord(entities[i], economies[i]));
            }

            return records;
        }

        private static bool TryFindEconomyRecord(
            NativeList<FactionEconomyRecord> records,
            byte factionId,
            out int index,
            out FactionEconomyRecord record)
        {
            for (int i = 0; i < records.Length; i++)
            {
                FactionEconomyRecord candidate = records[i];
                if (candidate.Economy.FactionId != factionId)
                    continue;

                index = i;
                record = candidate;
                return true;
            }

            index = -1;
            record = default;
            return false;
        }

        private void ProcessProductionPlan(
            ref SystemState state,
            Entity boundaryEntity,
            ref AIProductionPlan plan,
            DynamicBuffer<AIProductionPlanEntry> entries,
            ref FactionEconomy economy,
            float now,
            bool shouldLog)
        {
            float interval = math.max(0.1f, plan.UnitProductionIntervalSeconds);
            if (now - plan.LastProductionTime < interval)
                return;

            if (entries.Length == 0)
            {
                LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);
                return;
            }

            ProductionDecision decision = SelectProductionDecision(ref state, boundaryEntity, entries, plan, economy.Money);
            bool handledDecision = decision.Result != ProductionDecisionResult.None;
            switch (decision.Result)
            {
                case ProductionDecisionResult.Pending:
                    plan.LastProductionTime = now;
                    break;

                case ProductionDecisionResult.MissingConfig:
                    plan.NextUnitIndex = decision.EntryIndex + 1;
                    plan.LastProductionTime = now;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={decision.UnitId} result=MissingConfig");
                    break;

                case ProductionDecisionResult.InsufficientFunds:
                    plan.LastProductionTime = now;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={decision.Unit.DisplayName.ToString()} cost={decision.Cost} result=InsufficientFunds money={economy.Money}");
                    break;

                case ProductionDecisionResult.InsufficientFuel:
                    plan.LastProductionTime = now;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={decision.Unit.DisplayName.ToString()} result=InsufficientFuel");
                    break;

                case ProductionDecisionResult.Request:
                    EnqueueProductionRequest(ref state, boundaryEntity, plan.FactionId, decision.UnitId);
                    plan.LastProductionTime = now;
                    plan.NextUnitIndex = decision.EntryIndex + 1;
                    if (shouldLog)
                        EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={decision.Unit.DisplayName.ToString()} cost={decision.Cost} result=Requested");
                    break;
            }

            if (!handledDecision && now - plan.LastLogTime >= LogIntervalSeconds)
            {
                plan.LastLogTime = now;
                if (shouldLog)
                    EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} result=Complete");
            }
        }

        private bool TryGetBuildingRuntimeStateEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
                return false;

            entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
            return entity != Entity.Null && state.EntityManager.Exists(entity);
        }

        private ProductionDecision SelectProductionDecision(
            ref SystemState state,
            Entity boundaryEntity,
            DynamicBuffer<AIProductionPlanEntry> entries,
            AIProductionPlan plan,
            int economyMoney)
        {
            EntityManager em = state.EntityManager;
            DynamicBuffer<BuildingConfiguredUnitReadModel> units = em.HasBuffer<BuildingConfiguredUnitReadModel>(boundaryEntity)
                ? em.GetBuffer<BuildingConfiguredUnitReadModel>(boundaryEntity, true)
                : default;
            DynamicBuffer<BuildingRuntimeFactionSummary> factionSummaries = em.HasBuffer<BuildingRuntimeFactionSummary>(boundaryEntity)
                ? em.GetBuffer<BuildingRuntimeFactionSummary>(boundaryEntity, true)
                : default;
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> usableFuelSummaries = em.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundaryEntity)
                ? em.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundaryEntity, true)
                : default;
            DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries = em.HasBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity)
                ? em.GetBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity, true)
                : default;
            DynamicBuffer<BuildingFactionUnitProductionRequest> requests = em.HasBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity)
                ? em.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity, true)
                : default;

            return SelectProductionDecision(entries, units, factionSummaries, usableFuelSummaries, summaries, requests, plan, economyMoney);
        }

        private static ProductionDecision SelectProductionDecision(
            DynamicBuffer<AIProductionPlanEntry> entries,
            DynamicBuffer<BuildingConfiguredUnitReadModel> units,
            DynamicBuffer<BuildingRuntimeFactionSummary> factionSummaries,
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> usableFuelSummaries,
            DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries,
            DynamicBuffer<BuildingFactionUnitProductionRequest> requests,
            AIProductionPlan plan,
            int economyMoney)
        {
            ProductionDecision decision = default;
            if (entries.Length == 0)
                return decision;

            int attempts = math.max(1, entries.Length);
            int maxQueuedUnits = math.max(1, plan.MaxQueuedUnits);
            int targetProducedUnits = math.max(1, plan.TargetProducedUnits);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateIndex = PositiveModulo(plan.NextUnitIndex + attempt, entries.Length);
                FixedString64Bytes unitId = entries[candidateIndex].UnitId;
                if (unitId.Length == 0)
                    continue;

                TryGetUnitProductionSummary(
                    summaries,
                    plan.FactionId,
                    unitId,
                    out int producedCount,
                    out int queuedCount);
                if (producedCount + queuedCount >= targetProducedUnits ||
                    queuedCount >= maxQueuedUnits)
                {
                    continue;
                }

                decision.EntryIndex = candidateIndex;
                decision.UnitId = unitId;
                if (HasPendingProductionRequest(requests, plan.FactionId, unitId))
                {
                    decision.Result = ProductionDecisionResult.Pending;
                    break;
                }

                if (!TryResolveUnitReadModel(units, unitId, out BuildingConfiguredUnitReadModel unit) ||
                    unit.CanRequest == 0)
                {
                    decision.Result = ProductionDecisionResult.MissingConfig;
                    break;
                }

                int cost = math.max(0, unit.Price);
                decision.Unit = unit;
                decision.Cost = cost;
                if (!HasFuelSupportForProduction(unit, plan.FactionId, factionSummaries, usableFuelSummaries))
                {
                    decision.Result = ProductionDecisionResult.InsufficientFuel;
                    break;
                }

                if (economyMoney < cost)
                {
                    decision.Result = ProductionDecisionResult.InsufficientFunds;
                    break;
                }

                decision.Result = ProductionDecisionResult.Request;
                break;
            }

            return decision;
        }

        private static bool HasFuelSupportForProduction(
            BuildingConfiguredUnitReadModel unit,
            byte factionId,
            DynamicBuffer<BuildingRuntimeFactionSummary> factionSummaries,
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> usableFuelSummaries)
        {
            if (unit.IsVehicle == 0)
                return true;

            if (usableFuelSummaries.IsCreated)
            {
                for (int i = 0; i < usableFuelSummaries.Length; i++)
                {
                    BuildingRuntimeFactionUsableFuelSummary summary = usableFuelSummaries[i];
                    if (summary.FactionId != factionId)
                        continue;

                    if (summary.CurrentFuelBarrels > 0f)
                        return true;
                    break;
                }
            }

            if (!factionSummaries.IsCreated)
                return false;

            for (int i = 0; i < factionSummaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = factionSummaries[i];
                if (summary.FactionId != factionId)
                    continue;

                return summary.StoredFuelBarrels > 0f || summary.FuelBarrelsPerDay > 0f;
            }

            return false;
        }

        private static bool TryResolveUnitReadModel(
            DynamicBuffer<BuildingConfiguredUnitReadModel> units,
            FixedString128Bytes unitId,
            out BuildingConfiguredUnitReadModel unit)
        {
            if (!units.IsCreated)
            {
                unit = default;
                return false;
            }

            for (int i = 0; i < units.Length; i++)
            {
                BuildingConfiguredUnitReadModel candidate = units[i];
                if (!candidate.UnitId.Equals(unitId))
                    continue;

                unit = candidate;
                return true;
            }

            unit = default;
            return false;
        }

        private static bool TryGetUnitProductionSummary(
            DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries,
            byte factionId,
            FixedString128Bytes unitId,
            out int producedCount,
            out int queuedCount)
        {
            if (summaries.IsCreated)
            {
                for (int i = 0; i < summaries.Length; i++)
                {
                    BuildingRuntimeUnitProductionSummary summary = summaries[i];
                    if (summary.FactionId != factionId || !summary.UnitId.Equals(unitId))
                        continue;

                    producedCount = summary.ProducedCount;
                    queuedCount = summary.QueuedCount;
                    return true;
                }
            }

            producedCount = 0;
            queuedCount = 0;
            return false;
        }

        private static bool HasPendingProductionRequest(
            DynamicBuffer<BuildingFactionUnitProductionRequest> requests,
            byte factionId,
            FixedString128Bytes unitId)
        {
            if (!requests.IsCreated)
                return false;

            for (int i = 0; i < requests.Length; i++)
            {
                BuildingFactionUnitProductionRequest request = requests[i];
                if (request.FactionId == factionId &&
                    request.UnitId.Equals(unitId) &&
                    request.Status == BuildingFactionUnitProductionRequest.Pending)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnqueueProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, FixedString128Bytes unitId)
        {
            DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
                state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity);
            requests.Add(new BuildingFactionUnitProductionRequest
            {
                RequestId = ++_nextProductionRequestId,
                FactionId = factionId,
                UnitId = unitId,
                Status = BuildingFactionUnitProductionRequest.Pending
            });
        }

        private void ProcessCompletedProductionRequests(
            ref SystemState state,
            Entity boundaryEntity,
            ref FactionEconomy economy,
            bool shouldLog)
        {
            if (!state.EntityManager.HasBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity))
                return;

            DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
                state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity);
            for (int i = requests.Length - 1; i >= 0; i--)
            {
                BuildingFactionUnitProductionRequest request = requests[i];
                if (request.FactionId != economy.FactionId ||
                    request.Status == BuildingFactionUnitProductionRequest.Pending)
                {
                    continue;
                }

                if (request.Status == BuildingFactionUnitProductionRequest.Succeeded)
                    economy.Money = math.max(0, economy.Money - math.max(0, request.Cost));

                if (shouldLog)
                {
                    EnqueueDiagnostic(
                        ref state,
                        $"[AIProduction] faction={request.FactionId} producer={request.ProducerDisplayName.ToString()} unit={request.UnitDisplayName.ToString()} cost={request.Cost} queue={request.QueueCount} result={ProductionResultLabel(request)}");
                }

                requests.RemoveAt(i);
            }
        }

        private static string ProductionResultLabel(BuildingFactionUnitProductionRequest request)
        {
            if (request.Status == BuildingFactionUnitProductionRequest.Succeeded)
                return "Queued";

            return request.ResultCode switch
            {
                1 => "MissingUnitConfig",
                2 => "MissingProducerBuilding",
                3 => "ProducerUnavailable",
                _ => "Failed"
            };
        }

        private static bool IsFactionAIControlled(byte factionId, bool hasControls, DynamicBuffer<FactionControlEntry> controls)
        {
            if (!hasControls)
                return FactionIdentity.IsAiControlledByDefault(factionId);

            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.FactionId == factionId)
                    return control.AIControlled != 0;
            }

            return FactionIdentity.IsAiControlledByDefault(factionId);
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
                return 0;

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static bool ShouldQueueDiagnostics(EntityQuery runtimeDiagnosticsQuery)
        {
            return runtimeDiagnosticsQuery.CalculateEntityCount() == 1 &&
                runtimeDiagnosticsQuery.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
        }

        private void EnqueueDiagnostic(ref SystemState state, FixedString512Bytes message)
        {
            if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
                return;

            EntityManager em = state.EntityManager;
            Entity queueEntity = _diagnosticLogQueueQuery.GetSingletonEntity();
            DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
            logs.Add(new AIDiagnosticLogComponent { Message = message });
        }

        private void EnsureDiagnosticLogQueue(ref SystemState state)
        {
            if (!_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
                return;

            EntityManager em = state.EntityManager;
            Entity queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
        }

        private void LogNoPlanIfNeeded(ref SystemState state, ref AIProductionPlan plan, float now, bool shouldLog)
        {
            if (now - plan.LastLogTime < LogIntervalSeconds)
                return;

            plan.LastLogTime = now;
            if (shouldLog)
                EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} result=NoPlan");
        }

        private readonly struct FactionEconomyRecord
        {
            public FactionEconomyRecord(Entity entity, FactionEconomy economy)
            {
                Entity = entity;
                Economy = economy;
            }

            public readonly Entity Entity;
            public readonly FactionEconomy Economy;
        }
    }
}
