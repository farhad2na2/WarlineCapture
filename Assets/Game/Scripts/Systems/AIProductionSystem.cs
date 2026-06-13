using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AIBuildPlannerSystem))]
public partial struct AIProductionSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;
    private int _nextProductionRequestId;
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityQuery _planQuery;
    private EntityQuery _economyQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<AIProductionPlan> _aiProductionPlanType;
    private BufferTypeHandle<AIProductionPlanEntry> _aiProductionPlanEntryType;
    private ComponentTypeHandle<FactionEconomy> _factionEconomyType;

    public void OnCreate(ref SystemState state)
    {
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingConfiguredUnitReadModel>(),
            ComponentType.ReadOnly<BuildingRuntimeUnitProductionSummary>(),
            ComponentType.ReadWrite<BuildingFactionUnitProductionRequest>());
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
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        if (!TryGetBuildingRuntimeBoundaryEntity(ref state, out Entity boundaryEntity))
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        bool shouldLog = ShouldQueueDiagnostics(ref state);
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
        float interval = Mathf.Max(0.1f, plan.UnitProductionIntervalSeconds);
        if (now - plan.LastProductionTime < interval)
            return;

        if (entries.Length == 0)
        {
            LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);
            return;
        }

        bool handledDecision = false;
        int attempts = math.max(1, entries.Length);
        int maxQueuedUnits = math.max(1, plan.MaxQueuedUnits);
        int targetProducedUnits = math.max(1, plan.TargetProducedUnits);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int entryIndex = PositiveModulo(plan.NextUnitIndex + attempt, entries.Length);
            FixedString128Bytes unitId = entries[entryIndex].UnitId;
            if (unitId.Length == 0)
                continue;

            TryGetUnitProductionSummary(ref state, boundaryEntity, plan.FactionId, unitId, out int producedCount, out int queuedCount);
            if (producedCount + queuedCount >= targetProducedUnits || queuedCount >= maxQueuedUnits)
                continue;

            handledDecision = true;
            if (HasPendingProductionRequest(ref state, boundaryEntity, plan.FactionId, unitId))
            {
                plan.LastProductionTime = now;
                break;
            }

            if (!TryResolveUnitReadModel(ref state, boundaryEntity, unitId, out BuildingConfiguredUnitReadModel unit) ||
                unit.CanRequest == 0)
            {
                plan.NextUnitIndex = entryIndex + 1;
                plan.LastProductionTime = now;
                if (shouldLog)
                    EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={unitId} result=MissingConfig");
                break;
            }

            int cost = Mathf.Max(0, unit.Price);
            if (economy.Money < cost)
            {
                plan.LastProductionTime = now;
                if (shouldLog)
                    EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={unit.DisplayName.ToString()} cost={cost} result=InsufficientFunds money={economy.Money}");
                break;
            }

            EnqueueProductionRequest(ref state, boundaryEntity, plan.FactionId, unitId);
            plan.LastProductionTime = now;
            plan.NextUnitIndex = entryIndex + 1;
            if (shouldLog)
                EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} unit={unit.DisplayName.ToString()} cost={cost} result=Requested");
            break;
        }

        if (!handledDecision && now - plan.LastLogTime >= LogIntervalSeconds)
        {
            plan.LastLogTime = now;
            if (shouldLog)
                EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} result=Complete");
        }
    }

    private bool TryGetBuildingRuntimeBoundaryEntity(ref SystemState state, out Entity entity)
    {
        entity = Entity.Null;
        if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
        return entity != Entity.Null && state.EntityManager.Exists(entity);
    }

    private bool TryResolveUnitReadModel(ref SystemState state, Entity boundaryEntity, FixedString128Bytes unitId, out BuildingConfiguredUnitReadModel unit)
    {
        unit = default;
        if (!state.EntityManager.HasBuffer<BuildingConfiguredUnitReadModel>(boundaryEntity))
            return false;

        DynamicBuffer<BuildingConfiguredUnitReadModel> units =
            state.EntityManager.GetBuffer<BuildingConfiguredUnitReadModel>(boundaryEntity, true);
        for (int i = 0; i < units.Length; i++)
        {
            BuildingConfiguredUnitReadModel candidate = units[i];
            if (!candidate.UnitId.Equals(unitId))
                continue;

            unit = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetUnitProductionSummary(
        ref SystemState state,
        Entity boundaryEntity,
        byte factionId,
        FixedString128Bytes unitId,
        out int producedCount,
        out int queuedCount)
    {
        producedCount = 0;
        queuedCount = 0;
        if (!state.EntityManager.HasBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity))
            return false;

        DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries =
            state.EntityManager.GetBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeUnitProductionSummary summary = summaries[i];
            if (summary.FactionId != factionId || !summary.UnitId.Equals(unitId))
                continue;

            producedCount = summary.ProducedCount;
            queuedCount = summary.QueuedCount;
            return true;
        }

        return false;
    }

    private bool HasPendingProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, FixedString128Bytes unitId)
    {
        if (!state.EntityManager.HasBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity))
            return false;

        DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
            state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity, true);
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
                economy.Money = Mathf.Max(0, economy.Money - Mathf.Max(0, request.Cost));

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
            return FactionIdentitySystem.IsAiControlledByDefault(factionId);

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return FactionIdentitySystem.IsAiControlledByDefault(factionId);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
            return 0;

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        return InitialUnitsRuntimeState.VerboseAILogs ||
            SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
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
