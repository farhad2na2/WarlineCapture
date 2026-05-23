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
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        NativeArray<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true).ToNativeArray(Allocator.Temp)
            : default;
        bool shouldLog = ShouldQueueDiagnostics(ref state);

        EntityManager em = state.EntityManager;
        EntityQuery planQuery = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>(), ComponentType.ReadOnly<AIProductionPlanEntry>());
        using NativeArray<Entity> planEntities = planQuery.ToEntityArray(Allocator.Temp);
        planQuery.Dispose();

        for (int i = 0; i < planEntities.Length; i++)
        {
            Entity planEntity = planEntities[i];
            AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(planEntity);
            if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                continue;

            if (!TryFindEconomyEntity(em, plan.FactionId, out Entity economyEntity, out FactionEconomy economy))
                continue;

            ProcessCompletedProductionRequests(ref state, boundaryEntity, ref economy, shouldLog);
            em.SetComponentData(economyEntity, economy);

            float interval = Mathf.Max(0.1f, plan.UnitProductionIntervalSeconds);
            if (now - plan.LastProductionTime < interval)
                continue;

            DynamicBuffer<AIProductionPlanEntry> entries = em.GetBuffer<AIProductionPlanEntry>(planEntity);
            if (entries.Length == 0)
            {
                LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);
                em.SetComponentData(planEntity, plan);
                continue;
            }

            bool handledDecision = false;
            int attempts = math.max(1, entries.Length);
            int maxQueuedUnits = math.max(1, plan.MaxQueuedUnits);
            int targetProducedUnits = math.max(1, plan.TargetProducedUnits);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int entryIndex = PositiveModulo(plan.NextUnitIndex + attempt, entries.Length);
                string unitId = entries[entryIndex].UnitId.ToString();
                if (string.IsNullOrWhiteSpace(unitId))
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

            em.SetComponentData(planEntity, plan);
        }

        if (controls.IsCreated)
            controls.Dispose();
    }

    private static bool TryFindEconomyEntity(EntityManager em, byte factionId, out Entity entity, out FactionEconomy economy)
    {
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy candidate = em.GetComponentData<FactionEconomy>(entities[i]);
            if (candidate.FactionId != factionId)
                continue;

            entity = entities[i];
            economy = candidate;
            return true;
        }

        entity = Entity.Null;
        economy = default;
        return false;
    }

    private bool TryGetBuildingRuntimeBoundaryEntity(ref SystemState state, out Entity entity)
    {
        entity = Entity.Null;
        if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
        return entity != Entity.Null && state.EntityManager.Exists(entity);
    }

    private bool TryResolveUnitReadModel(ref SystemState state, Entity boundaryEntity, string unitId, out BuildingConfiguredUnitReadModel unit)
    {
        unit = default;
        if (!state.EntityManager.HasBuffer<BuildingConfiguredUnitReadModel>(boundaryEntity))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(unitId);
        DynamicBuffer<BuildingConfiguredUnitReadModel> units =
            state.EntityManager.GetBuffer<BuildingConfiguredUnitReadModel>(boundaryEntity, true);
        for (int i = 0; i < units.Length; i++)
        {
            BuildingConfiguredUnitReadModel candidate = units[i];
            if (candidate.UnitId.ToString() != normalized)
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
        string unitId,
        out int producedCount,
        out int queuedCount)
    {
        producedCount = 0;
        queuedCount = 0;
        if (!state.EntityManager.HasBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(unitId);
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries =
            state.EntityManager.GetBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeUnitProductionSummary summary = summaries[i];
            if (summary.FactionId != factionId || summary.UnitId.ToString() != normalized)
                continue;

            producedCount = summary.ProducedCount;
            queuedCount = summary.QueuedCount;
            return true;
        }

        return false;
    }

    private bool HasPendingProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, string unitId)
    {
        if (!state.EntityManager.HasBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(unitId);
        DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
            state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity, true);
        for (int i = 0; i < requests.Length; i++)
        {
            BuildingFactionUnitProductionRequest request = requests[i];
            if (request.FactionId == factionId &&
                request.UnitId.ToString() == normalized &&
                request.Status == BuildingFactionUnitProductionRequest.Pending)
            {
                return true;
            }
        }

        return false;
    }

    private void EnqueueProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, string unitId)
    {
        DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
            state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity);
        requests.Add(new BuildingFactionUnitProductionRequest
        {
            RequestId = ++_nextProductionRequestId,
            FactionId = factionId,
            UnitId = ToFixedString128(BuildingDefinitionSystem.NormalizeSpawnableKey(unitId)),
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

    private static bool IsFactionAIControlled(byte factionId, bool hasControls, NativeArray<FactionControlEntry> controls)
    {
        if (!hasControls)
            return factionId != 0;

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return factionId != 0;
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
        if (Application.isBatchMode)
            return true;

        return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
    }

    private void EnqueueDiagnostic(ref SystemState state, FixedString512Bytes message)
    {
        EntityManager em = state.EntityManager;
        Entity queueEntity;
        if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
        {
            queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
        }
        else
        {
            queueEntity = _diagnosticLogQueueQuery.GetSingletonEntity();
        }

        DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
        logs.Add(new AIDiagnosticLogComponent { Message = message });
    }

    private static FixedString128Bytes ToFixedString128(string value)
    {
        return new FixedString128Bytes(value ?? string.Empty);
    }

    private void LogNoPlanIfNeeded(ref SystemState state, ref AIProductionPlan plan, float now, bool shouldLog)
    {
        if (now - plan.LastLogTime < LogIntervalSeconds)
            return;

        plan.LastLogTime = now;
        if (shouldLog)
            EnqueueDiagnostic(ref state, $"[AIProduction] faction={plan.FactionId} result=NoPlan");
    }
}
