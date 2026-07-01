using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

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
        Request = 4
    }

    private struct ProductionCandidateEntry
    {
        public int EntryIndex;
        public FixedString128Bytes UnitId;
        public byte IsValid;
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
        using NativeList<ProductionCandidateEntry> candidateEntries = BuildProductionCandidateEntries(entries);
        using NativeArray<BuildingConfiguredUnitReadModel> units =
            CopyBoundaryBuffer<BuildingConfiguredUnitReadModel>(state.EntityManager, boundaryEntity, Allocator.TempJob);
        using NativeArray<BuildingRuntimeUnitProductionSummary> summaries =
            CopyBoundaryBuffer<BuildingRuntimeUnitProductionSummary>(state.EntityManager, boundaryEntity, Allocator.TempJob);
        using NativeArray<BuildingFactionUnitProductionRequest> requests =
            CopyBoundaryBuffer<BuildingFactionUnitProductionRequest>(state.EntityManager, boundaryEntity, Allocator.TempJob);
        using NativeReference<ProductionDecision> decision = new(Allocator.TempJob);

        new SelectProductionDecisionJob
        {
            Entries = candidateEntries.AsArray(),
            Units = units,
            Summaries = summaries,
            Requests = requests,
            Plan = plan,
            EconomyMoney = economyMoney,
            Decision = decision
        }.Schedule(state.Dependency).Complete();

        return decision.Value;
    }

    private static NativeList<ProductionCandidateEntry> BuildProductionCandidateEntries(DynamicBuffer<AIProductionPlanEntry> entries)
    {
        NativeList<ProductionCandidateEntry> candidateEntries = new(entries.Length, Allocator.TempJob);
        for (int i = 0; i < entries.Length; i++)
        {
            FixedString64Bytes unitId = entries[i].UnitId;
            if (unitId.Length == 0)
            {
                candidateEntries.Add(new ProductionCandidateEntry
                {
                    EntryIndex = i,
                    IsValid = 0
                });
                continue;
            }

            candidateEntries.Add(new ProductionCandidateEntry
            {
                EntryIndex = i,
                UnitId = unitId,
                IsValid = 1
            });
        }

        return candidateEntries;
    }

    private static NativeArray<T> CopyBoundaryBuffer<T>(
        EntityManager em,
        Entity boundaryEntity,
        Allocator allocator)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(boundaryEntity))
            return new NativeArray<T>(0, allocator);

        DynamicBuffer<T> buffer = em.GetBuffer<T>(boundaryEntity, true);
        NativeArray<T> copy = new(buffer.Length, allocator);
        for (int i = 0; i < buffer.Length; i++)
            copy[i] = buffer[i];

        return copy;
    }

    [BurstCompile]
    private struct SelectProductionDecisionJob : IJob
    {
        [ReadOnly] public NativeArray<ProductionCandidateEntry> Entries;
        [ReadOnly] public NativeArray<BuildingConfiguredUnitReadModel> Units;
        [ReadOnly] public NativeArray<BuildingRuntimeUnitProductionSummary> Summaries;
        [ReadOnly] public NativeArray<BuildingFactionUnitProductionRequest> Requests;
        public AIProductionPlan Plan;
        public int EconomyMoney;
        public NativeReference<ProductionDecision> Decision;

        public void Execute()
        {
            ProductionDecision decision = default;
            if (Entries.Length == 0)
            {
                Decision.Value = decision;
                return;
            }

            int attempts = math.max(1, Entries.Length);
            int maxQueuedUnits = math.max(1, Plan.MaxQueuedUnits);
            int targetProducedUnits = math.max(1, Plan.TargetProducedUnits);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateIndex = PositiveModulo(Plan.NextUnitIndex + attempt, Entries.Length);
                ProductionCandidateEntry entry = Entries[candidateIndex];
                if (entry.IsValid == 0)
                    continue;

                TryGetUnitProductionSummary(
                    Plan.FactionId,
                    entry.UnitId,
                    out int producedCount,
                    out int queuedCount);
                if (producedCount + queuedCount >= targetProducedUnits ||
                    queuedCount >= maxQueuedUnits)
                {
                    continue;
                }

                decision.EntryIndex = entry.EntryIndex;
                decision.UnitId = entry.UnitId;
                if (HasPendingProductionRequest(Plan.FactionId, entry.UnitId))
                {
                    decision.Result = ProductionDecisionResult.Pending;
                    break;
                }

                if (!TryResolveUnitReadModel(entry.UnitId, out BuildingConfiguredUnitReadModel unit) ||
                    unit.CanRequest == 0)
                {
                    decision.Result = ProductionDecisionResult.MissingConfig;
                    break;
                }

                int cost = math.max(0, unit.Price);
                decision.Unit = unit;
                decision.Cost = cost;
                if (EconomyMoney < cost)
                {
                    decision.Result = ProductionDecisionResult.InsufficientFunds;
                    break;
                }

                decision.Result = ProductionDecisionResult.Request;
                break;
            }

            Decision.Value = decision;
        }

        private bool TryResolveUnitReadModel(
            FixedString128Bytes unitId,
            out BuildingConfiguredUnitReadModel unit)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                BuildingConfiguredUnitReadModel candidate = Units[i];
                if (!candidate.UnitId.Equals(unitId))
                    continue;

                unit = candidate;
                return true;
            }

            unit = default;
            return false;
        }

        private bool TryGetUnitProductionSummary(
            byte factionId,
            FixedString128Bytes unitId,
            out int producedCount,
            out int queuedCount)
        {
            for (int i = 0; i < Summaries.Length; i++)
            {
                BuildingRuntimeUnitProductionSummary summary = Summaries[i];
                if (summary.FactionId != factionId || !summary.UnitId.Equals(unitId))
                    continue;

                producedCount = summary.ProducedCount;
                queuedCount = summary.QueuedCount;
                return true;
            }

            producedCount = 0;
            queuedCount = 0;
            return false;
        }

        private bool HasPendingProductionRequest(byte factionId, FixedString128Bytes unitId)
        {
            for (int i = 0; i < Requests.Length; i++)
            {
                BuildingFactionUnitProductionRequest request = Requests[i];
                if (request.FactionId == factionId &&
                    request.UnitId.Equals(unitId) &&
                    request.Status == BuildingFactionUnitProductionRequest.Pending)
                {
                    return true;
                }
            }

            return false;
        }
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
        if (InitialUnitsRuntimeState.VerboseAILogs)
            return true;

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
